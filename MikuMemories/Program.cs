using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;
using System.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;

namespace MikuMemories
{
    class Program
    {
        
        public static string characterName;
        public static CharacterCard characterCard;

        static async Task Main(string[] args)
        {
            new Config(); //create instance
            new Mongo(); //create instance
            new LlmApi(); //create instance

            string pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python");
            string pythonLibPath = Path.Combine(pythonPath, "Lib");

            Environment.SetEnvironmentVariable("PATH", $"{pythonPath};{Environment.GetEnvironmentVariable("PATH")}");
            Environment.SetEnvironmentVariable("PYTHONHOME", pythonPath);
            Environment.SetEnvironmentVariable("PYTHONPATH", pythonLibPath);

            // Initialize the Python runtime
            PythonEngine.Initialize();

            // Register event handlers for application exit
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Console.CancelKeyPress += OnCancelKeyPress;


            /*
            var request = new LLmApiRequest(LlmInputParams.defaultParams);
            request.prompt = "hello";
            request.@params.max_new_tokens = 5;

            //test request
            Console.WriteLine(request.AsString());
            LlmApi.QueueRequest(request);
            */

            // Prompt the user to enter their name
            Console.WriteLine("Welcome to MikuMemories!");

            Console.WriteLine(" Enabling chat interface");
            Console.Write("Please enter your name: ");
            string userName = Console.ReadLine();

            // Print a welcome message to the user
            Console.WriteLine($"Welcome to the chat, {userName}!");


            await Task.Run(LlmApi.instance.TryProcessQueue);

            string characterCardFilePath = Config.FindCharacterCardFilePath(Config.GetValue("characterCardFileName"));

            if (characterCardFilePath == null)
            {
                throw new Exception("character card " + Config.GetValue("characterCardFileName") + " not found, place in /Characters folder");
            }

            characterCard = Tools.LoadCharacterCardFromFile(characterCardFilePath);

            characterName = characterCard.char_name;

            //TODO: create a more broad character that can develop and is stored in the database rather than the character card

            //TODO: test summarization

            string startingContext = GenerateContext(characterCard, true);

            await ProcessUserInput(userName, startingContext);

            while (true)
            {
                await ProcessUserInput(userName);
            }

        }

        // Event handler for application exit
        private static void OnProcessExit(object sender, EventArgs e)
        {
            PythonEngine.Shutdown();
        }

        // Event handler for keyboard interrupt (e.g., Ctrl+C)
        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            PythonEngine.Shutdown();
            Environment.Exit(0);
        }

        public static async Task<List<Response>> GetRecentResponsesAsync(IMongoCollection<Response> collection, int responseLimit)
        {
            return await collection.Find(_ => true)
                .SortByDescending(r => r.Timestamp)
                .Limit(responseLimit)
                .ToListAsync();
        }

        public static async Task InsertResponseAsync(IMongoCollection<Response> collection, Response response)
        {
            await collection.InsertOneAsync(response);
        }

        private static async Task<string> CompileRecentResponsesAsync(IMongoCollection<Response> responsesCollection, int n)
        {
            var recentResponses = await responsesCollection.Find(_ => true)
                .Sort("{Timestamp: -1}")
                .Limit(n)
                .ToListAsync();

            recentResponses.Reverse();

            StringBuilder contextBuilder = new StringBuilder();

            foreach (var response in recentResponses)
            {
                contextBuilder.AppendLine($"{response.UserName}: {response.Text}");
            }

            return contextBuilder.ToString();
        }

        private static async Task<IEnumerable<Summary>> GetSummaries()
        {
            int[] summaryLengths = Config.GetSummaryLengths();
            var collection = Mongo.instance.GetSummariesCollection(characterName);
            var filterBuilder = Builders<Summary>.Filter;

            var summaries = new List<Summary>();

            foreach (int length in summaryLengths)
            {
                var filter = filterBuilder.Eq("SummaryLength", length);
                var latestSummary = await collection.Find(filter).SortByDescending(s => s.Timestamp).FirstOrDefaultAsync();

                if (latestSummary != null)
                {
                    summaries.Add(latestSummary);
                }
                else
                {
                    Console.WriteLine($"No summaries found for length {length}");
                }
            }

            return summaries;
        }

        private static string CompileFullContext(string baseContext, string recentResponses, string summaries)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(baseContext);
            sb.AppendLine(); // Adds an extra newline
            sb.AppendLine(recentResponses);
            sb.AppendLine(); // Adds an extra newline
            sb.AppendLine(summaries);

            return sb.ToString();

        }

        public static async Task TrySummarize()
        {
            var mongo = Mongo.instance;
            int[] summaryLengths = Config.GetSummaryLengths();

            foreach (int length in summaryLengths)
            {
                var latestSummary = await Mongo.instance.GetLatestSummary(characterName, length);
                bool shouldGenerateSummary = false;

                if (latestSummary != null)
                {
                    var messagesSinceLastSummary = await mongo.GetLatestMessages(characterName, length);
                    shouldGenerateSummary = messagesSinceLastSummary.Count >= length;
                }
                else
                {
                    shouldGenerateSummary = true;
                }

                if (shouldGenerateSummary)
                {
                    var latestMessages = await mongo.GetLatestMessagesFromUserResponses(length);

                    string summaryText = PythonInterop.GenerateSummary(string.Join(Environment.NewLine, latestMessages), Tools.CalculateSummaryRatio(length));

                    var summary = new Summary { SummaryLength = length, Text = summaryText, Timestamp = DateTime.UtcNow };
                    await mongo.GetSummariesCollection(characterName).InsertOneAsync(summary);
                }
            }
        }



        //startConversation will include the world scenario and character greeting
        public static string GenerateContext(CharacterCard characterCard, bool startConversation = true)
        {
            StringBuilder contextBuilder = new StringBuilder();

            contextBuilder.AppendLine($"Name: {characterCard.name}");
            contextBuilder.AppendLine($"Personality: {characterCard.personality}");
            contextBuilder.AppendLine($"Description: {characterCard.description}");

            if (startConversation)
            {
                contextBuilder.AppendLine($"World Scenario: {characterCard.world_scenario}");
                contextBuilder.AppendLine($"Character Greeting: {characterCard.char_greeting}");
            }

            return contextBuilder.ToString();
        }


        static async Task ProcessUserInput(string userName, string startingContext = null)
        {
            // Generate continuingContext if startingContext is not specified
            if (startingContext == null)
            {
                startingContext = GenerateContext(characterCard, false);
            }

            // Get responses collection and recent responses.
            var responsesCollection = Mongo.instance.GetResponsesCollection(userName);

            // Get user input and append it to recentResponses.
            Console.Write($"{userName}: ");
            string userInput = Console.ReadLine();
            Response userResponse = new Response { UserName = userName, Text = userInput, Timestamp = System.DateTime.UtcNow };

            await InsertResponseAsync(responsesCollection, userResponse);
            

            string recentResponses = await CompileRecentResponsesAsync(responsesCollection, int.Parse(Config.GetValue("numRecentResponses")));

            IEnumerable<Summary> summariesRaw = await GetSummaries();
            string summaries = string.Join(Environment.NewLine, summariesRaw.Select(entry => entry.Text));

            // Get the compiled responses.
            string fullContext = CompileFullContext(startingContext, recentResponses, summaries);

            ///add request to be sent later in a queue
            LlmApi.QueueRequest(new LLmApiRequest(fullContext, LlmInputParams.defaultParams));
        }


    } //end class Main

}
