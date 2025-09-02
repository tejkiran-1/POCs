using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LocationTrackingApi.Services;
using LocationTrackingApi.DTOs;
using System.Security.Claims;

namespace LocationTrackingApi.Controllers
{
    /// <summary>
    /// Authentication controller handling user registration, login, and logout
    /// Implements security-first principles with comprehensive validation and logging
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="registerDto">User registration data</param>
        /// <returns>Authentication response with JWT token</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Registration validation failed: {Errors}", string.Join(", ", errors));

                    return BadRequest(new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = $"Validation failed: {string.Join(", ", errors)}"
                    });
                }

                var result = await _authService.RegisterAsync(registerDto);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
                    return Ok(result);
                }

                _logger.LogWarning("Registration failed for {Email}: {Message}", registerDto.Email, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", registerDto.Email);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred during registration."
                });
            }
        }

        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        /// <param name="loginDto">User login credentials</param>
        /// <returns>Authentication response with JWT token</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Login validation failed: {Errors}", string.Join(", ", errors));

                    return BadRequest(new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = $"Validation failed: {string.Join(", ", errors)}"
                    });
                }

                var result = await _authService.LoginAsync(loginDto);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
                    return Ok(result);
                }

                _logger.LogWarning("Login failed for {Email}: {Message}", loginDto.Email, result.Message);
                
                // Return 401 for invalid credentials, 400 for other validation issues
                if (result.Message.Contains("Invalid email or password"))
                {
                    return Unauthorized(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}", loginDto.Email);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred during login."
                });
            }
        }

        /// <summary>
        /// Log out the authenticated user
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Logout attempted without valid user ID");
                    return Unauthorized(new { message = "Invalid user session" });
                }

                var success = await _authService.LogoutAsync(userId);

                if (success)
                {
                    _logger.LogInformation("User {UserId} logged out successfully", userId);
                    return Ok(new { message = "Logout successful", timestamp = DateTime.UtcNow });
                }

                _logger.LogWarning("Logout failed for user {UserId}", userId);
                return BadRequest(new { message = "Logout failed" });
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogError(ex, "Unexpected error during logout for user {UserId}", userId);
                
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An unexpected error occurred during logout." });
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        /// <returns>Current user details</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var displayName = User.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user session" });
                }

                return Ok(new
                {
                    userId,
                    email,
                    displayName,
                    isAuthenticated = true,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogError(ex, "Error getting current user info for {UserId}", userId);
                
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An error occurred while retrieving user information." });
            }
        }

        /// <summary>
        /// Refresh JWT token (placeholder for future implementation)
        /// </summary>
        /// <param name="refreshTokenDto">Refresh token data</param>
        /// <returns>New JWT token</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
                {
                    return BadRequest(new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "Refresh token is required."
                    });
                }

                var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "An error occurred during token refresh."
                });
            }
        }
    }

    /// <summary>
    /// DTO for refresh token requests
    /// </summary>
    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
