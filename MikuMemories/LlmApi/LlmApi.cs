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

        public static async Task<string> QueueRequest(List<Response> chatResponses, LLmApiRequest request)
        {
            // Convert the chatResponses to a chat context string.
            StringBuilder chatContext = new StringBuilder();
            foreach (var response in chatResponses)
            {
                chatContext.AppendLine($"{response.UserName}: {response.Text}");
            }

            instance.requestQueue.Add(request);
        }

        public async Task TryProcessQueue()
        {
            while (true)
            {
                if (requestQueue.Count > 0)
                {

                    //take from bottom of stack and process
                    LLmApiRequest request = requestQueue[0];
                    requestQueue.Remove(request);

                    string jsonResponse = RestApi.PostRequest(Config.GetValue("llmsrv"), request.AsString());
                    JObject parsedResponse = JObject.Parse(jsonResponse);
                    JArray data = (JArray)parsedResponse["data"];
                    string response = data[0].ToString();

                    Console.WriteLine(response);

                    // Split the LLM response into lines and get the last non-empty line
                    var lines = response.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    var lastLine = lines.LastOrDefault();

                    if (string.IsNullOrWhiteSpace(lastLine)) return;

                    // Find the sender based on the start of the line up to the first colon ':'
                    int colonIndex = lastLine.IndexOf(':');
                    if (colonIndex < 0) return;

                    string sender = lastLine.Substring(0, colonIndex).Trim();

                    // Remove the "User:" or "LLM:" part from the last line
                    string text = lastLine.Substring(sender.Length + 1).Trim();

                    var res = new Response
                    {
                        Timestamp = DateTime.UtcNow,
                        Sender = sender,
                        Text = text
                    };

                    // Get or create the user-specific collection and insert the response
                    var userCollection = Mongo.instance.GetUserCollection(sender, "responses");
                    userCollection.InsertOne(res);

                    request.callback?.Invoke(response); //do whatever with response

                }

                await Task.Delay(TimeSpan.FromSeconds(1));

            } //end while
        } //end func TryProcessQueue


    }
}
