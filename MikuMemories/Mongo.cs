using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Diagnostics;

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

        public async Task<List<IMongoDatabase>> GetUserDatabases()
        {
            var databasesCursor = await _client.ListDatabasesAsync();
            var databasesList = await databasesCursor.ToListAsync();
            var userDatabases = new List<IMongoDatabase>();

            foreach (var database in databasesList)
            {
                string databaseName = database["name"].AsString;
                if (databaseName.StartsWith("user_"))
                {
                    userDatabases.Add(_client.GetDatabase(databaseName));
                }
            }

            return userDatabases;
        }

        public IMongoCollection<Response> GetUserCollection(string userName, string type)
        {
            return _client.GetDatabase(FormatUserName(userName)).GetCollection<Response>($"user_{type}");
        }


        public IMongoCollection<Response> GetResponsesCollection(string userName)
        {

            var database = _client.GetDatabase("user_" + FormatUserName(userName));
            var collection = database.GetCollection<Response>(responseCollection);
            return collection;
        }
        public IMongoCollection<Summary> GetSummariesCollection(string userName)
        {
            var database = _client.GetDatabase("user_" + FormatUserName(userName));
            var collection = database.GetCollection<Summary>(summariesCollection);

            return collection;
        }

        public async Task<List<IMongoCollection<Response>>> GetCharacterResponseCollections()
        {
            var databaseNamesCursor = await _client.ListDatabaseNamesAsync();
            var databaseNames = await databaseNamesCursor.ToListAsync();
            var characterDatabases = databaseNames.Where(dbName => dbName.StartsWith("user_"));

            var characterResponseCollections = new List<IMongoCollection<Response>>();

            foreach (var dbName in characterDatabases)
            {
                var database = _client.GetDatabase(dbName);
                var collectionExists = await CollectionExists(database, responseCollection);

                if (collectionExists)
                {
                    var collection = database.GetCollection<Response>(responseCollection);
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

        public async Task<List<string>> GetLatestMessages(string characterName, int count)
        {
            var database = _client.GetDatabase("user_" + FormatUserName(characterName));
            var collection = database.GetCollection<BsonDocument>(responseCollection);
            var messages = await collection.Find(new BsonDocument())
                                           .SortByDescending(m => m["timestamp"])
                                           .Limit(count)
                                           .ToListAsync();

            return messages.Select(m => m["text"].AsString).ToList();
        }

        //used to get the summary that uses a length called length, it is then combined with all the other latest summaries to form the context
        public async Task<Summary> GetLatestSummary(string characterName, int length)
        {
            var collection = GetSummariesCollection(FormatUserName(characterName));
            var filter = Builders<Summary>.Filter.Eq("SummaryLength", length);
            var latestSummaryBson = await collection.Find(filter).SortByDescending(s => s.Timestamp).FirstOrDefaultAsync();

            if (latestSummaryBson != null)
            {
                return latestSummaryBson;
            }
            return null;
        }


        public async Task<List<Response>> GetLatestMessagesFromUserResponses(int count)
        {
            var userDatabases = await GetUserDatabases();
            var allMessages = new List<Response>();

            foreach (var database in userDatabases)
            {
                var collection = database.GetCollection<Response>("responses");
                var messages = await collection.Find(new BsonDocument())
                                               .SortByDescending(m => m.Timestamp)
                                               .Limit(count)
                                               .ToListAsync();
                allMessages.AddRange(messages);
            }

            allMessages = allMessages.OrderBy(m => m.Timestamp).Take(count).ToList();
            //return allMessages.Select(m => m.Text).ToList();
            return allMessages;
        }



        public static string FormatUserName(string username)
        {
            if(string.IsNullOrEmpty(username) || username == "") {
                StackTrace trace = new StackTrace();
                Console.WriteLine(trace.ToString());
                throw new ArgumentException("blank username supplied");
            }
            string returns = username.Replace(".", "");

            if (string.IsNullOrEmpty(returns) || returns.Contains('\0'))
            {
                throw new ArgumentException("Invalid userName value.", nameof(returns));
            }
            return returns;
        }

        public static async Task<Response> GetLatestResponseFromAllAsync()
        {
            var responseCollections = await instance.GetCharacterResponseCollections();

            Response latestResponse = null;

            foreach (var collection in responseCollections)
            {
                var response = await collection.Find(FilterDefinition<Response>.Empty)
                    .Sort(Builders<Response>.Sort.Descending(r => r.Timestamp))
                    .Limit(1)
                    .FirstOrDefaultAsync();

                if (response != null && (latestResponse == null || response.Timestamp > latestResponse.Timestamp))
                {
                    latestResponse = response;
                }
            }

            if (latestResponse != null)
            {
                return latestResponse;
            }

            return null;
        }


    } //end class Mongo

    public class Response : SearchResult
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }
        
    }

    public class Summary
    {
        public ObjectId Id { get; set; }
        public string Text { get; set; }
        public int SummaryLength { get; set; }
        public DateTime Timestamp { get; set; }


    }


}
