using AutoMapper;
using Topluluk.Services.PostAPI.Data.Interface;
using Topluluk.Services.PostAPI.Model.Dto;
using Topluluk.Services.PostAPI.Model.Entity;
using Topluluk.Services.PostAPI.Services.Interface;
using Topluluk.Shared.Dtos;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Topluluk.Services.PostAPI.Model.Dto.Http;
using RestSharp;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Topluluk.Shared.Constants;
using Topluluk.Shared.Exceptions;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;
using Topluluk.Shared.Helper;

namespace Topluluk.Services.PostAPI.Services.Implementation
{
	public class PostService : IPostService
	{
        private readonly IPostRepository _postRepository;
        private readonly ISavedPostRepository _savedPostRepository;
        private readonly IPostInteractionRepository _postInteractionRepository;
        private readonly IPostCommentRepository _commentRepository;
        private readonly IMapper _mapper;
        private readonly RestClient _client;
        private readonly IMongoClient _mongoClient;
        public PostService(IPostRepository postRepository, IPostInteractionRepository postInteractionRepository, ISavedPostRepository savedPostRepository, IPostCommentRepository commentRepository, IMapper mapper, IMongoClient mongoClient)
        {
            _postRepository = postRepository;
            _savedPostRepository = savedPostRepository;
            _postInteractionRepository = postInteractionRepository;
            _mapper = mapper;
            _mongoClient = mongoClient;
            _commentRepository = commentRepository;
            _client = new RestClient();
        }
        public async Task<Response<string>> SavePost(string token, string userId, string postId)
        {
            User? user = await HttpRequestHelper.GetUser(token);
            if(user == null) throw new UnauthorizedAccessException("User not found");

            SavedPost? _savedPost = await _savedPostRepository.GetFirstAsync(sp => sp.PostId == postId);
            if (_savedPost == null)
            {
                SavedPost savedPost = new SavedPost
                {
                    PostId = postId,
                    User = user
                };
                DatabaseResponse response = await _savedPostRepository.InsertAsync(savedPost);

                if (response.IsSuccess == false) throw new Exception("Some error occurred");

                return await Task.FromResult(Response<string>.Success("Success", ResponseStatus.Success));
            }
            else
            {
                _savedPostRepository.Delete(_savedPost.Id);
                return await Task.FromResult(Response<string>.Success("Success", ResponseStatus.Success));
            }

        }
        public async Task<Response<List<GetPostForFeedDto>>> GetPostForFeedScreen(string userId, string token,
            int skip = 0, int take = 10)
        {
            var getUserFollowingsRequest = new RestRequest(ServiceConstants.API_GATEWAY + "/user/user-followings")
                .AddHeader("Authorization",token)
                .AddQueryParameter("id", userId);
            var getUserFollowingsTask = _client.ExecuteGetAsync<Response<List<string>>>(getUserFollowingsRequest);
            var userCommunitiesRequest = new RestRequest(ServiceConstants.API_GATEWAY + "/community/user-communities").AddQueryParameter("id", userId);
            var userCommunitiesTask = _client.ExecuteGetAsync<Response<List<CommunityInfoPostLinkDto>>>(userCommunitiesRequest);

            await Task.WhenAll(getUserFollowingsTask, userCommunitiesTask);


            // Id list of users followed by Source User
            var getUserFollowingsResponse = getUserFollowingsTask.Result.Data.Data;
            List<string>? communityIds = userCommunitiesTask.Result?.Data?.Data?.Select(c => c.Id).ToList();
            List<Post>? posts = new();

            if(getUserFollowingsResponse != null)
            {
                if (communityIds?.Count > 0)
                {
                    posts = await _postRepository.GetPostsWithDescending(skip, take,
                       p => !p.IsDeleted
                            && (getUserFollowingsResponse.Contains(p.User.Id) || p.User.Id == userId)
                            && (p.CommunityId == null || communityIds.Contains(p.CommunityId))
                            );
                }
                else
                {
                    posts = await _postRepository.GetPostsWithDescending(skip, take,
                        p => !p.IsDeleted
                             && (getUserFollowingsResponse.Contains(p.User.Id) || p.User.Id == userId));
                }
            }
            else
            {
                /// Rastgele post gösterecek şekilde düzenle.
                posts = await _postRepository.GetPostsWithDescending(skip, take,
                                           p => !p.IsDeleted
                                           && (p.User.Id == userId));
            }

            List<string> postIds = posts.Select(p => p.Id).ToList();
            var interactedTask = _postInteractionRepository.IsUserInteractedPosts(userId, postIds);
            var interactionCountsTask = _postInteractionRepository.PostsInteractionCounts(postIds);
            var postCommentCountsTask = _commentRepository.GetPostCommentCounts(postIds);
            var postSavedTask = _savedPostRepository.IsUserSavedPosts(userId, postIds);
            var interactionsTask = _postInteractionRepository.GetPostInteractionPreviews(postIds);
            await Task.WhenAll(interactionsTask, postCommentCountsTask, postSavedTask, interactionCountsTask, interactedTask, getUserFollowingsTask);

            var postDtos = _mapper.Map<List<Post>, List<GetPostForFeedDto>>(posts);


            for (int i = 0; i < posts.Count; i++)
            {

                var postDto = postDtos.FirstOrDefault(p => p.Id == postDtos[i].Id);
                postDto.IsFollowing = getUserFollowingsResponse.Contains(postDto.User.Id);
                postDto.InteractionCount = interactionCountsTask.Result.TryGetValue(postDto.Id, out var value)
                    ? value
                    : 0;
                postDto.CommentCount = postCommentCountsTask.Result.ContainsKey(postDto.Id) ? postCommentCountsTask.Result[postDto.Id] : 0;

                postDto.IsSaved = postSavedTask.Result.ContainsKey(postDto.Id) && postSavedTask.Result[postDto.Id];

                if (interactedTask.Result.ContainsKey(postDto.Id))
                {
                    postDto.IsInteracted = new PostInteractedDto()
                    {
                        Interaction = interactedTask.Result[postDto.Id].InteractionType
                    };
                }
                var interactions = interactionsTask.Result != null && interactionsTask.Result.TryGetValue(postDto.Id, out var interactionArray)
                    ? interactionArray.ToList()
                    : new List<PostInteractionPreviewDto>();

                postDto!.InteractionPreviews = interactions;

                if (!posts[i].CommunityLink.IsNullOrEmpty())
                {
                    // Get-community-title and image request
                    var communityInfoRequest =
                        new RestRequest("https://localhost:7149/api/community/community-info-post-link")
                            .AddQueryParameter("id", posts[i].CommunityLink);
                    var communityInfoTask =
                        _client.ExecuteGetAsync<Response<CommunityInfoPostLinkDto>>(communityInfoRequest);
                }
            }

            return Response<List<GetPostForFeedDto>>.Success(postDtos, ResponseStatus.Success);
        }

