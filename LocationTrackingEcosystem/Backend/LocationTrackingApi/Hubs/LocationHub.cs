using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using LocationTrackingApi.DTOs;
using LocationTrackingApi.Services;
using System.Security.Claims;

namespace LocationTrackingApi.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time location tracking and user status updates
    /// Implements secure, authenticated real-time communication for location data
    /// </summary>
    [Authorize]
    public class LocationHub : Hub
    {
        private readonly ILocationService _locationService;
        private readonly ILogger<LocationHub> _logger;

        public LocationHub(ILocationService locationService, ILogger<LocationHub> logger)
        {
            _locationService = locationService;
            _logger = logger;
        }

        /// <summary>
        /// Handle user connection - join location tracking group
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";

            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to the location tracking group
                await Groups.AddToGroupAsync(Context.ConnectionId, "LocationTrackers");
                
                _logger.LogInformation("User {UserId} ({UserName}) connected to LocationHub with connection {ConnectionId}", 
                    userId, userName, Context.ConnectionId);

                // Notify other users that this user is online
                await Clients.GroupExcept("LocationTrackers", Context.ConnectionId)
                    .SendAsync("UserConnected", new
                    {
                        UserId = userId,
                        DisplayName = userName,
                        ConnectedAt = DateTime.UtcNow
                    });
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Handle user disconnection - remove from groups and update status
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";

            if (!string.IsNullOrEmpty(userId))
            {
                // Set user offline in the database
                await _locationService.SetUserOfflineAsync(userId);

                // Remove user from location tracking group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "LocationTrackers");

                _logger.LogInformation("User {UserId} ({UserName}) disconnected from LocationHub. Connection: {ConnectionId}", 
                    userId, userName, Context.ConnectionId);

                // Notify other users that this user is offline
                await Clients.Group("LocationTrackers")
                    .SendAsync("UserDisconnected", new
                    {
                        UserId = userId,
                        DisplayName = userName,
                        DisconnectedAt = DateTime.UtcNow
                    });

                if (exception != null)
                {
                    _logger.LogWarning(exception, "User {UserId} disconnected with exception", userId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Update user location and broadcast to connected clients
        /// </summary>
        /// <param name="locationUpdate">Location update data</param>
        public async Task UpdateLocation(LocationUpdateDto locationUpdate)
        {
            try
            {
                var userId = Context.UserIdentifier;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("UpdateLocation called without valid user ID");
                    return;
                }

                // Update location in database
                var success = await _locationService.UpdateUserLocationAsync(userId, locationUpdate);
                
                if (success)
                {
                    var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                    // Broadcast location update to all connected clients except sender
                    await Clients.GroupExcept("LocationTrackers", Context.ConnectionId)
                        .SendAsync("LocationUpdated", new
                        {
                            UserId = userId,
                            DisplayName = userName,
                            Latitude = locationUpdate.Latitude,
                            Longitude = locationUpdate.Longitude,
                            Accuracy = locationUpdate.Accuracy,
                            Timestamp = DateTime.UtcNow
                        });

                    // Send confirmation to sender
                    await Clients.Caller.SendAsync("LocationUpdateConfirmed", new
                    {
                        Success = true,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogDebug("Location updated and broadcasted for user {UserId}: {Lat}, {Lng}", 
                        userId, locationUpdate.Latitude, locationUpdate.Longitude);
                }
                else
                {
                    // Send error to sender
                    await Clients.Caller.SendAsync("LocationUpdateConfirmed", new
                    {
                        Success = false,
                        Message = "Failed to update location",
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogWarning("Failed to update location for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateLocation for user {UserId}", Context.UserIdentifier);
                
                await Clients.Caller.SendAsync("LocationUpdateConfirmed", new
                {
                    Success = false,
                    Message = "Internal server error",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Join a specific tracking group (for future group-based tracking features)
        /// </summary>
        /// <param name="groupName">Name of the group to join</param>
        public async Task JoinTrackingGroup(string groupName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    await Clients.Caller.SendAsync("Error", "Group name cannot be empty");
                    return;
                }

                // Sanitize group name
                var sanitizedGroupName = $"Group_{groupName.Replace(" ", "_")}";
                
                await Groups.AddToGroupAsync(Context.ConnectionId, sanitizedGroupName);
                
                var userId = Context.UserIdentifier;
                var userName = Context.User?.Identity?.Name ?? "Unknown";
                
                _logger.LogInformation("User {UserId} joined tracking group {GroupName}", userId, sanitizedGroupName);
                
                await Clients.Caller.SendAsync("JoinedGroup", new
                {
                    GroupName = sanitizedGroupName,
                    JoinedAt = DateTime.UtcNow
                });

                // Notify other group members
                await Clients.GroupExcept(sanitizedGroupName, Context.ConnectionId)
                    .SendAsync("UserJoinedGroup", new
                    {
                        UserId = userId,
                        DisplayName = userName,
                        GroupName = sanitizedGroupName,
                        JoinedAt = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining tracking group {GroupName} for user {UserId}", 
                    groupName, Context.UserIdentifier);
                
                await Clients.Caller.SendAsync("Error", "Failed to join tracking group");
            }
        }

        /// <summary>
        /// Leave a specific tracking group
        /// </summary>
        /// <param name="groupName">Name of the group to leave</param>
        public async Task LeaveTrackingGroup(string groupName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    await Clients.Caller.SendAsync("Error", "Group name cannot be empty");
                    return;
                }

                var sanitizedGroupName = $"Group_{groupName.Replace(" ", "_")}";
                
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, sanitizedGroupName);
                
                var userId = Context.UserIdentifier;
                var userName = Context.User?.Identity?.Name ?? "Unknown";
                
                _logger.LogInformation("User {UserId} left tracking group {GroupName}", userId, sanitizedGroupName);
                
                await Clients.Caller.SendAsync("LeftGroup", new
                {
                    GroupName = sanitizedGroupName,
                    LeftAt = DateTime.UtcNow
                });

                // Notify other group members
                await Clients.Group(sanitizedGroupName)
                    .SendAsync("UserLeftGroup", new
                    {
                        UserId = userId,
                        DisplayName = userName,
                        GroupName = sanitizedGroupName,
                        LeftAt = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving tracking group {GroupName} for user {UserId}", 
                    groupName, Context.UserIdentifier);
                
                await Clients.Caller.SendAsync("Error", "Failed to leave tracking group");
            }
        }

        /// <summary>
        /// Get list of online users (for dashboard display)
        /// </summary>
        public async Task GetOnlineUsers()
        {
            try
            {
                var onlineUsers = await _locationService.GetOnlineUsersAsync();
                
                await Clients.Caller.SendAsync("OnlineUsersList", onlineUsers);
                
                _logger.LogDebug("Sent online users list to {UserId}", Context.UserIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online users for {UserId}", Context.UserIdentifier);
                await Clients.Caller.SendAsync("Error", "Failed to get online users");
            }
        }
    }
}
