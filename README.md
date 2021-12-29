# PokerBot

This repo is the source code for the Azure Digital Twins/Azure Percept DK PokerBot as described in the blog: https://techcommunity.microsoft.com/t5/internet-of-things-blog/playing-poker-with-azure-iot/ba-p/3020447

This code consists of the following:
* DigitalTwinsPoker Directory
  * DigitalTwinsPoker.sln
    * Azure Function Code, written in C#, .Net 6, Successfully compiled using Visual Studio Enterprise 2022, Version 17.0.4
      * HoldemFunctions, the game playing functions
      * PerceptFunctions, the function for reading the Suite Identifier model output
      * DigitalTwinLibrary, for interfacing with Azure Digital Twins (only useful for this implementation)
      * DataTransferLibrary, as a format for passing game information back from the functions
      * CardDataAndMath, the library for doing all the game math, including C# game object classes
    * ContinuousFileTransfer, a piece of code to run in the background and transfer the images from VLC into Azure Blob Storage
* PokerBot/PokerBot Directory
  * PokerBot.sln
    * Poker Bot for publishing to Azure using the Bot Framework Composer, Version 2.1.1
* PokerBot/AdaptiveCards Directoyr
  * PlayerHandTemplate.json, the adaptive card template, which is also installed in PokerBot.sln
* The Suite ML Model
  * Suite-Detrmination-ML-Model/Suite-Detrmination-ML-Model (requires git lfs for download)

