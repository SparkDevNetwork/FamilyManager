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
using System.Linq;
using Rock.Mobile.Util.Strings;
using Rock.Mobile.Network;

namespace FamilyManager
{
    public class SettingsViewController : UIViewController
    {
        class CampusTableData : UITableViewSource
        {
            public SettingsViewController Parent { get; set; }
            
            public override nint RowsInSection (UITableView tableview, nint section)
            {
                return Parent.Campuses.Count;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                Parent.RowSelected( );

                tableView.CellAt( indexPath ).TextLabel.TextColor = UIColor.White;
            }

            public override void RowDeselected(UITableView tableView, NSIndexPath indexPath)
            {
                tableView.CellAt( indexPath ).TextLabel.TextColor = UIColor.Black;
            }

            public void SetRowColor( UITableView tableView, NSIndexPath indexPath, UIColor color )
            {
                tableView.CellAt( indexPath ).TextLabel.TextColor = color;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                UITableViewCell cell = tableView.DequeueReusableCell( "identifier" );
                if ( cell == null )
                {
                    cell = new UITableViewCell( UITableViewCellStyle.Default, "identifier" );
                    cell.SelectedBackgroundView = new UIView();
                    cell.SelectedBackgroundView.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.SelectedPersonColor );
                }

                cell.TextLabel.Text = Parent.Campuses[ indexPath.Row ].Name;
                return cell;
            }
        }

        class TemplateTableData : UITableViewSource
        {
            public SettingsViewController Parent { get; set; }

            public override nint RowsInSection (UITableView tableview, nint section)
            {
                return Parent.ConfigurationTemplates.Count;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                Parent.RowSelected( );

                tableView.CellAt( indexPath ).TextLabel.TextColor = UIColor.White;
            }

            public override void RowDeselected(UITableView tableView, NSIndexPath indexPath)
            {
                tableView.CellAt( indexPath ).TextLabel.TextColor = UIColor.Black;
            }

            public void SetRowColor( UITableView tableView, NSIndexPath indexPath, UIColor color )
            {
                tableView.CellAt( indexPath ).TextLabel.TextColor = color;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                UITableViewCell cell = tableView.DequeueReusableCell( "identifier" );
                if ( cell == null )
                {
                    cell = new UITableViewCell( UITableViewCellStyle.Default, "identifier" );
                    cell.SelectedBackgroundView = new UIView();
                    cell.SelectedBackgroundView.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.SelectedPersonColor );
                }

