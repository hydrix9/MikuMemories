using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Linq.Expressions;
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

        //used to execute things on the main thread
        public static SemaphoreSlim MainThreadSemaphore = new SemaphoreSlim(1, 1);

        public static string characterName;
        public static CharacterCard characterCard;

        public static bool logInputSteps = true;

        // Set a timeout in milliseconds for mongo 
        const int timeoutMs = 5000; 


        static async Task Main(string[] args)
        {


            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.log");
            var dualWriter = new DualWriter(logFilePath);
            Console.SetOut(dualWriter);
            Console.SetError(dualWriter);

            new Config(); //create instance
            new Mongo(); //create instance
            new LlmApi(); //create instance
            new PythonInterop(); //create instance

            string summary = await PythonInterop.GenerateSummary("test", 0.5);
            Console.WriteLine(summary);

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
                Console.WriteLine("\nCtrl+C pressed. Exiting...");
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

            characterName = characterCard.name;

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

            Console.WriteLine("Enabling chat interface");

            Chat(cts);

            //run queue loop for LLM operations
            var rdrtr_timer = new System.Timers.Timer(5); // Set the interval to 5ms
            string startingContext = GenerateContext(characterCard, true);
            rdrtr_timer.Elapsed += async (sender, e) => await ReadDatabaseResponsesTryRespond(startingContext);
            rdrtr_timer.Start();


            //run queue loop for LLM operations
            var llmApi_timer = new System.Timers.Timer(5); // Set the interval to 5ms
            llmApi_timer.Elapsed += async (sender, e) => await LlmApi.instance.TryProcessQueue();
            llmApi_timer.Start();
            

            //timer.Stop(); ///not sure where to put this...

        }

        //show error when async method fails
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled exception in async method:");
            Console.WriteLine(e.Exception);
            Console.WriteLine("exiting environment");
            Environment.Exit(0); // exit the process with exit code 0
            //e.SetObserved(false); // Allow the exception to propagate and crash the application
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


            //print latest messages
            var latestMessages = await Mongo.instance.GetLatestMessagesFromUserResponses(25);
            foreach (var message in latestMessages)
            {
                Console.WriteLine($"{message.UserName}: {message.Text}");
            }


            Console.WriteLine($"{characterCard.name} has entered the chat.");


            //TODO: create a more broad character that can develop and is stored in the database rather than the character card


            var processUserInputTask = Task.Run(() => ProcessUserInput(userName, cts));

            try
            {
                await Task.WhenAny(processUserInputTask);
            }
            catch (OperationCanceledException)
            {

            }

            try
            {
                var input = Console.ReadLine();

                // Execute the main logic of your program here
                while (!cts.Token.IsCancellationRequested)
                {
                    await ProcessUserInput(userName, cts);
                }

                Console.WriteLine("Shutting down gracefully...");
            }
            catch (OperationCanceledException)
            {
                // Handle any cleanup required on cancellation, if necessary
            }
            finally
            {

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

        public static async Task InsertResponseAsync(IMongoCollection<Response> collection, Response response)
        {
            if(logInputSteps) Console.WriteLine("InsertResponseAsync: Trying to insert the user response...");

            try
            {
                var insertTask = collection.InsertOneAsync(response);
                const int timeoutMs = 5000;

                if (await Task.WhenAny(insertTask, Task.Delay(timeoutMs)) == insertTask)
                {
                    await insertTask;
                    if(logInputSteps) Console.WriteLine("InsertResponseAsync: User response inserted.");
                }
                else
                {
                    Console.WriteLine("InsertResponseAsync: Insert operation timed out.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InsertResponseAsync: Error: " + ex.Message);
            }
        }

        private static async Task<string> CompileRecentResponsesAsync_Single(IMongoCollection<Response> responsesCollection, int n)
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
            try
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
                        if(logInputSteps) Console.WriteLine($"No summaries found for length {length}");
                    }
                }

                return summaries;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetSummaries(): " + ex.Message);
                return new List<Summary>(); // Return an empty list instead of a null value
            }
        }


        private static string CompileFullContext(string baseContext, string recentResponses, string summaries)
        {
            /*
            Continue the dialogue below by generating the next line for the character {characterName}. Your response should reflect {characterName}'s personality and speech quirks while considering the context of the conversation. Be sure to prefix the response with \"{characterName}:\" (e.g., \"{characterName}: Hello!\") Feel free to include actions or reactions to make the response more engaging.
            (character attributes here)
            ### Conversation So Far:
            (conversation so far)
            ### Note:
            In your response, you may incorporate actions or reactions (e.g., *moves closer to you*, *blushes*, *spins around while singing*) to make the dialogue more engaging and dynamic.
            ### Summaries of Previous Conversation and Events:
            (summaries)
            ### Actions:
            Describe any actions the character might perform during this interaction, using a format like "{actionType}:{parameters}" (e.g., "MoveAvatar:forward,5", "SearchOnline:best pizza places"). You can chain multiple actions by separating them with semicolons.
            ### Response:
            */

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Continue the dialogue below by generating the next line for the character {characterName}. Your response should reflect {characterName}'s personality and speech quirks while considering the context of the conversation. Be sure to prefix the response with \"{characterName}:\" (e.g., \"{characterName}: Hello!\") Feel free to include actions or reactions to make the response more engaging.");
            sb.AppendLine(baseContext);
            sb.AppendLine(); // Adds an extra newline
            sb.AppendLine("### Conversation So Far:");
            sb.AppendLine(recentResponses);
            sb.AppendLine(); // Adds an extra newline
            sb.AppendLine("### Note:");
            sb.AppendLine("In your response, you may incorporate actions or reactions (e.g., *moves closer to you*, *blushes*, *spins around while singing*) to make the dialogue more engaging and dynamic.");
            sb.AppendLine(); // Adds an extra newline            
            if(summaries.Length > 0) {
                sb.AppendLine("### Summaries of Previous Conversation and Events:");
                sb.AppendLine(summaries);
            }
            sb.AppendLine("### Response:");

            return sb.ToString();

        }

        public static async Task TrySummarize()
        {
            try {
                var mongo = Mongo.instance;
                int[] summaryLengths = Config.GetSummaryLengths();

                foreach (int length in summaryLengths)
                {

                    var latestSummary = await Mongo.instance.GetLatestSummary(characterName, length);
                    var latestResponses = await Mongo.instance.GetLatestMessagesFromUserResponses(length);
                    
                    int messagesSinceLastSummary;

                    //either find number since last summary or simply use the number of last messages
                    if(latestSummary != null) 
                        messagesSinceLastSummary = latestResponses.Count(r => r.Timestamp > latestSummary.Timestamp);
                    else
                        messagesSinceLastSummary = latestResponses.Count;

                    bool shouldGenerateSummary = true;
                    if (latestSummary != null)
                    {

                        if (messagesSinceLastSummary >= length)
                        {
                            shouldGenerateSummary = true;
                        }
                    } else {
                        //latest summary of length doesn't exist
                        if(messagesSinceLastSummary >= length)
                            shouldGenerateSummary = true;

                    }

                    if (shouldGenerateSummary)
                    {
                        var latestMessagesRaw = await mongo.GetLatestMessagesFromUserResponses(length);
                        StringBuilder latestMessages = new StringBuilder();
                        foreach (var response in latestMessagesRaw)
                        {
                            latestMessages.AppendLine($"{response.UserName}: {response.Text}");
                        }

                        string latestMessagesFinal = latestMessages.ToString();
                        double ratio = Tools.CalculateSummaryRatio(length);

                        string summaryText = await PythonInterop.GenerateSummary(latestMessagesFinal, ratio);

                        var summary = new Summary { SummaryLength = length, Text = summaryText, Timestamp = DateTime.UtcNow };
                        await mongo.GetSummariesCollection(characterName).InsertOneAsync(summary);
                    }
                }
            } catch(Exception ex) {
                Console.WriteLine("Exception in TrySummarize: " + ex.Message);
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
                if(!string.IsNullOrEmpty(characterCard.world_scenario) && characterCard.world_scenario != "")
                    contextBuilder.AppendLine($"World Scenario: {characterCard.world_scenario}");
                if(!string.IsNullOrEmpty(characterCard.char_greeting) && characterCard.char_greeting != "")
                    contextBuilder.AppendLine($"Character Greeting: {characterCard.char_greeting}");
            }

            return contextBuilder.ToString();
        }


        static async Task ProcessUserInput(string userName, CancellationTokenSource cts)
        {
            StringBuilder inputBuilder = new StringBuilder();

            // Get responses collection and recent responses.
            var responsesCollection = Mongo.instance.GetResponsesCollection(userName);

            // Get user input and append it to recentResponses.
            string input = string.Empty;

            while (!cts.Token.IsCancellationRequested)
            {
                int initialLeft = Console.CursorLeft;
                int initialTop = Console.CursorTop;

                input = Console.ReadLine();
                if (!string.IsNullOrEmpty(input))
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.Write(userName + ": " + input + Environment.NewLine);
                }

                
                Response userResponse = new Response { UserName = userName, Text = input, Timestamp = System.DateTime.UtcNow };

                try
                {
                    if(logInputSteps) Console.WriteLine("Starting InsertResponseAsync...");
                    try
                    {
                        var insertResponseTask = InsertResponseAsync(responsesCollection, userResponse);
                        if (await Task.WhenAny(insertResponseTask, Task.Delay(timeoutMs)) == insertResponseTask)
                        {
                            await insertResponseTask;
                            if(logInputSteps) Console.WriteLine("InsertResponseAsync completed.");
                        }
                        else
                        {
                            Console.WriteLine("InsertResponseAsync timed out.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in InsertResponseAsync: " + ex.Message);
                    }

                } catch (Exception ex)
                {
                    Console.WriteLine("Error in ProcessUserInput: " + ex.Message);
                }
            }
        }

        private static SemaphoreSlim rdrtr_functionLock = new SemaphoreSlim(1, 1);

        private static async Task ReadDatabaseResponsesTryRespond(string startingContext = null) {
            if (!await rdrtr_functionLock.WaitAsync(0)) {
                // If the function is already running, exit immediately.
                return;
            }

            try {
                //don't do anything if we've already sent a request to get our response 
                if(LlmApi.requestQueue.Any(entry => entry.type == LLmApiRequest.Type.Response && entry.author == characterName)) {
                    //Console.WriteLine("current queue: " +  LlmApi.requestQueue.Count);
                    return;
                }


                // Generate continuingContext if startingContext is not specified
                if (startingContext == null)
                {
                    startingContext = GenerateContext(characterCard, false);
                }


                // Get responses collection and recent responses.
                //var responsesCollection = Mongo.instance.GetResponsesCollection(userName);

                List<Response> recentResponses;
                StringBuilder recentResponsesText = new StringBuilder();

                Response latestResponse = await Mongo.GetLatestResponseFromAllAsync();
                //if the response is from our character, nothing to do, waiting for others to respond
                if(latestResponse != null  && latestResponse.UserName == characterName) {
                    return;
                }

                if(logInputSteps) Console.WriteLine("Starting CompileRecentResponsesAsync...");
                try
                {
                    var compileRecentResponsesTask = Mongo.instance.GetLatestMessagesFromUserResponses(int.Parse(Config.GetValue("numRecentResponses")));
                    if (await Task.WhenAny(compileRecentResponsesTask, Task.Delay(timeoutMs)) == compileRecentResponsesTask)
                    {

                        recentResponses = await compileRecentResponsesTask;
                        foreach (var response in recentResponses)
                        {
                            recentResponsesText.AppendLine($"{response.UserName}: {response.Text}");
                        }


                        if(logInputSteps) Console.WriteLine("CompileRecentResponsesAsync completed.");
                    }
                    else
                    {
                        Console.WriteLine("CompileRecentResponsesAsync timed out.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in CompileRecentResponsesAsync: " + ex.Message);
                }

                if(logInputSteps) Console.WriteLine("Starting GetSummaries and Context Compile...");

                try
                {

                    var getSummariesTask = GetSummaries();
                    string fullContext = "";
                    
                    if (await Task.WhenAny(getSummariesTask, Task.Delay(timeoutMs)) == getSummariesTask)
                    {

                        IEnumerable<Summary> summariesRaw = await getSummariesTask;

                        string summaries = string.Join(Environment.NewLine, summariesRaw.Select(entry => entry.Text));

                        // Get the compiled responses.
                        fullContext = CompileFullContext(startingContext, recentResponsesText.ToString(), summaries);

                        if(logInputSteps) Console.WriteLine("GetSummaries and Context Compile completed.");
                    }
                    else
                    {
                        Console.WriteLine("GetSummaries timed out.");
                    }


                    // Add request to be sent later in a queue
                    var request = new LLmApiRequest(fullContext, LlmInputParams.defaultParams, LLmApiRequest.Type.Response, characterName);
                    LlmApi.QueueRequest(request);
 

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in GetSummaries or QueueRequest: " + ex.Message);
                }
            } catch(Exception ex) {
                Console.WriteLine("Error in ReadDatabaseResponsesTryRespond: " + ex.Message);
            }
            finally {
                    // Release the lock.
                    rdrtr_functionLock.Release();
            }
        }

    } //end class Main

}
