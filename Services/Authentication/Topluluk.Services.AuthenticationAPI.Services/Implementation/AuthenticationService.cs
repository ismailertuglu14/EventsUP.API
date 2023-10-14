﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using Topluluk.Services.AuthenticationAPI.Data.Interface;
using Topluluk.Services.AuthenticationAPI.Model.Dto;
using Topluluk.Services.AuthenticationAPI.Model.Dto.Http;
using Topluluk.Services.AuthenticationAPI.Model.Entity;
using Topluluk.Services.AuthenticationAPI.Services.Helpers;
using Topluluk.Services.AuthenticationAPI.Services.Interface;
using Topluluk.Shared.Constants;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Helper;
using Topluluk.Shared.Messages.Authentication;
using _MassTransit = MassTransit;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;

namespace Topluluk.Services.AuthenticationAPI.Services.Implementation
{
	public class AuthenticationService : IAuthenticationService
	{
        private readonly IAuthenticationRepository _repository;
        private readonly ILoginLogRepository _loginLogRepository;
        private readonly IConfiguration _configuration;
        private readonly RestClient _client;
        private readonly _MassTransit.ISendEndpointProvider _endpointProvider;
        private readonly ILogger<AuthenticationService> _logger;
        public AuthenticationService(ILogger<AuthenticationService> logger,IAuthenticationRepository repository, _MassTransit.ISendEndpointProvider endpointProvider, IConfiguration configuration, ILoginLogRepository loginLogRepository)
		{
            _repository = repository;
            _configuration = configuration;
            _loginLogRepository = loginLogRepository;
            _endpointProvider = endpointProvider;
            _client = new RestClient();
            _logger = logger;
		}

        public async Task<Response<TokenDto>> SignIn(SignInUserDto userDto, string? ipAdress, string? deviceId)
        {
            TokenHelper _tokenHelper = new TokenHelper(_configuration);
            UserCredential? user = new();
            if (userDto.UserName.IsNullOrEmpty() && userDto.Email.IsNullOrEmpty()) throw new Exception("Bad Request");
            if (!userDto.UserName.IsNullOrEmpty())
            {
                user = await _repository.GetFirstAsync(u => u.UserName == userDto.UserName && u.Provider == userDto.Provider);
            }
            else if (!userDto.Email.IsNullOrEmpty())
            {
                user = await _repository.GetFirstAsync(u => u.Email == userDto.Email && u.Provider == userDto.Provider);
            }

            var isPasswordVerified = PasswordFunctions.VerifyPassword(userDto.Password, user.HashedPassword);
            if (isPasswordVerified)
            {
                // Dead code fix later.b
                if (DateTime.Now < user.LockoutEnd)
                {
                    return Response<TokenDto>.Fail($"User locked until {user.LockoutEnd}", ResponseStatus.AccountLocked);
                }
                TokenDto token = _tokenHelper.CreateAccessToken(user.Id, user.UserName, user.Role, 2);
                user.AccessFailedCount = 0;
                user.LockoutEnd = DateTime.MinValue;
                user.Locked = false;
                UpdateRefreshToken(user, token, 2);
                LoginLog loginLog = new()
                {
                    UserId = user.Id,
                    IpAdress = ipAdress ?? "",
                    DeviceId = deviceId ?? ""
                };
                await _loginLogRepository.InsertAsync(loginLog);

                return Response<TokenDto>.Success(token, ResponseStatus.Success);
            }
            // User found but password wrong.
            if (user.AccessFailedCount < 15)
            {
                user.AccessFailedCount += 1;
            }
            else
            {
                user.Locked = true;
                user.LockoutEnd = DateTime.Now.AddMinutes(20);
                // todo We have to send a mail to user about someone wants to login without permission him/her account.
            }
            _repository.Update(user);

            return Response<TokenDto>.Fail("Username or password wrong!", ResponseStatus.NotAuthenticated);
        }

