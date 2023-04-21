using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
namespace MikuMemories
{
    public class Mongo
    {

        public static Mongo instance;

        //constructor
        public Mongo()
        {
            instance = this; //set singleton instance

            MongoClient dbClient = new MongoClient(Config.GetValue("mongosrv"));

        }

    }
}
