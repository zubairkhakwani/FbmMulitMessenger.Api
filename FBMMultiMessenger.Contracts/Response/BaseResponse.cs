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

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }


        public static BaseResponse<T> Success(string message, T? result, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = new BaseResponse<T>()
            {
                Data = result,
                StatusCode = statusCode,
                Message = message
            };

            return response;
        }


        public static BaseResponse<T> Error(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, T? result = null)
        {
            var response = new BaseResponse<T>()
            {
                Message = message,
                StatusCode = statusCode,
                Data = result,
                IsSuccess = false
            };

            return response;
        }
    }
}