        public async Task<Response<string>> Create(string token, string userId, CreatePostDto postDto)
        {
            User? user = await HttpRequestHelper.GetUser(token);
            if (user == null)
            {
                return await Task.FromResult(Response<string>.Fail("User not found", ResponseStatus.NotFound));
            }

            Post post = _mapper.Map<Post>(postDto);
            DatabaseResponse response = new();

            post.User = user;

            // If communityId given
            if (!postDto.CommunityId.IsNullOrEmpty())
            {
                var isUserParticipiantRequest =
               new RestRequest(ServiceConstants.API_GATEWAY + $"/Community/user-communities")
               .AddQueryParameter("id", userId)
               .AddQueryParameter("skip", 0)
               .AddQueryParameter("take", 100);

                var isUserParticipiantResponse =
                    await _client.ExecuteGetAsync<Response<List<CommunityInfoPostLinkDto>>>(isUserParticipiantRequest);

                // Community servisi ayakta olmayabilir.
                if (!isUserParticipiantResponse.IsSuccessful || !isUserParticipiantResponse.Data.Data.Any(c => c.Id == postDto.CommunityId))
                {
                    throw new UnauthorizedAccessException("User not participiant of this community.");
                }
                post.CommunityId = postDto.CommunityId;
            }

            Response<List<string>>? responseData = new();
            if (postDto.Files != null)
            {
                using (var client = new HttpClient())
                {

                    var imageContent = new MultipartFormDataContent();
                    foreach (var postDtoFile in postDto.Files)
                    {
                        var stream = new MemoryStream();
                        postDtoFile.CopyTo(stream);
                        var fileContent = new ByteArrayContent(stream.ToArray());
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                        imageContent.Add(fileContent, "Files", postDtoFile.FileName);
                    }


                    var responseClient =
                        await client.PostAsync("https://localhost:7165/file/upload-post-files", imageContent);

                    if (responseClient.IsSuccessStatusCode)
                    {
                        responseData = await responseClient.Content.ReadFromJsonAsync<Response<List<string>>>();

                        if (responseData != null)
                        {
                            foreach (var file in responseData.Data)
                            {
                                post.Files.Add(new FileModel(file));
                            }
                        }
                    }
                    else
                    {
                        return await Task.FromResult(Response<string>.Fail(
                            "Failed while uploading image with http client", ResponseStatus.InitialError));
                    }

                }
            }
            response = await _postRepository.InsertAsync(post);
            return Response<string>.Success(response.Data, ResponseStatus.Success);
        }

