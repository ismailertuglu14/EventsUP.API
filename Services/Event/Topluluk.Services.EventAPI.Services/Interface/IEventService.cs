﻿using System;
using Topluluk.Services.EventAPI.Model.Dto;
using Topluluk.Services.EventAPI.Model.Enums;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.EventAPI.Services.Interface
{
	public interface IEventService
	{
		Task<Response<string>> CreateEvent(string userId, string token, CreateEventDto dto);
		Task<Response<List<EventDto>>> GetUserEvents(string userId, string token);
		Task<Response<List<EventGetSuggestionDto>>> GetEventSuggestions(EventFilter filter, int skip = 0, int take = 10);
		Task<Response<EventDto>> GetEventById(string userId, string token, string id);

        Task<Response<NoContent>> JoinEvent(string userId, string eventId);

        Task<Response<NoContent>> LeaveEvent(string userId, string eventId);

		Task<Response<string>> ExpireEvent(string userId, string id);
        Task<Response<string>> DeleteEvent(string userId, string id);
		Task<Response<string>> DeleteCompletelyEvent(string userId, string id);

		
        Task<Response<List<GetEventAttendeesDto>>> GetEventAttendees(string token, string eventId, int skip = 0,
            int take = 10);

		
    }
}

