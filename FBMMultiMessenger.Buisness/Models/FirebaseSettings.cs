namespace FBMMultiMessenger.Buisness.Models
{
    public class FirebaseSettings
    {
        public bool Enabled { get; set; }

        /// <summary>
        /// Relative or absolute path to the service-account JSON (e.g. Secrets/....json).
        /// </summary>
        public string CredentialsPath { get; set; } = string.Empty;

        /// <summary>URL opened when the admin clicks the browser notification.</summary>
        public string ClickActionUrl { get; set; } = string.Empty;
    }
}
