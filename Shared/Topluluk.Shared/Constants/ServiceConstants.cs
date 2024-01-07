using System;
namespace Topluluk.Shared.Constants
{
	public static class ServiceConstants
	{
		public static string API_GATEWAY
		{
			get
			{
				string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
					throw new ArgumentNullException("ASPNETCORE_ENVIRONMENT", "ASPNETCORE_ENVIRONMENT variable is can not be null");
                return environment switch
                {
                    "Development" => "https://localhost:7149/api",
                    "PreProduction" => "https://toplulukapi.azurewebsites.net/api",
                    "Production" => "https://toplulukapi.azurewebsites.net/api",
                    _ => "https://toplulukapi.azurewebsites.net/api"
                };
            }
		}
			
	}
}

