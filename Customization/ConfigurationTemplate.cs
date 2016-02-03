using System;
using Rock.Mobile.Network;
using RestSharp;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Rock.Mobile.IO;
using FamilyManager.UI;
using FamilyManager;
using Rock.Mobile;

namespace Customization
{
    /// <summary>
    /// Defines the preference and theme for the app.
    /// </summary>
    public class ConfigurationTemplate
    {
        public ConfigurationTemplate( )
        {
            VisualSettings = new Theme();
            FamilyAttributes = new List<Dictionary<string, string>>();
            PersonAttributes = new List<Dictionary<string, string>>();
        }
        
        [JsonProperty]
        public List<Dictionary<string, string>> FamilyAttributes { get; set; }

        [JsonProperty]
        public List<Dictionary<string, string>> PersonAttributes { get; set; }

        [JsonProperty("visualSettings")]
        public Theme VisualSettings { get; set; }

        // true if we should ask the user if they want to flag new people as first time guests.
        [JsonProperty("firstVisitPrompt")]
        public bool FirstVisitPrompt { get; set; }

        [JsonProperty("defaultState")]
        public string DefaultState { get; protected set; }

        /// <summary>
        /// Used to make sure the attributes and their definitions match up.
        /// </summary>
        public void SortAttributeLists( )
        {
            // first, sort the model's attributes
            FamilyAttributes.Sort( delegate(Dictionary<string, string> x, Dictionary<string, string> y )
                {
                    if ( int.Parse( x[ "attributeId" ] ) < int.Parse( y[ "attributeId" ] ) )
                    {
                        return -1;
                    }

                    return 1;
                } );

            PersonAttributes.Sort( delegate(Dictionary<string, string> x, Dictionary<string, string> y )
                {
                    if ( int.Parse( x[ "attributeId" ] ) < int.Parse( y[ "attributeId" ] ) )
                    {
                        return -1;
                    }

                    return 1;
                } );
        }

        /// <summary>
        /// This will download the latest version and update if there's a difference
        /// ConfigurationTemplateId should be the template Id for this configuration
        /// </summary>
        public delegate void OnTemplateUpdated( bool result, Rock.Client.DefinedValue templateDefinedValue );
        public static void UpdateTemplate( Rock.Client.DefinedValue templateDefinedValue, OnTemplateUpdated onUpdated )
        {
            // get the Type and Specific Ids so we can request this specific template.
            int typeId = TemplateTypeId( templateDefinedValue );
            int uniqueId = TemplateUniqueId( templateDefinedValue );
            
            FamilyManagerApi.GetConfigurationTemplates( typeId, uniqueId,
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.DefinedValue> definedValueModels )
                {
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true && definedValueModels != null && definedValueModels.Count > 0 )
                    {
                        // extract the ConfigurationTemplate itself from both, and compare them.
                        ConfigurationTemplate currentTemplate = Template( templateDefinedValue );
                        currentTemplate.SortAttributeLists( );

                        ConfigurationTemplate downloadedTemplate = Template( definedValueModels[ 0 ] );
                        downloadedTemplate.SortAttributeLists( );

                        // is it different?
                        string currentVersion = JsonConvert.SerializeObject( currentTemplate );
                        string downloadedVersion = JsonConvert.SerializeObject( downloadedTemplate );

                        if ( string.Compare( currentVersion, downloadedVersion ) != 0 )
                        {
                            // they're different, so provide the latest one
                            onUpdated( true, definedValueModels[ 0 ] );
                        }
                        else
                        {
                            onUpdated( true, null );
                        }
                    }
                    else
                    {
                        onUpdated( false, null );
                    }
                } );
        }

        public delegate void TemplatesDownloaded( List<Rock.Client.DefinedValue> templateDefinedValues );
        public static void DownloadConfigurationTemplates( TemplatesDownloaded onDownloaded )
        {
            // first resolve the Guid to the correct type ID
            ApplicationApi.GetDefinedTypeIdForGuid( FamilyManagerApi.ConfigurationTemplateDefinedTypeGuid,
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.DefinedType> definedTypeModel ) 
                {
                    // if the request for the defined type worked
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true && definedTypeModel != null && definedTypeModel.Count > 0 )
                    {
                        // now get the actual values
                        FamilyManagerApi.GetConfigurationTemplates( definedTypeModel[ 0 ].Id, 0,
                            delegate(System.Net.HttpStatusCode configStatusCode, string configStatusDescription, List<Rock.Client.DefinedValue> definedValueModels ) 
                            {
                                if( Rock.Mobile.Network.Util.StatusInSuccessRange( configStatusCode ) == true && definedValueModels != null )
                                {
                                    onDownloaded( definedValueModels );   
                                }
                                else
                                {
                                    // fail
                                    onDownloaded( null );
                                }
                            });
                    }
                    else
                    {
                        // fail
                        onDownloaded( null );
                    }
                });
        }

        /// <summary>
        /// Extract the actual ConfigurationTemplate from the defined value
        /// </summary>
        public static ConfigurationTemplate Template( Rock.Client.DefinedValue templateDefinedValue )
        {
            ConfigurationTemplate configTemplate = null;
            if ( templateDefinedValue.AttributeValues != null )
            {
                string configString = templateDefinedValue.AttributeValues[ "ConfigurationTemplate" ].Value;

                try
                {
                    configTemplate = JsonConvert.DeserializeObject<ConfigurationTemplate>( configString ) as ConfigurationTemplate;
                }
                catch
                {
                    Console.WriteLine( "WARNING! Configuration Template Deserialization FAILED!" );
                    configTemplate = null;
                }
            }
            else
            {
                Console.WriteLine( "WARNING! Configuration Template Defined Value is EMPTY!" );
            }

            // if we failed to get it (maybe the templateDefinedValue is blank?) return an empty one.
            return configTemplate != null ? configTemplate : new ConfigurationTemplate( );
        }

        /// <summary>
        /// Extract the Template's unique ID
        /// </summary>
        public static int TemplateUniqueId( Rock.Client.DefinedValue templateDefinedValue )
        {
            return templateDefinedValue.Id;
        }

        /// <summary>
        /// Extract the Template's TYPE ID ( same for all ConfigurationTemplates)
        /// </summary>
        public static int TemplateTypeId( Rock.Client.DefinedValue templatedDefinedValue )
        {
            return templatedDefinedValue.DefinedTypeId;
        }

        /// <summary>
        /// Extract the template's user readable name
        /// </summary>
        public static string TemplateName( Rock.Client.DefinedValue templateDefinedValue )
        {
            return templateDefinedValue.Value;
        }
    }
}
