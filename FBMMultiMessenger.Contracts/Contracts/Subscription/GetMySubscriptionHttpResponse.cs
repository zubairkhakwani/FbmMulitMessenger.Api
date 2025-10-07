using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Subscription
{
    public class GetMySubscriptionHttpResponse
    {
        public int MaxLimit { get; set; }
        public int LimitUsed { get; set; }
        public bool HasActiveSubscription { get; set; }
        public bool IsExpired { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiredAt { get; set; }
    }
}
