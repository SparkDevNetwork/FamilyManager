﻿using System;
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
using System.Linq;
using Rock.Mobile.Util.Strings;
using Rock.Mobile.Network;
using System.Net;

namespace FamilyManager
{
    public class PersonInfoViewController : UIViewController
    {
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

        /// We use this because setting an image on a button via SetImage causes the button to size to the image,
        /// even with ContentMode set.
        /// </summary>
        /// <value>The profile image view.</value>
        UIImageView ProfileImageView { get; set; }
        UIButton EditPictureButton { get; set; }

        UIToggle AdultChildToggle { get; set; }

        UIScrollViewWrapper ScrollView { get; set; }

        UIButton SubmitButton { get; set; }

        class BaseMemberPanel
        {
            protected static nfloat verticalControlSpacing = 30;

            protected Dynamic_UITextField FirstName { get; set; }
            protected Dynamic_UITextField LastName { get; set; }

            protected Dynamic_UIDatePicker BirthdatePicker { get; set; }
            protected Dynamic_UIToggle GenderToggle { get; set; }

            protected Dynamic_UITextField PhoneNumber { get; set; }
            protected Dynamic_UITextField EmailAddress { get; set; }

            protected UIView RootView { get; set; }

            protected List<IDynamic_UIView> Dynamic_RequiredControls { get; set; }
            protected List<IDynamic_UIView> Dynamic_OptionalControls { get; set; }

            public List<KeyValuePair<string, string>> PendingAttribUpdates { get; protected set; }

            public BaseMemberPanel( )
            {
                Dynamic_RequiredControls = new List<IDynamic_UIView>();
                Dynamic_OptionalControls = new List<IDynamic_UIView>();
                PendingAttribUpdates = new List<KeyValuePair<string, string>>();
            }

            public virtual void Copy( BaseMemberPanel rhs )
            {
                FirstName.SetCurrentValue( rhs.FirstName.GetCurrentValue( ).ToUpperWords( ) );
                LastName.SetCurrentValue( rhs.LastName.GetCurrentValue( ).ToUpperWords( ) );

                BirthdatePicker.SetCurrentValue( rhs.BirthdatePicker.GetCurrentValue( ) );
                GenderToggle.SetCurrentValue( rhs.GenderToggle.GetCurrentValue( ) );

                PhoneNumber.SetCurrentValue( rhs.PhoneNumber.GetCurrentValue( ) );
                EmailAddress.SetCurrentValue( rhs.EmailAddress.GetCurrentValue( ) );
            }

            public virtual void ViewDidLoad( UIViewController parentViewController )
            {
                RootView = new UIView();
                RootView.Layer.AnchorPoint = CGPoint.Empty;

                FirstName = new Dynamic_UITextField( parentViewController, RootView, Strings.General_FirstName, false, true );
                FirstName.GetTextField( ).AutocapitalizationType = UITextAutocapitalizationType.Words;
                FirstName.GetTextField( ).AutocorrectionType = UITextAutocorrectionType.No;
                FirstName.AddToView( RootView );

                LastName = new Dynamic_UITextField( parentViewController, RootView, Strings.General_LastName, false, true );
                LastName.GetTextField( ).AutocapitalizationType = UITextAutocapitalizationType.Words;
                LastName.GetTextField( ).AutocorrectionType = UITextAutocorrectionType.No;
                LastName.AddToView( RootView );

                BirthdatePicker = new Dynamic_UIDatePicker( parentViewController, Strings.General_Birthday, false , "");
                BirthdatePicker.AddToView( RootView );

                GenderToggle = new Dynamic_UIToggle( parentViewController, RootView, Strings.Gender_Header, Strings.General_Male, Strings.General_Female, true );
                GenderToggle.AddToView( RootView );

                EmailAddress = new Dynamic_UITextField( parentViewController, RootView, Strings.General_Email, false, false );
                EmailAddress.GetTextField( ).AutocorrectionType = UITextAutocorrectionType.No;
                EmailAddress.GetTextField( ).Delegate = new KeyboardAdjustManager.TextFieldDelegate( );
                EmailAddress.ShouldAdjustForKeyboard( true );
                EmailAddress.AddToView( RootView );

                PhoneNumber = new Dynamic_UITextField( parentViewController, RootView, Strings.General_PhoneNumber, false, false );
                PhoneNumber.GetTextField( ).Delegate = new PhoneNumberFormatterDelegate( );
                PhoneNumber.AddToView( RootView );
            }

            public virtual void PersonInfoToUI( Rock.Client.Person workingPerson, Rock.Client.PhoneNumber workingPhoneNumber, Dictionary<string, Rock.Client.AttributeValue> attributeValues )
            {
                FirstName.SetCurrentValue( workingPerson.NickName );
                LastName.SetCurrentValue( workingPerson.LastName );

                if ( workingPerson.BirthDate.HasValue )
                {
                    BirthdatePicker.SetCurrentValue( string.Format( "{0:MMMMM dd yyyy}", workingPerson.BirthDate.Value ) );
                    BirthdatePicker.SizeToFit( );
                }
                else
                {
                    BirthdatePicker.SetCurrentValue( string.Empty );
                }

                PhoneNumber.SetCurrentValue( workingPhoneNumber.NumberFormatted );
                EmailAddress.SetCurrentValue( workingPerson.Email );

                string gender = RockActions.Genders[ (int)workingPerson.Gender ];
                if ( gender == Strings.General_Male || gender == Strings.General_Female )
                {
                    GenderToggle.SetCurrentValue( gender );
                }
                else
                {
                    GenderToggle.SetCurrentValue( "" );
                }
            }

