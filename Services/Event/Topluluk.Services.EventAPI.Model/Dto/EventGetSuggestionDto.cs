using System;
namespace Topluluk.Services.EventAPI.Model.Dto
{
	public class EventGetSuggestionDto
	{
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public bool IsPaid { get; set; }
        public double Price { get; set; }
        public bool IsOnline { get; set; }
        public List<string>? MutualFriendImages { get; set; }
        public bool IsParticipantLimited { get; set; }
        public int CurrentParticipants { get; set; }
        public int TotalParticipants { get; set; }
        public string Date { get; set; }
        public string Address { get; set; }

        public EventGetSuggestionDto()
		{
		}
	}
}

