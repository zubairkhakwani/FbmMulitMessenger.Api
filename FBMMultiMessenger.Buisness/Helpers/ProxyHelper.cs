using Newtonsoft.Json;
using System.Net;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public class IPInfo
    {
        public string city_name { get; set; }
        public string region_name { get; set; }
        public string time_zone { get; set; }
        public string country_name { get; set; }
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
    }

    public static class ProxyHelper
    {
        private static HttpClient GetProxyHttpClient(string proxyAddress, string username, string password)
        {
            // Create a WebProxy instance with the proxy address.
            WebProxy proxy = new WebProxy(proxyAddress);

            // Set the credentials for the proxy.
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                proxy.Credentials = new NetworkCredential(username, password);
            }

            HttpClient httpClient = new HttpClient(new HttpClientHandler { Proxy = proxy });
            return httpClient;
        }

        public static async Task<IPInfo> GetProxyDetail(string proxyAddress, string username, string password)
        {
            try
            {
                HttpClient httpClient = GetProxyHttpClient(proxyAddress, username, password);

                // Obtain geolocation information for the IP address associated with the proxy
                string ipAddress = new WebProxy(proxyAddress).Address.Host; // Extract the IP address from the proxy URL
                string geolocationUrl = $"https://api.ip2location.io/?key=A1C21D9A9DA3E249B34683153CFB5024&ip={ipAddress}";

                HttpResponseMessage geoResponse = await httpClient.GetAsync(geolocationUrl);
                string geoInfo = await geoResponse.Content.ReadAsStringAsync();

                var ipInfo = JsonConvert.DeserializeObject<IPInfo>(geoInfo);

                return ipInfo;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task<bool> ValidateProxy(string proxyAddress, string username, string password)
        {
            try
            {
                HttpClient httpClient = GetProxyHttpClient(proxyAddress, username, password);

                string testUrl = "https://www.google.com";
                HttpResponseMessage response = await httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {

            }

            return false;
        }
    }
}