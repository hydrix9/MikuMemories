using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Python.Runtime;
using System.Linq;

namespace MikuMemories {
    public class Query {
        public string original;
        public string[] expanded_tokenized;
        public List<string> intents = new List<string>();
        public List<Tuple<string, string>> entities = new List<Tuple<string, string>>();

        public string final;

        public Query(string original) {
            this.original = original;
            CompileQuery(original);
        }


        //process the query using NLP to get an enriched, simplified version
        Query CompileQuery(string original) {
            //perform tokenization and query expansion
            this.expanded_tokenized = PythonInterop.instance.ProcessQuery(original);
            

            this.final = string.Join(" ", this.expanded_tokenized);
            dynamic doc = PythonInterop.instance.GetNlpDoc(this.final); //doc is the query loaded into NLP model
            
            this.intents = PythonInterop.instance.ExtractIntents(doc);
            this.entities = PythonInterop.instance.ExtractEntities(doc);

            return this;
        }


    }

    public class QuerySystem
    {
        public static QuerySystem instance;

        public QuerySystem() {
            instance = this;

        }
        



    }

}