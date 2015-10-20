using System;
using UIKit;
using Rock.Mobile.Animation;
using CoreGraphics;
using System.Drawing;
using Rock.Mobile.PlatformSpecific.Util;
using iOS;
using System.IO;
using Foundation;
using Rock.Mobile.IO;
using CoreAnimation;
using FamilyManager.UI;
using System.Collections.Generic;
using Rock.Mobile.PlatformSpecific.iOS.UI;
using Customization;
using Rock.Mobile.PlatformSpecific.iOS.Graphics;
using Rock.Mobile.Util;
using Rock.Client;
using Rock.Mobile.Network;

namespace FamilyManager
{
    public class AddPersonViewController : UIViewController
    {
        Family WorkingFamily { get; set; }

        ContainerViewController Parent { get; set; }

        /// <summary>
        /// The background panel that will darken the parent view controllers
        /// </summary>
        /// <value>The background panel.</value>
        UIView BackgroundPanel { get; set; }

        /// <summary>
        /// This will be our main UI panel.
        /// </summary>
        /// <value>The main panel.</value>
        UIView MainPanel { get; set; }

        /// <summary>
        ///  True if we're animating OUT the view.
        /// </summary>
        bool IsDismissing { get; set; }

        UIScrollViewWrapper ScrollView { get; set; }

        interface IMemberPanel
        {
            UIView GetRootView( );
            void ViewDidLayoutSubviews( CGRect parentBounds );
            void TouchesEnded( );
        }

        /// <summary>
        ///  This defines the panel to display when we're searching for families
        /// </summary>
        class SearchFamilyPanel : IMemberPanel
        {
            UIView RootView { get; set; }

            Dynamic_UITextField SearchField { get; set; }
            UIButton SearchButton { get; set; }

            List<FamilyResult> Results { get; set; }

            AddPersonViewController Parent { get; set; }
            UIView ParentView { get; set; }

            class FamilyResult
            {
                SearchFamilyPanel Parent { get; set; }

                UIButton Button { get; set; }
                FamilySearchResultView FamilyView { get; set; }

                Family Family { get; set; }

                public FamilyResult( SearchFamilyPanel parent, Family family )
                {
                    Parent = parent;
                    Family = family;

                    FamilyView = new FamilySearchResultView( );
                    parent.GetRootView( ).AddSubview( FamilyView );

                    Button = new UIButton( );
                    Button.Layer.AnchorPoint = CGPoint.Empty;
                    FamilyView.AddSubview( Button );
                    Button.BackgroundColor = UIColor.Clear;

                    Button.TouchUpInside += (object sender, EventArgs e) => 
                        {
                            // don't process the touch if there's no valid family
                            if( Family != null )
                            {
                                Parent.FamilySelected( Family );
                            }
                        };
                }

                public void Remove( )
                {
                    FamilyView.RemoveFromSuperview( );
                }

                public CGRect GetFrame( )
                {
                    return FamilyView.Frame;
                }

