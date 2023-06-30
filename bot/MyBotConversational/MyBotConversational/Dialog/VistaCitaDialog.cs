using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MyBotConversational.ModelsApi;
using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyBotConversational.Dialog
{
    public class VistaCitaDialog : CancelAndHelpDialog
    {
        private readonly HttpClient _httpClient;

        private const string IntroStepMsgText = "Escogio la intención de ver cita";
        private const string NombreUsuarioStepMsgText = "¿Cual es su nombre de usuario? Es necesario para consultar sus reservas pendientes";
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
                UsernameUsuarioStepAsync,
                VerifyUsernameUsuarioStepAsync,
                TokenStepAsync,
                VerifyTokenStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var message = MessageFactory.Text(IntroStepMsgText, IntroStepMsgText, InputHints.ExpectingInput);
            await stepContext.Context.SendActivityAsync(message, cancellationToken);

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> UsernameUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;

            var promptMessage = MessageFactory.Text(NombreUsuarioStepMsgText, NombreUsuarioStepMsgText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> VerifyUsernameUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;
            citaVDetalles.username = (string)stepContext.Result;


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"https://rimberioback-production.up.railway.app/rimbeiro/usuario/{citaVDetalles.username}"),
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
                citaVDetalles.idUsuario = usuario.id;
                citaVDetalles.username = usuario.username;
            }

            return await stepContext.NextAsync(citaVDetalles.username, cancellationToken);
        }

        private async Task<DialogTurnResult> TokenStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;
            citaVDetalles.username = (string)stepContext.Result;

            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"https://rimberioback-production.up.railway.app/rimbeiro/token/usuario/generar/{citaVDetalles.username}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            var tokenresult = JsonSerializer.Deserialize<TokenBot>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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
                RequestUri = new Uri($@"https://rimberioback-production.up.railway.app/rimbeiro/token/usuario/{token}/{citaVDetalles.idUsuario}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            var isVerifyToken = JsonSerializer.Deserialize<object>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            var boolResutlToken = Convert.ToBoolean(isVerifyToken.ToString());

            if (!boolResutlToken)
            {
                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 5;
            }

            return await stepContext.NextAsync(citaVDetalles.idUsuario, cancellationToken);

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