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

            //TODO: only run initial conversation if there is no chat history
            //TODO: create a more broad character that can develop and is stored in the database rather than the character card

            //TODO: fix all instances of userName and make sure it uses characterName where appropriate instead
            //TODO: make sure not generating summaries for USER

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

        private static async Task<IEnumerable<Summary>> GetSummaries(string userName)
        {
            return await Mongo.instance.GetSummariesCollection(userName).Find(_ => true).ToListAsync();
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

        public static async Task TrySummarize(string userName)
        {
            var characterResponseCollections = await Mongo.instance.GetCharacterResponseCollections();
            var allResponses = new List<Response>();

            foreach (var collection in characterResponseCollections)
            {
                var responses = await collection.Find(_ => true).ToListAsync();
                allResponses.AddRange(responses.Select(responseBson => BsonSerializer.Deserialize<Response>(responseBson)));
            }

            int messageCount = allResponses.Count;

            if (messageCount == 0)
            {
                Console.WriteLine("No responses found.");
                return;
            }

            // Replace these values with your desired message count thresholds for different summary lengths
            int shortSummaryThreshold = 10;
            int mediumSummaryThreshold = 30;
            int longSummaryThreshold = 100;

            if (messageCount % shortSummaryThreshold == 0 || messageCount % mediumSummaryThreshold == 0 || messageCount % longSummaryThreshold == 0)
            {
                int summaryLength = 0;

                if (messageCount % longSummaryThreshold == 0)
                {
                    summaryLength = 3; // Replace 3 with the desired summary length for the long summary
                }
                else if (messageCount % mediumSummaryThreshold == 0)
                {
                    summaryLength = 2; // Replace 2 with the desired summary length for the medium summary
                }
                else
                {
                    summaryLength = 1; // Replace 1 with the desired summary length for the short summary
                }

                //string compiledResponses = await CompileRecentResponsesAsync(Mongo.instance.GetResponsesCollection(userName), int.Parse(Config.GetValue("numRecentResponses")));

                string compiledText = string.Join(Environment.NewLine, allResponses.Select(r => $"{r.UserName}: {r.Text}"));

                var summary = new Summary
                {
                    Text = compiledText,
                    SummaryLength = summaryLength,
                };

                await Mongo.instance.GetSummariesCollection(userName).InsertOneAsync(summary);
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

            IEnumerable<Summary> summariesRaw = await GetSummaries(userName);
            string summaries = string.Join(Environment.NewLine, summariesRaw.Select(entry => entry.Text));

            // Get the compiled responses.
            string fullContext = CompileFullContext(startingContext, recentResponses, summaries);

            ///add request to be sent later in a queue
            LlmApi.QueueRequest(new LLmApiRequest(fullContext, LlmInputParams.defaultParams));
        }


    } //end class Main

}
