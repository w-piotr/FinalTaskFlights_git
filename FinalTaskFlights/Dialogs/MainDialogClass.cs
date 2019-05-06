using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace FinalTaskFlights
{
    /// <summary>
    /// Root dialog class.
    /// </summary>
    public class MainDialogClass : ComponentDialog
    {
        private const string MainDialogId = "MainDialog";
        private const string GetFlightDetailsDialogId = "GetFlightDetailsDialog";
        private const string ShowOneReservationId = "ShowOneReservationDialog";
        private const string ShowAllReservationsId = "ShowAllReservationsDialog";
        private readonly ChatbotAccessor _accessor;

        /// <summary>
        /// MainDialogClass constructor.
        /// </summary>
        /// <param name="dialogId">Dialog Id parameter inherited from CompenentDialog Class.</param>
        /// <param name="accessor">Chatbot accessors.</param>
        public MainDialogClass(string dialogId, ChatbotAccessor accessor) : base(dialogId)
        {
            InitialDialogId = MainDialogId;
            this._accessor = accessor ?? throw new System.ArgumentException("Accessor object is empty!");

            //Definition of waterfall steps to be executed in this class as a waterfall dialog.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
                GetOperationTypeAsync,
                VerifyOperationTypeAsync,
                RedirectMainDialogAsync,
            };

            //Definition of dialogs which are executed in GetFlightDetailsDialog class.
            AddDialog(new WaterfallDialog(MainDialogId, waterfallSteps));
            AddDialog(new GetFlightDetailsDialog(GetFlightDetailsDialogId, _accessor));
            AddDialog(new ShowOneReservationDialog(ShowOneReservationId, _accessor));
            AddDialog(new ShowAllReservationsDialog(ShowAllReservationsId, _accessor));
            //Definition of prompts which are used in the GetFlightDetailsDialog class.
            AddDialog(new ChoicePrompt("OperationType"));
        }

        /// <summary>
        /// Method to execute the task which is to display main manu to the user.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>OperationType TextPrompt</returns>
        private async Task<DialogTurnResult> GetOperationTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("OperationType", new PromptOptions
            {

                Prompt = MessageFactory.Text("Please choose one of the options:"),
                RetryPrompt = MessageFactory.Text("I don't recognize this option. Try again."),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Buy flight ticket", "Show single reservation", "Show all reservations", "Cancel the reservation", "Finish conversation" })
            },
                cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to verify the option chosen by user in previos step and start the proper subdialog.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Begin Dialog Async (ShowOneReservationId, ShowAllReservationsId, GetFlightDetailsDialogId) or End Dialog Async</returns>
        private async Task<DialogTurnResult> VerifyOperationTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FoundChoice foundChoice = stepContext.Result as FoundChoice;

            //This block verifies the response from previos steps and returns suitable action. 
            if (foundChoice.Value.Equals("Show single reservation", StringComparison.InvariantCultureIgnoreCase))
            {
                return await stepContext.BeginDialogAsync(ShowOneReservationId, null, cancellationToken);
            }
            else if (foundChoice.Value.Equals("Show all reservations", StringComparison.InvariantCultureIgnoreCase))
            {
                return await stepContext.BeginDialogAsync(ShowAllReservationsId, null, cancellationToken);
            }
            else if (foundChoice.Value.Equals("Cancel the reservation", StringComparison.InvariantCultureIgnoreCase))
            {
                return await stepContext.BeginDialogAsync(ShowOneReservationId, null, cancellationToken);
            }
            else if (foundChoice.Value.Equals("Finish conversation", StringComparison.InvariantCultureIgnoreCase))
            {
                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.BeginDialogAsync(GetFlightDetailsDialogId, null, cancellationToken);
            }
        }

        /// <summary>
        /// Method to execute the task which is to display main menu after other subdialogs are finished.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Replace Dialog Async with MainDialog</returns>
        private async Task<DialogTurnResult> RedirectMainDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.ReplaceDialogAsync(MainDialogId, null, cancellationToken);
        }
    }
}
