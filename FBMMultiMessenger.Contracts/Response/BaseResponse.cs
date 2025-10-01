using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Response
{
    public class BaseResponse<T> where T : class
    {
        public HttpStatusCode StatusCode { get; set; }

        public bool IsSuccess { get; set; } = true;

        public bool IsSubsriptionExpired { get; set; }
        public bool IsSubscriptionActive { get; set; }

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }


        public static BaseResponse<T> Success(string message, T? result, bool isSubscriptionExpired = false, bool isSubscriptionActive = true, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = new BaseResponse<T>()
            {
                Data = result,
                StatusCode = statusCode,
                Message = message,
                IsSubsriptionExpired = isSubscriptionExpired,
                IsSubscriptionActive = isSubscriptionActive
            };

            return response;
        }


        public static BaseResponse<T> Error(string message, bool isSubscriptionExpired = false, bool isSubscriptionActive = true, T? result = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var response = new BaseResponse<T>()
            {
                Message = message,
                StatusCode = statusCode,
                Data = result,
                IsSuccess = false,
                IsSubsriptionExpired = isSubscriptionExpired,
                IsSubscriptionActive = isSubscriptionActive
            };

            return response;
        }
    }
}
