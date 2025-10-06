using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using FBMMultiMessenger.Services;

namespace FBMMultiMessenger.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            WebViewSoftInputPatch.Initialize();
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e.KeyCode == Keycode.Back && e.Action == KeyEventActions.Down)
            {
                var service = IPlatformApplication.Current.Services.GetService<BackButtonService>();
                if (service?.HasSubscribers == true)
                {
                    service.NotifyBackPressed();
                    return true;
                }
            }


            return base.DispatchKeyEvent(e);
        }
    }
}
