using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Threading.Tasks;
using System.Threading;

namespace MyBotConversational.Dialog
{
    public class RegistroCitaDialog : CancelAndHelpDialog
    {
        private const string IntroStepMsgText = "Escogio la intención de registrar cita";
        private const string FechaSinFormatoStepMsgText = "¿Cual es la fecha sin formato?";
        private const string MascotaStepMsgText = "¿Que mascota va a traer?";
        private const string IdCitaStepMsgText = "¿Cual es el tipo de servicio?";
        private const string FechaStepMsgText = "¿Coloqué la fecha con formato";

        public RegistroCitaDialog()
            : base(nameof(RegistroCitaDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                NombreStepAsync,
                AnimalStepAsync,
                TipoCitaStepAsync,
                FechaStepAsync,
                ConfirmStepAsync,
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

        private async Task<DialogTurnResult> NombreStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            if (citaRDetalles.nombre == null)
            {
                var promptMessage = MessageFactory.Text(FechaSinFormatoStepMsgText, FechaSinFormatoStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.nombre, cancellationToken);
        }

        private async Task<DialogTurnResult> AnimalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.nombre = (string)stepContext.Result;

            if (citaRDetalles.animal == null)
            {
                var promptMessage = MessageFactory.Text(MascotaStepMsgText, MascotaStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.animal, cancellationToken);
        }



        private async Task<DialogTurnResult> TipoCitaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.animal = (string)stepContext.Result;

            if (citaRDetalles.tipocita == null)
            {
                var promptMessage = MessageFactory.Text(IdCitaStepMsgText, IdCitaStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.tipocita, cancellationToken);
        }

        private async Task<DialogTurnResult> FechaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.tipocita = (string)stepContext.Result;

            if (citaRDetalles.fecha == null || IsAmbiguous(citaRDetalles.fecha))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), citaRDetalles.fecha, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.fecha, cancellationToken);
        }


        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.fecha = (string)stepContext.Result;

            var messageText = $"Por favor confirma, la fecha sin formato es ->  {citaRDetalles.nombre} , el tipo de servicio es -> {citaRDetalles.tipocita} y la mascota es -> {citaRDetalles.animal}. ¿Es correcto?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var citaRDetalles = (CitaRDetalles)stepContext.Options;

                return await stepContext.EndDialogAsync(citaRDetalles, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }

    }
}
