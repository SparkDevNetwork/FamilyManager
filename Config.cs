using System;
using Rock.Mobile.Network;
using RestSharp;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Rock.Mobile.IO;
using FamilyManager.UI;
using Customization;

namespace FamilyManager
{
    /// <summary>
    /// App configuration class that manages app settings, downloaded rock values (marital status, campuses, etc.) and
    /// the configurationTemplates, which define the appearance and certain preferences.
    /// </summary>
    public class Config
    {
        const string FileName = "config.dat";

        static Config _Instance = new Config( );
        public static Config Instance { get { return _Instance; } }

        public Config( )
        {
            FamilyAttributeDefines = new List<Rock.Client.Attribute>();
            PersonAttributeDefines = new List<Rock.Client.Attribute>();
            CurrentConfigurationTemplate = new Rock.Client.DefinedValue();

            // default recording first visits to true.
            RecordFirstVisit = true;
        }

        /// <summary>
        /// This is the defined template we're currently using. It's basically
        /// the wrapper within which a ConfigurationTemplate exists, and has meta data
        /// like its Rock Database Id.
        /// </summary>
        [JsonProperty]
        Rock.Client.DefinedValue CurrentConfigurationTemplate { get; set; }

        /// <summary>
        /// The extracted CurrentTemplate, taken out of the CurrentConfigurationTemplate above. 
        /// We store this for convenience
        /// </summary>
        /// <value>The current template.</value>
        [JsonIgnore]
        ConfigurationTemplate CurrentTemplate
        { 
            get
            {
                return ConfigurationTemplate.Template( CurrentConfigurationTemplate );
            }
        }

        /// <summary>
        /// The family attributes above tell me which attributes to go get from Rock, and then 
        /// store here.
        /// </summary>
        public List<Rock.Client.Attribute> FamilyAttributeDefines { get; set; }

        /// <summary>
        /// The person attributes above tell me which attributes to go get from Rock, and then 
        /// store here.
        /// </summary>
        public List<Rock.Client.Attribute> PersonAttributeDefines { get; set; }

        /// <summary>
        /// The URL to reach Rock.
        /// </summary>
        /// <value>The rock UR.</value>
        public string RockURL { get; set; }

        // The key used for hitting end points on Rock. The user will still have to pass a login check.
        //public string RockAuthorizationKey = "fknuHvzQS7tFK2XN7tJ5jhqS";
        public string RockAuthorizationKey { get; set; }

        // Campuses available
        [JsonProperty]
        public int SelectedCampusIndex { get; set; }

        [JsonProperty]
        public List<Rock.Client.Campus> Campuses { get; protected set; }

        public void SetCampuses( List<Rock.Client.Campus> campuses )
        {
            SelectedCampusIndex = 0;
            Campuses = campuses;
        }

        [JsonProperty]
        public List<Rock.Client.DefinedValue> MaritalStatus { get; set; }

        [JsonProperty]
        public List<Rock.Client.DefinedValue> SchoolGrades { get; set; }

        [JsonProperty]
        public Rock.Client.GroupTypeRole FamilyMemberChildGroupRole { get; set; }

        [JsonProperty]
        public Rock.Client.GroupTypeRole FamilyMemberAdultGroupRole { get; set; }

        [JsonProperty]
        public Rock.Client.Attribute FirstTimeVisitAttrib { get; set; }

        [JsonProperty]
        public Rock.Client.GroupTypeRole CanCheckInGroupRole { get; set; }

        [JsonProperty]
        public List<Rock.Client.DefinedValue> ConfigurationTemplates { get; set; }

        [JsonIgnore]
        public Theme VisualSettings { get { return CurrentTemplate.VisualSettings; } }

        // This simply determines whether the app should ask about recording first visits.
        [JsonIgnore]
        public bool FirstVisitPrompt { get { return CurrentTemplate.FirstVisitPrompt; } }

