using System;
using UIKit;
using System.Collections.Generic;
using Foundation;
using CoreGraphics;
using Rock.Mobile.UI;
using FamilyManager;
using FamilyManager.UI;
using Customization;
using System.Linq;

namespace FamilyManager
{
    /// <summary>
    /// A custom toolbar used for navigation within activities.
    /// Activities may change buttons according to their needs.
    /// </summary>
    public class HistoryBar : UIToolbar
    {
        public class HistoryItem
        {
            public UIButton Button { get; set; }
            public Rock.Client.Family Family { get; set; }

            public delegate void OnHistoryItemClick( Rock.Client.Family family );
            public HistoryItem( Rock.Client.Family family, OnHistoryItemClick onClick )
            {
                // store the family
                Family = family;

                // create the button and add its click handler
                Button = UIButton.FromType( UIButtonType.System );
                Button.SetTitle( UI.FamilySuffixManager.FamilyNameNoSuffix( Family.Name ), UIControlState.Normal );
                Button.SetTitleColor( Theme.GetColor( Config.Instance.VisualSettings.FooterTextColor ), UIControlState.Normal );

                // now measure the button label
                NSString createLabel = new NSString( UI.FamilySuffixManager.FamilyNameNoSuffix( Family.Name ) );
                CGSize buttonSize = createLabel.StringSize( Button.Font );
                Button.Bounds = new CGRect( 0, 0, buttonSize.Width, buttonSize.Height );


                Button.TouchUpInside += delegate 
                    {
                        onClick( Family );
                    };
            }

        }
        List<HistoryItem> HistoryList { get; set; }

        const int MaxHistory = 6;

        UIButton SettingsButton { get; set; }

        public HistoryBar( CGRect parentFrame, EventHandler onSettingsPressed ) : base()
        {
            Layer.AnchorPoint = CGPoint.Empty;


            // setup the left label
            NSString leftLabel = new NSString( "" );

            SettingsButton = UIButton.FromType( UIButtonType.System );
            SettingsButton.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_IconFont, 32 );
            SettingsButton.SetTitle( leftLabel.ToString( ), UIControlState.Normal );
            SettingsButton.SetTitleColor( Theme.GetColor( Config.Instance.VisualSettings.FooterTextColor ), UIControlState.Normal );
            SettingsButton.TouchUpInside += onSettingsPressed;

            CGSize labelSize = leftLabel.StringSize( SettingsButton.Font );
            SettingsButton.Bounds = new CGRect( 0, 0, labelSize.Width, labelSize.Height );

            // create the list that will store our items
            HistoryList = new List<HistoryItem>();

            // even tho there's nothing, update it so we have our label.
            UpdateItems( );

            TintColor = UIColor.Clear;
            Translucent = false;

            BarTintColor = Theme.GetColor( Config.Instance.VisualSettings.FooterBGColor );
            Layer.Opacity = Settings.StatusBar_Opacity;

            Bounds = new CGRect( 0, 0, parentFrame.Width, Settings.StatusBar_Height );
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            // with the layout ready, set ourselves to be at the bottom of the screen.
            Frame = new CGRect( 0, Superview.Frame.Height - 44, Frame.Width, Frame.Height );
        }

        public bool TryPushHistoryItem( Rock.Client.Family family, HistoryItem.OnHistoryItemClick onClick )
        {
            // key off the family's ID to see if it's unique or not.
            if ( HistoryList.Where( h => h.Family.Id == family.Id ).SingleOrDefault( ) == null )
            {
                HistoryItem newItem = new HistoryItem( family, onClick );

                // now add it to our list
                HistoryList.Add( newItem );

                // cap the list at 4 (beyond that, start dropping off the oldest one)
                if ( HistoryList.Count > MaxHistory )
                {
                    HistoryList.RemoveAt( 0 );
                }

                UpdateItems( );

                return true;
            }
            return false;
        }

        public bool TryUpdateHistoryItem( Rock.Client.Family family )
        {
            // find the history item storing this family
            HistoryItem currItem = HistoryList.Where( h => h.Family.Id == family.Id ).SingleOrDefault( );
            if ( currItem != null )
            {
                // update the family and button title for the entry
                currItem.Family = family;
                currItem.Button.SetTitle( UI.FamilySuffixManager.FamilyNameNoSuffix( family.Name ), UIControlState.Normal );

                return true;
            }

            return false;
        }

        void UpdateItems( )
        {
            // add the initial label to the left
            List<UIBarButtonItem> itemList = new List<UIBarButtonItem>( );
            itemList.Add( new UIBarButtonItem( SettingsButton ) );


            // now determine how large the label spacer should be
            nfloat itemListWidth = 0;
            foreach ( HistoryItem historyItem in HistoryList )
            {
                // add an extra ~20 to account for the spacing iOS adds between buttons (which we cannot access)
                itemListWidth += historyItem.Button.Bounds.Width + Settings.StatusBar_SpacerWidth + 20;
            }

            // create and add the label spacer
            UIBarButtonItem spacer = new UIBarButtonItem( UIBarButtonSystemItem.FixedSpace );
            spacer.Width = Bounds.Width - SettingsButton.Bounds.Width - itemListWidth;
            itemList.Add( spacer );



            // now add all items provided
            UIBarButtonItem itemSpacer = new UIBarButtonItem( UIBarButtonSystemItem.FixedSpace );
            itemSpacer.Width = Settings.StatusBar_SpacerWidth;

            foreach ( HistoryItem historyItem in HistoryList )
            {
                itemList.Add( new UIBarButtonItem( historyItem.Button ) );
                itemList.Add( itemSpacer );
            }



            // for some reason, it will not accept a new array of items
            // until we clear the existing.
            SetItems( new UIBarButtonItem[0], false );

            SetItems( itemList.ToArray( ), false );
        }
    }
}
    