        public async Task<Response<string>> Delete(PostDeleteDto postDto)
        {
            IClientSessionHandle session = null;

            try
            {
                session = await _mongoClient.StartSessionAsync();
                session.StartTransaction();

                Post post = await _postRepository.GetFirstAsync(p => p.Id == postDto.PostId);
                if (post != null)
                {
                    _postInteractionRepository.DeleteByExpression(p => p.PostId == post.Id);
                    _commentRepository.DeleteByExpression(p =>p.PostId == post.Id);
                    _savedPostRepository.DeleteByExpression(p => p.PostId == post.Id);
                    _postRepository.DeleteById(post.Id);
                }

                // commit transaction
                await session.CommitTransactionAsync();

                return Response<string>.Success("Success", Shared.Enums.ResponseStatus.Success);

            }
            catch (Exception ex)
            {
                // rollback transaction
                await session!.AbortTransactionAsync();

                return Response<string>.Fail("Failed", Shared.Enums.ResponseStatus.InitialError);
            }
        }




        public async Task<Response<List<GetPostForFeedDto>>> GetCommunityPosts(string userId, string communityId, int skip = 0, int take = 10)
        {
            var posts = await _postRepository.GetPostsWithDescending(
                skip, take, p => !p.IsDeleted && p.CommunityId == communityId);
            if (posts == null)
            {
                return Response<List<GetPostForFeedDto>>.Success(new() { }, ResponseStatus.Success);
            }

            var postDtos = _mapper.Map<List<Post>, List<GetPostForFeedDto>>(posts);

            var postIds = postDtos.Select(p => p.Id).ToList();
            var interactedTask = _postInteractionRepository.IsUserInteractedPosts(userId, postIds);
            var interactionCountsTask = _postInteractionRepository.PostsInteractionCounts(postIds);
            var postCommentCountsTask = _commentRepository.GetPostCommentCounts(postIds);
            var postSavedTask = _savedPostRepository.IsUserSavedPosts(userId, postIds);
            var interactionsTask = _postInteractionRepository.GetPostInteractionPreviews(postIds);

            await Task.WhenAll(interactionsTask, postCommentCountsTask, postSavedTask, interactionCountsTask, interactedTask);

            foreach (var post in postDtos)
            {
                post.InteractionCount = interactionCountsTask.Result.ContainsKey(post.Id) == true
                    ? interactionCountsTask.Result[post.Id]
                    : 0;
                post.CommentCount = postCommentCountsTask.Result.ContainsKey(post.Id) ? postCommentCountsTask.Result[post.Id] : 0;
                post.IsSaved = postSavedTask.Result.ContainsKey(post.Id) ? postSavedTask.Result[post.Id] : false;
                if (interactedTask.Result.ContainsKey(post.Id))
                {
                    post.IsInteracted = new PostInteractedDto()
                    {
                        Interaction = interactedTask.Result[post.Id].InteractionType
                    };
                }

                var interactions = interactionsTask.Result != null && interactionsTask.Result.TryGetValue(post.Id, out var interactionArray)
                    ? interactionArray.ToList()
                    : new List<PostInteractionPreviewDto>();

                post!.InteractionPreviews = interactions;
            }
            return Response<List<GetPostForFeedDto>>.Success(postDtos, ResponseStatus.Success);
        }

