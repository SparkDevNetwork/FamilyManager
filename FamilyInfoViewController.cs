using System;
using UIKit;
using System.IO;
using Foundation;
using Rock.Mobile.PlatformSpecific.iOS.UI;
using CoreGraphics;
using Rock.Mobile.Util.Strings;
using System.Collections.Generic;
using Rock.Mobile.IO;
using FamilyManager.UI;
using iOS;
using CoreAnimation;
using Customization;
using Rock.Mobile.PlatformSpecific.iOS.Graphics;
using Rock.Mobile.Util;
using Rock.Mobile.PlatformSpecific.Util;
using Rock.Mobile.Network;
using System.Net;
using System.Linq;

namespace FamilyManager
{
    public class FamilyInfoViewController : UIViewController
    {
        public class TableSource : UITableViewSource 
        {
            /// <summary>
            /// Defines the "button" that displays a large symbol and some text
            /// </summary>
            public class SymbolButton : UIView
            {
                UILabel Symbol { get; set; }
                UILabel Label { get; set; }
                UIButton Button { get; set; }

                public SymbolButton( string symbol, UIFont font, string description, UIColor bgColor, UIColor textColor, CGRect bounds, EventHandler onTouch ) : base( )
                {
                    Layer.AnchorPoint = CGPoint.Empty;
                    Bounds = bounds;
                    BackgroundColor = bgColor;

                    Symbol = new UILabel( );
                    Symbol.Layer.AnchorPoint = CGPoint.Empty;
                    Symbol.Font = font;
                    Symbol.TextColor = textColor;
                    Symbol.Text = symbol;
                    Symbol.SizeToFit( );
                    AddSubview( Symbol );

                    Label = new UILabel( );
                    Label.Layer.AnchorPoint = CGPoint.Empty;
                    Label.Font = FontManager.GetFont( Settings.General_LightFont, 16 );
                    Label.TextColor = textColor;
                    Label.Text = description;
                    Label.Bounds = new CGRect( 0, 0, Bounds.Width - 10, 0 );
                    Label.Lines = 0;
                    Label.SizeToFit( );
                    Label.TextAlignment = UITextAlignment.Center;
                    Label.Bounds = new CGRect( 0, 0, Label.Bounds.Width, Label.Bounds.Height );
                    AddSubview( Label );

                    Button = UIButton.FromType( UIButtonType.System );
                    Button.Layer.AnchorPoint = CGPoint.Empty;
                    Button.Bounds = Bounds;
                    AddSubview( Button );

                    Button.TouchUpInside += onTouch;

                    Symbol.Layer.Position = new CGPoint( (Bounds.Width - Symbol.Bounds.Width) / 2, 10 );
                    Label.Layer.Position = new CGPoint( (Bounds.Width - Label.Bounds.Width) / 2, Symbol.Frame.Bottom );
                }
            }

            /// <summary>
            /// Defines the top cell that displays all family members
            /// </summary>
            class PrimaryCell : UITableViewCell
            {
                public static string Identifier = "PrimaryCell";

                public class PersonEntry
                {
                    public static nfloat ImageSize = 90;
                    public static nfloat PersonEntrySize = 120;
                    public static nfloat AgeMaskSize = 40;

                    public UIView View { get; set; }
                    UIButton Button { get; set; }
                    public UIImageView ImageView { get; set; }
                    public UILabel Name { get; set; }
                    public UILabel Age { get; set; }

                    public Rock.Client.Person PersonModel { get; set; }
                    public bool IsChild { get; set; }
                    public NSData ImageBuffer { get; set; }

                    CAShapeLayer AgeShapeLayer { get; set; }

                    EventHandler OnImageDownloaded { get; set; }

                    public delegate void OnPersonEntryClicked( Rock.Client.Person person, NSData imageBuffer );
                    public PersonEntry( Rock.Client.Person personModel, bool isChild, OnPersonEntryClicked personClickedDelegate )
                    {
                        View = new UIView( );
                        View.Layer.AnchorPoint = CGPoint.Empty;
                        View.Layer.CornerRadius = 4;
                        View.Bounds = new CGRect( 0, 0, PersonEntrySize, PersonEntrySize );

                        Button = UIButton.FromType( UIButtonType.System );
                        Button.Layer.AnchorPoint = CGPoint.Empty;
                        Button.Bounds = View.Bounds;
                        View.AddSubview( Button );


                        ImageView = new UIImageView( );
                        ImageView.Layer.AnchorPoint = CGPoint.Empty;
                        View.AddSubview( ImageView );

                        PersonModel = personModel;
                        IsChild = isChild;

                        // set the profile image mask so it's circular
                        CALayer maskLayer = new CALayer();
                        maskLayer.AnchorPoint = new CGPoint( 0, 0 );
                        maskLayer.BackgroundColor = UIColor.Black.CGColor;
                        ImageView.Layer.Mask = maskLayer;
                        ImageView.BackgroundColor = UIColor.Black;


                        // we can't position these until we know their length
                        Name = new UILabel( );
                        Name.Layer.AnchorPoint = CGPoint.Empty;
                        Name.Font = FontManager.GetFont( Settings.General_LightFont, Config.Instance.VisualSettings.SmallFontSize );
                        Name.TextAlignment = UITextAlignment.Center;
                        View.AddSubview( Name );

                        // procedurally create the little fancy mask that goes under the age.
                        AgeShapeLayer = new CAShapeLayer( );
                        CGPath path = new CGPath( );

                        // render as a triangle
                        path.MoveToPoint( 0, 0 );
                        path.AddLineToPoint( AgeMaskSize, 0 );
                        path.AddLineToPoint( AgeMaskSize, AgeMaskSize );
                        path.AddLineToPoint( 0, 0 );
                        path.CloseSubpath( );

                        AgeShapeLayer.Path = path;
                        AgeShapeLayer.FillColor = UIColor.Black.CGColor;
                        AgeShapeLayer.Position = new CGPoint( View.Bounds.Width - AgeMaskSize, 0 );
                        AgeShapeLayer.Opacity = .10f;
                        View.Layer.AddSublayer( AgeShapeLayer );


                        // setup the age value
                        Age = new UILabel( );
                        Age.Layer.AnchorPoint = CGPoint.Empty;
                        Age.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
                        View.AddSubview( Age );

                        Name.TextColor = Theme.GetColor( Config.Instance.VisualSettings.FamilyCellStyle.EntryTextColor );
                        Age.TextColor = Theme.GetColor( Config.Instance.VisualSettings.FamilyCellStyle.EntryTextColor );
                        View.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.FamilyCellStyle.EntryBGColor );

                        Button.TouchUpInside += (object sender, EventArgs e) => 
                            {
                                personClickedDelegate( PersonModel, ImageBuffer );
                            };
                    }

                    public void SetInfo( )
                    {
                        // set and limit the name width
                        Name.Text = PersonModel.NickName;
                        Name.SizeToFit( );
                        Name.Bounds = new CGRect( 0, 0, View.Bounds.Width, Name.Bounds.Height );
                        Name.Layer.Position = new CGPoint( ( View.Bounds.Width - Name.Bounds.Width ) / 2, View.Bounds.Height - Name.Bounds.Height );

                        int personAge = -1;
                        if ( PersonModel.BirthDate.HasValue )
                        {
                            personAge = PersonModel.BirthDate.Value.AsAge( );
                        }

                        // set their age.
                        // if they actually have an age value then great, use that.
                        if ( personAge != -1 )
                        {
                            Age.Text = personAge.ToString( );
                        }
                        // otherwise, check their role in the group to at least see if they're an adult or child
                        else
                        {
                            if ( IsChild == true )
                            {
                                Age.Text = "C";
                            }
                            else
                            {
                                Age.Text = "A";
                            }
                        }
                        Age.SizeToFit( );

                        // position the age "centered" within the age shape layer. 
                        // we add a magic number to get it more centered within the visible triangular area
                        Age.Layer.Position = new CGPoint( AgeShapeLayer.Position.X + 7 + ( ( AgeMaskSize - Age.Bounds.Width ) / 2 ), 0 );



                        // set their picture.
                        // first decide which placeholder image to use. (default to adult male)
                        string placeholderImageName = string.Empty;
                        if ( IsChild == false )
                        {
                            // default to male in the case of no gender
                            placeholderImageName = Theme.AdultMaleNoPhotoName;

                            if ( PersonModel.Gender == Rock.Client.Enums.Gender.Female )
                            {
                                placeholderImageName = Theme.AdultFemaleNoPhotoName;
                            }
                        }
                        else
                        {
                            // default to male in the case of no gender
                            placeholderImageName = Theme.ChildMaleNoPhotoName;

                            if ( PersonModel.Gender == Rock.Client.Enums.Gender.Female )
                            {
                                placeholderImageName = Theme.ChildFemaleNoPhotoName;
                            }
                        }

                        // load the placeholder image
                        MemoryStream imageStream = (MemoryStream)FileCache.Instance.LoadFile( placeholderImageName );
                        if ( imageStream != null )
                        {
                            NSData imageBuffer = NSData.FromArray( imageStream.ToArray( ) );
                            SetProfilePic( imageBuffer );
                        }

                        if ( PersonModel.PhotoId.HasValue && PersonModel.PhotoId.Value > -1 )
                        {
                            // don't allow viewing them if we're downloading their image.
                            Button.Enabled = false;
                            DownloadProfilePic( PersonModel.PhotoId.Value );
                        }
                    }