            public virtual void UIInfoToPerson( Rock.Client.Person workingPerson, Rock.Client.PhoneNumber workingPhoneNumber )
            {
                workingPerson.NickName = FirstName.GetCurrentValue( ).ToUpperWords( );
                workingPerson.LastName = LastName.GetCurrentValue( ).ToUpperWords( );
                workingPerson.Email = EmailAddress.GetCurrentValue( );

                // take the birthday IF there's a value set.
                string birthday = BirthdatePicker.GetCurrentValue( );
                if ( string.IsNullOrEmpty( birthday ) == false )
                {
                    RockActions.SetBirthday( workingPerson, DateTime.Parse( birthday ) );
                }
                else
                {
                    RockActions.SetBirthday( workingPerson, null );
                }

                // try to get their phone number. If it's null, create a new one for them (even if we just put an empty string in it)
                RockActions.SetPhoneNumberDigits( workingPhoneNumber, PhoneNumber.GetCurrentValue( ).AsNumeric( ) );

                // only change their gender if it's been set to something.
                switch( GenderToggle.GetCurrentValue( ) )
                {
                    case Strings.General_Male:
                    {
                        workingPerson.Gender = (Rock.Client.Enums.Gender) RockActions.Genders.IndexOf( Strings.General_Male );
                        break;
                    }

                    case Strings.General_Female:
                    {
                        workingPerson.Gender = (Rock.Client.Enums.Gender) RockActions.Genders.IndexOf( Strings.General_Female );
                        break;
                    }
                }

            }

            public virtual bool IsInfoDirty( Rock.Client.Person workingPerson, Rock.Client.PhoneNumber workingPhoneNumber )
            {
                // if anything in the UI is different from the objects, then yeah, it's dirty
                if ( workingPerson.NickName != FirstName.GetCurrentValue( ) )
                {
                    return true;
                }

                // check last name
                if ( workingPerson.LastName != LastName.GetCurrentValue( ) )
                {
                    return true;
                }

                // check email
                string workingEmail = workingPerson.Email == null ? "" : workingPerson.Email;
                if ( workingEmail != EmailAddress.GetCurrentValue( ) )
                {
                    return true;
                }

                // check their birthday. First, get the working birthday as a normal string (June 14 2008) style
                string workingBirthday = "";
                if ( workingPerson.BirthDate.HasValue == true )
                {
                    workingBirthday = string.Format( "{0:MMMM dd yyyy}", workingPerson.BirthDate.Value );
                }

                if ( workingBirthday != BirthdatePicker.GetCurrentValue( ) )
                {
                    return true;
                }

                // check their phone number. first, if either is NOT null, we need to inspect further
                if ( string.IsNullOrEmpty( workingPhoneNumber.Number ) == false || string.IsNullOrEmpty( PhoneNumber.GetCurrentValue( ) ) == false )
                {
                    // if they don't match, it's dirty. We can't ONLY do this, because if they dont' match due to one being null and the other being "", that isn't dirty.
                    if ( workingPhoneNumber.Number != PhoneNumber.GetCurrentValue( ).AsNumeric( ) )
                    {
                        return true;
                    }
                }

                // check gender
                Rock.Client.Enums.Gender genderIndex = 0;
                switch( GenderToggle.GetCurrentValue( ) )
                {
                    case Strings.General_Male:
                    {
                        genderIndex = (Rock.Client.Enums.Gender) RockActions.Genders.IndexOf( Strings.General_Male );
                        break;
                    }

                    case Strings.General_Female:
                    {
                        genderIndex = (Rock.Client.Enums.Gender) RockActions.Genders.IndexOf( Strings.General_Female );
                        break;
                    }
                }

                if ( genderIndex != workingPerson.Gender )
                {
                    return true;
                }

                return false;
            }

            public virtual bool ValidateInfo( )
            {
                // ensure all required fields are filled out
                if ( string.IsNullOrEmpty( FirstName.GetCurrentValue( ) ) )
                {
                    return false;
                }

                if ( string.IsNullOrEmpty( LastName.GetCurrentValue( ) ) )
                {
                    return false;
                }

                if( string.IsNullOrEmpty( GenderToggle.GetCurrentValue( ) ) )
                {
                    return false;
                }

                // Make sure if there IS an email address, it's a valid format.
                if ( EmailAddress.GetCurrentValue( ) != string.Empty && EmailAddress.GetCurrentValue( ).IsEmailFormat( ) == false )
                {
                    return false;
                }

                // add more here if needed

                return true;
            }

            public virtual void TouchesEnded( )
            {
                BirthdatePicker.ResignFirstResponder( );
                FirstName.ResignFirstResponder( );
                LastName.ResignFirstResponder( );
                PhoneNumber.ResignFirstResponder( );
                EmailAddress.ResignFirstResponder( );
            }

            public virtual void ViewDidLayoutSubviews( CGRect parentBounds )
            {
                CGRect legalBounds = new CGRect( parentBounds.Left, parentBounds.Top, parentBounds.Width * .75f, parentBounds.Height );

                FirstName.ViewDidLayoutSubviews( legalBounds );
                FirstName.Layer.Position = new CGPoint( 10, 30 );
                FirstName.ShouldAdjustForKeyboard( true );

                LastName.ViewDidLayoutSubviews( legalBounds );
                LastName.Layer.Position = new CGPoint( 10, FirstName.Frame.Bottom + verticalControlSpacing );
                LastName.ShouldAdjustForKeyboard( true );

                BirthdatePicker.ViewDidLayoutSubviews( legalBounds );
                BirthdatePicker.SetPosition( new CGPoint( 10, LastName.Frame.Bottom + verticalControlSpacing ) );

                // now do phone and email...
                EmailAddress.ViewDidLayoutSubviews( new CGRect( 0, 0, parentBounds.Width / 2, parentBounds.Height ) );
                EmailAddress.Layer.Position = new CGPoint( 10, BirthdatePicker.Frame.Bottom + verticalControlSpacing );


                nfloat phoneNumberWidth = parentBounds.Width - EmailAddress.Bounds.Width - 40;
                PhoneNumber.ViewDidLayoutSubviews( new CGRect( 0, 0, phoneNumberWidth, parentBounds.Height ) );
                PhoneNumber.Layer.Position = new CGPoint( EmailAddress.Frame.Right + 20, BirthdatePicker.Frame.Bottom + verticalControlSpacing );

                GenderToggle.ViewDidLayoutSubviews( legalBounds );
                GenderToggle.Layer.Position = new CGPoint( 10, EmailAddress.Frame.Bottom + verticalControlSpacing );
            }

