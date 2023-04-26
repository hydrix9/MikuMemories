using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace MikuMemories
{

    public class LLmApiRequest
    {

        //my custom "enum"
        public class Type {
            public static readonly string 
                Response = "Request"
            ;
        }

        public LlmInputParams @params;
        [JsonIgnore]
        public string prompt;
        [JsonIgnore]
        public string request_id;

        //(optional) what to do with string results from llm
        public delegate void Callback<T>(T obj);
        [JsonIgnore]
        public Callback<string> callback;
        
        [JsonIgnore]
        public string type;

        [JsonIgnore]
        public string author;

   
        public LLmApiRequest(string prompt, LlmInputParams @params, string type, string author, Callback<string> callback = null)
        {
            this.prompt = prompt;
            this.@params = @params;
            this.type = type;
            this.author = author;
            request_id = generateID();
            this.callback = callback;
        }


        public string generateID()
        {
            return Guid.NewGuid().ToString("N");
        }


        public string AsString()
        {
            string promptWithoutConsecutiveNewlines = Regex.Replace(prompt, @"\n{2,}", "\n");
            string jsonString = JsonConvert.SerializeObject(new object[] { promptWithoutConsecutiveNewlines, @params });
            jsonString = jsonString.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "{\"data\": [\"" + jsonString + "\"]}";
        }

        public string AsString_raw()
        {
            string jsonString = JsonConvert.SerializeObject(new object[] { prompt, @params });
            jsonString = jsonString.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "{\"data\": [\"" + jsonString + "\"]}";
        }


        public string AsString_old()
        {
            string jsonString = JsonConvert.SerializeObject(new object[] { prompt, @params });
            jsonString = jsonString.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "{\"data\": [\"" + jsonString + "\"]}";
        }

        public string AsString_old3()
        {
            return "{\"data\": [[" + "\"" + prompt + "\"," + JsonConvert.SerializeObject(@params) + "]]}";

        }

        public string AsString_old2()
        {
            return "[" + "{\"prompt\":" + "\"" + prompt + "\"}," + JsonConvert.SerializeObject(@params) + "]";

        }

    }
}
