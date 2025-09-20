using FBMMultiMessenger.Components.Pages.Authenticaion;

namespace FBMMultiMessenger
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "FBMMultiMessenger" };

        }
    }
}
