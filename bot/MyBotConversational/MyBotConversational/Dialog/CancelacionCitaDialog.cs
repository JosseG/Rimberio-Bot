using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;

namespace MyBotConversational.Dialog
{
    public class CancelacionCitaDialog : CancelAndHelpDialog
    {
        private const string IntroStepMsgText = "Escogio la intención de cancelar cita";
        private const string IdStepMsgText = "¿Cual es el id de la reserva o cita para cancelarlo?";

        public CancelacionCitaDialog()
            : base(nameof(CancelacionCitaDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                IdStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);

        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var message = MessageFactory.Text(IntroStepMsgText, IntroStepMsgText, InputHints.ExpectingInput);

            await stepContext.Context.SendActivityAsync(message, cancellationToken);
        
            return await stepContext.NextAsync(cancellationToken:cancellationToken);
        }

        private async Task<DialogTurnResult> IdStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaCDetalles = (CitaCDetalles)stepContext.Options;

            if (citaCDetalles.idcita == null)
            {
                var promptMessage = MessageFactory.Text(IdStepMsgText, IdStepMsgText, InputHints.ExpectingInput);

                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaCDetalles.idcita, cancellationToken);
        }


        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaCDetalles = (CitaCDetalles)stepContext.Options;

            citaCDetalles.idcita = (string)stepContext.Result;

            var messageText = $"La cita a cancelar tiene el siguiente ID ->  {citaCDetalles.idcita}. ¿Es correcto?";

            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var citaCDetalles = (CitaCDetalles)stepContext.Options;

                return await stepContext.EndDialogAsync(citaCDetalles, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

    }
}

