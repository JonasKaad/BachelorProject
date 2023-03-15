namespace GuiWebApp.Client
{
    public static class TimeUtils
    {
        // From: https://stackoverflow.com/a/250400
        public static DateTime ToDateTime(this long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dateTime;
        }
    }
}
