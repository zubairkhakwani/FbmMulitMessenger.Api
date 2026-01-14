using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Shared;
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
        [HttpPost("import")]
        public async Task<BaseResponse<UpsertAccountHttpResponse>> BulkImport([FromBody] List<ImportAccountsHttpRequest> httpRequest)
        {
            List<ImportAccounts> request = _mapper.Map<List<ImportAccounts>>(httpRequest);

            BaseResponse<UpsertAccountModelResponse> response = await _mediator.Send(new ImportAccountsModelRequest() { Accounts = request });

            BaseResponse<UpsertAccountHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpsertAccountHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<UserAccountsOverviewHttpResponse>> GetAll([FromQuery] GetMyAccountsHttpRequest httpRequest)
        {
            BaseResponse<UserAccountsOverviewModelResponse> response = await _mediator.Send(_mapper.Map<GetMyAccountsModelRequest>(httpRequest));
            BaseResponse<UserAccountsOverviewHttpResponse> httpResponse = _mapper.Map<BaseResponse<UserAccountsOverviewHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpDelete("{accountId}")]
        public async Task<BaseResponse<RemoveAccountHttpResponse>> Delete([FromRoute] int accountId)
        {
            BaseResponse<ToggleAcountStatusModelResponse> response = await _mediator.Send(new RemoveAcountModelRequest() { AccountIds = [accountId] });
            BaseResponse<RemoveAccountHttpResponse> httpResponse = _mapper.Map<BaseResponse<RemoveAccountHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpDelete("bulk")]
        public async Task<BaseResponse<RemoveAccountHttpResponse>> DeleteBulk([FromBody] List<int> accountIds)
        {
            BaseResponse<ToggleAcountStatusModelResponse> response = await _mediator.Send(new RemoveAcountModelRequest() { AccountIds = accountIds });
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
        public async Task<BaseResponse<GetAllMyAccountsChatsHttpResponse>> GetAllMyAccountChats(CancellationToken cancellationToken = default)
        {
            BaseResponse<GetAllMyAccountsChatsModelResponse> response = await _mediator.Send(new GetAllMyAccountsChatsModelRequest(), cancellationToken);
            BaseResponse<GetAllMyAccountsChatsHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetAllMyAccountsChatsHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpPost("{accountId}/connect")]
        public async Task<BaseResponse<object>> OpenInBrwoser([FromRoute] int accountId)
        {
            ConnectAccountModelRequest request = new ConnectAccountModelRequest() { AccountId = accountId };

            BaseResponse<object> httpResponse = await _mediator.Send(request);

            return httpResponse;
        }

        [Authorize]
        [HttpPut("update-status")]
        public async Task<BaseResponse<UpdateAccountStatusHttpResponse>> UpdateStatus([FromBody] UpdateAccountStatusHttpRequest httpRequest)
        {
            BaseResponse<UpdateAccountStatusModelResponse> response = await _mediator.Send(_mapper.Map<UpdateAccountStatusModelRequest>(httpRequest));
            BaseResponse<UpdateAccountStatusHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpdateAccountStatusHttpResponse>>(response);

            return httpResponse;
        }
    }
}
