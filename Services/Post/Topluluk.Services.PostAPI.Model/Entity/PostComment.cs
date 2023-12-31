﻿using MongoDB.Bson.Serialization.Attributes;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.PostAPI.Model.Entity
{
	public class PostComment : AbstractEntity
	{
		public User User { get; set; }

		[BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
		public string PostId { get; set; }
		public string? ParentCommentId { get; set; }
		public string Message { get; set; }
		public List<PreviousMessage>? PreviousMessages { get; set; }
	}

	public class PreviousMessage
	{
		public string Message { get; set; }
		public DateTime EditedDate { get; set; }
	}
}

