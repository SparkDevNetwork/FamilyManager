using System;
using UIKit;
using Rock.Mobile.IO;
using System.IO;
using Foundation;
using CoreGraphics;
using iOS;
using FamilyManager.UI;
using Customization;

namespace FamilyManager
{
    public class ContainerViewController : UIViewController
    {
        CoreViewController Parent { get; set; }
        UINavigationController SubNavigationController { get; set; }
        HistoryBar HistoryBar { get; set; }
        UIButton HomeButton { get; set; }
        UIButton AddFamilyButton { get; set; }
        SettingsViewController SettingsViewController { get; set; }
        FamilyInfoViewController CurrentFamilyInfoViewController { get; set; }
        SearchFamiliesViewController SearchFamiliesViewController { get; set; }
        bool FirstPromptVisitDone { get; set; }

        public override bool PrefersStatusBarHidden()
        {
            return true;
        }

        public ContainerViewController( CoreViewController parent ) : base( )
        {
            Parent = parent;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // First setup the SpringboardReveal button, which rests in the upper left
            // of the MainNavigationUI. (We must do it here because the ContainerViewController's
            // NavBar is the active one.)
            NSString buttonLabel = new NSString( "" );

            HomeButton = new UIButton(UIButtonType.System);
            HomeButton.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "Bh", 36 );
            HomeButton.SetTitle( buttonLabel.ToString( ), UIControlState.Normal );
            HomeButton.SetTitleColor( Theme.GetColor( Config.Instance.VisualSettings.TopHeaderTextColor ), UIControlState.Normal );
            HomeButton.SetTitleColor( UIColor.DarkGray, UIControlState.Disabled );

            // determine its dimensions
            CGSize buttonSize = buttonLabel.StringSize( HomeButton.Font );
            HomeButton.Bounds = new CGRect( 0, 0, buttonSize.Width, buttonSize.Height );

            // set its callback
            HomeButton.TouchUpInside += (object sender, EventArgs e) => 
                {
                    // clear any current family VC
                    CurrentFamilyInfoViewController = null;

                    // reset our stack
                    SubNavigationController.PopToRootViewController( true );
                    SubNavigationController.View.SetNeedsLayout( );

                    // turn off the home button
                    HomeButton.Enabled = false;

                    // and enable the add family button
                    AddFamilyButton.Enabled = true;
                };


            // set the "Add Family" button
            buttonLabel = new NSString( "" );

            AddFamilyButton = new UIButton(UIButtonType.System);
            AddFamilyButton.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "Bh", 36 );
            AddFamilyButton.SetTitle( buttonLabel.ToString( ), UIControlState.Normal );
            AddFamilyButton.SetTitleColor( Theme.GetColor( Config.Instance.VisualSettings.TopHeaderTextColor ), UIControlState.Normal );
            AddFamilyButton.SetTitleColor( UIColor.DarkGray, UIControlState.Disabled );

            // determine its dimensions
            buttonSize = buttonLabel.StringSize( HomeButton.Font );
            AddFamilyButton.Bounds = new CGRect( 0, 0, buttonSize.Width, buttonSize.Height );

            // set its callback
            AddFamilyButton.TouchUpInside += (object sender, EventArgs e) => 
            {
                // clear any current family VC
                CurrentFamilyInfoViewController = null;

                // and present a new family page
                PresentNewFamilyPage( );
            };

            UIBarButtonItem[] leftItems = new UIBarButtonItem[]
            {
                new UIBarButtonItem( HomeButton ),
                new UIBarButtonItem( AddFamilyButton )
            };
            this.NavigationItem.SetLeftBarButtonItems( leftItems, false );


            // First setup the campus button, which rests in the upper right
            // of the MainNavigationUI. (We must do it here because the ContainerViewController's
            // NavBar is the active one.)
            buttonLabel = new NSString( Config.Instance.Campuses[ Config.Instance.SelectedCampusIndex ].Name );

            UIButton campusButton = new UIButton(UIButtonType.System);
            campusButton.SetTitle( buttonLabel.ToString( ), UIControlState.Normal );
            campusButton.SetTitleColor( Theme.GetColor( Config.Instance.VisualSettings.TopHeaderTextColor ), UIControlState.Normal );

            // determine its dimensions
            buttonSize = buttonLabel.StringSize( campusButton.Font );
            campusButton.Bounds = new CGRect( 0, 0, buttonSize.Width, buttonSize.Height );

            // set its callback
            campusButton.TouchUpInside += (object sender, EventArgs e) => 
                {
                    //Console.WriteLine( "Tapped!" );
                };

            // add a Logout button as well
            buttonLabel = new NSString( "" );

