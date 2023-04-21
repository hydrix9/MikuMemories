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

        static readonly string responseCollection = "responses";
        static readonly string summariesCollection = "summaries";

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
            var collection = database.GetCollection<Response>(responseCollection);
            return collection;
        }
        public IMongoCollection<Summary> GetSummariesCollection(string username)
        {

            var database = _client.GetDatabase(username);
            var collection = database.GetCollection<Summary>(summariesCollection);
            return collection;
        }

        public async Task<List<IMongoCollection<BsonDocument>>> GetCharacterResponseCollections()
        {
            var databaseNamesCursor = await _client.ListDatabaseNamesAsync();
            var databaseNames = await databaseNamesCursor.ToListAsync();
            var characterDatabases = databaseNames.Where(dbName => dbName.StartsWith("user_"));

            var characterResponseCollections = new List<IMongoCollection<BsonDocument>>();

            foreach (var dbName in characterDatabases)
            {
                var database = _client.GetDatabase(dbName);
                var collectionExists = await CollectionExists(database, responseCollection);

                if (collectionExists)
                {
                    var collection = database.GetCollection<BsonDocument>(responseCollection);
                    characterResponseCollections.Add(collection);
                }
            }

            return characterResponseCollections;
        }

        private async Task<bool> CollectionExists(IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var options = new ListCollectionsOptions { Filter = filter };

            var collections = await database.ListCollectionsAsync(options);
            return await collections.AnyAsync();
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