        public async Task<Response<GetPostByIdDto>> GetPostById(string postId, string sourceUserId, bool isDeleted = false)
        {
            var post = await _postRepository.GetFirstAsync(p => p.Id == postId);
            if (post == null)
                throw new NotFoundException();
            var postDto = _mapper.Map<GetPostByIdDto>(post);
            var postIds = new List<string>
                {
                    postDto.Id
                };
            var interactionCountTask = _postInteractionRepository.PostsInteractionCounts(postIds);
            var interactionPreviewsTask = _postInteractionRepository.GetPostInteractionPreviews(postIds);
            var isSavedTask = _savedPostRepository.AnyAsync(p => !p.IsDeleted && p.PostId == postId && p.User.Id == sourceUserId);
            var isInteractedTask = _postInteractionRepository.GetFirstAsync(p => p.PostId == p.Id && p.User.Id == sourceUserId);
            var commentCountTask = _commentRepository.Count(c => !c.IsDeleted && c.PostId == postId && (c.ParentCommentId == "" || c.ParentCommentId == null));

            await Task.WhenAll(interactionPreviewsTask, commentCountTask, isSavedTask, isInteractedTask, interactionCountTask);

            postDto.IsSaved = isSavedTask.Result;

            postDto.InteractionCount = interactionCountTask.Result.ContainsKey(postDto.Id) ? interactionCountTask.Result[postDto.Id] : 0;
            var interactions = interactionPreviewsTask.Result != null && interactionPreviewsTask.Result.TryGetValue(postDto.Id, out var interactionArray)
                ? interactionArray.ToList()
                : new List<PostInteractionPreviewDto>();

            postDto!.InteractionPreviews = interactions;

            if (isInteractedTask.Result != null)
            {
                postDto.IsInteracted = new PostInteractedDto()
                {
                    Interaction = isInteractedTask.Result.InteractionType
                };
            }


            var comments = _commentRepository.GetAllAsync(10, 0, c => c.PostId == postId && (c.ParentCommentId == "" || c.ParentCommentId == null)).Result.Data as List<PostComment>;
            var commentReplyCounts = await _commentRepository.GetCommentsReplyCounts(comments.Select(c => c.Id).ToList());


            if (comments != null && comments.Count > 0)
            {
                postDto.CommentCount = commentCountTask.Result;
                List<CommentGetDto> commentDtos = _mapper.Map<List<PostComment>, List<CommentGetDto>>(comments);

                foreach (var commentDto in commentDtos)
                {
                    commentDto.IsEdited =
                        comments.FirstOrDefault(c => c.Id == commentDto.Id)!.PreviousMessages != null && comments.FirstOrDefault(c => c.Id == commentDto.Id)!.PreviousMessages.Count != 0;

                    commentDto.ReplyCount = commentReplyCounts.TryGetValue(commentDto.Id, out int value)
                        ? value
                        : 0;
                    postDto.Comments?.Add(commentDto);
                }
            }

            if (post.CommunityId != null)
            {
                // Get community title request
                var communityGetTitleRequest = new RestRequest("https://localhost:7132/Community/getCommunityTitle").AddParameter("id", post.CommunityId);
                var communityGetTitleResponse = await _client.ExecuteGetAsync<Response<string>>(communityGetTitleRequest);
                var communityResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Response<string>>(communityGetTitleResponse.Content);
                postDto.CommunityTitle = communityResponse?.Data;
            }

            return Response<GetPostByIdDto>.Success(postDto, ResponseStatus.Success);

        }

