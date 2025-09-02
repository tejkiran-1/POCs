using LocationTrackerApp.Services;

namespace LocationTrackerApp;

public partial class App : Application
{
    private readonly IAuthService _authService;

    public App(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;

        MainPage = new AppShell();

        // Subscribe to authentication state changes
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    protected override async void OnStart()
    {
        // Initialize auth service and check for existing token
        if (_authService is AuthService authService)
        {
            await authService.InitializeAsync();
        }

        // Navigate to appropriate page based on authentication state
        if (_authService.IsAuthenticated)
        {
            await Shell.Current.GoToAsync("//main");
        }
        else
        {
            await Shell.Current.GoToAsync("//login");
        }
    }

    private async void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (isAuthenticated)
            {
                await Shell.Current.GoToAsync("//main");
            }
            else
            {
                await Shell.Current.GoToAsync("//login");
            }
        });
    }
}
