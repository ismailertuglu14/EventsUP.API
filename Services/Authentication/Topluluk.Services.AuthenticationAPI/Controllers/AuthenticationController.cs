using Microsoft.AspNetCore.Mvc;
using Topluluk.Services.AuthenticationAPI.Model.Dto;
using Topluluk.Services.AuthenticationAPI.Model.Dto.Http;
using Topluluk.Services.AuthenticationAPI.Services.Interface;
using Topluluk.Shared.BaseModels;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.AuthenticationAPI.Controllers
{ 
    public class AuthenticationController : BaseController
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IExternalAuthenticationService _externalAuthenticationService;
        public AuthenticationController(IAuthenticationService authenticationService, IExternalAuthenticationService externalAuthenticationService)
        {
            _authenticationService = authenticationService;
            _externalAuthenticationService = externalAuthenticationService;
        }

        [HttpPost("SignIn")]
        public async Task<Response<TokenDto>> SignIn(SignInUserDto userDto)
        {
            string? ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            string? deviceId = Request.Headers["User-Agent"];
            return await _authenticationService.SignIn(userDto,ipAddress,deviceId);
        }

        [HttpPost("google-login")]
        public async Task<Response<TokenDto>> GoogleLogin(GoogleLoginDto dto)
        {
            return await _externalAuthenticationService.SignInWithGoogle(dto);
        }

        [HttpPost("[action]")]
        public async Task<Response<TokenDto>> SignUp(CreateUserDto userDto)
        {
            return await _authenticationService.SignUp(userDto);
        }

        [HttpPost("[action]")]
        public async Task<Response<NoContent>> SignOut(SignOutUserDto userDto)
        {
            return await _authenticationService.SignOut(this.UserId, userDto);
        }
        [HttpPost("reset-password-request")]
        public async Task<Response<NoContent>> ResetPasswordRequest(MailDto mail)
        {
            return await _authenticationService.ResetPasswordRequest(mail.Mail);
        }
        [HttpPost("check-otp")]
        public async Task<Response<NoContent>> ResetPasswordCheckOTPCode(ResetPasswordCheckOTPDto dto)
        {
            return await _authenticationService.ResetPasswordCheckOTP(dto);
        }
        
        [HttpPost("reset-password")]
        public async Task<Response<NoContent>> ResetPassword( ResetPasswordDto resetPasswordDto)
        {
            return await _authenticationService.ResetPassword( resetPasswordDto);
        }

        [HttpPost("change-password")]
        public async Task<Response<NoContent>> ChangePassword(PasswordChangeDto passwordDto)
        {
            return await _authenticationService.ChangePassword(this.UserId, passwordDto);
        }

        // @@@@@@@@@@@ Http Requests @@@@@@@@@@@@@@@

        [HttpPost("delete")]
        public async Task<Response<NoContent>> Delete(UserDeleteDto dto)
        {

            return await _authenticationService.DeleteUser(this.UserId, dto);
        }


        [HttpPost("update-profile")]
        public async Task<Response<NoContent>> UpdateProfile(UserUpdateDto userDto)
        {
            return await _authenticationService.UpdateProfile(this.UserId, userDto);
        }

    }
}

