using Topluluk.Shared.Enums;

namespace Topluluk.Services.EventAPI.Model.Dto
{
    public class GetEventAttendeesDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string? ProfileImage { get; set; }
        public GenderEnum Gender { get; set; }
    }
}
