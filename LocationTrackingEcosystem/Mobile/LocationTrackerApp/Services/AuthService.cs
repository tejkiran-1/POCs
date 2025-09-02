using LocationTrackerApp.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace LocationTrackerApp.Services
{
    /// <summary>
    /// Service for handling authentication operations with the backend API
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Register a new user account
        /// </summary>
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Login with email and password
        /// </summary>
        Task<AuthResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// Logout the current user
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Get the current user's information
        /// </summary>
        Task<User?> GetCurrentUserAsync();

        /// <summary>
        /// Check if user is currently authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Get the current JWT token
        /// </summary>
        string? CurrentToken { get; }

        /// <summary>
        /// Event fired when authentication state changes
        /// </summary>
        event EventHandler<bool> AuthenticationStateChanged;
    }

    /// <summary>
    /// Implementation of authentication service
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ISecureStorage _secureStorage;
        private readonly string _baseUrl;
        private string? _currentToken;
        private User? _currentUser;

        public AuthService(HttpClient httpClient, ISecureStorage secureStorage)
        {
            _httpClient = httpClient;
            _secureStorage = secureStorage;
            
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

        public bool IsAuthenticated => !string.IsNullOrEmpty(_currentToken);

        public string? CurrentToken => _currentToken;

        public event EventHandler<bool>? AuthenticationStateChanged;

        /// <summary>
        /// Register a new user account
        /// </summary>
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/register", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseJson);
                    if (authResponse != null && authResponse.IsSuccess)
                    {
                        await StoreTokenAsync(authResponse.Token);
                        _currentToken = authResponse.Token;
                        _currentUser = authResponse.User;
                        SetAuthHeader();
                        AuthenticationStateChanged?.Invoke(this, true);
                    }
                    return authResponse ?? new AuthResponse { IsSuccess = false, Message = "Invalid response from server" };
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<AuthResponse>(responseJson);
                    return errorResponse ?? new AuthResponse 
                    { 
                        IsSuccess = false, 
                        Message = $"Registration failed: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/login", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseJson);
                    if (authResponse != null && authResponse.IsSuccess)
                    {
                        await StoreTokenAsync(authResponse.Token);
                        _currentToken = authResponse.Token;
                        _currentUser = authResponse.User;
                        SetAuthHeader();
                        AuthenticationStateChanged?.Invoke(this, true);
                    }
                    return authResponse ?? new AuthResponse { IsSuccess = false, Message = "Invalid response from server" };
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<AuthResponse>(responseJson);
                    return errorResponse ?? new AuthResponse 
                    { 
                        IsSuccess = false, 
                        Message = $"Login failed: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Logout the current user
        /// </summary>
        public async Task LogoutAsync()
        {
            _secureStorage.Remove("auth_token");
            _currentToken = null;
            _currentUser = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            AuthenticationStateChanged?.Invoke(this, false);
        }

        /// <summary>
        /// Get the current user's information
        /// </summary>
        public async Task<User?> GetCurrentUserAsync()
        {
            if (_currentUser != null)
                return _currentUser;

            if (string.IsNullOrEmpty(_currentToken))
                return null;

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/auth/user");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _currentUser = JsonConvert.DeserializeObject<User>(json);
                    return _currentUser;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Initialize the service by checking for stored token
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _currentToken = await _secureStorage.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(_currentToken))
                {
                    SetAuthHeader();
                    _currentUser = await GetCurrentUserAsync();
                    AuthenticationStateChanged?.Invoke(this, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing auth service: {ex.Message}");
                await LogoutAsync(); // Clear any invalid stored data
            }
        }

        /// <summary>
        /// Store the JWT token securely
        /// </summary>
        private async Task StoreTokenAsync(string token)
        {
            await _secureStorage.SetAsync("auth_token", token);
        }

        /// <summary>
        /// Set the authorization header for HTTP requests
        /// </summary>
        private void SetAuthHeader()
        {
            if (!string.IsNullOrEmpty(_currentToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _currentToken);
            }
        }
    }
}
