using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;

namespace MyBotConversational.Dialog
{
    public class VistaCitaDialog : CancelAndHelpDialog
    {

        private const string IdStepMsgText = "¿Cual es el id de la reserva o cita para su consulta?";

        public VistaCitaDialog()
            : base(nameof(VistaCitaDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NombreStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> NombreStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;

            if (citaVDetalles.idcita == null)
            {
                var promptMessage = MessageFactory.Text(IdStepMsgText, IdStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaVDetalles.idcita, cancellationToken);
        }


        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaVDetalles = (CitaVDetalles)stepContext.Options;

            citaVDetalles.idcita = (string)stepContext.Result;

            var messageText = $"La cita a consultar tiene el siguiente ID ->  {citaVDetalles.idcita}. ¿Es correcto?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
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

    }
}