        public async Task<Response<TokenDto?>> SignUp(CreateUserDto userDto)
        {
            var checkUniqueResult = await CheckUserNameAndEmailUnique(userDto.UserName, userDto.Email);

            if (!checkUniqueResult.IsSuccess)
            {
                return Response<TokenDto?>.Fail(checkUniqueResult.Errors, ResponseStatus.InitialError);
            }
            var response = await _repository.InsertAsync(new UserCredential
            {
                UserName = userDto.UserName,
                Email = userDto.Email,
                Provider = userDto.Provider,
                HashedPassword = PasswordFunctions.HashPassword(userDto.Password),
            });
            var content = new UserInsertDto
            {
                Id = response.Data,
                FullName = userDto.FullName,
                UserName = userDto.UserName,
                Email = userDto.Email,
                BirthdayDate = DateTime.Now,
                Gender = userDto.Gender
            };
            var userInsertRequest = new RestRequest("https://localhost:7202/user/insertuser").AddBody(content);
            var userInsertResponse = await _client.ExecutePostAsync(userInsertRequest);
            if (!userInsertResponse.IsSuccessful)
            {
                _repository.DeleteCompletely(response.Data);
                return Response<TokenDto?>.Fail("Error occurred while user inserting!", ResponseStatus.InitialError);
            }
            var role = new List<string>() { UserRoles.USER };
            var token = new TokenHelper(_configuration).CreateAccessToken(response.Data, userDto.UserName, role, 2);
            var user = _repository.GetFirst(u => u.UserName == userDto.UserName);
            UpdateRefreshToken(user, token, 2);
            SendRegisteredMail sendRegisteredMail = new(_endpointProvider);
            //sendRegisteredMail.send(userDto.FirstName, userDto.LastName, userDto.Email);
            return Response<TokenDto?>.Success(token, ResponseStatus.Success);
        }

        public async Task<Response<NoContent>> SignOut(string userId,SignOutUserDto userDto)
        {
            if (userId.IsNullOrEmpty() || userDto.RefreshToken.IsNullOrEmpty())
                throw new Exception("User Not Found");
            UserCredential? user = await _repository.GetFirstAsync(u => u.Id == userId);
            if (user == null)
                return Response<NoContent>.Fail("User not found!", ResponseStatus.NotFound);
            user.RefreshToken = null;
            _repository.Update(user);
            return Response<NoContent>.Success(ResponseStatus.Success);
        }
        public async Task<Response<TokenDto>> CreateAccessTokenByRefreshToken(string refreshToken)
        {
            TokenHelper _tokenHelper = new TokenHelper(_configuration);
            UserCredential? user = await _repository.GetFirstAsync(u => u.RefreshToken == refreshToken);
            if (user == null)
                return Response<TokenDto>.Fail("User not found!", ResponseStatus.Unauthorized);
            if (user.RefreshTokenEndDate < DateTime.Now)
                return Response<TokenDto>.Fail("Refresh token expired!", ResponseStatus.NotAuthenticated);
            TokenDto token = _tokenHelper.CreateAccessToken(user.Id, user.UserName, user.Role, 2);
            UpdateRefreshToken(user, token, 2);
            return Response<TokenDto>.Success(token, ResponseStatus.Success);
        }


        public async Task<Response<NoContent>> ResetPasswordRequest(string email)
        {
            UserCredential? user = await _repository.GetFirstAsync(u => u.Email == email);
            if (user == null)
                return Response<NoContent>.Fail("User not found", ResponseStatus.NotFound);
            Random random = new Random();
            int randomNumber = random.Next(100000, 999999);
            user.ResetPasswordCode = randomNumber.ToString();
            _repository.Update(user);
            var sendEndpoint = await _endpointProvider.GetSendEndpoint(new Uri("queue:reset-password"));
            var registerMessage = new ResetPasswordCommand()
            {
                To = email,
                UserId = user.Id,
                Code = randomNumber.ToString()
            };
            sendEndpoint.Send<ResetPasswordCommand>(registerMessage);
            return Response<NoContent>.Success(ResponseStatus.Success);
        }

        public async Task<Response<NoContent>> ResetPasswordCheckOTP(ResetPasswordCheckOTPDto codeDto)
        {
            UserCredential user = await _repository.GetFirstAsync(u => u.Email == codeDto.Mail);
            if (user == null)
            {
                return Response<NoContent>.Fail("User Not Found", ResponseStatus.NotFound);
            }
            if (user.ResetPasswordCode != codeDto.Code)
            {
                return Response<NoContent>.Fail("UnAuthorized", ResponseStatus.Unauthorized);
            }
            return Response<NoContent>.Success(ResponseStatus.Success);
        }


