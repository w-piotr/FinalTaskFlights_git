using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using AdaptiveCards;
using System.IO;
using Newtonsoft.Json;

namespace FinalTaskFlights
{
    /// <summary>
    /// Main Chatbot class
    /// </summary>
    public class FinalTaskFlightsBot : IBot
    {
        private const string MainDialogId = "MainDialog";
        private readonly ChatbotAccessor _accessor;
        private readonly DialogSet _dialogSet;


        public FinalTaskFlightsBot(ChatbotAccessor accessor)
        {
            this._accessor = accessor ?? throw new System.ArgumentException("Accessor object is empty!");

            _dialogSet = new DialogSet(this._accessor.ConversationDialogStateAccessor);
            _dialogSet.Add(new MainDialogClass(MainDialogId, _accessor));

        }

        /// <summary>
        /// Main activity methond of the chatbot.
        /// </summary>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            //Create dialogContext which is used to store all information around state.
            DialogContext dialogContext = await _dialogSet.CreateContextAsync(turnContext);

            //Assing user response to be validated against being interruption.
            string interruption = turnContext.Activity.Text;
            //This block validates user response and if one of the key words is used (more,help,cancel,exit) suitable action is taken.
            if (!string.IsNullOrWhiteSpace(interruption))
            {
                if (interruption.Trim().ToLowerInvariant() == "more flight")
                {
                    await turnContext.SendActivityAsync(
                        "Standard - 2 or 3 seats next to each other, radio output in the seat, no meal, cold beverage (water or juice)\n\n" +
                        "Premium - onboarding priority over Standard class, 2 seats next to each other, 230V AC/DC connector and USB connector in the seat, 20% more space for legs then in the Standard class, no meal, cold beverage (water or juice)\n\n" +
                        "Business - Business lounge with buffet and open bar, onboarding priority over Premium and Standard classes, separate seat which can be converted in to bed, 24 inches flat screen (TV, DVD, USB, HDIM), headset, meal and beverage included",
                        cancellationToken: cancellationToken);
                    await dialogContext.RepromptDialogAsync();
                }
                else if (interruption.Trim().ToLowerInvariant() == "more cars")
                {
                    await turnContext.SendActivityAsync(
                        "Economy - Basic radio, manually opened windows and central aircondition. Costs 15$ per a day.\n\n" +
                        "Standard - Audio with jack and usb connectors, electric windows in first seats row, separate aircondition for every seats row. Costs 40$ per a day.\n\n" +
                        "Business - Hight class audio system with jack and usb connectors, colorful satellite navigation with voice control, all electric windows and tailgate, separate aircondition for every seat. Costs 80$ per a day.",
                        cancellationToken: cancellationToken);
                    await dialogContext.RepromptDialogAsync();
                }
                else if (interruption.Trim().ToLowerInvariant() == "help")
                {
                    Attachment attachment = CreateHelpAttachement();
                    var reply = turnContext.Activity.CreateReply();
                    reply.Attachments = new List<Attachment>() { attachment };
                    await turnContext.SendActivityAsync(reply, cancellationToken: cancellationToken);
                    await dialogContext.RepromptDialogAsync(cancellationToken: cancellationToken);

                }
                else if (interruption.Trim().ToLowerInvariant() == "cancel")
                {
                    await dialogContext.CancelAllDialogsAsync();
                    await dialogContext.BeginDialogAsync(MainDialogId);
                }
                else if (interruption.Trim().ToLowerInvariant() == "exit")
                {
                    await turnContext.SendActivityAsync("Goodby Passenger!");
                    await dialogContext.CancelAllDialogsAsync();
                }
            }
            //This block is executed if message is posted, existing dialog is continued. 
            if (turnContext.Activity.Type == ActivityTypes.Message && !turnContext.Responded)
            {
                DialogTurnResult turnResult = await dialogContext.ContinueDialogAsync();
                if (turnResult.Status == DialogTurnStatus.Complete || turnResult.Status == DialogTurnStatus.Cancelled)
                {
                    await turnContext.SendActivityAsync("Goodby Passenger!");
                }
                else if (!dialogContext.Context.Responded)
                {
                    await turnContext.SendActivityAsync("I am unable to do anything...");
                }
            }
            //This block is executed on conversation update.
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
                {
                    if (turnContext.Activity.Recipient.Id != member.Id)
                    {
                        //Message to be send at the begining of the diatlog when user join the conversation
                        await turnContext.SendActivityAsync("Hello new Passenger!", cancellationToken: cancellationToken);
                        //Invoke of the Main Dialog
                        await dialogContext.BeginDialogAsync(MainDialogId);
                    }
                }
            }
            // Save changes after every turn.
            await _accessor.ConversationState.SaveChangesAsync(turnContext, false);
        }

        /// <summary>
        /// Method to execute the task which is to create hero card attachment with help content to be displayed to the user.
        /// </summary>
        /// <returns>Hero Card attachement object.</returns>
        private static Attachment CreateHelpAttachement()
        {
            var heroCard = new HeroCard()
            {
                Title = "Help",
                Text = "Flight Reservation Bot is an assistant which helps you to book a flight.\n\n" +
                        "In order to book a flight ticket please provide following information: Passenger's Name, Departure Airport, Arrival Airport, are you going to travel one way or return back to destination airport, " +
                        "Departure Date, Return Date (if applicable), Flight Class. At the end of reservation process you will be able to book a car rental at destination airport. " +
                        "Flight Reservation Bot allows you also to display all reservations you have done during current conversation, display specific reservation or even cancel it.\n\n" +
                        "In case you:" +
                        "\n-would like to abort current reservation process and get back to main menu please type in 'cancel'," +
                        "\n-need help please type in 'help'," +
                        "\n-would like to immediately end the conversation please typ in 'exit'." +
                        "\n\n" +
                        "If you want to make a reservation by yourself just use Skyscanner.",
                Buttons = new List<CardAction>() { new CardAction(ActionTypes.OpenUrl, title: "Go to Skyscanner", value: "https://www.skyscanner.pl/") },
            };
            Attachment attachment = heroCard.ToAttachment();

            return attachment;
        }
    }
}
