using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Models.Shared;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Components.Pages.Account
{
    public partial class UpsertAccount
    {
        public UpsertAccountHttpRequest model { get; set; } = new();
        public PopupFormSettings popupFormSettings { get; set; }

        [Parameter]
        public int? AccountId { get; set; }

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public string Cookie { get; set; }


        [Inject]
        public IAccountService AccountService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        public IMudDialogInstance mudDialog { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private IJSRuntime JS { get; set; }


        protected override void OnInitialized()
        {
            popupFormSettings = new PopupFormSettings()
            {
                Title = $"{(AccountId is null ? "Add" : "Edit")} Account",
                Icon = Icons.Material.TwoTone.Payments,
                ContentHeight = "390px",
                PopupView = PopupView.Single
            };
            if (AccountId is not null)
            {
                model.Name = Name;
                model.Cookie = Cookie;
            }
        }

        public async Task OnValidSubmit()
        {
            var response = await AccountService.UpsertAccountAsync<BaseResponse<UpsertAccountHttpResponse>>(model, AccountId);

            mudDialog.Close(DialogResult.Ok(true));

            if (!response.IsSuccess && response.Data is not null && response.Data.IsLimitExceeded)
            {
                await JS.InvokeVoidAsync("myInterop.showSweetAlert", "Limit Exceeded", response.Message, true, "Click here to upgrade from the available packages", "/packages");
            }

            else if (!response.IsSuccess && response.RedirectToPackages)
            {
                var isSubscriptionExpired = response.Data?.IsSubscriptionExpired ?? false;

                Navigation.NavigateTo($"/packages?isExpired={isSubscriptionExpired}&message={response.Message}");
            }

            else
            {
                Snackbar.Add(response?.Message ?? "Something went wrong when adding account, please try later.", Severity.Success);
                return;
            }

        }

        public void Cancel()
        {
            mudDialog.Cancel();
        }
    }
}
