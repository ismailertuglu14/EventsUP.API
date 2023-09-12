using Microsoft.AspNetCore.Http;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.CommunityAPI.Services.Interface;

public interface ICommunityImageService
{
    Task<Response<string>> UpdateCoverImage(string userId, string communityId, IFormFile dto);
    Task<Response<string>> UpdateBannerImage(string userId, string communityId, IFormFile dto);
    Task<Response<NoContent>> RemoveCoverImage(string userId, string communityId);
    Task<Response<NoContent>> RemoveBannerImage(string userId, string communityId);

}