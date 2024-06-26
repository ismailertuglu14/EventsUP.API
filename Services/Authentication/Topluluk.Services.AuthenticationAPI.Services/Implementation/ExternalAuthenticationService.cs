﻿using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Topluluk.Services.AuthenticationAPI.Data.Interface;
using Topluluk.Services.AuthenticationAPI.Model.Dto;
using Topluluk.Services.AuthenticationAPI.Model.Entity;
using Topluluk.Services.AuthenticationAPI.Services.Helpers;
using Topluluk.Services.AuthenticationAPI.Services.Interface;
using Topluluk.Shared.Constants;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Helper;
using _MassTransit = MassTransit;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;

namespace Topluluk.Services.AuthenticationAPI.Services.Implementation
{
    public class ExternalAuthenticationService : IExternalAuthenticationService
	{

        private readonly IAuthenticationRepository _repository;
        private readonly ILoginLogRepository _loginLogRepository;
        private readonly IConfiguration _configuration;
        private readonly RestClient _client;
        private readonly _MassTransit.ISendEndpointProvider _endpointProvider;
        private readonly TokenHelper _tokenHelper;
        public ExternalAuthenticationService(
            IAuthenticationRepository repository,
            ILoginLogRepository loginLogRepository,
            _MassTransit.ISendEndpointProvider endpointProvider,
            IConfiguration configuration,
            TokenHelper tokenHelper
            )
        {
            _repository = repository;
            _loginLogRepository = loginLogRepository;
            _configuration = configuration;
            _client = new RestClient();
            _endpointProvider = endpointProvider;
            _tokenHelper = tokenHelper;
        }

        public async Task<Response<TokenDto>> SignInWithGoogle(GoogleLoginDto dto)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string> { _configuration["Google:Client_Id"] ?? throw new ArgumentNullException() }
            };
            GoogleJsonWebSignature.Payload? payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            UserCredential? userCredentials = await _repository.GetFirstAsync(x => x.Email == payload.Email && x.Provider == Shared.Enums.LoginProvider.Google);    
            TokenDto token = new();
            if (userCredentials != null)
            {
                token = _tokenHelper.CreateAccessToken(userCredentials.Id, userCredentials.UserName, userCredentials.Role);
                return Response<TokenDto>.Success(token, ResponseStatus.Success);
            }
            string newUserName = payload.Name.Replace(" ", "");
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            if (await _repository.GetFirstAsync(x => x.UserName == newUserName) != null)
                newUserName += timestamp;
            
            UserCredential credential = new()
            {
                Email = payload.Email,
                Provider = Shared.Enums.LoginProvider.Google,
                UserName = newUserName,
                EmailConfirmed = payload.EmailVerified,
                HashedPassword = PasswordFunctions.HashPassword(newUserName)
            };

            UserInsertDto insertUserDto = new() {
                Id = credential.Id,
                FullName = payload.GivenName,
                UserName = newUserName,
                Email = payload.Email,
                ProfileImage = payload.Picture,
                Gender = Shared.Enums.GenderEnum.Unspecified
            };

            var request = new RestRequest(ServiceConstants.API_GATEWAY + "/user/InsertUser").AddBody(insertUserDto);
            var response = await _client.ExecutePostAsync<Response<string>>(request);

            if (!response.IsSuccessful || response.Data == null)
                throw new Exception("Kullanıcı insert edilirken beklenmeyen bir hata oluştu.");
            await _repository.InsertAsync(credential);
            token = _tokenHelper.CreateAccessToken(credential.Id, newUserName, credential.Role);
            SendRegisteredMail sendRegisteredMail = new(_endpointProvider);
            await sendRegisteredMail.send(payload.GivenName + " " + payload.FamilyName, payload.Email);
            return Response<TokenDto>.Success(token ,ResponseStatus.Success);
        }
        public Task<Response<NoContent>> SignInWithApple()
        {
            throw new NotImplementedException();
        }
    }
}

