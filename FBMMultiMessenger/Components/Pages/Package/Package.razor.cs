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

        [SupplyParameterFromQuery]
        public string? Message { get; set; }

        [Inject]
        public IJSRuntime JS { get; set; }


        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(Message))
            {
                bool.TryParse(IsExpired, out bool isExpired);

                var title = isExpired ? "Subscription Expired" : "No Active Subscription";
                await JS.InvokeVoidAsync("myInterop.showSweetAlert", title, Message);
            }
        }
    }
}
