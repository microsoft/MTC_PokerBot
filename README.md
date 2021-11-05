# PokerBot

This repo is the source code for the Azure Digital Twins/Azure Percept DK PokerBot as descripted in the blog: **URL-Coming-Soon**

This code consists of the following:
* DigitalTwinsPoker Directory
  * DigitalTwinsPoker.sln
    * Azure Function Code, written in C#, .Net Core 3.1, Successfully compiled using Visual Studio Enterprise 2019, Version 16.11.5
      * HoldemFunctions, the game playing functions
      * PerceptFunctions, the function for reading the Suite Identifier model output
      * DigitalTwinLibrary, for interfacing with Azure Digital Twins
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

I am unable to place the card images in this repo, but you can download them yourselves at: [American Contract Bridge League Resource Center](https://acbl.mybigcommerce.com/)52-playing-cards/

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

Although there will be no updates to this repo, there will be no updates except that I will attempt to update all of the C# code (functions and ContinuousFileTransfer) to .Net 6 and Visual Studio 2022 in the December-January timeframe.

## Contributing

This project is for demo purposes only, and there is no guarantee that it will ever be updated or maintained in any manner.  Similarly, you may add questions or problems in GitHub issues, however there is no guarantee that the will be read or responded to by the repo owner.

If you like this project and want to turn it into an ongoing project in any manner, you are welcome to make a copy of the repo, as long as the license terms are kept in tact.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
