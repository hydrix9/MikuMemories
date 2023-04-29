using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using IO.Milvus;
using Google.Protobuf;
using IO.Milvus.Client;
using IO.Milvus.Common;
using IO.Milvus.Connection;
using IO.Milvus.Grpc;
using IO.Milvus.Param;
using IO.Milvus.Response;
using IO.Milvus.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using Grpc.Net.Client;
using Google.Protobuf.Collections;

namespace MikuMemories
{
    public class Milvus
    {
        private static Milvus _instance;
        public static Milvus instance => _instance ??= new Milvus();

        private readonly MilvusService.MilvusServiceClient _milvusClient;

        private Milvus()
        {
            var channel = GrpcChannel.ForAddress(Config.GetValue("milvus_address"));
            _milvusClient = new MilvusService.MilvusServiceClient(channel);
        }
                
        public long InsertEmbedding(string collectionName, ObjectId mongoId, float[] embedding)
        {
            VectorField vectorField = new VectorField
            {
                FloatVector = new FloatArray { Data = { embedding } }
            };

            var embeddingField = new FieldData
            {
                Type = DataType.FloatVector,
                FieldName = "embedding",
                Vectors = vectorField
            };


            var idField = new FieldData
            {
                Type = DataType.String,
                FieldName = "mongo_id",
                Scalars = new ScalarField { StringData = new StringArray { Data = { mongoId.ToString() }  } }
             };

            var request = new InsertRequest
            {
                CollectionName = collectionName,
                FieldsData = { embeddingField, idField },
                NumRows = 1
            };

            var response = _milvusClient.Insert(request);
            return response.IDs.IntId.Data[0];
        }

        public List<ObjectId> SearchEmbeddings(string collectionName, float[] queryEmbedding, int topK = 10)
        {
            byte[] byteArray = new byte[queryEmbedding.Length * sizeof(float)];

            for (int i = 0; i < queryEmbedding.Length; i++)
            {
                BitConverter.GetBytes(queryEmbedding[i]).CopyTo(byteArray, i * sizeof(float));
            }

            ByteString byteString = ByteString.CopyFrom(byteArray);


            var placeholderGroup = new PlaceholderGroup
            {
                Placeholders = { new PlaceholderValue { Tag = "$0", Type = PlaceholderType.FloatVector, Values = { byteString } } }
            };

            var searchRequest = new SearchRequest
            {
                CollectionName = collectionName,
                Dsl = "{\"bool\": {\"must\": [{\"vector\": {\"embedding\": {\"topk\": " + topK + ", \"query\": $0, \"metric_type\": \"L2\"}}}]}}",
                PlaceholderGroup = placeholderGroup.ToByteString(),
                SearchParams = { new IO.Milvus.Grpc.KeyValuePair { Key = "metric_type", Value = "L2" } },
                DslType = DslType.Dsl,
                OutputFields = { "embedding" }
            };

            var searchResponse = _milvusClient.Search(searchRequest);
            var resultIds = new List<long>();

            var resultData = searchResponse.Results;

            List<ObjectId> mongoIds = new List<ObjectId>();
            /*
            switch (resultData.Ids.IdFieldCase)
            {
                case IDs.IdFieldOneofCase.IntId:
                    // Assuming you want to work with the IntId field
                    foreach (var id in resultData.Ids.IntId.Data)
                    {
                        resultIds.Add(id);
                    }
                    break;
                case IDs.IdFieldOneofCase.StrId:
                    // Assuming you want to work with the StrId field
                    foreach (var id in resultData.Ids.StrId.Data)
                    {
                        resultIds.Add(long.Parse(id)); // Assuming you want to convert the string to a long
                    }
                    break;
                default:
                    // No valid IdField found
                    break;
            }
            return resultIds;
            */

            foreach (var fields in resultData.FieldsData) {
                mongoIds.Add(new ObjectId(fields.Scalars.StringData.Data[0]));
            }

            return mongoIds;

        }


        public async Task<List<string>> GetUserEmbeddingCollectionsAsync()
        {
            // List all collections in Milvus
            var request = new ShowCollectionsRequest();
            var response = await _milvusClient.ShowCollectionsAsync(request);

            // Filter collections based on the desired name pattern
            var userEmbeddingCollections = response.CollectionNames
                .Where(collectionName => collectionName.StartsWith("user_") && collectionName.EndsWith("_embeddings"))
                .ToList();

            return userEmbeddingCollections;
        }

        public async Task<List<Response>> SearchSimilarMessages(string userName, float[] queryEmbedding, int topK = 10)
        {

            List<ObjectId> similarIds = SearchEmbeddings(GetUserEmebddingsName(userName), queryEmbedding, topK);

            var collection = Mongo.instance.GetResponsesCollection(userName);

            // Update the filter to use the EmbeddingId field
            var filter = Builders<Response>.Filter.In("_id", similarIds);
            var similarMessages = await collection.Find(filter).ToListAsync();

            return similarMessages;
        }

        public static string GetUserEmebddingsName(string userName) {
            return $"user_{userName}_embeddings";
        }

    }
}
