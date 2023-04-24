using System;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Newtonsoft.Json.Linq;

namespace MikuMemories
{
    public class CharacterLoader
    {
        public static CharacterCard LoadCharacter(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath);
            JObject characterData;

            switch (fileExtension.ToLower())
            {
                case ".json":
                    characterData = LoadJsonFromFile(filePath);
                    break;
                case ".png":
                    characterData = LoadJsonFromPng(filePath);
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

        private static JObject LoadJsonFromPng(string filePath)
        {
            using (Image<Rgba32> image = Image.Load<Rgba32>(filePath))
            {
                StringBuilder jsonData = new StringBuilder();
                byte currentByte = 0;
                int bitIndex = 0;

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        Rgba32 pixel = image[x, y];
                        byte[] bytes = { pixel.R, pixel.G, pixel.B, pixel.A };

                        foreach (byte b in bytes)
                        {
                            currentByte |= (byte)((b & 1) << (7 - bitIndex));
                            bitIndex++;

                            if (bitIndex == 8)
                            {
                                if (currentByte == 0)
                                {
                                    // End of JSON data
                                    return JObject.Parse(jsonData.ToString());
                                }

                                jsonData.Append((char)currentByte);
                                currentByte = 0;
                                bitIndex = 0;
                            }
                        }
                    }
                }

                // If the loop finishes, the JSON data may be incomplete or not properly terminated
                throw new ArgumentException("The JSON data in the PNG file appears to be incomplete or not properly terminated.");
            }
        }


    }
}