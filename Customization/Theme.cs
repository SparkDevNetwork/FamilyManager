using System;
using Newtonsoft.Json;
using System.IO;
using Rock.Mobile.Network;
using RestSharp;
using System.Net;
using UIKit;
using Rock.Mobile.IO;
using Foundation;
using CoreAnimation;
using Rock.Mobile.PlatformSpecific.iOS.UI;
using Rock.Mobile.PlatformSpecific.iOS.Graphics;

namespace Customization
{   
    /// <summary>
    /// Defines the aspects of the app that can be skinned
    /// </summary>
    [Serializable]
    public class Theme
    {
        [JsonProperty]
        public string BackgroundURL { get; protected set; }

        [JsonProperty]
        public string BackgroundColor { get; protected set; }

        [JsonProperty]
        public string SidebarBGColor { get; protected set; }

        [JsonProperty]
        public string PhotoOutlineColor { get; protected set; }

        [JsonProperty]
        public string LogoURL { get; protected set; }

        [JsonProperty]
        public string AdultMaleNoPhoto { get; protected set; }

        [JsonProperty]
        public string AdultFemaleNoPhoto { get; protected set; }

        [JsonProperty]
        public string ChildMaleNoPhoto { get; protected set; }

        [JsonProperty]
        public string ChildFemaleNoPhoto { get; protected set; }

        [JsonProperty]
        public string TopHeaderBGColor { get; protected set; }

        [JsonProperty]
        public string TopHeaderTextColor { get; protected set; }

        [JsonProperty]
        public string FooterBGColor { get; protected set; }

        [JsonProperty]
        public string FooterTextColor { get; protected set; }

        [JsonProperty]
        public string SelectedPersonColor { get; protected set; }


        [JsonProperty]
        public uint LargeFontSize { get; protected set; }

        [JsonProperty]
        public uint MediumFontSize { get; protected set; }

        [JsonProperty]
        public uint SmallFontSize { get; protected set; }
            
        /// <summary>
        /// Defines the properties for a button used for this theme
        /// </summary>
        [Serializable]
        public class Button
        {
            [JsonProperty]
            public string BackgroundColor { get; protected set; }

            [JsonProperty]
            public string TextColor { get; protected set; }

            [JsonProperty]
            public string BorderColor { get; protected set; }
                
            [JsonProperty]
            public uint BorderWidth { get; protected set; }

            [JsonProperty]
            public uint CornerRadius { get; protected set; }
        }
        [JsonProperty]
        public Button DefaultButtonStyle { get; protected set; }

        [JsonProperty]
        public Button PrimaryButtonStyle { get; protected set; }

        /// <summary>
        /// Defines the properties for the text used for this theme
        /// </summary>
        [Serializable]
        public class Label
        {
            [JsonProperty]
            public string TextColor { get; protected set; }
        }
        [JsonProperty]
        public Label LabelStyle { get; protected set; }

        /// <summary>
        /// Defines the properties for the values of DatePicker fields used for this theme
        /// </summary>
        [Serializable]
        public class DatePicker
        {
            [JsonProperty]
            public string BackgroundColor { get; protected set; }

            [JsonProperty]
            public string TextColor { get; protected set; }
        }
        [JsonProperty]
        public DatePicker DatePickerStyle { get; protected set; }

        /// <summary>
        /// Defines the properties for the values of a Switch used for this theme
        /// </summary>
        [Serializable]
        public class Switch
        {
            [JsonProperty]
            public string OnColor { get; protected set; }
        }
        [JsonProperty]
        public Switch SwitchStyle { get; protected set; }

        /// <summary>
        /// Defines the properties for the values of a Switch used for this theme
        /// </summary>
        [Serializable]
        public class FamilyCell
        {
            [JsonProperty]
            public string BackgroundColor { get; protected set; }

            [JsonProperty]
            public string AddFamilyButtonBGColor { get; protected set; }

            [JsonProperty]
            public string AddFamilyButtonTextColor { get; protected set; }

            [JsonProperty]
            public string EntryBGColor { get; protected set; }

