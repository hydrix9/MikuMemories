using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikuMemories
{
    public class Tools
    {
        public static CharacterCard LoadCharacterCardFromFile(string filePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                CharacterCard characterCard = JsonConvert.DeserializeObject<CharacterCard>(jsonContent);
                return characterCard;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading character card from file: {ex.Message}");
                return null;
            }
        }

    }
}
