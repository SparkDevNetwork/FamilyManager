using System;
using UIKit;
using CoreGraphics;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Foundation;
using Rock.Mobile.PlatformSpecific.Util;
using Rock.Mobile.Animation;
using Rock.Mobile.Util.Strings;
using Rock.Mobile.PlatformSpecific.iOS.UI;
using CoreAnimation;
using Customization;
using Rock.Mobile.PlatformSpecific.iOS.Graphics;
using Rock.Mobile.Util;

namespace FamilyManager
{
    namespace UI
    {
        public static class FamilySuffixManager
        {
            const string FamilySuffix = " Family";

            // returns true if the family name is either blank, or just "Family"
            public static bool FamilyNameBlankOrSuffix( string familyName )
            {
                // if it's null, then we're done.
                if ( familyName == null )
                {
                    return true;
                }
                else
                {
                    // otherwise see if there's a suffix. If so, remove it.
                    string strippedFamilyName = familyName;
                    if ( FamilyNameHasSuffix( strippedFamilyName ) )
                    {
                        strippedFamilyName = strippedFamilyName.Substring( 0, strippedFamilyName.Length - FamilySuffix.Length );
                    }

                    // now see if it's only whitespace. If it is, it was either blank or only a suffix.
                    if ( string.IsNullOrWhiteSpace( strippedFamilyName ) )
                    {
                        return true;
                    }

                    return false;
                }
            }

            public static string FamilyNameWithSuffix( string familyName )
            {
                // if they pass null, give them back just the suffix.
                if ( familyName == null )
                {
                    return FamilySuffix;
                }
                // otherwise handle normal checks
                else
                {
                    // returns a string guaranteed to have the family suffix
                    if ( FamilyNameHasSuffix( familyName ) == false )
                    {
                        familyName += FamilySuffix;
                    }

                    return familyName;
                }
            }

            // returns true if the family name is only the "Family" suffix.
            public static bool FamilyNameHasSuffix( string familyName )
            {
                // if it's null, no it doesnt.
                if ( familyName == null )
                {
                    return false;
                }
                else
                {
                    return familyName.EndsWith( FamilySuffix, StringComparison.OrdinalIgnoreCase );
                }
            }

            public static string FamilyNameNoSuffix( string familyName )
            {
                // if they gave us null, then return an empty family name to them.
                if ( familyName == null )
                {
                    return string.Empty;
                }
                else
                {
                    if ( FamilyNameHasSuffix( familyName ) )
                    {
                        return familyName.Substring( 0, familyName.Length - FamilySuffix.Length );
                    }

                    return familyName;
                }
            }
        }
        
        /// <summary>
        /// Definition for a cell that can be used to display family search results
        /// </summary>
        class FamilySearchResultView : UIView
        {
            public UIView Container { get; set; }

            public UILabel Title { get; set; }
            public UILabel AdultMembers { get; set; }
            public UILabel ChildMembers { get; set; }

            public UILabel Address1 { get; set; }
            public UILabel Address2 { get; set; }

            public FamilySearchResultView( ) : base( )
            {
                Layer.AnchorPoint = CGPoint.Empty;

                BackgroundColor = UIColor.Clear;

                Container = new UIView( );
                Container.Layer.AnchorPoint = CGPoint.Empty;
                Container.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.BackgroundColor );
                Container.Layer.CornerRadius = 4;
                AddSubview( Container );

                Title = new UILabel( );
                Title.Layer.AnchorPoint = CGPoint.Empty;
                Title.BackgroundColor = UIColor.Clear;
                Title.LineBreakMode = UILineBreakMode.TailTruncation;
                Title.TextColor = Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.TextColor );
                Title.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_BoldFont, Config.Instance.VisualSettings.LargeFontSize );
                Container.AddSubview( Title );

