using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace DBHelper.Connection.Redis
{
    public class RedisDatabaseSettings : IRedisDatabaseSettings
    {
        public RedisDatabaseSettings(IConfiguration configuration)
        {
            lazyConnection = new Lazy<ConnectionMultiplexer>(() => {
                return ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? throw new ArgumentNullException());
            });
        }

        private static Lazy<ConnectionMultiplexer> lazyConnection;
        public ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

    }
}
