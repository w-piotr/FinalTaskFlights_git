using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace FinalTaskFlights
{
    /// <summary>
    /// Subdialog class which displays all the flight reservations details.
    /// </summary>
    public class ShowOneReservationDialog : ComponentDialog
    {
        private const string ShowOneReservationId = "ShowOneReservationDialog";
        private readonly ChatbotAccessor _accessor;

        /// <summary>
        /// ShowOneReservationDialog constructor.
        /// </summary>
        /// <param name="dialogId">Dialog Id parameter inherited from CompenentDialog Class.</param>
        /// <param name="accessor">Chatbot accessor.</param>
        public ShowOneReservationDialog(string dialogId, ChatbotAccessor accessor) : base(dialogId)
        {
            this.InitialDialogId = ShowOneReservationId;
            this._accessor = accessor ?? throw new System.ArgumentException("Accessor object is empty!");

            //Definition of waterfall steps to be executed in this class as a waterall dialog.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
                AskForReservationIdAsync,
                DisplayReservationAsync,
                AskForNextActionAsync,
                EndShowOneReservationAsync
            };

            //Definition of dialog which is executed in GetFlightDetailsDialog class.
            AddDialog(new WaterfallDialog(ShowOneReservationId, waterfallSteps));
            //Definition of prompts which are used in the GetFlightDetailsDialog class.
            AddDialog(new TextPrompt("ReservationId", ReservationIdValidatorAsync));
            AddDialog(new ChoicePrompt("OperationType"));
        }

        /// <summary>
        /// Method to execute the task which is to collect ID of the reservation to be displayed.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ReservationId TextPrompt</returns>
        private async Task<DialogTurnResult> AskForReservationIdAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            ReservationStorage rstorage = await _accessor.ReservationStorageAccessor.GetAsync(stepContext.Context, () => new ReservationStorage());

            //This block decides if rstorage is not empty and asks for reservation ID.
            if (rstorage.Reservations != null)
            {
                if (rstorage.Reservations.Count > 0)
                {
                    return await stepContext.PromptAsync("ReservationId", new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Please enter reservation ID.")
                    },
                    cancellationToken);
                }
            }
            //Inform user that there is not any reservation to dispaly and end subdialog.
            await stepContext.Context.SendActivityAsync("There is no reservation to display.");
            return await stepContext.EndDialogAsync();

        }

        /// <summary>
        /// Method to execute the task which is to create adaptive card attachment of the reservation indicated by the user and display it.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>NextAsync</returns>
        private async Task<DialogTurnResult> DisplayReservationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //Get ReservationStorage object.
            ReservationStorage rstorage = await _accessor.ReservationStorageAccessor.GetAsync(stepContext.Context, () => new ReservationStorage());
            //Initialize reservation list and assign list of reservations stored in rstorage.
            IList<FlightInfo> reservations = rstorage.Reservations;
            //Get flight ID provided by the user and convert it to integer.
            int flightId = int.Parse(stepContext.Result as string);
            //Initialize reservationInfo variable.
            FlightInfo reservationInfo = new FlightInfo();

            //This block tries to find suitable reservation ID in the list of reservations using Linq expression, if there is not any suitable reservation ID it caches the exception and assigns reservationInfo variable as null.
            try
            {
                var result = from r in reservations
                             where r.ReservationId == flightId
                             select r;
                reservationInfo = result.First();
            }
            catch (Exception e) when (e.Message == "Sequence contains no elements")
            {
                reservationInfo = null;
            };
            //This block decides if reservationInfo is not empty and there is any reservation to display. 
            if (reservationInfo != null)
            {
                Attachment attachment = new Attachment();

                // This block decides which adaptive card template should be used based on reservation details.
                if (reservationInfo.OneWayFlight && reservationInfo.Rental == null)
                {
                    attachment = CreateReservationAttachement(@".\Resources\FlightDetailsOneWay.txt", reservationInfo.PassengerName, reservationInfo.FromAirport, reservationInfo.ToAirport, reservationInfo.StartDate, reservationInfo.TripClass, (reservationInfo.FlightCost.ToString() + " $"), reservationInfo.ReservationId.ToString());
                }
                else if (!reservationInfo.OneWayFlight && reservationInfo.Rental == null)
                {
                    attachment = CreateReservationAttachement(@".\Resources\FlightDetailsTwoWays.txt", reservationInfo.PassengerName, reservationInfo.FromAirport, reservationInfo.ToAirport, reservationInfo.StartDate, reservationInfo.TripClass, (reservationInfo.FlightCost.ToString() + " $"), reservationInfo.ReservationId.ToString(), reservationInfo.EndDate);
                }
                else if (reservationInfo.OneWayFlight && reservationInfo.Rental != null)
                {
                    attachment = CreateCombinedAttachement(@".\Resources\FlightDetailsOneWayRental.txt", reservationInfo.PassengerName, reservationInfo.FromAirport, reservationInfo.ToAirport, reservationInfo.StartDate, reservationInfo.TripClass, (reservationInfo.FlightCost.ToString() + " $"), reservationInfo.ReservationId.ToString(), reservationInfo.Rental.RentalLength, reservationInfo.Rental.PassengersNumber, reservationInfo.Rental.ChildSeats, reservationInfo.Rental.CarClass, reservationInfo.Rental.RentalCost);
                }
                else if (!reservationInfo.OneWayFlight && reservationInfo.Rental != null)
                {
                    attachment = CreateCombinedAttachement(@".\Resources\FlightDetailsTwoWaysRental.txt", reservationInfo.PassengerName, reservationInfo.FromAirport, reservationInfo.ToAirport, reservationInfo.StartDate, reservationInfo.TripClass, (reservationInfo.FlightCost.ToString() + " $"), reservationInfo.ReservationId.ToString(), reservationInfo.Rental.RentalLength, reservationInfo.Rental.PassengersNumber, reservationInfo.Rental.ChildSeats, reservationInfo.Rental.CarClass, reservationInfo.Rental.RentalCost, reservationInfo.EndDate);
                };
                //Initialize reply Activity.
                Activity reply = stepContext.Context.Activity.CreateReply();
                //Add created attachment to the lift of attachments.
                reply.Attachments = new List<Attachment>() { attachment };
                await stepContext.Context.SendActivityAsync(reply, cancellationToken: cancellationToken);
                //Add flight id provided by user to the stepContext.Values distionary.
                stepContext.Values["RequestID"] = flightId;
            }
            //Inform user that there is not any reservation to dispaly.
            else
            {
                await stepContext.Context.SendActivityAsync("There is not any reservation matching provided ID.");
                //Add default value of request id = 0 to the stepContext.Values distionary.
                stepContext.Values["RequestID"] = 0;
            }

            return await stepContext.NextAsync();

        }

        /// <summary>
        /// Method to execute the task which is to ask if user wants to: cancel reservation, display other reservation or return to main menu.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>OperationType ChoicePrompt</returns>
        private async Task<DialogTurnResult> AskForNextActionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            int flightId = int.Parse(stepContext.Values["RequestID"].ToString());
            //Initialize availableChoices list of strings.
            List<string> awailableChoices = null;
            //This block checks if there was any reservation displayed and create list of available choice prompts to the user.
            if (flightId == 0)
            {
                awailableChoices = new List<string>() { "Dispaly other reservation", "Return to main menu" };
            }
            else
            {
                awailableChoices = new List<string>() { $"Cancel reservation {flightId}", "Dispaly other reservation", "Return to main menu" };
            }
            return await stepContext.PromptAsync("OperationType", new PromptOptions
            {

                Prompt = MessageFactory.Text("What would you like to do next:"),
                RetryPrompt = MessageFactory.Text("I don't recognize this option. Try again."),
                Choices = ChoiceFactory.ToChoices(awailableChoices)
            },
            cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to confirm cancellation action or end dialog.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>End dialog async</returns>
        private async Task<DialogTurnResult> EndShowOneReservationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FoundChoice foundChoice = stepContext.Result as FoundChoice;
            //This block decides if dialog should start over, reservation should be cancelled or end.
            if (foundChoice.Value.Equals("Dispaly other reservation", StringComparison.InvariantCultureIgnoreCase))
            {
                return await stepContext.ReplaceDialogAsync(ShowOneReservationId, null, cancellationToken);
            }
            else if (foundChoice.Value.Contains("Cancel reservation"))
            {
                int flightId = int.Parse(stepContext.Values["RequestID"].ToString());
                //Get ReservationStorage object.
                ReservationStorage rstorage = await _accessor.ReservationStorageAccessor.GetAsync(stepContext.Context, () => new ReservationStorage());
                //Initialize reservation list and assign list of reservations stored in rstorage.
                IList<FlightInfo> reservations = rstorage.Reservations;
                //Initialize reservationInfo variable.
                FlightInfo reservationInfo = new FlightInfo();
                //This block searches suitable reservation ID in the list of reservations using Linq expression.
                var result = from r in reservations
                             where r.ReservationId == flightId
                             select r;
                reservationInfo = result.First();
                //Remove reservation from ResercationStorage object.
                rstorage.Reservations.Remove(reservationInfo);
                await _accessor.ReservationStorageAccessor.SetAsync(stepContext.Context, rstorage);
                //Inform user that reservation has been cancelled.
                await stepContext.Context.SendActivityAsync("Your reservaton has been successfully cancelled.");
                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.EndDialogAsync();
            }
        }



        /// <summary>
        /// Method to execute the taks which is to validate the format of reservation id.
        /// </summary>
        /// <param name="promptValidatorContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Boolean true if validation passed, false if validation fails.</returns>
        public async Task<bool> ReservationIdValidatorAsync(PromptValidatorContext<string> promptValidatorContext, CancellationToken cancellationToken)
        {
            string temp_reservationId = promptValidatorContext.Recognized.Value;
            if (!int.TryParse(temp_reservationId, out int reservationId) || reservationId <= 1000000 || reservationId >= 9999999)
            {
                await promptValidatorContext.Context.SendActivityAsync("Please type in correct reservation ID.", cancellationToken: cancellationToken);
                return false;
            }
            else
            {
                return true;
            }
        }


        /// <summary>
        /// Method to execute the task which is to create adaptive card attachement to display the flight reservation details.
        /// </summary>
        /// <param name="filePath">Path to the file with adaptive card txt file Json formatted.</param>
        /// <param name="PassengerName">Name of passenger to be displayed on the card.</param>
        /// <param name="DepartureAirport">Starting point airport to be displayed on the card.</param>
        /// <param name="ArrivalAirport">Ending point airport to be displayed on the card.</param>
        /// <param name="DepartureDate">Starting point departure date to be displayed on the card.</param>
        /// <param name="FlightClass">Flight class to be displayed on the card.</param>
        /// <param name="FlightCost">Flight cost to be displayed on the card.</param>
        /// <param name="ReservationNumber">Number of the reservation to be displayed on the card.</param>
        /// <param name="ReturnDate">Ending point departure date to be displayed on the card.</param>
        /// <returns>Adaptive Card attachment object.</returns>
        private static Attachment CreateReservationAttachement(string filePath, string PassengerName, string DepartureAirport, string ArrivalAirport, string DepartureDate, string FlightClass, string FlightCost, string ReservationNumber, string ReturnDate = null)
        {
            // Read adaptive card template.
            string cardJson = File.ReadAllText(filePath);
            //Replace the generic labels with flight details.
            cardJson = cardJson.Replace("<Header>", $"Reservation {ReservationNumber}")
                .Replace("<PassengerName>", PassengerName)
                .Replace("<DepartureAirport>", DepartureAirport)
                .Replace("<ArrivalAirport>", ArrivalAirport)
                .Replace("<DepartureDate>", DepartureDate)
                .Replace("<FlightClass>", FlightClass)
                .Replace("<FlightCost>", FlightCost);
            //Replace the <ReturnDate> if two ways flight.
            if (!(ReturnDate == null))
            {
                cardJson = cardJson.Replace("<ReturnDate>", ReturnDate);
            };
            Attachment adaptiveCardAttachement = new Attachment()
            {
                ContentType = @"application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };
            return adaptiveCardAttachement;
        }

        /// <summary>
        /// Method to execute the task which is to create adaptive card attachement to display the flight and car rental reservation details.
        /// </summary>
        /// <param name="filePath">Path to the file with adaptive card txt file Json formatted.</param>
        /// <param name="PassengerName">Name of passenger to be displayed on the card.</param>
        /// <param name="DepartureAirport">Starting point airport to be displayed on the card.</param>
        /// <param name="ArrivalAirport">Ending point airport to be displayed on the card.</param>
        /// <param name="DepartureDate">Starting point departure date to be displayed on the card.</param>
        /// <param name="FlightClass">Flight class to be displayed on the card.</param>
        /// <param name="FlightCost">Flight cost to be displayed on the card.</param>
        /// <param name="ReservationNumber">Number of the reservation to be displayed on the card.</param>
        /// <param name="RentalLength">Number of days passenger rents a car for.</param>
        /// <param name="PassengersNumber">Number of people travelling with passenger by car.</param>
        /// <param name="ChildSeats">Number of child seats passenger would need.</param>
        /// <param name="CarClass">Class of the car.</param>
        /// <param name="ReturnDate">Ending point departure date to be displayed on the card.</param>
        /// <returns>Adaptive Card attachment object.</returns>
        private static Attachment CreateCombinedAttachement(string filePath, string PassengerName, string DepartureAirport, string ArrivalAirport, string DepartureDate, string FlightClass, string FlightCost, string ReservationNumber, int RentalLength, int PassengersNumber, int ChildSeats, string CarClass, int RentalCost, string ReturnDate = null)
        {
            // Read adaptive card template.
            string cardJson = File.ReadAllText(filePath);
            //Replace the generic labels with flight details.
            cardJson = cardJson.Replace("<Header>", $"Reservation {ReservationNumber}")
                .Replace("<PassengerName>", PassengerName)
                .Replace("<DepartureAirport>", DepartureAirport)
                .Replace("<ArrivalAirport>", ArrivalAirport)
                .Replace("<DepartureDate>", DepartureDate)
                .Replace("<FlightClass>", FlightClass)
                .Replace("<FlightCost>", FlightCost)
                .Replace("<RentalHeader>", "Car Rental details ")
                .Replace("<RentalLength>", RentalLength.ToString())
                .Replace("<PassengersNumber>", PassengersNumber.ToString())
                .Replace("<ChildSeats>", ChildSeats.ToString())
                .Replace("<CarClass>", CarClass)
                .Replace("<RentalCost>", RentalCost.ToString() + " $");
            //Replace the <ReturnDate> if two ways flight.
            if (!(ReturnDate == null))
            {
                cardJson = cardJson.Replace("<ReturnDate>", ReturnDate);
            };
            Attachment adaptiveCardAttachement = new Attachment()
            {
                ContentType = @"application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };
            return adaptiveCardAttachement;
        }
    }
}

