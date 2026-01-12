using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Shared
{
    public class BaseResponse<T> where T : class
    {
        public HttpStatusCode StatusCode { get; set; }

        public bool IsSuccess { get; set; } = true;
        public bool RedirectToPackages { get; set; }
        public bool ShowSweetAlert { get; set; }
        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }


        public static BaseResponse<T> Success(string message, T? result, bool redirectToPackages = false, bool showSweetAlert = false, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = new BaseResponse<T>()
            {
                Data = result,
                StatusCode = statusCode,
                Message = message,
                RedirectToPackages = redirectToPackages,
                ShowSweetAlert = showSweetAlert,
            };

            return response;
        }


        public static BaseResponse<T> Error(string message, bool redirectToPackages = false, bool showSweetAlert = false, T? result = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var response = new BaseResponse<T>()
            {
                Message = message,
                StatusCode = statusCode,
                Data = result,
                IsSuccess = false,
                RedirectToPackages = redirectToPackages,
                ShowSweetAlert = showSweetAlert
            };

            return response;
        }
    }
}
