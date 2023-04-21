using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
namespace MikuMemories
{
    public class Mongo
    {

        public static Mongo instance;

        static readonly string responseCollectionSuffix = "_responses";
        static readonly string summariesCollectionSuffix = "_summaries";

        private readonly IMongoClient _client;

        //constructor
        public Mongo()
        {
            _client = new MongoClient(Config.GetValue("mongosrv"));
            instance = this; //set singleton instance

        }

        public IMongoCollection<Response> GetUserCollection(string userName, string type)
        {
            return _client.GetDatabase(userName).GetCollection<Response>($"user_{type}");
        }

        public void InsertResponse(string userName, Response response)
        {
            GetResponsesCollection(userName).InsertOne(response);
        }


        public IMongoCollection<Response> GetResponsesCollection(string userName)
        {

            var database = _client.GetDatabase(userName);
            var collection = database.GetCollection<Response>(userName + responseCollectionSuffix);
            return collection;
        }
        public IMongoCollection<Summary> GetSummariesCollection(string username)
        {

            var database = _client.GetDatabase(username);
            var collection = database.GetCollection<Summary>(username + summariesCollectionSuffix);
            return collection;
        }


    }

    public class Response
    {
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
    }

    public class Summary
    {
        public ObjectId Id { get; set; }
        public string Text { get; set; }
        public int SummaryLength { get; set; }
    }


}
