using System.ComponentModel.DataAnnotations;

namespace LocationTrackingApi.DTOs
{
    /// <summary>
    /// Data Transfer Object for location update requests from mobile clients
    /// </summary>
    public class LocationUpdateDto
    {
        /// <summary>
        /// GPS latitude coordinate (decimal degrees)
        /// Range: -90 to +90
        /// </summary>
        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        public decimal Latitude { get; set; }

        /// <summary>
        /// GPS longitude coordinate (decimal degrees)
        /// Range: -180 to +180
        /// </summary>
        [Required(ErrorMessage = "Longitude is required")]
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
        /// Timestamp when the location was captured (defaults to current UTC time)
        /// </summary>
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for location responses
    /// </summary>
    public class LocationResponseDto
    {
        /// <summary>
        /// Unique identifier for the location record
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User ID who recorded this location
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
        /// Timestamp when the location was recorded
        /// </summary>
        public DateTime Timestamp { get; set; }

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
        /// Optional bearing/heading in degrees
        /// </summary>
        public decimal? Bearing { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for user status information
    /// </summary>
    public class UserStatusDto
    {
        /// <summary>
        /// User's unique identifier
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User's display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the user is currently online
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// User's current latitude (if sharing and online)
        /// </summary>
        public decimal? CurrentLatitude { get; set; }

        /// <summary>
        /// User's current longitude (if sharing and online)
        /// </summary>
        public decimal? CurrentLongitude { get; set; }

        /// <summary>
        /// Timestamp of the user's last activity
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Date when the user account was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