            UIButton logoutButton = new UIButton(UIButtonType.System);
            logoutButton.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "Bh", 36 );
            logoutButton.SetTitle( buttonLabel.ToString( ), UIControlState.Normal );
            logoutButton.SetTitleColor( Theme.GetColor( Config.Instance.VisualSettings.TopHeaderTextColor ), UIControlState.Normal );

            // determine its dimensions
            buttonSize = buttonLabel.StringSize( HomeButton.Font );
            logoutButton.Bounds = new CGRect( 0, 0, buttonSize.Width, buttonSize.Height );

            // set its callback
            logoutButton.TouchUpInside += (object sender, EventArgs e) => 
                {
                    UIAlertController actionSheet = UIAlertController.Create( Strings.General_Logout, 
                        Strings.General_ConfirmLogout, 
                        UIAlertControllerStyle.Alert );


                    UIAlertAction yesAction = UIAlertAction.Create( Strings.General_Yes, UIAlertActionStyle.Default, delegate(UIAlertAction obj) 
                        {
                            Parent.DisplayLoginScreen( true );   
                        } );

                    //setup cancel
                    UIAlertAction cancelAction = UIAlertAction.Create( Strings.General_No, UIAlertActionStyle.Default, delegate
                        { 
                            
                        } );

                    actionSheet.AddAction( yesAction );
                    actionSheet.AddAction( cancelAction );

                    PresentViewController( actionSheet, true, null );
                };


            this.NavigationItem.SetRightBarButtonItems( new UIBarButtonItem[] { new UIBarButtonItem( logoutButton ), new UIBarButtonItem( campusButton ) }, false );
            //

            CreateSubNavigationController( );

            SearchFamiliesViewController = new SearchFamiliesViewController( this );
            SubNavigationController.PushViewController( SearchFamiliesViewController, true );

            HomeButton.Enabled = false;

            SettingsViewController = new SettingsViewController( this );

            // set our theme
            CoreViewController.ApplyTheme( this );
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            // handle asking the user what to do about newly created people
            if ( FirstPromptVisitDone == false && Config.Instance.FirstVisitPrompt == true )
            {
                FirstPromptVisitDone = true;

                UIAlertController actionSheet = UIAlertController.Create( Strings.General_Welcome, 
                    Strings.General_FirstTimeVisitors, 
                    UIAlertControllerStyle.Alert );


                UIAlertAction yesAction = UIAlertAction.Create( Strings.General_Yes, UIAlertActionStyle.Default, delegate(UIAlertAction obj) 
                    {
                        Config.Instance.RecordFirstVisit = true;   
                    } );

                //setup cancel
                UIAlertAction cancelAction = UIAlertAction.Create( Strings.General_No, UIAlertActionStyle.Default, delegate
                    { 
                        Config.Instance.RecordFirstVisit = false;
                    } );

                actionSheet.AddAction( yesAction );
                actionSheet.AddAction( cancelAction );

                PresentViewController( actionSheet, true, null );
            }
        }

        protected void CreateSubNavigationController( )
        {
            // Create the sub navigation controller
            SubNavigationController = new UINavigationController();
            SubNavigationController.NavigationBarHidden = true;
            SubNavigationController.Delegate = new NavDelegate( );

            // add this navigation controller (and its toolbar) as a child
            // of this ContainerViewController, which will effectively make it a child
            // of the primary navigation controller.
            AddChildViewController( SubNavigationController );
            View.AddSubview( SubNavigationController.View );

            // setup the history bar that tracks families previously viewed.
            // we add it LAST so it has the highest Z order.
            HistoryBar = new HistoryBar( View.Frame, delegate
                {
                    PresentViewController( SettingsViewController, true, null );
                } );
            View.AddSubview( HistoryBar );
        }

        // make sure the controller about to show gets ViewWillAppear.
        class NavDelegate : UINavigationControllerDelegate
        {
            public override void WillShowViewController(UINavigationController navigationController, UIViewController viewController, bool animated)
            {
                viewController.ViewWillAppear( animated );
            }
        }

        public void PresentNewFamilyPage( )
        {
            // call the presentation function
            PresentFamilyPage_Internal( null );

            // and disable the add family button, since we KNOW we're creating a new family.
            AddFamilyButton.Enabled = false;
        }

        public void PresentFamilyPage( Rock.Client.Family family )
        {
            // add this family to the history bar, and setup a delegate that will call the internal presentation function
            HistoryBar.TryPushHistoryItem( family, PresentFamilyPage_Internal );

            // call the presentation function
            PresentFamilyPage_Internal( family );
        }

        void PresentFamilyPage_Internal( Rock.Client.Family family )
        {
            // display the view controller. Ignore requests to re-view the same family (which we key off the ID to know)
            if ( CurrentFamilyInfoViewController == null || CurrentFamilyInfoViewController.Family.Id != family.Id )
            {
                // first, clear out whatever our current is, because that's changing
                CurrentFamilyInfoViewController = null;

                // now, see if this family is already in a controller within the stack
                foreach ( UIViewController controller in SubNavigationController.ChildViewControllers )
                {
                    // is it a match?
                    FamilyInfoViewController currController = controller as FamilyInfoViewController;
                    if ( currController != null && currController.Family == family )
                    {
                        // then pop to it, and take it as our current reference
                        SubNavigationController.PopToViewController( currController, true );
                        CurrentFamilyInfoViewController = currController;
                        break;
                    }
                }

                // if this is still null, it isn't in our stack, so we can go ahead and push a new one.
                if ( CurrentFamilyInfoViewController == null )
                {
                    CurrentFamilyInfoViewController = new FamilyInfoViewController( this, family );
                    SubNavigationController.PushViewController( CurrentFamilyInfoViewController, true );
                }

                HomeButton.Enabled = true;
                AddFamilyButton.Enabled = true;
            }
        }

        public void FamilyUpdated( Rock.Client.Family family )
        {
            // this is called when a page (likely the current family page) updates a family.
            // This lets us notify SearchFamilies and the HistoryBar.
            if ( HistoryBar.TryUpdateHistoryItem( family ) == false )
            {
                // it failed to update, so it's likely a new family. Add it to the history.
                HistoryBar.TryPushHistoryItem( family, PresentFamilyPage_Internal );
            }

            SearchFamiliesViewController.TryUpdateFamily( family );
        }

        public void SettingsComplete( )
        {
            SettingsViewController.DismissViewController( true, null );
        }

        public nfloat GetVisibleHeight( )
        {
            // this will return the area of the screen that's useful,
            // which is between the top nav bar and the bottom history bar.
            return View.Bounds.Height - HistoryBar.Bounds.Height - NavigationController.NavigationBar.Bounds.Height;
        }

        public void CaptureImage( FamilyManager.CoreViewController.OnCaptureComplete onCaptureComplete )
        {
            Parent.BeginPictureCapture( onCaptureComplete );
        }
    }
}
