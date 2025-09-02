using LocationTrackerApp.Models;
using Newtonsoft.Json;
using System.Text;

namespace LocationTrackerApp.Services
{
    /// <summary>
    /// Service for handling GPS location operations
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Get the current device location
        /// </summary>
        Task<DeviceLocation?> GetCurrentLocationAsync();

        /// <summary>
        /// Send location update to the backend API
        /// </summary>
        Task<bool> UpdateLocationAsync(LocationUpdateRequest request);

        /// <summary>
        /// Check if location permissions are granted
        /// </summary>
        Task<bool> CheckLocationPermissionAsync();

        /// <summary>
        /// Request location permissions from the user
        /// </summary>
        Task<bool> RequestLocationPermissionAsync();

        /// <summary>
        /// Start continuous location tracking
        /// </summary>
        Task StartLocationTrackingAsync();

        /// <summary>
        /// Stop continuous location tracking
        /// </summary>
        Task StopLocationTrackingAsync();

        /// <summary>
        /// Whether location tracking is currently active
        /// </summary>
        bool IsTrackingActive { get; }

        /// <summary>
        /// Event fired when location is updated
        /// </summary>
        event EventHandler<DeviceLocation> LocationUpdated;

        /// <summary>
        /// Event fired when location tracking status changes
        /// </summary>
        event EventHandler<bool> TrackingStatusChanged;
    }

    /// <summary>
    /// Implementation of location service
    /// </summary>
    public class LocationService : ILocationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private Timer? _locationTimer;
        private bool _isTrackingActive;
        private const int LOCATION_UPDATE_INTERVAL_MS = 30000; // 30 seconds

        public LocationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            
            // Configure base URL based on platform and device type
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // For Android real devices, we need to use the computer's actual IP address
                _baseUrl = "http://192.168.2.104:5167"; // Your Mac's IP address
            }
            else
            {
                _baseUrl = "http://localhost:5167"; // iOS simulator and other platforms
            }
        }

        public bool IsTrackingActive => _isTrackingActive;

        public event EventHandler<DeviceLocation>? LocationUpdated;
        public event EventHandler<bool>? TrackingStatusChanged;

        /// <summary>
        /// Get the current device location
        /// </summary>
        public async Task<DeviceLocation?> GetCurrentLocationAsync()
        {
            try
            {
                var hasPermission = await CheckLocationPermissionAsync();
                if (!hasPermission)
                {
                    hasPermission = await RequestLocationPermissionAsync();
                    if (!hasPermission)
                        return null;
                }

                var request = new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var location = await Geolocation.Default.GetLocationAsync(request);
                if (location != null)
                {
                    return new DeviceLocation
                    {
                        Latitude = (decimal)location.Latitude,
                        Longitude = (decimal)location.Longitude,
                        Accuracy = location.Accuracy.HasValue ? (decimal)location.Accuracy.Value : null,
                        Altitude = location.Altitude.HasValue ? (decimal)location.Altitude.Value : null,
                        Speed = location.Speed.HasValue ? (decimal)location.Speed.Value : null,
                        Bearing = location.Course.HasValue ? (decimal)location.Course.Value : null,
                        Timestamp = location.Timestamp.DateTime
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting location: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Send location update to the backend API
        /// </summary>
        public async Task<bool> UpdateLocationAsync(LocationUpdateRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/location/update", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating location: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if location permissions are granted
        /// </summary>
        public async Task<bool> CheckLocationPermissionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking location permission: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Request location permissions from the user
        /// </summary>
        public async Task<bool> RequestLocationPermissionAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error requesting location permission: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start continuous location tracking
        /// </summary>
        public async Task StartLocationTrackingAsync()
        {
            if (_isTrackingActive)
                return;

            var hasPermission = await CheckLocationPermissionAsync();
            if (!hasPermission)
            {
                hasPermission = await RequestLocationPermissionAsync();
                if (!hasPermission)
                    return;
            }

            _isTrackingActive = true;
            TrackingStatusChanged?.Invoke(this, true);

            // Start the timer for periodic location updates
            _locationTimer = new Timer(async _ => await UpdateLocationPeriodically(), 
                null, TimeSpan.Zero, TimeSpan.FromMilliseconds(LOCATION_UPDATE_INTERVAL_MS));
        }

        /// <summary>
        /// Stop continuous location tracking
        /// </summary>
        public async Task StopLocationTrackingAsync()
        {
            if (!_isTrackingActive)
                return;

            _isTrackingActive = false;
            _locationTimer?.Dispose();
            _locationTimer = null;

            TrackingStatusChanged?.Invoke(this, false);

            // Send one final update to mark user as offline
            var currentLocation = await GetCurrentLocationAsync();
            if (currentLocation != null)
            {
                var request = new LocationUpdateRequest
                {
                    Latitude = currentLocation.Latitude,
                    Longitude = currentLocation.Longitude,
                    Accuracy = currentLocation.Accuracy,
                    Altitude = currentLocation.Altitude,
                    Speed = currentLocation.Speed,
                    Bearing = currentLocation.Bearing,
                    Timestamp = DateTime.UtcNow
                };

                await UpdateLocationAsync(request);
            }
        }

        /// <summary>
        /// Periodic location update method
        /// </summary>
        private async Task UpdateLocationPeriodically()
        {
            if (!_isTrackingActive)
                return;

            try
            {
                var location = await GetCurrentLocationAsync();
                if (location != null)
                {
                    // Fire the location updated event
                    LocationUpdated?.Invoke(this, location);

                    // Send location to API
                    var request = new LocationUpdateRequest
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Accuracy = location.Accuracy,
                        Altitude = location.Altitude,
                        Speed = location.Speed,
                        Bearing = location.Bearing,
                        Timestamp = location.Timestamp
                    };

                    await UpdateLocationAsync(request);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in periodic location update: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            _locationTimer?.Dispose();
        }
    }
}
