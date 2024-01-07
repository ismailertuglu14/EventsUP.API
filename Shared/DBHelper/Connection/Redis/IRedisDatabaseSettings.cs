using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBHelper.Connection.Redis
{
    public interface IRedisDatabaseSettings
    {
         ConnectionMultiplexer Connection { get;  }
    }
}
