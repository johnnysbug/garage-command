using Foundation;
using UIKit;
using System;

namespace GarageCommand.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		UIWindow _window;
		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
		{
			_window = new UIWindow(UIScreen.MainScreen.Bounds);
			_window.RootViewController = new GarageViewController();
			_window.MakeKeyAndVisible();
			return true;
		}
	}
}

