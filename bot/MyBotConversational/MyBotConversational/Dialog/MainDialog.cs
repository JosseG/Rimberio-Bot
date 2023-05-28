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

        // Dependency injection uses this constructor to instantiate MainDialog
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

            // The initial child Dialog to run.
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

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var weekLaterDate = DateTime.Now.AddDays(7).ToString("MMMM d, yyyy");
            var messageText = stepContext.Options?.ToString() ?? $"¿En que te puedo ayudar?\n Tenemos los servicios de registro de cita y consulta general a través del modelo de texto GPT";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_cluRecognizer.IsConfigured)
            {
                // CLU is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof( RegistroCitaDialog), new CitaRDetalles(), cancellationToken);
            }



            // Call CLU and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var cluResult = await _cluRecognizer.RecognizeAsync<CitaVeterinaria>(stepContext.Context, cancellationToken);
            Debug.WriteLine("SECOND RESULT |||||||||||| ");
            switch (cluResult.GetTopIntent().intent)
            {
                case CitaVeterinaria.Intent.registrarCita:
                    // Initialize BookingDetails with any entities we may have found in the response.
                    var citaRDetalles = new CitaRDetalles()
                    {

                    };

                    // Run the BookingDialog giving it whatever details we have from the CLU call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(RegistroCitaDialog), citaRDetalles, cancellationToken);

                case CitaVeterinaria.Intent.verCita:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var verCitaDetalles = new CitaVDetalles()
                    {
                        idcita = cluResult.Entities.GetIdCita(),
                    };

                    // Run the BookingDialog giving it whatever details we have from the CLU call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(VistaCitaDialog), verCitaDetalles, cancellationToken);

                case CitaVeterinaria.Intent.cancelarCita:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var cancelarCitaDetalles = new CitaCDetalles()
                    {
                        idcita = cluResult.Entities.GetIdCita(),
                    };

                    // Run the BookingDialog giving it whatever details we have from the CLU call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(CancelacionCitaDialog), cancelarCitaDetalles, cancellationToken);

                case CitaVeterinaria.Intent.Salir:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var setOutMessage = "Saliendo del proceso";
                    var getWeatherMessage = MessageFactory.Text(setOutMessage, setOutMessage, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                    break;
                default:
                    // Catch all for unhandled intents
                    string objeto = (string)stepContext.Result;

                    return await stepContext.BeginDialogAsync(nameof(GptDialog), objeto);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            if (stepContext.Result is CitaRDetalles result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

               /* var timeProperty = new TimexProperty(result.TravelDate);
                var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);*/
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "¿Qué mas quieres que haga por ti?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        
    }

}
}
