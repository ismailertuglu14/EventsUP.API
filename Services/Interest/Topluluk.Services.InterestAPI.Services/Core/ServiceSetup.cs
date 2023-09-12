using DBHelper.BaseDto;
using DBHelper.Connection.Mongo;
using DBHelper.Connection;
using Microsoft.Extensions.DependencyInjection;
using Topluluk.Services.InterestAPI.Data.Implementation;
using Topluluk.Services.InterestAPI.Data.Interface;
using Topluluk.Services.InterestAPI.Data.Settings;
using Topluluk.Services.InterestAPI.Services.Implementation;
using Topluluk.Services.InterestAPI.Services.Interface;

namespace Topluluk.Services.InterestAPI.Services.Core;

public static class ServiceSetup
{

    public static void AddInfrastructure(this IServiceCollection services)
    {
        AddServicesForRepository(services);
        AddServicesForServices(services);
    }

    public static void AddServicesForRepository(this IServiceCollection services)
    {
        services.AddSingleton<IDbConfiguration, InterestAPIDbSettings>();
        services.AddSingleton<IConnectionFactory, MongoConnectionFactory>();
        services.AddSingleton<IBaseDatabaseSettings, MongoDatabaseSettings>();
        services.AddScoped<IInterestRepository, InterestRepository>();
    }

    public static void AddServicesForServices(this IServiceCollection services)
    {
        services.AddScoped<IInterestService, InterestService>();
    }
}