                cell.TextLabel.Text = Parent.ConfigurationTemplates[ indexPath.Row ].Value;
                return cell;
            }
        }

        ContainerViewController Parent { get; set; }

        UILabel RockUrlTitle { get; set; }
        UIInsetTextField RockUrlField { get; set; }

        UILabel RockAuthKeyTitle { get; set; }
        UIInsetTextField RockAuthKeyField { get; set; }

        UILabel CampusesLabel { get; set; }
        UITableView CampusTableView { get; set; }

        UILabel TemplateLabel { get; set; }
        UITableView TemplateTableView { get; set; }


        List<Rock.Client.DefinedValue> ConfigurationTemplates;
        List<Rock.Client.Campus> Campuses;

        UILabel CampusSwitchLabel { get; set; }
        UISwitch CampusSwitch { get; set; }

        UIButton Sync { get; set; }
        UILabel SyncResultLabel { get; set; }
        UIBlockerView BlockerView { get; set; }
        UIButton Cancel { get; set; }
        UIButton Save { get; set; }

        public override bool PrefersStatusBarHidden()
        {
            return true;
        }

        public SettingsViewController( ContainerViewController parent ) : base( )
        {
            Parent = parent;
            ConfigurationTemplates = new List<Rock.Client.DefinedValue>();
            Campuses = new List<Rock.Client.Campus>();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.BackgroundColor );

            // Rock URL Title
            RockUrlTitle = new UILabel();
            RockUrlTitle.Layer.AnchorPoint = CGPoint.Empty;
            RockUrlTitle.Text = "Rock Server Address";
            Theme.StyleLabel( RockUrlTitle, Config.Instance.VisualSettings.LabelStyle );
            RockUrlTitle.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_LightFont, Config.Instance.VisualSettings.MediumFontSize );
            RockUrlTitle.SizeToFit( );
            View.AddSubview( RockUrlTitle );

            // Rock URL Address
            RockUrlField = new UIInsetTextField( );
            RockUrlField.Layer.AnchorPoint = CGPoint.Empty;
            RockUrlField.Text = Config.Instance.RockURL;
            RockUrlField.AutocorrectionType = UITextAutocorrectionType.No;
            RockUrlField.AutocapitalizationType = UITextAutocapitalizationType.None;
            RockUrlField.InputAssistantItem.LeadingBarButtonGroups = null;
            RockUrlField.InputAssistantItem.TrailingBarButtonGroups = null;
            Theme.StyleTextField( RockUrlField, Config.Instance.VisualSettings.TextFieldStyle );
            RockUrlField.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
            View.AddSubview( RockUrlField );

            // Rock AuthKey Title
            RockAuthKeyTitle = new UILabel();
            RockAuthKeyTitle.Layer.AnchorPoint = CGPoint.Empty;
            RockAuthKeyTitle.Text = "Rock Authorization Key";
            Theme.StyleLabel( RockAuthKeyTitle, Config.Instance.VisualSettings.LabelStyle );
            RockAuthKeyTitle.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_LightFont, Config.Instance.VisualSettings.MediumFontSize );
            RockAuthKeyTitle.SizeToFit( );
            View.AddSubview( RockAuthKeyTitle );

            // Rock AuthKey Field
            RockAuthKeyField = new UIInsetTextField( );
            RockAuthKeyField.Layer.AnchorPoint = CGPoint.Empty;
            RockAuthKeyField.TextColor = UIColor.White;
            RockAuthKeyField.Text = Config.Instance.RockAuthorizationKey;
            RockAuthKeyField.AutocorrectionType = UITextAutocorrectionType.No;
            RockAuthKeyField.AutocapitalizationType = UITextAutocapitalizationType.None;
            RockAuthKeyField.InputAssistantItem.LeadingBarButtonGroups = null;
            RockAuthKeyField.InputAssistantItem.TrailingBarButtonGroups = null;
            Theme.StyleTextField( RockAuthKeyField, Config.Instance.VisualSettings.TextFieldStyle );
            RockAuthKeyField.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
            View.AddSubview( RockAuthKeyField );

            // setup the campus switch label
            CampusSwitchLabel = new UILabel( );
            CampusSwitchLabel.Layer.AnchorPoint = CGPoint.Empty;
            CampusSwitchLabel.Text = "Autodetect Campus";
            Theme.StyleLabel( CampusSwitchLabel, Config.Instance.VisualSettings.LabelStyle );
            CampusSwitchLabel.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_LightFont, Config.Instance.VisualSettings.MediumFontSize );
            CampusSwitchLabel.SizeToFit( );
            View.AddSubview( CampusSwitchLabel );

            // default the campus switch to whatever the autodetect preference is
            CampusSwitch = new UISwitch( );
            CampusSwitch.Layer.AnchorPoint = CGPoint.Empty;
            CampusSwitch.OnTintColor = Theme.GetColor( Config.Instance.VisualSettings.ToggleStyle.ActiveColor );
            CampusSwitch.On = Config.Instance.AutoDetectCampus;
            View.AddSubview( CampusSwitch );


            // Campus Label and Table
            CampusesLabel = new UILabel();
            CampusesLabel.Layer.AnchorPoint = CGPoint.Empty;
            CampusesLabel.Text = "Campus";
            Theme.StyleLabel( CampusesLabel, Config.Instance.VisualSettings.LabelStyle );
            CampusesLabel.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_LightFont, Config.Instance.VisualSettings.MediumFontSize );
            CampusesLabel.SizeToFit( );
            View.AddSubview( CampusesLabel );

            CampusTableView = new UITableView();
            CampusTableView.Layer.AnchorPoint = CGPoint.Empty;
            CampusTableView.Source = new CampusTableData() { Parent = this };
            CampusTableView.Layer.CornerRadius = 4;
            View.AddSubview( CampusTableView );


            // Template Label and Table
            TemplateLabel = new UILabel();
            TemplateLabel.Layer.AnchorPoint = CGPoint.Empty;
            TemplateLabel.Text = "Theme";
            Theme.StyleLabel( TemplateLabel, Config.Instance.VisualSettings.LabelStyle );
            TemplateLabel.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_LightFont, Config.Instance.VisualSettings.MediumFontSize );
            TemplateLabel.SizeToFit( );
            View.AddSubview( TemplateLabel );

            TemplateTableView = new UITableView();
            TemplateTableView.Layer.AnchorPoint = CGPoint.Empty;
            TemplateTableView.Source = new TemplateTableData() { Parent = this };
            TemplateTableView.Layer.CornerRadius = 4;
            View.AddSubview( TemplateTableView );


            Sync = UIButton.FromType( UIButtonType.System );
            Sync.Layer.AnchorPoint = CGPoint.Empty;
            Sync.SetTitle( "Sync", UIControlState.Normal );
            Sync.SetTitleColor( UIColor.White, UIControlState.Normal );
            Theme.StyleButton( Sync, Config.Instance.VisualSettings.DefaultButtonStyle );
            Sync.SizeToFit( );
            View.AddSubview( Sync );

            SyncResultLabel = new UILabel();
            SyncResultLabel.Layer.AnchorPoint = CGPoint.Empty;
            SyncResultLabel.TextColor = UIColor.White;
            SyncResultLabel.TextAlignment = UITextAlignment.Center;
            SyncResultLabel.Font = SyncResultLabel.Font.WithSize( 36 );
            Theme.StyleLabel( SyncResultLabel, Config.Instance.VisualSettings.LabelStyle );
            View.AddSubview( SyncResultLabel );

            Cancel = UIButton.FromType( UIButtonType.System );
            Cancel.Layer.AnchorPoint = CGPoint.Empty;
            Cancel.SetTitle( "Cancel", UIControlState.Normal );
            Theme.StyleButton( Cancel, Config.Instance.VisualSettings.DefaultButtonStyle );
            Cancel.SizeToFit( );
            View.AddSubview( Cancel );

            Save = UIButton.FromType( UIButtonType.System );
            Save.Layer.AnchorPoint = CGPoint.Empty;
            Save.SetTitle( "Save", UIControlState.Normal );
            Theme.StyleButton( Save, Config.Instance.VisualSettings.PrimaryButtonStyle );
            Save.SetTitleColor( UIColor.LightGray, UIControlState.Disabled );
            Save.SizeToFit( );
            View.AddSubview( Save );

            BlockerView = new UIBlockerView( View, View.Bounds.ToRectF( ) );

            // setup the Sync action
            Sync.TouchUpInside += (object sender, EventArgs e ) =>
                {
                    DownloadRockValues( );
                };

            RockUrlField.EditingDidBegin += (object sender, EventArgs e) => 
                {
                    // if they start editing the text field, immediatley disable the submit button.
                    // it will be enabled again when they tap Sync and we can verify they connected to Rock.
                    ToggleSaveButton( false );

                    // clear out lists which will effectively force the user to sync
                    ClearLists( );
                    CampusTableView.ReloadData( );
                    TemplateTableView.ReloadData( );
                };

            RockAuthKeyField.EditingDidBegin += (object sender, EventArgs e ) =>
                {
                    ToggleSaveButton( false );

                    // clear out lists which will effectively force the user to sync
                    ClearLists( );
                    CampusTableView.ReloadData( );
                    TemplateTableView.ReloadData( );
                };

            RockUrlField.ShouldReturn += ( UITextField textField) =>
                {
                    DownloadRockValues( );
                    return true;
                };

            Save.TouchUpInside += (object sender, EventArgs e ) =>
                {
                    ToggleSaveButton( false );

                    BlockerView.BringToFront( );
                    BlockerView.Show( delegate
                        {
                            // officially save and switch over the server
                            Config.Instance.CommitRockSync( );

                            // take the new campus
                            if( CampusSwitch.On )
                            {
                                Config.Instance.AutoDetectCampus = true;
                            }
                            else
                            {
                                Config.Instance.AutoDetectCampus = false;
                            }

                            // go ahead and set the campus index to what they chose. 
                            // If auto-detect is on, it'll override it next time they run
                            int campusIndex = CampusTableView.IndexPathForSelectedRow.Row;
                            Config.Instance.SelectedCampusIndex = campusIndex;

                            int templateIndex = TemplateTableView.IndexPathForSelectedRow.Row;
                            Config.Instance.SetConfigurationDefinedValue( ConfigurationTemplates[ templateIndex ], 
                                delegate(bool result)
                                {
                                    BlockerView.Hide( delegate
                                        {
                                            if( result == true )
                                            {
                                                // it worked, so now apply the campus and URL
                                                Rock.Mobile.Util.Debug.DisplayError( "Settings Updated", "Please restart the app (double tap and flick to quit) to see changes." );

                                                Parent.SettingsComplete( );
                                            }
                                            else
                                            {
                                                // setting the theme failed, but the server switch worked.
                                                ToggleSaveButton( true );
                                                DisplaySyncResult( Strings.General_Rock_ThemeFailed );
                                            }   
                                        });
                                });
                        });
                };

            Cancel.TouchUpInside += (object sender, EventArgs e ) =>
                {
                    // restore the rock URL
                    RockApi.SetRockURL( Config.Instance.RockURL );

                    Parent.SettingsComplete( );
                };
            //
        }

        void ToggleSaveButton( bool enabled )
        {
            Save.Enabled = enabled;

            // adjust the color of the background based on the enabled state
            if ( enabled == true )
            {
                Save.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.PrimaryButtonStyle.BackgroundColor );
            }
            else
            {
                Save.BackgroundColor = UIColor.Gray;
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            //seed the rockURL & authKey in the config
            RockUrlField.Text = Config.Instance.RockURL;
            RockAuthKeyField.Text = Config.Instance.RockAuthorizationKey;

            // download the values
            DownloadRockValues( );

            // start with the submit button disabled, because we need them to re-select stuff
            // in the case that the downloaded values have changed.
            ToggleSaveButton( false );
        }

        public void RowSelected( )
        {
            // the only way we can allow the Save button to be enabled is when the user
            // selects BOTH entries (for campus, either turning on "Auto detect" or picking one)
            if ( (CampusTableView.IndexPathForSelectedRow != null || CampusSwitch.On == true ) && TemplateTableView.IndexPathForSelectedRow != null )
            {
                ToggleSaveButton( true );
            }
        }

        void DownloadRockValues( )
        {
            // only let them submit if they have something beyond "http://" (or some other short crap)
            if ( RockUrlField.Text.IsValidURL( ) == true )
            {
                // hide the keyboard
                RockUrlField.ResignFirstResponder( );
                
                // disable until we're done
                Sync.Enabled = false;
                ToggleSaveButton( false );

                BlockerView.BringToFront( );
                BlockerView.Show( delegate
                    {
                        // see if Rock is AT this server.
                        ApplicationApi.IsRockAtURL( RockUrlField.Text, 
                            delegate(System.Net.HttpStatusCode statusCode, string statusDescription) 
                            {
                                if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                                {
                                    // attempt to contact Rock and get all the required information.
                                    Config.Instance.TryBindToRockServer( RockUrlField.Text, RockAuthKeyField.Text,
                                        delegate( bool result )
                                        {
                                            // it worked, so Rock is valid.
                                            if( result == true )
                                            {
                                                ConfigurationTemplates = Config.Instance.TempConfigurationTemplates;
                                                Campuses = Config.Instance.TempCampuses;
                                                HandleDownloadResult( Strings.General_RockBindSuccess );
                                            }
                                            else
                                            {
                                                // error - clear the lists
                                                ClearLists( );

                                                HandleDownloadResult( Strings.General_RockBindError_Data );
                                            }
                                        });
                                }
                                else
                                {
                                    BlockerView.Hide( 
                                        delegate
                                        {
                                            // error - clear the lists
                                            ClearLists( );

                                            HandleDownloadResult( Strings.General_RockBindError_NotFound );
                                        });
                                }
                            });
                    } );
            }
        }

        void ClearLists( )
        {
            ConfigurationTemplates = new List<Rock.Client.DefinedValue>();
            Campuses = new List<Rock.Client.Campus>();
        }

        void HandleDownloadResult( string result )
        {
            // unhide the blocker
            BlockerView.Hide( 
                delegate
                {
                    Sync.Enabled = true;
                    DisplaySyncResult( result );

                    // reset the currently selected rows if necessary, so that the text color is visible
                    if( CampusTableView.IndexPathForSelectedRow != null )
                    {
                        ((CampusTableData)CampusTableView.Source).SetRowColor( CampusTableView, CampusTableView.IndexPathForSelectedRow, UIColor.Black );
                    }

                    if( TemplateTableView.IndexPathForSelectedRow != null )
                    {
                        ((TemplateTableData)TemplateTableView.Source).SetRowColor( TemplateTableView, TemplateTableView.IndexPathForSelectedRow, UIColor.Black );
                    }

                    CampusTableView.ReloadData( );
                    TemplateTableView.ReloadData( );

                    // now set the appropriate campus selection
                    if( Campuses.Count > 0 )
                    {
                        int campusIndex = 0;

                        // try to find the selected campus in the newly downloaded list.
                        Rock.Client.Campus campus = Campuses.Where( c => c.Id == Config.Instance.Campuses[ Config.Instance.SelectedCampusIndex ].Id ).SingleOrDefault( );
                        if( campus != null )
                        {
                            // we found it, so take its index and set it.
                            campusIndex = Campuses.IndexOf( campus );

                            // update the table selections
                            NSIndexPath rowToSelect = NSIndexPath.FromRowSection( campusIndex, 0 );
                            CampusTableView.SelectRow( rowToSelect, false, UITableViewScrollPosition.None );
                            ((CampusTableData)CampusTableView.Source).SetRowColor( CampusTableView, rowToSelect, UIColor.White );
                        }
                    }


                    // and theme
                    if( ConfigurationTemplates.Count > 0 )
                    {
                        int themeIndex = 0;

                        // try to find the selected campus in the newly downloaded list.
                        Rock.Client.DefinedValue configTemplate = ConfigurationTemplates.Where( ct => ct.Id == Config.Instance.ConfigurationTemplateId ).SingleOrDefault( );
                        if( configTemplate != null )
                        {
                            // we found it, so take its index and set it.
                            themeIndex = ConfigurationTemplates.IndexOf( configTemplate );

                            // update the table selections
                            NSIndexPath rowToSelect = NSIndexPath.FromRowSection( themeIndex, 0 );
                            TemplateTableView.SelectRow( rowToSelect, false, UITableViewScrollPosition.None );
                            ((TemplateTableData)TemplateTableView.Source).SetRowColor( TemplateTableView, rowToSelect, UIColor.White );
                        }
                    }

                    // run this to see if we can enable the save button.
                    RowSelected( );
                });
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();


            // because there's no placeholder, we can't SizeToFit the URL Field, so we'll measure the font and then size it.
            RockUrlField.Bounds = new CGRect( 0, 0, View.Bounds.Width * .75f, 0 );
            CGSize size = RockUrlField.SizeThatFits( new CGSize( RockUrlField.Bounds.Width, RockUrlField.Bounds.Height ) );
            RockUrlField.Bounds = new CGRect( RockUrlField.Bounds.X, RockUrlField.Bounds.Y, RockUrlField.Bounds.Width, (float) System.Math.Ceiling( size.Height ) );


            // center the rock URL field and title
            RockUrlTitle.Layer.Position = new CGPoint( ( View.Bounds.Width - RockUrlField.Bounds.Width ) / 2, View.Bounds.Height * .05f );
            RockUrlField.Layer.Position = new CGPoint( ( View.Bounds.Width - RockUrlField.Bounds.Width ) / 2,
                                                        RockUrlTitle.Frame.Bottom + 10 );



            // because there's no placeholder, we can't SizeToFit the URL Field, so we'll measure the font and then size it.
            RockAuthKeyField.Bounds = new CGRect( 0, 0, View.Bounds.Width * .75f, 0 );
            size = RockAuthKeyField.SizeThatFits( new CGSize( RockAuthKeyField.Bounds.Width, RockAuthKeyField.Bounds.Height ) );
            RockAuthKeyField.Bounds = new CGRect( RockAuthKeyField.Bounds.X, RockAuthKeyField.Bounds.Y, RockAuthKeyField.Bounds.Width, (float) System.Math.Ceiling( size.Height ) );


            // center the rock URL field and title
            RockAuthKeyTitle.Layer.Position = new CGPoint( ( View.Bounds.Width - RockAuthKeyField.Bounds.Width ) / 2, RockUrlField.Frame.Bottom + 10 );
            RockAuthKeyField.Layer.Position = new CGPoint( ( View.Bounds.Width - RockAuthKeyField.Bounds.Width ) / 2, RockAuthKeyTitle.Frame.Bottom + 10 );


            // set the submit bounds and position
            Sync.Bounds = new CGRect( 0, 0, RockAuthKeyField.Bounds.Width * .15f, Sync.Bounds.Height );
            Sync.Layer.Position = new CGPoint( RockAuthKeyField.Frame.Right - Sync.Bounds.Width, RockAuthKeyField.Frame.Bottom + 10 );

            // let the label stretch the entire width
            SyncResultLabel.Bounds = new CGRect( 0, 0, View.Bounds.Width, 0 );
            SyncResultLabel.SizeToFit( );
            SyncResultLabel.Frame = new CGRect( 0, Sync.Frame.Bottom + 20, View.Bounds.Width, SyncResultLabel.Bounds.Height );


            // set the campus table and center its label above it
            CampusesLabel.Layer.Position = new CGPoint( RockUrlTitle.Layer.Position.X, Sync.Frame.Bottom + 60 );

            // table
            CampusTableView.Layer.Position = new CGPoint( RockUrlTitle.Layer.Position.X, CampusesLabel.Frame.Bottom );
            CampusTableView.Bounds = new CGRect( 0, 0, 200, 300 );


            nfloat switchWidth = CampusSwitchLabel.Bounds.Width + CampusSwitch.Bounds.Width + 10;
            nfloat switchXPos = (CampusTableView.Bounds.Width - switchWidth) / 2;

            CampusSwitchLabel.Layer.Position = new CGPoint( RockUrlTitle.Layer.Position.X + switchXPos, View.Bounds.Height * .85f);
            CampusSwitch.Layer.Position = new CGPoint( CampusSwitchLabel.Frame.Right + 10, View.Bounds.Height * .85f );


            // set the template table and center ITS label as well
            TemplateLabel.Layer.Position = new CGPoint( View.Bounds.Width - 300, Sync.Frame.Bottom + 60 );

            TemplateTableView.Layer.Position = new CGPoint( View.Bounds.Width - 300, TemplateLabel.Frame.Bottom );
            TemplateTableView.Bounds = new CGRect( 0, 0, 200, 300 );


            // set the blocker
            BlockerView.SetBounds( View.Bounds.ToRectF( ) );


            // set the Cancel / Save button toward the bottom
            Cancel.Bounds = new CGRect( 0, 0, 100, Cancel.Bounds.Height );
            Save.Bounds = new CGRect( 0, 0, 100, Save.Bounds.Height );

            //nfloat buttonWidth = Cancel.Bounds.Width + Save.Bounds.Width + 10;

            Save.Layer.Position = new CGPoint( View.Bounds.Width - 300, View.Bounds.Height * .85f );
            Cancel.Layer.Position = new CGPoint( Save.Frame.Right + 10, View.Bounds.Height * .85f );

        }

        void DisplaySyncResult( string result )
        {
            SyncResultLabel.Text = result;

            SyncResultLabel.SizeToFit( );

            View.SetNeedsLayout( );
        }
    }
}
