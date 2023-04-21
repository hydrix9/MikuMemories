using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MikuMemories
{

    public class LLmApiRequest
    {
        public LlmInputParams @params;
        [JsonIgnore]
        public string prompt;
        [JsonIgnore]
        public string request_id;

        //(optional) what to do with string results from llm
        public delegate void Callback<T>(T obj);
        [JsonIgnore]
        public Callback<string> callback;

        public LLmApiRequest(LlmInputParams @params, Callback<string> callback = null)
        {
            this.@params = @params;
            request_id = generateID();
            this.callback = callback;
        }


        public string generateID()
        {
            return Guid.NewGuid().ToString("N");
        }

        public string AsString()
        {
            string jsonString = JsonConvert.SerializeObject(new object[] { prompt, @params });
            return "{\"data\": [ " + "\"" + jsonString.Replace("\"", "\\\"") + "\"" + "]}";
        }

        public string AsString_old2()
        {
            return "{\"data\": [[" + "\"" + prompt + "\"," + JsonConvert.SerializeObject(@params) + "]]}";

        }

        public string AsString_old()
        {
            return "[" + "{\"prompt\":" + "\"" + prompt + "\"}," + JsonConvert.SerializeObject(@params) + "]";

        }

    }
}
