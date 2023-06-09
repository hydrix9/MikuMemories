﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public static string FindCharacterCardFilePath(string characterCardFileName)
        {
            // Search for the character card file next to the executable
            string executablePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string charactersFolderPath = Path.Combine(executablePath, "Characters");
            string characterCardPath = Path.Combine(charactersFolderPath, characterCardFileName);

            if (File.Exists(characterCardPath))
            {
                return characterCardPath;
            }

            // If the file is not found next to the executable, search in the project folder
            string projectFolderPath = Directory.GetParent(executablePath).Parent.Parent.FullName;
            string projectCharactersFolderPath = Path.Combine(projectFolderPath, "Characters");
            string projectCharacterCardPath = Path.Combine(projectCharactersFolderPath, characterCardFileName);

            if (File.Exists(projectCharacterCardPath))
            {
                return projectCharacterCardPath;
            }

            // If the file is not found in both locations, return null
            return null;
        }
        
        public static int[] GetSummaryLengths()
        {
            try
            {
                var lengths = jObject.GetValue("summariesLengths");
                return lengths.ToObject<int[]>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetSummaryLengths: " + ex.Message);
                throw; // Re-throw the exception so it can be caught and handled by the calling code
            }
        }

    } //end class config
}
