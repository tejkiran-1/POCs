using System.ComponentModel.DataAnnotations;

namespace LocationTrackerApp.Models
{
    /// <summary>
    /// Represents a user in the location tracking system
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User's email address (also serves as username)
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's display name
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Full display name
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        /// <summary>
        /// Current latitude position (if available)
        /// </summary>
        public decimal? CurrentLatitude { get; set; }

        /// <summary>
        /// Current longitude position (if available)
        /// </summary>
        public decimal? CurrentLongitude { get; set; }

        /// <summary>
        /// Whether the user is currently online and sharing location
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Whether the user is currently sharing their location
        /// </summary>
        public bool IsLocationSharing { get; set; }

        /// <summary>
        /// When the user's location was last updated
        /// </summary>
        public DateTime? LastLocationUpdate { get; set; }

        /// <summary>
        /// When the user account was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
