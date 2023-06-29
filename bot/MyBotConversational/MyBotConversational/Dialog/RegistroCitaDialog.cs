using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Json;
using MyBotConversational.ModelsApi;
using System;
using System.Diagnostics;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;

namespace MyBotConversational.Dialog
{
    public class RegistroCitaDialog : CancelAndHelpDialog
    {

        private readonly HttpClient _httpClient;

        private const string IntroStepMsgText = "Escogio la intención de registrar cita";
        private const string UsernameUsuarioStepMsgText = "¿Cual es su nombre de usuario?";
        private const string TokenStepMsgText = "Fue enviado un token a su bandeja de correo, inserte su token en el siguiente mensaje";
        private const string MascotaStepMsgText = "¿Que mascota va a traer?";

        public RegistroCitaDialog(HttpClient httpClient)
            : base(nameof(RegistroCitaDialog))
        {

            this._httpClient = httpClient;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                UsernameUsuarioStepAsync,
                VerifyUsernameUsuarioStepAsync,
                TokenStepAsync,
                VerifyTokenStepAsync,
                MascotasStepAsync,
                EspecialidadesStepAsync,
                VeterinariosStepAsync,
                HoraStepAsync,
                FechaStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }



        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var message = MessageFactory.Text(IntroStepMsgText, IntroStepMsgText, InputHints.ExpectingInput);

            await stepContext.Context.SendActivityAsync(message, cancellationToken);

            var dateNow = DateOnly.FromDateTime(DateTime.Now);

            CultureInfo culture = new CultureInfo("es-MX");

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> UsernameUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            var promptMessage = MessageFactory.Text(UsernameUsuarioStepMsgText, UsernameUsuarioStepMsgText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> VerifyUsernameUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;
            citaRDetalles.username = (string)stepContext.Result;


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"https://rimberioback-production.up.railway.app/rimbeiro/usuario/{citaRDetalles.username}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            Usuario usuario = JsonSerializer.Deserialize<Usuario>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            if (usuario == null)
            {

                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 2;

            }
            else
            {
                citaRDetalles.idUsuario = usuario.id;
                citaRDetalles.username = usuario.username;
            }




            return await stepContext.NextAsync(citaRDetalles.username, cancellationToken);
        }

        private async Task<DialogTurnResult> TokenStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;
            citaRDetalles.username = (string)stepContext.Result;

            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"https://rimberioback-production.up.railway.app/rimbeiro/token/usuario/generar/{citaRDetalles.username}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            var tokenresult = JsonSerializer.Deserialize<TokenBot>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            var promptMessage = MessageFactory.Text(TokenStepMsgText, TokenStepMsgText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> VerifyTokenStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            var token = (string)stepContext.Result;


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"https://rimberioback-production.up.railway.app/rimbeiro/token/usuario/{token}/{citaRDetalles.idUsuario}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            var isVerifyToken = JsonSerializer.Deserialize<object>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            var boolResutlToken = Convert.ToBoolean(isVerifyToken.ToString());

            if (!boolResutlToken)
            {
                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 5;

            }

            return await stepContext.NextAsync(citaRDetalles.idUsuario, cancellationToken);

        }


        private async Task<DialogTurnResult> MascotasStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;




            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"https://rimberioback-production.up.railway.app/rimbeiro/mascota/usuario/{citaRDetalles.username}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Mascota> mascotas = JsonSerializer.Deserialize<List<Mascota>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });



            string listastringmascotas = "";
            var listaOpciones = new List<Choice>();

            for (int i = 0; i < mascotas.Count; i++)
            {
                var cardAction = new CardAction();
                cardAction.Title = mascotas[i].nombre;
                cardAction.Value = mascotas[i].nombre;
                cardAction.Type = "imBack";



                listaOpciones.Add(new Choice()
                {
                    //Value = mascotas[i].id.ToString(),
                    Value = JsonSerializer.Serialize<Mascota>(mascotas[i]),
                    Action = cardAction
                });
                listastringmascotas = listastringmascotas + " " + mascotas[i].nombre;
            }


