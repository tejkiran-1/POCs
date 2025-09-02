using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocationTrackerApp.Models;
using LocationTrackerApp.Services;
using System.ComponentModel.DataAnnotations;

namespace LocationTrackerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Register page with modern MVVM implementation
    /// </summary>
    public partial class RegisterViewModel : ObservableValidator
    {
        private readonly IAuthService _authService;

        public RegisterViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        [ObservableProperty]
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        private string firstName = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        private string lastName = string.Empty;

        /// <summary>
        /// Full name property for UI binding (combines first and last name)
        /// </summary>
        public string FullName
        {
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var parts = value.Split(' ', 2);
                    FirstName = parts.Length > 0 ? parts[0] : string.Empty;
                    LastName = parts.Length > 1 ? parts[1] : string.Empty;
                }
            }
        }

        [ObservableProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        private string email = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        private string password = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Please confirm your password")]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasError = false;

        /// <summary>
        /// Command to handle registration action
        /// </summary>
        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (IsLoading)
                return;

            // Validate input
            ValidateAllProperties();
            
            // Check password confirmation
            if (Password != ConfirmPassword)
            {
                HasError = true;
                ErrorMessage = "Passwords do not match";
                return;
            }

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

                var registerRequest = new RegisterRequest
                {
                    FirstName = FirstName,
                    LastName = LastName,
                    Email = Email,
                    Password = Password,
                    ConfirmPassword = ConfirmPassword
                };

                var result = await _authService.RegisterAsync(registerRequest);

                if (result.IsSuccess)
                {
                    // Registration successful, navigate to main page
                    await Shell.Current.GoToAsync("//main");
                }
                else
                {
                    HasError = true;
                    ErrorMessage = result.Message ?? "Registration failed. Please try again.";
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
        /// Command to navigate back to login page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToLoginAsync()
        {
            await Shell.Current.GoToAsync("//login");
        }

        /// <summary>
        /// Clear error message when user starts typing
        /// </summary>
        partial void OnFirstNameChanged(string value)
        {
            ClearError();
            OnPropertyChanged(nameof(FullName));
        }

        partial void OnLastNameChanged(string value)
        {
            ClearError();
            OnPropertyChanged(nameof(FullName));
        }

        partial void OnEmailChanged(string value)
        {
            ClearError();
        }

        partial void OnPasswordChanged(string value)
        {
            ClearError();
        }

        partial void OnConfirmPasswordChanged(string value)
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
