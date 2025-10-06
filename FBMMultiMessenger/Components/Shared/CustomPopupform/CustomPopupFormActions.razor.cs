using Microsoft.AspNetCore.Components;

namespace FBMMultiMessenger.Components.Shared.CustomPopupform
{
    public partial class CustomPopupFormActions
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
