using Topluluk.Services.InterestAPI.Model.Dto;
using Topluluk.Services.InterestAPI.Model.Entity;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.InterestAPI.Services.Interface;

public interface IInterestService
{
    Task<Response<List<GetInterestDto>>> GetInterests(string userId);
    Task<Response<Interest>> CreateInterest(CreateInterestDto dto, string token);
}