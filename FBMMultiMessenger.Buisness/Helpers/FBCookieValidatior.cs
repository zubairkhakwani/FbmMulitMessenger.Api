namespace FBMMultiMessenger.Buisness.Helpers
{
    public static class FBCookieValidatior
    {
        public static (bool isValid, string? fbAccountId) Validate(string cookieString)
        {
            try
            {
                // Parse cookies into dictionary
                var cookies = cookieString
                    .Split(';')
                    .Select(x => x.Trim().Split('=', 2))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1]);


                if (!cookies.ContainsKey("c_user") || !cookies.ContainsKey("xs"))
                    return (false, null);

                return (true, cookies["c_user"]);
            }
            catch
            {
                return (false, null);
            }
        }
    }
}
