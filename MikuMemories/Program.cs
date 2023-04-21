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

            StringBuilder chatContext = new StringBuilder();
            while (true)
            {
                ProcessUserInput(userName, chatContext);
            }

            await Task.Run(LlmApi.instance.TryProcessQueue);
        }

        static void ProcessUserInput(string userName, StringBuilder chatContext)
        {
            Console.Write($"{userName}: ");
            string userInput = Console.ReadLine();
            chatContext.AppendLine($"{userName}: {userInput}");

            // Call the LLM API with the updated context.
            LlmApi.QueueRequest(new LLmApiRequest(chatContext.ToString(), LlmInputParams.defaultParams, (string response) =>
            {
                // Append the LLM's response to the chat context.
                chatContext.AppendLine($"LLM: {response}");

                // Print the LLM's response.
                Console.WriteLine($"LLM: {response}");
            }));
        }

    } //end class Main

}
