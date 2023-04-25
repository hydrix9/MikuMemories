using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MikuMemories
{
    public class LlmApi
    {
        public static LlmApi instance;

        public LlmApi()
        {
            instance = this;
            requestQueue = Channel.CreateBounded<LLmApiRequest>(new BoundedChannelOptions(100) { SingleReader = true });

        }

        private static Channel<LLmApiRequest> requestQueue;

        public static void QueueRequest(LLmApiRequest request)
        {
            if (requestQueue.Writer.TryWrite(request))
            {
                if(Program.logInputSteps) Console.WriteLine("Request added to queue. Queue size: " + requestQueue.Reader.Count);
            }
            else
            {
                Console.WriteLine("Failed to add request to queue.");
            }
        }


        public async Task TryProcessQueue()
        {
                try {

                    if (requestQueue.Reader.TryRead(out var request))
                    {

                        if(Program.logInputSteps) Console.WriteLine("sending request: \n" + request.AsString());
                        
                        string jsonResponse = await RestApi.PostRequest(Config.GetValue("llmsrv"), request.AsString());
                        JObject parsedResponse = JObject.Parse(jsonResponse);
                        JArray data = (JArray)parsedResponse["data"];
                        string response = data[0].ToString();

                        if(Program.logInputSteps) Console.WriteLine(response);
                        if(Program.logInputSteps) Console.WriteLine("(end response)");

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
                            UserName = sender,
                            Text = text
                        };

                        // Get or create the user-specific collection and insert the response
                        var userCollection = Mongo.instance.GetUserCollection(sender, "responses");

                        // Insert the LLM's response into the database.
                        await Program.InsertResponseAsync(Mongo.instance.GetResponsesCollection(sender), res);
                        
                        Console.WriteLine(lastLine); //append the character's response to chat

                        //create AI's viewpoint summary of the events
                        await Program.TrySummarize();
                        request.callback?.Invoke(response); //do whatever with response

                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in TryProcessQueue: " + ex.Message);
            }

        } //end func TryProcessQueue


    }
}
