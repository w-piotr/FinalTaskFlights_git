using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace FinalTaskFlights
{
    /// <summary>
    /// Subdialog class which collects the flight details in waterfall dialog.
    /// </summary>
    public class GetFlightDetailsDialog : ComponentDialog
    {
        private const string GetFlightDetailsDialogId = "GetFlightDetailsDialog";
        private const string GetRentalDetailsDialogId = "GetRentalDetailsDialog";
        private readonly ChatbotAccessor _accessor;

        /// <summary>
        /// GetFlightDetailsDialog constructor.
        /// </summary>
        /// <param name="dialogId">Dialog Id parameter inherited from CompenentDialog Class.</param>
        /// <param name="accessor">Chatbot accessors.</param>
        public GetFlightDetailsDialog(string dialogId, ChatbotAccessor accessor): base(dialogId)
        {
            this.InitialDialogId = GetFlightDetailsDialogId;
            this._accessor = accessor ?? throw new System.ArgumentException("Accessor object is empty!");

            //Definition of waterfall steps to be executed in this class as a waterfall dialog.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
                GetPassengerNameAsync,
                GetFromAirportAsync,
                GetToAirportAsync,
                GetOneOrTwoWayFlightAsync,
                GetStartDateAsync,
                GetEndDateAsync,
                ClassChoiceAsync,
                DisplayReservationAsync,
                ConfirmReservationAsync,
                AskForCarRentalAsync,
                EndDialogAsync
            };

            //Definition of dialogs which are executed in GetFlightDetailsDialog class.
            AddDialog(new WaterfallDialog(GetFlightDetailsDialogId, waterfallSteps));
            AddDialog(new GetRentalDetailsDialog(GetRentalDetailsDialogId, _accessor));
            //Definition of prompts which are used in the GetFlightDetailsDialog class.
            AddDialog(new TextPrompt("PassengerName", PassengerNameValidatorAsync));
            AddDialog(new TextPrompt("FromAirport", FromValidatorAsync));
            AddDialog(new TextPrompt("ToAirport", ToValidatorAsync));
            AddDialog(new ChoicePrompt("OneOrTwoWayFlight"));
            AddDialog(new DateTimePrompt("StartDate", StartDateValidatorAsync));
            AddDialog(new DateTimePrompt("EndDate", EndDateValidatorAsync));
            AddDialog(new ChoicePrompt("ClassChoice"));
            AddDialog(new ChoicePrompt("ConfirmChoice"));
            AddDialog(new ChoicePrompt("CarReservationChoice"));

        }

        
        /// <summary>
        /// Method to execute the task which is to collect passenger's name.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>PassengerName TextPrompt</returns>
        private async Task<DialogTurnResult> GetPassengerNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //Create an adaptive card attachement to inform user about reservation process start and dispaly interruption key words with descriptions.
            Attachment attachment = CreateQuickHelpAttachement();
            //Initialize reply Activity.
            Activity reply = stepContext.Context.Activity.CreateReply();
            reply.Attachments = new List<Attachment>() { attachment };
            await stepContext.Context.SendActivityAsync(reply, cancellationToken: cancellationToken);

            return await stepContext.PromptAsync("PassengerName", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter your name."),

            },
            cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to collect starting point airport.
        /// </summary>
        /// <remarks>Method gets FlightInfoAccessor in order to save the previous waterfall step result as passengers name and then sets the FlightInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>FromAirport TextPrompt</returns>
        private async Task<DialogTurnResult> GetFromAirportAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());
            flightInfo.PassengerName = stepContext.Result as string;
            await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);

            return await stepContext.PromptAsync("FromAirport", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter departure airport.")
            },
            cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to collect ending point airport.
        /// </summary>
        /// <remarks>Method gets FlightInfoAccessor in order to save the previous waterfall step result as starting point airport and then sets the FlightInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ToAirport TextPrompt</returns>
        private async Task<DialogTurnResult> GetToAirportAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());
            flightInfo.FromAirport = stepContext.Result as string;
            await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);

            return await stepContext.PromptAsync("ToAirport", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter arrival airport")
            },
            cancellationToken);

        }

        /// <summary>
        /// Method to execute the task which is to collect the information if passenger is going to travel one way or two ways.
        /// </summary>
        /// <remarks>Method gets FlightInfoAccessor in order to save the previous waterfall step result as ending point airport and then sets the FlightInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>OneOrTwoWayFlight ChoicePrompt</returns>
        private async Task<DialogTurnResult> GetOneOrTwoWayFlightAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());
            flightInfo.ToAirport = stepContext.Result as string;
            await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);

            return await stepContext.PromptAsync("OneOrTwoWayFlight", new PromptOptions
            {

                Prompt = MessageFactory.Text("Please choose one of the options:"),
                RetryPrompt = MessageFactory.Text("I don't recognize this option. Try again"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "One way flight", "Two ways flight" })
            },
                cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to collect the date of departure on starting point airport.
        /// </summary>
        /// <remarks>Method gets FlightInfoAccessor in order to save the previous waterfall step result as boolean One Way Flight and then sets the FlightInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>StartDate DateTimePrompt</returns>
        private async Task<DialogTurnResult> GetStartDateAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FoundChoice oneortwowayflightChoice = stepContext.Result as FoundChoice;
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());
            
            if (oneortwowayflightChoice.Value == "One way flight")
            {
                flightInfo.OneWayFlight = true;
            }
            else
            {
                flightInfo.OneWayFlight = false;
            }

            await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);

            return await stepContext.PromptAsync("StartDate", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter departure date.")
            },
            cancellationToken);

        }

        /// <summary>
        /// Method to execute the task which is to collect the date of return from ending point airport.
        /// </summary>
        /// <remarks>Method gets FlightInfoAccessor in order to save the previous waterfall step result as departure date on starting point airport and then sets the FlightInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>EndDate DateTimePrompt</returns>
        private async Task<DialogTurnResult> GetEndDateAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DateTimeResolution resolution = (stepContext.Result as IList<DateTimeResolution>)[0];
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());
            flightInfo.StartDate = resolution.Value;
            await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);

            //This block redirects to the next step if return date is not needed or ask for return date.
            if (flightInfo.OneWayFlight)
            {
                return await stepContext.NextAsync();
            }
            else
            {

                return await stepContext.PromptAsync("EndDate", new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter date of return.")
                },
                cancellationToken);
            }
        }

        /// <summary>
        /// Method to execute the task which is to collect the flight class.
        /// </summary>
        /// <remarks>Method gets FlightInfoAccessor in order to save the previous waterfall step result as departure date on ending point airport and then sets the FlightInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ClassChoice ChoicePrompt</returns>
        private async Task<DialogTurnResult> ClassChoiceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());
            if (flightInfo.OneWayFlight)
            {
                flightInfo.EndDate = null;
                await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);
            }
            else
            {
                DateTimeResolution resolution = (stepContext.Result as IList<DateTimeResolution>)[0];
                flightInfo.EndDate = resolution.Value;
                await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);
            }
            return await stepContext.PromptAsync("ClassChoice", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please choose flight class (to access more details type in 'more flight'):"),
                RetryPrompt = MessageFactory.Text("I don't recognize this option. Try again"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Standard", "Business", "Premium" })
            },
            cancellationToken);

        }

        /// <summary>
        /// Method to execute the task which is to display already gathered information as an adaptive card and ask user to confirm or start over the reservation waterfall dialog.
        /// </summary>
        /// <remarks>Method gets FlightInfoAccessor in order to save the previous waterfall step result as flight class and then sets the FlightInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ConfirmChoice ChoicePrompt</returns>
        private async Task<DialogTurnResult> DisplayReservationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FoundChoice classflightChoice = stepContext.Result as FoundChoice;
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());

            flightInfo.TripClass = classflightChoice.Value;
            //Use GenerateFlightCost method to generate flight cost base on class chosen by user.
            flightInfo.FlightCost = GenerateFlightCost(flightInfo.TripClass);
            await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);

            await stepContext.Context.SendActivityAsync("Thank you, below you can find your reservation.");

            FlightInfo reservationInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());

            //Create reply attachement and based on OneWayFlight use suitable adaptive card schema.
            Attachment attachment = new Attachment();
            if (reservationInfo.OneWayFlight)
            {
                attachment = CreateReservationAttachement(@".\Resources\FlightDetailsOneWay.txt", reservationInfo.PassengerName, reservationInfo.FromAirport, reservationInfo.ToAirport, reservationInfo.StartDate, reservationInfo.TripClass, (reservationInfo.FlightCost.ToString() + " $"));
            }
            else
            {
                attachment = CreateReservationAttachement(@".\Resources\FlightDetailsTwoWays.txt", reservationInfo.PassengerName, reservationInfo.FromAirport, reservationInfo.ToAirport, reservationInfo.StartDate, reservationInfo.TripClass, (reservationInfo.FlightCost.ToString() + " $"), reservationInfo.EndDate);
            };

            //Create reply and send it to the user.
            Activity reply = stepContext.Context.Activity.CreateReply();
            reply.Attachments.Add(attachment);
            await stepContext.Context.SendActivityAsync(reply, cancellationToken: cancellationToken);

            return await stepContext.PromptAsync("ConfirmChoice", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please verify if flight details are correct and choose option:"),
                RetryPrompt = MessageFactory.Text("I don't recognize this option. Try again"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Yes, confirm", "No, start over" })
            },
            cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to generate the reservation id, display it to the user and ask if user want to rent a car.
        /// <remarks>Method gets FlightInfoAccessor in order to save the previous waterfall step result as departure date on ending point airport and then sets the FlightInfoAccessor.</remarks>
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>CarReservationChoice ChoicePrompt</returns>
        private async Task<DialogTurnResult> ConfirmReservationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FoundChoice confirmdetailsChoice = stepContext.Result as FoundChoice;
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());

            //This block asks for car rental or starts over the dialog.
            if (confirmdetailsChoice.Value.Equals("Yes, confirm", StringComparison.InvariantCultureIgnoreCase))
            {
                Random random = new Random();
                flightInfo.ReservationId = random.Next(1000000, 9999999);
                await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);
                await stepContext.Context.SendActivityAsync($"Thank you, please save your reservation id: {flightInfo.ReservationId}.");

                return await stepContext.PromptAsync("CarReservationChoice", new PromptOptions
                {
                    Prompt = MessageFactory.Text("Would you like to rent a car on destination aiport?"),
                    RetryPrompt = MessageFactory.Text("I don't recognize this option. Try again"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Yes, I would like to rent a car.", "No, thank you." })
                },
                cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(GetFlightDetailsDialogId, cancellationToken);
            }
        }

        /// <summary>
        /// Method to execute the task which is to begin GetRentalDetailsDialog dialog if user chose to rent a car in previous step or skip to the next step if user did not choose to rent a car.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<DialogTurnResult> AskForCarRentalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FoundChoice carrentalChoice = stepContext.Result as FoundChoice;

            if (carrentalChoice.Value.Equals("Yes, I would like to rent a car.", StringComparison.InvariantCultureIgnoreCase))
            {
                return await stepContext.BeginDialogAsync(GetRentalDetailsDialogId, null, cancellationToken);
            }
            else
            {
                var flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());
                flightInfo.Rental = null;
                await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);
                return await stepContext.NextAsync();
            }
        }

        /// <summary>
        /// Method to execute the task which is to add the object of FlithInfo class to the object of ReservatonStorage class (list of FlightInfo objects) and end the GetFlightDetailsDialog.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Ends the GetFlightDetailsDialog</returns>
        private async Task<DialogTurnResult> EndDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());

            ReservationStorage rstorage = await _accessor.ReservationStorageAccessor.GetAsync(stepContext.Context, () => new ReservationStorage());
            if (rstorage.Reservations == null)
            {
                List<FlightInfo> newReservation = new List<FlightInfo> { flightInfo };
                rstorage.Reservations = newReservation;
            }
            else
            {
                rstorage.Reservations.Add(flightInfo);
            }
            await _accessor.ReservationStorageAccessor.SetAsync(stepContext.Context, rstorage);
            return await stepContext.EndDialogAsync();
        }

        

        /// <summary>
        /// Method to execute the task which is to validate passenger's name.
        /// </summary>
        /// <param name="promptValidatorContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Boolean true if validation passed, false if validation fails.</returns>
        public async Task<bool> PassengerNameValidatorAsync(PromptValidatorContext<string> promptValidatorContext, CancellationToken cancellationToken)
        {
            //Get user answer and convert it to list.
            string passengerName = promptValidatorContext.Recognized.Value.Trim();
            string[] splited = passengerName.Split(null);
            //Validate if passengers name is 1 or 3 words long.
            if (string.IsNullOrEmpty(passengerName) || splited.Length < 1 || splited.Length > 3)
            {
                await promptValidatorContext.Context.SendActivityAsync("Please type in correct name.", cancellationToken: cancellationToken);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Method to execute the task which is to validate name of the city of staring point airport.
        /// </summary>
        /// <param name="promptValidatorContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Boolean true if validation passed, false if validation fails.</returns>
        public async Task<bool> FromValidatorAsync(PromptValidatorContext<string> promptValidatorContext, CancellationToken cancellationToken)
        {
            //Get starting point airport and convert it to list.
            string fromAirport = promptValidatorContext.Recognized.Value;
            string[] splited = fromAirport.Split(null);
            //Validate if starting point airport name is 1 or 2 words long.
            if (string.IsNullOrEmpty(fromAirport) || splited.Length < 1 || splited.Length > 2)
            {
                await promptValidatorContext.Context.SendActivityAsync("Please type in correct departure airport.", cancellationToken: cancellationToken);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Method to execute the task which is to validate name of the city of ending point airport.
        /// </summary>
        /// <param name="promptValidatorContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Boolean true if validation passed, false if validation fails.</returns>
        public async Task<bool> ToValidatorAsync(PromptValidatorContext<string> promptValidatorContext, CancellationToken cancellationToken)
        {
            //Get ending point airport and convert it to list.
            string toAirport = promptValidatorContext.Recognized.Value;
            string[] splited = toAirport.Split(null);
            
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(promptValidatorContext.Context, () => new FlightInfo());
            //Validate if ending point airport is 1 or 2 words long and is not the same as starting point airport.
            if (string.IsNullOrEmpty(toAirport) || splited.Length < 1 || splited.Length > 2 || flightInfo.FromAirport == toAirport)
            {
                await promptValidatorContext.Context.SendActivityAsync("Please type in correct arrival airport.", cancellationToken: cancellationToken);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Method to execute the task which is to validate departure date on starting point airport.
        /// </summary>
        /// <param name="promptValidatorContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Boolean true if validation passed, false if validation fails.</returns>
        public async Task<bool> StartDateValidatorAsync(PromptValidatorContext<IList<DateTimeResolution>> promptValidatorContext, CancellationToken cancellationToken)
        {
            //Get starting point departure date and check if it is successfully recognized.
            if (!promptValidatorContext.Recognized.Succeeded || !DateTime.TryParse((promptValidatorContext.Recognized.Value as IList<DateTimeResolution>)[0].Value, out DateTime recognizedStartDate))
            {
                await promptValidatorContext.Context.SendActivityAsync("Please type in correct departure date.", cancellationToken: cancellationToken);
                return false;
            }
            DateTime today = DateTime.Now.Date;
            //This block checks if starting point departure date is grater then today.
            if (DateTime.Compare(recognizedStartDate, today) > 0)
            {
                return true;
            }
            else
            {
                await promptValidatorContext.Context.SendActivityAsync("Departure date should be greater than today.", cancellationToken: cancellationToken);
                return false;
            }
        }

        /// <summary>
        /// Method to execute the task which is to validate departure date on ending point airport.
        /// </summary>
        /// <param name="promptValidatorContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Boolean true if validation passed, false if validation fails.</returns>
        public async Task<bool> EndDateValidatorAsync(PromptValidatorContext<IList<DateTimeResolution>> promptValidatorContext, CancellationToken cancellationToken)
        {
            //Get ending point departure date and check if it is successfully recognized.
            if (!promptValidatorContext.Recognized.Succeeded || !DateTime.TryParse((promptValidatorContext.Recognized.Value as IList<DateTimeResolution>)[0].Value, out DateTime recognizedEndDate))
            {
                await promptValidatorContext.Context.SendActivityAsync("Please type in correct arrival date.", cancellationToken: cancellationToken);
                return false;
            }
            FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(promptValidatorContext.Context, () => new FlightInfo());
            DateTime recognizedStartDate = DateTime.Parse(flightInfo.StartDate);
            //This block checks if ending point departure date is grater or equal than starting point departure date.
            if (DateTime.Compare(recognizedEndDate, recognizedStartDate) >= 0)
            {
                return true;
            }
            else
            {
                await promptValidatorContext.Context.SendActivityAsync("Return date should be greater or the same as departure date", cancellationToken: cancellationToken);
                return false;
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
        /// <param name="ReturnDate">Ending point departure date to be displayed on the card.</param>
        /// <returns>Adaptive Card attachment object.</returns>
        private static Attachment CreateReservationAttachement(string filePath, string PassengerName, string DepartureAirport, string ArrivalAirport, string DepartureDate, string FlightClass, string FlightCost, string ReturnDate = null)
        {
            //Read adaptive card template.
            string cardJson = File.ReadAllText(filePath);
            //Replace the generic labels with flight details.
            cardJson = cardJson.Replace("<Header>", "New Flight Reservation")
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
        /// Method to execute the task which is to create adaptive card attachement to display quick help to the user.
        /// </summary>
        /// <returns></returns>
        private static Attachment CreateQuickHelpAttachement()
        {
            //Initialize the AdaptiveCard object.
            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));
            //Add heade to the body of adaptive card.
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Let's start reservation process!" +
                "\n In case you get lost just send one of the messages to the bot:",
                Size = AdaptiveTextSize.Default,
            });

            //Add facts set with interruption key words and descriptions.
            card.Body.Add(new AdaptiveFactSet()
            {
                Facts = new List<AdaptiveFact>()
                {
                    new AdaptiveFact {Title = "help", Value= "Information about the flight bot will be displayed."},
                    new AdaptiveFact {Title = "cancel", Value="Current resercation will be aborted and main menu will be displayed."},
                    new AdaptiveFact {Title="exit", Value="Conversation will be immediately ended."}
                }
            });
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            return attachment;
        }

        /// <summary>
        /// Method to execute the task which is to generate flight cost based on the flight class.
        /// </summary>
        /// <param name="tripClass">Flight class</param>
        /// <returns></returns>
        public int GenerateFlightCost(string tripClass)
        {
            //Initialize FlightCost.
            int FlightCost;
            //This block decides which cost range should be generated based on the tripClass..
            Random random = new Random();
            if (tripClass == "Standard")
            {
                FlightCost = random.Next(500, 900);
            }
            else if (tripClass == "Business")
            {
                FlightCost = random.Next(1000, 2000);
            }
            else
            {
                FlightCost = random.Next(2100, 4000);
            }
            return FlightCost;
        }

    }
}
