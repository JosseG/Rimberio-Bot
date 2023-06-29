using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Threading.Tasks;
using System.Threading;
using System;
using MyBotConversational.CognitiveModels;
using System.Diagnostics;

namespace MyBotConversational.Dialog
{
    public class MainDialog : ComponentDialog
    {
        private readonly CitaVetRecognizer _cluRecognizer;
        protected readonly ILogger Logger;


        public MainDialog(CitaVetRecognizer cluRecognizer, RegistroCitaDialog registroCitaDialog,CancelacionCitaDialog cancelacionCitaDialog,VistaCitaDialog vistaCitaDialog,GptDialog gptDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _cluRecognizer = cluRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(registroCitaDialog);
            AddDialog(cancelacionCitaDialog);
            AddDialog(vistaCitaDialog);
            AddDialog(gptDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_cluRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: CLU is not configured. To enable all capabilities, add 'CluProjectName', 'CluDeploymentName', 'CluAPIKey' and 'CluAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }


            var weekLaterDate = DateTime.Now.AddDays(7).ToString("MMMM d, yyyy");
            var messageText = stepContext.Options?.ToString() ?? $"¿En qué te puedo ayudar?\n Tenemos los servicios de registro de cita y consulta general a través del modelo de texto GPT";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_cluRecognizer.IsConfigured)
            { 
                return await stepContext.BeginDialogAsync(nameof( RegistroCitaDialog), new CitaRDetalles(), cancellationToken);
            }


            var cluResult = await _cluRecognizer.RecognizeAsync<CitaVeterinaria>(stepContext.Context, cancellationToken);
            switch (cluResult.GetTopIntent().intent)
            {
                case CitaVeterinaria.Intent.registrarCita:

                    var citaRDetalles = new CitaRDetalles()
                    {

                    };

                    return await stepContext.BeginDialogAsync(nameof(RegistroCitaDialog), citaRDetalles, cancellationToken);

                case CitaVeterinaria.Intent.verCita:

                    var verCitaDetalles = new CitaVDetalles()
                    {
                        idcita = cluResult.Entities.GetIdCita(),
                    };

                    return await stepContext.BeginDialogAsync(nameof(VistaCitaDialog), verCitaDetalles, cancellationToken);

                case CitaVeterinaria.Intent.cancelarCita:

                    var cancelarCitaDetalles = new CitaCDetalles()
                    {
                        idcita = cluResult.Entities.GetIdCita(),
                    };

                    return await stepContext.BeginDialogAsync(nameof(CancelacionCitaDialog), cancelarCitaDetalles, cancellationToken);

                case CitaVeterinaria.Intent.Salir:

                    var setOutMessage = "Saliendo del proceso";
                    var getWeatherMessage = MessageFactory.Text(setOutMessage, setOutMessage, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                    break;

                default:

                    string objeto = (string)stepContext.Result;
                    return await stepContext.BeginDialogAsync(nameof(GptDialog), objeto);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptMessage = "¿Qué mas quieres que haga por ti?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        
        }

}
}
