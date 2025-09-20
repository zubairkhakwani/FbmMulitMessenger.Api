using FBMMultiMessenger.Contracts.Contracts;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    internal interface IBaseService
    {
        Task<TResponse> SendAsync<TRequest, TResponse>(ApiRequest<TRequest> apiRequest, bool withBearer = true) where TResponse : class
                                                                                where TRequest : class;
    }
}