            var Texto = $"Tienes estas mascotas registradas, seleccione una mascota a tratar";


            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = listaOpciones, Style = ListStyle.HeroCard }, cancellationToken);

        }




        private async Task<DialogTurnResult> EspecialidadesStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;


            citaRDetalles.mascota = JsonSerializer.Deserialize<Mascota>(((FoundChoice)stepContext.Result).Value);



            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://rimberioback-production.up.railway.app/rimbeiro/especialidades"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Especialidad> especialidades = JsonSerializer.Deserialize<List<Especialidad>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            var listaOpciones = new List<Choice>();

            for (int i = 0; i < especialidades.Count; i++)
            {
                var cardAction = new CardAction();
                cardAction.Title = especialidades[i].descripcion;
                cardAction.Value = especialidades[i].descripcion;
                cardAction.Type = "imBack";

                listaOpciones.Add(new Choice()
                {
                    Value = especialidades[i].Id.ToString(),
                    Action = cardAction
                });
            }


            var Texto = $"Tenemos disponible las siguientes especialidades, seleccione una de ellas";

            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = listaOpciones, Style = ListStyle.HeroCard }, cancellationToken);

        }


        private async Task<DialogTurnResult> VeterinariosStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.idEspecialidad = long.Parse(((FoundChoice)stepContext.Result).Value);


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"https://rimberioback-production.up.railway.app/rimbeiro/veterinario/especialidad/{citaRDetalles.idEspecialidad}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Veterinario> veterinarios = JsonSerializer.Deserialize<List<Veterinario>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });



            var listaOpciones = new List<Choice>();

            for (int i = 0; i < veterinarios.Count; i++)
            {
                var cardAction = new CardAction();
                cardAction.Title = veterinarios[i].nombres;
                cardAction.Value = veterinarios[i].nombres;
                cardAction.Type = "imBack";



                listaOpciones.Add(new Choice()
                {
                    Value = JsonSerializer.Serialize<Veterinario>(veterinarios[i]),
                    Action = cardAction
                });
            }


            var Texto = $"Veterinarios que gestionan esa especialidad, escoge a cualquiera de ellos";

            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = listaOpciones, Style = ListStyle.HeroCard }, cancellationToken);
        }

        private async Task<DialogTurnResult> HoraStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;


            citaRDetalles.veterinario = JsonSerializer.Deserialize<Veterinario>(((FoundChoice)stepContext.Result).Value);



            Debug.WriteLine("El id del veterinario es " + citaRDetalles.idVeterinario);


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"https://rimberioback-production.up.railway.app/rimbeiro/horarios/veterinario/{citaRDetalles.veterinario.id}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Horario> horarios = JsonSerializer.Deserialize<List<Horario>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            var listaOpciones = new List<Choice>();


            for (int i = 0; i < horarios.Count; i++)
            {
                Debug.WriteLine(horarios[i].id);
                var cardAction = new CardAction();
                cardAction.Title = horarios[i].horaInicio + " - " + horarios[i].horaFin;
                cardAction.Value = horarios[i].horaInicio + " - " + horarios[i].horaFin;
                cardAction.Type = "imBack";

                listaOpciones.Add(new Choice()
                {
                    Value = JsonSerializer.Serialize<Horario>(horarios[i]),
                    Action = cardAction
                });
            }
            var Texto = $"Tenemos disponible los siguientes horarios para ese servicio, seleccione uno de ellos";

            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = listaOpciones, Style = ListStyle.HeroCard }, cancellationToken);
        }

        private async Task<DialogTurnResult> FechaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;
            citaRDetalles.horario = JsonSerializer.Deserialize<Horario>(((FoundChoice)stepContext.Result).Value);
            Debug.WriteLine(citaRDetalles.horario.id);

            var Texto = $"Dígite la fecha de la reserva con el siguiente formato año-mes-dia / aaaa-mm-dd";

            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }



        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.fecha = (string)stepContext.Result;

            var messageText = $"Por favor confirma, el nombre del usuario es ->  {citaRDetalles.username} , la mascota es -> {citaRDetalles.mascota.nombre} y la fecha de la reserva es -> {citaRDetalles.fecha}. ¿Es correcto?";

            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var citaRDetalles = (CitaRDetalles)stepContext.Options;

                var parameters = new
                {
                    idmascota = citaRDetalles.mascota.id,
                    idveterinario = citaRDetalles.veterinario.id,
                    fecha = citaRDetalles.fecha,
                    hora = citaRDetalles.horario.horaInicio
                };

                HttpRequestMessage msg = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://rimberioback-production.up.railway.app/rimbeiro/reserva"),
                    Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json")

                };

                var response = await _httpClient.SendAsync(msg);

                Debug.WriteLine("La respuesta " + response);

                return await stepContext.EndDialogAsync(citaRDetalles, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.DateTime);

        }


        private HttpContent ValidateContent(HttpResponseMessage response)
        {
            if (string.IsNullOrEmpty(response.Content?.ReadAsStringAsync().Result))
            {
                return response.Content = new StringContent("null", Encoding.UTF8, MediaTypeNames.Application.Json);
            }
            else
            {
                return response.Content;
            }
        }

    }
}
