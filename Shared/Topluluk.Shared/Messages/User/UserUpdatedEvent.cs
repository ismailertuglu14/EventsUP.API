using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topluluk.Shared.Enums;

namespace Topluluk.Shared.Messages.User
{
    public class UserUpdatedEvent
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string? UserName { get; set; }
        public string? ProfileImage { get; set; }
        public GenderEnum Gender { get; set; }
    }
}