                    void DownloadProfilePic( int photoId )
                    {
                        RockApi.Get_GetImage( photoId.ToString( ), (uint)PersonEntry.ImageSize * 2, delegate(System.Net.HttpStatusCode statusCode, string statusDescription, MemoryStream imageStream )
                            {
                                Rock.Mobile.Threading.Util.PerformOnUIThread( delegate
                                    {
                                        Button.Enabled = true;

                                        // if the image downloaded, use it
                                        if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                                        {
                                            ImageBuffer = NSData.FromArray( imageStream.ToArray( ) );
                                            SetProfilePic( ImageBuffer );
                                        }

                                        ImageView.SetNeedsDisplay( );
                                    });
                            } );
                    }

                    void SetProfilePic( NSData imageBuffer )
                    {
                        ImageView.Image = new UIImage( imageBuffer );
                        ImageView.Bounds = new CGRect( 0, 0, ImageSize, ImageSize );

                        nfloat availableHeight = View.Bounds.Height - Name.Frame.Height;
                        ImageView.Layer.Position = new CGPoint( (View.Bounds.Width - ImageView.Bounds.Width) / 2, (availableHeight - ImageView.Bounds.Height) / 2 );

                        ImageView.Layer.Mask.Bounds = ImageView.Bounds;
                        ImageView.Layer.Mask.CornerRadius = ImageView.Layer.Mask.Bounds.Width / 2;
                    }
                }
                public List<PersonEntry> Members { get; set; }

                UILabel FamilyMembersLabel { get; set; }
                UIView Container { get; set; }
                TableSource ParentTableSource { get; set; }
                SymbolButton AddFamilyMemberIcon { get; set; }
                UILabel GuestFamily { get; set; }
                Rock.Client.Family Family { get; set; }

                public PrimaryCell( UITableViewCellStyle style, string cellIdentifier, TableSource parentTableSource ) : base( style, cellIdentifier )
                {
                    SelectionStyle = UITableViewCellSelectionStyle.None;

                    ParentTableSource = parentTableSource;

                    BackgroundColor = UIColor.Clear;

                    Container = new UIView( );
                    Container.Layer.AnchorPoint = CGPoint.Empty;
                    Container.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.FamilyCellStyle.BackgroundColor );
                    AddSubview( Container );

                    // setup the "Family Members" label (which goes at the top of this primary cell)
                    FamilyMembersLabel = new UILabel( );
                    FamilyMembersLabel.Layer.AnchorPoint = CGPoint.Empty;
                    FamilyMembersLabel.Font = FontManager.GetFont( Settings.General_LightFont, Config.Instance.VisualSettings.MediumFontSize );
                    FamilyMembersLabel.Text = Strings.FamilyInfo_FamilyMembers;
                    Theme.StyleLabel( FamilyMembersLabel, Config.Instance.VisualSettings.LabelStyle );
                    FamilyMembersLabel.SizeToFit( );

                    // setup the Guest Family label (which goes at the bottom of this primary cell)
                    GuestFamily = new UILabel( );
                    GuestFamily.Layer.AnchorPoint = CGPoint.Empty;
                    GuestFamily.Font = FontManager.GetFont( Settings.General_LightFont, Config.Instance.VisualSettings.MediumFontSize );
                    GuestFamily.Text = Strings.FamilyInfo_GuestFamilies;
                    Theme.StyleLabel( GuestFamily, Config.Instance.VisualSettings.LabelStyle );
                    GuestFamily.SizeToFit( );

                    AddFamilyMemberIcon = new SymbolButton( "", 
                                                      Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "Bh", 64 ), 
                                                      Strings.General_AddFamilyMember,
                                                      Theme.GetColor( Config.Instance.VisualSettings.FamilyCellStyle.AddFamilyButtonBGColor ),
                                                      Theme.GetColor( Config.Instance.VisualSettings.FamilyCellStyle.AddFamilyButtonTextColor ),
                                                      new CGRect( 0, 0, PrimaryCell.PersonEntry.PersonEntrySize, PrimaryCell.PersonEntry.PersonEntrySize ), 
                        delegate(object sender, EventArgs e) 
                        {
                            ParentTableSource.AddMemberToFamily( AddFamilyMemberIcon, Family.Id, Family.Name );
                        } );
                    AddFamilyMemberIcon.Layer.CornerRadius = 4;
                }

                public void SetFamily( Rock.Client.Family family )
                {
                    Family = family;
                    
                    // remove any current family member from the view hierarchy
                    if ( Container.Subviews.Length > 0 )
                    {
                        foreach ( UIView child in Container.Subviews )
                        {
                            child.RemoveFromSuperview( );
                        }
                    }

                    // add the "Family Members" label
                    AddSubview( FamilyMembersLabel );

                    // add the Guest label
                    AddSubview( GuestFamily );

                    // add our "Add Family Member" button
                    Container.AddSubview( AddFamilyMemberIcon );
                                        
                    // setup our list
                    Members = new List<PersonEntry>( family.FamilyMembers.Count );

                    // for each family member, create a person entry and add them to the cell
                    foreach ( Rock.Client.GroupMember familyMember in family.FamilyMembers )
                    {
                        // create and add the person entry. First see if they're an adult or child
                        bool isChild = familyMember.GroupRole.Id == Config.Instance.FamilyMemberChildGroupRole.Id ? true : false;

                        PersonEntry personEntry = new PersonEntry( familyMember.Person, isChild,
                            delegate(Rock.Client.Person person, NSData imageBuffer )
                            {
                                ParentTableSource.EditFamilyMember( familyMember, imageBuffer );
                            });
                        Members.Add( personEntry );

                        // set their name and age
                        personEntry.SetInfo( );

                        // add this person to the cell
                        Container.AddSubview( personEntry.View );
                    }
                }

                static int PeoplePerRow = 3;

