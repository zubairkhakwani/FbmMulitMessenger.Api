using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Models.Shared
{
    public class PopupFormSettings
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public PopupView PopupView { get; set; } = PopupView.Single;
        public string ContentHeight { get; set; } = "500px";
        public string ContentWidth { get; set; } = "500px";
        public string ImagePath { get; set; }
        public Action OnAvatarClick { get; set; }
        public AvatarContent AvatarContent { get; set; } = AvatarContent.Icon;
        public RenderFragment RenderFragment { get; set; }
    }

    public enum AvatarContent
    {
        Icon = 1,
        ImagePath = 2,
        RenderFragenment = 3
    }


    public enum PopupView
    {
        Single = 1,
        Double = 2
    }
}
