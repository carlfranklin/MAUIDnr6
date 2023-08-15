namespace MAUIDnr1;

public partial class App : Application
{
    private ApiService _apiService;

	public App(ApiService apiService)
	{
		InitializeComponent();
        _apiService = apiService;
        MainPage = new MainPage();
	}

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Created += Window_Created;
        window.Destroying += Window_Destroying;
        return window;
    }

    private void Window_Destroying(object sender, EventArgs e)
    {
        // Tell CascadingAppState to CleanUp
        _apiService.RaiseShuttingDownEvent();
    }

    private async void Window_Created(object sender, EventArgs e)
    {
#if WINDOWS
        const int defaultWidth = 1280;
        const int defaultHeight = 720;

        var window = (Window)sender;
        window.Width = defaultWidth;
        window.Height = defaultHeight;
        window.X = -defaultWidth;
        window.Y = -defaultHeight;

        await window.Dispatcher.DispatchAsync(() => { });

        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
        window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;
#endif
    }
}
