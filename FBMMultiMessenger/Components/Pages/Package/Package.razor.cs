using FBMMultiMessenger.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Components.Pages.Package
{
    public partial class Package
    {
        [SupplyParameterFromQuery]
        public string? IsExpired { get; set; }

        [Inject]
        public IJSRuntime JS { get; set; }


        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(IsExpired))
            {
                bool.TryParse(IsExpired, out bool isExpired);

                var title = isExpired ? "Subscription Expired" : "No Active Subscription";
                var message = isExpired ? "Your subscription has expired. Please renew to continue." : "Oh Snap, Looks like you don't have any subscription yet.";
                
                await JS.InvokeVoidAsync("myInterop.showSweetAlert", title, message);
            }
        }
    }
}
