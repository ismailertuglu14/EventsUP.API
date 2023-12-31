﻿using System;
using Topluluk.Services.CommunityAPI.Model.Dto.Http;
using Topluluk.Services.CommunityAPI.Model.Entity;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Enums;

namespace Topluluk.Services.CommunityAPI.Model.Dto
{
	public class CommunityGetByIdDto
    {
	    public string Id { get; set; }
        public User Admin { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public string? Location { get; set; }

        public string? CoverImage { get; set; }
        public string? BannerImage { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool IsPublic { get; set; } = true;
        public bool IsRestricted { get; set; } = false;

        public int ParticipiantsCount { get; set; }

        public bool IsOwner { get; set; }

        public bool IsParticipiant { get; set; }
        
        public CommunityGetByIdDto()
		{
		}
	}
}