            public UIView GetRootView( )
            {
                return RootView;
            }
        }

        /// <summary>
        ///  This defines the panel to display when we're editing an adult
        /// </summary>
        class AdultMemberPanel : BaseMemberPanel
        {
            //UIToggle MaritalStatusToggle { get; set; }
            protected Dynamic_UIToggle MaritalStatusToggle { get; set; }

            public override void Copy( BaseMemberPanel rhs )
            {
                base.Copy( rhs );

                // there's no way to copy this value, so simply clear it out.
                // This could cause an adult that's loaded, switched to a child,
                // and BACK to an adult to then have a blank marital status, but that
                // is ok, because that results in it simply not being set to the person
                // on Submission.
                MaritalStatusToggle.SetCurrentValue( "" );
            }

            public override void PersonInfoToUI( Rock.Client.Person workingPerson, Rock.Client.PhoneNumber workingPhoneNumber, Dictionary<string, Rock.Client.AttributeValue> attributeValues )
            {
                base.PersonInfoToUI( workingPerson, workingPhoneNumber, attributeValues );

                // set the marital status. default to Unknown
                MaritalStatusToggle.SetCurrentValue( "" );

                if ( workingPerson.MaritalStatusValueId != null )
                {
                    Rock.Client.DefinedValue maritalStatus = Config.Instance.MaritalStatus.Where( ms => ms.Id == workingPerson.MaritalStatusValueId ).SingleOrDefault( );

                    if ( maritalStatus != null )
                    {
                        MaritalStatusToggle.SetCurrentValue( maritalStatus.Value );
                    }
                }

                // reset the control values
                foreach ( IDynamic_UIView dynamicView in Dynamic_RequiredControls )
                {
                    dynamicView.SetCurrentValue( string.Empty );
                }

                foreach ( IDynamic_UIView dynamicView in Dynamic_OptionalControls )
                {
                    dynamicView.SetCurrentValue( string.Empty );
                }

                FamilyManager.UI.Dynamic_UIFactory.AttributesToUI( attributeValues, Dynamic_RequiredControls );
                FamilyManager.UI.Dynamic_UIFactory.AttributesToUI( attributeValues, Dynamic_OptionalControls );
            }

            public override void UIInfoToPerson( Rock.Client.Person workingPerson, Rock.Client.PhoneNumber workingPhoneNumber )
            {
                base.UIInfoToPerson( workingPerson, workingPhoneNumber );

                // only update their marital status if it's set to something.
                if ( MaritalStatusToggle.GetCurrentValue( ) != string.Empty )
                {
                    string maritalStatusStr = MaritalStatusToggle.GetCurrentValue( );

                    // find the appropriate definef value
                    Rock.Client.DefinedValue maritalStatus = Config.Instance.MaritalStatus.Where( ms => ms.Value == maritalStatusStr ).SingleOrDefault( );
                    if ( maritalStatus != null )
                    {
                        workingPerson.MaritalStatusValueId = maritalStatus.Id;
                    }
                }

                // GET attribute values
                FamilyManager.UI.Dynamic_UIFactory.UIToAttributes( Dynamic_RequiredControls, PendingAttribUpdates );
                FamilyManager.UI.Dynamic_UIFactory.UIToAttributes( Dynamic_OptionalControls, PendingAttribUpdates );
            }

            public override bool ValidateInfo( )
            {
                // return false if our base control has a blank required field
                if( base.ValidateInfo( ) == false )
                {
                    return false;
                }


                // check the required attribs. If any of htem are blank, return false.
                foreach ( IDynamic_UIView dynamicControl in Dynamic_RequiredControls )
                {
                    if ( string.IsNullOrEmpty( dynamicControl.GetCurrentValue( ) ) == true )
                    {
                        return false;
                    }
                }

                // if we made it here, all data is good. return true.
                return true;
            }

            public override void ViewDidLoad( UIViewController parentViewController )
            {
                base.ViewDidLoad( parentViewController );

                MaritalStatusToggle = new Dynamic_UIToggle( parentViewController, RootView, Strings.MaritalStatus_Header, Strings.MaritalStatus_Married, Strings.MaritalStatus_Single, false );
                MaritalStatusToggle.AddToView( RootView );

                // build the dynamic UI controls
                for( int i = 0; i < Config.Instance.PersonAttributeDefines.Count; i++ )
                {
                    // make sure this control is for an adult
                    if ( Config.Instance.PersonAttributes[ i ][ "filter" ] == null || Config.Instance.PersonAttributes[ i ][ "filter" ].ToLower( ) == "adult" )
                    {
                        // get the required flag and the attribs that define what type of UI control this is.
                        bool isRequired = bool.Parse( Config.Instance.PersonAttributes[ i ][ "required" ] );
                        Rock.Client.Attribute uiControlAttrib = Config.Instance.PersonAttributeDefines[ i ];

                        // build it and add it to our UI
                        IDynamic_UIView uiView = Dynamic_UIFactory.CreateDynamic_UIControl( parentViewController, RootView, uiControlAttrib, isRequired, Config.Instance.PersonAttributeDefines[ i ].Key );
                        if ( uiView != null )
                        {
                            if ( isRequired == true )
                            {
                                Dynamic_RequiredControls.Add( uiView );
                                Dynamic_RequiredControls[ Dynamic_RequiredControls.Count - 1 ].AddToView( RootView );
                            }
                            else
                            {
                                Dynamic_OptionalControls.Add( uiView );
                                Dynamic_OptionalControls[ Dynamic_OptionalControls.Count - 1 ].AddToView( RootView );
                            }

                            uiView.ShouldAdjustForKeyboard( true );
                        }
                    }
                }
            }

