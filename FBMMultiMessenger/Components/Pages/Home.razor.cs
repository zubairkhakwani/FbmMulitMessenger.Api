using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Components.Pages
{
    public partial class Home
    {
        [Inject]
        private NavigationManager Navigation { get; set; }
        protected override void OnInitialized()
        {
            Navigation.NavigateTo("/login");
        }
    }
}
