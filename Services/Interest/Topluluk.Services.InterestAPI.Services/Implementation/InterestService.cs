using System.Net.Http.Headers;
using DBHelper.Repository;
using RestSharp;
using System.Net.Http.Json;
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
    private readonly RestClient _client;

    public InterestService(IInterestRepository interestRepository)
    {
        _interestRepository = interestRepository;
        _client = new RestClient();
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