using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace FinalTaskFlights
{
    /// <summary>
    /// Chatbot accessor class
    /// </summary>
    public class ChatbotAccessor
    {
        //Name of the Chatbot accessor
        public const string ChatbotAccessorName = nameof(ChatbotAccessor);
        //Name of accessor which stores flight reservation details
        public const string FlightReservationAccessorName = "FlightInfo";
        //Name of accessor which stores all the reservations
        public const string ReservatonStorageAccessorName = "ReservationStorage";
        //Name of accessor which store car rental details
        public const string CarRentalAccessorName = "RentalInfo";

        //Conversation state handler
        public ConversationState ConversationState;
        //Coverstation state is of type DialogState.
        public IStatePropertyAccessor<DialogState> ConversationDialogStateAccessor;
        public IStatePropertyAccessor<FlightInfo> FlightInfoAccessor { get; set; }
        public IStatePropertyAccessor<ReservationStorage> ReservationStorageAccessor { get; set; }
        public IStatePropertyAccessor<RentalInfo> RentalInfoAccessor { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatbotAccessor"/> class.
        /// </summary>
        /// <param name="conversationState">The state object that stores the dialog state.</param>
        public ChatbotAccessor(ConversationState conversationState)
        {
            this.ConversationState = conversationState;
        }
    }
}
