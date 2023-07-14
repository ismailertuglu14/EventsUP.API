using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Topluluk.Services.AuthenticationAPI.Data.Interface;
using Topluluk.Services.AuthenticationAPI.Model.Dto;
using Topluluk.Services.AuthenticationAPI.Model.Entity;
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
        private readonly IAuthenticationService _authenticationService;

        public ExternalAuthenticationService(IAuthenticationRepository repository, ILoginLogRepository loginLogRepository, IConfiguration configuration,
            _MassTransit.ISendEndpointProvider endpointProvider, IAuthenticationService authenticationService)
        {
            _repository = repository;
            _loginLogRepository = loginLogRepository;
            _configuration = configuration;
            _client = new RestClient();
            _endpointProvider = endpointProvider;
            _authenticationService = authenticationService;
        }

        public async Task<Response<TokenDto>> SignInWithGoogle(GoogleLoginDto dto)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {

                Audience = new List<string> { _configuration["Google:Client_Id"] ?? throw new ArgumentNullException()   }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);

            var userCredentials = await _repository.GetFirstAsync(x => x.Email == payload.Email);

            TokenHelper tokenHelper = new TokenHelper(_configuration);
            TokenDto token = new();

            if (userCredentials != null)
            {
                token = tokenHelper.CreateAccessToken(userCredentials.Id, userCredentials.UserName, userCredentials.Role);
                return Response<TokenDto>.Success(token, ResponseStatus.Success);
            }

            string newUserName = payload.Name.Replace(" ", "");
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            
            if (await _repository.GetFirstAsync(x => x.UserName == newUserName) != null)
            {
                newUserName += timestamp;
            }

            UserCredential credential = new()
            {
                Email = payload.Email,
                Provider = Shared.Enums.LoginProvider.Google,
                UserName = newUserName,
                EmailConfirmed = payload.EmailVerified,
                HashedPassword = HashPassword(newUserName)
            };

            UserInsertDto insertUserDto = new() {
                Id = credential.Id,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
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
            token = tokenHelper.CreateAccessToken(credential.Id, newUserName, credential.Role);

            return Response<TokenDto>.Success(token ,ResponseStatus.Success);
        }

        public Task<Response<NoContent>> SignInWithApple()
        {
            throw new NotImplementedException();
        }

        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = GetHash(password, salt);
            return Convert.ToBase64String(salt) + "|" + Convert.ToBase64String(hash);
        }

        private byte[] GetHash(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] passwordAndSalt = new byte[passwordBytes.Length + salt.Length];
                Buffer.BlockCopy(passwordBytes, 0, passwordAndSalt, 0, passwordBytes.Length);
                Buffer.BlockCopy(salt, 0, passwordAndSalt, passwordBytes.Length, salt.Length);
                return sha256.ComputeHash(passwordAndSalt);
            }
        }

    }
}

