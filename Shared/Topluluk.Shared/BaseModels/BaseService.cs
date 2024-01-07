using Microsoft.AspNetCore.Http;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Helper;

namespace Topluluk.Shared.BaseModels
{
    public class BaseService : IBaseService
    {
        public IHttpContextAccessor _httpContextAccessor { get; set; }

        public string Token => _httpContextAccessor.HttpContext.Request.Headers["Authorization"];
        public string UserId { get { return GetUserId(); } }
       
        public BaseService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            return await HttpRequestHelper.GetUser(this.Token);
        }
        private string GetUserId()
        {
            return TokenHelper.GetUserIdByToken(Token);
        }
    }
}
