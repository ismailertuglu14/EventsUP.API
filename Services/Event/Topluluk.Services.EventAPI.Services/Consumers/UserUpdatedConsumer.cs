using MassTransit;
using Topluluk.Services.EventAPI.Data.Interface;
using Topluluk.Services.EventAPI.Model.Entity;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Messages.User;

namespace Topluluk.Services.EventAPI.Services.Consumers
{
    public class UserUpdatedConsumer : IConsumer<UserUpdatedEvent>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IEventAttendeesRepository _attendeesRepository;

        public UserUpdatedConsumer(IEventRepository eventRepository, IEventAttendeesRepository attendeesRepository)
        {
            _eventRepository = eventRepository;
            _attendeesRepository = attendeesRepository;
        }

        public async Task Consume(ConsumeContext<UserUpdatedEvent> context)
        {
            User user = new()
            {
                Id = context.Message.Id,
                FullName = context.Message.FullName,
                UserName = context.Message.UserName,
                Gender = context.Message.Gender,
                ProfileImage = context.Message.ProfileImage,
            };
            EventAttendee attendee = await _attendeesRepository.GetFirstAsync(u => u.User.Id == context.Message.Id);
            attendee.User = user;
            _attendeesRepository.Update(attendee);
        }
    }
}