                public void LayoutSubviews( CGRect parentBounds )
                {
                    if ( Family != null )
                    {
                        // set the title (their last name)
                        string title = Family.Name;

                        // create and add the person entry. First see if they're an adult or child
                        string adultMembersText = string.Empty;
                        string childMembersText = string.Empty;
                        if ( Family.FamilyMembers.Count > 0 )
                        {
                            // first do adults
                            adultMembersText = "Adults: ";
                            adultMembersText += FamilyView.GetMembersOfTypeString( Family.FamilyMembers, Config.Instance.FamilyMemberAdultGroupRole.Id );

                            // now add kids
                            childMembersText = "Children: ";
                            childMembersText += FamilyView.GetMembersOfTypeString( Family.FamilyMembers, Config.Instance.FamilyMemberChildGroupRole.Id );
                        }

                        // Create the first address line
                        string address1Text = Strings.General_NoAddress;
                        string address2Text = string.Empty;

                        if ( Family.HomeLocation != null )
                        {
                            address1Text = Family.HomeLocation.Street1;

                            // make sure the remainder exists
                            if ( string.IsNullOrWhiteSpace( Family.HomeLocation.City ) == false &&
                                 string.IsNullOrWhiteSpace( Family.HomeLocation.State ) == false &&
                                 string.IsNullOrWhiteSpace( Family.HomeLocation.PostalCode ) == false )
                            {
                                address2Text = Family.HomeLocation.City + ", " +
                                Family.HomeLocation.State + " " +
                                Family.HomeLocation.PostalCode;
                            }
                            else
                            {
                                address2Text = string.Empty;
                            }
                        }

                        FamilyView.FormatCell( parentBounds.Width * .98f, title, adultMembersText, childMembersText, address1Text, address2Text );
                    }
                    else
                    {
                        // since there's no family, have it display a "not found" result
                        FamilyView.FormatCell( parentBounds.Width * .98f, Strings.Search_NoResults_Title, Strings.Search_NoResults_Suggestions, "", Strings.Search_NoResults_Suggestion1, Strings.Search_NoResults_Suggestion2 );
                    }

                    Button.Bounds = FamilyView.Bounds;
                }

                public void SetPosition( CGPoint position )
                {
                    FamilyView.Layer.Position = position;
                }
            }

            public SearchFamilyPanel( )
            {
                Results = new List<FamilyResult>( );
            }

            void FamilySelected( Family family )
            {
                // launch the other view
                Parent.FamilySelected( family );
            }

            public void ViewDidLoad( AddPersonViewController parentViewController )
            {
                Parent = parentViewController;
                ParentView = parentViewController.View;

                RootView = new UIView();
                RootView.Layer.AnchorPoint = CGPoint.Empty;

                // setup the search field
                SearchField = new Dynamic_UITextField( parentViewController, RootView, Strings.General_Search, false, false );
                RootView.AddSubview( SearchField );

                SearchField.GetTextField( ).ShouldReturn += delegate(UITextField textField) 
                    {
                        PerformSearch( );
                        return true;
                    };

                // setup the search button
                SearchButton = UIButton.FromType( UIButtonType.System );
                SearchButton.Layer.AnchorPoint = CGPoint.Empty;
                SearchButton.SetTitle( Strings.General_Search, UIControlState.Normal );
                SearchButton.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
                Theme.StyleButton( SearchButton, Config.Instance.VisualSettings.PrimaryButtonStyle );
                RootView.AddSubview( SearchButton );


                SearchButton.TouchUpInside += (object sender, EventArgs e ) =>
                    {
                        PerformSearch( );  
                    };
            }

            public void PerformSearch( )
            {
                // is there something to search? 
                if( SearchField.GetCurrentValue( ).Length > Settings.General_MinSearchLength )
                {
                    SearchField.ResignFirstResponder( );

                    // disable the button, reset the results, and show the blocker
                    SearchButton.Enabled = false;

                    ClearResults( );

                    SearchField.ResignFirstResponder( );

                    Parent.BlockerView.BringToFront( );

                    Parent.BlockerView.Show( 
                        delegate 
                        {
                            // search for the family
                            RockApi.Get_Groups_FamiliesByPersonNameSearch( SearchField.GetCurrentValue( ), 
                                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.Family> model) 
                                {
                                    // re-enable the search button
                                    SearchButton.Enabled = true;

                                    // if results came back, populate our list
                                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true && model != null && model.Count > 0 )
                                    {
                                        foreach( Rock.Client.Family family in model )
                                        {
                                            FamilyManagerApi.SortFamilyMembers( family.FamilyMembers );

                                            FamilyResult result = new FamilyResult( this, family );
                                            Results.Add( result );

                                            // tell our parent to re-layout so we position the results
                                            ParentView.SetNeedsLayout( );
                                        }
                                    }
                                    else
                                    {
                                        // just create a single entry that represents a 'no result' entry
                                        FamilyResult result = new FamilyResult( this, null );
                                        Results.Add( result );

                                        ParentView.SetNeedsLayout( );
                                    }

                                    Parent.BlockerView.Hide( );
                                });
                        });
                }
            }

