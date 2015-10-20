using System;
using UIKit;
using System.IO;
using Newtonsoft.Json;
using Rock.Mobile.IO;
using FamilyManager.UI;
using CoreGraphics;
using Foundation;
using iOS;
using Rock.Mobile.PlatformSpecific.iOS.UI;
using Customization;
using Rock.Mobile.Network;

namespace FamilyManager
{
    public class CoreViewController : UINavigationController
    {
        ImageCropViewController ImageCropViewController { get; set; }

        UIImage ImageCropperPendingImage { get; set; }

        FirstRunViewController FirstRunViewController { get; set; }

        LoginViewController LoginViewController { get; set; }

        ContainerViewController ContainerViewController { get; set; }

        bool ViewDidAppearCalled { get; set; }

        DateTime LockTimer { get; set; }

        UILabel ConnectingLabel { get; set; }
        UIActivityIndicatorView ProgressIndicator { get; set; }

        public CoreViewController( )
        {
        }

        public override bool PrefersStatusBarHidden()
        {
            return true;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            ImageCropViewController = new ImageCropViewController( );
            LoginViewController = new LoginViewController( this );
            ContainerViewController = new ContainerViewController( this );
            FirstRunViewController = new FirstRunViewController( this );

            // load any current cached config
            Config.Instance.LoadFromDevice( );

            // color it with whatever we currently have.
            if ( Config.Instance.VisualSettings.TopHeaderBGColor != null && Config.Instance.VisualSettings.BackgroundColor != null )
            {
                ColorNavBar( Config.Instance.VisualSettings.TopHeaderBGColor );
                View.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.BackgroundColor );
            }
            else
            {
                // if either of our colors are null, this is probably our first run, so just use black.

                ColorNavBar( "#000000FF" );
                View.BackgroundColor = Theme.GetColor( "#000000FF" );
            }

            // create a simple centered label (slightly above center, actually) that lets the user know we're trying to connect
            ConnectingLabel = new UILabel();
            ConnectingLabel.Layer.AnchorPoint = CGPoint.Empty;
            ConnectingLabel.Font = ConnectingLabel.Font.WithSize( 32 );
            ConnectingLabel.Text = "Connecting to Rock";
            ConnectingLabel.TextColor = Theme.GetColor( "#6A6A6AFF" );
            ConnectingLabel.SizeToFit( );
            ConnectingLabel.Layer.Position = new CGPoint( ( View.Bounds.Width - ConnectingLabel.Bounds.Width ) / 2,
                                                         ( (View.Bounds.Height - ConnectingLabel.Bounds.Height ) / 2) - ConnectingLabel.Bounds.Height  );
            View.AddSubview( ConnectingLabel );

            // show a spinner so the user feels like there's activity happening
            ProgressIndicator = new UIActivityIndicatorView();
            ProgressIndicator.Layer.AnchorPoint = CGPoint.Empty;
            ProgressIndicator.ActivityIndicatorViewStyle = UIActivityIndicatorViewStyle.WhiteLarge;
            ProgressIndicator.Color = UIColor.Gray;
            ProgressIndicator.StartAnimating( );
            ProgressIndicator.Layer.Position = new CGPoint( ( View.Bounds.Width - ProgressIndicator.Bounds.Width ) / 2, ConnectingLabel.Frame.Bottom + 30 );
            View.AddSubview( ProgressIndicator );

            LockTimer = DateTime.Now;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            // iOS will call ViewDidAppear plenty of times, 
            // but we also have to wait until this is called to begin
            // showing view controllers, so we have to have a check for the first call.
            if ( ViewDidAppearCalled == false )
            {
                ViewDidAppearCalled = true;

                // when deciding which ViewController to start with, check to see if we have a RockURL
                if ( string.IsNullOrWhiteSpace( Config.Instance.RockURL ) == true )
                {
                    // we need to know where Rock is, so prompt them for this.
                    PresentViewController( FirstRunViewController, false, null );
                }
                // we're good, so do the normal launch
                else
                {
                    Start( );
                }
            }
            else
            {
                TryPresentImageCropper( );
            }
        }

        public void SetupComplete( )
        {
            Start( );
        }

