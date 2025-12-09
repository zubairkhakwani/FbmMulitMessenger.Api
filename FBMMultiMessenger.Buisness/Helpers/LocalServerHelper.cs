using FBMMultiMessenger.Data.Database.DbModels;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public static class LocalServerHelper
    {
        public static LocalServer? GetAvailablePowerfulServer(List<LocalServer>? servers)
        {
            if (servers is null || servers.Count == 0)
                return null;

            return servers
                .Where(s => s.IsActive && s.ActiveBrowserCount < s.MaxBrowserCapacity)
                .OrderByDescending(s => s.TotalMemoryGB)
                .ThenByDescending(s => s.MaxBrowserCapacity - s.ActiveBrowserCount)
                .ThenByDescending(s => s.HasSSD)
                .ThenByDescending(s => s.MaxClockSpeedMHz)
                .ThenByDescending(s => s.CoreCount)
                .FirstOrDefault();
        }


        public static string GenereatetUniqueId(string systemUUID)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"LocalServer_{systemUUID}-{Guid.NewGuid()}"));
        }
    }
}
