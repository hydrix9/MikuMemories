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
            Console.Write("Please enter your name: ");
            string userName = Console.ReadLine();

            // Print a welcome message to the user
            Console.WriteLine($"Welcome to the chat, {userName}!");

            ProcessUserInput(userName);

            await Task.Run(LlmApi.instance.TryProcessQueue);
        }

        static void ProcessUserInput(string userName)
        {
  
        }
    }

}
