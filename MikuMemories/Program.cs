using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;
using System.IO;

namespace MikuMemories
{
    class Program
    {
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

            int responseLimit = 10;

            await Task.Run(LlmApi.instance.TryProcessQueue);

            while (true)
            {
                await ProcessUserInput(userName, responseLimit);
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

        private static string CompileFullContext(string recentResponses, string summaries)
        {
            return recentResponses + Environment.NewLine + summaries;
        }

        public static async Task TrySummarize(string userName)
        {
            int messageCount = (int)await Mongo.instance.GetResponsesCollection(userName).CountDocumentsAsync(FilterDefinition<Response>.Empty);

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

                string compiledResponses = await CompileRecentResponsesAsync(Mongo.instance.GetResponsesCollection(userName), int.Parse(Config.GetValue("numRecentResponses")));
                string summaryText = PythonInterop.GenerateSummary(compiledResponses, summaryLength);

                var summary = new Summary
                {
                    Text = summaryText,
                    SummaryLength = summaryLength,
                };

                await Mongo.instance.GetSummariesCollection(userName).InsertOneAsync(summary);
            }
        }

        static async Task ProcessUserInput(string userName, int responseLimit)
        {
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
            string fullContext = CompileFullContext(recentResponses, summaries);

            ///add request to be sent later in a queue
            LlmApi.QueueRequest(new LLmApiRequest(fullContext, LlmInputParams.defaultParams));
        }


    } //end class Main

}
