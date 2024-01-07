using MongoDB.Bson.Serialization.Attributes;
using System;
using Topluluk.Services.PostAPI.Model.Entity;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Enums;

namespace Topluluk.Services.PostAPI.Model.Dto
{
	public class GetPostByIdDto
	{
		[BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
		public string Id { get; set; }

        public User User { get; set; }

        public string? CommunityTitle { get; set; }

		public string Description { get; set; }
		public DateTime CreatedAt { get; set; }
		public PostInteractedDto IsInteracted { get; set; }

		// Kullanıcı postu paylaşan kişiyi takip ediyor mu?
		public bool IsUserFollowing { get; set; }

		public int InteractionCount { get; set; }

		public List<PostInteractionPreviewDto> InteractionPreviews { get; set; }

		public List<FileModel>? Files { get; set; }

		public int CommentCount { get; set; }
		public bool IsSaved { get; set; }
		public ICollection<CommentGetDto>? Comments { get; set; }

		public GetPostByIdDto()
		{
			InteractionPreviews = new List<PostInteractionPreviewDto>();
            Files = new List<FileModel>();
			Comments = new HashSet<CommentGetDto>();
		}
	}
}

