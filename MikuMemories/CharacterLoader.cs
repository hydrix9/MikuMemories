using System;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace MikuMemories
{
    public class CharacterLoader
    {
        public static async Task<CharacterCard> LoadCharacter(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath);
            JObject characterData;

            switch (fileExtension.ToLower())
            {
                case ".json":
                    characterData = LoadJsonFromFile(filePath);
                    break;
                case ".png":
                case ".webp":
                    //characterData = CharacterLoader.LoadJsonFromPng(filePath);
                    // characterData = JObject.Parse(ReadLastLine(filePath));
                    characterData = await LoadJsonFromPng(filePath);

                    break;
                default:
                    throw new ArgumentException("Unsupported file type. Please provide a JSON or PNG file.");
            }

            return CharacterCard.FromJson(characterData);
        }

        private static JObject LoadJsonFromFile(string filePath)
        {
            string jsonData = File.ReadAllText(filePath);
            return JObject.Parse(jsonData);
        }

        private static readonly string WrapperJsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TavernAiWrapper", "tavernaiWrapper.js");

        
        public static async Task<JObject> LoadJsonFromPng(string filePath)
            {
                try
                {
                    string json = await NodeServiceHandler.nodeJSService.InvokeFromFileAsync<string>(WrapperJsPath, "loadJsonFromPng", args: new object[] { filePath });
                    return JObject.Parse(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading character card: {ex.Message}");
                    return null;
                }
            }
    }

}