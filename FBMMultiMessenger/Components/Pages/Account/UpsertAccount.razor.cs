using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Models.Shared;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
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

            if (response is not null &&  response.IsSuccess)
            {
                mudDialog.Close(DialogResult.Ok(true));
                Snackbar.Add(response.Message, Severity.Success);
                return;
            }

            Snackbar.Add(response?.Message ?? "Something went wrong when adding account, please try later.", Severity.Error);
        }

        public void Cancel()
        {
            mudDialog.Cancel();
        }
    }
}