                AdultMembers = new UILabel( );
                AdultMembers.Layer.AnchorPoint = CGPoint.Empty;
                AdultMembers.BackgroundColor = UIColor.Clear;
                AdultMembers.TextColor = Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.TextColor );
                AdultMembers.LineBreakMode = UILineBreakMode.TailTruncation;
                AdultMembers.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
                Container.AddSubview( AdultMembers );

                ChildMembers = new UILabel( );
                ChildMembers.Layer.AnchorPoint = CGPoint.Empty;
                ChildMembers.BackgroundColor = UIColor.Clear;
                ChildMembers.TextColor = Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.TextColor );
                ChildMembers.LineBreakMode = UILineBreakMode.TailTruncation;
                ChildMembers.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
                Container.AddSubview( ChildMembers );

                Address1 = new UILabel( );
                Address1.Layer.AnchorPoint = CGPoint.Empty;
                Address1.BackgroundColor = UIColor.Clear;
                Address1.TextColor = Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.TextColor );
                Address1.LineBreakMode = UILineBreakMode.TailTruncation;
                Address1.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
                Container.AddSubview( Address1 );

                Address2 = new UILabel( );
                Address2.Layer.AnchorPoint = CGPoint.Empty;
                Address2.TextColor = Theme.GetColor( Config.Instance.VisualSettings.SearchResultStyle.TextColor );
                Address2.BackgroundColor = UIColor.Clear;
                Address2.LineBreakMode = UILineBreakMode.TailTruncation;
                Address2.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
                Container.AddSubview( Address2 );
            }

            /// <summary>
            /// Use if you want the standard formatting for a family.
            /// </summary>
            public void FormatCell( nfloat cellWidth, Rock.Client.Family family )
            {
                // set the title (their last name)
                string title = family.Name;

                // create and add the person entry. First see if they're an adult or child
                string adultMembersText = string.Empty;
                string childMembersText = string.Empty;
                if ( family.FamilyMembers.Count > 0 )
                {
                    // first do adults
                    adultMembersText = Strings.General_Adults + ": ";
                    adultMembersText += GetMembersOfTypeString( family.FamilyMembers, Config.Instance.FamilyMemberAdultGroupRole.Id );

                    // now add kids
                    childMembersText = Strings.General_Children + ": ";
                    childMembersText += GetMembersOfTypeString( family.FamilyMembers, Config.Instance.FamilyMemberChildGroupRole.Id );
                }

                // Create the first address line
                string address1Text = Strings.General_NoAddress;
                string address2Text = string.Empty;

                if ( family.HomeLocation != null )
                {
                    address1Text = family.HomeLocation.Street1;

                    // make sure the remainder exists
                    if ( string.IsNullOrWhiteSpace( family.HomeLocation.City ) == false &&
                         string.IsNullOrWhiteSpace( family.HomeLocation.State ) == false &&
                         string.IsNullOrWhiteSpace( family.HomeLocation.PostalCode ) == false )
                    {
                        address2Text = family.HomeLocation.City + ", " +
                                       family.HomeLocation.State + " " +
                                       family.HomeLocation.PostalCode;
                    }
                }

                // now use our "other" formatting function to lay out the properties the way we want.
                FormatCell( cellWidth, title, adultMembersText, childMembersText, address1Text, address2Text );
            }

            /// <summary>
            /// Use if you want to display custom info (like maybe use it for a No Search Results) entry.
            /// </summary>
            public void FormatCell( nfloat cellWidth, string title, string adultMembers, string childMembers, string address1, string address2 )
            {
                nfloat containerWidth = cellWidth - 20;

                Container.Hidden = false;

                // there are no results, so put that in one cell.
                Title.Text = title;
                Title.SizeToFit( );
                Title.Frame = new CGRect( 10, 10, containerWidth, Title.Frame.Height );

                AdultMembers.Text = adultMembers;
                AdultMembers.SizeToFit( );
                AdultMembers.Frame = new CGRect( 10, Title.Frame.Bottom, containerWidth, AdultMembers.Frame.Height );

                ChildMembers.Text = childMembers;
                ChildMembers.SizeToFit( );
                ChildMembers.Frame = new CGRect( 10, AdultMembers.Frame.Bottom, containerWidth, ChildMembers.Frame.Height );

                Address1.Text = address1;
                Address1.SizeToFit( );
                Address1.Frame = new CGRect( 10, ChildMembers.Frame.Bottom + 10, containerWidth, Address1.Frame.Height );

                Address2.Text = address2;
                Address2.SizeToFit( );
                Address2.Frame = new CGRect( 10, Address1.Frame.Bottom, containerWidth, Address2.Frame.Height );

                // set the container height
                Container.Bounds = new CGRect( 0, 0, cellWidth, Address2.Frame.Bottom + 10 );

                Bounds = new CGRect( 0, 0, cellWidth, Container.Bounds.Height * 1.10f );
                Container.Layer.Position = new CGPoint( 0, ( Bounds.Height - Container.Bounds.Height ) / 2 );
            }

            public string GetMembersOfTypeString( List<Rock.Client.GroupMember> familyMembers, int adultChildId )
            {
                // build and return the string to use either adult or child members
                string membersText = string.Empty;

                bool hasMemberType = false;
                for ( int i = 0; i < familyMembers.Count; i++ )
                {
                    // add a comma seperator and then their name
                    if ( familyMembers[ i ].GroupRoleId == adultChildId )
                    {
                        hasMemberType = true;

                        // add their name
                        membersText += familyMembers[ i ].Person.NickName;

                        // can we add their age?
                        if ( familyMembers[ i ].Person.BirthDate.HasValue )
                        {
                            membersText += string.Format( " ({0})", familyMembers[ i ].Person.BirthDate.Value.AsAge( ) );
                        }

                        membersText += ", ";
                    }
                }

                // if there were adults
                if ( hasMemberType )
                {
                    // remove the trailing comma
                    membersText = membersText.Remove( membersText.Length - 2, 2 );
                }
                else
                {
                    // otherwise, just clear the text
                    membersText = string.Empty;
                }

                return membersText;
            }
        }
        
        public class RequiredAnchor : UIView 
        {
            public UIView Target { get; protected set; }

            public RequiredAnchor( ) : base( )
            {
                Layer.Opacity = .50f;
                Layer.AnchorPoint = CGPoint.Empty;
                BackgroundColor = UIColor.Red;
            }

            public void AttachToTarget( UIView target )
            {
                Target = target;
                Target.AddSubview( this );
            }

            public void SyncToTarget( )
            {
                nfloat area = Target.Frame.Height * .25f;

                Bounds = new CGRect( 0, 0, area, area );
                Layer.CornerRadius = area / 2;

                // position ourselves next to our target (doing this, along with an anchorpoint of 0 above, prevents the bottom two pixels from being clipped)
                Layer.Position = new CGPoint( Target.Frame.Width + 2, 5 );
                //Frame = new CGRect( Target.Frame.Width + 2, (( Target.Frame.Height - Frame.Height ) / 2) - 2, Frame.Width, Frame.Height );
            }
        }

        /// <summary>
        /// Manages creating the appropriate UI Control based on the provided control attribute object.
        /// (Which basically defines a control via json) 
        /// </summary>
        public static class Dynamic_UIFactory
        {
            public static void AttributesToUI( Dictionary<string, Rock.Client.AttributeValue> attributeValues, List<IDynamic_UIView> uiControls )
            {
                // obviously if there ARE no values, don't do anything.
                if ( attributeValues != null )
                {
                    // set attribute values
                    foreach ( Rock.Client.Attribute attrib in Config.Instance.PersonAttributeDefines )
                    {
                        // find this attribute in the person
                        KeyValuePair<string, Rock.Client.AttributeValue> attribValue = attributeValues.Where( av => av.Key == attrib.Key ).SingleOrDefault( );
                        if ( attribValue.Value != null )
                        {
                            // now find the matching UI control and set it.
                            IDynamic_UIView dynamicView = uiControls.Where( uic => uic.GetAssociatedAttributeKey( ) == attribValue.Key ).SingleOrDefault( );

                            // if it's valid, set it
                            if ( dynamicView != null )
                            {
                                dynamicView.SetCurrentValue( attribValue.Value.Value );
                            }
                        }
                    }
                }
            }

            public static void UIToAttributes( List<IDynamic_UIView> uiControls, List<KeyValuePair<string, string>> attribValues )
            {
                // GET attribute values
                foreach ( IDynamic_UIView uiControl in uiControls )
                {
                    string uiValue = uiControl.GetCurrentValue( );

                    // create and add the attrib value
                    attribValues.Add( new KeyValuePair<string, string>( uiControl.GetAssociatedAttributeKey( ), uiValue ) );
                }
            }
            
            public static IDynamic_UIView CreateDynamic_UIControl( UIViewController parentViewController, UIView parentView, Rock.Client.Attribute uiControlAttrib, bool required, string attribKey )
            {
                IDynamic_UIView createdControl = null;
                 
                // first, get the field type
                switch ( uiControlAttrib.FieldType.Guid.ToString( ) )
                {
                    // Single-Select
                    case "7525c4cb-ee6b-41d4-9b64-a08048d5a5c0":
                    {
                        // now what KIND of single select is it? Find the attributeQualifier defining the field type
                        List<Rock.Client.AttributeQualifier> qualifers = uiControlAttrib.AttributeQualifiers.Cast<Rock.Client.AttributeQualifier>( ) as List<Rock.Client.AttributeQualifier>;
                        Rock.Client.AttributeQualifier fieldTypeQualifer = qualifers.Where( aq => aq.Key.ToLower( ) == "fieldtype" ).SingleOrDefault( );

                        // now get the values that the control will use.
                        Rock.Client.AttributeQualifier valuesQualifer = qualifers.Where( aq => aq.Key.ToLower( ) == "values" ).SingleOrDefault( );

                        // check for the known supported types
                        // Drop-Down List
                        if ( fieldTypeQualifer.Guid.ToString( ) == "62a411f9-5cec-492d-9bab-98f23b4df44c" )
                        {
                            // create a drop down
                            createdControl = new Dynamic_UIDropDown( parentViewController, parentView, uiControlAttrib, valuesQualifer, required, attribKey );
                        }
                        else if ( fieldTypeQualifer.Guid.ToString( ) == "" )
                        {
                            //todo: create a radio button
                        }
                        else
                        {
                            // here, show the value as WELL as the guid
                            Rock.Mobile.Util.Debug.WriteLine( string.Format( "Unknown single-select control type specified: {0}, Guid: {1}", fieldTypeQualifer.Value, fieldTypeQualifer.Guid ) );
                        }
                        break;
                    }

                    // Date Picker
                    case "6b6aa175-4758-453f-8d83-fcd8044b5f36":
                    {
                        createdControl = new Dynamic_UIDatePicker( parentViewController, uiControlAttrib.Name, required, attribKey );
                        break;
                    }

                    // Check Box (A Switch on iOS)
                    case "1edafded-dfe6-4334-b019-6eecba89e05a":
                    {
                        // now what KIND of single select is it? Find the attributeQualifier defining the field type
                        List<Rock.Client.AttributeQualifier> qualifers = uiControlAttrib.AttributeQualifiers.Cast<Rock.Client.AttributeQualifier>( ) as List<Rock.Client.AttributeQualifier>;

                        // now get the values that the control will use.
                        Rock.Client.AttributeQualifier falseQualifer = qualifers.Where( aq => aq.Key.ToLower( ) == "falsetext" ).SingleOrDefault( );
                        Rock.Client.AttributeQualifier trueQualifer = qualifers.Where( aq => aq.Key.ToLower( ) == "truetext" ).SingleOrDefault( );

                        //createdControl = new Dynamic_UISwitch( parentViewController, parentView, uiControlAttrib, falseQualifer.Value, trueQualifer.Value, required );
                        createdControl = new Dynamic_UIToggle( parentViewController, parentView, uiControlAttrib, falseQualifer.Value, trueQualifer.Value, required, attribKey );
                        break;
                    }

                    // Integer Input Field
                    case "a75dfc58-7a1b-4799-bf31-451b2bbe38ff":
                    {
                        createdControl = new Dynamic_UITextField( parentViewController, parentView, uiControlAttrib, true, required, attribKey );
                        break;
                    }

                    // Text Field
                    case "9c204cd0-1233-41c5-818a-c5da439445aa":
                    {
                        createdControl = new Dynamic_UITextField( parentViewController, parentView, uiControlAttrib, false, required, attribKey );
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }

                return createdControl;
            }
        }

        /// <summary>
        /// Interface that allows the app to manage these dynamic UI controls in an abstracted way.
        /// </summary>
        public interface IDynamic_UIView
        {
            /// <summary>
            /// Determines whether this UI value is required when submitting.
            /// </summary>
            bool IsRequired( );

            /// <summary>
            /// Returns the current value input / selected by the control
            /// </summary>
            string GetCurrentValue( );

            /// <summary>
            /// Sets the current value
            /// </summary>
            /// <param name="value">Value.</param>
            void SetCurrentValue( string value );

            /// <summary>
            /// Adds this UI View as child of parent.
            /// </summary>
            void AddToView( UIView parent );

            /// <summary>
            /// Removes the UI View from its parent.
            /// </summary>
            void RemoveFromView( );

            /// <summary>
            /// Positions the view.
            /// </summary>
            void SetPosition( CGPoint position );

            /// <summary>
            /// Returns the bounding frame for the control (same as View.Frame, except
            /// allows it from the interface)
            /// </summary>
            CGRect GetFrame( );

            /// <summary>
            /// Calls ResignFirstResponder on the control
            /// </summary>
            bool ResignFirstResponder( );

            void ShouldAdjustForKeyboard( bool shouldAdjust );

            /// <summary>
            /// Gives the control a chance to layout everything correctly
            /// </summary>
            void ViewDidLayoutSubviews( CGRect parentBounds );

            /// <summary>
            /// Returns the attribute key for the Attribute this UI Control manipulates.
            /// </summary>
            /// <returns>The associated attribute key.</returns>
            string GetAssociatedAttributeKey( );
        }

        public class Dynamic_UIToggle : UIView, IDynamic_UIView
        {
            UILabel Header { get; set; }

            UIToggle ToggleValue { get; set; }

            RequiredAnchor RequiredAnchor { get; set; }
            bool Required { get; set; }

            UIViewController ParentViewController { get; set; }
            UIView ParentView { get; set; }

            string FalseText { get; set; }

            string TrueText { get; set; }

            string AttribKey { get; set; }

            /// <summary>
            /// Call this constructor to create the control via code
            /// </summary>
            public Dynamic_UIToggle( UIViewController parentViewController, UIView parentView, string headerText, string falseText, string trueText, bool requiredField )
            {
                Create( parentViewController, parentView, headerText, falseText, trueText, requiredField, null );
            }

            /// <summary>
            /// Call this constructor to create the control via JSON
            /// </summary>
            public Dynamic_UIToggle( UIViewController parentViewController, UIView parentView, Rock.Client.Attribute uiControlAttrib, string falseText, string trueText, bool requiredField, string attribKey )
            {
                Create( parentViewController, parentView, uiControlAttrib.Name, falseText, trueText, requiredField, attribKey );
            }

            void Create( UIViewController parentViewController, UIView parentView, string headerText, string falseText, string trueText, bool requiredField, string attribKey )
            {
                AttribKey = attribKey;

                // store our parent and the required flag
                ParentViewController = parentViewController;
                ParentView = parentView;

                Required = requiredField;

                // populate our values
                FalseText = falseText;

                TrueText = trueText;


                // setup the controls
                Layer.AnchorPoint = CGPoint.Empty;

                // header label
                Header = new UILabel( );
                Header.Layer.AnchorPoint = CGPoint.Empty;
                Header.Text = headerText;
                Header.Font = FontManager.GetFont( Settings.General_BoldFont, Config.Instance.VisualSettings.SmallFontSize );
                Theme.StyleLabel( Header, Config.Instance.VisualSettings.LabelStyle );
                AddSubview( Header );

                RequiredAnchor = new RequiredAnchor( );
                RequiredAnchor.AttachToTarget( Header );
                RequiredAnchor.SyncToTarget( );
                RequiredAnchor.Hidden = !requiredField;

                // setup the value and button
                ToggleValue = new UIToggle( falseText, trueText, null );
                ToggleValue.Layer.AnchorPoint = CGPoint.Empty;
                Theme.StyleToggle( ToggleValue, Config.Instance.VisualSettings.ToggleStyle );

                AddSubview( ToggleValue );
            }

            public string GetAssociatedAttributeKey( )
            {
                return AttribKey;
            }

            public void ShouldAdjustForKeyboard( bool shouldAdjust )
            {
                // nothing needs to happen
            }

            public void SetPosition( CGPoint position )
            {
                Layer.Position = position;
            }

            public void AddToView( UIView parent )
            {
                parent.AddSubview( this );
            }

            public void RemoveFromView( )
            {
                RemoveFromSuperview( );
            }

            public CGRect GetFrame( )
            {
                return Frame;
            }

            public bool IsRequired( )
            {
                return Required;
            }

            public string GetCurrentValue( )
            {
                switch ( ToggleValue.SideToggled )
                {
                    case UIToggle.Toggle.Left:
                    {
                        return FalseText;
                    }

                    case UIToggle.Toggle.Right:
                    {
                        return TrueText;
                    }

                    default:
                    {
                        return string.Empty;
                    }
                }
            }

            public void SetCurrentValue( string value )
            {
                if ( string.IsNullOrWhiteSpace( value ) )
                {
                    ToggleValue.ToggleSide( UIToggle.Toggle.None );
                }
                else if ( value == TrueText )
                {
                    ToggleValue.ToggleSide( UIToggle.Toggle.Right );
                }
                else
                {
                    ToggleValue.ToggleSide( UIToggle.Toggle.Left );
                }
            }

            public void ViewDidLayoutSubviews( CGRect parentBounds )
            {
                ToggleValue.SizeToFit( );
                ToggleValue.Layer.Position = new CGPoint( 0, Header.Frame.Bottom + 5 );

                // and wrap our controls
                Bounds = new CGRect( 0, 0, ToggleValue.Frame.Right, ToggleValue.Frame.Bottom );
            }
        }

        public class Dynamic_UIDatePicker : UIView, IDynamic_UIView
        {
            UILabel Header { get; set; }

            UIButton ButtonOverlay { get; set; }
            UILabel ValueLabel { get; set; }
            UILabel ValueSymbol { get; set; }
            UIButton ClearButton { get; set; }
            RequiredAnchor RequiredAnchor { get; set; }
            bool Required { get; set; }

            UIViewController ParentViewController { get; set; }
            UIView ParentView { get; set; }

            // "Contains" the date picker, and acts as its parent.
            // This allows us to block all other input until the user is done picking.
            UIButton ContainerButton { get; set; }

            static UIDatePicker DatePicker { get; set; }

            string AttribKey { get; set; }

            bool IsActive { get; set; }

            /// <summary>
            /// Construct for either json OR in code.
            /// </summary>
            public Dynamic_UIDatePicker( UIViewController parentViewController, string headerLabel, bool requiredField, string attribKey )
            {
                AttribKey = attribKey;

                if ( DatePicker == null )
                {
                    DatePicker = new UIDatePicker();
                    DatePicker.Mode = UIDatePickerMode.Date;
                    DatePicker.MinimumDate = new DateTime( 1900, 1, 1 ).DateTimeToNSDate( );
                    DatePicker.MaximumDate = DateTime.Now.DateTimeToNSDate( );
                }

                // store our parent and the required flag
                ParentViewController = parentViewController;

                Required = requiredField;

                // the date picker needs to attach itself to the aboslute root controller
                ParentView = UIApplication.SharedApplication.KeyWindow.RootViewController.View;

                // now create the container button that will act as the DatePicker's parent, and attach itself
                // to the Root Controller.
                ContainerButton = new UIButton( ParentView.Bounds );
                ContainerButton.BackgroundColor = UIColor.Clear;
                ContainerButton.TouchUpInside += (object sender, EventArgs e ) =>
                {
                        ToggleDatePickerVisible( false );
                };


                // setup the controls
                Layer.AnchorPoint = CGPoint.Empty;

                DatePicker.Layer.AnchorPoint = CGPoint.Empty;
                DatePicker.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.DatePickerStyle.BackgroundColor );
                DatePicker.SetValueForKey( Theme.GetColor( Config.Instance.VisualSettings.DatePickerStyle.TextColor ), new NSString( "textColor" ) );

                DatePicker.ValueChanged += (object sender, EventArgs e ) =>
                    {
                        if ( IsActive == true )
                        {
                            NSDate pickerDate = ((UIDatePicker) sender).Date;

                            UpdateDateLabel( pickerDate );
                        }
                    };
                
                // header label
                Header = new UILabel( );
                Header.Layer.AnchorPoint = CGPoint.Empty;
                Header.Text = headerLabel;
                Header.Font = FontManager.GetFont( Settings.General_BoldFont, Config.Instance.VisualSettings.SmallFontSize );
                Theme.StyleLabel( Header, Config.Instance.VisualSettings.LabelStyle );
                AddSubview( Header );

                RequiredAnchor = new RequiredAnchor( );
                RequiredAnchor.AttachToTarget( Header );
                RequiredAnchor.Hidden = !requiredField;
                    

                // setup the value and button
                ValueLabel = new UILabel( );
                ValueLabel.Layer.AnchorPoint = CGPoint.Empty;
                ValueLabel.Text = "";
                ValueLabel.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
                ValueLabel.SizeToFit( );
                AddSubview( ValueLabel );

                ValueSymbol = new UILabel( );
                AddSubview( ValueSymbol );
                ValueSymbol.Font = FontManager.GetFont( "Bh", 36 );


                ClearButton = new UIButton();
                ClearButton.Font = FontManager.GetFont( "Bh", 36 );
                AddSubview( ClearButton );
                ClearButton.TouchUpInside += (object sender, EventArgs e ) =>
                    {
                        ValueLabel.Text = string.Empty;
                        UpdateLayout( );
                    };

                Theme.StyleDatePicker( ValueLabel, ValueSymbol, ClearButton, Config.Instance.VisualSettings.DatePickerStyle );


                ButtonOverlay = new UIButton();
                ButtonOverlay.Layer.AnchorPoint = CGPoint.Empty;
                ButtonOverlay.Layer.Opacity = .50f;
                AddSubview( ButtonOverlay );

                // finally, setup the popup when the button is tapped
                ButtonOverlay.TouchUpInside += (object sender, EventArgs e ) =>
                    {
                        ParentView.BringSubviewToFront( DatePicker );

                        bool shouldDisplay = DatePicker.Superview == null ? true : false;

                        ToggleDatePickerVisible( shouldDisplay );
                    };
            }

            void UpdateDateLabel( NSDate pickerDate )
            {
                ValueLabel.Text = string.Format( "{0:MMMMM dd yyyy}", pickerDate.NSDateToDateTime( ) );

                // update the value field and button overlay
                UpdateLayout( );
            }

            void ToggleDatePickerVisible( bool shouldDisplay )
            {
                // we are HIDING
                nfloat targetYPos = 0;
                if( shouldDisplay == false )
                {
                    targetYPos = ParentView.Bounds.Height;
                }
                // we are UNHIDING
                else
                {
                    // setup the initial date time to display. (As a default value, use 1 year ago)
                    DateTime initialDate = DateTime.Now.AddYears( -1 );

                    // if there's already a value, use that.
                    if( string.IsNullOrWhiteSpace( ValueLabel.Text ) == false )
                    {
                        initialDate = DateTime.Parse( ValueLabel.Text );
                    }
                    DatePicker.Date = initialDate.DateTimeToNSDate( );

                    // make sure we update the label too
                    UpdateDateLabel( DatePicker.Date );

                    ParentView.AddSubview( ContainerButton );
                    ContainerButton.AddSubview( DatePicker );
                    targetYPos = ParentView.Bounds.Height - DatePicker.Bounds.Height;

                    IsActive = true;
                }

                SimpleAnimator_Float floatAnim = new SimpleAnimator_Float( (float)DatePicker.Layer.Position.Y, (float)targetYPos, .18f, 
                    delegate(float percent, object value) 
                    {
                        DatePicker.Layer.Position = new CGPoint( DatePicker.Layer.Position.X, (float)value );
                    }, delegate 
                    {
                        if( shouldDisplay == false )
                        {
                            IsActive = false;
                            DatePicker.RemoveFromSuperview( );
                            ContainerButton.RemoveFromSuperview( );
                        }
                    });

                floatAnim.Start( );
            }

            public string GetAssociatedAttributeKey( )
            {
                return AttribKey;
            }

            public override void TouchesEnded(NSSet touches, UIEvent evt)
            {
                base.TouchesEnded(touches, evt);

                ToggleDatePickerVisible( false );
            }

            public void SetPosition( CGPoint position )
            {
                Layer.Position = position;
            }

            public void AddToView( UIView parent )
            {
                parent.AddSubview( this );
            }

            public void RemoveFromView( )
            {
                RemoveFromSuperview( );
            }

            public void ShouldAdjustForKeyboard( bool shouldAdjust )
            {
                // nothing needs to happen
            }

            public CGRect GetFrame( )
            {
                return Frame;
            }

            public bool IsRequired( )
            {
                return Required;
            }

            public string GetCurrentValue( )
            {
                return ValueLabel.Text;
            }

            public void SetCurrentValue( string value )
            {
                // force correct formatting
                if ( string.IsNullOrWhiteSpace( value ) == false )
                {
                    DateTime dateTimeValue = DateTime.Parse( value );
                    ValueLabel.Text = string.Format( "{0:MMMMM dd yyyy}", dateTimeValue );
                }
                else
                {
                    ValueLabel.Text = string.Empty;
                }

                ToggleClearButton( );
            }

            public override bool ResignFirstResponder( )
            {
                ToggleDatePickerVisible( false );

                return base.ResignFirstResponder( );
            }

            public void ViewDidLayoutSubviews( CGRect parentBounds )
            {
                // only update the date picker here in LayoutSubviews.
                DatePicker.Bounds = new CGRect( 0, 0, ParentView.Bounds.Width, DatePicker.Bounds.Height );
                DatePicker.Layer.Position = new CGPoint( 0, ParentView.Bounds.Height );


                UpdateLayout( );
            }

            void UpdateSymbol( )
            {
                // if there's a value in the label...
                if ( string.IsNullOrWhiteSpace( ValueLabel.Text ) == false )
                {
                    // position the symbol at the end of the label, centered vertically
                    ValueSymbol.Layer.Position = new CGPoint( ValueLabel.Frame.Right + ( ValueSymbol.Bounds.Width / 2 ) + 5, ValueLabel.Frame.Top + ( ValueLabel.Bounds.Height / 2 ) );   
                }
                else
                {
                    // otherwise position it to look nice with no valueLabel text.
                    ValueSymbol.Layer.Position = new CGPoint( Header.Frame.Left + 10, Header.Frame.Bottom + 10 );
                }
            }

            void ToggleClearButton( )
            {
                if ( string.IsNullOrWhiteSpace( ValueLabel.Text ) == true )
                {
                    ClearButton.Enabled = false;
                    ClearButton.Hidden = true;
                }
                else
                {
                    ClearButton.Enabled = true;
                    ClearButton.Hidden = false;
                }
            }

            void UpdateLayout( )
            {
                RequiredAnchor.SyncToTarget( );

                ValueLabel.Layer.Position = new CGPoint( 0, Header.Frame.Bottom );
                ValueLabel.SizeToFit( );

                UpdateSymbol( );

                nfloat controlWidth = Math.Max( (float) Header.Frame.Right, (float) ValueSymbol.Frame.Right );
                nfloat controlHeight = Math.Max( (float)ValueLabel.Frame.Bottom, (float)ValueSymbol.Frame.Bottom );

                ButtonOverlay.Layer.Position = CGPoint.Empty;
                ButtonOverlay.Bounds = new CGRect( 0, 0, controlWidth, controlHeight );
                ButtonOverlay.Layer.ZPosition = 1;

                ClearButton.Layer.Position = new CGPoint( controlWidth + ( ClearButton.Bounds.Width / 2 ), ValueSymbol.Layer.Position.Y );

                ToggleClearButton( );

                controlWidth += ClearButton.Bounds.Width;

                // and wrap our controls
                Bounds = new CGRect( 0, 0, controlWidth, controlHeight );
            }
        }

        public class Dynamic_UIDropDown : UIView, IDynamic_UIView
        {
            UILabel Header { get; set; }

            UIButton ButtonOverlay { get; set; }
            UILabel ValueLabel { get; set; }
            UILabel ValueSymbol { get; set; }
            RequiredAnchor RequiredAnchor { get; set; }
            bool Required { get; set; }

            UIViewController ParentViewController { get; set; }
            UIView ParentView { get; set; }

            string AttribKey { get; set; }

            public string[] Values { get; protected set; }

            /// <summary>
            /// Call this constructor to create the control via code
            /// </summary>
            public Dynamic_UIDropDown( UIViewController parentViewController, UIView parentView, string name, string description, string[] values, bool requiredField )
            {
                Create( parentViewController, parentView, name, description, values, requiredField, "" );
            }

            /// <summary>
            /// Call this constructor to create the control via JSON
            /// </summary>
            public Dynamic_UIDropDown( UIViewController parentViewController, UIView parentView, Rock.Client.Attribute uiControlAttrib, Rock.Client.AttributeQualifier valueList, bool requiredField, string attribKey )
            {
                Create( parentViewController, parentView, uiControlAttrib.Name, uiControlAttrib.Description, valueList.Value.Split( ',' ), requiredField, attribKey );
            }

            public void Create( UIViewController parentViewController, UIView parentView, string name, string description, string[] values, bool requiredField, string attribKey )
            {
                AttribKey = attribKey;

                // store our parent and the required flag
                ParentView = parentView;
                ParentViewController = parentViewController;

                Required = requiredField;

                // populate our values list
                Values = values;


                // setup the controls
                Layer.AnchorPoint = CGPoint.Empty;

                // header label
                Header = new UILabel( );
                Header.Layer.AnchorPoint = CGPoint.Empty;
                Header.Text = name; //uiControlAttrib.Name;
                Header.Font = FontManager.GetFont( Settings.General_BoldFont, Config.Instance.VisualSettings.SmallFontSize );
                Theme.StyleLabel( Header, Config.Instance.VisualSettings.LabelStyle );
                AddSubview( Header );

                RequiredAnchor = new RequiredAnchor( );
                RequiredAnchor.AttachToTarget( Header );
                RequiredAnchor.Hidden = !requiredField;

                // setup the value and button
                ValueLabel = new UILabel( );
                ValueLabel.Layer.AnchorPoint = CGPoint.Empty;
                ValueLabel.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
                //ValueLabel.Text = Values[ 0 ];
                Theme.StyleLabel( ValueLabel, Config.Instance.VisualSettings.LabelStyle );
                AddSubview( ValueLabel );


                ValueSymbol = new UILabel( );
                AddSubview( ValueSymbol );
                ValueSymbol.Text = "";
                ValueSymbol.Font = FontManager.GetFont( "Bh", 36 );
                ValueSymbol.TextColor = Theme.GetColor( Config.Instance.VisualSettings.LabelStyle.TextColor );
                ValueSymbol.SizeToFit( );


                ButtonOverlay = new UIButton();
                ButtonOverlay.Layer.AnchorPoint = CGPoint.Empty;
                ButtonOverlay.BackgroundColor = UIColor.Clear;
                ButtonOverlay.Layer.Opacity = .50f;
                AddSubview( ButtonOverlay );

                // finally, setup the popup when the button is tapped
                ButtonOverlay.TouchUpInside += (object sender, EventArgs e ) =>
                    {
                        UIAlertController actionSheet = UIAlertController.Create( Header.Text, 
                            description, 
                            UIAlertControllerStyle.ActionSheet );

                        // the device is a tablet, anchor the menu
                        actionSheet.PopoverPresentationController.SourceView = ButtonOverlay;
                        actionSheet.PopoverPresentationController.SourceRect = ButtonOverlay.Bounds;

                        // for each campus, create an entry in the action sheet, and its callback will assign
                        // that campus index to the user's viewing preference
                        for( int i = 0; i < Values.Length; i++ )
                        {
                            UIAlertAction valueAction = UIAlertAction.Create( Values[ i ], UIAlertActionStyle.Default, delegate(UIAlertAction obj) 
                                {
                                    ValueLabel.Text = obj.Title;

                                    // update the value field and button overlay
                                    ValueLabel.SizeToFit( );
                                    ButtonOverlay.Bounds = ValueLabel.Bounds;
                                    UpdateLayout( );
                                } );

                            actionSheet.AddAction( valueAction );
                        }

                        // let them cancel, too
                        UIAlertAction cancelAction = UIAlertAction.Create( FamilyManager.Strings.General_Cancel, UIAlertActionStyle.Cancel, delegate { });
                        actionSheet.AddAction( cancelAction );

                        ParentViewController.PresentViewController( actionSheet, true, null );
                    };
            }

            public string GetAssociatedAttributeKey( )
            {
                return AttribKey;
            }

            public void SetPosition( CGPoint position )
            {
                Layer.Position = position;
            }

            public void AddToView( UIView parent )
            {
                parent.AddSubview( this );
            }

            public void RemoveFromView( )
            {
                RemoveFromSuperview( );
            }

            public CGRect GetFrame( )
            {
                return Frame;
            }

            public bool IsRequired( )
            {
                return Required;
            }

            public string GetCurrentValue( )
            {
                return ValueLabel.Text;
            }

            public void SetCurrentValue( string value )
            {
                ValueLabel.Text = value;
                UpdateLayout( );
            }

            public void ShouldAdjustForKeyboard( bool shouldAdjust )
            {
                // nothing needs to happen
            }

            public override bool ResignFirstResponder( )
            {
                return base.ResignFirstResponder( );
            }

            public void ViewDidLayoutSubviews( CGRect parentBounds )
            {
                UpdateLayout( );
            }

            void UpdateLayout( )
            {
                RequiredAnchor.SyncToTarget( );

                ValueLabel.Layer.Position = new CGPoint( 0, Header.Frame.Bottom );
                ValueLabel.SizeToFit( );

                // if the label has a string, position the symbol near it.
                if ( string.IsNullOrWhiteSpace( ValueLabel.Text ) == false )
                {
                    ValueSymbol.Layer.Position = new CGPoint( ValueLabel.Frame.Right + ( ValueSymbol.Bounds.Width / 2 ) + 5, ValueLabel.Frame.Top + ( ValueLabel.Bounds.Height / 2 ) );
                }
                else
                {
                    // otherwise position it to look nice with no valueLabel text.
                    ValueSymbol.Layer.Position = new CGPoint( Header.Frame.Left + 10, Header.Frame.Bottom + 10 );
                }


                nfloat controlWidth = Math.Max( (float) Header.Frame.Right, (float) ValueSymbol.Frame.Right );
                nfloat controlHeight = Math.Max( (float)ValueLabel.Frame.Bottom, (float)ValueSymbol.Frame.Bottom );

                ButtonOverlay.Layer.Position = CGPoint.Empty;
                ButtonOverlay.Bounds = new CGRect( 0, 0, controlWidth, controlHeight );

                // and wrap our controls
                Bounds = new CGRect( 0, 0, controlWidth, controlHeight );
            }
        }

        public class Dynamic_UITextField : UIView, IDynamic_UIView
        {
            // create a delegate that can send the keyboard adjust notifications.
            public class TextFieldDelegate : UITextFieldDelegate
            {
                Dynamic_UITextField Parent { get; set; }
                CGRect InitialTransformedFrame { get; set; }

                // create with the parent
                public TextFieldDelegate( Dynamic_UITextField parent ) : base( )
                {
                    Parent = parent;
                }

                public override bool ShouldBeginEditing(UITextField textField)
                {
                    // convert into absolute scroll position
                    CGRect transformedFrame = Parent.TextFieldPosLocalToScroll( textField ); 
                    InitialTransformedFrame = transformedFrame;

                    NSNotificationCenter.DefaultCenter.PostNotificationName( Rock.Mobile.PlatformSpecific.iOS.UI.KeyboardAdjustManager.TextControlDidBeginEditingNotification, NSValue.FromCGRect( transformedFrame ) );
                    return true;
                }
            }
            
            UILabel Header { get; set; }
            RequiredAnchor RequiredAnchor { get; set; }
            UIInsetTextField TextField { get; set; }
            bool Required { get; set; }
            bool NumbersOnly { get; set; }

            UIView ParentView { get; set; }
            UIViewController ParentViewController { get; set; }

            string AttribKey { get; set; }

            // transform the next field position to scroll space.
            public CGRect TextFieldPosLocalToScroll( UITextField textField )
            {
                // first transform to our superview
                CGRect textFieldFrame = ConvertRectToView( textField.Frame, null );

                // now see if it's a scroll view, and should consider scroll offset.
                UIScrollView parentScroll = GetParentScrollView( );
                if ( parentScroll != null )
                {
                    textFieldFrame = new CGRect( textFieldFrame.X, textFieldFrame.Y + parentScroll.ContentOffset.Y, textFieldFrame.Width, textFieldFrame.Height );
                }

                return textFieldFrame;
            }

            UIScrollView GetParentScrollView( )
            {
                // walk our hierarchy and find the scrollview (if any) that we're a child of.
                UIView parentView = ParentView;

                //
                while ( parentView != null )
                {
                    UIScrollView scrollView = parentView as UIScrollView;
                    if ( scrollView != null )
                    {
                        return scrollView;
                    }

                    parentView = parentView.Superview;
                }

                return null;
            }

            /// <summary>
            /// Call this constructor to create the control via JSON
            /// </summary>
            public Dynamic_UITextField( UIViewController parentViewController, UIView parentView, Rock.Client.Attribute uiControlAttrib, bool numbersOnly, bool requiredField, string attribKey )
            {
                Create( parentViewController, parentView, uiControlAttrib.Name, numbersOnly, requiredField, attribKey );
            }

            /// <summary>
            /// A constructor for creating a UI in code vs thru data
            /// </summary>
            public Dynamic_UITextField( UIViewController parentViewController, UIView parentView, string labelText, bool numbersOnly, bool requiredField ) : base( )
            {
                Create( parentViewController, parentView, labelText, numbersOnly, requiredField, "" );
            }

            void Create( UIViewController parentViewController, UIView parentView, string labelText, bool numbersOnly, bool requiredField, string attribKey )
            {
                AttribKey = attribKey;

                ParentViewController = parentViewController;
                ParentView = parentView;

                Required = requiredField;

                NumbersOnly = numbersOnly;

                Layer.AnchorPoint = CGPoint.Empty;

                Header = new UILabel( );
                Header.Layer.AnchorPoint = CGPoint.Empty;
                Header.Text = labelText;
                Theme.StyleLabel( Header, Config.Instance.VisualSettings.LabelStyle );
                Header.Font = FontManager.GetFont( Settings.General_BoldFont, Config.Instance.VisualSettings.SmallFontSize );
                Header.SizeToFit( );
                AddSubview( Header );

                RequiredAnchor = new RequiredAnchor( );
                RequiredAnchor.AttachToTarget( Header );
                RequiredAnchor.SyncToTarget( );
                RequiredAnchor.Hidden = !requiredField;

                TextField = new UIInsetTextField( );
                TextField.Layer.AnchorPoint = CGPoint.Empty;
                TextField.InputAssistantItem.LeadingBarButtonGroups = null;
                TextField.InputAssistantItem.TrailingBarButtonGroups = null;
                Theme.StyleTextField( TextField, Config.Instance.VisualSettings.TextFieldStyle );
                TextField.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.MediumFontSize );
                AddSubview( TextField );

                TextField.AutocorrectionType = UITextAutocorrectionType.No;
            }

            public string GetAssociatedAttributeKey( )
            {
                return AttribKey;
            }

            public void ShouldAdjustForKeyboard( bool shouldAdjust )
            {
                if ( shouldAdjust == true )
                {
                    TextField.Delegate = new TextFieldDelegate( this );
                }
                else
                {
                    TextField.Delegate = null;
                }
            }

            // useful when these are procedurally created and
            // the owner wants custom behavior.
            public UITextField GetTextField( )
            {
                return TextField;
            }

            public bool ShouldChangeCharacters( UIKit.UITextField textField, Foundation.NSRange range, string replacementString )
            {
                return NumbersOnly == true ? replacementString.IsNumeric( ) : true;
            }

            public void SetPosition( CGPoint position )
            {
                Layer.Position = position;
            }

            public void AddToView( UIView parent )
            {
                parent.AddSubview( this );
            }

            public void RemoveFromView( )
            {
                RemoveFromSuperview( );
            }

            public bool IsRequired( )
            {
                return Required;
            }

            public string GetCurrentValue( )
            {
                return TextField.Text;
            }

            public void SetCurrentValue( string value )
            {
                TextField.Text = value;
            }

            public CGRect GetFrame( )
            {
                return Frame;
            }

            public override bool ResignFirstResponder()
            {
                TextField.ResignFirstResponder( );

                return base.ResignFirstResponder();
            }

            public void ViewDidLayoutSubviews( CGRect parentBounds )
            {
                // set the width desired for the text field
                TextField.Bounds = new CGRect( 0, 0, parentBounds.Width, 0 );


                // now the height is determined by the size of the font.

                // measure the font
                CGSize size = TextField.SizeThatFits( new CGSize( TextField.Bounds.Width, TextField.Bounds.Height ) );

                // round up
                TextField.Bounds = new CGRect( TextField.Bounds.X, TextField.Bounds.Y, TextField.Bounds.Width, (float) System.Math.Ceiling( size.Height ) );

                // position the text field
                TextField.Layer.Position = new CGPoint( 0, Header.Frame.Bottom );


                // and wrap our controls
                Bounds = new CGRect( 0, 0, Math.Max( Header.Frame.Right, TextField.Frame.Right ), TextField.Frame.Bottom );   
            }
        }
    }
}
