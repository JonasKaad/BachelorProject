using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FlightPatternDetection.Models
{
    [Index(nameof(FlightId), IsUnique = true)]
    public class AutomatedCollection
    {
        [Key]
        public long FlightId { get; set; }
        public DateTime Fetched { get; set; }
        public bool IsProcessed { get; set; }
        public bool? DidHold { get; set; }
        public byte[]? RawJson { get; set; }

        public string? RawJsonAsString()
        {
            if (RawJson is null)
            {
                return null;
            }
            return ZipUtils.UnzipData(RawJson);
        }
    }
}
