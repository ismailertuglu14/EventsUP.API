using MongoDB.Bson.Serialization.Attributes;
using System;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.PostAPI.Model.Dto
{
	public class CommentGetDto
	{
		[BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
		public string Id { get; set; }
        public User User { get; set; }
        public string Message { get; set; }

		public DateTime CreatedAt { get; set; }

		public bool IsEdited { get; set; }

		public int ReplyCount { get; set; }
		public CommentInteracted IsInteracted { get; set; }
		public CommentLikes InteractionCounts { get; set; }
	}

	public class CommentInteracted
	{
		public bool Like { get; set; }
		public bool Dislike { get; set; }
	}

	public class CommentLikes
	{
		public int LikeCount { get; set; }
		public int DislikeCount { get; set; }
	}
}

