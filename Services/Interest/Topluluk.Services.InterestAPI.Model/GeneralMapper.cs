using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Topluluk.Services.InterestAPI.Model.Dto;
using Topluluk.Services.InterestAPI.Model.Entity;

namespace Topluluk.Services.InterestAPI.Model
{
    public class GeneralMapper : Profile
    {
        public GeneralMapper()
        {
            CreateMap<Interest, GetInterestDto>();
        }
    }
}
