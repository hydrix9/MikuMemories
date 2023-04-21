using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MikuMemories
{
    class Program
    {
        static async Task Main(string[] args)
        {
            new Config(); //create instance
            new Mongo(); //create instance
            new LlmApi(); //create instance

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

            while (true)
            {
                await ProcessUserInput(userName, responseLimit);
            }

            await Task.Run(LlmApi.instance.TryProcessQueue);
        }

        private static IMongoCollection<Response> GetResponsesCollection()
        {
            // Replace the following with your own MongoDB connection details.
            string connectionString = "mongodb+srv://<username>:<password>@cluster.mongodb.net/database_name?retryWrites=true&w=majority";
            string databaseName = "your_database_name";
            string collectionName = "responses";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<Response>(collectionName);

            return collection;
        }

        private static List<Response> GetRecentResponses(IMongoCollection<Response> collection, int responseLimit)
        {
            return collection.Find(_ => true)
                .SortByDescending(r => r.Timestamp)
                .Limit(responseLimit)
                .ToList();
        }

        private static async Task InsertResponseAsync(IMongoCollection<Response> collection, Response response)
        {
            await collection.InsertOneAsync(response);
        }

        static async Task ProcessUserInput(string userName, int responseLimit)
        {
            // Get responses collection and recent responses.
            var responsesCollection = GetResponsesCollection();
            List<Response> recentResponses = GetRecentResponses(responsesCollection, responseLimit);

            // Get user input and append it to recentResponses.
            Console.Write($"{userName}: ");
            string userInput = Console.ReadLine();
            Response userResponse = new Response { UserName = userName, Text = userInput, Timestamp = System.DateTime.UtcNow };
            recentResponses.Add(userResponse);

            // Insert the user's response into the database.
            await InsertResponseAsync(responsesCollection, userResponse);

            

            string llmResponseText = await LlmApi.QueueRequest(recentResponses, new LLmApiRequest(newRequest, LlmInputParams.defaultParams));

            // Create an LLM response object.
            Response llmResponse = new Response { UserName = "LLM", Text = llmResponseText, Timestamp = System.DateTime.UtcNow };

            // Print the LLM's response.
            Console.WriteLine($"LLM: {llmResponseText}");

            // Insert the LLM's response into the database.
            await InsertResponseAsync(responsesCollection, llmResponse);
        }


    } //end class Main

}
