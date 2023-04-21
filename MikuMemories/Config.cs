using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuMemories
{
    public class Config
    {
        string path = "config.json";

        static Config instance;

        static JsonTextReader reader;
        static JObject jObject;
        static StreamReader streamReader;
        public Config()
        {
            instance = this; //set singleton

            if(!File.Exists(path))
            {
                path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + "\\" + path; //try find config at root of project folder
                if (!File.Exists(path))
                    throw new FileNotFoundException("config.json does not exist at specified location or " + path);
            }

            streamReader = File.OpenText(@path);
            reader = new JsonTextReader(streamReader);
            jObject = ((JObject)JToken.ReadFrom(reader));
        }

        public static string GetValue(string propertyName)
        {
            return jObject.GetValue(propertyName).ToString();

        }
    }
}
