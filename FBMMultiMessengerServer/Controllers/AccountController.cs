using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Server.Controllers
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
            var userId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value);
            request.UserId = userId;
            BaseResponse<UpsertAccountModelResponse> response = await _mediator.Send(request);

            BaseResponse<UpsertAccountHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpsertAccountHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<List<GetMyAccountsHttpResponse>>> GetAll()
        {
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value);
            GetMyAccountsModelRequest request = new GetMyAccountsModelRequest() { UserId = userId };
            request.UserId = userId;

            BaseResponse<List<GetMyAccountsModelResponse>> response = await _mediator.Send(request);
            BaseResponse<List<GetMyAccountsHttpResponse>> httpResponse = _mapper.Map<BaseResponse<List<GetMyAccountsHttpResponse>>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpPut("{accountId}/status")]
        public async Task<BaseResponse<ToggleAccountStatusHttpResponse>> ToggleAccountStatus([FromRoute] int accountId)
        {
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value);
            ToggleAcountStatusModelRequest request = new ToggleAcountStatusModelRequest() { UserId = userId, AccountId = accountId };
            BaseResponse<ToggleAcountStatusModelResponse> response = await _mediator.Send(request);

            BaseResponse<ToggleAccountStatusHttpResponse> httpResponse = _mapper.Map<BaseResponse<ToggleAccountStatusHttpResponse>>(response);
            return httpResponse;
        }

        [Authorize]
        [HttpPut("{accountId}")]
        public async Task<BaseResponse<UpsertAccountHttpResponse>> Edit([FromBody] UpsertAccountHttpRequest httpRequest, [FromRoute] int accountId)
        {
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value);
            UpsertAccountModelRequest request = _mapper.Map<UpsertAccountModelRequest>(httpRequest);
            request.UserId = userId;
            request.AccountId = accountId;

            BaseResponse<UpsertAccountModelResponse> response = await _mediator.Send(request);
            BaseResponse<UpsertAccountHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpsertAccountHttpResponse>>(response);
            return httpResponse;
        }

        [Authorize]
        [HttpGet("me/chats")]
        public async Task<BaseResponse<GetAllMyAccountsChatsHttpResponse>> GetAllMyAccountChats()
        {
            GetAllMyAccountsChatsModelRequest request = new GetAllMyAccountsChatsModelRequest()
            {
                UserId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value)
            };

            BaseResponse<GetAllMyAccountsChatsModelResponse> response = await _mediator.Send(request);

            BaseResponse<GetAllMyAccountsChatsHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetAllMyAccountsChatsHttpResponse>>(response);

            return httpResponse;
        }
    }
}
