using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using System.Runtime.InteropServices;
using ObjCRuntime;

namespace FamilyManager
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register( "AppDelegate" )]
    public partial class AppDelegate : UIApplicationDelegate
    {
        // class-level declarations
        UIWindow window;

        CoreViewController CoreViewController { get; set; }

        //
        // This method is invoked when the application has loaded and is ready to run. In this
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching( UIApplication app, NSDictionary options )
        {
            // create a new window instance based on the screen size
            window = new UIWindow( UIScreen.MainScreen.Bounds );
			
            // If you have defined a root view controller, set it here:
            CoreViewController = new CoreViewController();
            window.RootViewController = CoreViewController;
			
            // make the window visible
            window.MakeKeyAndVisible( );
			
            return true;
        }

        public override void OnActivated(UIApplication application)
        {
            Rock.Mobile.Util.Debug.WriteLine("OnActivated called, App is active.");

            CoreViewController.OnActivated( );
        }
        public override void WillEnterForeground(UIApplication application)
        {
            Rock.Mobile.Util.Debug.WriteLine("App will enter foreground");

            //CoreViewController.WillEnterForeground( );
        }

        public override void OnResignActivation(UIApplication application)
        {
            CoreViewController.OnResignActivation( );
        }

        public override void DidEnterBackground(UIApplication application)
        {
            CoreViewController.DidEnterBackground( );
        }
    }
}

