using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace MessageModels
{
    public class Messages
    {
        public string Text { get; set; }

    }
    //class to hols the request type
    public class Request
    {
        public RequestType RequestType { get; set; }
        public string Id { get; set; }
        public string CorrelationId { get; set; }


    }
    public enum RequestType
    {
        GetAll,
        GetById
    }
    public class Response
    {
        public string CorrelationId { get; set; }
        public List<ProductResponse> ProductsList { get; set; }

    }

    public class Product
    {
        [BsonId]
        public ObjectId Id { get; set; } // This property maps to the MongoDB _id field
        public string Name { get; set; }
        public string ProductId { get; set; }
        public string EntryDate { get; set; }
        public string PriceForToday { get; set; }
    }
    public class ProductResponse
    {
        public string Name { get; set; }
        public string ProductId { get; set; }
        public string EntryDate { get; set; }
        public string PriceForToday { get; set; }
    }

}
