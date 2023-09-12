using Microsoft.AspNetCore.Http;

namespace Topluluk.Services.InterestAPI.Model.Dto;

public class CreateInterestDto
{
    public string Title { get; set; }
    public IFormFile File { get; set; }
}