            [JsonProperty]
            public string EntryTextColor { get; protected set; }
        }
        [JsonProperty]
        public FamilyCell FamilyCellStyle { get; protected set; }

        /// <summary>
        /// Defines the properties for the values of a Switch used for this theme
        /// </summary>
        [Serializable]
        public class SearchResult
        {
            [JsonProperty]
            public string BackgroundColor { get; protected set; }

            [JsonProperty]
            public string TextColor { get; protected set; }
        }
        [JsonProperty]
        public SearchResult SearchResultStyle { get; protected set; }

        /// <summary>
        /// Defines the properties for the text used for this theme
        /// </summary>
        [Serializable]
        public class TextField
        {
            [JsonProperty]
            public string TextColor { get; protected set; }

            [JsonProperty]
            public string PlaceHolderColor { get; protected set; }

            [JsonProperty]
            public string BorderColor { get; protected set; }

            [JsonProperty]
            public uint BorderWidth { get; protected set; }

            [JsonProperty]
            public uint CornerRadius { get; protected set; }
        }
        [JsonProperty]
        public TextField TextFieldStyle { get; protected set; }

        /// <summary>
        /// Defines the properties for the toggle used for this theme
        /// </summary>
        [Serializable]
        public class Toggle
        {
            [JsonProperty]
            public string TextColor { get; protected set; }

            [JsonProperty]
            public string InActiveColor { get; protected set; }

            [JsonProperty]
            public string ActiveColor { get; protected set; }

            [JsonProperty]
            public uint CornerRadius { get; protected set; }

            [JsonProperty]
            public uint BorderWidth { get; protected set; }

            [JsonProperty]
            public string BorderColor { get; protected set; }
        }
        [JsonProperty]
        public Toggle ToggleStyle { get; protected set; }



        // Below are helper functions for managing the theme and applying it.
        public static string BackgroundImageName = "backgroundImage";
        public static string LogoImageName = "logoImage";
        public static string AdultMaleNoPhotoName = "adultMaleNoPhoto";
        public static string AdultFemaleNoPhotoName = "adultFemaleNoPhoto";
        public static string ChildMaleNoPhotoName = "childMaleNoPhoto";
        public static string ChildFemaleNoPhotoName = "childFemaleNoPhoto";

        void DownloadImage( string imageUrl, string imageFileName, string rockUrl, FileCache.FileDownloaded onResult )
        {
            if ( string.IsNullOrEmpty( imageUrl ) == false )
            {
                string qualifiedUrl;
                if ( imageUrl[ 0 ] == '~' )
                {
                    qualifiedUrl = imageUrl.Replace( "~", rockUrl );
                }
                else
                {
                    qualifiedUrl = imageUrl;
                }

                FileCache.Instance.DownloadFileToCache( qualifiedUrl, imageFileName, onResult );
            }
        }

        public void DownloadImages( string rockUrl, FileCache.FileDownloaded onResult )
        {
            DownloadImage( LogoURL, LogoImageName, rockUrl, null );
            DownloadImage( BackgroundURL, BackgroundImageName, rockUrl, null );
            DownloadImage( AdultMaleNoPhoto, AdultMaleNoPhotoName, rockUrl, null );
            DownloadImage( AdultFemaleNoPhoto, AdultFemaleNoPhotoName, rockUrl, null );
            DownloadImage( ChildMaleNoPhoto, ChildMaleNoPhotoName, rockUrl, null );
            DownloadImage( ChildFemaleNoPhoto, ChildFemaleNoPhotoName, rockUrl, null );

            // dont make them wait.
            onResult( true );
        }

        public void RemoveDownloadedImages( )
        {
            // used before we switch themes
            FileCache.Instance.RemoveFile( BackgroundImageName );
            FileCache.Instance.RemoveFile( LogoImageName );
        }

        public static UIColor GetColor( string color )
        {
            if( color == null || color[0] != '#' ) 
            {
                //throw new Exception( String.Format( "Colors must be in the format #RRGGBBAA. Color found: {0}", color ) );
                // force Magenta so they notice that the color isn't set right.
                color = "#FF00FFFF";
            }
            UInt32 uintColor = Convert.ToUInt32( color.Substring( 1 ), 16 ); //skip the first character

            return Rock.Mobile.UI.Util.GetUIColor( uintColor );
        }

