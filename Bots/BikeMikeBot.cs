// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class BikeMikeBot : ActivityHandler
    {
        private static string CosmosServiceEndpoint = "";
        private static string CosmosDBKey = "";
        private static string CosmosDBDatabaseName = "";
        private static string CosmosDBCollectionName = "";
        private static string luisEndPoint = "";
        private static string searchServiceName = "";
        private static string queryApiKey = "";
        private static string indexName = "";

        private IConfigurationRoot Configuration { get; }
        private static CosmosDbStorage _myStorage;
        private static SearchIndexClient indexClient;


        public BikeMikeBot(IConfiguration config)
        {
            Configuration = config as IConfigurationRoot;
            Load();
        }

        private void Load()
        {
            //Set all service settings from config files.
            CosmosDBCollectionName = Configuration.GetValue<string>("CosmosDBSettings:CosmosDBCollectionName");
            CosmosServiceEndpoint = Configuration.GetValue<string>("CosmosDBSettings:CosmosServiceEndpoint");
            CosmosDBDatabaseName = Configuration.GetValue<string>("CosmosDBSettings:CosmosDBDatabaseName");
            CosmosDBKey = Configuration.GetValue<string>("CosmosDBSettings:CosmosDBKey");
            searchServiceName = Configuration.GetValue<string>("AzureSearchSettings:searchServiceName");
            queryApiKey = Configuration.GetValue<string>("AzureSearchSettings:queryApiKey");
            indexName = Configuration.GetValue<string>("AzureSearchSettings:indexName");
            luisEndPoint = Configuration.GetValue<string>("LuisSettings:luisEndPoint");

            //Open connection with CosmosDB.
            _myStorage = new CosmosDbStorage(new CosmosDbStorageOptions
            {
                AuthKey = CosmosDBKey,
                CollectionId = CosmosDBCollectionName,
                CosmosDBEndpoint = new Uri(CosmosServiceEndpoint),
                DatabaseId = CosmosDBDatabaseName,
            });

            //Create a search index client object to interact with Azure Search Service.
            indexClient = new SearchIndexClient(
              searchServiceName,
              indexName,
              new SearchCredentials(queryApiKey));
        }


        // Create cancellation token (used by Async Write operation).
        public CancellationToken cancellationToken { get; private set; }

        // Class for storing a log of utterances (text of messages) as a list.
        public class UtteranceLog : IStoreItem
        {
            // A list of things that users have said to the bot
            public List<string> UtteranceList { get; } = new List<string>();

            // The number of conversational turns that have occurred        
            public int TurnNumber { get; set; } = 0;

            // Create concurrency control where this is used.
            public string ETag { get; set; } = "*";
        }


        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // preserve user input.
            var utterance = turnContext.Activity.Text;
            // make empty local logitems list.
            UtteranceLog logItems = null;
            // Object to help in manipulating the search results object.
            StringBuilder searchresults = new StringBuilder();

            // see if there are previous messages saved in storage.
            try
            {
                string[] utteranceList = { "UtteranceLog" };
                logItems = _myStorage.ReadAsync<UtteranceLog>(utteranceList).Result?.FirstOrDefault().Value;
            }
            catch
            {
                // Inform the user an error occured.
                await turnContext.SendActivityAsync(
                     Speak($"Echo: Sorry, something went wrong reading your stored messages!"), cancellationToken);
            }

            // If no stored messages were found, create and store a new entry.
            if (logItems is null)
            {
                // add the current utterance to a new object.
                logItems = new UtteranceLog();
                logItems.UtteranceList.Add(utterance);
                // set initial turn counter to 1.
                logItems.TurnNumber++;

                await turnContext.SendActivityAsync(Speak($"Echo: {turnContext.Activity.Text}"), cancellationToken);

                // Create Dictionary object to hold received user messages.
                var changes = new Dictionary<string, object>();
                {
                    changes.Add("UtteranceLog", logItems);
                }
                try
                {
                    // Save the user message to your Storage.
                    await _myStorage.WriteAsync(changes, cancellationToken);
                }
                catch
                {
                    // Inform the user an error occured.
                    ProcessSpeechForResults(turnContext,
                       "Echo: Sorry, something went wrong reading your stored messages!", cancellationToken);
                }
            }
            // Else, our Storage already contained saved user messages, add new one to the list.
            else
            {
                // add new message to list of messages to display.
                logItems.UtteranceList.Add(utterance);
                // increment turn counter.
                logItems.TurnNumber++;

                //Pass the utterance to the LUIS endpoint and furthur to Azure Search for processing.
                DocumentSearchResult<dynamic> searchResults = await ProcessUserUtterance(utterance);

                //After LUIS and Azure Search processing, provide results if any back to the Bot.

                if (searchResults != null)
                {
                    if (searchResults.Results.Count > 0)
                    {
                        searchResults.Results.ToList<SearchResult<dynamic>>()
                        .ForEach(x =>
                        {
                            searchresults.Append("Bike " + x.Document["BikeName"].ToString() + " with price "
                                + x.Document["BaseRate"].ToString() + " Dollars and Color "
                                + x.Document["Color"].ToString() + ". ");
                        }
                    );
                        ProcessSpeechForResults(turnContext,
                           searchresults.ToString(), cancellationToken);
                    }
                    else
                        ProcessSpeechForResults(turnContext,
                            "Echo: Sorry, unable to find any bikes. Please try a different query.", cancellationToken);
                }
                else
                    ProcessSpeechForResults(turnContext,
                        "Echo: Sorry, unable to find any bikes. Please try a different query.", cancellationToken);

                // Create Dictionary object to hold new list of messages.
                var changes = new Dictionary<string, object>();
                {
                    changes.Add("UtteranceLog", logItems);
                };

                try
                {
                    // Save new list to your Storage.
                    await _myStorage.WriteAsync(changes, cancellationToken);
                }
                catch
                {
                    // Inform the user an error occured.
                    ProcessSpeechForResults(turnContext,
                        "Echo: Sorry, something went wrong reading your stored messages!", cancellationToken);
                }
            }
        }

        private async void ProcessSpeechForResults(ITurnContext<IMessageActivity> turnContext, string message, CancellationToken cancellationToken)
        {
            //Speech property needed to be set for the Bot to read out to the user for voice based conversation.
            await turnContext.SendActivityAsync(
                   Speak($"{message}"), cancellationToken);
        }

        /// <summary>
        /// Funcion to process the user utterance by passing the Azure Speech Service response to LUIS.
        /// The response from LUIS is furthur processed to find the Intent of the user and then invoke the function to search or order bikes.
        /// </summary>
        /// <param name="utterance"></param>
        /// <returns>Azure Search Query Results as a list of dynamic objects.</returns>
        private async Task<DocumentSearchResult<dynamic>> ProcessUserUtterance(string utterance)
        {
            dynamic LuisResponse;
            var LuisIntents = new List<dynamic>();
            var LuisEntities = new List<dynamic>();
            dynamic LuisTopScoringIntent;

            var response = FecthLUISResponseAsync(luisEndPoint + utterance);

            if (response != null)
            {
                //Deserialize using Newtonsoft.json
                LuisResponse = JsonConvert.DeserializeObject<dynamic>(response.Result);

                //fetch the Top Scoring Intent
                LuisTopScoringIntent = LuisResponse.topScoringIntent;

                //Convert from Newtonsoft JArray
                LuisIntents = (LuisResponse.intents as JArray).ToList<dynamic>();
                LuisEntities = (LuisResponse.entities as JArray).ToList<dynamic>();

                var SearchEntities = new List<dynamic>();

                //Based on the Intent from LUIS, invoke the function.
                switch (LuisTopScoringIntent.intent)
                {
                    case "BikeMike.GetLatestBikes":
                        return await AzureSearchQueryRequest(LuisEntities);
                    case "BikeMike.OrderBike":
                        return null;
                    default:
                        return null;
                }
            }

            return null;
        }

        private async Task<DocumentSearchResult<dynamic>> AzureSearchQueryRequest(List<dynamic> luisEntities)
        {
            // Configure the search service and establish a connection.

            SearchParameters searchParameters;
            DocumentSearchResult<dynamic> searchResults = null;
            string filter = string.Empty;

            //Logic to build the search filter for the query. For this sample, limiting to only 2 entities. 
            foreach (var entity in luisEntities)
            {
                switch (entity.type.ToString())
                {
                    case "BikeMike.Color":
                        filter += filter.Length > 0 ? ($" AND Color eq '{entity.resolution.values[0].ToString()}'") : ($"Color eq '{entity.resolution.values[0].ToString()}'");
                        break;
                    case "BikeMike.BikeTypes":
                        filter += filter.Length > 0 ? ($" AND Category eq '{entity.resolution.values[0].ToString()}'") : ($"Category eq '{entity.resolution.values[0].ToString()}'");
                        break;
                    default:
                        break;
                }
            }
            try
            {
                searchParameters =
                   new SearchParameters()
                   {
                       Filter = filter,
                       Select = new[] { "BikeName", "BaseRate", "Color" }
                   };

                searchResults = (await indexClient.Documents.SearchAsync<dynamic>("*", searchParameters));
            }
            catch (Exception ex)
            {
            }

            return searchResults;
        }

        /// <summary>
        /// Function to invoke the LUIS enpoint to fetch the Intents, Entities and Top Scoring Intent.
        /// </summary>
        /// <param name="luisEndPoint"></param>
        /// <returns>Response from LUIS in json format.</returns>
        public async Task<string> FecthLUISResponseAsync(string luisEndPoint)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(luisEndPoint);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Based on the Intent from LUIS to Order Bikes, invoke this function to Order a Bike based on values from Entities.
        /// </summary>
        /// <param name="Entities"></param>
        /// <returns>Response from LUIS in json format.</returns>
        public async Task<string> InsertDatatoSQLDB(dynamic Entities)
        {
            //To be implemented for Order Entity from LUIS
            return string.Empty;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(Speak($"Hello and welcome!"), cancellationToken);
                }
            }
        }

        /// <summary>
        /// Function to set the Speak property for the bot to read out user voice based utterances.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Activity object with the Speak property.</returns>
        public IActivity Speak(string message)
        {
            var activity = MessageFactory.Text(message);
            string body = @"<speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
              <voice name='Microsoft Server Speech Text to Speech Voice (en-IE, Sean)'>" +
              $"{message}" + "</voice></speak>";
            activity.Speak = body;
            return activity;
        }
    }
}
