using System;
using Foundation;
using UIKit;
using System.CodeDom.Compiler;
using Rock.Mobile.Network;
using System.IO;
using Rock.Mobile.UI;
using Rock.Mobile.Threading;
using Rock.Mobile.PlatformSpecific.iOS.UI;
using System.Collections.Generic;
using Rock.Mobile.Animation;
using CoreGraphics;
using Rock.Mobile.PlatformSpecific.Util;
using FamilyManager.UI;
using Customization;
using Rock.Mobile.IO;
using Rock.Mobile.PlatformSpecific.iOS.Graphics;
using System.Drawing;

namespace FamilyManager
{
	partial class LoginViewController : UIViewController
	{
        /// <summary>
        /// Reference to the parent springboard for returning apon completion
        /// </summary>
        /// <value>The springboard.</value>
        public CoreViewController Parent { get; set; }

        /// <summary>
        /// Timer to allow a small delay before returning to the springboard after a successful login.
        /// </summary>
        /// <value>The login successful timer.</value>
        System.Timers.Timer LoginSuccessfulTimer { get; set; }

        PlatformBusyIndicator BusyIndicator { get; set; }

        public bool Visible { get; protected set; }

        public LoginViewController (CoreViewController parent) : base ()
		{
            // setup our timer
            LoginSuccessfulTimer = new System.Timers.Timer();
            LoginSuccessfulTimer.AutoReset = false;
            LoginSuccessfulTimer.Interval = 1;

            Parent = parent;
		}

        protected enum LoginState
        {
            Out,
            Trying
        };
        LoginState State { get; set; }

        UILabel HeaderLabel { get; set; }

        UIInsetTextField UserNameField { get; set; }

        UIInsetTextField PasswordField { get; set; }

        UIButton LoginButton { get; set; }

        UILabel LoginResult { get; set; }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // set our theme
            CoreViewController.ApplyTheme( this );

            // and the busy indicator
            BusyIndicator = PlatformBusyIndicator.Create( );
            BusyIndicator.Color = 0x999999FF;
            BusyIndicator.BackgroundColor = 0;
            BusyIndicator.Opacity = 0;
            BusyIndicator.AddAsSubview( View );

            // setup the header label
            HeaderLabel = new UILabel( );
            View.AddSubview( HeaderLabel );
            Theme.StyleLabel( HeaderLabel, Config.Instance.VisualSettings.LabelStyle );
            HeaderLabel.Font = FontManager.GetFont( Settings.General_RegularFont, 32 );
            HeaderLabel.Text = "Family Manager";
            HeaderLabel.SizeToFit( );
            HeaderLabel.TextAlignment = UITextAlignment.Center;


            UserNameField = new UIInsetTextField();
            View.AddSubview( UserNameField );

            Theme.StyleTextField( UserNameField, Config.Instance.VisualSettings.TextFieldStyle );
            UserNameField.InputAssistantItem.LeadingBarButtonGroups = null;
            UserNameField.InputAssistantItem.TrailingBarButtonGroups = null;
            UserNameField.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
            UserNameField.Layer.AnchorPoint = CGPoint.Empty;
            UserNameField.AutocorrectionType = UITextAutocorrectionType.No;
            UserNameField.AutocapitalizationType = UITextAutocapitalizationType.None;
            UserNameField.Placeholder = "Username";
            UserNameField.ShouldReturn += (textField) => 
                {
                    textField.ResignFirstResponder();

                    TryRockBind();
                    return true;
                };

            PasswordField = new UIInsetTextField();
            View.AddSubview( PasswordField );

            Theme.StyleTextField( PasswordField, Config.Instance.VisualSettings.TextFieldStyle );
            PasswordField.InputAssistantItem.LeadingBarButtonGroups = null;
            PasswordField.InputAssistantItem.TrailingBarButtonGroups = null;
            PasswordField.Layer.AnchorPoint = CGPoint.Empty;
            PasswordField.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
            PasswordField.AutocorrectionType = UITextAutocorrectionType.No;
            PasswordField.AutocapitalizationType = UITextAutocapitalizationType.None;
            PasswordField.Placeholder = "Password";
            PasswordField.SecureTextEntry = true;

            PasswordField.ShouldReturn += (textField) => 
                {
                    textField.ResignFirstResponder();

                    TryRockBind();
                    return true;
                };

            // obviously attempt a login if login is pressed
            LoginButton = UIButton.FromType( UIButtonType.System );
            View.AddSubview( LoginButton );

