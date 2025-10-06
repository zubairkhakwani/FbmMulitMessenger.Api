using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services
{
    public class BackButtonService
    {
        public event Action? BackButtonPressed;
        public bool HasSubscribers => BackButtonPressed != null;
        public void NotifyBackPressed()
        {
            BackButtonPressed?.Invoke();
        }
    }
}
