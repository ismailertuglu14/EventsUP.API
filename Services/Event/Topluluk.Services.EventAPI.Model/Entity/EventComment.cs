﻿using System;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.EventAPI.Model.Entity
{
	public class EventComment : AbstractEntity
	{
		public string EventId { get; set; }
        public User User { get; set; }
        public string Message { get; set; }

        public EventComment()
		{
        }
    }
}