        public void LoginComplete( )
        {
            // with login complete, dismiss it and launch the main viewController
            PopViewController( true );
        }

        void Start( )
        {
            RockApi.SetRockURL( Config.Instance.RockURL );
            RockApi.SetAuthorizationKey( Config.Instance.RockAuthorizationKey );

            // get the initial config (this is where we call the new Update)
            Config.Instance.UpdateCurrentConfigurationDefinedValue( 
                delegate(bool result )
                {
                    // failure or not, hide the connection UI
                    ProgressIndicator.Hidden = true;
                    ProgressIndicator.HidesWhenStopped = true;
                    ProgressIndicator.StopAnimating( );

                    ConnectingLabel.Hidden = true;

                    if( result )
                    {
                        // we're good to move on.

                        // first hide the FirstRun (in case it was showing)
                        if( FirstRunViewController != null )
                        {
                            FirstRunViewController.DismissViewController( true, null );
                        }

                        // update our theme
                        ColorNavBar( Config.Instance.VisualSettings.TopHeaderBGColor );
                        View.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.BackgroundColor );

                        // add the container, so that when we're done with the login it will already be there.
                        PushViewController( ContainerViewController, false );

                        DisplayLoginScreen( false );
                    }
                    else
                    {
                        // display an error and return them to the first run screen.
                        DisplayFirstRun( );
                    }
                } );
        }

        void ColorNavBar( string topHeaderBGColor )
        {
            // setup the style of the nav bar
            NavigationBar.TintColor = Theme.GetColor( topHeaderBGColor );

            // create a 1x1 image of the desired color and render it as an image
            UIImage solidColor = new UIImage();
            UIGraphics.BeginImageContext( new CGSize( 1, 1 ) );
            CGContext context = UIGraphics.GetCurrentContext( );

            context.SetFillColor( Theme.GetColor( topHeaderBGColor ).CGColor );
            context.FillRect( new CGRect( 0, 0, 1, 1 ) );

            solidColor = UIGraphics.GetImageFromCurrentImageContext( );

            UIGraphics.EndImageContext( );

            // now apply it as the background image, tiled.
            NavigationBar.BarTintColor = UIColor.Clear;
            NavigationBar.SetBackgroundImage( solidColor, UIBarMetrics.Default );
            NavigationBar.Translucent = false;
        }

        public static void ApplyTheme( UIViewController viewController )
        {
            // set the title image for the bar
            if ( FileCache.Instance.FileExists( Theme.LogoImageName ) == true )
            {
                MemoryStream imageStream = (MemoryStream)FileCache.Instance.LoadFile( Theme.LogoImageName );
                NSData imageData = NSData.FromStream( imageStream );
                viewController.NavigationItem.TitleView = new UIImageView( UIImage.LoadFromData( imageData, 2 ) );
            }


            // set the background image if it exists
            if ( FileCache.Instance.FileExists( Theme.BackgroundImageName ) == true )
            {
                MemoryStream imageStream = (MemoryStream)FileCache.Instance.LoadFile( Theme.BackgroundImageName );
                NSData imageData = NSData.FromStream( imageStream );
                viewController.View.Layer.Contents = UIImage.LoadFromData( imageData ).CGImage;
            }

            // and set the background color either way.
            viewController.View.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.BackgroundColor );
        }

        public delegate void OnCaptureComplete( NSData dataStream );
        OnCaptureComplete OnCaptureCompleteDelegate;

        public void BeginPictureCapture( OnCaptureComplete onCaptureComplete )
        {
            OnCaptureCompleteDelegate = onCaptureComplete;

            // only allow the camera if they HAVE one
            if( Rock.Mobile.Media.PlatformCamera.Instance.IsAvailable( ) )
            {
                // launch the camera
                string jpgFilename = System.IO.Path.Combine ( Environment.GetFolderPath(Environment.SpecialFolder.Personal), "cameraTemp.jpg" );
                Rock.Mobile.Media.PlatformCamera.Instance.CaptureImage( jpgFilename, this, delegate(object s, Rock.Mobile.Media.PlatformCamera.CaptureImageEventArgs args) 
                    {
                        // if the result is true, they either got a picture or pressed cancel
                        bool success = false;
                        if( args.Result == true )
                        {
                            // either way, no need for an error
                            success = true;

                            // if the image path is valid, they didn't cancel
                            if ( string.IsNullOrWhiteSpace( args.ImagePath ) == false )
                            {
                                // load the image for cropping
                                ImageCropperPendingImage = UIImage.FromFile( args.ImagePath );
                            }
                        }

                        if( success == false )
                        {
                            Rock.Mobile.Util.Debug.DisplayError( Strings.Camera_Error_Header, Strings.Camera_Error_Message );
                        }
                    });
            }
            else
            {
                // notify them they don't have a camera
                Rock.Mobile.Util.Debug.DisplayError( Strings.Camera_None_Header, Strings.Camera_None_Message );
                onCaptureComplete( null );
            }
        }

        public void TryPresentImageCropper( )
        {
            // if the image cropper is pending, launch it now.
            if ( ImageCropperPendingImage != null )
            {
                ImageCropViewController.Begin( ImageCropperPendingImage, 1.0f, HandleImageCropRequest );
                PresentModalViewController( ImageCropViewController, true );

                ImageCropperPendingImage = null;
            }
        }

        object HandleImageCropRequest( ImageCropViewController.Request request, object context )
        {
            switch ( request )
            {
                case ImageCropViewController.Request.SupportedInterfaceOrientations:
                {
                    return (object)(UIInterfaceOrientationMask.LandscapeLeft | UIInterfaceOrientationMask.LandscapeRight);
                }

                case ImageCropViewController.Request.ShouldAutorotate:
                {
                    return (object)false;
                }

                case ImageCropViewController.Request.PreferStatusBarHidden:
                {
                    return (object)PrefersStatusBarHidden( );
                }

                case ImageCropViewController.Request.IsDeviceLandscape:
                {
                    return (object)true;
                }

                case ImageCropViewController.Request.CropperDone:
                {
                    // if croppedImage is null, they simply cancelled
                    UIImage croppedImage = (UIImage)context;

                    if ( croppedImage != null )
                    {
                        NSData croppedImageData = croppedImage.AsJPEG( );

                        // notify the initial caller we're done.
                        OnCaptureCompleteDelegate( croppedImageData );
                        OnCaptureCompleteDelegate = null;
                    }

                    ImageCropViewController.DismissModalViewController( true );
                    break;
                }
            }

            return null;
        }

        void RestoreFromBackground( )
        {
            // check to see if we should re-run the Start() process
            TimeSpan delta = DateTime.Now - LockTimer;
            if ( delta.TotalMinutes > Settings.General_AutoLockTime )
            {
                // if they're not on the firstRun controller, we need to first verify Rock
                // is still where it should be, and second, force them to log back in.
                if ( FirstRunViewController.Visible == false )
                {
                    // see if Rock is present
                    ApplicationApi.IsRockAtURL( Config.Instance.RockURL, 
                        delegate(System.Net.HttpStatusCode statusCode, string statusDescription )
                        {
                            // if it IS, show the login controller (if we're not already)
                            if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                            {
                                DisplayLoginScreen( false );
                            }
                            // Rock wasn't found, so display the first run VC again
                            else
                            {
                                DisplayFirstRun( );
                            }
                        } );
                }
            }
        }

        public void DisplayFirstRun( )
        {
            ContainerViewController.RemoveFromParentViewController( );
            LoginViewController.RemoveFromParentViewController( );

            // display an error and return them to the first run screen.
            Rock.Mobile.Util.Debug.DisplayError( Strings.General_StartUp_Error_Header, Strings.General_StartUp_Error_Message );

            PresentViewController( FirstRunViewController, false, null );
        }

        public void DisplayLoginScreen( bool animated )
        {
            if ( LoginViewController.Visible == false )
            {
                PushViewController( LoginViewController, animated );
            }
        }

        public void DidEnterBackground( )
        {
            FileCache.Instance.SaveCacheMap( );
            Config.Instance.SaveToDevice( );

            LockTimer = DateTime.Now;
        }

        public void OnResignActivation( )
        {
            FileCache.Instance.SaveCacheMap( );
            Config.Instance.SaveToDevice( );

            LockTimer = DateTime.Now;
        }

        public void OnActivated( )
        {
            RestoreFromBackground( );
        }
    }
}
