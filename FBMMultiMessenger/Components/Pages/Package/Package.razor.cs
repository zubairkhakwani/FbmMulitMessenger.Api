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
                var title = !string.IsNullOrWhiteSpace(IsExpired) ? "Subscrition Expired" : "Subscription renew";
                await JS.InvokeVoidAsync("myInterop.showSweetAlert", title, Message);
            }
        }
    }
}