You will also need VLC: [VLC](https://www.videolan.org/vlc/)

I am unable to place the card images in this repo, but you can download them yourself at: [American Contract Bridge League Resource Center](https://acbl.mybigcommerce.com/52-playing-cards/)

.Net Core 3.1 version: commit 38f773df822dd3bb696600efc1fca99af20fc73a on Dec 2, 2021 contains the working .Net Core 3.1 version, Successfully compiled using Visual Studio Enterprise 2019, Version 16.11.5

## Installation and Execution

Please read the blog post for a full description of the solution, however, the installation steps are:

**Create**
1. Install the Azure Percept DK and Azure Percept Studio per the instructions provided with the Azure Percept DK (which will involve creating an IoT Hub)
2. On a laptop/PC on the same WiFi network as the Azure Percept, install VLC and the ContinuousFileTransfer application.
3. Create two Azure Function Apps (Holdem and Percept).  Two would match the blog post exactly, but you could also use one for all functions
4. Create two Azure Blob Storage Accounts (archive and runtime) Two would match the blog post exactly, but you could also use one for everything
   1. Create a "Card" container to hold the card images in the Archive Storage Account, copy the card images here
   2. Create a "SuiteModel" container to hold the Suite Model in the Archive Storage Account, copy the suite model here
   3. Create a "Lockfile" container in the Runtime Storage Account
   4. Create a "Frames" container in the Runtime Storage Account
   5. Create a "Models" container in the Runtime Storage Account, copy the 4 digital twin models from DigitalTwinsPoker.sln into this container
5. Create an Azure Digital Twins instance
6. Create an Azure Stream Analytics Job instance
7. Create an Azure ComputerVision Account
8. Create an Azure FormsRecognizer Account

**Configure**

9. Deploy the function code to Azure
10. Deploy the Bot code to Azure, you will need the urls of the Azure Function App(s) as part of the configuration in the Bot Framework Composer Solution
11. Modify the Azure Percept IoT Hub device twin so that the Azure Percept uploads the Suite Model on startup (in the azureeyemodel)
12. Configure the Azure Stream Analytics Job to read from the IoT Hub and to output with calls to the single function in the Percepts Function App (HandlePlayerCard).  The query to run in the job is in the blog post
13. Configure VLC to use the "Scene Filter" to create a single, updating frame image on the local machine 
14. In the Percept Functions configuration add:
    1. The lockfile container and lockfile filename
    2. The frames container and frame filename
    3. The Azure ComputerVision  and Azure FormsRecognizer account information and keys
    4. The PlayerCard function url
    5. The access key to the runtime storage account
15. In the ContinuousFileTransfer application configure
    1. The frames container and frame filename
    2. The VLC "Scene Filter" location information
16. In the Holdem Functions configuration add:
    1. The digital twins instance URL
    2. The access key to the runtime storage account
    3. The Card Image location and a SAS key (from the Archive Storage Account)

**Starting Up**

17. Turn on the Azure Percept DK
18. Start both function apps
19. Start the Streaming Analytics job
20. Start the ContinuousFileTransfer application
21. Start VLC.  You want to stream: rtsp://192.168.1.31:8554/raw, where substitute 192.168.1.31 for the IP address of your Percept
22. Run the bot from where ever you deployed it.  You can run it from the Azure Test Web Chat panel in the Azure Portal or deploy it anywhere a bot can be deployed

## Architecture

![Poker Architecture](https://github.com/microsoft/MTC_PokerBot/blob/main/images/Poker-Architecture.png)

## Game Logic Functions

These are the game logic functions.  Below the table is a description of how they are used:

![Game Logic Functions](https://github.com/microsoft/MTC_PokerBot/blob/main/images/Game-Logic-Functions.png)

In non-camera mode, CreateGame is called first and just returns success or failure.  PlayerCard is then called twice, also returning success or failure.  The Bot does keep track of how many times it has been called.  Similarly, FlopTurnRiver causes the cards to be dealt within the Azure Digital Twins game model, but only returns success or failure.  After any call to FlopTurnRiver or a second call to PlayerCard, GetCurrentGameStatus is called.  It returns a full JSON representation of the game, the cards in the game as they are related to hands, and all the statistics that the Bot would need to display to the user.  The Bot merely picks the relevant information out of the structure and displays it to the player. 
Many of the functions require the player’s name as input, as it is a label within the digital twin.  PlayerCard and FlopTurnRiver do not require this since they need to request this information from the digital twin anyway.  The parameter is supplied to GetCurrentGameStatus and GetNumPlayerCards as a performance enhancement.

Conversely, in camera-mode, CreateGame is still called first, but then the Player will not enter a card, but will instead enter “Camera”.  This will cause the bot to begin counting down, “3…2…1…Cheese” at which point the card image is captured.  However, this is merely UI redirection.  The camera is running at 30 frames a second and will attempt to perform a capture from the moment that it identifies a card.  The countdown is important, as when it is complete, the Bot will call GetNumPlayerCards and the game will tell it how many player cards it knows about.  If it knows about 2 cards, the bot will then call GetCurrentGameStatus and continue on.

After a call to GetCurrentGameStatus, the Bot will display the hands and contained cards to the player, along with some information regarding who is in the lead.  

## Details on the Azure Bot to Power Virtual Agent Conversion

I either needed to do some sort of manual conversion or a full rewrite from scratch.  Since the bot would need to move into the Framework Composer anyway, I opted for a manual conversion.  This way I could effectively keep most of the work I did to create my Bot Dialogs and Adaptive Cards.  Converting in this manner is fully undocumented, so I thought I would include some details here in order to help anyone attempting the same thing.

The conversion was very straight forward, but there were a couple of gotchas to watch out for.  First off, when you create a new bot in the Bot Framework Composer, under the hood you end up with a Visual Studio project which contains Dialog, Language Understanding (lu), and Language Generation (lg) templates in order to define the Bot logic.  A Visual Studio project is also configured so that you can add custom code into your Bot.  Once again, I am not going to discuss how the Bot Framework works in this blog, but if you need a good starting point, start here:  https://docs.microsoft.com/azure/bot-service/index-bf-sdk?view=azure-bot-service-4.0.  I did not include any custom code in my Bot, since all of my custom code was in Azure Functions called by the bot.

On the other hand, when a PVA is opened in the Bot Framework Composer (from the PVA menu), there is no Visual Studio project, as it’s all templates.  However, both seemed to have the same internal structure, and since I did not have any custom code in my initial Bot, this manual conversion was easy to accomplish.  My conversion methodology was to a) open the PVA with the Bot Framework Composer, b) create triggers and empty dialogs in the Composer interface, c) copy the dialog, lu, and lg configurations from my Bot Framework Bot into the new structure, and d) make any manual changes necessary.  The Bot Composer will not upload a Bot back into PVA if there are any irregularities in any of its files.  This worked and I was able to do the conversion in a single afternoon, but:

