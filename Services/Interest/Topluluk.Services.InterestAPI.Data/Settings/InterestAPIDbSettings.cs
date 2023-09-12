using DBHelper.Connection;
using Microsoft.Extensions.Configuration;

namespace Topluluk.Services.InterestAPI.Data.Settings;

public class InterestAPIDbSettings : IDbConfiguration
{
    private readonly IConfiguration _configuration;

    public InterestAPIDbSettings(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string ConnectionString => _configuration.GetConnectionString("MongoDB") ?? throw new Exception("Interest API Db Settings con string null");
    public string DatabaseName { get { return "Interest"; } }
}