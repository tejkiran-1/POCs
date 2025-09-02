using System.ComponentModel.DataAnnotations;

namespace LocationTrackingApi.DTOs
{
    /// <summary>
    /// Data Transfer Object for user registration requests
    /// </summary>
    public class RegisterDto
    {
        /// <summary>
        /// Email address for the new user account
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password for the new user account
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the user
        /// </summary>
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data Transfer Object for user login requests
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// Email address for authentication
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password for authentication
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data Transfer Object for authentication responses
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>
        /// Indicates if the authentication operation was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Message describing the result of the authentication operation
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// JWT token for authenticated requests
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// User ID of the authenticated user
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the authenticated user
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the authenticated user
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time
        /// </summary>
        public DateTime Expires { get; set; }
    }
}
