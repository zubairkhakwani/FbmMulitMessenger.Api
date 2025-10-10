using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;


namespace FBMMultiMessenger.Components.Layout
{
    public partial class NavMenu
    {
        public bool isMobilePlatform = DeviceInfo.Platform != DevicePlatform.WinUI;

        [Inject]
        public IAuthService AuthService { get; set; }

        public async Task Logout()
        {
            await AuthService.Logout();
        }
    }
}
