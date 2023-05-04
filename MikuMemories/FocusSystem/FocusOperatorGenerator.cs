using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MikuMemories {
    public class FocusOperatorGenerator
    {
        
        public FocusOperatorGenerator() {

        }



        //to get better and better data, we could just continuosly run this at depth 5, 6, 7, 8, 9...
        //then switch up the ExpandAspect prompts to add or subtract parents nested or in sequence
        
        public static async Task GenerateOperatorsContinuously(string prompt) {

            int depth = int.Parse(Config.GetValue("FocusOperatorGeneratorSearchDepth"));
            while(true) {
                
                PromptNode rootNode = await GenerateAndExpandPrompts(prompt, depth);
                rootNode.Dispose(); //dispose children recursively, allows GC

                depth++;
            }

        }

        public static  async Task<PromptNode> GenerateAndExpandPrompts(string prompt, int depth)
        {
            // Get the aspects involved in the prompt
            string aspectsResponse = await AskLLM($"What aspects are involved in {prompt}?");
            List<string> aspects = ParseResponseToList(aspectsResponse);

            PromptNode root = new PromptNode(prompt);

            // Expand each aspect
            foreach (string aspect in aspects)
            {
                PromptNode childNode = await ExpandAspect(aspect, depth, prompt);
                root.Children.Add(childNode);
            }

            return root;
        }

        private static  async Task<PromptNode> ExpandAspect(string aspect, int depth, params string[] parents)
        {
            if (depth == 0)
            {
                return new PromptNode(aspect);
            }


            //ex: aspect1 and aspect2 and aspect3 and aspect4
            string parentsString = string.Join(" and ", parents);

            string expansionResponse = await AskLLM($"What aspects of {aspect} are related to {parentsString}?");


            List<string> subAspects = ParseResponseToList(expansionResponse);

            PromptNode currentNode = new PromptNode(aspect);

            foreach (string subAspect in subAspects)
            {
                //make new array with subaspect included in parents
                string[] newParents = new string[parents.Length + 1];
                Array.Copy(parents, newParents, parents.Length);
                newParents[newParents.Length - 1] = subAspect;

                PromptNode childNode = await ExpandAspect(subAspect, depth - 1, newParents);
                currentNode.Children.Add(childNode);
            }

            return currentNode;
        }

        private static  List<string> ParseResponseToList(string response)
        {
            // Implement a method to parse the LLM response to a list of strings
            // based on the format of the response
            Console.WriteLine(response);


            throw new NotImplementedException();
        }

        private static async Task<string> AskLLM(string query)
        {
            //does tokenization, query expand, entity recognition
            Query queryObj = new Query(query);
            //create formatted query
            FocusWriterContext context = new FocusWriterContext(queryObj);

            // Instead of using a callback, we'll create a TaskCompletionSource
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            // Pass the completion source's SetResult method as a callback
            LlmApi.QueueRequest(new LLmApiRequest(context.final, LlmInputParams.defaultParams, LLmApiRequest.Type.FocusWriter, "", tcs.SetResult));

            // Await the completion source's Task
            return await tcs.Task;
        }

    }
}