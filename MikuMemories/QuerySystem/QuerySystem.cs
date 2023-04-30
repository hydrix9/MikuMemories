using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Python.Runtime;
using System.Linq;

namespace MikuMemories {


    public class QuerySystem
    {
        public static QuerySystem instance;

        public QuerySystem() {
            instance = this;

        }

        public static string[] CompileQuery(string query) {
            string[] returns = PythonInterop.instance.ProcessQuery(query);






            return returns;
        }


    }

}