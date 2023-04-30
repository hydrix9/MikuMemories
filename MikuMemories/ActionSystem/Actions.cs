using System.Collections.Generic;
using System.Threading.Tasks;
using System;


namespace MikuMemories {
    public class SearchOnlineAction : IAction
    {
        public string Name => "SearchOnline";

        public async Task ExecuteAsync(ConversationContext context, IDictionary<string, object> parameters)
        {
            
            if (parameters.TryGetValue("query", out var queryObj) && queryObj is string query)
            {
                //List<string> searchResults = await SearchOnlineAsync(query);
                // Store the search results in the context or process them further.
            }
        }
    }

}