            public override void TouchesEnded( )
            {
                base.TouchesEnded( );

                foreach ( IDynamic_UIView dynamicView in Dynamic_RequiredControls )
                {
                    dynamicView.ResignFirstResponder( );
                }

                foreach ( IDynamic_UIView dynamicView in Dynamic_OptionalControls )
                {
                    dynamicView.ResignFirstResponder( );
                }
            }

            public override void ViewDidLayoutSubviews( CGRect parentBounds )
            {
                base.ViewDidLayoutSubviews( parentBounds );

                CGRect legalBounds = new CGRect( 0, 0, parentBounds.Width * .75f, parentBounds.Height );

                //MaritalStatusToggle.SizeToFit( );
                MaritalStatusToggle.ViewDidLayoutSubviews( legalBounds );
                MaritalStatusToggle.Layer.Position = new CGPoint( parentBounds.Width - MaritalStatusToggle.Frame.Width - 10, EmailAddress.Frame.Bottom + verticalControlSpacing );

                // now procedurally add all the REQUIRED person attributes
                nfloat controlYPos = MaritalStatusToggle.Frame.Bottom + verticalControlSpacing;

                foreach ( IDynamic_UIView dynamicView in Dynamic_RequiredControls )
                {
                    dynamicView.ViewDidLayoutSubviews( legalBounds );

                    dynamicView.SetPosition( new CGPoint( 10, controlYPos ) );

                    controlYPos = dynamicView.GetFrame( ).Bottom + verticalControlSpacing;
                }

                // and now optional attributes
                foreach ( IDynamic_UIView dynamicView in Dynamic_OptionalControls )
                {
                    dynamicView.ViewDidLayoutSubviews( legalBounds );
                    dynamicView.SetPosition( new CGPoint( 10, controlYPos ) );

                    controlYPos = dynamicView.GetFrame( ).Bottom + verticalControlSpacing;
                }

                // wrap our view around the control
                RootView.Bounds = new CGRect( 0, 0, parentBounds.Width, controlYPos );
            }
        }
        AdultMemberPanel AdultPanel { get; set; }

        class ChildMemberPanel : BaseMemberPanel
        {
            Dynamic_UIDropDown GradeDropDown { get; set; }

            public override void Copy( BaseMemberPanel rhs )
            {
                base.Copy( rhs );

                // there's no way to copy this value, so simply clear it out.
                // If they loaded a child, switched to an adult, and went back to the child,
                // the grade will be empty, but it's ok because that will simply
                // cause the value to not be set when syncing with the person.
                GradeDropDown.SetCurrentValue( string.Empty );
            }

            public override void PersonInfoToUI( Rock.Client.Person workingPerson, Rock.Client.PhoneNumber workingPhoneNumber, Dictionary<string, Rock.Client.AttributeValue> attributeValues )
            {
                base.PersonInfoToUI( workingPerson, workingPhoneNumber, attributeValues );

                // set their grade (if they have it)
                GradeDropDown.SetCurrentValue( string.Empty );

                if ( workingPerson.GradeOffset.HasValue && workingPerson.GradeOffset.Value >= 0 )
                {
                    // get the defined value for their grade
                    Rock.Client.DefinedValue gradeValue = Config.Instance.SchoolGrades.Where( sg => int.Parse( sg.Value ) == workingPerson.GradeOffset.Value ).SingleOrDefault( );
                    GradeDropDown.SetCurrentValue( gradeValue.Description );
                }

                // reset the control values
                foreach ( IDynamic_UIView dynamicView in Dynamic_RequiredControls )
                {
                    dynamicView.SetCurrentValue( string.Empty );
                }

                foreach ( IDynamic_UIView dynamicView in Dynamic_OptionalControls )
                {
                    dynamicView.SetCurrentValue( string.Empty );
                }

                FamilyManager.UI.Dynamic_UIFactory.AttributesToUI( attributeValues, Dynamic_RequiredControls );
                FamilyManager.UI.Dynamic_UIFactory.AttributesToUI( attributeValues, Dynamic_OptionalControls );
            }

            public override void UIInfoToPerson( Rock.Client.Person workingPerson, Rock.Client.PhoneNumber workingPhoneNumber )
            {
                base.UIInfoToPerson( workingPerson, workingPhoneNumber );

                // update their grade
                string gradeValueStr = GradeDropDown.GetCurrentValue( );
                if ( string.IsNullOrEmpty( gradeValueStr ) == false )
                {
                    // find the defined value with the matching description
                    Rock.Client.DefinedValue gradeValue = Config.Instance.SchoolGrades.Where( sg => sg.Description == gradeValueStr ).SingleOrDefault( );

                    workingPerson.GradeOffset = int.Parse( gradeValue.Value );
                }

                // GET attribute values
                FamilyManager.UI.Dynamic_UIFactory.UIToAttributes( Dynamic_RequiredControls, PendingAttribUpdates );
                FamilyManager.UI.Dynamic_UIFactory.UIToAttributes( Dynamic_OptionalControls, PendingAttribUpdates );
            }

