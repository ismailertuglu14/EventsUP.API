using Topluluk.Shared.Dtos;

namespace Topluluk.Services.EventAPI.Model.Entity;

public class EventAttendee : AbstractEntity
{
    public User User { get; set; }
    public string EventId { get; set; }
}