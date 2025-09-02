using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocationTrackerApp.Models;
using LocationTrackerApp.Services;
using System.ComponentModel.DataAnnotations;

namespace LocationTrackerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Login page with modern MVVM implementation
    /// </summary>
    public partial class LoginViewModel : ObservableValidator
    {
        private readonly IAuthService _authService;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        [ObservableProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        private string email = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Password is required")]
        private string password = string.Empty;

        [ObservableProperty]
        private bool rememberMe = false;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasError = false;

        /// <summary>
        /// Command to handle login action
        /// </summary>
        [RelayCommand]
        private async Task LoginAsync()
        {
            if (IsLoading)
                return;

            // Validate input
            ValidateAllProperties();
            if (HasErrors)
            {
                HasError = true;
                ErrorMessage = string.Join(", ", GetErrors().Select(e => e.ErrorMessage));
                return;
            }

            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var loginRequest = new LoginRequest
                {
                    Email = Email,
                    Password = Password,
                    RememberMe = RememberMe
                };

                var result = await _authService.LoginAsync(loginRequest);

                if (result.IsSuccess)
                {
                    // Navigation will be handled by the authentication state change event
                    await Shell.Current.GoToAsync("//main");
                }
                else
                {
                    HasError = true;
                    ErrorMessage = result.Message ?? "Login failed. Please check your credentials.";
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Command to navigate to register page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToRegisterAsync()
        {
            await Shell.Current.GoToAsync("//register");
        }

        /// <summary>
        /// Clear error message when user starts typing
        /// </summary>
        partial void OnEmailChanged(string value)
        {
            ClearError();
        }

        partial void OnPasswordChanged(string value)
        {
            ClearError();
        }

        private void ClearError()
        {
            if (HasError)
            {
                HasError = false;
                ErrorMessage = string.Empty;
            }
        }
    }
}
