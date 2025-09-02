using LocationTrackerApp.ViewModels;

namespace LocationTrackerApp.Views;

public partial class TrackingPage : ContentPage
{
    private readonly TrackingViewModel _viewModel;

    public TrackingPage(TrackingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
