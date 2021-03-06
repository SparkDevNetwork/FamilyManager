﻿using System;
using Rock.Mobile.Network;
using System.Net;
using Rock.Mobile;
using Rock.Mobile.Util;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using System.Linq;

namespace FamilyManager
{
    /// <summary>
    /// Implements API Methods that are specific to the FamilyManager app.
    /// </summary>
    public static class FamilyManagerApi
    {
        public static void SortFamilyMembers( List<Rock.Client.GroupMember> familyMembers )
        {
            // sort the family members by adult / child
            familyMembers.Sort( delegate(Rock.Client.GroupMember x, Rock.Client.GroupMember y )
                {
                    // determine whether they're adults by finding out if they ARE NOT children (that will let them be adults
                    // if their GroupRole happens to be "Unknown" or something)
                    bool xIsAdult = x.GroupRole.Id == Config.Instance.FamilyMemberChildGroupRole.Id ? false : true;
                    bool yIsAdult = y.GroupRole.Id == Config.Instance.FamilyMemberChildGroupRole.Id ? false : true;

                    // if they're both adults or children
                    if( xIsAdult == yIsAdult )
                    {
                        // then the non-female should win
                        if( x.Person.Gender != Rock.Client.Enums.Gender.Female )
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }

                    // if one is an adult and the other a child, the adult should win
                    if( xIsAdult == true && yIsAdult == false )
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                } );
        }

        public static void SortGuestFamilyMembers( List<Rock.Client.GuestFamily.Member> familyMembers )
        {
            // sort the family members by adult / child
            familyMembers.Sort( delegate( Rock.Client.GuestFamily.Member x, Rock.Client.GuestFamily.Member y )
                {
                    // determine whether they're adults by finding out if they ARE NOT children (that will let them be adults
                    // if their GroupRole happens to be "Unknown" or something)
                    bool xIsAdult = x.Role == "Child" ? false : true;
                    bool yIsAdult = y.Role == "Child" ? false : true;

                    // if they're both adults or children
                    if( xIsAdult == yIsAdult )
                    {
                        // then the non-female should win
                        if( x.Gender != Rock.Client.Enums.Gender.Female )
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }

                    // if one is an adult and the other a child, the adult should win
                    if( xIsAdult == true && yIsAdult == false )
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                } );
        }

