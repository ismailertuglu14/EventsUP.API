﻿using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.PostAPI.Model.Entity
{
    public class SavedPost : AbstractEntity
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string PostId { get; set; }
        public User User { get; set; }
    }
}
