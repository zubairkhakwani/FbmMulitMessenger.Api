using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static FBMMultiMessenger.Utility.SD;

namespace FBMMultiMessenger.Services
{
    internal class BaseService : IBaseService
    {
        public IHttpClientFactory httpClient { get; set; }
        private readonly ITokenProvider _tokenProvider;


        public BaseService(IHttpClientFactory httpClientFactory, ITokenProvider tokenProvider)
        {
            httpClient = httpClientFactory;
            this._tokenProvider=tokenProvider;
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

                var token = await _tokenProvider.GetTokenAsync();

                if (token is not null && withBearer)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                if (apiRequest.ContentType == ContentType.MultipartFormData)
                {
                    message.Content = await CreateMultipartContentAsync(apiRequest);
                }

                else
                {
                    message.Content = new StringContent(
                        JsonConvert.SerializeObject(apiRequest.Data),
                        Encoding.UTF8,
                        "application/json"
                    );
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
            catch (Exception)
            {
                var data = BaseResponse<TResponse>.Error("Something went wrong, please try later");
                var res = JsonConvert.SerializeObject(data);
                var APIResponse = JsonConvert.DeserializeObject<TResponse>(res);

                return APIResponse;
            }
        }

        private bool HasBrowserFiles<TRequest>(TRequest data) where TRequest : class
        {
            if (data == null) return false;

            var properties = data.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(data);
                if (value == null) continue;

                // Check if property is IBrowserFile
                if (value is IBrowserFile)
                    return true;

                // Check if property is List<IBrowserFile>
                if (value is IEnumerable<IBrowserFile> list && list.Any())
                    return true;
            }

            return false;
        }
        private async Task<MultipartFormDataContent> CreateMultipartContentAsync<TRequest>(ApiRequest<TRequest> apiRequest)
        where TRequest : class
        {
            var content = new MultipartFormDataContent();

            if (apiRequest.Data == null) return content;

            var properties = apiRequest.Data.GetType().GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(apiRequest.Data);
                if (value == null) continue;

                // CASE 1: Single IBrowserFile property
                // ====================================
                if (value is IBrowserFile singleFile)
                {
                    var stream = singleFile.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(singleFile.ContentType);

                    // Use property name as form field name
                    // Example: property "ProfileImage" becomes name="ProfileImage"
                    content.Add(streamContent, prop.Name, singleFile.Name);
                }

                // CASE 2: List<IBrowserFile> property
                // ===================================
                else if (value is IEnumerable<IBrowserFile> fileList)
                {
                    foreach (var file in fileList)
                    {
                        var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                        var streamContent = new StreamContent(stream);
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                        // Use property name as form field name
                        // Example: property "Files" becomes name="Files"
                        content.Add(streamContent, prop.Name, file.Name);
                    }
                }
                // CASE 3: Regular property (string, int, etc.)
                // ============================================
                else
                {
                    // Add as text form field
                    // Example: FbChatId="chat123", Message="Hello!"
                    content.Add(new StringContent(value.ToString()), prop.Name);
                }
            }

            return content;
        }
    }
}
