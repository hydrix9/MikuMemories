using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Python.Runtime;
namespace MikuMemories
{
    public class SearchResult
    {
        public string Author { get; set; }
        public string Source { get; set; } //chat history, Wikipedia, etc. Possibly just the db name
        public string Content { get; set; }
        public double Relevance { get; set; }
        public DateTime Timestamp { get; set; }
        public long EmbeddingId { get; set; }

        public double Weight { get; set; }

    }
    
    public class FocusSystem
    {


        public class FocusOperator
        {
            public string Source { get; set; }
            public string[] Context { get; set; }
            public string[] SearchOperator { get; set; }
            public string[] ApplyTo { get; set; }

        }

        public string GenerateFocus(string prompt) {

            //will automatically create, format, tokenize, do query expansion, and other things
            Query query = new Query(prompt);

            //start a recursive search through the databases
            List<SearchResult> allresults = RecursiveSearch(
                query,
                new List<FocusOperator>(),
                int.Parse(Config.GetValue("query_recursive_maxDepth")),
                DateTime.UtcNow,
                new TimeSpan(0, 0, int.Parse(Config.GetValue("query_recursive_seconds"))),
                new List<Response>(), Program.characterCard
                );


            //weigh, filter, reduce, rank results and take the max that will fit in context
            

            double decayRate = double.Parse(Config.GetValue("response_temporal_decay"));

            // Apply the temporal weight to Response results and calculate the total weight increase
            double totalWeightIncrease = 0;
            foreach (SearchResult result in allresults) {
                if (result is Response response) {
                    double oldWeight = response.Weight;
                    response.Weight *= CalculateWeight(response.Timestamp, decayRate);
                    totalWeightIncrease += response.Weight - oldWeight;
                }
            }

            // Normalize the weights of Response results based on the total weight increase
            foreach (SearchResult result in allresults) {
                if (result is Response response) {
                    response.Weight -= (response.Weight - 1) * (totalWeightIncrease / (allresults.Count(r => r is Response) * totalWeightIncrease));
                }
            }


            return "";

        }

        // Calculate the temporal weight based on the timestamp and decay rate
        double CalculateWeight(DateTime timestamp, double decayRate) {
            double elapsedTime = (DateTime.UtcNow - timestamp).TotalSeconds;
            return Math.Exp(-decayRate * elapsedTime);
        }


        static double CalculateFocusOperatorWeight(FocusOperator op, List<Response> recentMessages, CharacterCard characterCard)
        {
            // Calculate the weight of the focus operator based on its relevance to
            // recent messages, character attributes, and other criteria (realism, entertainment, etc.)
            double weight = 0.0;
            return weight; 
        }

        public double CalculateRecencyWeight(DateTime messageTimestamp, double decayRate)
        {
            /*
            | Time Difference | Very Slow Decay (0.000056) | Slow Decay (0.000167) | Moderate Decay (0.0005) |
            | --------------- | -------------------------- | --------------------- | ----------------------- |
            | 1 Hour          | 0.99996                    | 0.99983               | 0.9995                  |
            | 1 Day           | 0.9987                     | 0.9959                | 0.9854                  |
            | 1 Week          | 0.9921                     | 0.9724                | 0.8902                  |
            | 1 Month         | 0.9835                     | 0.8919                | 0.6105                  |
            | 1 Year          | 0.8738                     | 0.4286                | 0.0067                  |
            */

            TimeSpan timeDifference = DateTime.UtcNow - messageTimestamp;
            double timeDifferenceInHours = timeDifference.TotalHours;
            double recencyWeight = Math.Exp(-decayRate * timeDifferenceInHours);
            return recencyWeight;
        }


        List<SearchResult> RecursiveSearch(Query query, List<FocusOperator> focusOperators, int maxDepth, DateTime startTime, TimeSpan timeLimit, List<Response> recentMessages, CharacterCard characterCard, int depth = 0)
        {
            if (depth == maxDepth || (DateTime.UtcNow - startTime) > timeLimit)
            {
                return new List<SearchResult>();
            }

            List<SearchResult> bestResults = new List<SearchResult>();

            // Calculate weights for each focus operator
            List<(FocusOperator, double)> weightedOperators = focusOperators
                .Select(op => (op, CalculateFocusOperatorWeight(op, recentMessages, characterCard)))
                .OrderByDescending(x => x.Item2)
                .ToList();
            
            /*
            foreach (var (op, weight) in weightedOperators)
            {
                string newQuery = ApplyFocusOperator(query, op);

                List<SearchResult> results = Search(newQuery);

                List<SearchResult> relevantResults = FilterRelevantResults(query, results);

                if (!relevantResults.Any())
                {
                    continue;
                }

                //generate more focus operators
                List<FocusOperator> newFocusOperators = GetFocusOperators(relevantResults);
                focusOperators.AddRange(newFocusOperators);
                
                FilterFocusOperators(focusOperators);

                // Recursively apply focus operators
                List<SearchResult> childResults = RecursiveSearch(new Query(newQuery), focusOperators, maxDepth, startTime, timeLimit, recentMessages, characterCard, depth + 1);

                // Combine and sort results based on relevance
                List<SearchResult> combinedResults = relevantResults.Concat(childResults).ToList();

                combinedResults = combinedResults.OrderByDescending(r => r.Relevance).ToList();

                // Update best results if the current branch yields better results
                if (!bestResults.Any() || combinedResults[0].Relevance > bestResults[0].Relevance)
                {
                    bestResults = combinedResults;
                }
            }
            */
            
            return bestResults;
        }

        //find search operators that are relevant (using cosine similarity?)
        List<FocusOperator> GetFocusOperators(List<SearchResult> searchResults) {
            
            return default;
        }

        //remove redundant, irrelevant focus operators
        void FilterFocusOperators(List<FocusOperator> focusOperators) {

        }

        void CalculateRelevance(List<SearchResult> results) {

        }

        List<SearchResult> FilterRelevantResults(string query, List<SearchResult> results)
        {
            List<SearchResult> relevantResults = new List<SearchResult>();

            // Filter the search results based on relevance to the query
            // This could involve using a scoring function or a threshold for relevance

            return relevantResults;
            // Apply the focus operator to the query
            // This could involve adding, removing, or modifying keywords or search conditions

        }

        List<SearchResult> Search(string query) {
            //search data using focus operators 
            //focusOperator.Item1.Source;

            return default;
        }

    } //end class
}
