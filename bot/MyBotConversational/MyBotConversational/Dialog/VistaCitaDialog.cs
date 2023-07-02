using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;
using System.Net.Http;
using System;
using System.Globalization;
using System.Diagnostics;
using System.Net.Mime;
using System.Text;

using MyBotConversational.ModelsApi;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MyBotConversational.Dialog
{
    public class VistaCitaDialog : CancelAndHelpDialog
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        private const string IntroStepMsgText = "Escogio la intención de ver cita  👀 📋";
        private const string UsernameUsuarioStepMsgText = "¿Cual es su nombre de usuario? 👩 👦";
        private const string TokenStepMsgText = "Fue enviado un token a su bandeja de correo, inserte su token en el siguiente mensaje 🗝 📧";
        private const string MascotaStepMsgText = "¿Que mascota va a traer? 🐶 🐱 🐹 🐰";
        public VistaCitaDialog(HttpClient httpClient, IConfiguration config)
            : base(nameof(VistaCitaDialog))
        {
            this._httpClient = httpClient;
            this._config = config;


            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

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
                DetallesMascotaStepAsync,
                FinalStepAsync
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
            //var message = MessageFactory.Text(IntroStepMsgText, IntroStepMsgText, InputHints.ExpectingInput);
            await stepContext.Context.SendActivityAsync(IntroStepMsgText, cancellationToken: cancellationToken);

            var dateNow = DateOnly.FromDateTime(DateTime.Now);
            CultureInfo culture = new CultureInfo("es-MX");


            return await stepContext.BeginDialogAsync("flow1", stepContext.Options, cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> UsernameUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;

            var promptMessage = MessageFactory.Text(UsernameUsuarioStepMsgText, UsernameUsuarioStepMsgText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> VerifyUsernameUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;

            citaVDetalles.username = (string)stepContext.Result;


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/usuario/{citaVDetalles.username}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            Usuario usuario = JsonSerializer.Deserialize<Usuario>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            if (usuario == null)
            {

                citaVDetalles.tipoInteraccion = 2;
                return await stepContext.ReplaceDialogAsync("flow2", stepContext.Options, cancellationToken: cancellationToken);

            }
            else
            {
                citaVDetalles.idUsuario = usuario.id;
                citaVDetalles.username = usuario.username;
                citaVDetalles.tipoInteraccion = 1;
                return await stepContext.ReplaceDialogAsync("flow3", stepContext.Options, cancellationToken: cancellationToken);
            }

        }


        private async Task<DialogTurnResult> VerifyBeforeFinishStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            /*var card = new HeroCard
           {
               Text = $@"Se verificó que el usuario ingresado no se encuentra registrado, sin embargo hay 2 opciones a manejar para la gestión del registro. {Environment.NewLine}{Environment.NewLine} La primera opción es para volver a iniciar sesión con un usuario válido. {Environment.NewLine}{Environment.NewLine} La segunda opción es para iniciar sesión con un usuario predeterminado, sin embargo tiene la posibilidad de colocar su correo eléctronico personalizado.",
              Images = new CardImage[] {
                   new CardImage(url: $"https://i.postimg.cc/HnRypyMX/Mesa-de-trabajo-1.png"),
               }

           };

           var reply = MessageFactory.Attachment(card.ToAttachment());*/


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
            var citaVDetalles = (CitaVDetalles)stepContext.Options;
            string a = ((FoundChoice)stepContext.Result).Value;
            if (long.Parse(a) == 1)
            {
                return await stepContext.ReplaceDialogAsync("flow1", stepContext.Options, cancellationToken: cancellationToken);
            }


            return await stepContext.ReplaceDialogAsync("flow5", stepContext.Options, cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> TokenStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;
            //citaRDetalles.username = (string)stepContext.Result;

            if (citaVDetalles.tipoInteraccion == 2)
            {
                citaVDetalles.username = "nicole07";
                HttpRequestMessage msg = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/token/usuario/generarConUserAdmin/{citaVDetalles.username}/{citaVDetalles.temporalEmail}"),
                };

                var response = await _httpClient.SendAsync(msg);

                var content = await ValidateContent(response).ReadAsStringAsync();

                var tokenresult = JsonSerializer.Deserialize<TokenBot>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                citaVDetalles.idUsuario = tokenresult.usuario.id;

                Debug.WriteLine("Este es mi token " + tokenresult.token);

            }
            else
            {
                HttpRequestMessage msg = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/token/usuario/generar/{citaVDetalles.username}"),
                };

                var response = await _httpClient.SendAsync(msg);

                var content = await ValidateContent(response).ReadAsStringAsync();

                var tokenresult = JsonSerializer.Deserialize<TokenBot>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                Debug.WriteLine("Este es mi token " + tokenresult.token);

            }





            var card = new HeroCard
            {
                Text = "Se está gestionando su token, espere un momento :) ",
                /*Images = new CardImage[] {
                    new CardImage(url: $"https://i.postimg.cc/HnRypyMX/Mesa-de-trabajo-1.png"),
                }*/

            };

            var attachments = new List<Attachment>()
            {
                card.ToAttachment()

            };

            var reply = MessageFactory.Carousel(attachments);


            await stepContext.Context.SendActivityAsync(reply, cancellationToken: cancellationToken);


            var promptMessage = MessageFactory.Text(TokenStepMsgText, TokenStepMsgText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> VerifyTokenStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;


            var token = (string)stepContext.Result;


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/token/usuario/{token}/{citaVDetalles.idUsuario}"),
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
            var citaVDetalles = (CitaVDetalles)stepContext.Options;




            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/usuariomascota/{citaVDetalles.idUsuario}"),
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
                    Value = JsonSerializer.Serialize<Mascota>(mascotas[i]),
                    Action = cardAction
                });
                listastringmascotas = listastringmascotas + " " + mascotas[i].nombre;
            }


            var Texto = $"Tienes estas mascotas registradas, seleccione una mascota a tratar";


            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = listaOpciones, Style = ListStyle.HeroCard }, cancellationToken);

        }


        private async Task<DialogTurnResult> DetallesMascotaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;


            citaVDetalles.mascota = JsonSerializer.Deserialize<Mascota>(((FoundChoice)stepContext.Result).Value);



            HttpRequestMessage msg = new HttpRequestMessage
            {
                //https://rimberiobackmejorado-production.up.railway.app/rimbeiro/especialidades
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http{_config.GetValue<string>("backendUrl")}/rimbeiro/reservasMascota/{citaVDetalles.mascota.id}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Reservacion> reservas = JsonSerializer.Deserialize<List<Reservacion>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            //var attachments = new List<Attachment>();
            for (int i = 0; i < reservas.Count; i++)
            {
                var estado = reservas[i].estado ? "Activo" : "Inactivo";

                /*attachments.Add(
                    new HeroCard()
                    {
                        Text = $@"Nombre : {reservas[i].mascota.nombre} {Environment.NewLine} La fecha es :  {reservas[i].fecha}{Environment.NewLine}   La hora es :  {reservas[i].hora} {Environment.NewLine} Disponibilidad : {estado}",
                        Title = $@"Reserva N° {i + 1}",
                        Subtitle = $@"Veterinario {reservas[i].veterinario.nombres}"
                        /*Images = new CardImage[] {
                            new CardImage(url: $"https://i.postimg.cc/HnRypyMX/Mesa-de-trabajo-1.png"),
                        }
                    }.ToAttachment()
                );*/
                await stepContext.Context.SendActivityAsync($@"Nombre : {reservas[i].mascota.nombre} {Environment.NewLine} La fecha es :  {reservas[i].fecha}{Environment.NewLine}   La hora es :  {reservas[i].hora} {Environment.NewLine} Disponibilidad : {estado}", cancellationToken: cancellationToken);
            }


            //var reply = MessageFactory.Carousel(attachments);




            return await stepContext.NextAsync(stepContext, cancellationToken: cancellationToken);
        }



        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;


            return await stepContext.EndDialogAsync(citaVDetalles, cancellationToken);
        }






        private async Task<DialogTurnResult> GetEmailTemporalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;


            var Texto = $"Inserte el email temporal";

            var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> SetEmailTemporalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;
            citaVDetalles.temporalEmail = (string)stepContext.Result;


            return await stepContext.ReplaceDialogAsync("flow3", stepContext.Options, cancellationToken: cancellationToken);

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