                public void LayoutFamily( CGSize parentSize )
                {
                    nfloat cellWidth = parentSize.Width * .95f;

                    // determine the spacing between members
                    nfloat xSpacing = cellWidth - (PersonEntry.PersonEntrySize * PeoplePerRow);
                    xSpacing /= PeoplePerRow;

                    nfloat ySpacing = 25;

                    // seed the row, xPos and yPos with values that allow the loop to run without
                    // any special logic
                    nfloat yPos = -(PersonEntry.PersonEntrySize + ySpacing);
                    nfloat xPos = -1;
                    nfloat row = -1;

                    int i;
                    for( i = 0; i < Members.Count; i++ )
                    {
                        // when the row is full, reset X and advance the row
                        if ( i % PeoplePerRow == 0 )
                        {
                            yPos += ySpacing + PersonEntry.PersonEntrySize;
                            xPos = 0;
                            row++;
                        }

                        Members[ i ].View.Layer.Position = new CGPoint( xPos, yPos );
                        xPos += PersonEntry.PersonEntrySize + xSpacing;
                    }

                    // add the "Add Family Member" button to the bottom.
                    // if we're starting on a new row, advance the x/y position
                    if ( i % PeoplePerRow == 0 )
                    {
                        yPos += ySpacing + PersonEntry.PersonEntrySize;
                        xPos = 0;
                    }
                    AddFamilyMemberIcon.Layer.Position = new CGPoint( xPos, yPos );

                    // set the container bounds
                    Container.Bounds = new CGRect( 0, 0, cellWidth, yPos + PersonEntry.PersonEntrySize + ySpacing );

                    // set the cell bounds
                    Bounds = new CGRect( 0, 0, cellWidth, Container.Bounds.Height * 1.10f );

                    // layout the guest family
                    FamilyMembersLabel.Layer.Position = new CGPoint( 0, 10 );

                    // center the container within the cell
                    Container.Layer.Position = new CGPoint( 0, FamilyMembersLabel.Frame.Bottom + 10 );

                    // layout the guest family
                    GuestFamily.Layer.Position = new CGPoint( 0, Container.Frame.Bottom + 15 );

                    // finally, increase the frame itself to fit GuestFamily
                    Bounds = new CGRect( 0, 0, cellWidth, GuestFamily.Frame.Bottom );
                }
            }


            /// <summary>
            /// Defines the cells that display guest families
            /// </summary>
            class GuestFamilyCell : UITableViewCell
            {
                public static string Identifier = "GuestFamilyCell";

                public class PersonEntry
                {
                    public static nfloat PersonEntryWidth = 125;
                    public static nfloat PersonEntryHeight = 50;

                    public UIButton Button { get; set; }
                    public UILabel Name { get; set; }
                    public UILabel AgeLabel { get; set; } //this will be either adult or child
                    public bool CanCheckin { get; set; }
                    public int PersonAliasId { get; set; }

                    // These are the members of the primary family
                    public List<Rock.Client.GroupMember> PrimaryFamilyMembers { get; set; }

                    public PersonEntry( string personName, int personAliasId, bool canCheckin, string roleInFamily, List<Rock.Client.GroupMember> primaryFamilyMembers )
                    {
                        PrimaryFamilyMembers = primaryFamilyMembers;

                        PersonAliasId = personAliasId;

                        CanCheckin = canCheckin;

                        Button = new UIButton( UIButtonType.System );
                        Button.Layer.AnchorPoint = CGPoint.Empty;
                        Button.Layer.CornerRadius = 4;
                        Button.Bounds = new CGRect( 0, 0, PersonEntryWidth, PersonEntryHeight );

                        Name = new UILabel( );
                        Name.Layer.AnchorPoint = CGPoint.Empty;
                        Name.Text = personName;
                        Name.SizeToFit( );
                        Name.TextColor = Theme.GetColor( Config.Instance.VisualSettings.PrimaryButtonStyle.TextColor );
                        Button.AddSubview( Name );

                        AgeLabel = new UILabel( );
                        AgeLabel.Layer.AnchorPoint = CGPoint.Empty;
                        AgeLabel.Text = roleInFamily;
                        AgeLabel.TextColor = Theme.GetColor( Config.Instance.VisualSettings.PrimaryButtonStyle.TextColor );
                        AgeLabel.Font = FontManager.GetFont( Settings.General_LightFont, Config.Instance.VisualSettings.SmallFontSize );
                        AgeLabel.SizeToFit( );
                        Button.AddSubview( AgeLabel );


                        nfloat combinedHeight = Name.Layer.Bounds.Height + AgeLabel.Bounds.Height;
                        nfloat startingY = (PersonEntryHeight - combinedHeight) / 2;

                        Name.Layer.Position = new CGPoint( (PersonEntryWidth - Name.Bounds.Width) / 2, startingY );
                        AgeLabel.Layer.Position = new CGPoint( (PersonEntryWidth - AgeLabel.Bounds.Width) / 2, Name.Frame.Bottom );

                        Button.TouchUpInside += (object sender, EventArgs e) => 
                            {
                                Button.Enabled = false;

                                // are they currently able to check in?
                                if( CanCheckin == true )
                                {
                                    // then remove it.
                                    int pendingRemovals = 0;
                                    foreach( Rock.Client.GroupMember member in primaryFamilyMembers )
                                    {
                                        FamilyManagerApi.RemoveKnownRelationship( member.Person.PrimaryAliasId.Value, PersonAliasId, Config.Instance.CanCheckInGroupRole.Id, delegate
                                            {
                                                // once we hear back from all the requests, toggle the button
                                                pendingRemovals++;

                                                if( pendingRemovals == primaryFamilyMembers.Count )
                                                {
                                                    Button.Enabled = true;
                                                    CanCheckin = !CanCheckin;
                                                    UpdateBGColor( );
                                                    Button.SetNeedsDisplay( );
                                                }
                                            });
                                    }
                                }
                                else
                                {
                                    // simply bind them to the first person
                                    FamilyManagerApi.UpdateKnownRelationship( primaryFamilyMembers[ 0 ].Person.PrimaryAliasId.Value, PersonAliasId, Config.Instance.CanCheckInGroupRole.Id, delegate
                                        {
                                            Button.Enabled = true;
                                            CanCheckin = !CanCheckin;
                                            UpdateBGColor( );
                                            Button.SetNeedsDisplay( );
                                        });
                                }
                            };

                        UpdateBGColor( );
                    }

                    void UpdateBGColor( )
                    {
                        if ( CanCheckin == true )
                        {
                            Button.Layer.BorderColor = Theme.GetColor( Config.Instance.VisualSettings.PrimaryButtonStyle.BorderColor ).CGColor;
                            Button.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.PrimaryButtonStyle.BackgroundColor );
                            Button.Layer.BorderWidth = Config.Instance.VisualSettings.PrimaryButtonStyle.BorderWidth;
                            AgeLabel.TextColor = Theme.GetColor( Config.Instance.VisualSettings.PrimaryButtonStyle.TextColor );
                            Name.TextColor = Theme.GetColor( Config.Instance.VisualSettings.PrimaryButtonStyle.TextColor );
                        }
                        else
                        {
                            Button.Layer.BorderColor = Theme.GetColor( Config.Instance.VisualSettings.DefaultButtonStyle.BorderColor ).CGColor;
                            Button.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.DefaultButtonStyle.BackgroundColor );
                            Button.Layer.BorderWidth = Config.Instance.VisualSettings.DefaultButtonStyle.BorderWidth;
                            AgeLabel.TextColor = Theme.GetColor( Config.Instance.VisualSettings.DefaultButtonStyle.TextColor );
                            Name.TextColor = Theme.GetColor( Config.Instance.VisualSettings.DefaultButtonStyle.TextColor );
                        }
                    }
                }
                public List<PersonEntry> Members { get; set; }

                public UILabel FamilyName { get; set; }
                TableSource ParentTableSource { get; set; }
                UIButton AddFamilyMemberButton { get; set; }
                Rock.Client.GuestFamily GuestFamily { get; set; }
                public UIView Container { get; set; }

                // This is the primary family for which this GustFamily is a guest of.
                public Rock.Client.Family PrimaryFamily { get; set; }

                public GuestFamilyCell( UITableViewCellStyle style, string cellIdentifier, TableSource parentTableSource ) : base( style, cellIdentifier )
                {
                    SelectionStyle = UITableViewCellSelectionStyle.None;

                    ParentTableSource = parentTableSource;

                    BackgroundColor = UIColor.Clear;

                    // create the family name
                    FamilyName = new UILabel( );
                    FamilyName.Layer.AnchorPoint = CGPoint.Empty;
                    Theme.StyleLabel( FamilyName, Config.Instance.VisualSettings.LabelStyle );
                    FamilyName.Font = FontManager.GetFont( Settings.General_BoldFont, Config.Instance.VisualSettings.MediumFontSize );

                    Container = new UIView( );
                    Container.Layer.AnchorPoint = CGPoint.Empty;
                    Container.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.BackgroundColor );
                    Container.Layer.CornerRadius = 4;
                    AddSubview( Container );

                    //AddFamilyIcon = new AddFamilyEntry( parentTableSource );
                    AddFamilyMemberButton = UIButton.FromType( UIButtonType.System );
                    AddFamilyMemberButton.Layer.AnchorPoint = CGPoint.Empty;
                    AddFamilyMemberButton.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "Bh", 64 );
                    AddFamilyMemberButton.SetTitle( "", UIControlState.Normal );
                    AddFamilyMemberButton.SetTitleColor( Theme.GetColor( Config.Instance.VisualSettings.LabelStyle.TextColor ), UIControlState.Normal );
                    AddFamilyMemberButton.Layer.CornerRadius = 4;
                    AddFamilyMemberButton.TouchUpInside += (object sender, EventArgs e) => 
                        {
                            ParentTableSource.AddMemberToFamily( AddFamilyMemberButton, GuestFamily.Id, GuestFamily.Name );
                        };

                    AddFamilyMemberButton.SizeToFit( );
                }

                public void SetFamily( Rock.Client.Family primaryFamily, Rock.Client.GuestFamily guestFamily )
                {
                    // store a ref to the primary and guest family
                    PrimaryFamily = primaryFamily;
                    GuestFamily = guestFamily;

                    // remove any current family member from the view hierarchy
                    if ( Container.Subviews.Length > 0 )
                    {
                        foreach ( UIView child in Container.Subviews )
                        {
                            if ( child as UIButton != null )
                            {
                                child.RemoveFromSuperview( );
                            }
                        }
                    }

                    FamilyName.Text = guestFamily.Name;
                    FamilyName.SizeToFit( );

                    // add the family name
                    Container.AddSubview( FamilyName );

                    // add our "Add Family Member" button
                    Container.AddSubview( AddFamilyMemberButton );

                    // setup our list
                    Members = new List<PersonEntry>( guestFamily.FamilyMembers.Count );

                    // for each family member, create a person entry and add them to the cell
                    foreach ( Rock.Client.GuestFamily.Member familyMember in guestFamily.FamilyMembers )
                    {
                        // create and add the person entry
                        PersonEntry personEntry = new PersonEntry( familyMember.FirstName, familyMember.PersonAliasId, familyMember.CanCheckin, familyMember.Role, primaryFamily.FamilyMembers );
                        Members.Add( personEntry );

                        // add this person to the cell
                        Container.AddSubview( personEntry.Button );
                    }
                }

                static int PeoplePerRow = 3;

                static nfloat AddMemberWidth = 60;

                public void LayoutFamily( CGSize parentSize )
                {
                    nfloat cellWidth = parentSize.Width * .95f;

                    // the available row width should be reduced by the width of the edge button
                    nfloat availRowWidth = cellWidth - AddMemberWidth;
                    
                    // determine the spacing between members
                    nfloat xSpacing = availRowWidth - (PersonEntry.PersonEntryWidth * PeoplePerRow);
                    xSpacing /= PeoplePerRow;

                    nfloat ySpacing = 25;
                    FamilyName.Layer.Position = new CGPoint( 10, 10 );



                    // seed the row, xPos and yPos with values that allow the loop to run without
                    // any special logic
                    nfloat yPos = -(PersonEntry.PersonEntryHeight + ySpacing - 10) + FamilyName.Frame.Bottom;
                    nfloat xPos = -1;
                    nfloat row = -1;

                    int i;
                    for( i = 0; i < Members.Count; i++ )
                    {
                        // when the row is full, reset X and advance the row
                        if ( i % PeoplePerRow == 0 )
                        {
                            yPos += ySpacing + PersonEntry.PersonEntryHeight;
                            xPos = 10;
                            row++;
                        }

                        Members[ i ].Button.Layer.Position = new CGPoint( xPos, yPos );
                        xPos += PersonEntry.PersonEntryWidth + xSpacing;
                    }


                    // now set the add family button
                    AddFamilyMemberButton.Bounds = new CGRect( 0, 0, AddMemberWidth, AddMemberWidth );

                    // set the container bounds
                    Container.Bounds = new CGRect( 0, 0, cellWidth, Members[ i - 1 ].Button.Frame.Bottom + ySpacing );

                    AddFamilyMemberButton.Layer.Position = new CGPoint( cellWidth - AddFamilyMemberButton.Bounds.Width, (Container.Bounds.Height - AddMemberWidth) / 2 );

                    // set the cell bounds
                    Bounds = new CGRect( 0, 0, cellWidth, Container.Bounds.Height * 1.10f );

                    // center the container within the cell
                    Container.Layer.Position = new CGPoint( 0, (Bounds.Height - Container.Bounds.Height) / 2 );
                }
            }

            /// <summary>
            /// Defines the cells that display guest families
            /// </summary>
            class FooterCell : UITableViewCell
            {
                public static string Identifier = "GuestFamilyCell";

                public UIView Seperator { get; set; }
                TableSource ParentTableSource { get; set; }
                public SymbolButton AddGuestFamilyButton { get; set; }
                public UIView Container { get; set; }

                public FooterCell( UITableViewCellStyle style, string cellIdentifier, TableSource parentTableSource ) : base( style, cellIdentifier )
                {
                    SelectionStyle = UITableViewCellSelectionStyle.None;

                    BackgroundColor = UIColor.Clear;

                    ParentTableSource = parentTableSource;

                    Container = new UIView( );
                    Container.Layer.AnchorPoint = CGPoint.Empty;
                    Container.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.FamilyCellStyle.BackgroundColor );
                    Container.Layer.CornerRadius = 4;
                    AddSubview( Container );

                    AddGuestFamilyButton = new SymbolButton( "", 
                        Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( "Bh", 64 ), 
                        Strings.General_AddGuestFamily,
                        Theme.GetColor( Config.Instance.VisualSettings.FamilyCellStyle.AddFamilyButtonBGColor ),
                        Theme.GetColor( Config.Instance.VisualSettings.FamilyCellStyle.AddFamilyButtonTextColor ),
                        new CGRect( 0, 0, PrimaryCell.PersonEntry.PersonEntrySize, PrimaryCell.PersonEntry.PersonEntrySize ), 
                        delegate(object sender, EventArgs e) 
                        {
                            ParentTableSource.HandleAddGuestFamily( );
                        } );

                    AddGuestFamilyButton.Layer.CornerRadius = 4;

                    Container.AddSubview( AddGuestFamilyButton );
                }

                public void Layout( CGSize parentSize )
                {
                    nfloat cellWidth = parentSize.Width * .95f;

                    // set the container bounds
                    Container.Bounds = new CGRect( 0, 0, cellWidth, AddGuestFamilyButton.Bounds.Height * 1.15f );

                    // set the "Add Guest Family Button"
                    AddGuestFamilyButton.Layer.Position = new CGPoint( 0, (Container.Bounds.Height - AddGuestFamilyButton.Bounds.Height) / 2 );

                    // set the cell bounds
                    Bounds = new CGRect( 0, 0, cellWidth, Container.Bounds.Height * 1.10f );

                    // center the container within the cell
                    Container.Layer.Position = new CGPoint( 0, (Bounds.Height - Container.Bounds.Height) / 2 );
                }
            }

            FamilyInfoViewController Parent { get; set; }
            nfloat PrimaryCellHeight { get; set; }
            List<nfloat> GuestFamilyCellHeight { get; set; }
            nfloat FooterCellHeight { get; set; }
            public bool PrimaryCellDirty { get; set; }

            /// <summary>
            /// Definition for the source table that backs the tableView
            /// </summary>
            public TableSource ( FamilyInfoViewController parent )
            {
                Parent = parent;

                // create a list for the guest family cell heights, since they're variable
                GuestFamilyCellHeight = new List<nfloat>( );
            }

            /// <summary>
            /// Called by the "Add Family Member" button in either the primary or Guest Family rows.
            /// </summary>
            /// <param name="sourceView">Source view.</param>
            /// <param name="familyId">Family identifier.</param>
            public void AddMemberToFamily( UIView sourceView, int workingFamilyId, string familyLastName )
            {
                UIAlertController actionSheet = UIAlertController.Create( Strings.FamilyInfo_AddFamilyMemberHeader, 
                                                                          Strings.FamilyInfo_AddFamilyMemberBody, 
                                                                          UIAlertControllerStyle.ActionSheet );

                // if the device is a tablet, anchor the menu
                actionSheet.PopoverPresentationController.SourceView = sourceView;
                actionSheet.PopoverPresentationController.SourceRect = sourceView.Bounds;

                UIAlertAction addNewPerson = UIAlertAction.Create( Strings.FamilyInfo_AddNewPerson, UIAlertActionStyle.Default, delegate(UIAlertAction obj )
                    {
                        // display 
                        Parent.HandleAddNewPerson( workingFamilyId, familyLastName );
                    } );

                // setup the photo library
                UIAlertAction addExistingAction = UIAlertAction.Create( Strings.FamilyInfo_AddExistingPerson, UIAlertActionStyle.Default, delegate(UIAlertAction obj )
                    {
                        // launch the search person
                        Parent.HandleAddExistingPerson( workingFamilyId );
                    } );

                //setup cancel
                UIAlertAction cancelAction = UIAlertAction.Create( Strings.General_Cancel, UIAlertActionStyle.Cancel, delegate
                    {
                    } );

                actionSheet.AddAction( addNewPerson );
                actionSheet.AddAction( addExistingAction );
                actionSheet.AddAction( cancelAction );

                Parent.PresentViewController( actionSheet, true, null );
            }

            public void EditFamilyMember( Rock.Client.GroupMember familyMember, NSData imageBuffer )
            {
                Parent.HandleEditFamilyMember( familyMember, imageBuffer );
            }

            public void HandleAddGuestFamily( )
            {
                // launch the search person
                Parent.HandleAddGuestFamily( );
            }

            public override nint RowsInSection (UITableView tableview, nint section)
            {
                // if there are guest families, return all of them,
                // plus 1 for the primary, plus 1 more for padding.
                return Parent.GuestFamilies.Count + 2;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                tableView.DeselectRow( indexPath, true );
            }

            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
            {
                return CalcRowHeight( tableView, indexPath.Row );
            }

            public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath)
            {
                return CalcRowHeight( tableView, indexPath.Row );
            }

            nfloat CalcRowHeight( UITableView tableView, int rowIndex )
            {
                if ( rowIndex == 0 )
                { 
                    return PrimaryCellHeight != 0 ? PrimaryCellHeight : tableView.RowHeight;
                }
                else
                {
                    if ( rowIndex - 1 < GuestFamilyCellHeight.Count )
                    {
                        return GuestFamilyCellHeight[ rowIndex - 1 ];
                    }
                    else
                    {
                        return FooterCellHeight + 50;
                    }
                }
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                if ( indexPath.Row == 0 )
                {
                    return GetPrimaryCell( tableView, indexPath.Row );
                }
                else if ( indexPath.Row - 1 < Parent.GuestFamilies.Count )
                {
                    return GetGuestFamilyCell( tableView, indexPath.Row );
                }
                else
                {
                    return GetFooterCell( tableView, indexPath.Row );
                }
            }

            UITableViewCell GetPrimaryCell( UITableView tableView, int rowIndex )
            {
                PrimaryCell cell = tableView.DequeueReusableCell( PrimaryCell.Identifier ) as PrimaryCell;

                // if there are no cells to reuse, create a new one
                if (cell == null || PrimaryCellDirty == true )
                {
                    PrimaryCellDirty = false;

                    cell = new PrimaryCell( UITableViewCellStyle.Default, PrimaryCell.Identifier, this );

                    // setup the members in this family
                    cell.SetFamily( Parent.Family );
                }

                cell.LayoutFamily( tableView.Bounds.Size );

                PrimaryCellHeight = cell.Bounds.Height;

                return cell;
            }

            UITableViewCell GetGuestFamilyCell( UITableView tableView, int rowIndex )
            {
                GuestFamilyCell cell = tableView.DequeueReusableCell( GuestFamilyCell.Identifier ) as GuestFamilyCell;

                // if there are no cells to reuse, create a new one
                if (cell == null)
                {
                    cell = new GuestFamilyCell( UITableViewCellStyle.Default, GuestFamilyCell.Identifier, this );
                }


                // setup the members in this family
                cell.SetFamily( Parent.Family, Parent.GuestFamilies[ rowIndex - 1 ] );

                cell.LayoutFamily( tableView.Bounds.Size );

                // get the cell height
                nfloat cellHeight = cell.Bounds.Height;

                // if the row is beyond what we've stored, add it.
                if ( rowIndex - 1 >= GuestFamilyCellHeight.Count )
                {
                    GuestFamilyCellHeight.Add( cellHeight );
                }
                else
                {
                    // otherwise replace the existing entry
                    GuestFamilyCellHeight[ rowIndex - 1 ] = cellHeight;
                }

                return cell;
            }

            UITableViewCell GetFooterCell( UITableView tableView, int rowIndex )
            {
                FooterCell cell = tableView.DequeueReusableCell( FooterCell.Identifier ) as FooterCell;

                // if there are no cells to reuse, create a new one
                if (cell == null)
                {
                    cell = new FooterCell( UITableViewCellStyle.Default, FooterCell.Identifier, this );
                }

                cell.Layout( tableView.Bounds.Size );

                FooterCellHeight = cell.Bounds.Height;

                return cell;
            }
        }

        UITableView TableView { get; set; }

        public Rock.Client.Family Family { get; protected set; }

        Rock.Client.Group FamilyGroupObject { get; set; }
        Rock.Client.GroupLocation FamilyAddress { get; set; }

        List<Rock.Client.GuestFamily> GuestFamilies { get; set; }

        ContainerViewController Parent { get; set; }

        UIScrollViewWrapper ScrollView { get; set; }

        Dynamic_UITextField FamilyName { get; set; }

        Dynamic_UIDropDown FamilyCampus { get; set; }

        UILabel AddressHeader { get; set; }
        UIInsetTextField Street { get; set; }
        UIInsetTextField City { get; set; }
        UIInsetTextField State { get; set; }
        UIInsetTextField PostalCode { get; set; }

        UIButton SaveButton { get; set; }

        List<IDynamic_UIView> Dynamic_FamilyControls { get; set; }

        KeyboardAdjustManager KeyboardAdjustManager { get; set; }

        PersonInfoViewController PersonInfoViewController { get; set; }
        AddPersonViewController AddPersonViewController { get; set; }

        UIBlockerView BlockerView { get; set; }

        const string FamilySuffix = " Family";

        public FamilyInfoViewController( ContainerViewController parent, Rock.Client.Family family )
        {
            Parent = parent;

            FamilyGroupObject = null;

            // support creating a NEW family by letting them pass null as the 
            // family argument
            if ( family != null )
            {
                Family = family;
            }
            else
            {
                GuestFamilies = new List<Rock.Client.GuestFamily>();
                Family = new Rock.Client.Family();
            }

            Dynamic_FamilyControls = new List<IDynamic_UIView>( );
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.Layer.Contents = Parent.View.Layer.Contents;
            View.BackgroundColor = Parent.View.BackgroundColor;

            // setup a scroll view
            ScrollView = new UIScrollViewWrapper();
            ScrollView.Layer.AnchorPoint = CGPoint.Empty;
            ScrollView.Parent = this;
            ScrollView.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.SidebarBGColor );
            View.AddSubview( ScrollView );

            FamilyName = new Dynamic_UITextField( this, ScrollView, Strings.General_FamilyName, false, true );
            FamilyName.GetTextField( ).AutocorrectionType = UITextAutocorrectionType.No;
            FamilyName.GetTextField( ).AutocapitalizationType = UITextAutocapitalizationType.Words;
            FamilyName.AddToView( ScrollView );

            //TODO: Handle international
            AddressHeader = new UILabel();
            AddressHeader.Layer.AnchorPoint = CGPoint.Empty;
            AddressHeader.Text = Strings.General_HomeAddress;
            AddressHeader.Font = FontManager.GetFont( Settings.General_BoldFont, Config.Instance.VisualSettings.SmallFontSize );
            Theme.StyleLabel( AddressHeader, Config.Instance.VisualSettings.LabelStyle );
            ScrollView.AddSubview( AddressHeader );

            Street = new UIInsetTextField();
            Street.InputAssistantItem.LeadingBarButtonGroups = null;
            Street.InputAssistantItem.TrailingBarButtonGroups = null;
            Street.Layer.AnchorPoint = CGPoint.Empty;
            Street.Placeholder = Strings.General_Street;
            Street.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
            Theme.StyleTextField( Street, Config.Instance.VisualSettings.TextFieldStyle );
            Street.AutocorrectionType = UITextAutocorrectionType.No;
            Street.AutocapitalizationType = UITextAutocapitalizationType.Words;
            ScrollView.AddSubview( Street );

            City = new UIInsetTextField();
            City.InputAssistantItem.LeadingBarButtonGroups = null;
            City.InputAssistantItem.TrailingBarButtonGroups = null;
            City.Layer.AnchorPoint = CGPoint.Empty;
            City.Placeholder = Strings.General_City;
            City.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
            Theme.StyleTextField( City, Config.Instance.VisualSettings.TextFieldStyle );
            City.AutocorrectionType = UITextAutocorrectionType.No;
            City.AutocapitalizationType = UITextAutocapitalizationType.Words;
            ScrollView.AddSubview( City );

            State = new UIInsetTextField();
            State.InputAssistantItem.LeadingBarButtonGroups = null;
            State.InputAssistantItem.TrailingBarButtonGroups = null;
            State.Layer.AnchorPoint = CGPoint.Empty;
            State.Placeholder = Strings.General_State;
            State.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
            Theme.StyleTextField( State, Config.Instance.VisualSettings.TextFieldStyle );
            State.AutocorrectionType = UITextAutocorrectionType.No;
            State.AutocapitalizationType = UITextAutocapitalizationType.Words;
            ScrollView.AddSubview( State );

            PostalCode = new UIInsetTextField();
            PostalCode.InputAssistantItem.LeadingBarButtonGroups = null;
            PostalCode.InputAssistantItem.TrailingBarButtonGroups = null;
            PostalCode.Layer.AnchorPoint = CGPoint.Empty;
            PostalCode.Placeholder = Strings.General_Zip;
            PostalCode.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
            Theme.StyleTextField( PostalCode, Config.Instance.VisualSettings.TextFieldStyle );
            ScrollView.AddSubview( PostalCode );
            //

            // build an array with all the campuses
            string[] campuses = new string[ Config.Instance.Campuses.Count ];
            for ( int i = 0; i < Config.Instance.Campuses.Count; i++ )
            {
                campuses[ i ] = Config.Instance.Campuses[ i ].Name;
            }

            FamilyCampus = new Dynamic_UIDropDown( this, ScrollView, Strings.FamilyInfo_Select_Campus_Header, Strings.FamilyInfo_Select_Campus_Message, campuses, false );
            FamilyCampus.AddToView( ScrollView );

            //default the campus to whatever's selected by the app settings.
            FamilyCampus.SetCurrentValue( Config.Instance.Campuses[ Config.Instance.SelectedCampusIndex ].Name );

            // build the dynamic UI controls
            for( int i = 0; i < Config.Instance.FamilyAttributeDefines.Count; i++ )
            {
                // get the required flag and the attribs that define what type of UI control this is.
                bool isRequired = bool.Parse( Config.Instance.FamilyAttributes[ i ][ "required" ] );
                Rock.Client.Attribute uiControlAttrib = Config.Instance.FamilyAttributeDefines[ i ];

                // build it and add it to our UI
                IDynamic_UIView uiView = Dynamic_UIFactory.CreateDynamic_UIControl( this, ScrollView, uiControlAttrib, isRequired, Config.Instance.FamilyAttributeDefines[ i ].Key );
                if ( uiView != null )
                {
                    Dynamic_FamilyControls.Add( uiView );
                    Dynamic_FamilyControls[ Dynamic_FamilyControls.Count - 1 ].AddToView( ScrollView );
                }
            }

            KeyboardAdjustManager = new KeyboardAdjustManager( View );


            // save button goes last, BELOW the dynamic content
            SaveButton = UIButton.FromType( UIButtonType.System );
            SaveButton.Layer.AnchorPoint = CGPoint.Empty;
            SaveButton.SetTitle( Strings.General_Save, UIControlState.Normal );
            SaveButton.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
            Theme.StyleButton( SaveButton, Config.Instance.VisualSettings.PrimaryButtonStyle );
            SaveButton.SetTitleColor( UIColor.LightGray, UIControlState.Disabled );
            SaveButton.SizeToFit( );
            SaveButton.Bounds = new CGRect( 0, 0, SaveButton.Bounds.Width * 2.00f, SaveButton.Bounds.Height );
            ScrollView.AddSubview( SaveButton );

            SaveButton.TouchUpInside += (object sender, EventArgs e ) =>
                {
                    TrySubmitFamilyInfo( );
                };


            TableView = new UITableView( );
            TableView.Layer.AnchorPoint = CGPoint.Empty;
            TableView.BackgroundColor = UIColor.Clear;
            TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            View.AddSubview( TableView );

            // if there's no family ID this is a new family, so create a table that lets them begin adding people
            if ( Family.Id == 0 )
            {
                TableView.Source = new TableSource( this );
                //FamilyName.SetCurrentValue( Strings.FamilyInfo_Unnamed_Family );

                // we can also safely set the state to its default value.
                State.Text = Config.Instance.DefaultState;
            }

            // create the new person and add person view controllers
            PersonInfoViewController = new PersonInfoViewController( Parent );
            AddChildViewController( PersonInfoViewController );
            View.AddSubview( PersonInfoViewController.View );

            AddPersonViewController = new AddPersonViewController( Parent );
            AddChildViewController( AddPersonViewController );
            View.AddSubview( AddPersonViewController.View );

            BlockerView = new UIBlockerView( View, View.Bounds.ToRectF( ) );
        }

        bool IsRefreshingFamily { get; set; }
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            KeyboardAdjustManager.Activate( );

            // if there IS a family to refresh
            if ( Family.Id != 0 )
            {
                RefreshFamily( );
            }
        }

        void RefreshFamily( )
        {
            // don't allow refreshing if we're creating a new family or refreshing an existing.
            if ( IsRefreshingFamily == false )
            {
                IsRefreshingFamily = true;

                BlockerView.Show(
                    delegate
                    {
                        GuestFamilies = new List<Rock.Client.GuestFamily>();

                        // grab this specific family
                        RockApi.Get_Groups_GetFamily( Family.Id, 
                            delegate(System.Net.HttpStatusCode statusCode, string statusDescription, Rock.Client.Family model )
                            {
                                if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                                {
                                    if( model != null )
                                    {
                                        Family = model;

                                        GetFamilyGroup( );
                                    }
                                    else
                                    {
                                        HandleFamilyGone( );
                                    }
                                }
                                else
                                {
                                    RefreshFamilyDone( false );
                                }
                            } );
                    } );
            }
        }

        void HandleFamilyGone( )
        {
            // this is called if we search for a family and they aren't there. If that happens,
            // notify the user, and setup values as if this is a new family
            BlockerView.Hide( delegate 
                { 
                    Rock.Mobile.Util.Debug.DisplayError( Strings.FamilyInfo_Header_Gone, Strings.FamilyInfo_Body_Gone ); 

                    FamilyGroupObject = null;

                    GuestFamilies = new List<Rock.Client.GuestFamily>();
                    Family = new Rock.Client.Family();

                    TableView.Source = new TableSource( this );
                    FamilyName.SetCurrentValue( Strings.FamilyInfo_Unnamed_Family );
                    TableView.ReloadData( );
                } );
        }

        void GetFamilyGroup( )
        {
            // get the group object that represents this family
            ApplicationApi.GetFamilyGroupModelById( Family.Id, 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, Rock.Client.Group model) 
                {
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                    {
                        FamilyGroupObject = model;

                        GetGuestsForFamily( );
                    }
                    else
                    {
                        RefreshFamilyDone( false );
                    }
                });
        }

        void GetGuestsForFamily( )
        {
            // request the associated families
            RockApi.Get_Groups_GuestsForFamily( Family.Id, 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.GuestFamily> model )
                {
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                    {
                        GuestFamilies = model;

                        // sort the family members by age and gender.
                        foreach( Rock.Client.GuestFamily guestFamily in GuestFamilies )
                        {
                            FamilyManagerApi.SortGuestFamilyMembers( guestFamily.FamilyMembers );
                        }

                        RefreshFamilyDone( true );
                    }
                    else
                    {
                        RefreshFamilyDone( false );
                    }
                } );
        }

        void RefreshFamilyDone( bool result )
        {
            BlockerView.Hide(
                delegate 
                {
                    IsRefreshingFamily = false;

                    if ( result == true )
                    {
                        Rock.Mobile.Threading.Util.PerformOnUIThread( 
                            delegate
                            {
                                // notify our parent that the family was updated.
                                Parent.FamilyUpdated( Family );

                                FamilyInfoToUI( );
                            } );
                    }
                    else
                    {
                        Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );
                    }
                });
        }

        void FamilyInfoToUI( )
        {
            FamilyManagerApi.SortFamilyMembers( Family.FamilyMembers );

            // populate the address and dynamic fields
            FamilyName.SetCurrentValue( Family.Name );

            // address (or blank if thye don't have one)
            if ( Family.HomeLocation != null )
            {
                Street.Text = Family.HomeLocation.Street1;
                City.Text = Family.HomeLocation.City;
                State.Text = Family.HomeLocation.State;
                PostalCode.Text = Family.HomeLocation.PostalCode;
            }

            // campus...
            string campusName = Config.Instance.Campuses[ Config.Instance.SelectedCampusIndex ].Name;
            if ( FamilyGroupObject.CampusId.HasValue )
            {
                Rock.Client.Campus campus = Config.Instance.Campuses.Where( c => c.Id == FamilyGroupObject.CampusId.Value ).SingleOrDefault( );
                if ( campus != null )
                {
                    campusName = campus.Name;
                }
            }
            FamilyCampus.SetCurrentValue( campusName );


            // and dynamic info
            FamilyManager.UI.Dynamic_UIFactory.AttributesToUI( FamilyGroupObject.AttributeValues, Dynamic_FamilyControls );


            // finally, reload the table

            // if we haven't yet created our table source, do so now.
            if ( TableView.Source == null )
            {
                TableView.Source = new TableSource( this );
            }
            ((TableSource)TableView.Source).PrimaryCellDirty = true;
            TableView.ReloadData( );
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            KeyboardAdjustManager.Deactivate( );
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            nfloat leftPaneWidth = View.Bounds.Width * .48f;
            ScrollView.Bounds = new CGRect( 0, 0, leftPaneWidth - 25, View.Bounds.Height );

            FamilyName.ViewDidLayoutSubviews( new CGRect( View.Bounds.Left, View.Bounds.Top, leftPaneWidth - 45, View.Bounds.Height ) );

            FamilyName.SetPosition( new CGPoint( 10, 25 ) );
                

            // setup the position of all controls


            // Set the address
            nfloat addressAllowedWidth = leftPaneWidth - 25 - 20;
            AddressHeader.Layer.Position = new CGPoint( 10, FamilyName.Frame.Bottom + 50 );

            // first set the street
            Street.Bounds = new CGRect( 0, 0, addressAllowedWidth, 0 );
            Street.SizeToFit( );
            Street.Frame = new CGRect( 10, AddressHeader.Frame.Bottom, addressAllowedWidth, Street.Frame.Height );

            // the city, state and postal should go below
            nfloat cityWidth = (addressAllowedWidth / 2);
            City.Bounds = new CGRect( 0, 0, cityWidth, 0 );
            City.SizeToFit( );
            City.Frame = new CGRect( 10, Street.Frame.Bottom + 10, cityWidth, City.Frame.Height );

            nfloat stateWidth = ( addressAllowedWidth / 6 );
            State.Bounds = new CGRect( 0, 0, stateWidth, 0 );
            State.SizeToFit( );
            State.Frame = new CGRect( City.Frame.Right + 10, Street.Frame.Bottom + 10, stateWidth, State.Frame.Height );

            nfloat postalWidth = ( addressAllowedWidth / 3 ) - 20;
            PostalCode.Bounds = new CGRect( 0, 0, postalWidth, 0 );
            PostalCode.SizeToFit( );
            PostalCode.Frame = new CGRect( State.Frame.Right + 10, Street.Frame.Bottom + 10, postalWidth, PostalCode.Frame.Height );


            // set the campus selector
            FamilyCampus.ViewDidLayoutSubviews( new CGRect( View.Bounds.Left, View.Bounds.Top, leftPaneWidth, View.Bounds.Height ) );
            FamilyCampus.SetPosition( new CGPoint( 10, PostalCode.Frame.Bottom + 50 ) );


            // now procedurally add all the family attributes
            nfloat controlYPos = FamilyCampus.Frame.Bottom + 50;

            foreach ( IDynamic_UIView dynamicView in Dynamic_FamilyControls )
            {
                dynamicView.ViewDidLayoutSubviews( new CGRect( View.Bounds.Left, View.Bounds.Top, leftPaneWidth, View.Bounds.Height ) );
                
                dynamicView.SetPosition( new CGPoint( 10, controlYPos ) );

                controlYPos = dynamicView.GetFrame( ).Bottom + 50;
            }

            SaveButton.Layer.Position = new CGPoint( 10, controlYPos );

            // always allow scrolling to just below the save button
            ScrollView.ContentSize = new CGSize( ScrollView.ContentSize.Width, SaveButton.Frame.Bottom + 50 );

            // setup the right hand table
            TableView.Frame = new CGRect( leftPaneWidth, 0, View.Bounds.Width - leftPaneWidth, View.Bounds.Height );

            BlockerView.SetBounds( View.Bounds.ToRectF( ) );
        }

        public void HandleEditFamilyMember( Rock.Client.GroupMember familyMember, NSData imageBuffer )
        {
            BlockerView.Show( delegate
                {
                    // get their attributes before presenting.
                    ApplicationApi.GetPersonById( familyMember.Person.Id, true, 
                        delegate(System.Net.HttpStatusCode statusCode, string statusDescription, Rock.Client.Person refreshedPerson )
                        {
                            BlockerView.Hide( delegate
                                {
                                    // release our keyboard manager so IT can take over.
                                    KeyboardAdjustManager.Deactivate( );

                                    // use the REFRESHED PERSON'S attributes, but the ORIGINAL person's object data.
                                    bool isChild = familyMember.GroupRoleId == Config.Instance.FamilyMemberChildGroupRole.Id ? true : false;

                                    // present the new view controller, and implement an anon-delegate to handle the resposne
                                    PersonInfoViewController.PresentAnimated( Family.Id, Family.Name, familyMember.Person, isChild, refreshedPerson.AttributeValues, imageBuffer,
                                        delegate(bool didSave, int workingFamilyId, bool personIsChild, object context) 
                                        {
                                            // re-activate our keyboard manager
                                            KeyboardAdjustManager.Activate( );

                                            // only refresh if they actually made a change to something.
                                            if ( didSave == true )
                                            {
                                                // update the child / adult status
                                                int adultChildMemberRoleId = personIsChild == true ? Config.Instance.FamilyMemberChildGroupRole.Id : Config.Instance.FamilyMemberAdultGroupRole.Id;
                                                FamilyManagerApi.UpdatePersonRoleInFamily( familyMember, adultChildMemberRoleId, 
                                                    delegate(System.Net.HttpStatusCode updateRoleCode, string updateRoleDescription )
                                                    {
                                                        // whether it succeeded or not, go ahead and refresh the family
                                                        RefreshFamily( );
                                                    });
                                            }
                                        });
                                });
                        } );
                } );
        }

        public void HandleAddGuestFamily( )
        {
            // if the family ID is 0, it hasn't been saved, and therefore has no people in it. Don't allow this.
            if ( Family.Id == 0 )
            {
                Rock.Mobile.Util.Debug.DisplayError( Strings.FamilyInfo_Header_NoMembers, Strings.FamilyInfo_Body_NoMembers );
            }
            else
            {
                // release our keyboard manager so IT can take over.
                KeyboardAdjustManager.Deactivate( );

                // create the view controller
                AddPersonViewController.PresentAnimated( Family.Id, false, OnAddGuestFamilyComplete );
            }
        }

        void OnAddGuestFamilyComplete( bool didSave, int workingFamilyId, object context )
        {
            KeyboardAdjustManager.Activate( );

            // only refresh if they actually made a change to something.
            if ( didSave == true )
            {
                BlockerView.Show( delegate
                    {
                        Rock.Client.Family newGuestFamily = (Rock.Client.Family)context;

                        // for a guest family, use the first CHILD in the family. The user can
                        // make appropriate adjustments

                        // default to the first PERSON, in case we can't find any children
                        int? guestPrimaryValueId = newGuestFamily.FamilyMembers[ 0 ].Person.PrimaryAliasId;

                        foreach( Rock.Client.GroupMember guestMember in newGuestFamily.FamilyMembers )
                        {
                            if( guestMember.GroupRole.Id == Config.Instance.FamilyMemberChildGroupRole.Id )
                            {
                                guestPrimaryValueId = guestMember.Person.PrimaryAliasId;
                                break;
                            }
                        }

                        FamilyManagerApi.UpdateKnownRelationship( Family.FamilyMembers[ 0 ].Person.PrimaryAliasId, guestPrimaryValueId, Config.Instance.CanCheckInGroupRole.Id, 
                            delegate(HttpStatusCode statusCode, string statusDescription )
                            {
                                if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                                {
                                    RefreshFamily( );
                                }
                                else
                                {
                                    Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );
                                }
                            } );
                    } );
            }
        }

        public void HandleAddNewPerson( int workingFamilyId, string familyLastName )
        {
            // release our keyboard manager so IT can take over.
            KeyboardAdjustManager.Deactivate( );

            // if a family name was provided, 
            if ( familyLastName != null )
            {
                // strip off any " Family" that might be on the family's last name
                if ( familyLastName.EndsWith( FamilySuffix ) )
                {
                    familyLastName = familyLastName.Substring( 0, familyLastName.Length - FamilySuffix.Length );
                }
            }
            else
            {
                // otherwise, no family name, so use the FamilyName field.
                // note we cannot JUST do this, because if they're adding someone to a GUEST family,
                // we don't want to use the FamilyName field.
                familyLastName = FamilyName.GetCurrentValue( );
            }

            PersonInfoViewController.PresentAnimated( workingFamilyId, familyLastName, null, false, null, null, OnNewPersonComplete );
        }

        public delegate void OnPersonInfoCompleteDelegate( bool didSave, int workingFamilyId, bool isChild, object context );
        void OnNewPersonComplete( bool didSave, int workingFamilyId, bool isChild, object context )
        {
            KeyboardAdjustManager.Activate( );

            // did they make a change?
            if ( didSave == true )
            {
                BlockerView.Show( delegate
                    {
                        // Get the person that was just created.
                        ApplicationApi.GetPersonByGuid( ( (Rock.Client.Person)context ).Guid, 
                            delegate(HttpStatusCode statusCode, string statusDescription, Rock.Client.Person newPerson )
                            {
                                if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                                {
                                    // if our family ID is 0, we're supposed to be creating a new family
                                    if ( workingFamilyId == 0 )
                                    {
                                        // we know they were given a family when they were created. So get that family,
                                        // and we'll make that the family here that we're editing.
                                        ApplicationApi.GetFamiliesOfPerson( newPerson, 
                                            delegate(System.Net.HttpStatusCode familyCode, string familyDescription, List<Rock.Client.Group> familyList )
                                            {
                                                // we expect there to be exactly ONE family, and it will be the one that was just created.
                                                if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) && familyList != null && familyList.Count == 1 )
                                                {
                                                    // take this as our family group object
                                                    FamilyGroupObject = familyList[ 0 ];

                                                    // now that they're in a family, we need to set their role appropriately (child or adult)
                                                    int adultChildMemberRoleId = isChild == true ? Config.Instance.FamilyMemberChildGroupRole.Id : Config.Instance.FamilyMemberAdultGroupRole.Id;
                                                    FamilyManagerApi.UpdatePersonRoleInFamily( FamilyGroupObject.Members.ToList( )[ 0 ], adultChildMemberRoleId, delegate(System.Net.HttpStatusCode updateRoleCode, string updateRoleDescription )
                                                        {
                                                            // if updating their role worked, continue!
                                                            if( Rock.Mobile.Network.Util.StatusInSuccessRange( updateRoleCode ) )
                                                            {
                                                                // copy over all the filled in info so we can update this newly created family.
                                                                UIToFamilyInfo( );

                                                                // and immediately submit it. This will ensure we sync all the info they've typed 
                                                                // up to Rock!
                                                                FamilyManagerApi.UpdateFullFamily( FamilyGroupObject, FamilyAddress, PendingAttribChanges, delegate(System.Net.HttpStatusCode updateFamilyCode, string updateFamilyDescription )
                                                                    {
                                                                        if ( Rock.Mobile.Network.Util.StatusInSuccessRange( updateFamilyCode ) )
                                                                        {
                                                                            // setup a family object that can be refreshed.
                                                                            Family = new Rock.Client.Family();
                                                                            Family.Id = FamilyGroupObject.Id;

                                                                            // go ahead and refresh the family
                                                                            RefreshFamily( );
                                                                        }
                                                                        else
                                                                        {
                                                                            // couldnt move them to the current family.
                                                                            BlockerView.Hide( delegate
                                                                                {
                                                                                    Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );
                                                                                } );
                                                                        }   
                                                                    } );
                                                            }
                                                            else
                                                            {
                                                                // couldnt move them to the current family.
                                                                BlockerView.Hide( delegate
                                                                    {
                                                                        Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );
                                                                    } );
                                                            }
                                                        });
                                                }
                                                else
                                                {
                                                    // couldnt move them to the current family.
                                                    BlockerView.Hide( delegate
                                                        {
                                                            Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );
                                                        } );
                                                }
                                            } );
                                    }
                                    else
                                    {
                                        // new person to existing family is easy. Simply add them to the working family,
                                        // and request that they are removed from any families they're already in (which will be the
                                        // one new one created by Rock)
                                        int adultChildMemberRoleId = isChild == true ? Config.Instance.FamilyMemberChildGroupRole.Id : Config.Instance.FamilyMemberAdultGroupRole.Id;
                                        FamilyManagerApi.AddPersonToFamily( newPerson, adultChildMemberRoleId, workingFamilyId, true, delegate(System.Net.HttpStatusCode addToFamilyCode, string addToFamilyDescription )
                                            {
                                                if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                                                {
                                                    // update their status in this family (child / adult)
                                                    
                                                    // go ahead and refresh the family
                                                    RefreshFamily( );
                                                }
                                                else
                                                {
                                                    // couldnt move them to the current family.
                                                    BlockerView.Hide( delegate { Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );  } );
                                                }
                                            } );
                                    }
                                }
                            } );
                    } );
            }
        }

        public void HandleAddExistingPerson( int workingFamilyId )
        {
            KeyboardAdjustManager.Deactivate( );
            
            // create the view controller
            AddPersonViewController.PresentAnimated( workingFamilyId, true, OnBrowsePeopleComplete );
        }

        public delegate void OnBrowsePeopleCompleteDelegate( bool didSave, int workingFamilyId, object context );
        void OnBrowsePeopleComplete( bool didSave, int workingFamilyId, object context )
        {
            KeyboardAdjustManager.Activate( );

            if ( didSave == true )
            {
                BlockerView.Show( delegate
                    {
                        // ADD EXISTING PEOPLE TO NEW FAMILY
                        if ( workingFamilyId == 0 )
                        {    
                            AddPeopleToNewFamily( (List<Rock.Client.GroupMember>)context, AddPersonViewController.RemoveFromOtherFamilies );
                        }
                        // ADD EXISTING PEOPLE TO PRIMARY OR GUEST FAMILY
                        else
                        {
                            // setup our lists. personList defines the people to add.
                            // removalList defines people that are already IN the target family, and should
                            // therefore be removed from personList.
                            List<Rock.Client.GroupMember> familyMemberList = (List<Rock.Client.GroupMember>) context;
                            List<Rock.Client.GroupMember> removalList = new List<Rock.Client.GroupMember>( );


                            // we want to prevent a person from being added to a family they're already in.
                            // So are we adding to the primary family, or a guest family?

                            // if primary family
                            if( workingFamilyId == Family.Id )
                            {
                                // find any people who are already in the PRIMARY family
                                foreach( Rock.Client.GroupMember familyMember in familyMemberList )
                                {
                                    // if the person's ID is a person in the FamilyMembers List
                                    if( Family.FamilyMembers.Where( fm => fm.Person.Id == familyMember.Person.Id ).SingleOrDefault( ) != null )
                                    {
                                        // they're in the family, so put them in the removal list
                                        removalList.Add( familyMember );
                                    }
                                }
                            }
                            else
                            {
                                // figure out which guest family we're adding people to.
                                Rock.Client.GuestFamily guestFamily = GuestFamilies.Where( gf => gf.Id == workingFamilyId ).SingleOrDefault( );

                                // dont check for failure. It MUST be found, because the workingFamilyId CAME from GuestFamilies.
                                // I'd rather crash.

                                // find any people who are already in the PRIMARY family
                                foreach( Rock.Client.GroupMember familyMember in familyMemberList )
                                {
                                    // if the person's ID is a person in the FamilyMembers List
                                    if( guestFamily.FamilyMembers.Where( fm => fm.Id == familyMember.Person.Id ).SingleOrDefault( ) != null )
                                    {
                                        // they're in the family, so put them in the removal list
                                        removalList.Add( familyMember );
                                    }
                                }
                            }


                            // now remove anyone in the removal list.
                            foreach( Rock.Client.GroupMember familyMember in removalList )
                            {
                                familyMemberList.Remove( familyMember );
                            }

                            // if there are now still people to add, do it.
                            if( familyMemberList.Count > 0 )
                            {
                                // since a family exists, go straight to adding the people
                                AddPeopleToFamily( familyMemberList, workingFamilyId, AddPersonViewController.RemoveFromOtherFamilies );
                            }
                            else
                            {
                                // otherwise just hide the blocker and call it good.
                                BlockerView.Hide( );
                            }
                        }
                    } );
            }
        }

        void AddPeopleToNewFamily( List<Rock.Client.GroupMember> familyMembers, bool removeFromOtherFamilies )
        {
            // first create a new family
            Rock.Client.Group familyGroup = new Rock.Client.Group();
            familyGroup.CampusId = 1;
            familyGroup.Name = FamilyName.GetCurrentValue( ).ToUpperWords( );

            FamilyManagerApi.CreateNewFamily( familyGroup, delegate(System.Net.HttpStatusCode statusCode, string statusDescription )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                    {
                        // now get that group
                        ApplicationApi.GetFamilyGroupModelByGuid( familyGroup.Guid, 
                            delegate(HttpStatusCode familyCode, string familyDescription, Rock.Client.Group model )
                            {
                                if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                                {
                                    // take this as our family group object
                                    FamilyGroupObject = model;
                                    UIToFamilyInfo( );

                                    // setup a family object that can be refreshed.
                                    Family = new Rock.Client.Family();
                                    Family.Id = FamilyGroupObject.Id;

                                    // now use the standard addPeopleToFamily
                                    AddPeopleToFamily( familyMembers, Family.Id, removeFromOtherFamilies );
                                }
                                // Error retrieving the newly created family
                                else
                                {
                                    BlockerView.Hide( delegate
                                        {
                                            Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );
                                        } );
                                }
                            } );
                    }
                    // Error creating the new family
                    else
                    {
                        BlockerView.Hide( delegate
                            {
                                Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );
                            } );
                    }
                } );
        }

        void AddPeopleToFamily( List<Rock.Client.GroupMember> familyMembers, int familyId, bool removeFromOtherFamilies )
        {
            // dump all these people in the family provided by familyID
            int personCount = 0;
            foreach ( Rock.Client.GroupMember familyMember in familyMembers )
            {
                FamilyManagerApi.AddPersonToFamily( familyMember.Person, familyMember.GroupRole.Id, familyId, removeFromOtherFamilies, 
                    delegate(System.Net.HttpStatusCode personCode, string personDescription )
                    {
                        personCount++;

                        if ( personCount == familyMembers.Count )
                        {
                            RefreshFamily( );
                        }
                    } );
            }
        }


        List<KeyValuePair<string, string>> PendingAttribChanges = null;
        void UIToFamilyInfo( )
        {
            // grab the family name (and ensure it ends with " Family"
            FamilyGroupObject.Name = FamilyName.GetCurrentValue( ).ToUpperWords( );
            if ( FamilyGroupObject.Name.EndsWith( FamilySuffix ) == false )
            {
                FamilyGroupObject.Name += FamilySuffix;
            }

            FamilyAddress = null;
            if ( string.IsNullOrEmpty( Street.Text ) == false )
            {
                FamilyAddress = RockActions.CreateHomeAddress( Street.Text.ToUpperWords( ), City.Text.ToUpperWords( ), State.Text.ToUpper( ), PostalCode.Text );
            }

            // set the family's campus ID (if a value is selected)
            Rock.Client.Campus campus = Config.Instance.Campuses.Where( c => c.Name == FamilyCampus.GetCurrentValue( ) ).SingleOrDefault( );
            if ( campus != null )
            {
                FamilyGroupObject.CampusId = campus.Id;
            }

            // get the dynamic info
            PendingAttribChanges = new List<KeyValuePair<string, string>>();
            FamilyManager.UI.Dynamic_UIFactory.UIToAttributes( Dynamic_FamilyControls, PendingAttribChanges );
        }

        void TrySubmitFamilyInfo( )
        {
            // if the family ID is 0, it hasn't been saved, and therefore has no people in it. Don't allow it.
            if ( Family.Id == 0 )
            {
                Rock.Mobile.Util.Debug.DisplayError( Strings.FamilyInfo_Header_NoMembers, Strings.FamilyInfo_Body_NoMembers );
            }
            else
            {
                // make sure all required fields are filled in
                if ( ValidateInfo( ) )
                {
                    // show a blocker
                    BlockerView.Show( 
                        delegate
                        {
                            // take the new data
                            UIToFamilyInfo( );

                            // attempt the update
                            FamilyManagerApi.UpdateFullFamily( FamilyGroupObject, FamilyAddress, PendingAttribChanges, delegate(HttpStatusCode statusCode, string statusDescription )
                                {
                                    BlockerView.Hide( delegate
                                        {
                                            // if it FAILED, notify the user
                                            if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == false )
                                            {
                                                Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );
                                            }
                                        } );
                                } );
                        } );
                }
            }
        }

        public bool ValidateInfo( )
        {
            // require a family name
            if ( string.IsNullOrEmpty( FamilyName.GetCurrentValue( ) ) )
            {
                return false;
            }

            // if street is valid, we're fine to submit, regardless of the other values.
            if ( string.IsNullOrEmpty( Street.Text ) == true )
            {
                // since street is blank, we need to ensure all others are too
                if ( string.IsNullOrEmpty( City.Text ) == false ||
                     string.IsNullOrEmpty( State.Text ) == false ||
                     string.IsNullOrEmpty( PostalCode.Text ) == false )
                {
                    return false;
                }
            }

            foreach ( IDynamic_UIView dynamicView in Dynamic_FamilyControls )
            {
                // if any of the required dynamic views are empty, return false
                if ( dynamicView.IsRequired( ) && string.IsNullOrEmpty( dynamicView.GetCurrentValue( ) ) )
                {
                    return false;
                }
            }

            return true;
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);

            FamilyName.ResignFirstResponder( );
            Street.ResignFirstResponder( );
            City.ResignFirstResponder( );
            State.ResignFirstResponder( );
            PostalCode.ResignFirstResponder( );

            foreach ( IDynamic_UIView dynamicView in Dynamic_FamilyControls )
            {
                dynamicView.ResignFirstResponder( );
            }
        }
    }
}
