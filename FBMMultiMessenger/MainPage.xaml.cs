namespace FBMMultiMessenger
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("MainPage constructor called");

        }
        protected override bool OnBackButtonPressed()
        {
            System.Diagnostics.Debug.WriteLine("=== BACK BUTTON PRESSED IN MAINPAGE ===");
            return true;
        }
    }
}
