using System;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Enums;

namespace Topluluk.Services.EventAPI.Model.Dto
{
	public class GetEventCommentDto
	{
		public string Id { get; set; }
        public User User { get; set; }
        public string Message { get; set; }
		public int InteractionCount { get; set; }
		public bool IsInteracted { get; set; } = false;
	}
}