            void ClearResults( )
            {
                foreach ( FamilyResult family in Results )
                {
                    family.Remove( );
                }

                Results.Clear( );
            }

            public UIView GetRootView( )
            {
                return RootView;
            }

            public void TouchesEnded( )
            {
            }

            public void ViewDidLayoutSubviews( CGRect parentBounds )
            {
                // set the search field and refresh button, cause those are known
                SearchField.ViewDidLayoutSubviews( new CGRect( 0, 0, parentBounds.Width * .80f, parentBounds.Height ) );
                SearchField.Layer.Position = new CGPoint( (parentBounds.Width - SearchField.Bounds.Width) / 2, 25 );

                // now set the search button, which will use remaining width
                SearchButton.Layer.Position = new CGPoint( 10, SearchField.Frame.Bottom + 10 );
                SearchButton.Bounds = new CGRect( 0, 0, SearchField.Bounds.Width, 0 );
                SearchButton.SizeToFit( );
                SearchButton.Frame = new CGRect( (parentBounds.Width - SearchField.Bounds.Width) / 2, 
                                                 SearchField.Frame.Bottom + 10, 
                                                 SearchField.Bounds.Width, 
                                                 SearchButton.Frame.Height );


                nfloat currentYPos = SearchButton.Frame.Bottom + 25;

                // add all the families
                foreach ( FamilyResult family in Results )
                {
                    family.LayoutSubviews( parentBounds );
                    family.SetPosition( new CGPoint( (parentBounds.Width - family.GetFrame( ).Width) / 2, currentYPos ) );
                    currentYPos = family.GetFrame( ).Bottom + 25;
                }

                RootView.Bounds = new CGRect( 0, 0, parentBounds.Width, currentYPos );
            }
        }
        SearchFamilyPanel SearchPanel { get; set; }

        /// <summary>
        /// The panel that lists family members and lets you pick which ones to add
        /// </summary>
        class BrowsePeoplePanel : IMemberPanel
        {
            AddPersonViewController Parent { get; set; }

            UIView RootView { get; set; }

            Family Family { get; set; }

            UILabel Header { get; set; }

            class FamilyMember
            {
                public UIButton Button { get; set; }
                public bool Enabled { get; set; }
                public Rock.Client.GroupMember Member { get; set; }
            }
            List<FamilyMember> Members { get; set; }

            UIButton Cancel { get; set; }
            UIButton Add { get; set; }

            UILabel RemoveFromOtherFamiliesLabel { get; set; }
            UIToggle RemoveFromOtherFamiliesToggle { get; set; }

            public BrowsePeoplePanel( )
            {
                Members = new List<FamilyMember>( );
            }

            public void ViewDidLoad( AddPersonViewController parentViewController )
            {
                Parent = parentViewController;

                RootView = new UIView();
                RootView.Layer.AnchorPoint = CGPoint.Empty;

                Header = new UILabel();
                Header.Layer.AnchorPoint = CGPoint.Empty;
                Header.Text = Strings.AddPerson_SelectPeople_Header;
                Header.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
                Theme.StyleLabel( Header, Config.Instance.VisualSettings.LabelStyle );
                RootView.AddSubview( Header );

                Cancel = UIButton.FromType( UIButtonType.System );
                Cancel.Layer.AnchorPoint = CGPoint.Empty;
                Cancel.SetTitle( Strings.General_Cancel, UIControlState.Normal );
                Cancel.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
                Theme.StyleButton( Cancel, Config.Instance.VisualSettings.DefaultButtonStyle );
                RootView.AddSubview( Cancel );

                Cancel.TouchUpInside += (object sender, EventArgs e ) =>
                    {
                        Parent.BrowsePeopleFinished( false, false, null );
                    };

                Add = UIButton.FromType( UIButtonType.System );
                Add.Layer.AnchorPoint = CGPoint.Empty;
                Add.SetTitle( Strings.AddPerson_AddPeople_Header, UIControlState.Normal );
                Add.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
                Theme.StyleButton( Add, Config.Instance.VisualSettings.PrimaryButtonStyle );
                RootView.AddSubview( Add );
                Add.TouchUpInside += (object sender, EventArgs e ) =>
                {
                        // go thru and add all the selected people.
                        List<Rock.Client.GroupMember> familyMembers = new List<Rock.Client.GroupMember>( );

                        foreach( FamilyMember member in Members )
                        {
                            if( member.Enabled == true )
                            {
                                familyMembers.Add( member.Member );
                            }
                        }

                        // say "true", we added someone, and then when determining whether to remove them from other families, evaluate the toggle.
                        Parent.BrowsePeopleFinished( true, RemoveFromOtherFamiliesToggle.SideToggled == UIToggle.Toggle.Left ? false : true, familyMembers );
                };


                RemoveFromOtherFamiliesLabel = new UILabel();
                RemoveFromOtherFamiliesLabel.Layer.AnchorPoint = CGPoint.Empty;
                RemoveFromOtherFamiliesLabel.Text = Strings.AddPerson_KeepInOtherFamilies;
                Theme.StyleLabel( RemoveFromOtherFamiliesLabel, Config.Instance.VisualSettings.LabelStyle );
                RootView.AddSubview( RemoveFromOtherFamiliesLabel );


                RemoveFromOtherFamiliesToggle = new UIToggle( Strings.General_Stay, Strings.General_Remove, null );
                RemoveFromOtherFamiliesToggle.Layer.AnchorPoint = CGPoint.Empty;
                Theme.StyleToggle( RemoveFromOtherFamiliesToggle, Config.Instance.VisualSettings.ToggleStyle );
                RemoveFromOtherFamiliesToggle.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
                RootView.AddSubview( RemoveFromOtherFamiliesToggle );
            }

            public void SetFamily( Family family )
            {
                // remove any current members
                foreach ( FamilyMember person in Members )
                {
                    person.Button.RemoveFromSuperview( );
                }
                Members.Clear( );


                Family = family;

                // create a button for each member
                foreach ( Rock.Client.GroupMember member in Family.FamilyMembers )
                {
                    FamilyMember newEntry = new FamilyMember( );
                    newEntry.Button = UIButton.FromType( UIButtonType.System );
                    newEntry.Button.Layer.AnchorPoint = CGPoint.Empty;
                    newEntry.Button.SetTitle( member.Person.NickName, UIControlState.Normal );
                    newEntry.Button.Font = FontManager.GetFont( Settings.General_BoldFont, Config.Instance.VisualSettings.MediumFontSize );
                    newEntry.Button.SizeToFit( );
                    newEntry.Button.Layer.CornerRadius = 4;

                    newEntry.Button.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.BackgroundColor );
                    newEntry.Button.SetTitleColor( Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.TextColor ), UIControlState.Normal );

                    // give it a ref to the person so if it's clicked, it can provide that person back.
                    newEntry.Member = member;

                    newEntry.Button.TouchUpInside += (object sender, EventArgs e ) =>
                    {
                            newEntry.Enabled = !newEntry.Enabled;

                            if( newEntry.Enabled == true )
                            {
                                newEntry.Button.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.SelectedPersonColor );
                            }
                            else
                            {
                                newEntry.Button.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.BackgroundColor );
                            }
                    };

                    RootView.AddSubview( newEntry.Button );
                    Members.Add( newEntry );
                }

                RemoveFromOtherFamiliesToggle.ToggleSide( UIToggle.Toggle.Left );
            }

            public UIView GetRootView( )
            {
                return RootView;
            }

            public void TouchesEnded( )
            {
            }

            public void ViewDidLayoutSubviews( CGRect parentBounds )
            {
                Header.Layer.Position = new CGPoint( ( parentBounds.Width - Header.Bounds.Width ) / 2, Header.Bounds.Height );

                // layout each family member
                nfloat currentYPos = Header.Frame.Bottom + 25;
                foreach ( FamilyMember person in Members )
                {
                    person.Button.Bounds = new CGRect( 0, 0, parentBounds.Width * .45f, person.Button.Bounds.Height );

                    person.Button.Layer.Position = new CGPoint( (parentBounds.Width - person.Button.Bounds.Width) / 2, currentYPos );
                    currentYPos = person.Button.Frame.Bottom + 10;
                }


                // layout the add / cancel buttons
                nfloat buttonControlWidth = Cancel.Bounds.Width + 
                                            Add.Bounds.Width + 10;

                nfloat startingXPos = (parentBounds.Width - buttonControlWidth) / 2;

                Add.Layer.Position = new CGPoint( startingXPos, currentYPos + 10 );
                Cancel.Layer.Position = new CGPoint( Add.Frame.Right + 10, currentYPos + 10 );


                // now layout the switch and its label, below the buttons
                nfloat switchControlWidth = RemoveFromOtherFamiliesLabel.Bounds.Width +
                                            RemoveFromOtherFamiliesToggle.Bounds.Width + 10;

                startingXPos = (parentBounds.Width - switchControlWidth) / 2;

                //RemoveFromOtherFamiliesToggle.ViewDidLayoutSubviews( parentBounds );
                RemoveFromOtherFamiliesToggle.SizeToFit( );

                RemoveFromOtherFamiliesLabel.Layer.Position = new CGPoint( startingXPos, Add.Frame.Bottom + 25 );
                RemoveFromOtherFamiliesToggle.Layer.Position = new CGPoint( startingXPos, RemoveFromOtherFamiliesLabel.Frame.Bottom + 10 );


                RootView.Bounds = new CGRect( 0, 0, parentBounds.Width, RemoveFromOtherFamiliesToggle.Frame.Bottom );
            }
        }
        BrowsePeoplePanel PeoplePanel { get; set; }


        KeyboardAdjustManager KeyboardAdjustManager { get; set; }

        IMemberPanel ActivePanel { get; set; }

        UIBlockerView BlockerView { get; set; }

        UIButton CloseButton { get; set; }

        // used by our parent view controller to know what to pass the Rock endpoint
        public bool RemoveFromOtherFamilies { get; protected set; }

        public AddPersonViewController( ContainerViewController parent )
        {
            Parent = parent;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Background mask
            BackgroundPanel = new UIView( );
            BackgroundPanel.BackgroundColor = UIColor.Black;
            BackgroundPanel.Layer.Opacity = 0.00f;
            View.AddSubview( BackgroundPanel );

            // Floating "main" UI panel
            MainPanel = new UIView();
            MainPanel.Layer.AnchorPoint = CGPoint.Empty;
            MainPanel.BackgroundColor = UIColor.Black;
            MainPanel.Layer.Opacity = 1.00f;
            MainPanel.Bounds = new CoreGraphics.CGRect( 0, 0, View.Bounds.Width * .75f, View.Bounds.Height * .75f );
            View.AddSubview( MainPanel );

            // Scroll view on the right hand side
            ScrollView = new UIScrollViewWrapper( );
            ScrollView.Layer.AnchorPoint = CGPoint.Empty;
            ScrollView.Parent = this;
            //ScrollView.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.AddPersonBGColor );
            ScrollView.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.BackgroundColor );
            MainPanel.AddSubview( ScrollView );


            KeyboardAdjustManager = new KeyboardAdjustManager( MainPanel );


            SearchPanel = new SearchFamilyPanel();
            SearchPanel.ViewDidLoad( this );

            PeoplePanel = new BrowsePeoplePanel();
            PeoplePanel.ViewDidLoad( this );

            // add both views to the scrollView
            ScrollView.AddSubview( SearchPanel.GetRootView( ) );
            ScrollView.AddSubview( PeoplePanel.GetRootView( ) );

            // setup the initial panel positions
            SearchPanel.GetRootView( ).Bounds = ScrollView.Bounds;

            // hide the people/family panels until we know which one the user wants
            PeoplePanel.GetRootView( ).Layer.Position = new CGPoint( MainPanel.Bounds.Width, 0 );
            PeoplePanel.GetRootView( ).Hidden = true;

            // add our Close Button
            CloseButton = UIButton.FromType( UIButtonType.System );
            CloseButton.Layer.AnchorPoint = CGPoint.Empty;
            CloseButton.SetTitle( "X", UIControlState.Normal );
            Theme.StyleButton( CloseButton, Config.Instance.VisualSettings.DefaultButtonStyle );
            CloseButton.SizeToFit( );
            CloseButton.BackgroundColor = UIColor.Clear;
            CloseButton.Layer.BorderWidth = 0;
            MainPanel.AddSubview( CloseButton );
            CloseButton.TouchUpInside += (object sender, EventArgs e ) =>
                {
                    ActivePanel.TouchesEnded( );

                    //ConfirmCancel( );
                    DismissAnimated( false, null );
                };

            // setup the blocker view
            BlockerView = new UIBlockerView( MainPanel, MainPanel.Bounds.ToRectF( ) );

            ActivePanel = SearchPanel;

            MainPanel.Layer.CornerRadius = 4;

            // default to hidden until PresentAnimated() is called
            View.Hidden = true;
        }

        void AnimateToPanel( IMemberPanel fromPanel, CGPoint fromPanelEndPos, IMemberPanel toPanel )
        {
            ScrollView.SetContentOffset( CGPoint.Empty, true );
            
            // animate OUT the from panel (sending it to its endPos)
            SimpleAnimator_PointF fromPanelAnim = new SimpleAnimator_PointF( fromPanel.GetRootView( ).Layer.Position.ToPointF( ), fromPanelEndPos.ToPointF( ), .33f, 
                delegate(float percent, object value )
                {
                    fromPanel.GetRootView( ).Layer.Position = (PointF)value;
                }, 
                null );

            fromPanelAnim.Start( SimpleAnimator.Style.CurveEaseOut );


            // animate IN the toPanel (which we know goes to 0,0 )
            SimpleAnimator_PointF toPanelAnim = new SimpleAnimator_PointF( toPanel.GetRootView( ).Layer.Position.ToPointF( ), CGPoint.Empty.ToPointF( ), .33f, 
                delegate(float percent, object value )
                {
                    toPanel.GetRootView( ).Layer.Position = (PointF)value;
                }, 
                delegate
                {
                    ActivePanel = toPanel;

                    ViewDidLayoutSubviews( );
                } );

            toPanelAnim.Start( SimpleAnimator.Style.CurveEaseOut );
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews( );

            BlockerView.SetBounds( MainPanel.Bounds.ToRectF( ) );

            BackgroundPanel.Bounds = View.Bounds;
            BackgroundPanel.Layer.Position = View.Layer.Position;

            // setup the scroll view and contents
            ScrollView.Bounds = MainPanel.Bounds;

            // layout the active view
            SearchPanel.ViewDidLayoutSubviews( MainPanel.Bounds );
            PeoplePanel.ViewDidLayoutSubviews( MainPanel.Bounds );

            CloseButton.Layer.Position = new CGPoint( MainPanel.Bounds.Width - CloseButton.Bounds.Width - 10, 5 );

            nfloat scrollSize = Math.Max( (float)ActivePanel.GetRootView( ).Frame.Bottom, (float)MainPanel.Bounds.Height ) * 1.05f;

            ScrollView.ContentSize = new CGSize( MainPanel.Bounds.Width, scrollSize );
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);

            ActivePanel.TouchesEnded( );

            UITouch touch = (UITouch) touches.AnyObject;

            CGPoint posInMain = touch.LocationInView( MainPanel );

            // if they tap outside the window (in the shaded area) dismiss this.
            if ( (posInMain.X < 0 || posInMain.X > MainPanel.Bounds.Width) || 
                 (posInMain.Y < 0 || posInMain.Y > MainPanel.Bounds.Height) )
            {
                // confirm they want to cancel
                //ConfirmCancel( );
                DismissAnimated( false, null );
            }
        }

        /// <summary>
        /// Called by the Search panel when a family is selected
        /// </summary>
        void FamilySelected( Family family )
        {
            if ( PeoplePanel.GetRootView( ).Hidden == false )
            {    
                PeoplePanel.SetFamily( family );
                PeoplePanel.ViewDidLayoutSubviews( MainPanel.Bounds );

                AnimateToPanel( SearchPanel, new CGPoint( -MainPanel.Bounds.Width, 0 ), PeoplePanel );
            }
            else
            {
                /*FamilyPanel.SetFamily( family );
                FamilyPanel.ViewDidLayoutSubviews( MainPanel.Bounds );

                AnimateToPanel( SearchPanel, new CGPoint( -MainPanel.Bounds.Width, 0 ), FamilyPanel );*/

                // build the confirmation string
                string familyName = family.Name;
                if ( familyName.EndsWith( " Family" ) == false )
                {
                    familyName += " Family";
                }

                // add the up to the first 6 family members
                string members = null;
                for( int i = 0; i < Math.Min( 6, family.FamilyMembers.Count ); i++ )
                {
                    members += family.FamilyMembers[ i ].Person.NickName + ", ";
                }

                // truncate the trailing ", "
                members = members.Substring( 0, members.Length - 2 );

                string confirmString = string.Format( Strings.AddPerson_AddFamilyAsGuest_Message, familyName, members );

                // Confirm adding the family tapped.
                UIAlertController actionSheet = UIAlertController.Create( Strings.AddPerson_AddFamilyAsGuest_Header, 
                    confirmString,
                    UIAlertControllerStyle.Alert );


                UIAlertAction yesAction = UIAlertAction.Create( Strings.General_Yes, UIAlertActionStyle.Default, delegate(UIAlertAction obj) 
                    {
                        // display 
                        DismissAnimated( true, family );
                    } );

                //setup cancel
                UIAlertAction cancelAction = UIAlertAction.Create( Strings.General_No, UIAlertActionStyle.Cancel, delegate{ } );

                actionSheet.AddAction( yesAction );
                actionSheet.AddAction( cancelAction );

                Parent.PresentViewController( actionSheet, true, null );
            }
        }

        /// <summary>
        /// Called by the BrowsePeople panel when the user presses cancel
        /// </summary>
        void BrowsePeopleFinished( bool addPressed, bool removeFromOtherFamilies, List<Rock.Client.GroupMember> familyMembers )
        {
            // if the user pressed add, we'll add the family and then dismiss
            if ( addPressed )
            {
                RemoveFromOtherFamilies = removeFromOtherFamilies;
                DismissAnimated( true, familyMembers );
            }
            else
            {
                AnimateToPanel( PeoplePanel, new CGPoint( MainPanel.Bounds.Width, 0 ), SearchPanel );
            }
        }

        void ConfirmCancel( )
        {
            UIAlertController actionSheet = UIAlertController.Create( Strings.General_Cancel, 
                Strings.AddPerson_CancelAddingMember, 
                UIAlertControllerStyle.Alert );
            

            UIAlertAction yesAction = UIAlertAction.Create( Strings.General_Yes, UIAlertActionStyle.Default, delegate(UIAlertAction obj) 
                {
                    // display 
                    DismissAnimated( false, null );
                } );
            
            //setup cancel
            UIAlertAction cancelAction = UIAlertAction.Create( Strings.General_No, UIAlertActionStyle.Default, delegate{ } );

            actionSheet.AddAction( yesAction );
            actionSheet.AddAction( cancelAction );

            Parent.PresentViewController( actionSheet, true, null );
        }

        // This ID of the family this person is part of (or will be, if they're new)
        // Note it can be 0 if it's a new family that hasn't been posted yet.
        int WorkingFamilyId { get; set; }

        FamilyInfoViewController.OnBrowsePeopleCompleteDelegate OnCompleteDelegate { get; set; }
        public void PresentAnimated( int workingFamilyId, bool browsingPeople,  FamilyInfoViewController.OnBrowsePeopleCompleteDelegate onComplete )
        {
            KeyboardAdjustManager.Activate( );

            WorkingFamilyId = workingFamilyId;
            
            OnCompleteDelegate = onComplete;

            // default to false
            RemoveFromOtherFamilies = false;
                        
            // always begin at the SearchPanel
            SearchPanel.GetRootView( ).Layer.Position = CGPoint.Empty;

            PeoplePanel.GetRootView( ).Layer.Position = new CGPoint( MainPanel.Bounds.Width, 0 );

            // use the provided bool to set the correct panel visible
            PeoplePanel.GetRootView( ).Hidden = !browsingPeople;

            ActivePanel = SearchPanel;

            View.Hidden = false;

            // animate the background to dark
            BackgroundPanel.Layer.Opacity = 0;

            SimpleAnimator_Float alphaAnim = new SimpleAnimator_Float( BackgroundPanel.Layer.Opacity, Settings.DarkenOpacity, .33f, 
                delegate(float percent, object value )
                {
                    BackgroundPanel.Layer.Opacity = (float)value;
                }, null );

            alphaAnim.Start( SimpleAnimator.Style.CurveEaseOut );


            // animate in the main panel
            MainPanel.Layer.Position = new CoreGraphics.CGPoint( ( View.Bounds.Width - MainPanel.Bounds.Width ) / 2, View.Bounds.Height );

            // animate UP the main panel
            nfloat visibleHeight = Parent.GetVisibleHeight( );
            PointF endPos = new PointF( (float)( View.Bounds.Width - MainPanel.Bounds.Width ) / 2, 
                (float)( visibleHeight - MainPanel.Bounds.Height ) / 2 );

            SimpleAnimator_PointF posAnim = new SimpleAnimator_PointF( MainPanel.Layer.Position.ToPointF( ), endPos, .33f, 
                delegate(float percent, object value )
                {
                    MainPanel.Layer.Position = (PointF)value;
                }, 
                delegate 
                {
                    SearchPanel.PerformSearch( );
                });


            posAnim.Start( SimpleAnimator.Style.CurveEaseOut );
        }

        void DismissAnimated( bool didSave, object returnContext )
        {
            // guard against multiple dismiss requests
            if ( IsDismissing == false )
            {
                IsDismissing = true;

                // run an animation that will dismiss our view, and then remove
                // ourselves from the hierarchy
                SimpleAnimator_Float alphaAnim = new SimpleAnimator_Float( BackgroundPanel.Layer.Opacity, .00f, .33f, 
                                                     delegate(float percent, object value )
                    {
                        BackgroundPanel.Layer.Opacity = (float)value;
                    }, null );

                alphaAnim.Start( SimpleAnimator.Style.CurveEaseOut );


                // animate OUT the main panel
                PointF endPos = new PointF( (float)( View.Bounds.Width - MainPanel.Bounds.Width ) / 2, (float)View.Bounds.Height );

                SimpleAnimator_PointF posAnim = new SimpleAnimator_PointF( MainPanel.Layer.Position.ToPointF( ), endPos, .33f, 
                    delegate(float percent, object value )
                    {
                        MainPanel.Layer.Position = (PointF)value;
                    }, 
                    delegate
                    {
                        // hide ourselves
                        View.Hidden = true;

                        IsDismissing = false;

                        KeyboardAdjustManager.Deactivate( );

                        OnCompleteDelegate( didSave, WorkingFamilyId, returnContext );
                    } );

                posAnim.Start( SimpleAnimator.Style.CurveEaseOut );
            }
        }
    }
}
