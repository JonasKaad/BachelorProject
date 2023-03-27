using System.Data;

namespace FlightPatternDetection.Services
{
    public static class NavDBReaderExtensions
    {
        public static DateTime? GetNullableDateTime(this IDataRecord aRecord, string aColumnName)
        {
            return GetNullableDateTime(aRecord, aRecord.GetOrdinal(aColumnName));
        }

        public static DateTime? GetNullableDateTime(this IDataRecord aRecord, int aOrdinal)
        {
            if (!aRecord.IsDBNull(aOrdinal))
            {
                return aRecord.GetDateTime(aOrdinal);
            }

            return null;
        }

        public static string? GetNullableString(this IDataRecord aRecord, string aColumnName)
        {
            return GetNullableString(aRecord, aRecord.GetOrdinal(aColumnName));
        }

        public static string? GetNullableString(this IDataRecord aRecord, int aOrdinal)
        {
            if (!aRecord.IsDBNull(aOrdinal))
            {
                return aRecord.GetString(aOrdinal);
            }

            return null;
        }

        public static int? GetNullableInt32(this IDataRecord aRecord, string aColumnName)
        {
            return GetNullableInt32(aRecord, aRecord.GetOrdinal(aColumnName));
        }

        public static int? GetNullableInt32(this IDataRecord aRecord, int aOrdinal)
        {
            if (!aRecord.IsDBNull(aOrdinal))
            {
                return aRecord.GetInt32(aOrdinal);
            }

            return null;
        }
    }
}