            Theme.StyleButton( LoginButton, Config.Instance.VisualSettings.PrimaryButtonStyle );
            LoginButton.SetTitle( "Login", UIControlState.Normal );
            LoginButton.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
            LoginButton.SizeToFit( );
            LoginButton.Layer.AnchorPoint = CGPoint.Empty;
            LoginButton.TouchUpInside += (object sender, EventArgs e) => 
                {
                    TryRockBind();
                };
            
            // setup the result
            LoginResult = new UILabel( );
            View.AddSubview( LoginResult );
            Theme.StyleLabel( LoginResult, Config.Instance.VisualSettings.LabelStyle );
            LoginResult.TextAlignment = UITextAlignment.Center;
        }

        public override void ViewDidLayoutSubviews( )
        {
            base.ViewDidLayoutSubviews( );

            HeaderLabel.Frame = new CGRect( 0, View.Frame.Height * .05f, View.Frame.Width, HeaderLabel.Bounds.Height );

            // measure and size the username field
            UserNameField.Bounds = new CGRect( 0, 0, View.Bounds.Width * .40f, 0 );
            CGSize size = UserNameField.SizeThatFits( UserNameField.Bounds.Size );
            UserNameField.Bounds = new CGRect( UserNameField.Bounds.X, UserNameField.Bounds.Y, UserNameField.Bounds.Width, (float) System.Math.Ceiling( size.Height * 1.25f) );

            UserNameField.Layer.Position = new CGPoint( ( View.Bounds.Width - UserNameField.Bounds.Width ) / 2, HeaderLabel.Frame.Bottom + 50 );


            PasswordField.Bounds = new CGRect( 0, 0, View.Bounds.Width * .40f, 0 );
            size = PasswordField.SizeThatFits( PasswordField.Bounds.Size );
            PasswordField.Bounds = new CGRect( PasswordField.Bounds.X, PasswordField.Bounds.Y, PasswordField.Bounds.Width, (float) System.Math.Ceiling( size.Height * 1.25f ) );

            PasswordField.Layer.Position = new CGPoint( ( View.Bounds.Width - PasswordField.Bounds.Width ) / 2, UserNameField.Frame.Bottom + 10 );


            LoginButton.Bounds = new CGRect( 0, 0, 100, LoginButton.Bounds.Height );
            LoginButton.Layer.Position = new CGPoint( ( View.Bounds.Width - LoginButton.Bounds.Width ) / 2, PasswordField.Frame.Bottom + 30 );

            LoginResult.Frame = new CGRect( 0, LoginButton.Frame.Bottom + 20, View.Frame.Width, LoginResult.Bounds.Height );

            float width = 100;
            float height = 100;
            BusyIndicator.Frame = new RectangleF( ((float)View.Bounds.Width - width) / 2, (float)LoginResult.Frame.Bottom + 20, width, height );
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            LoginResult.Layer.Opacity = 0.00f;

            // clear these only on the appearance of the view. (As opposed to also 
            // when the state becomes LogOut.) This way, if they do something like mess
            // up their password, it won't force them to retype it all in.
            Config.Instance.CurrentPersonAliasId = 0;
            UserNameField.Text = string.Empty;
            PasswordField.Text = string.Empty;

            Visible = true;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            // restore the buttons
            LoginButton.Hidden = false;

            SetUIState( LoginState.Out );
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);

