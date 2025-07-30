using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatNetwork
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("hn_id")]
        public string? hn_id { get; set; }

        [BsonElement("location")]
        public string location { get; set; }

        [BsonElement("name")]
        public string name { get; set; }
    }
}
