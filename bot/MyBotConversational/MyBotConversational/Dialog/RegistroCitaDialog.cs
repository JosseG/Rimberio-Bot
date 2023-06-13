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
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Linq;
using System.Collections;

namespace MyBotConversational.Dialog
{
    public class RegistroCitaDialog : CancelAndHelpDialog
    {

        private readonly HttpClient _httpClient;

        private const string IntroStepMsgText = "Escogio la intención de registrar cita";
        private const string CodigoUsuarioStepMsgText = "¿Cual es su código de usuario?";
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
                IdUsuarioStepAsync,
                VerifyIdUsuarioStepAsync,
                TokenStepAsync,
                VerifyTokenStepAsync,
                AnimalStepAsync,
                ServiciosInfoStepAsync,
                FechaStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }



        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var message = MessageFactory.Text(IntroStepMsgText, IntroStepMsgText, InputHints.ExpectingInput);
            await stepContext.Context.SendActivityAsync(message, cancellationToken);

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> IdUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;



            if (citaRDetalles.idUsuario == 0 || citaRDetalles.idUsuario == -1)
            {

                Debug.WriteLine("2---------------------------------------");
                var promptMessage = MessageFactory.Text(CodigoUsuarioStepMsgText, CodigoUsuarioStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.idUsuario, cancellationToken);
        }

        private async Task<DialogTurnResult> VerifyIdUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;
            citaRDetalles.idUsuario = long.Parse((string)stepContext.Result);


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/usuario/{citaRDetalles.idUsuario}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            Usuario usuario = JsonSerializer.Deserialize<Usuario>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            if (usuario == null)
            {
                Debug.WriteLine("1---------------------------------------");
                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 2;

            }
            else
            {
                citaRDetalles.idUsuario = usuario.codigo;
            }




            return await stepContext.NextAsync(citaRDetalles.idUsuario, cancellationToken);
        }

        private async Task<DialogTurnResult> TokenStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;
            citaRDetalles.idUsuario = (long)stepContext.Result;

            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/usuario/token/generar/{citaRDetalles.idUsuario}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            var tokenresult = JsonSerializer.Deserialize<TokenBot>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            Debug.WriteLine("Este es mi token " + tokenresult.token);

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
                RequestUri = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/usuario/token/{token}/{citaRDetalles.idUsuario}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            bool isVerifyToken = JsonSerializer.Deserialize<bool>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            if (!isVerifyToken)
            {
                Debug.WriteLine("2---------------------------------------");
                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 5;

            }

            return await stepContext.NextAsync(citaRDetalles.idUsuario, cancellationToken);

        }


        private async Task<DialogTurnResult> AnimalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;




            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/mascota/usuario/{citaRDetalles.idUsuario}"),
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
                    Value = mascotas[i].nombre,
                    Action = cardAction
                });
                listastringmascotas = listastringmascotas + " " + mascotas[i].nombre;
            }


            var Texto = $"Tienes estas mascotas registradas, seleccione una mascota a tratar";


            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = listaOpciones, Style = ListStyle.HeroCard }, cancellationToken);

        }




        private async Task<DialogTurnResult> ServiciosInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.nombreMascota = ((FoundChoice)stepContext.Result).Value;



            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("http://127.0.0.1:8080/api/v1/reservaciones/servicios"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Servicio> servicios = JsonSerializer.Deserialize<List<Servicio>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            var listaOpciones = new List<Choice>();

            for (int i = 0; i < servicios.Count; i++)
            {
                var cardAction = new CardAction();
                cardAction.Title = servicios[i].tipo;
                cardAction.Value = servicios[i].tipo;
                cardAction.Type = "imBack";

                listaOpciones.Add(new Choice()
                {
                    Value = servicios[i].tipo,
                    Action = cardAction
                });
            }


            var Texto = $"Tenemos disponible los siguientes servicios, seleccione uno de estos servicios";

            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = listaOpciones, Style = ListStyle.HeroCard }, cancellationToken);

        }

        private async Task<DialogTurnResult> FechaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            //citaRDetalles.nombreServicio = (string)stepContext.Result;
            citaRDetalles.nombreServicio = ((FoundChoice)stepContext.Result).Value;

            Debug.WriteLine("El nombre del servicio es " + citaRDetalles.nombreServicio);


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/horario/service/{citaRDetalles.nombreServicio}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Horario> horarios = JsonSerializer.Deserialize<List<Horario>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });



            string listastringhorarios = "";
            var listaOpciones = new List<Choice>();


            for (int i = 0; i < horarios.Count; i++)
            {
                var cardAction = new CardAction();
                cardAction.Title = horarios[i].fecha.ToString();
                cardAction.Value = horarios[i].codigo.ToString();
                cardAction.Type = "imBack";

                listaOpciones.Add(new Choice()
                {
                    Value = horarios[i].codigo.ToString(),
                    Action = cardAction
                });
            }
            var Texto = $"Tenemos disponible los siguientes horarios para ese servicio, seleccione uno de ellos";

            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = listaOpciones, Style = ListStyle.HeroCard }, cancellationToken);

        }



        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            //citaRDetalles.idFecha = long.Parse((string)stepContext.Result);
            citaRDetalles.idFecha = long.Parse(((FoundChoice)stepContext.Result).Value);
            Debug.WriteLine("El código de fecha es  " + citaRDetalles.idFecha);


            var messageText = $"Por favor confirma, el id del usuario es ->  {citaRDetalles.idUsuario} , la mascota es -> {citaRDetalles.nombreMascota} y el código de la fecha es -> {citaRDetalles.idFecha}. ¿Es correcto?";
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
                    idUsuario = citaRDetalles.idUsuario,
                    nombreMascota = citaRDetalles.nombreMascota,
                    idFecha = citaRDetalles.idFecha
                };

                HttpRequestMessage msg = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("http://127.0.0.1:8080/api/v1/reservaciones"),
                    Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json")

                };

                var response = await _httpClient.SendAsync(msg);

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