        // This is the person alias ID for the currently logged in user.
        [JsonIgnore]
        public int CurrentPersonAliasId { get; set; }

        // This is set based on the user's response to FirstVisitPrompt. The default is true.
        [JsonIgnore]
        public bool RecordFirstVisit { get; set; }

        [JsonIgnore]
        public string DefaultState { get { return CurrentTemplate.DefaultState; } }

        [JsonIgnore]
        public List<Dictionary<string, string>> FamilyAttributes { get { return CurrentTemplate.FamilyAttributes; } }

        [JsonIgnore]
        public List<Dictionary<string, string>> PersonAttributes { get { return CurrentTemplate.PersonAttributes; } }

        [JsonIgnore]
        public int ConfigurationTemplateId { get { return CurrentConfigurationTemplate.Id; } }

        public bool LoadFromDevice( )
        {
            bool success = false;

            // load the latest version
            string filePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), FileName);

            // if the file exists
            if ( System.IO.File.Exists( filePath ) == true )
            {
                // read it
                using ( StreamReader reader = new StreamReader( filePath ) )
                {
                    string json = reader.ReadLine( );

                    try
                    {
                        // guard against the LaunchData changing and the user having old data.
                        _Instance = JsonConvert.DeserializeObject<Config>( json ) as Config;

                        success = true;
                    }
                    catch( Exception )
                    {
                        // if it fails for some reason, just create a new one
                        _Instance = new Config();
                    }
                }
            }

