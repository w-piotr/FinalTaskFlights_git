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
    /// Subdialog class which collects car rental details in waterfall dialog.
    /// </summary>
    public class GetRentalDetailsDialog : ComponentDialog
    {
        private const string GetRentalDetailsDialogId = "GetRentalDetailsDialog";
        private readonly ChatbotAccessor _accessor;


        /// <summary>
        /// GetRentalDetailsDialog constructor.
        /// </summary>
        /// <param name="dialogId">Dialog Id parameter inherited from CompenentDialog Class</param>
        /// <param name="accessor">Chatbot accessors</param>
        public GetRentalDetailsDialog(string dialogId, ChatbotAccessor accessor) : base(dialogId)
        {
            this.InitialDialogId = GetRentalDetailsDialogId;
            this._accessor = accessor ?? throw new System.ArgumentException("Accessor object is empty!");

            //Definition of waterfall steps to be executed in this class as a waterfall dialog.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
                AskForRentalLength,
                AskForPassengersNumberAsync,
                AskForChildSeat,
                AskForCarClass,
                DisplayCarReservation,
                EndCarReservatonDialogAsync
            };
            //Definition of dialogs which are executed in GetFlightDetailsDialog class.
            AddDialog(new WaterfallDialog(GetRentalDetailsDialogId, waterfallSteps));
            //Definition of waterfall steps to be executed in this class as a waterfall dialog.
            AddDialog(new TextPrompt("RentalLength", RentalLengthValidatorAsync));
            AddDialog(new TextPrompt("PassengersNumber", PassengersNumberValidatorAsync));
            AddDialog(new TextPrompt("ChildSeat", ChildSeatValidatorAsync));
            AddDialog(new ChoicePrompt("CarClassChoice"));
            AddDialog(new ChoicePrompt("ConfirmChoice"));
        }

        
        /// <summary>
        /// Method to execute the task which is to collect lenght of rental.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>RentalLength TextPrompt</returns>
        private async Task<DialogTurnResult> AskForRentalLength(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("RentalLength", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter the number of days you would like to rent a car.")
            },
            cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to collect number of passengers.
        /// </summary>
        /// <remarks>Method gets RentalInfoAccessor in order to save the previous waterfall step result as a car rental lenght and then sets the RentalInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>PassengersNumber TextPrompt</returns>
        private async Task<DialogTurnResult> AskForPassengersNumberAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            RentalInfo rentalInfo = await _accessor.RentalInfoAccessor.GetAsync(stepContext.Context, () => new RentalInfo());
            rentalInfo.RentalLength = int.Parse(stepContext.Result as string);
            await _accessor.RentalInfoAccessor.SetAsync(stepContext.Context, rentalInfo);

            return await stepContext.PromptAsync("PassengersNumber", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter the number of people which will travel with you.")
            },
            cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to collect the number of child seats.
        /// </summary>
        /// <remarks>Method gets RentalInfoAccessor in order to save the previous waterfall step result as a number of passengers travelling with user and then sets the RentalInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ChildSeats TextPrompt</returns>
        private async Task<DialogTurnResult> AskForChildSeat(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            RentalInfo rentalInfo = await _accessor.RentalInfoAccessor.GetAsync(stepContext.Context, () => new RentalInfo());
            rentalInfo.PassengersNumber = int.Parse(stepContext.Result as string);
            await _accessor.RentalInfoAccessor.SetAsync(stepContext.Context, rentalInfo);

            //This block decides if ChildSeat should be prompted or skip to the next step.
            if ((stepContext.Result as string) == "0")
            {
                return await stepContext.NextAsync();
            }
            else
            {
                return await stepContext.PromptAsync("ChildSeat", new PromptOptions
                {
                    Prompt = MessageFactory.Text("In case you are going to travel with child, please enter the number of child seats you will need.")
                },
                cancellationToken);
            }
        }

        /// <summary>
        /// Method to execute the task which is to collect the information about car class.
        /// </summary>
        /// <remarks>Method gets RentalInfoAccessor in order to save the previous waterfall step result as a number o child seats user would need and then sets the RentalInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>CarClassChoice ChoicePrompt</returns>
        private async Task<DialogTurnResult> AskForCarClass(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            RentalInfo rentalInfo = await _accessor.RentalInfoAccessor.GetAsync(stepContext.Context, () => new RentalInfo());
            //Create a boolean with information if childseat was provided or not prompted in previous step.
            bool anyChild = int.TryParse(stepContext.Result as string, out int childSeats);
            //This block is to set right childseats number.
            if (anyChild)
            {
                rentalInfo.ChildSeats = childSeats;
            }
            else
            {
                rentalInfo.ChildSeats = 0;
            }
            await _accessor.RentalInfoAccessor.SetAsync(stepContext.Context, rentalInfo);

            return await stepContext.PromptAsync("CarClassChoice", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please choose car class (to access more details type in 'more cars'):"),
                RetryPrompt = MessageFactory.Text("I don't recognize this option. Try again"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Economy", "Standard", "Premium" })
            },
            cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to display already gathered information as an adaptive card and ask user to confirm or start over the reservation waterfall dialog.
        /// </summary>
        /// <remarks>Method gets RentalInfoAccessor in order to save the previous waterfall step result as a class of car and then sets the RentalInfoAccessor.</remarks>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ConfirmChoice ChoicePrompt</returns>
        private async Task<DialogTurnResult> DisplayCarReservation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FoundChoice carclassChoice = stepContext.Result as FoundChoice;
            RentalInfo rentalInfo = await _accessor.RentalInfoAccessor.GetAsync(stepContext.Context, () => new RentalInfo());
            rentalInfo.CarClass = carclassChoice.Value;
            rentalInfo.RentalCost = GenerateRentalCost(rentalInfo.CarClass, rentalInfo.RentalLength);
            await _accessor.RentalInfoAccessor.SetAsync(stepContext.Context, rentalInfo);

            //This block creates adaptive card attachement with car rental details.
            await stepContext.Context.SendActivityAsync("Thank you, below you can find your car rental details.");
            Attachment attachment = new Attachment();
            attachment = CreateAdaptiveCardAttachement(@".\Resources\RentalDetails.txt", rentalInfo.RentalLength, rentalInfo.PassengersNumber, rentalInfo.ChildSeats, rentalInfo.CarClass, rentalInfo.RentalCost);

            //This block creats reply activity with adaptive card attachement and sends it to the user.
            Activity reply = stepContext.Context.Activity.CreateReply();
            reply.Attachments.Add(attachment);
            await stepContext.Context.SendActivityAsync(reply, cancellationToken: cancellationToken);

            return await stepContext.PromptAsync("ConfirmChoice", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please verify if rental details are correct and choose option:"),
                RetryPrompt = MessageFactory.Text("I don't recognize this option. Try again"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Yes, confirm", "No, start over" })
            },
            cancellationToken);
        }

        /// <summary>
        /// Method to execute the task which is to end the GetRentalDetailsDialog and set the data to flightInfo object or start over GetRentalDetailsDialog.
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>End dialog async or replace dialog async</returns>
        private async Task<DialogTurnResult> EndCarReservatonDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FoundChoice classflightChoice = stepContext.Result as FoundChoice;
            RentalInfo rentalInfo = await _accessor.RentalInfoAccessor.GetAsync(stepContext.Context, () => new RentalInfo());

            //This block is to decide if car rental details should be set to object of FlightInfo class or start over car reservation dialog.
            if (classflightChoice.Value.Equals("Yes, confirm", StringComparison.InvariantCultureIgnoreCase))
            {
                FlightInfo flightInfo = await _accessor.FlightInfoAccessor.GetAsync(stepContext.Context, () => new FlightInfo());
                flightInfo.Rental = rentalInfo;
                await _accessor.FlightInfoAccessor.SetAsync(stepContext.Context, flightInfo);
                await stepContext.Context.SendActivityAsync("Thank you, your car rental has been associated with you flight reservation. Contact with Rental Office at destination airport in order to get the car. You can ask airport staff to get directions of Rental Office.");

                return await stepContext.EndDialogAsync();
            }
            else
            {
                RentalInfo blankInfo = new RentalInfo();
                await _accessor.RentalInfoAccessor.SetAsync(stepContext.Context, blankInfo);
                return await stepContext.ReplaceDialogAsync(GetRentalDetailsDialogId, cancellationToken: cancellationToken);
            }
        }



        /// <summary>
        /// Method to execute the task which is to validate rental length.
        /// </summary>
        /// <param name="promptValidatorContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Boolean true if validation passed, false if validation fails.</returns>
        public async Task<bool> RentalLengthValidatorAsync(PromptValidatorContext<string> promptValidatorContext, CancellationToken cancellationToken)
        {
            //Create boolean true if rental length can be converted to int or false if not.
            bool correctLenght = int.TryParse(promptValidatorContext.Recognized.Value, out int rentalLenght);
            //This block decides if user response is correct and validates it agains the length - greater the 0 and less then 91.
            if (correctLenght==false)
            {
                await promptValidatorContext.Context.SendActivityAsync("The value you have provided is not correct, please try again.", cancellationToken: cancellationToken);
                return false;
            }
            else if (rentalLenght <= 0)
            {
                await promptValidatorContext.Context.SendActivityAsync("You cannot rent a car for less ten 1 day.", cancellationToken: cancellationToken);
                return false;
            }
            else if (rentalLenght >= 90)
            {
                await promptValidatorContext.Context.SendActivityAsync("You cannot rent a car for more ten 90 days.", cancellationToken: cancellationToken);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Method to execute the task which is to validate passengers number.
        /// </summary>
        /// <param name="promptValidatorContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Boolean true if validation passed, false if validation fails.</returns>
        public async Task<bool> PassengersNumberValidatorAsync(PromptValidatorContext<string> promptValidatorContext, CancellationToken cancellationToken)
        {
            //Create boolean true if passengers number can be converted to int or false if not.
            bool correctNumber = int.TryParse(promptValidatorContext.Recognized.Value, out int passengerNumber);
            //This block decides if user response is correct and validates it agains the number - greater the 0 and less then 7.
            if (correctNumber==false)
            {
                await promptValidatorContext.Context.SendActivityAsync("The value you have provided is not correct, please try again.", cancellationToken: cancellationToken);
                return false;
            }
            else if (passengerNumber < 0)
            {
                await promptValidatorContext.Context.SendActivityAsync("The value you have provided is not correct, if you are going to travel alone please type in 0.", cancellationToken: cancellationToken);
                return false;
            }
            else if (passengerNumber > 7)
            {
                await promptValidatorContext.Context.SendActivityAsync("Sorry, we do not have such a big cars.", cancellationToken: cancellationToken);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Method to execute the task which is to validate child seats number.
        /// </summary>
        /// <param name="promptValidatorContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Boolean true if validation passed, false if validation fails.</returns>
        public async Task<bool> ChildSeatValidatorAsync(PromptValidatorContext<string> promptValidatorContext, CancellationToken cancellationToken)
        {
            RentalInfo rentalInfo = await _accessor.RentalInfoAccessor.GetAsync(promptValidatorContext.Context, () => new RentalInfo());
            //Create boolean true if child seats number can be converted to int or false if not.
            bool correctNumber = int.TryParse(promptValidatorContext.Recognized.Value, out int childNumber);
            //This block decides if user response is correct and validates it agains the whole number of passengers.
            if (correctNumber == false)
            {
                await promptValidatorContext.Context.SendActivityAsync("The value you have provided is not correct, please try again.", cancellationToken: cancellationToken);
                return false;
            }
            else if (childNumber > rentalInfo.PassengersNumber)
            {
                await promptValidatorContext.Context.SendActivityAsync("The value is grater than value of passengers you have declared before, please type in correct child seats number.", cancellationToken: cancellationToken);
                return false;
            }
            else
            {
                return true;
            }
        }



        /// <summary>
        /// Method to execute the task which is to create adaptive card attachement to display the car rental reservation details.
        /// </summary>
        /// <param name="filePath">Path to the file with adaptive card txt file Json formatted.</param>
        /// <param name="RentalLength">Number of days passenger rents a car for.</param>
        /// <param name="PassengersNumber">Number of people travelling with passenger by car.</param>
        /// <param name="ChildSeats">Number of child seats passenger would need.</param>
        /// <param name="CarClass">Class of the car.</param>
        /// <returns>Adaptive Card attachment object.</returns>
        private static Attachment CreateAdaptiveCardAttachement(string filePath, int RentalLength, int PassengersNumber, int ChildSeats, string CarClass, int RentalCost)
        {
            //Read adaptive card template.
            string cardJson = File.ReadAllText(filePath);
            //Replace the generic labels with flight details.
            cardJson = cardJson.Replace("<Header>", "New Car Rental")
                .Replace("<PassengersNumber>", PassengersNumber.ToString())
                .Replace("<ChildSeats>", ChildSeats.ToString())
                .Replace("<CarClass>", CarClass)
                .Replace("<RentalCost>", RentalCost.ToString()+" $");
            //Add proper form of the word 'day' (singular or plural) based on rental lenght.
            if (RentalLength > 1)
            {
                cardJson = cardJson.Replace("<RentalLenght>", RentalLength.ToString() + " days");
            }
            else
            {
                cardJson = cardJson.Replace("<RentalLenght>", RentalLength.ToString() + " day");
            };
            Attachment adaptiveCardAttachement = new Attachment()
            {
                ContentType = @"application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };

            return adaptiveCardAttachement;
        }

        /// <summary>
        /// Method to execute the task which is to generate rental cost based on the car class and rental lenght.
        /// </summary>
        /// <param name="carClass"></param>
        /// <param name="rentalLength"></param>
        /// <returns></returns>
        private int GenerateRentalCost(string carClass, int rentalLength)
        {
            //Initialize RentalCost.
            int RentalCost;
            //This block decides which cost should be generated based on the carClass.
            if (carClass == "Economy")
            {
                RentalCost = 15 * rentalLength;
            }
            else if (carClass == "Standard")
            {
                RentalCost = 40 * rentalLength;
            }
            else
            {
                RentalCost = 80 * rentalLength;
            }
            return RentalCost;
        }
    }
    
}
