using FBMMultiMessenger.Models.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FBMMultiMessenger.Components.Shared.CustomPopupform
{
    public partial class PopupForm
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public PopupFormSettings Setting { get; set; }

        public object model { get; set; }

        [Parameter]
        public object Model { get; set; }

        [Parameter]
        public EditContext EditContext { get; set; }

        [Parameter]
        public EventCallback<EditContext> OnValidSubmit { get; set; }

        [Parameter]
        public EventCallback<EditContext> OnSubmit { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (Model != null && (EditContext == null || Model != model))
            {
                model = Model;
                EditContext = new(Model);
            }
        }

        private async void OnValidSubmitFunction(EditContext context)
        {
            await OnValidSubmit.InvokeAsync(context);
        }

        private async void OnSubmitFunction(EditContext context)
        {
            await OnSubmit.InvokeAsync(context);
        }

        private void OnAvatarClick()
        {
            if (Setting.OnAvatarClick != null)
            {
                Setting.OnAvatarClick();
            }
        }
    }
}
