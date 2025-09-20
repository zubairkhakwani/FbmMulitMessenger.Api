using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services
{
    internal class BaseService : IBaseService
    {
        public IHttpClientFactory httpClient { get; set; }

        public BaseService(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory;
        }
        public async Task<TResponse> SendAsync<TRequest, TResponse>(ApiRequest<TRequest> apiRequest, bool withBearer = true)
            where TRequest : class
            where TResponse : class
        {
            try
            {
                var client = httpClient.CreateClient("MagicAPI");
                HttpRequestMessage message = new HttpRequestMessage();
                message.Headers.Add("Accept", "application/json");
                message.RequestUri = new Uri(apiRequest.Url);

                //var token = tokenProvider.GetToken();

                //if (token is not null && withBearer)
                //{
                //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                //}

                if (apiRequest.Data != null)
                {
                    message.Content = new StringContent(JsonConvert.SerializeObject(apiRequest.Data), Encoding.UTF8, "application/json");
                }

                switch (apiRequest.ApiType)
                {
                    case SD.ApiType.POST:
                        message.Method = HttpMethod.Post;
                        break;

                    case SD.ApiType.PUT:
                        message.Method = HttpMethod.Put;
                        break;

                    case SD.ApiType.DELETE:
                        message.Method = HttpMethod.Delete;
                        break;

                    case SD.ApiType.GET:
                        break;

                    default:
                        message.Method = HttpMethod.Get;
                        break;
                }



                HttpResponseMessage responseMessage = await client.SendAsync(message);

                var apiContent = await responseMessage.Content.ReadAsStringAsync();

                var APIResponse = JsonConvert.DeserializeObject<TResponse>(apiContent);

                return APIResponse;

            }
            catch (Exception e)
            {
                var data = BaseResponse<TResponse>.Error("Something went wrong, please try later");
                var res = JsonConvert.SerializeObject(data);
                var APIResponse = JsonConvert.DeserializeObject<TResponse>(res);

                return APIResponse;
            }
        }
    }
}
