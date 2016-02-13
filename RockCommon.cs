using System;
using System.Collections.Generic;
using Rock.Mobile.Util.Strings;
using Rock.Mobile.Network;
using System.IO;
using Rock.Mobile.IO;
using Rock.Client;
using Rock.Mobile.Util;
using System.Net;
using Newtonsoft.Json;
using Rock.Mobile;

namespace FamilyManager
{
    //TODO: MOVE THIS OUT OF HERE AND CCV MOBILE AND INTO SOMETHING COMMON
    public static class RockActions
    {
        public static void SetBirthday( Rock.Client.Person person, DateTime? birthday )
        {
            // update the birthdate field
            person.BirthDate = birthday;

            // and the day/month/year fields.
            if ( person.BirthDate.HasValue )
            {
                person.BirthDay = person.BirthDate.Value.Day;
                person.BirthMonth = person.BirthDate.Value.Month;
                person.BirthYear = person.BirthDate.Value.Year;
            }
            else
            {
                person.BirthDay = null;
                person.BirthMonth = null;
                person.BirthYear = null;
            }
        }

        public static void SetPhoneNumberDigits( Rock.Client.PhoneNumber phoneNumber, string digits )
        { 
            phoneNumber.Number = digits;
            phoneNumber.NumberFormatted = digits.AsPhoneNumber( );
        }

        public static Rock.Client.GroupLocation CreateHomeAddress( string street, string city, string state, string zip )
        {
            // try to find the group, and if it doesn't exist, make it
            Rock.Client.GroupLocation homeLocation = new Rock.Client.GroupLocation();
            homeLocation.GroupLocationTypeValueId = GroupLocationTypeHomeValueId;

            // for the address location, default the country to the built in country code.
            homeLocation.Location = new Rock.Client.Location();
            homeLocation.Location.Country = CountryCode;

            // populate it
            homeLocation.Location.Street1 = street;
            homeLocation.Location.City = city;
            homeLocation.Location.State = state;
            homeLocation.Location.PostalCode = zip;

            // return it.
            return homeLocation;
        }

        public static Rock.Client.GroupLocation GetFamilyHomeAddress( Rock.Client.Group family )
        {
            // look at each location within the family
            foreach ( Rock.Client.GroupLocation groupLocation in family.GroupLocations )
            {
                // find their "Home Location" within the family group type.
                if ( groupLocation.GroupLocationTypeValue.Guid.ToString( ).ToLower( ) == Rock.Client.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.ToLower( ) )
                {
                    return groupLocation;
                }
            }

            return null;
        }

        public static bool IsFullAddress( Rock.Client.GroupLocation address )
        {
            // by full address, we mean street, city, state, zip
            if ( string.IsNullOrWhiteSpace( address.Location.Street1 ) == false &&
                 string.IsNullOrWhiteSpace( address.Location.City ) == false &&
                 string.IsNullOrWhiteSpace( address.Location.State ) == false &&
                 string.IsNullOrWhiteSpace( address.Location.PostalCode ) == false )
            {
                return true;
            }
            return false;
        }

        public const int CellPhoneValueId = 12;
        public static Rock.Client.PhoneNumber TryGetPhoneNumber( Rock.Client.Person person, int phoneTypeId )
        {
            Rock.Client.PhoneNumber requestedNumber = null;

            // if the user has phone numbers
            if ( person.PhoneNumbers != null )
            {
                // get an enumerator
                IEnumerator<Rock.Client.PhoneNumber> enumerator = person.PhoneNumbers.GetEnumerator( );
                enumerator.MoveNext( );

                // search for the phone number type requested
                while ( enumerator.Current != null )
                {
                    Rock.Client.PhoneNumber phoneNumber = enumerator.Current as Rock.Client.PhoneNumber;

                    // is this the right type?
                    if ( phoneNumber.NumberTypeValueId == phoneTypeId )
                    {
                        requestedNumber = phoneNumber;
                        break;
                    }
                    enumerator.MoveNext( );
                }
            }

            return requestedNumber;
        }


        public static List<string> Genders { get; set; }

        static RockActions( )
        {
            Genders = new List<string>( );
            Genders.Add( "Unknown" );
            Genders.Add( "Male" );
            Genders.Add( "Female" );
        }

        /// <summary>
        /// These are values that, while generated when the Rock database is created,
        /// are extremely unlikely to ever change. If they do change, simply update them here to match
        /// Rock.
        /// </summary>

        public const int NeighborhoodGroupGeoFenceValueId = 48;
        public const int NeighborhoodGroupValueId = 49;
        public const int GroupLocationTypeHomeValueId = 19;
        public const int PersonConnectionStatusValueId = 146;
        public const int PersonRecordStatusValueId = 5;
        public const string CountryCode = "US";
    }
}

