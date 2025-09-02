using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LocationTrackingApi.Services;
using LocationTrackingApi.DTOs;
using System.Security.Claims;

namespace LocationTrackingApi.Controllers
{
    /// <summary>
    /// Location controller for GPS tracking operations and user location management
    /// Implements real-time location updates with efficient data handling
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly ILogger<LocationController> _logger;

        public LocationController(ILocationService locationService, ILogger<LocationController> logger)
        {
            _locationService = locationService;
            _logger = logger;
        }

        /// <summary>
        /// Update the current user's location
        /// </summary>
        /// <param name="locationUpdate">GPS location data</param>
        /// <returns>Success status</returns>
        [HttpPost("update")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateLocation([FromBody] LocationUpdateDto locationUpdate)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Location update validation failed: {Errors}", string.Join(", ", errors));

                    return BadRequest(new
                    {
                        success = false,
                        message = $"Validation failed: {string.Join(", ", errors)}",
                        timestamp = DateTime.UtcNow
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Location update attempted without valid user ID");
                    return Unauthorized(new { message = "Invalid user session" });
                }

                var success = await _locationService.UpdateUserLocationAsync(userId, locationUpdate);

                if (success)
                {
                    _logger.LogDebug("Location updated for user {UserId}: {Lat}, {Lng}", 
                        userId, locationUpdate.Latitude, locationUpdate.Longitude);

                    return Ok(new
                    {
                        success = true,
                        message = "Location updated successfully",
                        timestamp = DateTime.UtcNow,
                        location = new
                        {
                            latitude = locationUpdate.Latitude,
                            longitude = locationUpdate.Longitude,
                            accuracy = locationUpdate.Accuracy
                        }
                    });
                }

                _logger.LogWarning("Failed to update location for user {UserId}", userId);
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to update location",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogError(ex, "Error updating location for user {UserId}", userId);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An error occurred while updating location",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get list of all online users with their current locations
        /// </summary>
        /// <returns>List of online users</returns>
        [HttpGet("online-users")]
        [ProducesResponseType(typeof(IEnumerable<UserStatusDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<UserStatusDto>>> GetOnlineUsers()
        {
            try
            {
                var onlineUsers = await _locationService.GetOnlineUsersAsync();
                
                _logger.LogDebug("Retrieved {Count} online users", onlineUsers.Count());
                
                return Ok(onlineUsers);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogError(ex, "Error retrieving online users for user {UserId}", userId);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving online users",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get location history for the current user
        /// </summary>
        /// <param name="from">Start date for history (optional)</param>
        /// <param name="to">End date for history (optional)</param>
        /// <returns>Location history</returns>
        [HttpGet("history")]
        [ProducesResponseType(typeof(IEnumerable<LocationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetLocationHistory(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                // Validate date range
                if (from.HasValue && to.HasValue && from.Value > to.Value)
                {
                    return BadRequest(new
                    {
                        message = "From date cannot be later than to date",
                        timestamp = DateTime.UtcNow
                    });
                }

                // Limit history to last 30 days if no dates specified
                if (!from.HasValue && !to.HasValue)
                {
                    from = DateTime.UtcNow.AddDays(-30);
                    to = DateTime.UtcNow;
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user session" });
                }

                var history = await _locationService.GetUserLocationHistoryAsync(userId, from, to);
                
                _logger.LogDebug("Retrieved {Count} location records for user {UserId}", 
                    history.Count(), userId);
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogError(ex, "Error retrieving location history for user {UserId}", userId);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving location history",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get location history for a specific user (admin/authorized users only)
        /// </summary>
        /// <param name="userId">Target user ID</param>
        /// <param name="from">Start date for history (optional)</param>
        /// <param name="to">End date for history (optional)</param>
        /// <returns>User's location history</returns>
        [HttpGet("history/{userId}")]
        [ProducesResponseType(typeof(IEnumerable<LocationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetUserLocationHistory(
            string userId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { message = "Invalid user session" });
                }

                // For now, users can only access their own history
                // TODO: Implement role-based access for admin users
                if (currentUserId != userId)
                {
                    _logger.LogWarning("User {CurrentUserId} attempted to access location history of user {TargetUserId}", 
                        currentUserId, userId);
                    
                    return Forbid("You can only access your own location history");
                }

                // Validate date range
                if (from.HasValue && to.HasValue && from.Value > to.Value)
                {
                    return BadRequest(new
                    {
                        message = "From date cannot be later than to date",
                        timestamp = DateTime.UtcNow
                    });
                }

                var history = await _locationService.GetUserLocationHistoryAsync(userId, from, to);
                
                _logger.LogDebug("Retrieved {Count} location records for user {UserId}", 
                    history.Count(), userId);
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving location history for user {UserId}", userId);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving location history",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get current location for a specific user
        /// </summary>
        /// <param name="userId">Target user ID</param>
        /// <returns>User's current location if online</returns>
        [HttpGet("current/{userId}")]
        [ProducesResponseType(typeof(LocationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocationResponseDto>> GetUserCurrentLocation(string userId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { message = "Invalid user session" });
                }

                var location = await _locationService.GetUserCurrentLocationAsync(userId);
                
                if (location == null)
                {
                    return NotFound(new
                    {
                        message = "User is not online or location not available",
                        userId,
                        timestamp = DateTime.UtcNow
                    });
                }

                return Ok(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current location for user {UserId}", userId);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving current location",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Set current user as offline
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("offline")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SetOffline()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user session" });
                }

                var success = await _locationService.SetUserOfflineAsync(userId);

                if (success)
                {
                    _logger.LogInformation("User {UserId} set to offline", userId);
                    
                    return Ok(new
                    {
                        success = true,
                        message = "User set to offline",
                        timestamp = DateTime.UtcNow
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Failed to set user offline",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogError(ex, "Error setting user {UserId} offline", userId);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An error occurred while setting user offline",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
