using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LocationTrackingApi.Models;
using LocationTrackingApi.DTOs;
using LocationTrackingApi.Data;

namespace LocationTrackingApi.Services
{
    /// <summary>
    /// Service for handling location operations including GPS tracking and user location management
    /// Implements efficient data handling with validation and real-time capabilities
    /// </summary>
    public interface ILocationService
    {
        Task<bool> UpdateUserLocationAsync(string userId, LocationUpdateDto locationUpdate);
        Task<IEnumerable<UserStatusDto>> GetOnlineUsersAsync();
        Task<IEnumerable<LocationResponseDto>> GetUserLocationHistoryAsync(string userId, DateTime? from = null, DateTime? to = null);
        Task<LocationResponseDto?> GetUserCurrentLocationAsync(string userId);
        Task<bool> SetUserOfflineAsync(string userId);
        Task<int> CleanupOldLocationRecordsAsync(TimeSpan olderThan);
    }

    public class LocationService : ILocationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LocationService> _logger;

        public LocationService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<LocationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Update user's current location and store historical record
        /// </summary>
        public async Task<bool> UpdateUserLocationAsync(string userId, LocationUpdateDto locationUpdate)
        {
            try
            {
                // Validate coordinates
                if (!IsValidCoordinate(locationUpdate.Latitude, locationUpdate.Longitude))
                {
                    _logger.LogWarning("Invalid coordinates provided for user {UserId}: {Lat}, {Lng}", 
                        userId, locationUpdate.Latitude, locationUpdate.Longitude);
                    return false;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Update user's current location
                    user.CurrentLatitude = locationUpdate.Latitude;
                    user.CurrentLongitude = locationUpdate.Longitude;
                    user.IsOnline = true;
                    user.LastSeen = DateTime.UtcNow;

                    await _userManager.UpdateAsync(user);

                    // Create location history record
                    var locationRecord = new LocationRecord
                    {
                        UserId = userId,
                        Latitude = locationUpdate.Latitude,
                        Longitude = locationUpdate.Longitude,
                        Accuracy = locationUpdate.Accuracy,
                        Altitude = locationUpdate.Altitude,
                        Speed = locationUpdate.Speed,
                        Bearing = locationUpdate.Bearing,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.LocationRecords.Add(locationRecord);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogDebug("Location updated for user {UserId}: {Lat}, {Lng}", 
                        userId, locationUpdate.Latitude, locationUpdate.Longitude);

                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Get list of all online users with their current status
        /// </summary>
        public async Task<IEnumerable<UserStatusDto>> GetOnlineUsersAsync()
        {
            try
            {
                var onlineUsers = await _context.Users
                    .Where(u => u.IsOnline)
                    .Select(u => new UserStatusDto
                    {
                        UserId = u.Id,
                        DisplayName = u.DisplayName,
                        Email = u.Email ?? string.Empty,
                        IsOnline = u.IsOnline,
                        LastSeen = u.LastSeen,
                        CurrentLatitude = u.CurrentLatitude,
                        CurrentLongitude = u.CurrentLongitude,
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync();

                return onlineUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving online users");
                return Enumerable.Empty<UserStatusDto>();
            }
        }

        /// <summary>
        /// Get user's location history within a specified time range
        /// </summary>
        public async Task<IEnumerable<LocationResponseDto>> GetUserLocationHistoryAsync(string userId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.LocationRecords
                    .Where(lr => lr.UserId == userId);

                if (from.HasValue)
                {
                    query = query.Where(lr => lr.Timestamp >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(lr => lr.Timestamp <= to.Value);
                }

                var locationHistory = await query
                    .OrderByDescending(lr => lr.Timestamp)
                    .Take(1000) // Limit to prevent large data transfers
                    .Select(lr => new LocationResponseDto
                    {
                        Id = lr.Id,
                        UserId = lr.UserId,
                        Latitude = lr.Latitude,
                        Longitude = lr.Longitude,
                        Accuracy = lr.Accuracy,
                        Altitude = lr.Altitude,
                        Speed = lr.Speed,
                        Bearing = lr.Bearing,
                        Timestamp = lr.Timestamp
                    })
                    .ToListAsync();

                return locationHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving location history for user {UserId}", userId);
                return new List<LocationResponseDto>();
            }
        }

        /// <summary>
        /// Get user's current location
        /// </summary>
        public async Task<LocationResponseDto?> GetUserCurrentLocationAsync(string userId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new
                    {
                        u.CurrentLatitude,
                        u.CurrentLongitude,
                        u.LastSeen,
                        u.IsOnline
                    })
                    .FirstOrDefaultAsync();

                if (user == null || !user.IsOnline || 
                    !user.CurrentLatitude.HasValue || !user.CurrentLongitude.HasValue)
                {
                    return null;
                }

                return new LocationResponseDto
                {
                    Latitude = user.CurrentLatitude.Value,
                    Longitude = user.CurrentLongitude.Value,
                    Timestamp = user.LastSeen ?? DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current location for user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Set user as offline
        /// </summary>
        public async Task<bool> SetUserOfflineAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsOnline = false;
                user.LastSeen = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} set to offline", userId);
                }

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting user {UserId} offline", userId);
                return false;
            }
        }

        /// <summary>
        /// Clean up old location records to maintain database performance
        /// </summary>
        public async Task<int> CleanupOldLocationRecordsAsync(TimeSpan olderThan)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.Subtract(olderThan);
                
                var oldRecords = await _context.LocationRecords
                    .Where(lr => lr.Timestamp < cutoffDate)
                    .ToListAsync();

                if (oldRecords.Any())
                {
                    _context.LocationRecords.RemoveRange(oldRecords);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Cleaned up {Count} old location records older than {CutoffDate}", 
                        oldRecords.Count, cutoffDate);
                }

                return oldRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during location records cleanup");
                return 0;
            }
        }

        /// <summary>
        /// Validate GPS coordinates
        /// </summary>
        private static bool IsValidCoordinate(decimal latitude, decimal longitude)
        {
            return latitude >= -90 && latitude <= 90 && 
                   longitude >= -180 && longitude <= 180;
        }
    }
}