            return success;
        }

        public void SaveToDevice( )
        {
            string filePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), FileName);

            // open a stream
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                string json = JsonConvert.SerializeObject( _Instance );
                writer.WriteLine( json );
            }
        }

        public delegate void OnComplete( bool result );
        string TempRockUrl { get; set; }
        string TempAuthKey { get; set; }
        public void TryBindToRockServer( string rockUrl, string authKey, OnComplete onComplete )
        {
            TempRockUrl = rockUrl;
            TempAuthKey = authKey;

            // this will let us use the url temporarily
            RockApi.SetRockURL( rockUrl );
            RockApi.SetAuthorizationKey( authKey );

            // get the config templates
            GetConfigTemplates( 

                // capute the result callback
                delegate( bool result )
                {
                    // restore the original URLs, so that they only update
                    // if Commit is called.
                    RockApi.SetRockURL( RockURL );
                    RockApi.SetAuthorizationKey( RockAuthorizationKey );

                    // now notify the original caller
                    onComplete( result );
                } );
        }

        public List<Rock.Client.DefinedValue> TempConfigurationTemplates { get; set; }
        void GetConfigTemplates( OnComplete onComplete )
        {
            ConfigurationTemplate.DownloadConfigurationTemplates( 
                delegate( List<Rock.Client.DefinedValue> model )
                {
                    if ( model != null )
                    {
                        TempConfigurationTemplates = model;

                        GetCampuses( onComplete );
                    }
                    // Config Templates Failed
                    else
                    {
                        onComplete( false );
                    }
                } );
        }

        public List<Rock.Client.Campus> TempCampuses { get; set; }
        void GetCampuses( OnComplete onComplete )
        {
            // get the campuses
            RockApi.Get_Campuses( 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.Campus> model )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        TempCampuses = model;

                        GetMaritalValues( onComplete );
                    }
                    // Campuses failed
                    else
                    {
                        onComplete( false );
                    }
                } );
        }

        List<Rock.Client.DefinedValue> TempMaritalStatus { get; set; }
        void GetMaritalValues( OnComplete onComplete )
        {
            ApplicationApi.GetDefinedValuesForDefinedType( Rock.Client.SystemGuid.DefinedType.PERSON_MARITAL_STATUS, 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.DefinedValue> model )
                {
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        TempMaritalStatus = model;

                        GetSchoolGrades( onComplete );
                    }
                    // Marital Status Failed
                    else
                    {
                        onComplete( false );
                    }
                } );
        }

        List<Rock.Client.DefinedValue> TempSchoolGrades { get; set; }
        void GetSchoolGrades( OnComplete onComplete )
        {
            ApplicationApi.GetDefinedValuesForDefinedType( Rock.Client.SystemGuid.DefinedType.SCHOOL_GRADES, 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.DefinedValue> model )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        TempSchoolGrades = model;

                        GetChildGroupTypeRole( onComplete );
                    }
                    // School Grades Failed
                    else
                    {
                        onComplete( false );
                    }
                } );
        }

        Rock.Client.GroupTypeRole TempChildRole { get; set; }
        void GetChildGroupTypeRole( OnComplete onComplete )
        {
            ApplicationApi.GetGroupTypeRoleForGuid( Rock.Client.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD,
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.GroupTypeRole> model )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true && model != null && model.Count > 0 )
                    {
                        TempChildRole = model[ 0 ];

                        GetAdultGroupTypeRole( onComplete );
                    }
                    // Child Role Failed
                    else
                    {
                        onComplete( false );
                    }
                } );
        }

        Rock.Client.GroupTypeRole TempAdultRole { get; set; }
        void GetAdultGroupTypeRole( OnComplete onComplete )
        {
            ApplicationApi.GetGroupTypeRoleForGuid( Rock.Client.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT,
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.GroupTypeRole> model )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true && model != null && model.Count > 0 )
                    {
                        TempAdultRole = model[ 0 ];

                        GetFirstTimeVisitAttribValue( onComplete );
                        //GetCanCheckInGroupTypeRole( onComplete );
                    }
                    // Adult Role Failed
                    else
                    {
                        onComplete( false );
                    }
                } );
        }

        Rock.Client.Attribute TempFirstTimeVisit { get; set; }
        void GetFirstTimeVisitAttribValue( OnComplete onComplete )
        {
            ApplicationApi.GetAttributeForGuid( "655D6FBA-F8C0-4919-9E31-C1C936653555",
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, Rock.Client.Attribute model )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true && model != null )
                    {
                        TempFirstTimeVisit = model;

                        GetCanCheckInGroupTypeRole( onComplete );
                    }
                    // Child Role Failed
                    else
                    {
                        onComplete( false );
                    }
                } );
        }

        Rock.Client.GroupTypeRole TempCanCheckInRole { get; set; }
        void GetCanCheckInGroupTypeRole( OnComplete onComplete )
        {
            ApplicationApi.GetGroupTypeRoleForGuid( Rock.Client.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_CAN_CHECK_IN,
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.GroupTypeRole> model )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true && model != null && model.Count > 0 )
                    {
                        TempCanCheckInRole = model[ 0 ];

                        // Note: add more stuff here if you need.
                        onComplete( true );
                    }
                    // Can Check In Role Failed
                    else
                    {
                        onComplete( false );
                    }
                } );
        }

        public void CommitRockSync( )
        {
            // Everything worked, so store all our values
            RockURL = TempRockUrl;

            RockAuthorizationKey = TempAuthKey;

            MaritalStatus = TempMaritalStatus;

            SchoolGrades = TempSchoolGrades;

            FamilyMemberChildGroupRole = TempChildRole;

            FamilyMemberAdultGroupRole = TempAdultRole;

            FirstTimeVisitAttrib = TempFirstTimeVisit;

            CanCheckInGroupRole = TempCanCheckInRole;

            ConfigurationTemplates = TempConfigurationTemplates;

            SetCampuses( TempCampuses );

            // permenantly update the api values.
            RockApi.SetRockURL( RockURL );
            RockApi.SetAuthorizationKey( RockAuthorizationKey );
        }


        public delegate void ConfigurationSet( bool result );
        public void SetConfigurationDefinedValue( Rock.Client.DefinedValue configDefinedValue, ConfigurationSet onConfigSet )
        {
            // extract the config template from the defined value
            ConfigurationTemplate configTemplate = ConfigurationTemplate.Template( configDefinedValue );

            configTemplate.VisualSettings.RemoveDownloadedImages( );

            // first get the theme images
            configTemplate.VisualSettings.DownloadImages( RockApi.BaseUrl,
                delegate(bool imageResult )
                {
                    if( imageResult == true )
                    {
                        // start by sorting our attributes so we get the defined values correctly
                        configTemplate.SortAttributeLists( );

                        // get the family attribute definitions
                        DownloadAttributeDefinitions( configTemplate.FamilyAttributes, delegate( List<Rock.Client.Attribute> familyAttribDefines )
                            {
                                if ( familyAttribDefines != null )
                                {                                        
                                    // finally, download the PERSON attribute definitions
                                    DownloadAttributeDefinitions( configTemplate.PersonAttributes, delegate( List<Rock.Client.Attribute> personAttribDefines )
                                        {
                                            // it worked! we're done.
                                            if ( personAttribDefines != null )
                                            {
                                                // it all worked, so store our values and save!
                                                CurrentConfigurationTemplate = configDefinedValue;

                                                _Instance.FamilyAttributeDefines = familyAttribDefines;
                                                _Instance.PersonAttributeDefines = personAttribDefines;

                                                // save to the device
                                                SaveToDevice( );
                                            }

                                            // and either way, return the result
                                            onConfigSet( personAttribDefines != null ? true : false );
                                        } );
                                }
                                // failed to download family attributes
                                else
                                {
                                    onConfigSet( false );
                                }
                            } );
                    }
                    // failed to get the images
                    else
                    {
                        onConfigSet( false );
                    }
                } );
        }

        public void UpdateCurrentConfigurationDefinedValue( ConfigurationSet onConfigSet )
        {
            // attempt to update our current config.
            ConfigurationTemplate.UpdateTemplate( CurrentConfigurationTemplate, 
                delegate(bool result, Rock.Client.DefinedValue templateDefinedValue )
                {
                    // if we got something back, continue and try to take it.
                    // if result is true and templateDefinedValue is null, that's fine, it means there isn't a new template.
                    // if reslut is false, we'll return that and fail.
                    if ( result && templateDefinedValue != null )
                    {
                        // now invoke the SetConfig, which will make sure we get all the dependencies we need.
                        SetConfigurationDefinedValue( templateDefinedValue, onConfigSet );
                    }
                    else
                    {
                        onConfigSet( result );
                    }
                } );
        }

        /// <summary>
        /// Broke attribute definition downloading into its own function
        /// for simplicity
        /// </summary>
        delegate void AttribDefinitionDownloaded( List<Rock.Client.Attribute> attribList );
        void DownloadAttributeDefinitions( List<Dictionary<string, string>> attribList, AttribDefinitionDownloaded onCompletion )
        {
            // now build an array of the attributeIds we should request
            int[] attributeIds = new int[ attribList.Count ];
            for ( int i = 0; i < attribList.Count; i++ )
            {
                attributeIds[ i ] = int.Parse( attribList[ i ][ "attributeId" ] );
            }
            
            // and lastly, request them
            if ( attribList.Count > 0 )
            {
                ApplicationApi.GetAttribute( attributeIds, 
                    delegate(HttpStatusCode attribStatusCode, string attribStatusDescription, List<Rock.Client.Attribute> attribModel )
                    {
                        if ( attribModel != null && Rock.Mobile.Network.Util.StatusInSuccessRange( attribStatusCode ) )
                        {
                            Rock.Mobile.Util.Debug.WriteLine( "Got attrib" );
                            onCompletion( attribModel );
                        }
                        else
                        {
                            onCompletion( null );
                        }
                    } );
            }
            else
            {
                onCompletion( new List<Rock.Client.Attribute>( ) );
            }
        }
    }
}
