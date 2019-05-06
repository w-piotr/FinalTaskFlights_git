using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace FinalTaskFlights
{
    /// <summary>
    /// Subdialog class which displays all the flight reservations details.
    /// </summary>
    public class ShowAllReservationsDialog : ComponentDialog
    {
        private const string ShowAllReservationsId = "ShowAllReservationsDialog";
        private const string ShowOneReservationId = "ShowOneReservationDialog";
        private readonly ChatbotAccessor _accessor;

        /// <summary>
        /// ShowAllReservationsDialog constructor.
        /// </summary>
        /// <param name="dialogId">Dialog Id parameter inherited from CompenentDialog Class.</param>
        /// <param name="accessor">Chatbot accessor.</param>
        public ShowAllReservationsDialog(string dialogId, ChatbotAccessor accessor) : base(dialogId)
        {
            this.InitialDialogId = ShowAllReservationsId;
            this._accessor = accessor ?? throw new System.ArgumentException("Accessor object is empty!");

            //Definition of waterfall step to be executed in this class as a waterall dialog.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
                DisplayReservationsAsync,
            };

            //Definition of dialog which is executed in GetFlightDetailsDialog class.
            AddDialog(new WaterfallDialog(ShowAllReservationsId, waterfallSteps));
        }

        /// <summary>
        /// Method to execute the task which is to create adaptive card attachements for every single reservation and display them.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>EndDialogAsync</returns>
        private async Task<DialogTurnResult> DisplayReservationsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            ReservationStorage rstorage = await _accessor.ReservationStorageAccessor.GetAsync(stepContext.Context, () => new ReservationStorage());

            //This block decides if rstorage is not empty and there is any reservation to display.
            if (rstorage.Reservations != null)
            {
                if (rstorage.Reservations.Count > 0)
                {
                    //Initialize reservation list and assign list of reservations stored in rstorage.
                    IList<FlightInfo> reservations = rstorage.Reservations;
                    //Initialize reply Activity.
                    Activity reply = stepContext.Context.Activity.CreateReply();
                    reply.Attachments = new List<Attachment>();

                    //Create attachement for every element of the reservations list.
                    foreach (FlightInfo reservationInfo in reservations)
                    {
                        Attachment attachment = new Attachment();

                        //This block decides which adaptive card template should be used based on reservation details.
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

                        //Add created attachment to list of attachments.
                        reply.Attachments.Add(attachment);
                    }
                    await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            //Inform user that there is not any reservation to dispaly.
            await stepContext.Context.SendActivityAsync("There is no reservation to display.");
            return await stepContext.EndDialogAsync();
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
            //Read adaptive card template.
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
            //Read adaptive card template.
            string cardJson = File.ReadAllText(filePath);
            //Replace the generic labels with flight and rental details.
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