        public static void StyleButton( UIButton button, Theme.Button style )
        {
            if ( style == null )
            {
                Rock.Mobile.Util.Debug.DisplayError( "Error", "Button Style is null!" );
            }
            else
            {
                button.BackgroundColor = Theme.GetColor( style.BackgroundColor );
                button.SetTitleColor( Theme.GetColor( style.TextColor ), UIControlState.Normal );
                button.Layer.CornerRadius = style.CornerRadius;
                button.Layer.BorderWidth = style.BorderWidth;
                button.Layer.BorderColor = Theme.GetColor( style.BorderColor ).CGColor;
                button.SizeToFit( );
                button.Bounds = new CoreGraphics.CGRect( 0, 0, button.Bounds.Width * 1.25f, button.Bounds.Height );
            }
        }

        public static void StyleLabel( UILabel label, Theme.Label style )
        {
            if ( style == null )
            {
                Rock.Mobile.Util.Debug.DisplayError( "Error", "Label Style is null!" );
            }
            else
            {
                label.BackgroundColor = UIColor.Clear;
                label.TextColor = Theme.GetColor( style.TextColor );
                label.SizeToFit( );
            }
        }

        public static void StyleDatePicker( UILabel datePicker, UILabel datePickerSymbol, UIButton clearButton, Theme.DatePicker style )
        {
            if ( style == null )
            {
                Rock.Mobile.Util.Debug.DisplayError( "Error", "DatePicker Style is null!" );
            }
            else
            {
                datePicker.BackgroundColor = UIColor.Clear;
                datePicker.TextColor = Theme.GetColor( style.TextColor );
                datePicker.SizeToFit( );

                // setup the date symbol, which uses a special icon font

                datePickerSymbol.Text = "";
                datePickerSymbol.TextColor = Theme.GetColor( style.TextColor );
                datePickerSymbol.SizeToFit( );


                clearButton.SetTitle( "", UIControlState.Normal );
                clearButton.SetTitleColor( UIColor.Red, UIControlState.Normal );
                clearButton.SizeToFit( );
            }
        }

        public static void StyleSwitch( UISwitch uiSwitch, Theme.Switch style )
        {
            if ( style == null )
            {
                Rock.Mobile.Util.Debug.DisplayError( "Error", "Switch style is null!" );
            }
            else
            {
                uiSwitch.OnTintColor = Theme.GetColor( style.OnColor );
            }
        }

        public static void StyleTextField( UITextField textField, Theme.TextField style )
        {
            if ( style == null )
            {
                Rock.Mobile.Util.Debug.DisplayError( "Error", "TextField Style is null!" );
            }
            else
            {
                textField.BackgroundColor = UIColor.Clear;
                textField.TextColor = Theme.GetColor( style.TextColor );
                textField.Layer.BorderColor = Theme.GetColor( style.BorderColor ).CGColor;
                textField.Layer.BorderWidth = style.BorderWidth;
                textField.Layer.CornerRadius = style.CornerRadius;
                //textField.AttributedPlaceholder = new NSAttributedString( textField.Placeholder, null, Theme.GetColor( style.PlaceHolderColor ) );
            }
        }

        public static void StyleToggle( UIToggle uiToggle, Theme.Toggle style )
        {
            if ( style == null )
            {
                Rock.Mobile.Util.Debug.DisplayError( "Error", "Toggle Style is null!" );
            }
            else
            {
                uiToggle.ActiveColor = Theme.GetColor( style.ActiveColor );
                uiToggle.InActiveColor = Theme.GetColor( style.InActiveColor );
                uiToggle.TextColor = Theme.GetColor( style.TextColor );

                uiToggle.ClipsToBounds = true;
                uiToggle.Layer.CornerRadius = style.CornerRadius;

                uiToggle.BorderWidth = style.BorderWidth;
                uiToggle.BorderColor = Theme.GetColor( style.BorderColor ).CGColor;
            }
        }
    }
}
