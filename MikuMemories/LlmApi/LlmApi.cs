using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace MikuMemories
{
    public class LlmApi
    {
        public static LlmApi instance;


        public LlmApi()
        {
            instance = this;

        }

        public static List<LLmApiRequest> requestQueue = new List<LLmApiRequest>();

        public static void QueueRequest(LLmApiRequest request)
        {
            requestQueue.Add(request);
            if(Program.logInputSteps) Console.WriteLine("Request added to queue. Queue size: " + requestQueue.Count);

        }

        private static SemaphoreSlim tpq_functionLock = new SemaphoreSlim(1, 1);

        public async Task TryProcessQueue()
        {
            if (!await tpq_functionLock.WaitAsync(0)) {
                // If the function is already running, exit immediately.
                return;
            }
            try {
                if(requestQueue.Count > 0) {
                    LLmApiRequest request = requestQueue[0];

                    if(Program.logInputSteps) Console.WriteLine("sending request: \n" + JsonConvert.SerializeObject(JObject.Parse(request.AsString()), Formatting.Indented));
                    
                    string jsonResponse = await RestApi.PostRequest(Config.GetValue("llmsrv"), request.AsString());
                    JObject parsedResponse = JObject.Parse(jsonResponse);

                    await ProcessPostResponse(request, parsedResponse);
                    
                    // Reset the flag to indicate that the response request has been processed

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in TryProcessQueue: " + ex.Message);
            } finally {
                tpq_functionLock.Release();
            }

        } //end func TryProcessQueue


        async Task ProcessPostResponse(LLmApiRequest request, JObject jresponse) {
                       JArray data = (JArray)jresponse["data"];
                        string response = data[0].ToString();

                        if(Program.logInputSteps) Console.WriteLine(response);
                        if(Program.logInputSteps) Console.WriteLine("(end response)");

                        // Split the LLM response into lines and get the last non-empty line
                        var lines = response.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                        var lastLine = lines.LastOrDefault();

                        if (string.IsNullOrWhiteSpace(lastLine)) return;

                        // Find the sender based on the start of the line up to the first colon ':'
                        int colonIndex = lastLine.IndexOf(':');

                        string pattern = @"\[(.*?)\]";

                        // Create a regular expression object with the pattern
                        Regex regex = new Regex(pattern);

                        // Find all the matches in the input string
                        MatchCollection matches = regex.Matches(lastLine);

                        //TODO: handling [SomeTag] tags for stuff like narration and [System]


                        if (colonIndex < 0 && matches.Count == 0) {
                            Console.WriteLine("error: LLM did not output in format MyCharacter: message_here and no supported [] tags found.");
                            Console.WriteLine("full output:\n" + lastLine);
                            
                            Console.WriteLine($"attempting to manually prefix with \"{Program.characterName}:\"");
                            lastLine = Program.characterName + ": " + lastLine; 
                            colonIndex = lastLine.IndexOf(':');

                            //return;
                        }

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
                        await Program.InsertResponseAsync(sender, Mongo.instance.GetResponsesCollection(sender), res);
                        
                        Console.WriteLine(lastLine); //append the character's response to chat

                        /*
                         don't want to do this immediately, otherwise it'll keep creating requests of the same type
                         and removing them, so they pile up on server
                         hopefully this isn't going to be an intended behavior in the future
                         */
                        requestQueue.Remove(request);

                        //create AI's viewpoint summary of the events
                        await Program.TrySummarize();
                        request.callback?.Invoke(response); //do whatever with response
        }

    }
}
