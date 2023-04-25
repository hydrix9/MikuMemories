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

        /*
        Below is an instruction that describes a task, paired with an input that provides further context. Write a response that appropriately completes the request.

        ### Instruction:
        {instruction}

        ### Input:
        {input}

        ### Response:


        */



        
        
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

            // Set the PYTHONNET_PYDLL environment variable
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", "/home/io/MikuMemories/MikuMemories/Python/lib/libpython3.so");


            // Initialize the Python runtime
            PythonEngine.Initialize();

            // Register event handlers for application exit
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Console.CancelKeyPress += OnCancelKeyPress;

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevent the process from being terminated immediately
                cts.Cancel();     // Signal the cancellation token to cancel
            };

            string characterCardFileName = null;

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--character" && i + 1 < args.Length)
                {
                    characterCardFileName = args[++i];
                }
                else if (args[i] == "--some-other-flag" && i + 1 < args.Length)
                {
                    // Handle other flag and its argument
                    string someOtherArgument = args[++i];
                    // Process the argument as needed
                }
                else if (!args[i].StartsWith("--") && characterCardFileName == null)
                {
                    characterCardFileName = args[i];
                }
            }

            Console.WriteLine($"characterCardFileName: {characterCardFileName}");

            // Check if a suitable file path argument was provided
            if (characterCardFileName != null)
            {
                try
                {
                    string characterCardFilePath = Config.FindCharacterCardFilePath(characterCardFileName);
                    characterCard = await CharacterLoader.LoadCharacter(characterCardFilePath);
                    Console.WriteLine("Character card loaded.");
                    //Console.WriteLine(characterCard.ToString());

                    // Use characterCard object as needed
                }
                catch (Exception ex)
                {
                    throw new Exception("Error loading character card: " + ex.Message);
                }
            }
            else
            {
                throw new Exception("Please provide a file path, either as the first non-flag argument or following the --character flag.");
            }


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

            Chat(cts);
        }

        private static async void Chat(CancellationTokenSource cts) {

            string userName = null;
            try
            {
                userName = await GetUserName(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Shutting down gracefully...");
                return;
            }

            if (userName == null)
            {
                Console.WriteLine("No user name provided. Exiting.");
                return;
            }

            // Print a welcome message to the user
            Console.WriteLine($"Welcome to the chat, {userName}!");


            //run queue loop for LLM operations
            Task.Run(LlmApi.instance.TryProcessQueue);


            characterName = characterCard.name;

            Console.WriteLine($"{characterCard.name} has entered the chat.");


            //TODO: create a more broad character that can develop and is stored in the database rather than the character card

            //TODO: test summarization

            string startingContext = GenerateContext(characterCard, true);

            var processUserInputTask = Task.Run(() => ProcessUserInput(userName, startingContext));

            try
            {
                await Task.WhenAny(processUserInputTask);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Shutting down gracefully...");
            }

            try
            {
                var input = Console.ReadLine();

                // Execute the main logic of your program here
                while (!cts.Token.IsCancellationRequested)
                {
                    await ProcessUserInput(userName);
                }

                Console.WriteLine("Shutting down gracefully...");
            }
            catch (OperationCanceledException)
            {
                // Handle any cleanup required on cancellation, if necessary
            }
            finally
            {
                Console.WriteLine("Shutting down gracefully...");
            }
            
        }

        static async Task<string> GetUserName(CancellationToken cancellationToken)
        {
            Console.Write("Please enter your name: ");

            string input = null;
            var inputTask = Task.Run(() =>
            {
                input = Console.ReadLine();
            });

            var completedTask = await Task.WhenAny(inputTask, Task.Delay(Timeout.Infinite, cancellationToken));
            if (completedTask == inputTask)
            {
                return input;
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
        }




        // Event handler for application exit
        private static void OnProcessExit(object sender, EventArgs e)
        {
            PythonEngine.Shutdown();
            Environment.Exit(0);

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
            StringBuilder inputBuilder = new StringBuilder();

            // Generate continuingContext if startingContext is not specified
            if (startingContext == null)
            {
                startingContext = GenerateContext(characterCard, false);
            }

            // Get responses collection and recent responses.
            var responsesCollection = Mongo.instance.GetResponsesCollection(userName);

            // Get user input and append it to recentResponses.

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    string input = inputBuilder.ToString();

                    if (!string.IsNullOrEmpty(input))
                    {

                        Response userResponse = new Response { UserName = userName, Text = input, Timestamp = System.DateTime.UtcNow };

                        await InsertResponseAsync(responsesCollection, userResponse);
                        

                        string recentResponses = await CompileRecentResponsesAsync(responsesCollection, int.Parse(Config.GetValue("numRecentResponses")));

                        IEnumerable<Summary> summariesRaw = await GetSummaries();
                        string summaries = string.Join(Environment.NewLine, summariesRaw.Select(entry => entry.Text));

                        // Get the compiled responses.
                        string fullContext = CompileFullContext(startingContext, recentResponses, summaries);

                        ///add request to be sent later in a queue
                        LlmApi.QueueRequest(new LLmApiRequest(fullContext, LlmInputParams.defaultParams));
                        
                    }
                }
                
                else
                {
                    inputBuilder.Append(keyInfo.KeyChar);
                }
            }
                    
        }


    } //end class Main

}
