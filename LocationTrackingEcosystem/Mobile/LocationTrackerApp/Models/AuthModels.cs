using System.ComponentModel.DataAnnotations;

namespace LocationTrackerApp.Models
{
    /// <summary>
    /// Data Transfer Object for user registration
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// User's email address
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's chosen password
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Password confirmation
        /// </summary>
        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// User's first name
        /// </summary>
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name
        /// </summary>
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data Transfer Object for user login
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// User's email address
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's password
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Whether to remember the user's login
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }

    /// <summary>
    /// Authentication response from the API
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// Whether the authentication was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// JWT access token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time
        /// </summary>
        public DateTime Expiration { get; set; }

        /// <summary>
        /// User information
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Response message (success or error)
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
