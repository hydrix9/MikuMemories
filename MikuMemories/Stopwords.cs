using System;
using System.Collections.Generic;
using System.IO;

namespace MikuMemories {
    public class StopWords
    {
        public static HashSet<string> stopWords;

        public StopWords()
        {
            stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (StreamReader reader = new StreamReader(Config.GetValue("stopwords")))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        stopWords.Add(line.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading stopwords from file: " + ex.Message);
            }
        }

        public bool IsStopWord(string word)
        {
            return stopWords.Contains(word);
        }

        public IEnumerable<string> FilterStopWords(IEnumerable<string> words)
        {
            List<string> filteredWords = new List<string>();

            foreach (string word in words)
            {
                if (!IsStopWord(word))
                {
                    filteredWords.Add(word);
                }
            }

            return filteredWords;
        }
    }
}