using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.Service
{
    public class SubscriptionServerProviderService : ISubscriptionServerProviderService
    {
        private readonly ApplicationDbContext _dbContext;

        public SubscriptionServerProviderService(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<List<LocalServer>?> GetEligibleServersAsync(Subscription userSubscription)
        {
            if (userSubscription.CanRunOnOurServer)
            {
                var superServers = _dbContext.LocalServers.Where(ls => ls.IsActive && ls.IsSuperServer).ToList();

                return superServers;
            }

            var user = await _dbContext.Users
                                       .Include(ls => ls.LocalServers)
                                       .FirstOrDefaultAsync(x => x.Id == userSubscription.Id);

            var userLocalServers = user?.LocalServers;

            return userLocalServers;
        }

    }
}
