using Topluluk.Shared.Dtos;

namespace Topluluk.Services.InterestAPI.Model.Entity;

public class Interest: AbstractEntity
{
    public string Title { get; set; }
    public string ImageUrl { get; set; }
}