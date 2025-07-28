using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class Counter
    {
        /// <summary>
        /// The unique identifier for this counter sequence.
        /// This will be used as the '_id', e.g., "orgId_sequence", "userId_sequence".
        /// </summary>
        [BsonId] // Specifies that this property should be used as the document's _id
        public string Id { get; set; } = null!; // Use null! to indicate it will be initialized

        /// <summary>
        /// The current value of the sequence. This is the number that gets incremented atomically.
        /// </summary>
        [BsonElement("sequence_value")]
        public long SequenceValue { get; set; }
    }
}
