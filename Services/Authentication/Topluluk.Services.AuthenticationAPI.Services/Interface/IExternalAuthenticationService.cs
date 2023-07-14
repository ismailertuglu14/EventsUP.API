using System;
using Topluluk.Services.AuthenticationAPI.Model.Dto;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.AuthenticationAPI.Services.Interface
{
	public interface IExternalAuthenticationService
	{
		Task<Response<TokenDto>> SignInWithGoogle(GoogleLoginDto dto);
		Task<Response<NoContent>> SignInWithApple();
	}
}

