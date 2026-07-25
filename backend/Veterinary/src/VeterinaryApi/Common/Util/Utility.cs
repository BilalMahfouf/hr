namespace VeterinaryApi.Common.Util
{
    /// <summary>General-purpose utility methods used across the application.</summary>
    public static class Utility
    {
        /// <summary>
        /// Builds a password-reset or email-verification callback URL by appending
        /// <paramref name="token"/> and <paramref name="email"/> as URL-encoded query parameters.
        /// </summary>
        internal static string GenerateResponseLink(string email, string token, string uri)
        {
            var param = new Dictionary<string, string>
                {
                    {"token",token},
                    {"email",email}
                };
            string link = $"{uri}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
            return link;
        }
    internal static string GetDateInFrench(DateTime date)
    {
        var month = date.Month switch
        {
            1 => "Janvier",
            2 => "Février",
            3 => "Mars",
            4 => "Avril",
            5 => "Mai",
            6 => "Juin",
            7 => "Juillet",
            8 => "Août",
            9 => "Septembre",
            10 => "Octobre",
            11 => "Novembre",
            12 => "Décembre",
            _ => ""
        };
        var stringDate = $"{date.Day} / {month} / {date.Year}";
        return stringDate;
    }
 

    }
}
