using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public AccountController(IMediator mediator, IMapper mapper)
        {
            _mediator=mediator;
            _mapper=mapper;
        }

        [Authorize]
        [HttpPost]
        public async Task<BaseResponse<UpsertAccountHttpResponse>> Create([FromBody] UpsertAccountHttpRequest httpRequest)
        {
            UpsertAccountModelRequest request = _mapper.Map<UpsertAccountModelRequest>(httpRequest);
            BaseResponse<UpsertAccountModelResponse> response = await _mediator.Send(request);

            BaseResponse<UpsertAccountHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpsertAccountHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<List<GetMyAccountsHttpResponse>>> GetAll()
        {
            BaseResponse<List<GetMyAccountsModelResponse>> response = await _mediator.Send(new GetMyAccountsModelRequest());
            BaseResponse<List<GetMyAccountsHttpResponse>> httpResponse = _mapper.Map<BaseResponse<List<GetMyAccountsHttpResponse>>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpPut("{accountId}/status")]
        public async Task<BaseResponse<RemoveAccountHttpResponse>> ToggleAccountStatus([FromRoute] int accountId)
        {
            BaseResponse<ToggleAcountStatusModelResponse> response = await _mediator.Send(new RemoveAcountModelRequest() { AccountId = accountId });
            BaseResponse<RemoveAccountHttpResponse> httpResponse = _mapper.Map<BaseResponse<RemoveAccountHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpPut("{accountId}")]
        public async Task<BaseResponse<UpsertAccountHttpResponse>> Edit([FromBody] UpsertAccountHttpRequest httpRequest, [FromRoute] int accountId)
        {
            UpsertAccountModelRequest request = _mapper.Map<UpsertAccountModelRequest>(httpRequest);
            request.AccountId = accountId;

            BaseResponse<UpsertAccountModelResponse> response = await _mediator.Send(request);
            BaseResponse<UpsertAccountHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpsertAccountHttpResponse>>(response);
            return httpResponse;
        }

        [Authorize]
        [HttpGet("me/chats")]
        public async Task<BaseResponse<GetAllMyAccountsChatsHttpResponse>> GetAllMyAccountChats()
        {
            BaseResponse<GetAllMyAccountsChatsModelResponse> response = await _mediator.Send(new GetAllMyAccountsChatsModelRequest());
            BaseResponse<GetAllMyAccountsChatsHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetAllMyAccountsChatsHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpPost("{accountId}/open-in-browser")]
        public async Task<BaseResponse<object>> OpenInBrwoser([FromRoute] int accountId)
        {
            OpenInBrowserModelRequest request = new OpenInBrowserModelRequest() { AccountId = accountId };

            BaseResponse<object> httpResponse = await _mediator.Send(request);

            return httpResponse;
        }
    }
}
