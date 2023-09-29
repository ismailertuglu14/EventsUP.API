
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using Topluluk.Services.CommunityAPI.Data.Interface;
using Topluluk.Services.CommunityAPI.Model.Dto;
using Topluluk.Services.CommunityAPI.Model.Dto.Http;
using Topluluk.Services.CommunityAPI.Model.Entity;
using Topluluk.Services.CommunityAPI.Services.Interface;
using Topluluk.Shared.Constants;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Exceptions;
using Topluluk.Shared.Helper;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;

namespace Topluluk.Services.CommunityAPI.Services.Implementation
{
    public class CommunityService : ICommunityService
    {
        private readonly ICommunityRepository _communityRepository;
        private readonly IMapper _mapper;
        private readonly ICommunityParticipiantRepository _participiantRepository;
        private readonly RestClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommunityService(ICommunityRepository communityRepository, IMapper mapper, ICommunityParticipiantRepository participiantRepository, IHttpContextAccessor httpContextAccessor)
        {
            _communityRepository = communityRepository;
            _participiantRepository = participiantRepository;
            _mapper = mapper;
            _client = new RestClient();
            _httpContextAccessor = httpContextAccessor;
        }
        private string? Token => _httpContextAccessor.HttpContext.Request.Headers["Authorization"];

