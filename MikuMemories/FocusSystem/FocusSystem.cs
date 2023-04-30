using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Python.Runtime;
namespace MikuMemories
{
    public class FocusSystem
    {
        public class SearchResult
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public double Relevance { get; set; }
        }

        public class FocusOperator
        {
            public string Source { get; set; }
            public string Context { get; set; }
            public string SearchOperator { get; set; }
        }

        public static string GenerateFocus(string prompt) {
            // Create an instance of the QueryExpander class

            // Extract and expand keywords from the user input
            List<string> keywords = QueryExpander.instance.ExtractKeywords(userInput);
            List<string> expandedKeywords = QueryExpander.instance.ExpandQueryWithEmbeddings(keywords);

            // Use the expandedKeywords to guide context selection and response generation


            return "";
        } 


        static double CalculateFocusOperatorWeight(FocusOperator op, List<Response> recentMessages, CharacterCard characterCard)
        {
            // Calculate the weight of the focus operator based on its relevance to
            // recent messages, character attributes, and other criteria (realism, entertainment, etc.)
            double weight = 0.0;
            return weight; 
        }

        List<SearchResult> RecursiveSearch(string query, List<FocusOperator> focusOperators, int maxDepth, DateTime startTime, TimeSpan timeLimit, List<Response> recentMessages, CharacterCard characterCard, int depth = 0)
        {
            if (depth == maxDepth || (DateTime.Now - startTime) > timeLimit)
            {
                return new List<SearchResult>();
            }

            List<SearchResult> bestResults = new List<SearchResult>();

            // Calculate weights for each focus operator
            List<(FocusOperator, double)> weightedOperators = focusOperators
                .Select(op => (op, CalculateFocusOperatorWeight(op, recentMessages, characterCard)))
                .OrderByDescending(x => x.Item2)
                .ToList();

            foreach (var (op, weight) in weightedOperators)
            {
                string newQuery = ApplyFocusOperator(query, op);
                List<SearchResult> results = Search(newQuery);

                List<SearchResult> relevantResults = FilterRelevantResults(query, results);

                if (!relevantResults.Any())
                {
                    continue;
                }

                // Recursively apply focus operators
                List<SearchResult> childResults = RecursiveSearch(newQuery, focusOperators, maxDepth, startTime, timeLimit, recentMessages, characterCard, depth + 1);

                // Combine and sort results based on relevance
                List<SearchResult> combinedResults = relevantResults.Concat(childResults).ToList();
                combinedResults = combinedResults.OrderByDescending(r => r.Relevance).ToList();

                // Update best results if the current branch yields better results
                if (!bestResults.Any() || combinedResults[0].Relevance > bestResults[0].Relevance)
                {
                    bestResults = combinedResults;
                }
            }

            return bestResults;
        }
        List<SearchResult> FilterRelevantResults(string query, List<SearchResult> results)
        {
            List<SearchResult> relevantResults = new List<SearchResult>();

            // Filter the search results based on relevance to the query
            // This could involve using a scoring function or a threshold for relevance

            return relevantResults;
        }

        string ApplyFocusOperator(string query, FocusOperator op)
        {
            string newQuery = "";
            // Apply the focus operator to the query
            // This could involve adding, removing, or modifying keywords or search conditions

            return newQuery;
        }

        List<SearchResult> Search(string query) {
            //search data using focus operators 
            //focusOperator.Item1.Source;

            return default;
        }

    } //end class
}
