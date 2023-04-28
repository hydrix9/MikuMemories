using Python.Runtime;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MikuMemories {
    public class ConversationContext
    {

        string characterContext;
        string recentResponses;
        string summaries;
        string focus;
        public Func<string> BuildContext { get; set; }

        public ConversationContext(string characterContext, string recentResponses, string summaries) {
            this.characterContext = characterContext;
            this.recentResponses = recentResponses;
            this.summaries = summaries;

            //generate focus here
            this.focus = "";
        }


        //uses focus to give virtual millions of token context
        public string CompileFullContext_GeneralMain()
        {

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Continue the dialogue below by generating the next line for the character {Program.characterName}. Your response should reflect {Program.characterName}'s personality and speech quirks while considering the context of the conversation. Be sure to prefix the response with \"{Program.characterName}: (e.g., \"{Program.characterName}: Hello!\") Feel free to include actions or reactions to make the response more engaging.");
            sb.AppendLine(characterContext);
            sb.AppendLine(); // Adds an extra newline
            sb.AppendLine("### Conversation So Far:");
            sb.AppendLine(recentResponses);
            sb.AppendLine(); // Adds an extra newline

            if(focus.Length > 0) {
                sb.AppendLine("### Focus:");
                sb.AppendLine(); // Adds an extra newline
                sb.AppendLine(focus);
                sb.AppendLine(); // Adds an extra newline
                sb.AppendLine("### Instruction:");
                sb.AppendLine(); // Adds an extra newline
                sb.AppendLine("Incorporate the information provided in the Focus section into your response, ensuring it is relevant and appropriate for the current conversation.");
                sb.AppendLine(); // Adds an extra newline
            }            
            if(summaries.Length > 0) {
                sb.AppendLine("### Summaries of Previous Conversation and Events:");
                sb.AppendLine(summaries);
            }
            sb.AppendLine("### Response:");

            return sb.ToString();

        }

        //old implimentation without Focus
        public string CompileFullContext_NoFocus_Old()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Continue the dialogue below by generating the next line for the character {characterName}. Your response should reflect {characterName}'s personality and speech quirks while considering the context of the conversation. Be sure to prefix the response with \"{characterName}:\" (e.g., \"{characterName}: Hello!\") Feel free to include actions or reactions to make the response more engaging.");
            sb.AppendLine(characterContext);
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



    }
}