        public async Task<Response<List<CommunityGetPreviewDto>>> GetCommunitySuggestions(string userId, HttpRequest request, int skip = 0, int take = 5)
        {
            try
            {
                DatabaseResponse response = await _communityRepository.GetAllAsync(take, skip, c => c.IsPublic != false
                                                                        && c.IsVisible != false );
                List<CommunityGetPreviewDto> dtos = _mapper.Map<List<CommunityGetPreviewDto>>(response.Data);
                var communityIds = dtos.Select(c => c.Id).ToList();
                var communityParticipiants = await _participiantRepository.GetCommunityParticipiants(communityIds);
                foreach (var dto in dtos)
                {
                    dto.ParticipiantsCount = communityParticipiants.FirstOrDefault(c => c.Key == dto.Id).Value;
                }
                return await Task.FromResult(Response<List<CommunityGetPreviewDto>>.Success(dtos, ResponseStatus.Success));
            }
            catch(Exception e)
            {
                return await Task.FromResult(Response<List<CommunityGetPreviewDto>>.Fail($"Some error occured: {e}", ResponseStatus.InitialError));

            }
        }
        public async Task<Response<List<CommunityInfoPostLinkDto>>> GetParticpiantsCommunities(string userId, string token)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                return await Task.FromResult(Response<List<CommunityInfoPostLinkDto>>.Fail($"Some error occured: {e}", ResponseStatus.InitialError));

            }
        }


        public async Task<Response<int>> GetUserParticipiantCommunitiesCount(string userId)
        {
            try
            {
                int count = await _participiantRepository.Count(cp => !cp.IsDeleted && cp.User.Id == userId);
                return Response<int>.Success(count, ResponseStatus.Success);
            }
            catch (Exception e)
            {
                return Response<int>.Fail($"Some error occurred: {e}", ResponseStatus.InitialError);
            }
        }

        public async Task<Response<CommunityGetByIdDto>> GetCommunityById(string userId, string token, string communityId)
        {
            Community? community = await _communityRepository.GetFirstCommunity(c => c.Id == communityId && c.IsVisible == true && c.IsRestricted == false);
            CommunityGetByIdDto _community = new();
            if (community == null || community.IsDeleted)
            {
                return await Task.FromResult(Response<CommunityGetByIdDto>.Fail("Not found",ResponseStatus.NotFound));
            }

            if (community.IsRestricted )
            {
                return await Task.FromResult(Response<CommunityGetByIdDto>.Fail("Restricted", ResponseStatus.NotFound));
            }

            if (community.IsVisible == false)
            {
                return await Task.FromResult(Response<CommunityGetByIdDto>.Fail("Not Visible public", ResponseStatus.NotFound));
            }


            var participiantCountTask = _participiantRepository.Count(p => !p.IsDeleted && p.CommunityId == community.Id && p.Status == ParticipiantStatus.ACCEPTED);
            var IsParticipiantTask =  _participiantRepository.AnyAsync(p => !p.IsDeleted && p.CommunityId == communityId && p.User.Id == userId && p.Status == ParticipiantStatus.ACCEPTED);

            await Task.WhenAll( participiantCountTask, IsParticipiantTask);
            _community.Id = communityId;
            _community.Admin = community.Admin;
            _community.Location = community.Location ?? "";
            _community.Title = community.Title;
            _community.Description = community.Description;
            _community.IsOwner = community.Admin.Id == userId ? true : false;
            _community.CoverImage = community.CoverImage;
            _community.BannerImage = community.BannerImage;
            _community.ParticipiantsCount = participiantCountTask.Result;
            _community.IsParticipiant = IsParticipiantTask.Result;
            return await Task.FromResult(Response<CommunityGetByIdDto>.Success(_community, ResponseStatus.Success));
        }



        public async Task<Response<string>> Create(string userId,string token, CommunityCreateDto communityInfo)
        {

            string slug = StringToSlugConvert(communityInfo.Title);
            bool isSluqUnique = false;
            byte index = 0;
            Community? existingCommunity = await _communityRepository.GetFirstAsync(c => c.Slug == slug);
            if (existingCommunity != null)
            {
                // If the slug already exists in the database, add a number to the end of the slug and save the new entity
                int number = 1;
                string newSlug = $"{slug}-{number}";
                while (await _communityRepository.GetFirstAsync(c=> c.Slug == newSlug) != null)
                {
                    number++;
                    newSlug = $"{slug}-{number}";
                }
                slug = newSlug;
            }

            Community community = _mapper.Map<Community>(communityInfo);
            community.Slug = slug;
            community.Admin.Id = userId;

            if (communityInfo.CoverImage != null)
            {
                byte[] imageBytes;

                using (var stream = new MemoryStream())
                {
                    communityInfo.CoverImage.CopyTo(stream);
                    imageBytes = stream.ToArray();
                }
                using (var client = new HttpClient())
                {
                    var content = new MultipartFormDataContent();
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                    content.Add(imageContent, "file", communityInfo.CoverImage.FileName);
                    var imageResponse = await client.PostAsync(ServiceConstants.API_GATEWAY + "/file/upload-community-cover-image", content);

                    if (imageResponse.IsSuccessStatusCode)
                    {
                        var responseData = await imageResponse.Content.ReadFromJsonAsync<Response<string>>();

                        if (responseData.Data != null)
                        {
                            community.CoverImage = responseData.Data;
                        }
                    }
                }

            }


            if (communityInfo.BannerImage != null)
            {
                byte[] imageBytes;

                using (var stream = new MemoryStream())
                {
                    communityInfo.BannerImage.CopyTo(stream);
                    imageBytes = stream.ToArray();
                }
                using (var client = new HttpClient())
                {
                    var content = new MultipartFormDataContent();
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                    content.Add(imageContent, "file", communityInfo.BannerImage.FileName);
                    var imageResponse = await client.PostAsync(ServiceConstants.API_GATEWAY + "/file/upload-community-banner-image", content);

                    if (imageResponse.IsSuccessStatusCode)
                    {
                        var responseData = await imageResponse.Content.ReadFromJsonAsync<Response<string>>();

                        if (responseData.Data != null)
                        {
                            community.BannerImage = responseData.Data;
                        }
                    }
                }

            }

            DatabaseResponse response = await _communityRepository.InsertAsync(community);
            
            User? user = await HttpRequestHelper.GetUser(Token);
            if (user == null) throw new UnauthorizedAccessException();

            CommunityParticipiant participiant = new CommunityParticipiant()
            {
                User= user,
                CommunityId = response.Data,
                Status = ParticipiantStatus.ACCEPTED
            };
            await _participiantRepository.InsertAsync(participiant);

            return await Task.FromResult(Response<string>.Success(response.Data, ResponseStatus.Success));
        }


        public async Task<Response<string>> Delete(string ownerId,string communityId)
        {
            try
            {
                Community community = await _communityRepository.GetFirstAsync(c => c.Id == communityId);

                if (community.Admin.Id == ownerId)
                {
                    _communityRepository.DeleteById(communityId);
                    _participiantRepository.DeleteByExpression(p => p.CommunityId == communityId);
                    return await Task.FromResult(Response<string>.Success("Deleted", ResponseStatus.Success));
                }
                else
                {
                    return await Task.FromResult(Response<string>.Fail("Not authorized for delete community. You are not an admin!", ResponseStatus.NotAuthenticated));
                }
            }
            catch(Exception e)
            {
                return await Task.FromResult(Response<string>.Fail($"Error occured: {e}", ResponseStatus.NotAuthenticated));
            }
        }


        public async Task<Response<NoContent>> KickUser(string token, string communityId, string userId)
        {
            try
            {
                string currentId = TokenHelper.GetUserIdByToken(token);

                Community? community = await _communityRepository.GetFirstAsync(c => c.Id == communityId && c.Admin.Id == currentId);

                if (community == null)
                    return Response<NoContent>.Fail("Community Not Found",ResponseStatus.NotFound);

                // Only admin can kick participiants.
                if(currentId != community.Admin.Id)
                    return Response<NoContent>.Fail("Unaturhoized for this feature.",ResponseStatus.Unauthorized);

                // Admin can't kick himself
                if(currentId == userId)
                    return Response<NoContent>.Fail("Admin can not kick yourself",ResponseStatus.BadRequest);

                CommunityParticipiant? participiant = await _participiantRepository.GetFirstAsync(p => p.CommunityId == communityId && p.User.Id == userId);

                if(participiant == null)
                    return Response<NoContent>.Fail("User Not Found",ResponseStatus.NotFound);

                _participiantRepository.DeleteByExpression(p => p.CommunityId == communityId && p.User.Id == userId);

                return Response<NoContent>.Success(ResponseStatus.Success);
            }
            catch (Exception e)
            {

                return Response<NoContent>.Fail(e.ToString(), ResponseStatus.InitialError);
            }
        }


        public async Task<Response<string>> AssignUserAsAdmin(string userId, AssignUserAsAdminDto dtoInfo)
        {
            try
            {
                if (userId.IsNullOrEmpty() || dtoInfo.CommunityId.IsNullOrEmpty())
                {
                    return await Task.FromResult(Response<string>.Fail("Bad Request", ResponseStatus.BadRequest));
                }
                Community community = await _communityRepository.GetFirstAsync(c => c.Id == dtoInfo.CommunityId);
                //   Admin yapılacak kişi participiant mı ?            Isteği atan kişi admin mi ?
                var isParticipiantTargetUser = await _participiantRepository.AnyAsync(p => p.CommunityId == community.Id && p.User.Id == dtoInfo.UserId );
                if (!isParticipiantTargetUser || userId != community.Admin.Id)
                    return await Task.FromResult(Response<string>.Fail("Failed", ResponseStatus.NotAuthenticated));
              
                User? user = await HttpRequestHelper.GetUser(Token);
                if(user == null) throw new NotFoundException("User not found");

                community.Admin = user;

                _communityRepository.Update(community);
                return await Task.FromResult(Response<string>.Success("Successfully updated new admin.", ResponseStatus.Success));

            }
            catch (Exception e)
            {
                return await Task.FromResult(Response<string>.Fail($"Some error occurred : {e}", ResponseStatus.InitialError));
            }
        }

        public async Task<Response<string>> AssignUserAsModerator(AssignUserAsModeratorDto dtoInfo)
        {
            Community community = await _communityRepository.GetFirstAsync(c => c.Id == dtoInfo.CommunityId);

            // Is it a moderator or admin who will assign the user as a moderator?
            if (community.ModeratorIds.FirstOrDefault(m => m.User.Id == dtoInfo.UserId) == null && community.Admin.Id == dtoInfo.AssignedById || community.ModeratorIds.FirstOrDefault(m => m.User.Id == dtoInfo.AssignedById) != null )
            {
                User? user = await HttpRequestHelper.GetUser(Token);
                if (user == null) throw new NotFoundException("User not found");
                community.ModeratorIds.Add(new() { User = user, AssignedById = dtoInfo.AssignedById });
                var response = _communityRepository.Update(community);
                return await Task.FromResult(Response<string>.Success("Success", ResponseStatus.Success));
            }
            else
            {
                return await Task.FromResult(Response<string>.Fail("Failed", ResponseStatus.InitialError));
            }
        }


        public async Task<Response<string>> DeletePermanently(string ownerId, string communityId)
        {

            Community community = await _communityRepository.GetFirstAsync(c => c.Id == communityId);

            if (community.Admin.Id == ownerId)
            {
                _communityRepository.DeleteCompletely(communityId);
                return Response<string>.Success("Deleted", ResponseStatus.Success);
            }
            else
            {
                return Response<string>.Fail("Not authorized for delete community. You are not an admin!", ResponseStatus.NotAuthenticated);
            }
        }


        public async Task<Response<string>> GetCommunityTitle(string id)
        {
            Community community = await _communityRepository.GetFirstAsync(c => c.Id == id);
            return Response<string>.Success(community.Title, ResponseStatus.Success);
        }

        public async Task<Response<List<CommunityGetPreviewDto>>> GetUserCommunities(string userId, int skip = 0, int take = 10)
        {
            var participiants =  _participiantRepository.GetListByExpressionPaginated(skip, take, c => c.User.Id == userId && c.IsShownOnProfile && c.Status == ParticipiantStatus.ACCEPTED);
            List<string> idList = participiants.Select(p => p.CommunityId).ToList();
            var communities = await _communityRepository.GetListByExpressionAsync(c => idList.Contains(c.Id));
            List<CommunityGetPreviewDto> dtos = _mapper.Map<List<CommunityGetPreviewDto>>(communities);
            List<string> communityIds = dtos.Select(c => c.Id).ToList();
            Dictionary<string, int> communityParticipiantCounts = await _participiantRepository.GetCommunityParticipiants(communityIds);

            foreach (var dto in dtos)
            {
                if (communityParticipiantCounts.TryGetValue(dto.Id, out int participiantCount))
                {
                    dto.ParticipiantsCount = participiantCount;
                }
                else
                {
                    dto.ParticipiantsCount = 0; // veya başka bir değer atanabilir
                }
            }
            return Response<List<CommunityGetPreviewDto>>.Success(dtos, ResponseStatus.Success);
        }

        public async Task<Response<bool>> CheckCommunityExist(string id)
        {
            Community community = await _communityRepository.GetFirstAsync(c => c.Id == id);
            if (community != null)
            {
                return await Task.FromResult(Response<bool>.Success(true, ResponseStatus.Success));
            }
            else
            {
                return await Task.FromResult(Response<bool>.Success(false, ResponseStatus.Success));
            }
        }

        public async Task<Response<CommunityInfoPostLinkDto>> GetCommunityInfoForPostLink(string id)
        {
            try
            {
                Community community = await _communityRepository.GetFirstAsync(c => c.Id == id);
                CommunityInfoPostLinkDto dto = _mapper.Map<CommunityInfoPostLinkDto>(community);
                return await Task.FromResult(Response<CommunityInfoPostLinkDto>.Success(dto, ResponseStatus.Success));
            }
            catch (Exception e)
            {
                return await Task.FromResult(Response<CommunityInfoPostLinkDto>.Fail($"Some error occured: {e}",
                    ResponseStatus.InitialError));
            }

        }

        public async Task<Response<bool>> CheckIsUserAdminOwner(string userId)
        {
            try
            {
                bool result = await _communityRepository.AnyAsync(c => !c.IsDeleted && c.Admin.Id == userId);
                return await Task.FromResult(Response<bool>.Success(result, ResponseStatus.Success));
            }
            catch (Exception e)
            {
                return await Task.FromResult(Response<bool>.Fail($"Some error occured: {e}",
                    ResponseStatus.InitialError));
            }
        }

        private string StringToSlugConvert(string phrase)
        {
            var turkishChars = new char[] { 'ç', 'ğ', 'ı', 'i', 'ö', 'ş', 'ü' };
            var englishChars = new char[] { 'c', 'g', 'i', 'i', 'o', 's', 'u' };

            string str = phrase.ToLower();

            for (int i = 0; i < turkishChars.Length; i++)
            {
                str = str.Replace(turkishChars[i], englishChars[i]);
            }

            str = Regex.Replace(str, @"\s+", " ").Trim(); // convert multiple spaces into one space
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim(); // cut and trim it
            str = Regex.Replace(str, @"\s", "-"); // hyphens

            return str;
        }

    }
}

