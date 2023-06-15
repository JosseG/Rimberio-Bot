using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;
using MyBotConversational.ModelsApi;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System;
using System.Net.Mime;
using System.Text;

namespace MyBotConversational.Dialog
{
    public class VistaCitaDialog : CancelAndHelpDialog
    {
        private readonly HttpClient _httpClient;

        private const string IntroStepMsgText = "Escogio la intención de ver cita";
        private const string CodigoUsuarioStepMsgText = "¿Cual es su código de usuario? Es necesario para consultar sus reservas pendientes";
        private const string TokenStepMsgText = "Fue enviado un token a su bandeja de correo, inserte su token en el siguiente mensaje";

        public VistaCitaDialog(HttpClient httpClient)
            : base(nameof(VistaCitaDialog))
        {
            this._httpClient = httpClient;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                IdUsuarioStepAsync,
                VerifyIdUsuarioStepAsync,
                TokenStepAsync,
                VerifyTokenStepAsync,
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
            Debug.WriteLine("2---------------------------------------");
            var promptMessage = MessageFactory.Text(CodigoUsuarioStepMsgText, CodigoUsuarioStepMsgText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> VerifyIdUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;
            citaRDetalles.idUsuario = long.Parse((string)stepContext.Result);


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"https://temporalbackendveterinaria-production.up.railway.app/api/v1/reservaciones/usuario/{citaRDetalles.idUsuario}"),
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
                RequestUri = new Uri($@"https://temporalbackendveterinaria-production.up.railway.app/api/v1/reservaciones/usuario/token/generar/{citaRDetalles.idUsuario}"),
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

            var isVerifyToken = JsonSerializer.Deserialize<object>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            Debug.WriteLine("Está verificado ? " + isVerifyToken + " con tipo de dato ");

            var boolResutlToken = Convert.ToBoolean(isVerifyToken.ToString());


            if (!boolResutlToken)
            {
                Debug.WriteLine("2---------------------------------------");
                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 5;

            }

            return await stepContext.NextAsync(citaRDetalles.idUsuario, cancellationToken);

        }



        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var citaVDetalles = (CitaVDetalles)stepContext.Options;

                return await stepContext.EndDialogAsync(citaVDetalles, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
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
