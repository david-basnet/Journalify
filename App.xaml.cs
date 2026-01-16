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

#if WINDOWS
		window.Created += (s, e) =>
		{
			var handler = window.Handler;
			if (handler != null)
			{
				var platformView = handler.PlatformView;
				if (platformView is Microsoft.UI.Xaml.Window nativeWindow)
				{
					try
					{
						var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
						var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
						var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
						
						if (appWindow != null)
						{
							appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
							var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
							if (displayArea != null)
							{
								var workArea = displayArea.WorkArea;
								appWindow.MoveAndResize(workArea);
							}
						}
					}
					catch
					{
						var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
						var screenWidth = displayInfo.Width / displayInfo.Density;
						var screenHeight = displayInfo.Height / displayInfo.Density;
						window.Width = screenWidth;
						window.Height = screenHeight;
						window.X = 0;
						window.Y = 0;
					}
				}
			}
		};
#endif

		return window;
	}
}
