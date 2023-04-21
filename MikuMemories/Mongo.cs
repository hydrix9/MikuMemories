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

        private readonly IMongoCollection<Response> _responses;

        private readonly IMongoClient _client;

        //constructor
        public Mongo(string dbName)
        {
            _client = new MongoClient(Config.GetValue("mongosrv"));
            instance = this; //set singleton instance

        }

        public IMongoCollection<Response> GetUserCollection(string dbName, string userId)
        {
            return _client.GetDatabase(dbName).GetCollection<Response>($"user_{userId}");
        }

        public void InsertResponse(Response response)
        {
            _responses.InsertOne(response);
        }

    }

    public class Response
    {
        public DateTime Timestamp { get; set; }
        public string Sender { get; set; }
        public string Text { get; set; }
    }

}
