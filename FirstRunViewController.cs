using System;
using UIKit;
using Rock.Mobile.IO;
using System.IO;
using Foundation;
using CoreGraphics;
using iOS;
using FamilyManager.UI;
using Rock.Mobile.PlatformSpecific.Util;
using System.Collections.Generic;
using Customization;
using Rock.Mobile.PlatformSpecific.iOS.UI;
using Rock.Mobile.Util.Strings;
using Rock.Mobile.Network;
using System.Net;

namespace FamilyManager
{
    public class FirstRunViewController : UIViewController
    {
        CoreViewController Parent { get; set; }

        UILabel RockUrlTitle { get; set; }
        UIInsetTextField RockUrlField { get; set; }

        UILabel RockAuthKeyTitle { get; set; }
        UIInsetTextField RockAuthKeyField { get; set; }

        UILabel RockUrlDesc { get; set; }

        UIButton Submit { get; set; }
        UILabel ResultLabel { get; set; }
        UIBlockerView BlockerView { get; set; }
        UIImageView Logo { get; set; }

        public bool Visible { get; protected set; }

        public override bool PrefersStatusBarHidden()
        {
            return true;
        }

        public FirstRunViewController( CoreViewController parent ) : base( )
        {
            Parent = parent;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = Theme.GetColor( "#ee7624ff" );


            Logo = new UIImageView();
            Logo.Layer.AnchorPoint = CGPoint.Empty;
            Logo.Image = UIImage.FromBundle( "rock-logo.png" );
            Logo.SizeToFit( );
            View.AddSubview( Logo );

            RockUrlTitle = new UILabel();
            RockUrlTitle.Layer.AnchorPoint = CGPoint.Empty;
            RockUrlTitle.TextColor = UIColor.White;
            RockUrlTitle.Text = "Rock Server Address";
            RockUrlTitle.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "OpenSans-Light", 20 );
            RockUrlTitle.SizeToFit( );
            View.AddSubview( RockUrlTitle );

            RockUrlField = new UIInsetTextField( );
            RockUrlField.InputAssistantItem.LeadingBarButtonGroups = null;
            RockUrlField.InputAssistantItem.TrailingBarButtonGroups = null;
            RockUrlField.Layer.AnchorPoint = CGPoint.Empty;
            RockUrlField.TextColor = UIColor.White;
            RockUrlField.Text = string.IsNullOrEmpty( Config.Instance.RockURL ) == true ? "http://" : Config.Instance.RockURL;
            RockUrlField.Layer.BorderColor = UIColor.White.CGColor;
            RockUrlField.Layer.BorderWidth = 1;
            RockUrlField.Layer.CornerRadius = 4;
            RockUrlField.AutocapitalizationType = UITextAutocapitalizationType.None;
            RockUrlField.AutocorrectionType = UITextAutocorrectionType.No;
            RockUrlField.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "OpenSans-Regular", 32 );
            View.AddSubview( RockUrlField );


