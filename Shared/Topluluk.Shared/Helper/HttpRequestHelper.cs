using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using X = System.Text.Json;
using System.Threading.Tasks;
using Topluluk.Shared.Constants;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Enums;

namespace Topluluk.Shared.Helper
{
	public static class HttpRequestHelper
	{
		public static async Task<HttpResponseMessage> handle<T>(T? content,string endpoint, HttpType httpType, string mediaType = "application/json")
        {
            var httpClient = new HttpClient();
            var contentHttp = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, mediaType);
			HttpResponseMessage responseHttp = new();
			switch (httpType)
			{
				case HttpType.GET:
					responseHttp = await httpClient.GetAsync(endpoint);
					break;
                case HttpType.POST:
                    responseHttp = await httpClient.PostAsync(endpoint, contentHttp);
                    break;
                case HttpType.PUT:
                    responseHttp = await httpClient.PutAsync(endpoint, contentHttp);
                    break;
                case HttpType.DELETE:
                    responseHttp = await httpClient.DeleteAsync(endpoint);
                    break;
                default:
                    break;
            }

            return responseHttp;
		}
        public static async Task<User?> GetUser(string token)
        {
            var httpClient = new HttpClient();
            string onlyToken = token.Split(' ')[1];
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", onlyToken);
            string userId = TokenHelper.GetUserIdByToken(token);
            var responseHttp = await httpClient.GetAsync($"{ServiceConstants.API_GATEWAY}/user/GetUserById?userid={userId}");

            if (responseHttp.IsSuccessStatusCode)
            {
                var responseContent = await responseHttp.Content.ReadAsStringAsync();
                var responseObject = X.JsonSerializer.Deserialize<Response<User>>(responseContent, new X.JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });

                if (responseObject != null && responseObject.StatusCode == ResponseStatus.Success)
                {
                    return responseObject.Data;
                }
            }

            return null;
        }
    }
}