        public async Task<Response<NoContent>> ResetPassword( ResetPasswordDto passwordDto)
        {
            UserCredential? user = await _repository.GetFirstAsync(u => u.Email == passwordDto.Mail);

            if (passwordDto.NewPassword != passwordDto.NewPasswordAgain)
            {
                return Response<NoContent>.Fail("Passwords does not match!", ResponseStatus.BadRequest);
            }
            if (user != null)
            {
                if (user.ResetPasswordTokenEndDate < DateTime.Now)
                {
                    return Response<NoContent>.Fail("Token expired", ResponseStatus.NotAuthenticated);
                }
                if (user.ResetPasswordCode == passwordDto.Code)
                {
                    user.HashedPassword = PasswordFunctions.HashPassword(passwordDto.NewPassword);
                    user.ResetPasswordCode = null;
                    user.ResetPasswordTokenEndDate = null;
                    _repository.Update(user);
                    return Response<NoContent>.Success(ResponseStatus.Success);
                }
            }
            return Response<NoContent>.Fail("Failed", ResponseStatus.Failed);
        }

        public async Task<Response<NoContent>> ChangePassword(string userId, PasswordChangeDto passwordDto)
        {
            UserCredential? user = await _repository.GetFirstAsync(u => u.Id == userId);
            if (user == null)
            {
                return Response<NoContent>.Fail("Not Found", ResponseStatus.NotFound);
            }
            var verifiedPassword = PasswordFunctions.VerifyPassword(passwordDto.OldPassword, user.HashedPassword);
            if (verifiedPassword == false)
            {
                return Response<NoContent>.Fail("Not authenticated", ResponseStatus.NotAuthenticated);
            }
            user.HashedPassword = PasswordFunctions.HashPassword(passwordDto.NewPassword);
            _repository.Update(user);
            return Response<NoContent>.Success(ResponseStatus.Success);
        }

        /// <summary>
        /// Checks if username and email is unique.
        /// </summary>
        /// <param name="userName">UserName must be unique</param>
        /// <param name="email">Email must be unique</param>
        /// <returns></returns>
        public async Task<Response<NoContent>> CheckUserNameAndEmailUnique(string userName, string email)
        {
            DatabaseResponse response = new();
            var _userName =  await _repository.GetFirstAsync(u => u.UserName == userName);
            if (_userName != null)
                response.ErrorMessage.Add("Username must be unique!");

            var _email =  await _repository.GetFirstAsync(u => u.Email == email);
            if (_email != null)
                response.ErrorMessage.Add("Email already in use!");

            if(_userName == null && _email == null)
            {
                return Response<NoContent>.Success(ResponseStatus.Success);
            }
            return Response<NoContent>.Fail(response.ErrorMessage, ResponseStatus.InitialError);
        }


        private void UpdateRefreshToken(UserCredential user, TokenDto token, int month)
        {
            user.RefreshToken = token.RefreshToken;
            user.RefreshTokenEndDate = token.ExpiredAt.AddMonths(month);
            _repository.UpdateRefreshToken(user);
        }
        public async Task<Response<NoContent>> DeleteUser(string id, UserDeleteDto userDto)
        {
            if (!id.IsNullOrEmpty())
            {
                _repository.DeleteById(id);
                return Response<NoContent>.Success(ResponseStatus.Success);
            }
            return Response<NoContent>.Fail("UnAuthorized", ResponseStatus.NotAuthenticated);
        }

        public async Task<Response<NoContent>> UpdateProfile(string userId, UserUpdateDto userDto)
        {
            UserCredential user = await _repository.GetFirstAsync(u => u.Id == userId);
            if (user == null)
            {
                return Response<NoContent>.Fail("User not found", ResponseStatus.NotFound);
            }
            if (user.Id != userId)
            {
                return Response<NoContent>.Fail("UnAuthorized",ResponseStatus.Unauthorized);
            }
            user.UserName = userDto.UserName;
            user.Email = userDto.Email;
            DatabaseResponse response = _repository.Update(user);
            if (response.IsSuccess)
            {
                return Response<NoContent>.Success(ResponseStatus.Success);
            }
            return Response<NoContent>.Fail("Failed", ResponseStatus.InitialError);
        }
        public string GenerateResetPasswordToken(string email)
        {
            byte[] tokenBytes = new byte[32];
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                rng.GetBytes(tokenBytes);
            }
            string token = Convert.ToBase64String(tokenBytes);
            return token;
        }
    }
}