            RockAuthKeyTitle = new UILabel();
            RockAuthKeyTitle.Layer.AnchorPoint = CGPoint.Empty;
            RockAuthKeyTitle.TextColor = UIColor.White;
            RockAuthKeyTitle.Text = "Rock Authorization Key";
            RockAuthKeyTitle.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "OpenSans-Light", 20 );
            RockAuthKeyTitle.SizeToFit( );
            View.AddSubview( RockAuthKeyTitle );

            RockAuthKeyField = new UIInsetTextField( );
            RockAuthKeyField.InputAssistantItem.LeadingBarButtonGroups = null;
            RockAuthKeyField.InputAssistantItem.TrailingBarButtonGroups = null;
            RockAuthKeyField.Layer.AnchorPoint = CGPoint.Empty;
            RockAuthKeyField.TextColor = UIColor.White;
            RockAuthKeyField.Text = Config.Instance.RockAuthorizationKey;
            RockAuthKeyField.AutocapitalizationType = UITextAutocapitalizationType.None;
            RockAuthKeyField.AutocorrectionType = UITextAutocorrectionType.No;
            RockAuthKeyField.Layer.BorderColor = UIColor.White.CGColor;
            RockAuthKeyField.Layer.BorderWidth = 1;
            RockAuthKeyField.Layer.CornerRadius = 4;
            RockAuthKeyField.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "OpenSans-Regular", 32 );
            View.AddSubview( RockAuthKeyField );


            RockUrlDesc = new UILabel();
            RockUrlDesc.Layer.AnchorPoint = CGPoint.Empty;
            RockUrlDesc.TextColor = UIColor.White;
            RockUrlDesc.Text = "You can update these values at any time from the settings panel.";
            RockUrlDesc.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "OpenSans-Light", 18 );
            RockUrlDesc.SizeToFit( );
            View.AddSubview( RockUrlDesc );



            Submit = UIButton.FromType( UIButtonType.System );
            Submit.Layer.AnchorPoint = CGPoint.Empty;
            Submit.SetTitle( "Submit", UIControlState.Normal );
            Submit.SetTitleColor( Theme.GetColor( "#ee7624ff" ), UIControlState.Normal );
            Submit.SizeToFit( );
            Submit.Layer.CornerRadius = 4;
            Submit.BackgroundColor = UIColor.White;
            Submit.Font = Submit.Font.WithSize( 24 );
            View.AddSubview( Submit );

            ResultLabel = new UILabel();
            ResultLabel.Layer.AnchorPoint = CGPoint.Empty;
            ResultLabel.TextColor = UIColor.White;
            ResultLabel.TextAlignment = UITextAlignment.Center;
            ResultLabel.Font = ResultLabel.Font.WithSize( 24 );
            View.AddSubview( ResultLabel );

            BlockerView = new UIBlockerView( View, View.Bounds.ToRectF( ) );


            // setup the Submit action
            Submit.TouchUpInside += (object sender, EventArgs e ) =>
            {
                PerformSearch( );
            };

            RockUrlField.ShouldReturn += ( UITextField textField) =>
                {
                    PerformSearch( );
                    return true;
                };
            //
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            Visible = true;
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            Visible = false;
        }

        void PerformSearch( )
        {
            // only let them submit if they have something beyond "http://" (or some other short crap)
            if ( RockUrlField.Text.IsValidURL( ) )
            {
                // hide the keyboard
                RockUrlField.ResignFirstResponder( );
                
                // disable until we're done
                Submit.Enabled = false;

                BlockerView.BringToFront( );
                BlockerView.Show( delegate
                    {
                        // see if Rock exists at this endpoint
                        ApplicationApi.IsRockAtURL( RockUrlField.Text, delegate(HttpStatusCode statusCode, string statusDesc )
                            {
                                if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                                {
                                    // attempt to contact Rock and get all the required information.
                                    Config.Instance.TryBindToRockServer( RockUrlField.Text, RockAuthKeyField.Text,
                                        delegate( bool result )
                                        {
                                            // it worked, so Rock is valid.
                                            if ( result == true )
                                            {
                                                // take these as our values.
                                                Config.Instance.CommitRockSync( );

                                                Config.Instance.SetConfigurationDefinedValue( Config.Instance.ConfigurationTemplates[ 0 ], 
                                                    delegate( bool configResult )
                                                    {
                                                        if ( configResult == true )
                                                        {
                                                            HandlePerformSearchResult( true, Strings.General_RockBindSuccess );
                                                        }
                                                        else
                                                        {
                                                            // ROCK DATA ERROR
                                                            HandlePerformSearchResult( false, Strings.General_RockBindError_Data );
                                                        }
                                                    } );
                                            }
                                            else
                                            {
                                                // ROCK DATA ERROR
                                                HandlePerformSearchResult( false, Strings.General_RockBindError_Data );
                                            }
                                        } );     
                                }
                                else
                                {
                                    // Rock NOT FOUND
                                    HandlePerformSearchResult( false, Strings.General_RockBindError_NotFound );
                                }
                            });
                    } );
            }
        }

        void HandlePerformSearchResult( bool result, string description )
        {
            // unhide the blocker
            BlockerView.Hide( 
                delegate
                {
                    Submit.Enabled = true;
                    DisplayResult( result, description );
                });
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            Logo.Layer.Position = new CGPoint( ( View.Bounds.Width - Logo.Bounds.Width ) / 2, 25 );

            // Title
            RockUrlTitle.Layer.Position = new CGPoint( ( View.Bounds.Width - RockUrlField.Bounds.Width ) / 2, View.Bounds.Height * .25f );

            // because there's no placeholder, we can't SizeToFit the URL Field, so we'll measure the font and then size it.
            RockUrlField.Bounds = new CGRect( 0, 0, View.Bounds.Width * .75f, 0 );
            CGSize size = RockUrlField.SizeThatFits( new CGSize( RockUrlField.Bounds.Width, RockUrlField.Bounds.Height ) );
            RockUrlField.Bounds = new CGRect( RockUrlField.Bounds.X, RockUrlField.Bounds.Y, RockUrlField.Bounds.Width, (float) System.Math.Ceiling( size.Height ) );

            // center the rock URL field and title
            RockUrlField.Layer.Position = new CGPoint( ( View.Bounds.Width - RockUrlField.Bounds.Width ) / 2,
                                                        RockUrlTitle.Frame.Bottom + 5 );


            // Title
            RockAuthKeyTitle.Layer.Position = new CGPoint( ( View.Bounds.Width - RockUrlField.Bounds.Width ) / 2, RockUrlField.Frame.Bottom + 20 );

            // because there's no placeholder, we can't SizeToFit the URL Field, so we'll measure the font and then size it.
            RockAuthKeyField.Bounds = new CGRect( 0, 0, View.Bounds.Width * .75f, 0 );
            size = RockAuthKeyField.SizeThatFits( new CGSize( RockAuthKeyField.Bounds.Width, RockAuthKeyField.Bounds.Height ) );
            RockAuthKeyField.Bounds = new CGRect( RockAuthKeyField.Bounds.X, RockAuthKeyField.Bounds.Y, RockAuthKeyField.Bounds.Width, (float) System.Math.Ceiling( size.Height ) );

            RockAuthKeyField.Layer.Position = new CGPoint( ( View.Bounds.Width - RockAuthKeyField.Bounds.Width ) / 2,
                RockAuthKeyTitle.Frame.Bottom + 5 );


            // Description
            RockUrlDesc.Layer.Position = new CGPoint( RockUrlField.Frame.Left, RockAuthKeyField.Frame.Bottom + 5 );


            // set the blocker
            BlockerView.SetBounds( View.Bounds.ToRectF( ) );

            // set the submit bounds and position
            Submit.SizeToFit( );
            Submit.Bounds = new CGRect( 0, 0, RockUrlField.Bounds.Width * .35f, Submit.Bounds.Height * 1.25f );
            Submit.Layer.Position = new CGPoint( RockUrlField.Frame.Left + ( ( RockUrlField.Bounds.Width - Submit.Bounds.Width ) / 2 ),
                                                 RockUrlDesc.Frame.Bottom + 60 );

            // let the label stretch the entire width
            ResultLabel.Bounds = new CGRect( 0, 0, View.Bounds.Width, 0 );
            ResultLabel.SizeToFit( );
            ResultLabel.Frame = new CGRect( 0, Submit.Frame.Bottom + 30, View.Bounds.Width, ResultLabel.Bounds.Height );
        }

        void DisplayResult( bool result, string description )
        {
            if ( result == true )
            {
                //ResultLabel.Text = "Got it, Thanks!";
                ResultLabel.Text = description;

                // kick off a timer to allow the user to see the result
                System.Timers.Timer timer = new System.Timers.Timer();
                timer.AutoReset = false;
                timer.Interval = 500;
                timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e ) =>
                {
                        Rock.Mobile.Threading.Util.PerformOnUIThread( delegate
                            {
                                Parent.SetupComplete( );
                            });
                };

                timer.Start( );
            }
            else
            {
                ResultLabel.Text = description;
            }
            ResultLabel.SizeToFit( );

            View.SetNeedsLayout( );
        }
    }
}
