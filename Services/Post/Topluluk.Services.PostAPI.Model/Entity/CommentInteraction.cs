using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.PostAPI.Model.Entity;

public class CommentInteraction : AbstractEntity
{
    public User User { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public string CommentId { get; set; }
    public CommentInteractionType Type { get; set; }
}
public enum CommentInteractionType
{
    DISLIKE = 0,
    LIKE = 1
}


