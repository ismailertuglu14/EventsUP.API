using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using Topluluk.Services.InterestAPI.Model.Dto;
using Topluluk.Services.InterestAPI.Model.Entity;
using Topluluk.Services.InterestAPI.Services.Interface;
using Topluluk.Shared.BaseModels;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.InterestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InterestController : BaseController
    {

        private readonly IInterestService _interestService;

        public InterestController(IInterestService interestService)
        {
            _interestService = interestService;
        }



        [HttpGet("All")]
        public async Task<Response<List<GetInterestDto>>> GetInterests()
        {
            return new();
        }


        [HttpGet("User/{userId}")]
        public async Task<Response<List<GetInterestDto>>> GetUserInterests(string userId)
        {
            return new();
        }

        [HttpPost("User")]
        public async Task<Response<List<GetInterestDto>>> SetUserInterests(List<string> interestIds)
        {
            return new();
        }
        
        [HttpGet("Community/{communityId}")]
        public async Task<Response<List<GetInterestDto>>> GetCommunityInterests(string communityId)
        {
            return new();
        }
        
        [HttpPost("Community")]
        public async Task<Response<List<GetInterestDto>>> SetCommunityInterests(List<string> interestIds)
        {
            return new();
        }

        [HttpPost("Create")]
        public async Task<Response<Interest>> Create([FromForm] CreateInterestDto dto)
        {

            return await _interestService.CreateInterest(dto,"this.Token");
        }   
    }
}
