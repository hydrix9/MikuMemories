using Python.Runtime;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MikuMemories {
    public class FocusWriterContext
    {

        public Query query;
        public string final;

        public FocusWriterContext(Query query) {
            this.query = query;
            final = CompileFullContext_GeneralMain();
        }


        //uses focus to give virtual millions of token context
        public string CompileFullContext_GeneralMain()
        {
            //"What aspects of {aspect} are related to {parentsString}?"

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"### Instruction");
            sb.AppendLine("Answer the question. Be as descriptive, elaborate, and imaginative as possible. Use a complete list when possible.");
            sb.AppendLine(); // Adds an extra newline
            sb.AppendLine("### Input:");
            sb.AppendLine(query.final);
            sb.AppendLine(); // Adds an extra newline


            sb.AppendLine("### Response:");

            return sb.ToString();

        }

    }
}