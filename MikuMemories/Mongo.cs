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
        public Mongo()
        {
            _client = new MongoClient(Config.GetValue("mongosrv"));
            instance = this; //set singleton instance

        }

        public IMongoCollection<Response> GetUserCollection(string username, string type)
        {
            return _client.GetDatabase(username).GetCollection<Response>($"user_{type}");
        }

        public void InsertResponse(Response response)
        {
            _responses.InsertOne(response);
        }

    }

    public class Response
    {
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
    }

}
