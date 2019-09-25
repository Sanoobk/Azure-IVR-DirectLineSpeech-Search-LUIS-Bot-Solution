## Interactive Voice Response Bot (IVR) using LUIS, Azure Search & Direct Line Speech

Proof of Concept for the solution design provided by Microsoft in their Azure Solution Architecture examples.  
   
 - Intent is to integrate services with basic configurations to make the solution work end to end.  
 - Not designed for Security, Performance, Availability and Resilience best practices.
 - Wherever possible, external references are provided which helps in configuration.

**Base Solution Architecture** - Provided by Microsoft - https://azure.microsoft.com/en-in/solutions/architecture/interactive-voice-response-bot

![Image of Solution from Microsoft](https://github.com/Sanoobk/Azure-DirectLineSpeech-Search-LUIS-Bot-Solution/blob/master/Images/SolutionArchitectureMicrosoft.PNG)

**Final Solution Architecture** - Modified to replace Skype with Direct Line Speech Client Application and Bing Speech API with Azure Speech Services

![Image of Solution updated](https://github.com/Sanoobk/Azure-DirectLineSpeech-Search-LUIS-Bot-Solution/blob/master/Images/SolutionArchitectureUpdated.png)

**Below is a rough aggregation of components, configurations and tools required for completing the solution.**

![Components and Configuraiton list](https://github.com/Sanoobk/Azure-DirectLineSpeech-Search-LUIS-Bot-Solution/blob/master/Images/ComponentsConfiguration.PNG)


## Stage 1: Create and Configure Bot Web App in Azure

**Create Azure Bot Web App**
- Bot Web App was created using the Microsoft [article](https://docs.microsoft.com/en-gb/azure/bot-service/abs-quickstart?view=azure-bot-service-4.0). 'Echo Bot' Bot Template was used.
- Connect the Bot to Direct Line Speech (DLS) channel using the Microsoft [article](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directlinespeech?view=azure-bot-service-4.0).
- Ignore the last step of downloading the code from Azure portal.
- Microsoft GitHub [source template](https://github.com/microsoft/BotBuilder-Samples/tree/master/experimental/directline-speech/csharp_dotnetcore/02.echo-bot)    was used which    covers basic Direct Link Speech (DLS)    configurations.
- There are many bot templates to choose from. For a good comparison, review the    [link](https://marketplace.visualstudio.com/items?itemName=BotBuilder.botbuilderv4)    and choose the template that suits the requirements and make    necessary configurations needed for DLS in the code.
- Dialogs based bot template such as Core Bot is a preferred choice for production scenarios. [Template Source code with    DLS](https://github.com/microsoft/BotBuilder-Samples/tree/master/experimental/directline-speech/csharp_dotnetcore/13.core-bot).

## Stage 2: Create and configure Cosmos DB Account and Container to store ASP.NET Session state

Cosmos DB Account and Container was created using the [instructions](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-v4-storage?view=azure-bot-service-4.0&tabs=csharp) from Microsoft.

**Configure appsettings.json key values for AzureSearchSettings.**

 - Navigate to Azure Portal Cosmos DB created and in the 'Overview' section, copy the 'URI' and update the appsettings.json 'cosmosServiceEndpoint'.
 - Navigate to 'Containers -> Browse' and copy the corresponding 'Collection ID' and 'Database' and update the appsettings.json 'cosmosDbCollectionName' and 'cosmosDBDatabaseName' respectively.
 - Navigate to 'Settings -> Keys' and copy the corresponding 'Primary Key' and update the appsettings.json 'cosmosDBKey'.

**Important - Update Code to extend the Echo Bot template to use Cosmos DB and store Session State.**
 - The sample Echo Bot template code used from Step 1 do not use any session data preservation.   
 - Please refer the source code of this repository to use the required changes to configure Cosmos DB in code.
 - This step preserves the data provided by the user via DLS on each Turn and saves the input to Cosmos DB.

## Key Source Code Excerpts:

Create a Cosmos DB connection using the instantiation below.
``` Csharp
_myStorage = new CosmosDbStorage(new CosmosDbStorageOptions
{
AuthKey = CosmosDBKey,
CollectionId = CosmosDBCollectionName,
CosmosDBEndpoint = new Uri(CosmosServiceEndpoint),
DatabaseId = CosmosDBDatabaseName,
});
```
Below is a sample line of code to write changes to the Cosmos DB using .Net TPL. 'changes' object is a Dictionary that keeps track of the user inputs over each Turn.
``` CSharp
// Save the user message to your Storage.
await _myStorage.WriteAsync(changes, cancellationToken);
```

## Quick Test:

 Deploy the solution from Visual Studio to the Azure Echo Bot Web App Bot.   
 -  Perform tests using the 'Web Chat' under Bot Management. 
 - Navigate to   Cosmos DB Container to validate entry saved as part of the 'Web Chat'   conversation.

## Stage 3: Install Bot Framework Emulator

Download and Install the [Bot Framework Emulator](https://github.com/microsoft/BotFramework-Emulator/releases/tag/v4.5.1).

 At this point the Echo bot solution can be tested using the Bot
    Framework Emulator.  
   1. Run the Echo bot solution in Visual Studio and copy the Echo bot localhost URL endpoint shown in the browser window where the bot framework service is listening. e.g. "http://localhost:port/api/messages"
   
   2. Run the Bot Framework Emulator and paste the url and leave other fields blank. 
   
   3. For troubleshooting errors, please read this [article](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-troubleshoot-authentication-problems?view=azure-bot-service-4.0#step-1-disable-security-and-test-on-localhost).

   4. If you have downloaded the source code of the bot from Azure portal in Step 1, then the appsettings.json will have the Azure bot MicrosoftAppId and MicrosoftAppPassword added.
   - If you prefer this way of testing, then ensure to use that ID and Password in step 2 above.
   - To copy the values later, you could navigate to the Bot Web App Resource in Azure, Naviagte to 'Settings -> Configuration' and copy the 'MicrosoftAppId' and 'MicrosoftAppPassword'

   5. Ignore the https endpoint error shown in the console output window as this local testing is scoped to http endpoint.
	

## Stage 4: Create and Configure LUIS
Create a basic LUIS (Language Understanding Intelligent Service) app following the Microsoft [article](https://docs.microsoft.com/en-gb/azure/cognitive-services/luis/luis-get-started-create-app). 

 - Try steps up to testing the endpoint with a GET https request. 
 - In this solution, we will reuse the 'Entities' of the 'Home Automation' prebuilt domain of LUIS. 
 - Export of Utterances, Entities and Intents used for this solution is added to the source repo.
 - Utterances and Intents of 'Home Automation' prebuilt domain was deleted.
 - Import the provided luis.json file and upload via the LUIS portal [Manage link](https://www.luis.ai/applications/{yourapplicationidhere}/versions/0.1/manage/versions). (Ensure to update the link with your Application ID)
 - Train the App and Test with some Utterances. 
  	- E.g. 1: "Get all blue bikes" - This should return 'BikeMike.GetLatestBikes' as the Top Scoring Intent.
  	- E.g. 2: "Show list of beans" - This should return 'None' as the Top Scoring Intent.
  	- Adding Utterances to Intent 'None' is important and should constitute 10-20% of the total Utterances authored.
- Curating the Utterances, Entities and Intents is important to improve accuracy.
- Publish the App as the final step.

**Configure appsettings.json key values for LUISSettings.**

- Copy the end point url and update the appsettings.json file 'luisEndPoint' key. For reference later, naviagate to 'https://www.luis.ai/applications/{PasteLUISappIDhere}/versions/0.1/manage/endpoints'
## Key Source Code Excerpts:

Below lines of code will fetch the LUIS response based on the user input utterance. Response is managed using dynamic objects. Strongly typed objects based on custom LUIS response Intents and Entities can also be implemented for flexibility.
``` csharp
private async Task<DocumentSearchResult<dynamic>> ProcessUserUtterance(string utterance)
{
dynamic LuisResponse;
var LuisIntents = new List<dynamic>();
var LuisEntities = new List<dynamic>();
var Luisquery = string.Empty;
dynamic LuisTopScoringIntent;

var response = FecthLUISResponseAsync(luisEndPoint + utterance);
```
Below lines of code first deserialize the LUIS response to list of dynamic objects using the Newtonsoft.json nuget package. Based on the Intent returned from LUIS, the corresponding function is called. In the below case, the intent is to search for latest bikes.
``` csharp
LuisIntents = (LuisResponse.intents as JArray).ToList<dynamic>();
LuisEntities = (LuisResponse.entities as JArray).ToList<dynamic>();
var SearchEntities = new List<dynamic>();

if (LuisTopScoringIntent.intent == "BikeMike.GetLatestBikes")
{
return await AzureSearchQueryRequest(LuisEntities);
}
``` 
## Stage 5: Create and Configure SQL Database
Create an Azure Single SQL Database using the Microsoft [link](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-single-database-get-started?tabs=azure-portal).

 - Use a 'Basic' tier and 100 MB storage as this is sufficient for this POC. For DataSource, use 'None'.
   
 - Complete rest of the steps up to 10.
   
 - In the Query the database step 3, copy paste the 'bikes.sql' contents file provided as part of this source repo and 'Run'.
 - The Bikes SQL Table with sample data should be populated.
  - A document database like Cosmos DB could be a better alternative. 
  - Integration with Azure search index schema is better with Cosmos DB as both services are in the direction of Schema-less.
  - If a Normalized SQL DB is used, then a View can be used joining the required tables and then integrate the view with Azure Search.

## Stage 6: Create and Configure Azure Search
Create and configure the Azure Search Service using the Microsoft [link](https://docs.microsoft.com/en-us/azure/search/search-create-service-portal).

 - Follow all the instructions provided in the link. Choose 'Free' tier
   instead of 'Standard' for this POC.
   
 -   Now create an Indexer to load data into the search service index.
   Follow instructions from
   [link](https://docs.microsoft.com/en-us/azure/search/search-get-started-portal).
  
 -  In Step 1, point 2 from the above link, ensure to select "SQL
   Database" as the Data Source instead of "Samples". 
   
 - Rest of the   configurations can be followed. Connect to the Database that was   created for this POC in Stage 4.
 - In Step 3 from the above link, you can use the [IndexSchema.png](https://github.com/Sanoobk/Azure-DirectLineSpeech-Search-LUIS-Bot-Solution/blob/master/Images/IndexSchema.PNG) image to configure the index. 
	 - Once defined, many field attributes cannot be changed via Portal and API's.
	 - Copy of the Index Definition JSON file is also added in the source repo for reference.	 
 - In Step 4 create the Indexer and configure a 'Once' schedule.
 - Navigate to the 'Search Explorer' and try the default search with the pre-filled input in the Request	URL. This should now return all the values from the index.
 - For reference on queryType 'full' which processes Lucene query syntax, check the [link](https://docs.microsoft.com/en-us/azure/search/search-query-lucene-examples).
 
 **Configure appsettings.json key values for AzureSearchSettings.**
	 
 - Navigate to the 'Setting -> Keys' and copy paste the Primary Admin Key to the appsettings.json 'queryApiKey'.
 - Copy and paste the Search Service Name from 'Settings -> Properties -> Name' to the appsettings.json searchServiceName key.
 - Copy and paste the Index name from 'Overview -> Indexes -> Name' to the appsettings.json 'indexName' key.

## Key Source Code Excerpts:

 - Below code uses the searchServiceName, indexName and queryApiKey from the appsettings.json file to create an instance of the search index client.
 ``` csharp 
//Create a search index client object to interact with Azure Search Service.
indexClient = new SearchIndexClient(searchServiceName, indexName,new SearchCredentials(queryApiKey)); 
```

- The below code uses a Switch case construct to update the filter parameter based on the LUIS entity passed as a parameter to this function. 

- Entity creation in LUIS and its corresponding Search query should be well thought of. For complex queries, helper and mapper classes need to be added. 

- Using Cognitive Services Skillsets in Search index pipeline could be better alternative to reduce developing queries based on LUIS intents.

``` csharp
private async Task<DocumentSearchResult<dynamic>> AzureSearchQueryRequest(List<dynamic> luisEntities)
{
----skipped----
Switch (entity.type.ToString())
{
case "BikeMike.Color":

filter += filter.Length > 0 ? ($" AND Color eq '{entity.resolution.values[0].ToString()}'") : ($"Color eq '{entity.resolution.values[0].ToString()}'");

break;

case "BikeMike.BikeTypes":
----skipped----
}
```
The Search Parameters and query (*) is then passed on to the index client for async execution to return a list of dynamic objects.
``` csharp 
searchParameters = new SearchParameters()
{
Filter = filter,
Select = new[] { "BikeName", "BaseRate", "Color" }
};
searchResults = (await indexClient.Documents.SearchAsync<dynamic>("*", searchParameters));
```
## Stage 7: Deployment

 - To deploy the Bot into Azure, please refer the   [link](https://docs.microsoft.com/en-gb/azure/bot-service/bot-builder-deploy-az-cli?view=azure-bot-service-4.0&tabs=csharp).
  - For this POC we have already created all resources shown in step 1 to   4 in the link above.  
 - Fast forward to step 5 to deploy the code to Azure.

## Stage 8: Testing

 - To test text based conversation with the bot, use the Azure Web Chat feature under Bot Management. 
 - Testing from virtual machine using Bot Emulator to Azure Bot is also possible for text based conversations by downloading a tunneling software like [ngrok](https://github.com/Microsoft/BotFramework-Emulator/wiki/Tunneling-%28ngrok%29).
 - To test Direct Line Speech plugged voice based conversation with the bot, follow the steps below to download and install a UWP client application that used the DLS channel.
	 - Download or clone the repo from the [link](https://github.com/Azure-Samples/Cognitive-Services-Direct-Line-Speech-Client)  to a machine that has a microphone input.
	 - Follow the instructions from the Readme file of the repo
	 - Use the Microphone button to start the voice based conversation with the bot.

## Solution Implementation is complete.

## References

 - [Solution Design Concept](https://azure.microsoft.com/en-in/solutions/architecture/interactive-voice-response-bot/?cdn=disable)
 - [Bot Builder Samples](https://github.com/microsoft/BotBuilder-Samples)
 - [Create Bot Web App in Azure](https://docs.microsoft.com/en-gb/azure/bot-service/abs-quickstart?view=azure-bot-service-4.0)
 - [Store conversation session state in Cosmos DB](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-v4-storage?view=azure-bot-service-4.0&tabs=csharp)
 - [Bot Framework Emulator](https://github.com/microsoft/BotFramework-Emulator/releases/tag/v4.5.1)
 - [Create and configure Azure Search service](https://docs.microsoft.com/en-us/azure/search/search-create-service-portal)
 - [Create and configure LUIS](https://docs.microsoft.com/en-gb/azure/cognitive-services/luis/luis-get-started-create-app)
 - [Index Azure SQL DB with Azure Search service Indexers](https://docs.microsoft.com/en-us/azure/search/search-create-service-portal)
 -  [Choose a Bot Template](https://marketplace.visualstudio.com/items?itemName=BotBuilder.botbuilderv4)
 - [LUIS - Azure Search Integration](https://azure.microsoft.com/en-us/resources/videos/learnai-creating-intelligent-applications-part1-2-azure-search-and-luis-part3/)
 - [Lucene Query Examples](https://docs.microsoft.com/en-us/azure/search/search-query-lucene-examples)
 - [Direct Line Speech Client](https://github.com/Azure-Samples/Cognitive-Services-Direct-Line-Speech-Client)
 - Bot Builder Dialogs Architecture - Link [1](https://github.com/microsoft/botbuilder-dotnet/tree/master/libraries/Microsoft.Bot.Builder.Dialogs), [2](https://github.com/Microsoft/BotBuilder-Samples/tree/v3-sdk-samples/CSharp/demo-Search/RealEstateBot), [3](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-dialog-manage-conversation-flow?view=azure-bot-service-4.0&tabs=csharp)

License
----

MIT