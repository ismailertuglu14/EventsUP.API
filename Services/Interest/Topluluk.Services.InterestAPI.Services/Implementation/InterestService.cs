using System.Net.Http.Headers;
using DBHelper.Repository;
using RestSharp;
using System.Net.Http.Json;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using Topluluk.Services.InterestAPI.Data.Implementation;
using Topluluk.Services.InterestAPI.Data.Interface;
using Topluluk.Services.InterestAPI.Model.Dto;
using Topluluk.Services.InterestAPI.Model.Entity;
using Topluluk.Services.InterestAPI.Services.Interface;
using Topluluk.Shared.Constants;
using Topluluk.Shared.Dtos;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;

namespace Topluluk.Services.InterestAPI.Services.Implementation;

public class InterestService: IInterestService
{
    private readonly IInterestRepository _interestRepository;
    private readonly IMapper _mapper;
    private readonly RestClient _client;

    public InterestService(IInterestRepository interestRepository, IMapper mapper)
    {
        _interestRepository = interestRepository;
        _mapper = mapper;
        _client = new RestClient();
    }

    public async Task<Response<List<GetInterestDto>>> GetInterests(string userId)
    {
        if (userId.IsNullOrEmpty()) throw new UnauthorizedAccessException();
        List<Interest> interest = await _interestRepository.GetListByExpressionAsync(x => true);
        return Response<List<GetInterestDto>>.Success(_mapper.Map<List<Interest>, List<GetInterestDto>>(interest), ResponseStatus.Success);

    }

    public async Task<Response<Interest>> CreateInterest(CreateInterestDto dto, string token)
    {
        var interest = new Interest();
        interest.Title = dto.Title;


        byte[] imageBytes;

        using (var stream = new MemoryStream())
        {
            dto.File.CopyTo(stream);
            imageBytes = stream.ToArray();
        }

        using (var client = new HttpClient())
        {
            Response<string>? responseData = new();

            var content = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(imageContent, "File", dto.File.FileName);

            var response = await client.PostAsync(ServiceConstants.API_GATEWAY+"/file/upload-interest-image", content);

            if (!response.IsSuccessStatusCode)
            {
                return Response<Interest>.Fail("Failed while uploading image with http client",
                    ResponseStatus.InitialError);
            }

            responseData = await response.Content.ReadFromJsonAsync<Response<string>>();

            interest.ImageUrl = responseData!.Data;
            await _interestRepository.InsertAsync(interest);


            return Response<Interest>.Success(interest, ResponseStatus.Success);
        }

    }
}