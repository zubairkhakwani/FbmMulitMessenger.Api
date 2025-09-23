using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FBMMultiMessenger.Components.Shared.CustomPopupform
{
    public partial class ConfirmationDialog
    {
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }

        [Parameter] public string ContentText { get; set; }

        [Parameter] public string ButtonText { get; set; }

        [Parameter] public MudBlazor.Color Color { get; set; }
        [Parameter] public string Icon { get; set; }
        [Parameter] public string TitleText { get; set; }

        void Submit() => MudDialog.Close(DialogResult.Ok(true));
        void Cancel() => MudDialog.Cancel();
    }
}
