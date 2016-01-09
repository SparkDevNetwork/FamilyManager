using System;

namespace FamilyManager
{
    public class Strings
    {
        public static string General_Version = "Version 1.0.4";
        
        public static string General_RockBindError_NotFound = "Rock couldn't be found. Try that again.";
        public static string General_RockBindSuccess = "";
        public static string General_RockBindError_Data = "Rock was found, but there was a problem downloading data.";

        public static string General_Login_Invalid = "Invalid Username or Password";
        public static string General_Login_NotAuthorized = "Unfortunately you're not authorized to use this app. Please contact your Rock Administrator if you need to access this resource.";

        public static string General_Rock_ThemeFailed = "There was a problem applying the selected Theme.";
        //public static string General_Return_LoginScreen = "Are you sure
        public static string General_FirstTimeVisitors = "Should new people be set as first-time visitors?";
        
        public static string General_Ok = "Ok";
        public static string General_Cancel = "Cancel";
        public static string General_Yes = "Yes";
        public static string General_No = "No";
        public static string General_Search = "Search";
        public static string General_AddFamily = "Add Family";
        public static string General_AddGuestFamily = "Add Guest Family";
        public static string General_AddFamilyMember = "Add Family Member";
        public static string General_NoAddress = "No Address On File";
        public static string General_Confirm = "Confirm";
        public static string General_FamilyName = "Family Name";
        public static string General_Save = "Save";
        public static string General_Welcome = "Welcome";
        public static string General_Logout = "Logout";
        public static string General_Stay = "Stay";
        public static string General_Remove = "Remove";
        public static string General_Clear = "Clear";

        public static string Search_NoResults_Title = "No Families Found";
        public static string Search_NoResults_Suggestions = "Suggestions:";
        public static string Search_NoResults_Suggestion1 = "Make sure names are spelled correctly.";
        public static string Search_NoResults_Suggestion2 = "Try partial names.";

        public static string General_ConfirmLogout = "Do you want to return to the Login Screen?";

        public static string General_HomeAddress = "Home Address";

        public static string General_Street = "Street";
        public static string General_City = "City";
        public static string General_State = "State";
        public static string General_Zip = "Zip";

        public static string General_FirstName = "First Name";
        public static string General_LastName = "Last Name";
        public static string General_Email = "Email Address";
        public static string General_Birthday = "Birthday";
        public static string General_PhoneNumber = "Phone Number";
        public const string General_Male = "Male";
        public const string General_Female = "Female";
        public static string General_Adult = "Adult";
        public static string General_Child = "Child";
        public static string General_Adults = "Adults";
        public static string General_Children = "Children";

        public static string General_Error_Header = "Network Error";
        public static string General_Error_Message = "There was a problem talking to Rock. Try again";

        public static string General_StartUp_Error_Header = "Network Error";
        public static string General_StartUp_Error_Message = "We couldn't find Rock, so we're taking you back to the setup screen.\n\nCheck your network connection and server address, then try again.";

        public static string Camera_None_Header = "Camera Unavailable";
        public static string Camera_None_Message = "Could not access the camera. Make sure this device has a camera and the app is allowed to use it.";

        public static string Camera_Error_Header = "Camera";
        public static string Camera_Error_Message = "There was a problem saving the image.";

        public static string Gender_Header = "Gender";

        public static string MaritalStatus_Header = "Marital Status";
        public static string MaritalStatus_Single = "Single";
        public static string MaritalStatus_Married = "Married";
        
        public static string PersonInfo_GradeHeader = "Grade";
        public static string PersonInfo_GradeMessage = "Select Grade";

        public static string PersonInfo_ConfirmCancelNewPerson = "Cancel adding this person?";
        public static string PersonInfo_ConfirmCancelExistingPerson = "Cancel making changes to this person?";


        public static string PersonInfo_BlankFirstName_Header = "First Name";
        public static string PersonInfo_BlankFirstName_Message = "The First Name field is blank. Add a first name and try again.";

        public static string PersonInfo_BlankLastName_Header = "Last Name";
        public static string PersonInfo_BlankLastName_Message = "The Last Name field is blank. Add a last name and try again.";

        public static string PersonInfo_BlankGender_Header = "Gender";
        public static string PersonInfo_BlankGender_Message = "Select a gender and try again.";

        public static string PersonInfo_BadEmail_Header = "Email";
        public static string PersonInfo_BadEmail_Message = "The email address is not formatted correctly. Make sure it is example@email.com and try again.";

        public static string PersonInfo_BlankAttrib_Header = "Person Attribute";
        public static string PersonInfo_BlankAttrib_Message = "One of the required person attributes is blank. Add the required information and try again.";


        public static string PersonInfo_MissingInfo_Header = "Missing Information";
        public static string PersonInfo_MissingInfo_Message = "At least one required field is empty, or there is an invalid email address. Double check and try again.";

        public static string PersonInfo_AllowCheckinsBy_Header = "Allow Checkins By";

        public static string FamilyInfo_AddFamilyMemberHeader = "Add Family Member";
        public static string FamilyInfo_AddFamilyMemberBody = "What Do You Want To Add?";
        public static string FamilyInfo_AddNewPerson = "Add New Person";
        public static string FamilyInfo_AddExistingPerson = "Add Existing Person";
        public static string FamilyInfo_FamilyMembers = "Family Members";
        public static string FamilyInfo_GuestFamilies = "Guest Families";
        public static string FamilyInfo_Select_Campus_Header = "Campus";
        public static string FamilyInfo_Select_Campus_Message = "Select Home Campus";
        public static string FamilyInfo_Unnamed_Family = "Unnamed Family";

        public static string FamilyInfo_MissingName_Header = "Family Name";
        public static string FamilyInfo_MissingName_Message = "The Family Name is blank. Add a name and try again.";

        public static string FamilyInfo_BadAddress_Header = "Family Address";
        public static string FamilyInfo_BadAddress_Message = "The Family Address isn't correct. Make sure it's either blank, or has at least a Street.";

        public static string FamilyInfo_BlankAttrib_Header = "Family Attribute";
        public static string FamilyInfo_BlankAttrib_Message = "One of the required family attributes is blank. Add the required information and try again.";

        public static string FamilyInfo_Header_NoMembers = "Empty Family";
        public static string FamilyInfo_Body_NoMembers = "This family is empty. Please add at least one family member and then try again.";

        public static string FamilyInfo_Header_Gone = "Family Gone";
        public static string FamilyInfo_Body_Gone = "It doesn't look like this family exists anymore. Try again or search for another family.";

        public static string FamilyInfo_SaveComplete = "Save Complete";

        public static string AddPerson_AddFamilyAsGuest_Header = "Add Family?";
        public static string AddPerson_AddFamilyAsGuest_Message = "Add the {0} (including {1}) as a guest family?";

        public static string AddPerson_CancelAddingMember = "Cancel adding this family member?";

        public static string AddPerson_KeepInOtherFamilies = "Should this person(s) stay in other families?";

        public static string AddPerson_AddPeople_Header = "Add People";

        public static string AddPerson_SelectPeople_Header = "Select people to add to the family.";
    }
}

