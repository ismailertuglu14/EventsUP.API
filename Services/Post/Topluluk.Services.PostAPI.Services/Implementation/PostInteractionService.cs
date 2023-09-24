using AutoMapper;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Net;
using Topluluk.Services.PostAPI.Data.Interface;
using Topluluk.Services.PostAPI.Model.Dto;
using Topluluk.Services.PostAPI.Model.Entity;
using Topluluk.Services.PostAPI.Services.Interface;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Enums;
using Topluluk.Shared.Helper;

namespace Topluluk.Services.PostAPI.Services.Implementation
{
    public class PostInteractionService : IPostInteractionService
    {
        private readonly IPostRepository _postRepository;
        private readonly IPostInteractionRepository _postInteractionRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public PostInteractionService(IPostRepository postRepository, IPostInteractionRepository postInteractionRepository, IHttpContextAccessor httpContextAccessor)
        {
            _postRepository = postRepository;
            _postInteractionRepository = postInteractionRepository;
            _httpContextAccessor = httpContextAccessor;
        }
        private string Token => _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
        public async Task<Response<string>> Interaction(string userId, string postId, PostInteractionCreateDto interactionCreate)
        {

            User? user = await HttpRequestHelper.GetUser(Token);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            Post? post = await _postRepository.GetFirstAsync(p => p.Id == postId);
            if (post == null) throw new Exception("Post not found");
            if (!Enum.IsDefined(typeof(InteractionEnum), interactionCreate.InteractionType))
            {
                return await Task.FromResult(Response<string>.Fail("Invalid InteractionType value", ResponseStatus.BadRequest));
            }
            PostInteraction _interaction = new()
            {
                PostId = postId,
                User = user,
                InteractionType = interactionCreate.InteractionType
            };
            PostInteraction? _interactionDb = await _postInteractionRepository.GetFirstAsync(i => i.PostId == postId && i.User.Id == userId);
            if (_interactionDb == null)
            {
                await _postInteractionRepository.InsertAsync(_interaction);
                return await Task.FromResult(Response<string>.Success("Success", Shared.Enums.ResponseStatus.Success));
            }
            _postInteractionRepository.DeleteCompletely(_interactionDb.Id);
            await _postInteractionRepository.InsertAsync(_interaction);
            return await Task.FromResult(Response<string>.Success("Success", Shared.Enums.ResponseStatus.Success));
        }

        public async Task<Response<List<User>>> GetInteractions(string userId, string postId, int type = 0, int take = 10, int skip = 0)
        {

            Post? post = await _postRepository.GetFirstAsync(p => p.Id == postId);
            if (post == null) return Response<List<User>>.Fail("", ResponseStatus.NotFound);

            List<PostInteraction> interactions = _postInteractionRepository.GetListByExpressionPaginated(skip, take, i => i.PostId == postId && (int)i.InteractionType == type);

            var interactionDtos = new List<User>();

            foreach (var interaction in interactions)
            {
                interactionDtos.Add(interaction.User);
            }

            return Response<List<User>>.Success(interactionDtos, ResponseStatus.Success);
        }

        public async Task<Response<string>> RemoveInteraction(string userId, string postId)
        {

            PostInteraction? interaction = await _postInteractionRepository.GetFirstAsync(pi => pi.PostId == postId && pi.User.Id == userId);

            if (interaction == null || interaction.User.Id != userId)
                return await Task.FromResult(Response<string>.Fail("Not found", ResponseStatus.NotFound));

            _postInteractionRepository.DeleteCompletely(interaction.Id);
            return await Task.FromResult(Response<string>.Success("Success", ResponseStatus.Success));

        }


    }
}
