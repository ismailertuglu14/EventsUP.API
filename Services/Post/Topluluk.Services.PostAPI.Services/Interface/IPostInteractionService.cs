using Topluluk.Services.PostAPI.Model.Dto;
using Topluluk.Services.PostAPI.Model.Dto.Http;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.PostAPI.Services.Interface
{
    public interface IPostInteractionService
    {
        Task<Response<string>> Interaction(string userId, string postId, PostInteractionCreateDto interactionCreate);
        Task<Response<List<User>>> GetInteractions(string userId, string postId, int type = 0, int take = 10, int skip = 0);
        Task<Response<string>> RemoveInteraction(string userId, string postId);

    }
}
