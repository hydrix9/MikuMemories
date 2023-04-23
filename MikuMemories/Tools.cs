﻿using Newtonsoft.Json;
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

        //get charater card from file path
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

        //calculate the ratio of the summary text given the summary length
        public static double CalculateSummaryRatio(int summaryLength)
        {
            // Minimum ratio you want to achieve
            double minRatio = 0.1;

            // Maximum ratio you want to achieve
            double maxRatio = 0.5;

            // Base of the logarithm, which controls the exponential decrease
            // A higher value will result in a faster decrease in ratio as the summary length increases
            double logBase = 2;

            // Calculate the ratio using a logarithmic function
            double ratio = maxRatio - (Math.Log(summaryLength, logBase) * (maxRatio - minRatio) / Math.Log(10000, logBase));

            // Ensure the calculated ratio is within the defined limits
            ratio = Math.Max(minRatio, Math.Min(maxRatio, ratio));

            return ratio;
        }



    } //end class tools
}
