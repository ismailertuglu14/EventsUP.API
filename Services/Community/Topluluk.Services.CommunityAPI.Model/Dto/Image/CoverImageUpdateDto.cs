using System;
using Microsoft.AspNetCore.Http;

namespace Topluluk.Services.CommunityAPI.Model.Dto.Image
{
    public class CoverImageUpdateDto
    {
        public IFormFile File { get; set; }
    }
}