        // Kullanıcı ekranında kullanıcının paylaşımlarını listelemek için kullanılacak action
        public async Task<Response<List<GetPostForFeedDto>>> GetUserPosts(string userId, string id, int take = 10,
            int skip = 0)
        {
            var posts = await _postRepository.GetPostsWithDescending(skip, take, p => !p.IsDeleted && p.User.Id == id);
            List<GetPostForFeedDto> dtos = _mapper.Map<List<Post>, List<GetPostForFeedDto>>(posts);
            List<string> postIds = posts.Select(p => p.Id).ToList();

            var interactedTask = _postInteractionRepository.IsUserInteractedPosts(userId, postIds);
            var interactionCountsTask = _postInteractionRepository.PostsInteractionCounts(postIds);
            var postCommentCountsTask = _commentRepository.GetPostCommentCounts(postIds);
            var postSavedTask = _savedPostRepository.IsUserSavedPosts(userId, postIds);
            var interactionsTask = _postInteractionRepository.GetPostInteractionPreviews(postIds);
            await Task.WhenAll(interactionsTask, postCommentCountsTask, postSavedTask, interactionCountsTask, interactedTask);

            foreach (var dto in dtos)
            {

                dto.InteractionCount = interactionCountsTask.Result.ContainsKey(dto.Id) == true
                    ? interactionCountsTask.Result[dto.Id]
                    : 0;
                dto.CommentCount = postCommentCountsTask.Result.ContainsKey(dto.Id) ? postCommentCountsTask.Result[dto.Id] : 0;
                dto.IsSaved = postSavedTask.Result.ContainsKey(dto.Id) ? postSavedTask.Result[dto.Id] : false;
                if (interactedTask.Result.ContainsKey(dto.Id))
                {
                    dto.IsInteracted = new PostInteractedDto()
                    {
                        Interaction = interactedTask.Result[dto.Id].InteractionType
                    };
                }
                var interactions = interactionsTask.Result != null && interactionsTask.Result.TryGetValue(dto.Id, out var interactionArray)
                ? interactionArray.ToList()
                : new List<PostInteractionPreviewDto>();
                dto!.InteractionPreviews = interactions;

            }
            return Response<List<GetPostForFeedDto>>.Success(dtos, ResponseStatus.Success);
        }
        public Task<Response<string>> Update()
        {
            throw new NotImplementedException();
        }

        public Task<Response<string>> UpdateComment(string userId, string commentId, string newComment)
        {
            throw new NotImplementedException();
        }
        public async Task<Response<List<GetPostForFeedDto>>> GetSavedPosts(string userId, int take = 10, int skip = 0)
        {
            try
            {
                if (userId == null)
                {
                    return Response<List<GetPostForFeedDto>>.Fail("User not found", ResponseStatus.NotFound);
                }

                var savedPostResponse = await _savedPostRepository.GetAllAsync(take, skip, sp => sp.User.Id == userId);
                var savedPosts = savedPostResponse.Data as List<SavedPost>;

                var postIds = savedPosts.Select(p => p.PostId).ToList();
                var postResponse = await _postRepository.GetAllAsync(take, skip, p => postIds.Contains(p.Id));
                if (postResponse.Data == null || (postResponse.Data as List<Post>).Count == 0)
                {
                    return Response<List<GetPostForFeedDto>>.Success(new(), ResponseStatus.Success);
                }
                var posts = postResponse.Data as List<Post>;

                var dtos = savedPosts.Select(dto =>
                {
                    var post = posts.FirstOrDefault(p => p.Id == dto.PostId);
                    var _dto = _mapper.Map<GetPostForFeedDto>(post);
                    _dto.Id = post!.Id;
                    _dto.Description = post.Description;
                    _dto.IsSaved = true;
                    return _dto;
                }).ToList();

                return await Task.FromResult(Response<List<GetPostForFeedDto>>.Success(dtos,ResponseStatus.Success));

            }
            catch (Exception e)
            {
                return await Task.FromResult(Response<List<GetPostForFeedDto>>.Fail($"Some error occurred: {e}",
                    ResponseStatus.InitialError));
            }

         }

        public async Task<Response<bool>> DeletePosts(string userId)
        {
            try
            {
                if (!userId.IsNullOrEmpty())
                {
                    bool result = await _postRepository.DeletePosts(userId);

                    if (result == true)
                    {
                        bool result2 = await _commentRepository.DeletePostsComments(userId);
                        bool result3 = await _savedPostRepository.DeleteSavedPostsByUserId(userId);
                    }

                    return await Task.FromResult(Response<bool>.Success(result, ResponseStatus.Success));
                }
                else
                {
                    return await Task.FromResult(Response<bool>.Fail("Not authorized", ResponseStatus.BadRequest));
                }
            }
            catch (Exception e)
            {
                return await Task.FromResult(Response<bool>.Fail($"Some error occurred: {e}",
                    ResponseStatus.InitialError));
            }
        }
    }
}

