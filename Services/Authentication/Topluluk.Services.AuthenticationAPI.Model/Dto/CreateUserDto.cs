﻿using System;
using Topluluk.Shared.Enums;

namespace Topluluk.Services.AuthenticationAPI.Model.Dto
{
	public class CreateUserDto
	{
		// this 2 properties for UserCollection
		public string FullName { get; set; }
		public string UserName { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public GenderEnum? Gender { get; set; }
		public CreateUserDto()
		{
			Gender = GenderEnum.Unspecified;
		}
	}
}

