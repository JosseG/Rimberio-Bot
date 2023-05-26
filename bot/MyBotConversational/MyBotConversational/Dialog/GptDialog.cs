using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyBotConversational.Dialog
{
    public class GptDialog : CancelAndHelpDialog
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string GptApiUrl = "https://api.openai.com/v1/chat/completions";  // URL de la API de GPT
        private const string GptApiToken = "YOUR_API_TOKEN";  // Token de autenticación de la API de GPT
        private readonly IConfiguration _config;

        public GptDialog(IConfiguration configuration)
            : base(nameof(GptDialog))
        {

            _config = configuration;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            /*String resultado = await responseGpt();*/
            

            string message = (string)stepContext.Options;

            string mirespuesta = await responseGpt(message);

            var promptMessage = MessageFactory.Text(mirespuesta, mirespuesta, InputHints.IgnoringInput);
            await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage });

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


        private async Task<String> responseGpt(string input)
        {
            var apitoken = _config["OpenAIKey"];

            // Agregar el token de autenticación a la cabecera de la solicitud

            var parameters = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = input }, },
                max_tokens = 1024,
                temperature = 0.2f,
            };
            var content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");
            /*content.Headers.Add("Authorization", $"Bearer {apitoken}");*/
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {apitoken}");
            // Send the HTTP request
            var response = await _httpClient.PostAsync(GptApiUrl, content);// new StringContent(json));

            // Read the response
            var responseContent = await response.Content.ReadAsStringAsync();

            // Extract the completed text from the response
            dynamic responseObject = JsonConvert.DeserializeObject(responseContent);
            string generatedText = responseObject.choices[0].message.content;


            return generatedText;
        }
    }
}