            public override void ViewDidLoad( UIViewController parentViewController )
            {
                base.ViewDidLoad( parentViewController );

                // build the grade list.
                string[] gradeList = new string[ Config.Instance.SchoolGrades.Count ];
                for( int i = 0; i < Config.Instance.SchoolGrades.Count; i++ )
                {
                    gradeList[ i ] = Config.Instance.SchoolGrades[ i ].Description;
                }

                GradeDropDown = new Dynamic_UIDropDown( parentViewController, RootView, Strings.PersonInfo_GradeHeader, Strings.PersonInfo_GradeMessage, gradeList, false );
                RootView.AddSubview( GradeDropDown );

                // build the dynamic UI controls
                for( int i = 0; i < Config.Instance.PersonAttributeDefines.Count; i++ )
                {
                    // make sure this control is for a child
                    if ( Config.Instance.PersonAttributes[ i ][ "filter" ] == null || Config.Instance.PersonAttributes[ i ][ "filter" ].ToLower( ) == "child" )
                    {
                        // get the required flag and the attribs that define what type of UI control this is.
                        bool isRequired = bool.Parse( Config.Instance.PersonAttributes[ i ][ "required" ] );
                        Rock.Client.Attribute uiControlAttrib = Config.Instance.PersonAttributeDefines[ i ];

                        // build it and add it to our UI
                        IDynamic_UIView uiView = Dynamic_UIFactory.CreateDynamic_UIControl( parentViewController, RootView, uiControlAttrib, isRequired, Config.Instance.PersonAttributeDefines[ i ].Key );
                        if ( uiView != null )
                        {
                            if ( isRequired == true )
                            {
                                Dynamic_RequiredControls.Add( uiView );
                                Dynamic_RequiredControls[ Dynamic_RequiredControls.Count - 1 ].AddToView( RootView );
                            }
                            else
                            {
                                Dynamic_OptionalControls.Add( uiView );
                                Dynamic_OptionalControls[ Dynamic_OptionalControls.Count - 1 ].AddToView( RootView );
                            }

                            uiView.ShouldAdjustForKeyboard( true );
                        }
                    }
                }
            }

            public override bool ValidateInfo( )
            {
                // return false if our base control has a blank required field
                if( base.ValidateInfo( ) == false )
                {
                    return false;
                }


                // check the required attribs. If any of htem are blank, return false.
                foreach ( IDynamic_UIView dynamicControl in Dynamic_RequiredControls )
                {
                    if ( string.IsNullOrEmpty( dynamicControl.GetCurrentValue( ) ) == true )
                    {
                        return false;
                    }
                }

                // if we made it here, all data is good. return true.
                return true;
            }

            public override void TouchesEnded( )
            {
                base.TouchesEnded( );

                foreach ( IDynamic_UIView dynamicView in Dynamic_RequiredControls )
                {
                    dynamicView.ResignFirstResponder( );
                }

                foreach ( IDynamic_UIView dynamicView in Dynamic_OptionalControls )
                {
                    dynamicView.ResignFirstResponder( );
                }
            }

            public override void ViewDidLayoutSubviews( CGRect parentBounds )
            {
                base.ViewDidLayoutSubviews( parentBounds );

                // vertically center the grade drop down with the gender toggle, which is to its left.
                GradeDropDown.ViewDidLayoutSubviews( parentBounds );
                nfloat gradeYPos = ( EmailAddress.Frame.Bottom + verticalControlSpacing ) - ( (GradeDropDown.Frame.Height - GenderToggle.Frame.Height) / 2 );
                GradeDropDown.Layer.Position = new CGPoint( parentBounds.Width - GradeDropDown.Frame.Width - 100,  gradeYPos );

                CGRect legalBounds = new CGRect( 0, 0, parentBounds.Width * .75f, parentBounds.Height );

                // now procedurally add all the REQUIRED person attributes
                nfloat controlYPos = GenderToggle.Frame.Bottom + verticalControlSpacing;

                foreach ( IDynamic_UIView dynamicView in Dynamic_RequiredControls )
                {
                    dynamicView.ViewDidLayoutSubviews( legalBounds );

                    dynamicView.SetPosition( new CGPoint( 10, controlYPos ) );

                    controlYPos = dynamicView.GetFrame( ).Bottom + verticalControlSpacing;
                }

                // and now optional attributes
                foreach ( IDynamic_UIView dynamicView in Dynamic_OptionalControls )
                {
                    dynamicView.ViewDidLayoutSubviews( legalBounds );
                    dynamicView.SetPosition( new CGPoint( 10, controlYPos ) );

                    controlYPos = dynamicView.GetFrame( ).Bottom + verticalControlSpacing;
                }

                // wrap our view around the control
                RootView.Bounds = new CGRect( 0, 0, parentBounds.Width, controlYPos );
            }
        }
        ChildMemberPanel ChildPanel { get; set; }



        /// <summary>
        /// Reference to the currently active panel
        /// </summary>
        BaseMemberPanel ActivePanel { get; set; }

        KeyboardAdjustManager KeyboardAdjustManager { get; set; }

        bool Animating { get; set; }

        UIButton CloseButton { get; set; }

        /// <summary>
        /// True if there's a profile picture that should be updated.
        /// </summary>
        bool ProfileImageViewDirty { get; set; }

        bool Dirty { get; set; }

        UIBlockerView BlockerView { get; set; }

        public PersonInfoViewController( ContainerViewController parent )
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
            MainPanel.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.SidebarBGColor );//Theme.GetColor( Config.Instance.VisualSettings.TopHeaderBGColor );
            MainPanel.Layer.Opacity = 1.00f;
            MainPanel.ClipsToBounds = true;
            View.AddSubview( MainPanel );

            // Scroll view on the right hand side
            ScrollView = new UIScrollViewWrapper( );
            ScrollView.Layer.AnchorPoint = CGPoint.Empty;
            ScrollView.Parent = this;
            ScrollView.BackgroundColor = Theme.GetColor( Config.Instance.VisualSettings.BackgroundColor );
            MainPanel.AddSubview( ScrollView );


            // setup controls that go on the left side
            AdultChildToggle = new UIToggle( Strings.General_Adult, Strings.General_Child, 
                delegate(bool wasLeft) 
                {
                    if( wasLeft == true )
                    {
                        SwitchActiveMemberView( AdultPanel );
                    }
                    else
                    {
                        SwitchActiveMemberView( ChildPanel );
                    }
                } );
            Theme.StyleToggle( AdultChildToggle, Config.Instance.VisualSettings.ToggleStyle );
            AdultChildToggle.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
            AdultChildToggle.Layer.AnchorPoint = CGPoint.Empty;
            MainPanel.AddSubview( AdultChildToggle );

