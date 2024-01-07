using MongoDB.Bson.Serialization.Attributes;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Enums;

namespace Topluluk.Services.PostAPI.Model.Entity
{
    public class PostInteraction : AbstractEntity
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string PostId { get; set; }
        public User User { get; set; }
        public InteractionEnum InteractionType { get; set; }
    }
}
