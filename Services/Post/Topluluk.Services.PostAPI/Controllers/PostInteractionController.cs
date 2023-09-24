using Microsoft.AspNetCore.Mvc;
using Topluluk.Services.PostAPI.Model.Dto;
using Topluluk.Services.PostAPI.Services.Interface;
using Topluluk.Shared.BaseModels;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.PostAPI.Controllers
{

    [ApiController]
    [Route("Post")]
    public class PostInteractionController : BaseController
    {
        private readonly IPostInteractionService _postInteractionService;

        public PostInteractionController(IPostInteractionService postInteractionService)
        {
            _postInteractionService = postInteractionService;
        }

        [HttpGet("interactions/{postId}")]
        public async Task<Response<List<User>>> GetInteractions(string postId, int type, int take, int skip)
        {
            return await _postInteractionService.GetInteractions(this.UserId, postId, type, take, skip);
        }
        [HttpPost("interaction/{postId}")]
        public async Task<Response<string>> Interaction(string postId, PostInteractionCreateDto createDto)
        {
            return await _postInteractionService.Interaction(this.UserId, postId, createDto);
        }
        [HttpPost("remove-interaction/{postId}")]
        public async Task<Response<string>> RemoveInteraction(string postId)
        {
            return await _postInteractionService.RemoveInteraction(this.UserId, postId);
        }
    }
}
