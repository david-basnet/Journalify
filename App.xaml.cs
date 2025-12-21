namespace MauiApp1;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) 
		{ 
			Title = "Journal App"
		};

		// Set to maximize on Windows by using screen dimensions
		#if WINDOWS
		var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
		window.Width = displayInfo.Width / displayInfo.Density;
		window.Height = displayInfo.Height / displayInfo.Density;
		#endif

		return window;
	}
}
