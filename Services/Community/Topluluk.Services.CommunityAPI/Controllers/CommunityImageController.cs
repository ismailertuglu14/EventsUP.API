using Microsoft.AspNetCore.Mvc;
using Topluluk.Services.CommunityAPI.Services.Interface;
using Topluluk.Shared.BaseModels;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.CommunityAPI.Controllers;

[Route("Community")]
public class CommunityImageController : BaseController
{
    private readonly ICommunityImageService _communityService;

    public CommunityImageController(ICommunityImageService communityService)
    {
        _communityService = communityService;
    }


    [HttpPost("{communityId}/update-cover-image")]
    public async Task<Response<string>> UpdateCoverImage(string communityId, [FromForm] IFormFile file)
    {
        return await _communityService.UpdateCoverImage(this.UserId,communityId, file);
    }

    [HttpPost("{communityId}/update-banner-image")]
    public async Task<Response<string>> UpdateBannerImage(string communityId, [FromForm] IFormFile file)
    {
        return await _communityService.UpdateBannerImage(this.UserId,communityId, file);
    }

    [HttpPost("{communityId}/remove-banner-image")]
    public async Task<Response<NoContent>> RemoveBannerImage(string communityId)
    {
        return await _communityService.RemoveBannerImage(this.UserId,communityId);
    }

    [HttpPost("{communityId}/remove-cover-image")]
    public async Task<Response<NoContent>> RemoveCoverImage(string communityId)
    {
        return await _communityService.RemoveCoverImage(this.UserId,communityId);
    }
}