            // if they tap somewhere outside of the text fields, 
            // hide the keyboard
            UserNameField.ResignFirstResponder( );
            PasswordField.ResignFirstResponder( );
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            Visible = false;
        }

        public override bool ShouldAutorotate()
        {
            return true;
        }

        public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
        {
            return UIInterfaceOrientationMask.Landscape;
        }

        public override bool PrefersStatusBarHidden()
        {
            return true;
        }

        public void TryRockBind()
        {
            if( ValidateInput( ) )
            {
                SetUIState( LoginState.Trying );

                RockApi.Post_Auth_Login( UserNameField.Text, PasswordField.Text, LoginComplete );
            }
        }

        bool ValidateInput( )
        {
            bool inputValid = true;

            if ( string.IsNullOrEmpty( UserNameField.Text ) == true )
            {
                inputValid = false;
            }

            if ( string.IsNullOrEmpty( PasswordField.Text ) == true )
            {
                inputValid = false;
            }

            return inputValid;
        }

        protected void SetUIState( LoginState state )
        {
            // reset the result label
            LoginResult.Text = "";

            switch( state )
            {
                case LoginState.Out:
                {
                    UserNameField.Enabled = true;
                    PasswordField.Enabled = true;
                    LoginButton.Enabled = true;

                    BusyIndicator.Opacity = 0;

                    break;
                }

                case LoginState.Trying:
                {
                    FadeLoginResult( false );

                    UserNameField.Enabled = false;
                    PasswordField.Enabled = false;
                    LoginButton.Enabled = false;

                    BusyIndicator.Opacity = 1;

                    break;
                }
            }

            State = state;
        }

        public void LoginComplete( System.Net.HttpStatusCode statusCode, string statusDescription )
        {
            switch ( statusCode )
            {
                // if we received No Content, we're logged in
                case System.Net.HttpStatusCode.NoContent:
                {
                    // validate that they're ok to use this app.
                    RockApi.Get_People_ByUserName( UserNameField.Text, ProfileComplete );
                    break;
                }

                case System.Net.HttpStatusCode.Unauthorized:
                {
                    // allow them to attempt logging in again
                    SetUIState( LoginState.Out );

                    // wrong user name / password
                    FadeLoginResult( true );
                    LoginResult.Text = Strings.General_Login_Invalid;
                    LoginResult.SizeToFit( );
                    break;
                }

                case System.Net.HttpStatusCode.ResetContent:
                {
                    // allow them to attempt logging in again
                    SetUIState( LoginState.Out );

                    LoginResult.Text = "";

                    break;
                }

                default:
                {                        
                    // allow them to attempt logging in again
                    SetUIState( LoginState.Out );

                    // failed to login for some reason
                    FadeLoginResult( true );
                    LoginResult.Text = Strings.General_Error_Message;
                    LoginResult.SizeToFit( );
                    break;
                }
            }
        }

        public void ProfileComplete(System.Net.HttpStatusCode code, string desc, Rock.Client.Person model) 
        {
            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate
                {
                    UIThread_ProfileComplete( code, desc, model );
                } );
        }

        void UIThread_ProfileComplete( System.Net.HttpStatusCode code, string desc, Rock.Client.Person model ) 
        {
            switch ( code )
            {
                case System.Net.HttpStatusCode.OK:
                {
                    // make sure they have an alias ID. if they don't, it's a huge bug, and we can't
                    // let them proceed.
                    if ( model.PrimaryAliasId.HasValue )
                    {
                        Config.Instance.CurrentPersonAliasId = model.PrimaryAliasId.Value;
                        
                        // now ensure they are authorized
                        FamilyManagerApi.GetAppAuthorization( model.Id, AuthorizationComplete );
                    }
                    else
                    {
                        // if we couldn't get their profile, that should still count as a failed login.
                        SetUIState( LoginState.Out );

                        // failed to login for some reason
                        FadeLoginResult( true );
                        LoginResult.Text = Strings.General_Error_Message;
                        LoginResult.SizeToFit( );
                    }

                    break;
                }

                default:
                {
                    // if we couldn't get their profile, that should still count as a failed login.
                    SetUIState( LoginState.Out );

                    // failed to login for some reason
                    FadeLoginResult( true );
                    LoginResult.Text = Strings.General_Error_Message;
                    LoginResult.SizeToFit( );

                    break;
                }
            }
        }

        public void AuthorizationComplete( System.Net.HttpStatusCode code, string desc )
        {
            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate
                {
                    UIThread_AuthorizationComplete( code, desc );
                } );
        }

        void UIThread_AuthorizationComplete( System.Net.HttpStatusCode code, string desc )
        {
            switch ( code )
            {
                case System.Net.HttpStatusCode.OK:
                {
                    // update the UI
                    FadeLoginResult( true );
                    LoginResult.Text = Strings.General_Welcome;
                    LoginResult.SizeToFit( );

                    // start the timer, which will notify the springboard we're logged in when it ticks.
                    LoginSuccessfulTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e ) =>
                        {
                            // when the timer fires, notify the springboard we're done.
                            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate
                                {
                                    Parent.LoginComplete( );
                                } );
                        };

                    LoginSuccessfulTimer.Start( );

                    break;
                }

                default:
                {
                    // if we couldn't get their profile, that should still count as a failed login.
                    SetUIState( LoginState.Out );

                    // failed to login for some reason
                    FadeLoginResult( true );
                    LoginResult.Text = Strings.General_Login_NotAuthorized;
                    LoginResult.SizeToFit( );

                    break;
                }
            }
        }

        void FadeLoginResult( bool fadeIn )
        {
            UIView.Animate( .33f, 0, UIViewAnimationOptions.CurveEaseInOut, 
                new Action( 
                    delegate 
                    { 
                        LoginResult.Layer.Opacity = fadeIn == true ? 1.00f : 0.00f;
                    })

                , new Action(
                    delegate
                    {
                    })
            );
        }
	}
}