* The PVA configuration files were insistent that every Action or ElseAction as the results of an if condition were written in the array syntax, even if there was only one Action.  Bot Framework bots seem to be more flexible in interpreting both syntaxes.
* PVA conditionals required that for every condition, something must occur on the Action branch.  You can’t just say, “if x > 3, do nothing, but if it is not then do this”, although the Bot Framework Bot was just fine with it.  I had a bunch of logic like this in my initial bot (because, why not), so the fix was to create a bot property that is unused and assign it a value in these cases.  So this way the bot does something.
* The Bot Framework Bot supports the Adaptive Card syntax 1.3, but PVA only supports version 1.2.  Luckily, I did not use any 1.3 features in my Adaptive Card definition, so I only needed to switch the version number.
* I was unclear how to copy the trigger definitions over, so I merely cut ‘n pasted within the composer interface.  This was a very easy process, as my triggers just called dialogs where all the real work was completed.
* The default Recognizer from the Bot Framework is not accepted by the PVA.  Specifically, I needed to change this in the .dialog files:

```
  "generator": "GameDialog.lg",
  "recognizer": "GameDialog.lu.qna",
  "id": "GameDialog"
```

To this:

```
  "generator": "GameDialog.lg",
  "recognizer": {
    "$kind": "Microsoft.VirtualAgents.Recognizer"
  },
  "id": "GameDialog"
```

When I was finished, I had two separate Bots, which were identical in functionality and very similar in form.  The PVA bot is on the left.

