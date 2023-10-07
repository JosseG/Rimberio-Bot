using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using MyBotConversational.ModelsApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyBotConversational.Dialog
{
    public class RegistroCitaDialog : CancelAndHelpDialog
    {

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;


        private const string IntroStepMsgText = "Escogio la intención de registrar cita  ✏️ 📎 📋";
        private const string UsernameUsuarioStepMsgText = "¿Cual es su nombre de usuario? 👩 👦";
        private const string TokenStepMsgText = "Fue enviado un token a su bandeja de correo, inserte su token en el siguiente mensaje 🗝 📧";
        private const string MascotaStepMsgText = "¿Que mascota va a traer? 🐶 🐱 🐹 🐰";

        public RegistroCitaDialog(HttpClient httpClient, IConfiguration config)
            : base(nameof(RegistroCitaDialog))
        {

            this._config = config;

            this._httpClient = httpClient;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new DateResolverDialog());

            var flow0Dialog = new WaterfallDialog("flow0", new WaterfallStep[]
            {
                IntroStepAsync,

            });
            AddDialog(flow0Dialog);

            var flow1Dialog = new WaterfallDialog("flow1", new WaterfallStep[]
            {

                UsernameUsuarioStepAsync,
                VerifyUsernameUsuarioStepAsync

            })
            ;
            AddDialog(flow1Dialog);

            var flowChooseTypeInteractionWithSystem = new WaterfallDialog("flow2", new WaterfallStep[]
            {
                VerifyBeforeFinishStep,
                VerifyFinishStep,
            });
            AddDialog(flowChooseTypeInteractionWithSystem);

            var flowTokenManagement = new WaterfallDialog("flow3", new WaterfallStep[]
            {
                TokenStepAsync,
                VerifyTokenStepAsync

            });
            AddDialog(flowTokenManagement);
            var flowBusinessLogic = new WaterfallDialog("flow4", new WaterfallStep[]
            {
                MascotasStepAsync,
                EspecialidadesStepAsync,
                VeterinariosStepAsync,
                HoraStepAsync,
                FechaStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            });
            AddDialog(flowBusinessLogic);

            var emailTemporal = new WaterfallDialog("flow5", new WaterfallStep[]
            {
                GetEmailTemporalStepAsync,
                SetEmailTemporalStepAsync,
            });
            AddDialog(emailTemporal);

            InitialDialogId = "flow0";
        }



        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(IntroStepMsgText, cancellationToken: cancellationToken);

            var dateNow = DateOnly.FromDateTime(DateTime.Now);
            CultureInfo culture = new CultureInfo("es-MX");


            return await stepContext.BeginDialogAsync("flow1", stepContext.Options, cancellationToken: cancellationToken);
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
                RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/usuario/{citaRDetalles.username}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            Usuario usuario = JsonSerializer.Deserialize<Usuario>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            if (usuario == null)
            {
                citaRDetalles.tipoInteraccion = 2;
                return await stepContext.ReplaceDialogAsync("flow2", stepContext.Options, cancellationToken: cancellationToken);

            }
            else
            {
                citaRDetalles.idUsuario = usuario.id;
                citaRDetalles.username = usuario.username;
                citaRDetalles.tipoInteraccion = 1;
                return await stepContext.ReplaceDialogAsync("flow3", stepContext.Options, cancellationToken: cancellationToken);
            }

        }


        private async Task<DialogTurnResult> VerifyBeforeFinishStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            await stepContext.Context.SendActivityAsync($@"Se verificó que el usuario ingresado no se encuentra registrado, sin embargo hay 2 opciones a manejar para la gestión del registro. {Environment.NewLine}{Environment.NewLine} La primera opción es para volver a iniciar sesión con un usuario válido. {Environment.NewLine}{Environment.NewLine} La segunda opción es para iniciar sesión con un usuario predeterminado, sin embargo tiene la posibilidad de colocar su correo eléctronico personalizado.", cancellationToken: cancellationToken);




            var listaOpciones = new List<Choice>();

            var cardAction = new CardAction();
            cardAction.Title = "1";
            cardAction.Value = "1";
            cardAction.Type = "imBack";

            listaOpciones.Add(new Choice()
            {
                Value = "1",
                Action = cardAction,

            });

            var cardAction2 = new CardAction();
            cardAction2.Title = "2";
            cardAction2.Value = "2";
            cardAction2.Type = "imBack";

            listaOpciones.Add(new Choice()
            {
                Value = "2",
                Action = cardAction2,

            });



            var promptMessage = MessageFactory.Text("Seleccione entre la opción 1 u 2 para gestionar la reserva", "Seleccione entre la opción 1 u 2 para gestionar la reserva", InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = promptMessage,
                Choices = listaOpciones,
                Style = ListStyle.HeroCard
            }, cancellationToken: cancellationToken);
        }


        private async Task<DialogTurnResult> VerifyFinishStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;
            string a = ((FoundChoice)stepContext.Result).Value;
            if (long.Parse(a) == 1)
            {
                return await stepContext.ReplaceDialogAsync("flow1", stepContext.Options, cancellationToken: cancellationToken);
            }


            return await stepContext.ReplaceDialogAsync("flow5", stepContext.Options, cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> TokenStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            if (citaRDetalles.tipoInteraccion == 2)
            {
                citaRDetalles.username = "nicole07";
                HttpRequestMessage msg = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/token/usuario/generarConUserAdmin/{citaRDetalles.username}/{citaRDetalles.temporalEmail}"),
                };

                var response = await _httpClient.SendAsync(msg);

                var content = await ValidateContent(response).ReadAsStringAsync();

                var tokenresult = JsonSerializer.Deserialize<TokenBot>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                citaRDetalles.idUsuario = tokenresult.usuario.id;


            }
            else
            {
                HttpRequestMessage msg = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/token/usuario/generar/{citaRDetalles.username}"),
                };

                var response = await _httpClient.SendAsync(msg);

                var content = await ValidateContent(response).ReadAsStringAsync();

                var tokenresult = JsonSerializer.Deserialize<TokenBot>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            }


            await stepContext.Context.SendActivityAsync("Se está gestionando su token, espere un momento :)", cancellationToken: cancellationToken);


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
                RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/token/usuario/{token}/{citaRDetalles.idUsuario}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            var isVerifyToken = JsonSerializer.Deserialize<object>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            var boolResutlToken = Convert.ToBoolean(isVerifyToken.ToString());


            if (!boolResutlToken)
            {
                return await stepContext.ReplaceDialogAsync("flow0", stepContext.Options, cancellationToken);

            }

            return await stepContext.ReplaceDialogAsync("flow4", stepContext.Options, cancellationToken);

        }


        private async Task<DialogTurnResult> MascotasStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;




            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/usuariomascota/{citaRDetalles.idUsuario}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Mascota> mascotas = JsonSerializer.Deserialize<List<Mascota>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            var listaOpciones = new List<Choice>();

            for (int i = 0; i < mascotas.Count; i++)
            {
                var cardAction = new CardAction();
                cardAction.Title = mascotas[i].nombre;
                cardAction.Value = mascotas[i].nombre;
                cardAction.Type = "imBack";

                listaOpciones.Add(new Choice()
                {
                    Value = JsonSerializer.Serialize<Mascota>(mascotas[i]),
                    Action = cardAction
                });

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
                //https://rimberiobackmejorado-production.up.railway.app/rimbeiro/especialidades
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http{_config.GetValue<string>(
                    "backendUrl")}/rimbeiro/especialidades"),
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

            var queryParameters = new Dictionary<string, string>
            {
                { "idEspecialidad", citaRDetalles.idEspecialidad.ToString() }
            };
            var dictFormUrlEncoded = new FormUrlEncodedContent(queryParameters);
            var queryString = await dictFormUrlEncoded.ReadAsStringAsync();


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/veterinarios?{queryString}"),


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
                    //Value = veterinarios[i].id.ToString(),
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



            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http{_config.GetValue<string>(
                    "backendUrl")}/rimbeiro/horarios/{citaRDetalles.veterinario.id}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Horario> horarios = JsonSerializer.Deserialize<List<Horario>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            var listaOpciones = new List<Choice>();


            for (int i = 0; i < horarios.Count; i++)
            {
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

            var Texto = $"Dígite la fecha de la reserva con el siguiente formato año-mes-dia / aaaa-mm-dd";

            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }












        private async Task<DialogTurnResult> GetEmailTemporalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;


            var Texto = $"Inserte el email temporal";

            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> SetEmailTemporalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;
            citaRDetalles.temporalEmail = (string)stepContext.Result;


            return await stepContext.ReplaceDialogAsync("flow3", stepContext.Options, cancellationToken: cancellationToken);

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
                    mascota = citaRDetalles.mascota,
                    veterinario = citaRDetalles.veterinario,
                    fecha = citaRDetalles.fecha,
                    hora = citaRDetalles.horario.horaInicio
                };

                HttpRequestMessage msg = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/reserva"),
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
