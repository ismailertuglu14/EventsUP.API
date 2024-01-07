using Topluluk.Shared.Enums;

namespace Topluluk.Shared.Messages.User
{
    public class UserUpdatedEvent
    {
        public string Id { get; init; }
        public string FullName { get; init; }
        public string? UserName { get; init; }
        public string? ProfileImage { get; init; }
        public GenderEnum Gender { get; init; }
    }
}
