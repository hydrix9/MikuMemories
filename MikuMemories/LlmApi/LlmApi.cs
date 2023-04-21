using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikuMemories
{
    public class LlmApi
    {
        public static LlmApi instance;

        public LlmApi()
        {
            instance = this;
        }

        List<LLmApiRequest> requestQueue = new List<LLmApiRequest>();

        public static void QueueRequest(LLmApiRequest request)
        {
            instance.requestQueue.Add(request);
        }


        public void TryProcessQueue()
        {
            if (requestQueue.Count <= 0)
                return;

            //take from bottom of stack and process
            LLmApiRequest request = requestQueue[0];
            requestQueue.Remove(request);

            string jsonResponse = RestApi.PostRequest(Config.GetValue("llmsrv"), request.AsString());
            JObject parsedResponse = JObject.Parse(jsonResponse);
            JArray data = (JArray)parsedResponse["data"];
            string response = data[0].ToString();

            Console.WriteLine(response);

            request.callback?.Invoke(response); //do whatever with response

        }

    }
}
