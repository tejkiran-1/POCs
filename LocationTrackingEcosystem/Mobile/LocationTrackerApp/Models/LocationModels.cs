using System.ComponentModel.DataAnnotations;

namespace LocationTrackerApp.Models
{
    /// <summary>
    /// Data Transfer Object for location updates
    /// </summary>
    public class LocationUpdateRequest
    {
        /// <summary>
        /// GPS latitude coordinate in decimal degrees
        /// </summary>
        [Required]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        public decimal Latitude { get; set; }

        /// <summary>
        /// GPS longitude coordinate in decimal degrees
        /// </summary>
        [Required]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
        public decimal Longitude { get; set; }

        /// <summary>
        /// Optional accuracy of the GPS reading in meters
        /// </summary>
        public decimal? Accuracy { get; set; }

        /// <summary>
        /// Optional altitude in meters above sea level
        /// </summary>
        public decimal? Altitude { get; set; }

        /// <summary>
        /// Optional speed in meters per second
        /// </summary>
        public decimal? Speed { get; set; }

        /// <summary>
        /// Optional bearing/heading in degrees (0-360)
        /// </summary>
        [Range(0.0, 360.0, ErrorMessage = "Bearing must be between 0 and 360 degrees")]
        public decimal? Bearing { get; set; }

        /// <summary>
        /// Timestamp when the location was recorded
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Location data response from API
    /// </summary>
    public class LocationResponse
    {
        /// <summary>
        /// Unique identifier for the location record
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User ID who owns this location
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// GPS latitude coordinate
        /// </summary>
        public decimal Latitude { get; set; }

        /// <summary>
        /// GPS longitude coordinate
        /// </summary>
        public decimal Longitude { get; set; }

        /// <summary>
        /// GPS accuracy in meters
        /// </summary>
        public decimal? Accuracy { get; set; }

        /// <summary>
        /// Altitude in meters
        /// </summary>
        public decimal? Altitude { get; set; }

        /// <summary>
        /// Speed in meters per second
        /// </summary>
        public decimal? Speed { get; set; }

        /// <summary>
        /// Bearing in degrees
        /// </summary>
        public decimal? Bearing { get; set; }

        /// <summary>
        /// When the location was recorded
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Current device location information
    /// </summary>
    public class DeviceLocation
    {
        /// <summary>
        /// Current latitude
        /// </summary>
        public decimal Latitude { get; set; }

        /// <summary>
        /// Current longitude
        /// </summary>
        public decimal Longitude { get; set; }

        /// <summary>
        /// Location accuracy in meters
        /// </summary>
        public decimal? Accuracy { get; set; }

        /// <summary>
        /// Altitude in meters
        /// </summary>
        public decimal? Altitude { get; set; }

        /// <summary>
        /// Speed in meters per second
        /// </summary>
        public decimal? Speed { get; set; }

        /// <summary>
        /// Bearing in degrees
        /// </summary>
        public decimal? Bearing { get; set; }

        /// <summary>
        /// When the location was obtained
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Whether this location is considered accurate enough
        /// </summary>
        public bool IsAccurate => Accuracy == null || Accuracy <= 100; // Within 100 meters is considered accurate
    }
}