![Bot Version Project Comparison](https://github.com/microsoft/MTC_PokerBot/blob/main/images/bot-version-comparison.png)

## Game Specific DigitalTwinLibrary Implementation

I created a single library for my Azure Functions to use to call the Azure Digital Twins service.  It is by no means appropriate for use outside of this particular implementation.  The thirteen functions in the library can be grouped into three subgroups:

**Login**

Within Azure, Digital Twin access is through a User Managed Identity.  The identity is assigned to the Function App, and then given the role of Azure Digital Twins Data Owner within the Azure Digital Twins service.  Data owner is necessary, since the functions will be modifying the underlying digital twin instances, not just reading them.

Actual login in accomplished through the DefaultAzureCredential method of the Azure.Identity library.  It is important to check for a null managedClientId – which is the id of the User Managed Identity, so the method can also be used for local debugging, where the identity will not exist.  In this demonstration, I placed the managedClientId in the configuration settings of the function app, but it could also be placed in an Azure Key Vault.

```
        public static DigitalTwinsClient DigitalTwinsLogin(string url, string managedClientId)
        {
            var options = new DefaultAzureCredentialOptions();

            if(!string.IsNullOrWhiteSpace(managedClientId))
            {
                options.ManagedIdentityClientId = managedClientId;
            }

            var credential = new DefaultAzureCredential(options);
            var dtclient = new DigitalTwinsClient(new Uri(url), credential);
            return dtclient;
        }
```

**Instance Manipulation Methods**

Since this library was not being produced for general use, I only needed 7 methods.  When a digital twin is returned, the return type is BasicDigitalTwin.  These are:

* GetOneTwin – Query Interface, returns a single digital twin based on a query (without joins)
* GetNumberOfRelationships – SDK/API, returns the number of relationships of a give type on a particular twin (so this can be used to determine the number of cards in a deck or hand), returns an integer
* MoveTopDeckCardToHand – SDK/API and Query Interface, breaks all relationships of the top card to the deck, and then creates relationships to a particular hand.  Returns the card number of the new top card in the deck along with the name of the card which was moved
* GetFirstCard – SDK/API, retrieves the digital twins of the first card of the deck
* GetCards – SDK/API, returns lists of digital twins of the cards in each hand
* GetHands – SDK/API, returns the digital twins of the hands of the game
* GetGame – SDK/API, returns the game digital twin

Taking a look at parts of MoveTopDeckCardToHand will show how the SDK can be used.  Within the game, the various components are related to each other.  When the game starts, all of the cards are related to the deck, but as they get dealt out, the cards become related to a particular hand and the relationships with the decks need to be removed.

This line calls GetOneTwin to make a query for an object of type card which has an id of the top of deck number.  When the game is initially created all of the cards get an id which is their placement in the shuffled deck.

```
nextCard = await DigitalTwinsLibrary.GetOneTwin(dtclient, $"SELECT * FROM DIGITALTWINS WHERE IS_OF_MODEL('dtmi:games:Card;1') and $dtId = '{topOfDeck:D2}'");
```

This line gets all the relationships on a twin.  Within the foreach, we loop until we find the relationship for which we are looking.  The AsyncPagable pattern is used often within the library.

```
AsyncPageable<BasicRelationship> rels = dtclient.GetRelationshipsAsync<BasicRelationship>(deckTwin.Id);
await foreach (var r in rels)
{
	…
}
```

Once we find the correct relationship between the card and the deck, we delete it.  The format of the relationship name is related to how I chose to name the relationships when the game is set up.  There is no set format which the relationship names need to follow.

```
await dtclient.DeleteRelationshipAsync(deckTwin.Id, $"{deckTwin.Id}-has-{nextCard.Id}");
```

The initial deck relationships appear like this in the Deck model, with no format, but an actual property on the relationship.  For example, card 1 will have a relationship to the deck with a cardOrder of 1:

```
      {
        "@type": "Relationship",
        "@id": "dtmi:games:deck_has_cards;1",
        "name": "has_cards",
        "displayName": "has_cards",
        "target": "dtmi:games:Card;1",
        "properties": [
          {
            "name": "cardOrder",
            "@type": "Property",
            "schema": "integer"
          }
        ]
      }
```
Finally, a new relationship with the hand needs to be created.  The iorder property is passed in, as the number of cards already present in the hand.

```
string handToCardRel = $"{handTwin.Id}-has-{nextCard.Id}";
await dtclient.CreateOrReplaceRelationshipAsync(handTwin.Id, handToCardRel, new BasicRelationship { TargetId = nextCard.Id, Name = "has_cards", Properties = { { "cardOrder", iorder } } });
```

**Telemetry Methods**

The last subset of methods has to do with updating telemetry values.  You can think of telemetry values as properties on objects (and on relationships of objects), which are defined as part of the model definition.  Although the same Azure.DigitalTwins.Core library is used, telemetry updates are going to center around the UpdateDigitalTwinsAsync method, as you are not going to be changing any twin instances or relationships.  I have 4 methods in my developed library:

* UpdateGameTelemetry
* UpdatePotentials
* UpdateProbabilityOfWinningTelemetry
* UpdateHandInfo

If we take a look at UpdateHandInfo, this method updates the current hand description and the cards ranked in order.  These properties were created so that the Bot does not have to recompute just to show the status of the game, but they update every time a card is dealt.

If we look at the top of the Hand Model Definition, we can see how the properties are defined:

```
{
  "@id": "dtmi:games:Hand;1",
  "@type": "Interface",
  "displayName": "Hand",
  "@context": "dtmi:dtdl:context;2",
  "contents": [
    {
      "@type": "Property",
      "name": "Name",
      "schema": "string"
    },
    {
      "@type": "Property",
      "name": "ProbabilityOfWinning",
      "schema": "float"
    },
    {
      "@type": "Property",
      "name": "HandType",
      "schema": "string"
    },
    {
      "@type": "Property",
      "name": "HighCard",
      "schema": "string"
    },
```

Then, we can see how this correlates to the update method:

```
public static async Task UpdateHandInfo(DigitalTwinsClient dtclient, BasicDigitalTwin twin, Hand hand)
        {
            var update = new JsonPatchDocument();
            update.AppendReplace("/HandType", Hand.ReverseTargetHands[hand.Description.handType]);
            update.AppendReplace("/HighCard", Card.CardValueNames[hand.Description.highCard]);
            update.AppendReplace("/SecondHighCard", Card.CardValueNames[hand.Description.secondHighCard]);
            update.AppendReplace("/ThirdHighCard", Card.CardValueNames[hand.Description.thirdHighCard]);
            update.AppendReplace("/FourthHighCard", Card.CardValueNames[hand.Description.fourthHighCard]);
            update.AppendReplace("/FifthHighCard", Card.CardValueNames[hand.Description.fifthHighCard]);
            await dtclient.UpdateDigitalTwinAsync(twin.Id, update);
        }
```

## Contributing

This project is for demo purposes only, and there is no guarantee that it will ever be updated or maintained in any manner.  Similarly, you may add questions or problems in GitHub issues, however, there is no guarantee that they will be read or responded to by the repo owner.

If you like this project and want to turn it into an ongoing project in any manner, you are welcome to make a copy of the repo, as long as the license terms are kept intact.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
