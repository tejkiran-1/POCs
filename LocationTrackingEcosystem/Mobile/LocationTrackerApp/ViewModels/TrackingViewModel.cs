using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocationTrackerApp.Models;
using LocationTrackerApp.Services;

namespace LocationTrackerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the main tracking page with modern MVVM implementation
    /// </summary>
    public partial class TrackingViewModel : ObservableObject
    {
        private readonly ILocationService _locationService;
        private readonly IAuthService _authService;

        public TrackingViewModel(ILocationService locationService, IAuthService authService)
        {
            _locationService = locationService;
            _authService = authService;

            // Subscribe to location service events
            _locationService.LocationUpdated += OnLocationUpdated;
            _locationService.TrackingStatusChanged += OnTrackingStatusChanged;
        }

        [ObservableProperty]
        private bool isLocationSharing = false;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = "Ready to share location";

        [ObservableProperty]
        private string userName = "User";

        [ObservableProperty]
        private DeviceLocation? currentLocation;

        [ObservableProperty]
        private string locationText = "Location not available";

        /// <summary>
        /// Current latitude for UI binding
        /// </summary>
        public double CurrentLatitude => CurrentLocation != null ? (double)CurrentLocation.Latitude : 0.0;

        /// <summary>
        /// Current longitude for UI binding
        /// </summary>
        public double CurrentLongitude => CurrentLocation != null ? (double)CurrentLocation.Longitude : 0.0;

        [ObservableProperty]
        private bool hasLocationPermission = false;

        [ObservableProperty]
        private string shareButtonText = "Start Sharing";

        [ObservableProperty]
        private Color shareButtonColor = Colors.Green;

        [ObservableProperty]
        private string lastUpdateTime = "Never";

        /// <summary>
        /// Alias command for ToggleLocationSharing (used by UI)
        /// </summary>
        public IRelayCommand ToggleTrackingCommand => ToggleLocationSharingCommand;

        /// <summary>
        /// Alias command for RefreshLocation (used by UI)
        /// </summary>
        public IRelayCommand UpdateLocationCommand => RefreshLocationCommand;

        /// <summary>
        /// Alias property for StatusMessage (used by UI)
        /// </summary>
        public string TrackingStatusText => StatusMessage;

        /// <summary>
        /// Alias property for StatusMessage (used by UI for error display)
        /// </summary>
        public string ErrorMessage => StatusMessage;

        /// <summary>
        /// Check if there's an error message to display
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(StatusMessage) && StatusMessage.Contains("Error");

        /// <summary>
        /// Initialize the view model
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Get current user info
                var user = await _authService.GetCurrentUserAsync();
                if (user != null)
                {
                    UserName = user.FullName;
                }

                // Check location permission
                HasLocationPermission = await _locationService.CheckLocationPermissionAsync();
                
                // Update tracking status
                IsLocationSharing = _locationService.IsTrackingActive;
                UpdateUI();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initialization error: {ex.Message}";
            }
        }

        /// <summary>
        /// Command to toggle location sharing
        /// </summary>
        [RelayCommand]
        private async Task ToggleLocationSharingAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;

                if (!HasLocationPermission)
                {
                    HasLocationPermission = await _locationService.RequestLocationPermissionAsync();
                    if (!HasLocationPermission)
                    {
                        StatusMessage = "Location permission is required to share your location";
                        return;
                    }
                }

                if (IsLocationSharing)
                {
                    // Stop sharing
                    await _locationService.StopLocationTrackingAsync();
                    StatusMessage = "Location sharing stopped";
                }
                else
                {
                    // Start sharing
                    await _locationService.StartLocationTrackingAsync();
                    StatusMessage = "Location sharing started";
                    
                    // Get initial location
                    var location = await _locationService.GetCurrentLocationAsync();
                    if (location != null)
                    {
                        CurrentLocation = location;
                        UpdateLocationText();
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Command to refresh current location
        /// </summary>
        [RelayCommand]
        private async Task RefreshLocationAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Getting current location...";

                if (!HasLocationPermission)
                {
                    HasLocationPermission = await _locationService.RequestLocationPermissionAsync();
                    if (!HasLocationPermission)
                    {
                        StatusMessage = "Location permission is required";
                        return;
                    }
                }

                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    CurrentLocation = location;
                    UpdateLocationText();
                    StatusMessage = "Location updated";
                }
                else
                {
                    StatusMessage = "Unable to get current location";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error getting location: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Command to logout
        /// </summary>
        [RelayCommand]
        private async Task LogoutAsync()
        {
            try
            {
                // Stop location sharing if active
                if (IsLocationSharing)
                {
                    await _locationService.StopLocationTrackingAsync();
                }

                // Logout
                await _authService.LogoutAsync();

                // Navigate to login page
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Logout error: {ex.Message}";
            }
        }

        /// <summary>
        /// Handle location updates from the service
        /// </summary>
        private void OnLocationUpdated(object? sender, DeviceLocation location)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentLocation = location;
                UpdateLocationText();
                StatusMessage = $"Location updated at {location.Timestamp:HH:mm:ss}";
                LastUpdateTime = location.Timestamp.ToString("HH:mm:ss");
                
                // Notify UI that coordinate properties have changed
                OnPropertyChanged(nameof(CurrentLatitude));
                OnPropertyChanged(nameof(CurrentLongitude));
            });
        }

        /// <summary>
        /// Handle tracking status changes from the service
        /// </summary>
        private void OnTrackingStatusChanged(object? sender, bool isTracking)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsLocationSharing = isTracking;
                UpdateUI();
            });
        }

        /// <summary>
        /// Update UI elements based on current state
        /// </summary>
        private void UpdateUI()
        {
            if (IsLocationSharing)
            {
                ShareButtonText = "Stop Sharing";
                ShareButtonColor = Colors.Red;
            }
            else
            {
                ShareButtonText = "Start Sharing";
                ShareButtonColor = Colors.Green;
            }
        }

        /// <summary>
        /// Update location display text
        /// </summary>
        private void UpdateLocationText()
        {
            if (CurrentLocation != null)
            {
                var accuracy = CurrentLocation.Accuracy.HasValue ? $" (±{CurrentLocation.Accuracy:F0}m)" : "";
                LocationText = $"Lat: {CurrentLocation.Latitude:F6}, Lng: {CurrentLocation.Longitude:F6}{accuracy}";
            }
            else
            {
                LocationText = "Location not available";
            }
        }

        /// <summary>
        /// Notify coordinate properties when location changes
        /// </summary>
        partial void OnCurrentLocationChanged(DeviceLocation? value)
        {
            OnPropertyChanged(nameof(CurrentLatitude));
            OnPropertyChanged(nameof(CurrentLongitude));
        }
    }
}