        public static void GetAppAuthorization( int personId, HttpRequest.RequestResult resultHandler )
        {
            // use PersonId 379595 for debugging. that one is allowed.
            string oDataFilter = string.Format( "?$filter=Guid eq guid'D832E933-1972-4482-B24D-6AF0AC6BDF20' and Members/any(p: p/PersonId eq  {0})", personId );

            RockApi.Get_Groups<List<Rock.Client.Group>>( oDataFilter, delegate(HttpStatusCode statusCode, string statusDescription, List<Rock.Client.Group> model )
                {
                    // if we get a successful status code, a valid model, and there's an object in it, they're authorized.
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) && model != null && model.Count > 0)
                    {
                        resultHandler( HttpStatusCode.OK, "" );
                    }
                    else
                    {
                        resultHandler( HttpStatusCode.Unauthorized, "" );
                    }
                } );
        }

        public static void AddPersonToFamily( Rock.Client.Person person, int childAdultRoleMemberId, int toFamilyId, bool removeFromOtherFamilies, HttpRequest.RequestResult result )
        {
            // setup the oData. childAdultRoleMemberId describes whether this person should be an adult or child in the family.
            string oDataFilter = string.Format( "?personId={0}&familyId={1}&groupRoleId={2}&removeFromOtherFamilies={3}", person.Id, toFamilyId, childAdultRoleMemberId, removeFromOtherFamilies );

            RockApi.Post_People_AddExistingPersonToFamily( oDataFilter, result );
        }

        public static void UpdateOrAddPersonAttribute( int personId, string key, string value, HttpRequest.RequestResult resultHandler )
        {
            string urlEncodedValue = System.Net.WebUtility.UrlEncode( value );
            string oDataFilter = string.Format( "/{0}?attributeKey={1}&attributeValue={2}", personId, key, urlEncodedValue );

            if( string.IsNullOrWhiteSpace( value ) )
            {
                RockApi.Delete_People_AttributeValue( oDataFilter, resultHandler );
            }
            else
            {
                RockApi.Post_People_AttributeValue( oDataFilter, resultHandler );
            }
        }

        public static void UpdateOrAddFamilyAttribute( int familyId, string key, string value, HttpRequest.RequestResult resultHandler )
        {
            string urlEncodedValue = System.Net.WebUtility.UrlEncode( value );
            string oDataFilter = string.Format( "/{0}?attributeKey={1}&attributeValue={2}", familyId, key, urlEncodedValue );

            //todo: add delete
            RockApi.Post_Groups_AttributeValue( oDataFilter, resultHandler );
        }

        public static void UpdateKnownRelationship( int personId, int relatedPersonId, int relationshipRoleId, HttpRequest.RequestResult resultHandler )
        {
            string oDataFilter = string.Format( "?personId={0}&relatedPersonId={1}&relationshipRoleId={2}", personId, relatedPersonId, relationshipRoleId );
            RockApi.Post_GroupMembers_KnownRelationships( oDataFilter, resultHandler );
        }

        public static void RemoveKnownRelationship( int personId, int relatedPersonId, int relationshipRoleId, HttpRequest.RequestResult resultHandler )
        {
            string oDataFilter = string.Format( "?personId={0}&relatedPersonId={1}&relationshipRoleId={2}", personId, relatedPersonId, relationshipRoleId );

            RockApi.Delete_GroupMembers_KnownRelationships( oDataFilter, resultHandler );
        }

        //public const string ConfigurationTemplateDefinedTypeGuid = "0F48CB3F-8A48-249A-412A-2DCA7648706F";
        public const string ConfigurationTemplateDefinedTypeGuid = "251D752B-0595-C3A6-4E2A-AD0264DAFCCD";
        public static void GetConfigurationTemplates( int typeId, int configurationTemplateId, HttpRequest.RequestResult<List<Rock.Client.DefinedValue>> resultHandler )
        {
            string oDataFilter = string.Format( "?LoadAttributes=simple&$filter=DefinedTypeId eq {0}", typeId );

            // if a SPECIFIC templateId was requested, append it
            if ( configurationTemplateId > 0 )
            {
                oDataFilter += string.Format( " and Id eq " + configurationTemplateId.ToString( ) );
            }

            RockApi.Get_DefinedValues( oDataFilter, resultHandler );
        }

        const int FamilyGroupTypeId = 10;
        public static void CreateNewFamily( Rock.Client.Group groupModel, HttpRequest.RequestResult resultHandler )
        {
            // give the group model required values
            groupModel.Guid = Guid.NewGuid( );
            groupModel.GroupTypeId = FamilyGroupTypeId;
            groupModel.IsActive = true;
            groupModel.IsPublic = true;

            RockApi.Post_Groups( groupModel, Config.Instance.CurrentPersonAliasId, resultHandler );
        }

        public static void UpdateFullPerson( bool isNewPerson, 
                                             Rock.Client.Person person, 
                                             bool isNewPhoneNumber,
                                             Rock.Client.PhoneNumber phoneNumber, 
                                             List<KeyValuePair<string, string>> attributes, 
                                             MemoryStream personImage, 
                                             HttpRequest.RequestResult resultHandler )
        {
            // first, we need to resolve their graduation year (if they have a valid grade offset set)
            if ( person.GradeOffset.HasValue && person.GradeOffset.Value >= 0 )
            {
                RockApi.Get_People_GraduationYear( person.GradeOffset.Value, 
                    delegate(HttpStatusCode statusCode, string statusDescription, int graduationYear )
                    {
                        // now set that and update the person
                        if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                        {
                            person.GraduationYear = graduationYear;

                            TryUpdatePerson( isNewPerson, person, isNewPhoneNumber, phoneNumber, attributes, personImage, resultHandler );
                        }
                        else
                        {
                            // error 
                            resultHandler( statusCode, statusDescription );
                        }
                    } );
            }
            else
            {
                TryUpdatePerson( isNewPerson, person, isNewPhoneNumber, phoneNumber, attributes, personImage, resultHandler );
            }
        }

        static void TryUpdatePerson( bool isNewPerson, 
            Rock.Client.Person person, 
            bool isNewPhoneNumber,
            Rock.Client.PhoneNumber phoneNumber, 
            List<KeyValuePair<string, string>> attributes, 
            MemoryStream personImage, 
            HttpRequest.RequestResult resultHandler )
        {
            // if they're a new person, flag them as a pending visitor.
            if ( isNewPerson == true )
            {
                person.RecordStatusValueId = Settings.Rock_RecordStatusValueId_Pending;
                person.ConnectionStatusValueId = Settings.Rock_ConnectionStatusValueId_Visitor;
            }
            
            ApplicationApi.UpdateOrAddPerson( person, isNewPerson, Config.Instance.CurrentPersonAliasId, 
                delegate( System.Net.HttpStatusCode statusCode, string statusDescription )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        // was this a new person?
                        if ( isNewPerson == true )
                        {
                            // then we need to get their profile so we know the ID to use for updating the rest of their stuff.
                            TryGetNewlyCreatedProfile( person, isNewPhoneNumber, phoneNumber, attributes, personImage, resultHandler );
                        }
                        else
                        {
                            // now update pending attributes.
                            foreach ( KeyValuePair<string, string> attribValue in attributes )
                            {
                                // just fire and forget these values.
                                FamilyManagerApi.UpdateOrAddPersonAttribute( person.Id, attribValue.Key, attribValue.Value, null );
                            }

                            // are we deleting an existing number?
                            if( string.IsNullOrWhiteSpace( phoneNumber.Number ) == true && isNewPhoneNumber == false )
                            {
                                TryDeleteCellPhone( person, phoneNumber, personImage, resultHandler );
                            }
                            // are we updating or adding an existing?
                            else if( string.IsNullOrWhiteSpace( phoneNumber.Number ) == false )
                            {
                                TryUpdateCellPhone( person, isNewPhoneNumber, phoneNumber, personImage, resultHandler );
                            }
                            else
                            {
                                TryUpdateProfilePic( person, personImage, resultHandler );
                            }
                        }
                    }
                    else
                    {
                        // error
                        resultHandler( statusCode, statusDescription );
                    }
                } );  
        }

        static void TryGetNewlyCreatedProfile( Rock.Client.Person person, 
                                               bool isNewPhoneNumber, 
                                               Rock.Client.PhoneNumber phoneNumber, 
                                               List<KeyValuePair<string, string>> attributes, 
                                               MemoryStream personImage, 
                                               HttpRequest.RequestResult resultHandler )
        {
            ApplicationApi.GetPersonByGuid( person.Guid, 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, Rock.Client.Person model )
                {
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        person = model;

                        // see if we should set their first time visit status
                        if( Config.Instance.RecordFirstVisit == true )
                        {
                            FamilyManagerApi.UpdateOrAddPersonAttribute( person.Id, Config.Instance.FirstTimeVisitAttrib.Key, DateTime.Now.ToString( ), null );
                        }

                        // now update pending attributes.
                        foreach( KeyValuePair<string, string> attribValue in attributes )
                        {
                            // just fire and forget these values.
                            FamilyManagerApi.UpdateOrAddPersonAttribute( person.Id, attribValue.Key, attribValue.Value, null );
                        }

                        // if there's a phone number to send, send it.
                        if( string.IsNullOrWhiteSpace( phoneNumber.Number ) == false )
                        {
                            TryUpdateCellPhone( person, isNewPhoneNumber, phoneNumber, personImage, resultHandler );
                        }
                        else
                        {
                            TryUpdateProfilePic( person, personImage, resultHandler );
                        }
                    }
                    else
                    {
                        resultHandler( statusCode, statusDescription );
                    }
                } );
        }

        static void TryUpdateCellPhone( Rock.Client.Person person, 
                                        bool isNewPhoneNumber, 
                                        Rock.Client.PhoneNumber phoneNumber, 
                                        MemoryStream personImage, 
                                        HttpRequest.RequestResult resultHandler )
        {
            // is their phone number new or existing?
            ApplicationApi.AddOrUpdateCellPhoneNumber( person, phoneNumber, isNewPhoneNumber, Config.Instance.CurrentPersonAliasId,
                delegate( System.Net.HttpStatusCode statusCode, string statusDescription )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        TryUpdateProfilePic( person, personImage, resultHandler );
                    }
                    else
                    {
                        resultHandler( statusCode, statusDescription );
                    }
                } );
        }

        static void TryDeleteCellPhone( Rock.Client.Person person, 
            Rock.Client.PhoneNumber phoneNumber, 
            MemoryStream personImage, 
            HttpRequest.RequestResult resultHandler )
        {
            // remove the current number
            ApplicationApi.DeleteCellPhoneNumber( phoneNumber, 
                delegate( System.Net.HttpStatusCode statusCode, string statusDescription )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        TryUpdateProfilePic( person, personImage, resultHandler );
                    }
                    else
                    {
                        resultHandler( statusCode, statusDescription );
                    }
                } );
        }

        static void TryUpdateProfilePic( Rock.Client.Person person, MemoryStream personImage, HttpRequest.RequestResult resultHandler )
        {
            // is there a picture needing to be uploaded?
            if ( personImage != null )
            {
                // upload
                ApplicationApi.UploadSavedProfilePicture( person, personImage, Config.Instance.CurrentPersonAliasId,
                    delegate( System.Net.HttpStatusCode statusCode, string statusDescription )
                    {
                        resultHandler( statusCode, statusDescription );
                    } );
            }
            else
            {
                resultHandler( HttpStatusCode.OK, "" );
            }
        }

        // UpdateFullFamily will update the family, address and attributes.
        // For simplicity, it's broken into 3 functions. This one,  and two private ones.
        public static void UpdateFullFamily( Rock.Client.Group family, Rock.Client.GroupLocation address, List<KeyValuePair<string, string>> attributes, HttpRequest.RequestResult resultHandler )
        {
            // start by updating the family group
            ApplicationApi.UpdateFamilyGroup( family, Config.Instance.CurrentPersonAliasId,
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                    {
                        UpdateFamilyAddress( family, address, attributes, resultHandler );

                    }
                    else
                    {
                        resultHandler( statusCode, statusDescription );
                    }
                } );
        }

        static void UpdateFamilyAddress( Rock.Client.Group family, Rock.Client.GroupLocation address, List<KeyValuePair<string, string>> attributes, HttpRequest.RequestResult resultHandler )
        {
            // is there an address?
            if ( address != null )
            {
                ApplicationApi.UpdateFamilyAddress( family, address, 
                    delegate(System.Net.HttpStatusCode statusCode, string statusDescription )
                    {
                        // if it updated ok, go to family attributes
                        if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                        {
                            UpdateFamilyAttributes( family, address, attributes, resultHandler );
                        }
                        // address failed
                        else
                        {
                            resultHandler( statusCode, statusDescription );
                        }
                    });
            }
            // no, go to family attrubutes
            else
            {
                UpdateFamilyAttributes( family, address, attributes, resultHandler );
            }
        }

        public static void UpdatePersonRoleInFamily( Rock.Client.GroupMember groupMember, int childAdultGroupRoleId, HttpRequest.RequestResult resultHandler )
        {
            Rock.Client.GroupMember destGroupMember = new Rock.Client.GroupMember();

            // copy over all the other data
            destGroupMember.GroupMemberStatus = groupMember.GroupMemberStatus;
            destGroupMember.PersonId = groupMember.PersonId;
            destGroupMember.Guid = groupMember.Guid;
            destGroupMember.GroupId = groupMember.GroupId;
            destGroupMember.GroupRoleId = childAdultGroupRoleId;
            destGroupMember.Id = groupMember.Id;
            destGroupMember.IsSystem = groupMember.IsSystem;

            RockApi.Put_GroupMembers( destGroupMember, Config.Instance.CurrentPersonAliasId, resultHandler );
        }

        static void UpdateFamilyAttributes( Rock.Client.Group family, Rock.Client.GroupLocation address, List<KeyValuePair<string, string>> attributes, HttpRequest.RequestResult resultHandler )
        {
            // are there attributes?
            int pendingCompleteCount = 0;
            if( attributes != null && attributes.Count > 0 )
            {
                // update each attribute
                foreach ( KeyValuePair<string, string> attribValue in attributes )
                {
                    // just fire and forget these values.
                    FamilyManagerApi.UpdateOrAddFamilyAttribute( family.Id, attribValue.Key, attribValue.Value, 
                        delegate
                        {
                            // once we complete updating them (whether successful or not) we're done.
                            pendingCompleteCount++;
                            if ( pendingCompleteCount == attributes.Count )
                            {
                                resultHandler( HttpStatusCode.OK, "" );
                            }
                        } );
                }
            }
            // no, so we're done
            else
            {
                resultHandler( HttpStatusCode.OK, "" );
            }
        }

        /// <summary>
        /// This gets all the info needed to let Family Manager edit a person. That includes:
        /// The Person info, and a list of all Rock.Client.Family entities that can check this person in.
        /// THAT process involves: 
        /// 1. Getting all GroupMembers with a AllowCheckin relationship.
        /// 2. Getting all families of each GroupMember as Rock.Client.Group
        /// 3. Finally, converting all Rock.Client.Groups to actual Rock.Client.Family
        /// </summary>
        public delegate void PersonEditResponseDelegate( Rock.Client.Person person, List<Rock.Client.Family> allowedCheckinList );
        public static void GetPersonForEdit( Rock.Client.GroupMember groupMember, PersonEditResponseDelegate response )
        {
            // get their attributes before presenting.
            ApplicationApi.GetPersonById( groupMember.Person.Id, true, 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, Rock.Client.Person refreshedPerson )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) && refreshedPerson != null )
                    {
                        // get the people allowed to check them in
                        string query = string.Format( "?personId={0}&relationshipRoleId={1}", groupMember.Person.Id, Config.Instance.AllowedCheckInByRole.Id );

                        RockApi.Get_GroupMembers_KnownRelationships( query, delegate(HttpStatusCode knownRelationshipCode, string knownRelationshipDesc, List<Rock.Client.GroupMember> groupMembers ) 
                            {
                                if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                                {
                                    // and lastly, from that, get the families of these group members
                                    if( groupMembers.Count > 0 )
                                    {
                                        GroupMembersToFamilyGroups( refreshedPerson, groupMembers, response );
                                    }
                                    else
                                    {
                                        response( refreshedPerson, null );
                                    }
                                }
                                else
                                {
                                    response( null, null );
                                }
                            });
                    }
                    else
                    {
                        response( null, null );
                    }
                });
        }

        /// <summary>
        /// Helper function to prevent GetPersonForEdit from getting crazy long. Given a list of Rock.Client.GroupMember,
        /// this gets the all families (as Rock.Client.Group) for each GroupMember.
        /// </summary>
        static void GroupMembersToFamilyGroups( Rock.Client.Person refreshedPerson, List<Rock.Client.GroupMember> groupMembers, PersonEditResponseDelegate response )
        {
            // first, we need to get the families for each group member
            int groupMembersResolved = 0;
            List<Rock.Client.Group> groupFamilies = new List<Rock.Client.Group>( );

            foreach( Rock.Client.GroupMember member in groupMembers )
            {
                RockApi.Get_FamiliesOfPerson( member.PersonId, string.Empty, 
                    delegate(HttpStatusCode statusCode, string statusDescription, List<Rock.Client.Group> groups) 
                    {
                        if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                        {
                            // serialize the responses on the UI thread so we can be sure it's all thread safe
                            Rock.Mobile.Threading.Util.PerformOnUIThread( 
                                delegate
                                {
                                    // first, add the returned groups to our global list.
                                    foreach( Rock.Client.Group group in groups )
                                    {
                                        // make sure we don't already have this group in our list
                                        Rock.Client.Group resultGroup = groupFamilies.Where( g => g.Id == group.Id ).FirstOrDefault( );
                                        if( resultGroup == null )
                                        {
                                            groupFamilies.Add( group );
                                        }
                                    }

                                    // now see if we're done waiting for all responses.
                                    groupMembersResolved++;
                                    if( groupMembersResolved == groupMembers.Count )
                                    {
                                        GroupsToFamilies( groupFamilies, refreshedPerson, response );
                                    }
                                });
                        }
                        // if any of them fail, then fail.
                        else
                        {
                            response( null, null );
                        }
                    });
            }
        }

        /// <summary>
        /// Second helper method for GetPersonForEdit. This basically takes a list of Rock.Client.Group and gets the Rock.Client.Family for each one.
        /// </summary>
        static void GroupsToFamilies( List<Rock.Client.Group> groupFamilies, Rock.Client.Person refreshedPerson, PersonEditResponseDelegate response )
        {
            List<Rock.Client.Family> families = new List<Rock.Client.Family>( );
            int groupsResolved = 0;
            
            // now get all the FAMILY objects for these family GROUPS
            foreach( Rock.Client.Group group in groupFamilies )
            {
                RockApi.Get_Groups_GetFamily( group.Id, 
                    delegate(HttpStatusCode statusCode, string statusDescription, Rock.Client.Family model) 
                    {
                        if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                        {
                            // serialize the responses on the UI thread so we can be sure it's all thread safe
                            Rock.Mobile.Threading.Util.PerformOnUIThread( 
                                delegate
                                {
                                    families.Add( model );

                                    groupsResolved++;
                                    if( groupsResolved == groupFamilies.Count )
                                    {
                                        response( refreshedPerson, families );
                                    }
                                });
                        }
                        else
                        {
                            response( null, null );
                        }
                    });
            }
        }
    }
}
