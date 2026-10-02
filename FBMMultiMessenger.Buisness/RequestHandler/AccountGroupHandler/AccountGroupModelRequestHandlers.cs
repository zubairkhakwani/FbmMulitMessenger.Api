using FBMMultiMessenger.Buisness.Request.AccountGroup;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountGroupHandler
{
    internal class GetMyAccountGroupsModelRequestHandler
        : IRequestHandler<GetMyAccountGroupsModelRequest, BaseResponse<GetMyAccountGroupsModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetMyAccountGroupsModelRequestHandler(
            ApplicationDbContext dbContext,
            CurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<BaseResponse<GetMyAccountGroupsModelResponse>> Handle(
            GetMyAccountGroupsModelRequest request,
            CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser is null)
            {
                return BaseResponse<GetMyAccountGroupsModelResponse>.Error(
                    "Invalid Request, Please login again to continue.");
            }

            var groups = await _dbContext.AccountGroups
                .AsNoTracking()
                .Include(g => g.Accounts.Where(a => a.IsActive))
                .Where(g => g.UserId == currentUser.Id)
                .OrderBy(g => g.Name)
                .ToListAsync(cancellationToken);

            var response = new GetMyAccountGroupsModelResponse
            {
                Groups = groups.Select(MapGroup).ToList(),
            };

            return BaseResponse<GetMyAccountGroupsModelResponse>.Success(
                "Operation performed successfully",
                response);
        }

        internal static AccountGroupModelResponse MapGroup(AccountGroup g) => new()
        {
            Id = g.Id,
            Name = g.Name,
            MemberCount = g.Accounts.Count(a => a.IsActive),
            AccountIds = g.Accounts.Where(a => a.IsActive).Select(a => a.Id).OrderBy(id => id).ToList(),
            CreatedAt = g.CreatedAt,
            UpdatedAt = g.UpdatedAt,
        };
    }

    internal class UpsertAccountGroupModelRequestHandler
        : IRequestHandler<UpsertAccountGroupModelRequest, BaseResponse<AccountGroupModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public UpsertAccountGroupModelRequestHandler(
            ApplicationDbContext dbContext,
            CurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<BaseResponse<AccountGroupModelResponse>> Handle(
            UpsertAccountGroupModelRequest request,
            CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser is null)
            {
                return BaseResponse<AccountGroupModelResponse>.Error(
                    "Invalid Request, Please login again to continue.");
            }

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BaseResponse<AccountGroupModelResponse>.Error("Group name is required.");
            }

            if (name.Length > 80)
            {
                return BaseResponse<AccountGroupModelResponse>.Error(
                    "Group name must be 80 characters or fewer.");
            }

            var accountIds = (request.AccountIds ?? new List<int>()).Distinct().ToList();

            var accounts = await _dbContext.Accounts
                .Where(a => a.UserId == currentUser.Id && a.IsActive && accountIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            if (accounts.Count != accountIds.Count)
            {
                return BaseResponse<AccountGroupModelResponse>.Error(
                    "One or more accounts were not found.");
            }

            var now = DateTime.UtcNow;

            if (request.GroupId is null)
            {
                var group = new AccountGroup
                {
                    UserId = currentUser.Id,
                    Name = name,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                await _dbContext.AccountGroups.AddAsync(group, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await AssignMembersAsync(group.Id, accounts, currentUser.Id, now, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await _dbContext.Entry(group).Collection(g => g.Accounts).LoadAsync(cancellationToken);
                return BaseResponse<AccountGroupModelResponse>.Success(
                    "Group created successfully",
                    GetMyAccountGroupsModelRequestHandler.MapGroup(group));
            }

            var existing = await _dbContext.AccountGroups
                .Include(g => g.Accounts)
                .FirstOrDefaultAsync(
                    g => g.Id == request.GroupId && g.UserId == currentUser.Id,
                    cancellationToken);

            if (existing is null)
            {
                return BaseResponse<AccountGroupModelResponse>.Error("Group not found.");
            }

            existing.Name = name;
            existing.UpdatedAt = now;

            var previousMembers = existing.Accounts.ToList();
            foreach (var prev in previousMembers)
            {
                if (!accountIds.Contains(prev.Id))
                {
                    prev.AccountGroupId = null;
                    prev.UpdatedAt = now;
                }
            }

            await AssignMembersAsync(existing.Id, accounts, currentUser.Id, now, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _dbContext.Entry(existing).Collection(g => g.Accounts).Query()
                .Where(a => a.IsActive)
                .LoadAsync(cancellationToken);

            // Reload active members for response
            existing.Accounts = await _dbContext.Accounts
                .Where(a => a.AccountGroupId == existing.Id && a.IsActive)
                .ToListAsync(cancellationToken);

            return BaseResponse<AccountGroupModelResponse>.Success(
                "Group updated successfully",
                GetMyAccountGroupsModelRequestHandler.MapGroup(existing));
        }

        private async Task AssignMembersAsync(
            int groupId,
            List<Account> accounts,
            int userId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            foreach (var account in accounts)
            {
                account.AccountGroupId = groupId;
                account.UpdatedAt = now;
            }

            // Clear any leftover members that somehow still point here but weren't in the set
            // (handled for update above). No-op for create.
            await Task.CompletedTask;
        }
    }

    internal class DeleteAccountGroupModelRequestHandler
        : IRequestHandler<DeleteAccountGroupModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public DeleteAccountGroupModelRequestHandler(
            ApplicationDbContext dbContext,
            CurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<BaseResponse<object>> Handle(
            DeleteAccountGroupModelRequest request,
            CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser is null)
            {
                return BaseResponse<object>.Error("Invalid Request, Please login again to continue.");
            }

            var group = await _dbContext.AccountGroups
                .Include(g => g.Accounts)
                .FirstOrDefaultAsync(
                    g => g.Id == request.GroupId && g.UserId == currentUser.Id,
                    cancellationToken);

            if (group is null)
            {
                return BaseResponse<object>.Error("Group not found.");
            }

            var now = DateTime.UtcNow;
            foreach (var account in group.Accounts)
            {
                account.AccountGroupId = null;
                account.UpdatedAt = now;
            }

            _dbContext.AccountGroups.Remove(group);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<object>.Success("Group deleted. Accounts were kept.", new());
        }
    }
}
