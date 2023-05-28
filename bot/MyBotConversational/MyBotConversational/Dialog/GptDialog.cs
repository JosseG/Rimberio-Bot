using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyBotConversational.Dialog
{
    public class GptDialog : CancelAndHelpDialog
    {
        private readonly HttpClient _httpClient;
        private const string GptApiUrl = "https://api.openai.com/v1/completions";  // URL de la API de GPT
        private readonly IConfiguration _config;

        public GptDialog(IConfiguration configuration, HttpClient httpClient)
            : base(nameof(GptDialog))
        {
            _httpClient = httpClient;
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

            string message = (string)stepContext.Options;

            string mirespuesta = await responseGpt(message);

            var promptMessage = MessageFactory.Text(mirespuesta, mirespuesta, InputHints.IgnoringInput);
            await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage });

            
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


        private async Task<String> responseGpt(string input)
        {
            var apitoken = _config["OpenAIKey"];
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(GptApiUrl);
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); ;
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apitoken}");
            }


            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post,_httpClient.BaseAddress);
            var parameters = new
            {
                model = "text-davinci-003",
                prompt= input,
                temperature = 0,
                max_tokens = 500
            };
            var serializado = JsonConvert.SerializeObject(parameters).ToString();
            Debug.WriteLine(serializado);
            var content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");
            requestMessage.Content = content; 

            var responsegpt = await _httpClient.SendAsync(requestMessage).ConfigureAwait(true);
                
            string responsetext = await responsegpt.Content.ReadAsStringAsync().ConfigureAwait(true);
            Debug.WriteLine(responsetext);
            dynamic responseObject = JsonConvert.DeserializeObject(responsetext);
            string generatedText = responseObject.choices[0].text;
            return generatedText;

        }
    }
}
