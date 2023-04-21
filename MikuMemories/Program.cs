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
        static void Main(string[] args)
        {
            new Config(); //create instance
            new Mongo(); //create instance
            new LlmApi(); //create instance


            var request = new LLmApiRequest(LlmInputParams.defaultParams);
            request.prompt = "hello";
            request.@params.max_new_tokens = 5;

            Console.WriteLine(request.AsString());

            LlmApi.QueueRequest(request);

            while(true)
            {
                LlmApi.instance.TryProcessQueue();
                Thread.Sleep(10);
            }
        }
    }
}
