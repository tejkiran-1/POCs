using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LocationTrackingApi.Models
{
    /// <summary>
    /// Extended user model that inherits from IdentityUser
    /// Represents a registered user in the location tracking system
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Display name for the user
        /// </summary>
        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the user is currently online and sharing location
        /// </summary>
        public bool IsOnline { get; set; } = false;

        /// <summary>
        /// Timestamp when the user last updated their location
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Current latitude of the user (if sharing location)
        /// </summary>
        public decimal? CurrentLatitude { get; set; }

        /// <summary>
        /// Current longitude of the user (if sharing location)
        /// </summary>
        public decimal? CurrentLongitude { get; set; }

        /// <summary>
        /// Collection of location history records for this user
        /// </summary>
        public virtual ICollection<LocationRecord> LocationHistory { get; set; } = new List<LocationRecord>();

        /// <summary>
        /// Date when the user account was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
