using AutoMapper;
using FBMMultiMessenger.Buisness.Request.AccountGroup;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/account-group")]
    [ApiController]
    public class AccountGroupController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public AccountGroupController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<GetMyAccountGroupsHttpResponse>> GetMine()
        {
            var response = await _mediator.Send(new GetMyAccountGroupsModelRequest());
            return _mapper.Map<BaseResponse<GetMyAccountGroupsHttpResponse>>(response);
        }

        [Authorize]
        [HttpPost]
        public async Task<BaseResponse<AccountGroupHttpResponse>> Create(
            [FromBody] UpsertAccountGroupHttpRequest httpRequest)
        {
            var request = _mapper.Map<UpsertAccountGroupModelRequest>(httpRequest);
            request.GroupId = null;
            var response = await _mediator.Send(request);
            return _mapper.Map<BaseResponse<AccountGroupHttpResponse>>(response);
        }

        [Authorize]
        [HttpPost("{groupId:int}")]
        public async Task<BaseResponse<AccountGroupHttpResponse>> Update(
            [FromRoute] int groupId,
            [FromBody] UpsertAccountGroupHttpRequest httpRequest)
        {
            var request = _mapper.Map<UpsertAccountGroupModelRequest>(httpRequest);
            request.GroupId = groupId;
            var response = await _mediator.Send(request);
            return _mapper.Map<BaseResponse<AccountGroupHttpResponse>>(response);
        }

        [Authorize]
        [HttpPost("{groupId:int}/delete")]
        public async Task<BaseResponse<object>> DeleteViaPost([FromRoute] int groupId)
        {
            var response = await _mediator.Send(new DeleteAccountGroupModelRequest { GroupId = groupId });
            return response;
        }

        [Authorize]
        [HttpDelete("{groupId:int}")]
        public async Task<BaseResponse<object>> Delete([FromRoute] int groupId)
        {
            var response = await _mediator.Send(new DeleteAccountGroupModelRequest { GroupId = groupId });
            return response;
        }
    }
}