            // setup our submit button
            SubmitButton = UIButton.FromType( UIButtonType.System );
            SubmitButton.Layer.AnchorPoint = CGPoint.Empty;
            SubmitButton.Font = FontManager.GetFont( Settings.General_RegularFont, Config.Instance.VisualSettings.SmallFontSize );
            SubmitButton.SetTitle( Strings.General_Save, UIControlState.Normal );
            SubmitButton.BackgroundColor = UIColor.Blue;
            SubmitButton.SizeToFit( );
            Theme.StyleButton( SubmitButton, Config.Instance.VisualSettings.PrimaryButtonStyle );
            SubmitButton.Bounds = new CGRect( 0, 0, SubmitButton.Bounds.Width * 2.00f, SubmitButton.Bounds.Height );
            MainPanel.AddSubview( SubmitButton );
            SubmitButton.TouchUpInside += (object sender, EventArgs e ) =>
            {
                TrySubmitPerson( );
            };

            // setup the button to tap for editing the picture
            EditPictureButton = new UIButton( new CGRect( 0, 0, 112, 112 )  );
            EditPictureButton.Layer.AnchorPoint = CGPoint.Empty;
            EditPictureButton.Font = FontManager.GetFont( Settings.AddPerson_Icon_Font_Primary, Settings.AddPerson_SymbolFontSize );
            EditPictureButton.SetTitleColor( Theme.GetColor( Config.Instance.VisualSettings.PhotoOutlineColor ), UIControlState.Normal );
            EditPictureButton.Layer.BorderColor = Theme.GetColor( Config.Instance.VisualSettings.PhotoOutlineColor ).CGColor;
            EditPictureButton.Layer.CornerRadius = EditPictureButton.Bounds.Width / 2;
            EditPictureButton.Layer.BorderWidth = 4;
            MainPanel.AddSubview( EditPictureButton );

            EditPictureButton.TouchUpInside += (object sender, EventArgs e ) =>
                {
                    Parent.CaptureImage( delegate(NSData imageBuffer )
                        {
                            // if an image was taken, flag this as dirty so 
                            // we know to upload it.
                            if( imageBuffer != null )
                            {
                                ProfileImageViewDirty = true;
                                UpdateProfilePic( imageBuffer );
                            }
                        });
                };

            // set the profile image mask so it's circular
            CALayer maskLayer = new CALayer();
            maskLayer.AnchorPoint = new CGPoint( 0, 0 );
            maskLayer.Bounds = EditPictureButton.Layer.Bounds;
            maskLayer.CornerRadius = EditPictureButton.Bounds.Width / 2;
            maskLayer.BackgroundColor = UIColor.Black.CGColor;
            EditPictureButton.Layer.Mask = maskLayer;
            //


            // setup the image that will display (and note it's a child of EditPictureButton)
            ProfileImageView = new UIImageView( );
            ProfileImageView.ContentMode = UIViewContentMode.ScaleAspectFit;

            ProfileImageView.Layer.AnchorPoint = CGPoint.Empty;
            ProfileImageView.Bounds = EditPictureButton.Bounds;
            ProfileImageView.Layer.Position = CGPoint.Empty;
            EditPictureButton.AddSubview( ProfileImageView );

            KeyboardAdjustManager = new KeyboardAdjustManager( MainPanel );

            // setup both types of member view. Adult and Child
            AdultPanel = new AdultMemberPanel( );
            AdultPanel.ViewDidLoad( this );
            ScrollView.AddSubview( AdultPanel.GetRootView( ) );

            ChildPanel = new ChildMemberPanel( );
            ChildPanel.ViewDidLoad( this );
            ScrollView.AddSubview( ChildPanel.GetRootView( ) );

            // add our Close Button
            CloseButton = UIButton.FromType( UIButtonType.System );
            CloseButton.Layer.AnchorPoint = CGPoint.Empty;
            CloseButton.SetTitle( "X", UIControlState.Normal );
            Theme.StyleButton( CloseButton, Config.Instance.VisualSettings.DefaultButtonStyle );
            CloseButton.SizeToFit( );
            CloseButton.BackgroundColor = UIColor.Clear;
            CloseButton.Layer.BorderWidth = 0;
            //CloseButton.Layer.CornerRadius = CloseButton.Bounds.Width / 2;
            MainPanel.AddSubview( CloseButton );
            CloseButton.TouchUpInside += (object sender, EventArgs e ) =>
            {
                ActivePanel.TouchesEnded( );

                if ( ActivePanel.IsInfoDirty( WorkingPerson, WorkingPhoneNumber ) )
                {
                    ConfirmCancel( );
                }
            };


            // default to the adult, and hide the child one
            ActivePanel = AdultPanel;
            ChildPanel.GetRootView( ).Hidden = true;

            View.SetNeedsLayout( );

            ProfileImageView.Image = null;
            EditPictureButton.SetTitle( Settings.AddPerson_NoPhotoSymbol, UIControlState.Normal );

            // remove any existing picture.
            FileCache.Instance.RemoveFile( Settings.AddPerson_PicName );

            // setup the main bounds
            MainPanel.Bounds = new CoreGraphics.CGRect( 0, 0, View.Bounds.Width * .75f, View.Bounds.Height * .75f );

            MainPanel.Layer.CornerRadius = 4;

            // default to hidden until PresentAnimated() is called.
            View.Hidden = true;

