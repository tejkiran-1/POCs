using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocationTrackingApi.Models
{
    /// <summary>
    /// Represents a single GPS location record in the system
    /// Stores historical location data for tracking and analytics
    /// </summary>
    public class LocationRecord
    {
        /// <summary>
        /// Primary key for the location record
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key reference to the user who recorded this location
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the associated user
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// GPS latitude coordinate (decimal degrees)
        /// Range: -90 to +90
        /// </summary>
        [Required]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        public decimal Latitude { get; set; }

        /// <summary>
        /// GPS longitude coordinate (decimal degrees)
        /// Range: -180 to +180
        /// </summary>
        [Required]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
        public decimal Longitude { get; set; }

        /// <summary>
        /// Timestamp when this location was recorded
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional accuracy of the GPS reading in meters
        /// </summary>
        public decimal? Accuracy { get; set; }

        /// <summary>
        /// Optional altitude in meters above sea level
        /// </summary>
        public decimal? Altitude { get; set; }

        /// <summary>
        /// Optional speed in meters per second at the time of recording
        /// </summary>
        public decimal? Speed { get; set; }

        /// <summary>
        /// Optional bearing/heading in degrees (0-360)
        /// </summary>
        [Range(0.0, 360.0, ErrorMessage = "Bearing must be between 0 and 360 degrees")]
        public decimal? Bearing { get; set; }

        /// <summary>
        /// Indicates if this location update triggered a real-time notification
        /// </summary>
        public bool NotificationSent { get; set; } = false;
    }
}
