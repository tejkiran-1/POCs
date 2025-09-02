using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LocationTrackingApi.Models;
using LocationTrackingApi.DTOs;

namespace LocationTrackingApi.Services
{
    /// <summary>
    /// Service for handling authentication operations including JWT token generation
    /// Implements security-first principles with comprehensive validation
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> LogoutAsync(string userId);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        string GenerateJwtToken(ApplicationUser user);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user with validation and security checks
        /// </summary>
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(registerDto.Email) || 
                    string.IsNullOrWhiteSpace(registerDto.Password) ||
                    string.IsNullOrWhiteSpace(registerDto.DisplayName))
                {
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "All fields are required."
                    };
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "User with this email already exists."
                    };
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    DisplayName = registerDto.DisplayName,
                    CreatedAt = DateTime.UtcNow,
                    IsOnline = false
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("User registration failed for {Email}: {Errors}", registerDto.Email, errors);
                    
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = $"Registration failed: {errors}"
                    };
                }

                // Generate JWT token
                var token = GenerateJwtToken(user);

                _logger.LogInformation("User {Email} registered successfully", registerDto.Email);

                return new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = "Registration successful",
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", registerDto.Email);
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "An error occurred during registration."
                };
            }
        }

        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(loginDto.Email) || 
                    string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "Email and password are required."
                    };
                }

                // Find user
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt with non-existent email: {Email}", loginDto.Email);
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password."
                    };
                }

                // Check password
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed login attempt for user: {Email}", loginDto.Email);
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password."
                    };
                }

                // Update user online status
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Generate JWT token
                var token = GenerateJwtToken(user);

                _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

                return new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.DisplayName ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login for {Email}", loginDto.Email);
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "An error occurred during login."
                };
            }
        }

        /// <summary>
        /// Log out user and update online status
        /// </summary>
        public async Task<bool> LogoutAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    
                    _logger.LogInformation("User {UserId} logged out successfully", userId);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Refresh JWT token (placeholder for future implementation)
        /// </summary>
        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            // TODO: Implement refresh token logic
            await Task.CompletedTask;
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Refresh token functionality not implemented yet."
            };
        }

        /// <summary>
        /// Generate JWT token with user claims
        /// </summary>
        public string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT Secret Key is not configured");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.DisplayName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