            BlockerView = new UIBlockerView( View, View.Bounds.ToRectF( ) );
        }

        void SwitchActiveMemberView( BaseMemberPanel memberPanel )
        {
            // don't change if the panel is already active
            if ( ActivePanel != memberPanel )
            {
                // and don't do it if we're animating
                if ( Animating == false )
                {
                    // disable the toggle button until we're done animating
                    AdultChildToggle.Enabled = false;

                    Animating = true;
                    
                    // animate out the current, bring in the new
                    SimpleAnimator_PointF activeOutAnim = new SimpleAnimator_PointF( ScrollView.Layer.Position.ToPointF( ), new PointF( (float)ScrollView.Layer.Position.X, (float)ScrollView.Bounds.Height * .95f ), .33f,
                        delegate(float percent, object value )
                        {
                            ScrollView.Layer.Position = (PointF)value;
                        }, 
                        delegate
                        {
                            // set the new panel as active, and position it off screen.
                            ActivePanel.GetRootView( ).Hidden = true;

                            // copy the data from the currently active panel to the new one
                            memberPanel.Copy( ActivePanel );

                            // now switch it
                            ActivePanel = memberPanel;
                            ActivePanel.GetRootView( ).Hidden = false;

                            ViewDidLayoutSubviews( );

                            // now kick off the animate IN for the new panel
                            SimpleAnimator_PointF activeInAnim = new SimpleAnimator_PointF( ScrollView.Layer.Position.ToPointF( ), new PointF( (float)ScrollView.Layer.Position.X, 0 ), .33f,
                                delegate(float percent, object value )
                                {
                                    ScrollView.Layer.Position = (PointF)value;
                                },
                                delegate
                                {
                                    // and finally we're done animating.

                                    // re-enable the toggle button, and flag that we're no longer animating
                                    AdultChildToggle.Enabled = true;
                                    Animating = false;
                                });

                            activeInAnim.Start( SimpleAnimator.Style.CurveEaseIn );
                        } );

                    activeOutAnim.Start( SimpleAnimator.Style.CurveEaseOut );
                }
            }
        }

        public void UpdateProfilePic( NSData profilePicBuffer )
        {
            Rock.Mobile.Threading.Util.PerformOnUIThread( 
                delegate
                {
                    // if there's no valid buffer, display the no photo
                    if ( profilePicBuffer == null )
                    {
                        ProfileImageView.Image = null;
                        EditPictureButton.SetTitle( Settings.AddPerson_NoPhotoSymbol, UIControlState.Normal );
                    }
                    else
                    {
                        // otherwise, go for it.
                        EditPictureButton.SetTitle( "", UIControlState.Normal );

                        UIImage image = new UIImage( profilePicBuffer );
                        ProfileImageView.Image = image;
                    }
                } );
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews( );

            BackgroundPanel.Bounds = View.Bounds;
            BackgroundPanel.Layer.Position = View.Layer.Position;

            // setup the scroll view and contents
            nfloat scrollWidth = MainPanel.Bounds.Width * .78f;

            nfloat sidePanelWidth = MainPanel.Bounds.Width - scrollWidth;

            // layout the scroll view
            ScrollView.Bounds = new CGRect( 0, 0, scrollWidth, MainPanel.Bounds.Height );
            ScrollView.Layer.Position = new CGPoint( sidePanelWidth, ScrollView.Layer.Position.Y );


            // layout the active view
            AdultPanel.ViewDidLayoutSubviews( ScrollView.Bounds );
            ChildPanel.ViewDidLayoutSubviews( ScrollView.Bounds );


            // always allow scrolling to just below the save button
            ScrollView.ContentSize = new CGSize( ScrollView.ContentSize.Width, ActivePanel.GetRootView( ).Frame.Bottom + 60 );


            // layout the left side bar
            EditPictureButton.Layer.AnchorPoint = CGPoint.Empty;
            EditPictureButton.Layer.Position = new CGPoint( (sidePanelWidth - EditPictureButton.Bounds.Width) / 2, 10 );

            // adult / child toggle
            AdultChildToggle.SizeToFit( );
            AdultChildToggle.Layer.Position = new CGPoint( (sidePanelWidth - AdultChildToggle.Bounds.Width) / 2, EditPictureButton.Frame.Bottom + 50 );

            // Submit Button
            SubmitButton.Layer.Position = new CGPoint( (sidePanelWidth - SubmitButton.Bounds.Width) / 2, AdultChildToggle.Frame.Bottom + 40 );

            CloseButton.Layer.Position = new CGPoint( MainPanel.Bounds.Width - CloseButton.Bounds.Width - 10, 5 );

            BlockerView.SetBounds( View.Bounds.ToRectF( ) );
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);

            // hide the keyboard _BEFORE_ showing an iOS dialog
            ActivePanel.TouchesEnded( );

            UITouch touch = (UITouch) touches.AnyObject;

            CGPoint posInMain = touch.LocationInView( MainPanel );

            // if they tap outside the window (in the shaded area) dismiss this.
            if ( (posInMain.X < 0 || posInMain.X > MainPanel.Bounds.Width) || 
                 (posInMain.Y < 0 || posInMain.Y > MainPanel.Bounds.Height) )
            {
                // confirm they want to cancel
                if ( ActivePanel.IsInfoDirty( WorkingPerson, WorkingPhoneNumber ) )
                {
                    ConfirmCancel( );
                }
                else
                {
                    DismissAnimated( false );
                }
            }
        }

        void ConfirmCancel( )
        {
            UIAlertController actionSheet = UIAlertController.Create( Strings.General_Confirm, 
                IsNewPerson == true ? Strings.PersonInfo_ConfirmCancelNewPerson : Strings.PersonInfo_ConfirmCancelExistingPerson,
                UIAlertControllerStyle.Alert );
            
            UIAlertAction yesAction = UIAlertAction.Create( Strings.General_Yes, UIAlertActionStyle.Destructive, delegate(UIAlertAction obj) 
                {
                    // display 
                    DismissAnimated( false );
                } );
            
            //setup cancel
            UIAlertAction cancelAction = UIAlertAction.Create( Strings.General_No, UIAlertActionStyle.Default, delegate{ } );

            actionSheet.AddAction( yesAction );
            actionSheet.AddAction( cancelAction );

            Parent.PresentViewController( actionSheet, true, null );
        }

        void TrySubmitPerson( )
        {
            if ( ActivePanel.ValidateInfo( ) )
            {
                ActivePanel.TouchesEnded( );

                BlockerView.Show( delegate
                    {
                        // gather all info
                        ActivePanel.UIInfoToPerson( WorkingPerson, WorkingPhoneNumber );

                        MemoryStream profileImage = null;
                        if ( ProfileImageViewDirty == true )
                        {
                            // convert it to a buffer
                            profileImage = new MemoryStream();
                            Stream nsDataStream = ProfileImageView.Image.AsJPEG( ).AsStream( );

                            nsDataStream.CopyTo( profileImage );
                            profileImage.Position = 0;
                        }

                        FamilyManagerApi.UpdateFullPerson( IsNewPerson, WorkingPerson, IsNewPhoneNumber, WorkingPhoneNumber, ActivePanel.PendingAttribUpdates, profileImage, 
                            delegate(HttpStatusCode statusCode, string statusDescription )
                            {
                                BlockerView.Hide( delegate
                                    {
                                        if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                                        {
                                            DismissAnimated( true );
                                        }
                                        else
                                        {
                                            Rock.Mobile.Util.Debug.DisplayError( Strings.General_Error_Header, Strings.General_Error_Message );
                                        }
                                    } );
                            } );
                    } );
            }
            else
            {
                Rock.Mobile.Util.Debug.DisplayError( Strings.PersonInfo_MissingInfo_Header, Strings.PersonInfo_MissingInfo_Message );
            }
        }

        void PersonInfoToUI( Rock.Client.Person workingPerson, Rock.Client.PhoneNumber workingPhoneNumber, bool isChild, Dictionary<string, Rock.Client.AttributeValue> attributeValues )
        {
            // reset the pending updates for attributes
            AdultPanel.PendingAttribUpdates.Clear( );
            ChildPanel.PendingAttribUpdates.Clear( );
                        
            // if they're an adult, use the adult panel
            if ( isChild == false )
            {
                ActivePanel = AdultPanel;
                ChildPanel.GetRootView( ).Hidden = true;

                AdultChildToggle.ToggleSide( UIToggle.Toggle.Left );
            }
            else
            {
                // otherwise, use the child panel
                ActivePanel = ChildPanel;
                AdultPanel.GetRootView( ).Hidden = true;

                AdultChildToggle.ToggleSide( UIToggle.Toggle.Right );
            }

            // set the appropriate info
            AdultPanel.PersonInfoToUI( workingPerson, workingPhoneNumber, attributeValues );
            ChildPanel.PersonInfoToUI( workingPerson, workingPhoneNumber, attributeValues );

            ActivePanel.GetRootView( ).Hidden = false;
        }

        bool IsNewPerson { get; set; }
        Rock.Client.Person WorkingPerson { get; set; }

        bool IsNewPhoneNumber { get; set; }
        Rock.Client.PhoneNumber WorkingPhoneNumber { get; set; }

        // This ID of the family this person is part of (or will be, if they're new)
        // Note it can be 0 if it's a new family that hasn't been posted yet.
        int WorkingFamilyId { get; set; }

        FamilyInfoViewController.OnPersonInfoCompleteDelegate OnCompleteDelegate { get; set; }
        public void PresentAnimated( int workingFamilyId,
                                     string workingFamilyLastName,
                                     Rock.Client.Person workingPerson, 
                                     bool isChild,
                                     Dictionary<string, Rock.Client.AttributeValue> attributeValues, 
                                     NSData profilePicBuffer, 
                                     FamilyInfoViewController.OnPersonInfoCompleteDelegate onComplete )
        {
            OnCompleteDelegate = onComplete;

            WorkingFamilyId = workingFamilyId;

            KeyboardAdjustManager.Activate( );
            
            // take the provided person as our working person
            if ( workingPerson == null )
            {
                IsNewPerson = true;
                WorkingPerson = new Rock.Client.Person( );

                // since it's a new person, put the last name of the family in there.
                WorkingPerson.LastName = workingFamilyLastName;
            }
            else
            {
                IsNewPerson = false;
                WorkingPerson = workingPerson;
            }

            // we need to know now what values AREN'T set, so we don't inadvertantly set them to defaults.
            WorkingPhoneNumber = RockActions.TryGetPhoneNumber( WorkingPerson, RockActions.CellPhoneValueId );
            if ( WorkingPhoneNumber == null )
            {
                IsNewPhoneNumber = true;
                WorkingPhoneNumber = new Rock.Client.PhoneNumber();
            }
            else
            {
                IsNewPhoneNumber = false;
            }

            PersonInfoToUI( WorkingPerson, WorkingPhoneNumber, isChild, attributeValues );

            // set their profile picture
            UpdateProfilePic( profilePicBuffer );

            View.Hidden = false;

            // animate the background to dark
            BackgroundPanel.Layer.Opacity = 0;

            SimpleAnimator_Float alphaAnim = new SimpleAnimator_Float( BackgroundPanel.Layer.Opacity, Settings.DarkenOpacity, .33f, 
                delegate(float percent, object value )
                {
                    BackgroundPanel.Layer.Opacity = (float)value;
                }, null );

            alphaAnim.Start( SimpleAnimator.Style.CurveEaseOut );


            // update the main panel
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
                null);


            posAnim.Start( SimpleAnimator.Style.CurveEaseOut );

            ViewDidLayoutSubviews( );
        }

        void DismissAnimated( bool didSave )
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

                        // if the active panel is the Child, then yes, they should be set as a child.
                        bool isChild = ActivePanel == ChildPanel ? true : false;

                        OnCompleteDelegate( didSave, WorkingFamilyId, isChild, WorkingPerson );
                    } );

                posAnim.Start( SimpleAnimator.Style.CurveEaseOut );
            }
        }
    }
}