using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using TextCopy;
using ArtificialTestValues.Name;
using ArtificialTestValues.Phone;
using ArtificialTestValues.Address;


#if !MOCK_SDK
// Using both namespace patterns to support different SDK versions
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Crm.CoreService.NuGet.Standard;
using Crm.CoreService.NuGet.Standard.Enums;
using Crm.CoreService.NuGet.Standard.Interfaces;
using Crm.CoreService.NuGet.Standard.Models;
#endif

namespace Icharus
{
    public class Program
    {
        // Configuration values from App.config
        private static string ApplicationToken = string.Empty;
        private static string BusinessUnitValue = string.Empty;
        private static string CrmServiceEnvironment = string.Empty;
        
        // Team GUID for lead ownership
        private static string LeadTeamId = string.Empty;
        
        // Realty Agent GUID (equivalent to Loan Officer for Realty)
        private static string RealtyAgent = string.Empty;
        
        // Available environments
        private const string ENV_DEV = "DEV";
        private const string ENV_UAT = "UAT";
        private const string ENV_PROD = "PROD";
        
        // Default environment
        private const string DEFAULT_ENVIRONMENT = ENV_UAT;
        
        // Flag to determine if we're running in mock mode (for testing without CRM SDK)
        private static readonly bool UseMockImplementation = 
#if MOCK_SDK
            true;
#else
            false;
#endif
            
#if MOCK_SDK
        // Mock implementation of BusinessUnit enum for testing
        private enum BusinessUnit
        {
            CRM,
            VUHL,
            VUR
        }
#endif
        
        static void Main(string[] args)
        {
            Console.WriteLine("Icharus Realty CRM Lead Creation Tool");
            Console.WriteLine("=====================================");
            
            // Check if help was requested
            if (args.Length > 0 && (args[0].ToLower() == "--help" || args[0].ToLower() == "-h"))
            {
                DisplayUsage();
                return;
            }
            
            // Parse command line arguments for environment and lead information
            var parameters = ParseCommandLineArgs(args);
            string environment = parameters.Environment;
            string firstName = parameters.FirstName;
            string lastName = parameters.LastName;
            string address1_line1 = parameters.address1_line1;
            string address1_city = parameters.address1_city;
            string address1_stateorprovince = parameters.address1_stateorprovince;
            string address1_postalcode = parameters.address1_postalcode;
            string emailaddress1 = parameters.emailaddress1;
            string rss_mostrecentphonenumber = parameters.rss_mostrecentphonenumber;
            string telephone1 = parameters.telephone1;
            string telephone2 = parameters.telephone2;
            string mobilephone = parameters.mobilephone;
            string vu_purchaselocationstate = parameters.vu_purchaselocationstate;
            string vu_purchaselocationcity = parameters.vu_purchaselocationcity;
            string vu_purchaselocationzip = parameters.vu_purchaselocationzip;
            string vu_subjectcity = parameters.vu_subjectcity;
            string vu_subjectstate = parameters.vu_subjectstate;
            string vu_subjectzipcode = parameters.vu_subjectzipcode;
            string vu_vurpurpose = parameters.vu_vurpurpose;
            
            // Load configuration for the specified environment
            LoadEnvironmentConfiguration(environment);
            
            Console.WriteLine($"Using environment: {environment}");
            
            // Validate configuration settings if not using mock implementation
            if (!UseMockImplementation && !ValidateConfigSettings())
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine($"Environment: {environment}, RealtyAgent: {RealtyAgent}");
            
            // Ensure the correct RealtyAgent GUID is used for each environment
            EnsureCorrectRealtyAgentGuid();
            
            // Generate test data for any parameters not provided
            var testData = GenerateTestLeadData();
            
            // Only use test data for parameters that weren't provided
            if (string.IsNullOrEmpty(firstName))
                firstName = testData.FirstName;
            if (string.IsNullOrEmpty(lastName))
                lastName = testData.LastName;
            if (string.IsNullOrEmpty(address1_line1))
                address1_line1 = testData.Address1_Line1;
            if (string.IsNullOrEmpty(address1_city))
                address1_city = testData.Address1_City;
            if (string.IsNullOrEmpty(address1_stateorprovince))
                address1_stateorprovince = testData.Address1_StateOrProvince;
            if (string.IsNullOrEmpty(address1_postalcode))
                address1_postalcode = testData.Address1_PostalCode;
            if (string.IsNullOrEmpty(emailaddress1))
                emailaddress1 = testData.EmailAddress1;
            if (string.IsNullOrEmpty(rss_mostrecentphonenumber))
                rss_mostrecentphonenumber = testData.Vu_MostRecentPhoneNumber;
            if (string.IsNullOrEmpty(telephone1))
                telephone1 = testData.Telephone2;
            if (string.IsNullOrEmpty(telephone2))
                telephone2 = testData.Telephone3;
            if (string.IsNullOrEmpty(mobilephone))
                mobilephone = testData.MobilePhone;
            if (string.IsNullOrEmpty(vu_purchaselocationstate))
                vu_purchaselocationstate = testData.Vu_PurchaseLocationState;
            if (string.IsNullOrEmpty(vu_purchaselocationcity))
                vu_purchaselocationcity = testData.Address1_City; // Use random city for purchase location city
            if (string.IsNullOrEmpty(vu_purchaselocationzip))
                vu_purchaselocationzip = testData.Vu_PurchaseLocationZip;
            if (string.IsNullOrEmpty(vu_subjectcity))
                vu_subjectcity = testData.Address1_City; // Use random city for subject city
            if (string.IsNullOrEmpty(vu_subjectstate))
                vu_subjectstate = "MO"; // Default to MO
            if (string.IsNullOrEmpty(vu_subjectzipcode))
                vu_subjectzipcode = "00920"; // Default to 00920
            if (string.IsNullOrEmpty(vu_vurpurpose))
                vu_vurpurpose = "827230000"; // Default to 827230000
            
            Console.WriteLine($"Creating lead for {firstName} {lastName}");
            
            // Create the lead in CRM
            string leadId = CreateCrmLead(firstName, lastName, address1_line1, address1_city, 
                address1_stateorprovince, address1_postalcode, emailaddress1, rss_mostrecentphonenumber, 
                telephone1, telephone2, mobilephone, vu_purchaselocationstate, vu_purchaselocationcity, vu_purchaselocationzip,
                vu_subjectcity, vu_subjectstate, vu_subjectzipcode, vu_vurpurpose);
            
            if (!string.IsNullOrEmpty(leadId))
            {
                // Copy lead ID to clipboard
                try
                {
                    ClipboardService.SetText(leadId);
                    Console.WriteLine($"Lead created successfully with ID: {leadId} (copied to clipboard)");
                }
                catch (Exception clipboardEx)
                {
                    Console.WriteLine($"Lead created successfully with ID: {leadId}");
                    Console.WriteLine($"Note: Could not copy to clipboard: {clipboardEx.Message}");
                }
            }
            else
            {
                Console.WriteLine("Failed to create lead in CRM");
            }
        }
        
        /// <summary>
        /// Displays usage information for the application
        /// </summary>
        private static void DisplayUsage()
        {
            Console.WriteLine("Usage: CreateRealtyLead.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --env, -e <environment>    Environment to use (DEV, UAT, PROD). Default: UAT");
            Console.WriteLine("  --firstname, -f <firstName>    Set the lead's first name. Default: Random");
            Console.WriteLine("  --lastname, -l <lastName>      Set the lead's last name. Default: Random");
            Console.WriteLine("  --address1_line1 <address>     Set the lead's street address. Default: Random");
            Console.WriteLine("  --address1_city <city>         Set the lead's city. Default: Random");
            Console.WriteLine("  --address1_stateorprovince <state>  Set the lead's state. Default: CA");
            Console.WriteLine("  --address1_postalcode <zipCode>  Set the lead's zip code. Default: 00920");
            Console.WriteLine("  --emailaddress1 <email>        Set the lead's email. Default: firstname.lastname@example.com");
            Console.WriteLine("  --rss_mostrecentphonenumber <phone>  Set the lead's primary phone. Default: Random");
            Console.WriteLine("  --telephone1 <phone>           Set the lead's home phone. Default: Random");
            Console.WriteLine("  --telephone2 <phone>           Set the lead's work phone. Default: Random");
            Console.WriteLine("  --mobilephone <phone>          Set the lead's mobile phone. Default: Random");
            Console.WriteLine("  --vu_purchaselocationstate <state>  Set the lead's purchase location state. Default: MO");
            Console.WriteLine("  --vu_purchaselocationcity <city>   Set the lead's purchase location city. Default: Random");
            Console.WriteLine("  --vu_purchaselocationzip <zipCode>  Set the lead's purchase location zip code. Default: 00920");
            Console.WriteLine("  --vu_subjectcity <city>         Set the lead's subject city. Default: Random");
            Console.WriteLine("  --vu_subjectstate <state>       Set the lead's subject state. Default: MO");
            Console.WriteLine("  --vu_subjectzipcode <zipCode>   Set the lead's subject zip code. Default: 00920");
            Console.WriteLine("  --vu_vurpurpose <purpose>       Set the lead's VUR purpose. Default: 827230000");
            Console.WriteLine("  --help, -h                 Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  CreateRealtyLead.exe --env DEV");
            Console.WriteLine("  CreateRealtyLead.exe --firstname John --lastname Doe --emailaddress1 john.doe@example.com");
            Console.WriteLine("  CreateRealtyLead.exe --env PROD --firstname Jane --lastname Smith --address1_city \"Chicago\" --address1_stateorprovince IL");
            Console.WriteLine("  CreateRealtyLead.exe --address1_line1 \"456 Oak Ave\" --address1_city Boston --address1_stateorprovince MA --address1_postalcode 02108");
        }
        
        /// <summary>
        /// Parses command line arguments and returns lead parameters
        /// </summary>
        private static (string Environment, string FirstName, string LastName, string address1_line1, 
            string address1_city, string address1_stateorprovince, string address1_postalcode, 
            string emailaddress1, string rss_mostrecentphonenumber, string telephone1, string telephone2, 
            string mobilephone, string vu_purchaselocationstate, string vu_purchaselocationcity, string vu_purchaselocationzip,
            string vu_subjectcity, string vu_subjectstate, string vu_subjectzipcode, string vu_vurpurpose) ParseCommandLineArgs(string[] args)
        {
            string environment = DEFAULT_ENVIRONMENT;
            string firstName = string.Empty;
            string lastName = string.Empty;
            string address1_line1 = string.Empty;
            string address1_city = string.Empty;
            string address1_stateorprovince = string.Empty;
            string address1_postalcode = string.Empty;
            string emailaddress1 = string.Empty;
            string rss_mostrecentphonenumber = string.Empty;
            string telephone1 = string.Empty;
            string telephone2 = string.Empty;
            string mobilephone = string.Empty;
            string vu_purchaselocationstate = string.Empty;
            string vu_purchaselocationcity = string.Empty;
            string vu_purchaselocationzip = string.Empty;
            string vu_subjectcity = string.Empty;
            string vu_subjectstate = string.Empty;
            string vu_subjectzipcode = string.Empty;
            string vu_vurpurpose = string.Empty;
            
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                
                // Parse environment parameter
                if (arg == "--env" || arg == "-e")
                {
                    if (i + 1 < args.Length)
                    {
                        string envValue = args[i + 1].ToUpper();
                        if (envValue == ENV_DEV || envValue == ENV_UAT || envValue == ENV_PROD)
                        {
                            environment = envValue;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Invalid environment '{args[i + 1]}'. Using default: {DEFAULT_ENVIRONMENT}");
                        }
                        i++; // Skip the next argument since we've processed it
                    }
                }
                // Parse lead information parameters
                else if ((arg == "--firstname" || arg == "-f") && i + 1 < args.Length)
                {
                    firstName = args[i + 1];
                    i++;
                }
                else if ((arg == "--lastname" || arg == "-l") && i + 1 < args.Length)
                {
                    lastName = args[i + 1];
                    i++;
                }
                else if (arg == "--address1_line1" && i + 1 < args.Length)
                {
                    address1_line1 = args[i + 1];
                    i++;
                }
                else if (arg == "--address1_city" && i + 1 < args.Length)
                {
                    address1_city = args[i + 1];
                    i++;
                }
                else if (arg == "--address1_stateorprovince" && i + 1 < args.Length)
                {
                    address1_stateorprovince = args[i + 1];
                    i++;
                }
                else if (arg == "--address1_postalcode" && i + 1 < args.Length)
                {
                    address1_postalcode = args[i + 1];
                    i++;
                }
                else if (arg == "--emailaddress1" && i + 1 < args.Length)
                {
                    emailaddress1 = args[i + 1];
                    i++;
                }
                else if (arg == "--rss_mostrecentphonenumber" && i + 1 < args.Length)
                {
                    rss_mostrecentphonenumber = args[i + 1];
                    i++;
                }
                else if (arg == "--telephone1" && i + 1 < args.Length)
                {
                    telephone1 = args[i + 1];
                    i++;
                }
                else if (arg == "--telephone2" && i + 1 < args.Length)
                {
                    telephone2 = args[i + 1];
                    i++;
                }
                else if (arg == "--mobilephone" && i + 1 < args.Length)
                {
                    mobilephone = args[i + 1];
                    i++;
                }
                else if (arg == "--vu_purchaselocationstate" && i + 1 < args.Length)
                {
                    vu_purchaselocationstate = args[i + 1];
                    i++;
                }
                else if (arg == "--vu_purchaselocationcity" && i + 1 < args.Length)
                {
                    vu_purchaselocationcity = args[i + 1];
                    i++;
                }
                else if (arg == "--vu_purchaselocationzip" && i + 1 < args.Length)
                {
                    vu_purchaselocationzip = args[i + 1];
                    i++;
                }
                else if (arg == "--vu_subjectcity" && i + 1 < args.Length)
                {
                    vu_subjectcity = args[i + 1];
                    i++;
                }
                else if (arg == "--vu_subjectstate" && i + 1 < args.Length)
                {
                    vu_subjectstate = args[i + 1];
                    i++;
                }
                else if (arg == "--vu_subjectzipcode" && i + 1 < args.Length)
                {
                    vu_subjectzipcode = args[i + 1];
                    i++;
                }
                else if (arg == "--vu_vurpurpose" && i + 1 < args.Length)
                {
                    vu_vurpurpose = args[i + 1];
                    i++;
                }
            }
            
            return (environment, firstName, lastName, address1_line1, address1_city, address1_stateorprovince, 
                address1_postalcode, emailaddress1, rss_mostrecentphonenumber, telephone1, telephone2, 
                mobilephone, vu_purchaselocationstate, vu_purchaselocationcity, vu_purchaselocationzip, vu_subjectcity, vu_subjectstate, vu_subjectzipcode, vu_vurpurpose);
        }
        
        /// <summary>
        /// Loads configuration values from App.config and environment-specific overrides
        /// </summary>
        private static void LoadEnvironmentConfiguration(string environment)
        {
            // First load settings from the base App.config
            ApplicationToken = ConfigurationManager.AppSettings["RealtyApplicationToken"];
            BusinessUnitValue = ConfigurationManager.AppSettings["RealtyBusinessUnit"];
            CrmServiceEnvironment = ConfigurationManager.AppSettings["RealtyCrmServiceEnvironment"];
            LeadTeamId = ConfigurationManager.AppSettings["RealtyLeadAdmin"];
            RealtyAgent = ConfigurationManager.AppSettings["RealtyLoanOfficer"];
            
            // Always override with environment-specific settings from centralized config
            CrmServiceEnvironment = environment;
            
            // Load environment-specific settings from centralized config location
            // Map DEV to Debug to match existing config file naming convention
            string configEnvironment = environment == "DEV" ? "Debug" : environment;
            string centralConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "app.env.config", $"App.{configEnvironment}.config");
            
            // Normalize the path
            centralConfigPath = Path.GetFullPath(centralConfigPath);
            
            if (File.Exists(centralConfigPath))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(centralConfigPath);
                    
                    // Extract settings from the environment-specific config
                    XmlNodeList appSettings = doc.SelectNodes("//appSettings/add");
                    if (appSettings != null)
                    {
                        Console.WriteLine($"Found {appSettings.Count} settings in environment config file");
                        foreach (XmlNode setting in appSettings)
                        {
                            string key = setting.Attributes["key"]?.Value;
                            string value = setting.Attributes["value"]?.Value;
                            
                            Console.WriteLine($"Processing config key: {key} = {value}");
                            
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                            {
                                // Update the corresponding setting
                                switch (key)
                                {
                                    case "RealtyApplicationToken":
                                        ApplicationToken = value;
                                        Console.WriteLine($"Updated ApplicationToken to: {value}");
                                        break;
                                    case "RealtyBusinessUnit":
                                        BusinessUnitValue = value;
                                        Console.WriteLine($"Updated BusinessUnitValue to: {value}");
                                        break;
                                    case "RealtyCrmServiceEnvironment":
                                        CrmServiceEnvironment = value;
                                        Console.WriteLine($"Updated CrmServiceEnvironment to: {value}");
                                        break;
                                    case "RealtyLeadAdmin":
                                        LeadTeamId = value;
                                        Console.WriteLine($"Updated LeadTeamId to: {value}");
                                        break;
                                    case "RealtyLoanOfficer":
                                        RealtyAgent = value;
                                        Console.WriteLine($"Updated RealtyAgent to: {value}");
                                        break;
                                    default:
                                        Console.WriteLine($"Unknown config key: {key}");
                                        break;
                                }
                            }
                        }
                    }
                    Console.WriteLine($"Loaded environment-specific configuration from: {centralConfigPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load environment-specific configuration: {ex.Message}");
                    Console.WriteLine("Using base configuration instead.");
                }
            }
            else
            {
                Console.WriteLine($"Warning: Environment-specific configuration file not found: {centralConfigPath}");
                Console.WriteLine("Using base configuration with environment override.");
            }
            
            // Ensure the correct RealtyAgent GUID is used for each environment
            EnsureCorrectRealtyAgentGuid();
        }
        
        /// <summary>
        /// Ensures the correct RealtyAgent GUID is used for each environment
        /// </summary>
        private static void EnsureCorrectRealtyAgentGuid()
        {
            // Validate that RealtyAgent GUID is configured properly
            if (string.IsNullOrEmpty(RealtyAgent))
            {
                Console.WriteLine($"Warning: RealtyAgent GUID is not configured for {CrmServiceEnvironment} environment.");
                return;
            }

            // Validate that RealtyAgent is a valid GUID format
            if (!Guid.TryParse(RealtyAgent, out _))
            {
                Console.WriteLine($"Warning: RealtyAgent value '{RealtyAgent}' is not a valid GUID format for {CrmServiceEnvironment} environment.");
                return;
            }

            // RealtyAgent GUID is now loaded from centralized app config files
            Console.WriteLine($"Using RealtyAgent GUID from config: {RealtyAgent} for {CrmServiceEnvironment} environment.");
        }

        /// <summary>
        /// Validates that all required configuration settings are present
        /// </summary>
        private static bool ValidateConfigSettings()
        {
            bool isValid = true;
            
            // Check ApplicationToken
            if (string.IsNullOrEmpty(ApplicationToken))
            {
                Console.WriteLine("Error: ApplicationToken is missing in App.config");
                isValid = false;
            }
            
            // Check BusinessUnitValue
            if (string.IsNullOrEmpty(BusinessUnitValue))
            {
                Console.WriteLine("Error: BusinessUnit is missing in App.config");
                isValid = false;
            }
            else
            {
                // Validate that BusinessUnitValue can be parsed as a BusinessUnit enum
                try
                {
                    Enum.Parse(typeof(BusinessUnit), BusinessUnitValue);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine($"Error: BusinessUnit value '{BusinessUnitValue}' is not valid");
                    isValid = false;
                }
            }
            
            // Check CrmServiceEnvironment
            if (string.IsNullOrEmpty(CrmServiceEnvironment))
            {
                Console.WriteLine("Error: CrmServiceEnvironment is missing in App.config");
                isValid = false;
            }
            
            // Check LeadTeamId
            if (string.IsNullOrEmpty(LeadTeamId))
            {
                Console.WriteLine("Error: LeadTeamId is missing in App.config");
                isValid = false;
            }
            
            // Check RealtyAgent
            if (string.IsNullOrEmpty(RealtyAgent))
            {
                Console.WriteLine("Error: RealtyAgent is missing in App.config");
                isValid = false;
            }
            
            if (isValid)
            {
                Console.WriteLine("Configuration validation successful.");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Generates test data for lead creation
        /// </summary>
        private static (string FirstName, string LastName, string Address1_Line1, string Address1_City, 
            string Address1_StateOrProvince, string Address1_PostalCode, string EmailAddress1, 
            string Vu_MostRecentPhoneNumber, string Telephone2, string MobilePhone, string Telephone3, 
            string Vu_PurchaseLocationState, string Vu_PurchaseLocationZip) GenerateTestLeadData()
        {
            // Generate random test data using ArtificialTestValues
            var firstName = RandomName.FirstName();
            var lastName = RandomName.LastName();
            var address1_line1 = RandomAddress.Street();
            var address1_city = RandomAddress.City();
            var address1_state = "MO";
            var address1_zip = "00920";
            var phone = RandomPhoneNumber.WithAreaCode();
            var homePhone = RandomPhoneNumber.WithAreaCode();
            var mobilePhone = RandomPhoneNumber.WithAreaCode();
            var otherPhone = RandomPhoneNumber.WithAreaCode();
            
            return (
                FirstName: firstName,
                LastName: lastName,
                Address1_Line1: address1_line1,
                Address1_City: address1_city,
                Address1_StateOrProvince: address1_state,
                Address1_PostalCode: address1_zip,
                EmailAddress1: $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
                Vu_MostRecentPhoneNumber: phone,
                Telephone2: homePhone,
                MobilePhone: mobilePhone,
                Telephone3: otherPhone,
                Vu_PurchaseLocationState: address1_state,
                Vu_PurchaseLocationZip: address1_zip
            );
        }
        
        /// <summary>
        /// Creates a lead in CRM using the CRM SDK or mock implementation
        /// </summary>
        private static string CreateCrmLead(string firstName, string lastName, string addressLine1, string city, 
            string state, string zipCode, string email, string mostRecentPhone, string telephone1, 
            string telephone2, string mobilePhone, string purchaseState, string purchaseCity, string purchaseZip,
            string subjectCity, string subjectState, string subjectZipCode, string vu_vurpurpose)
        {
            // Validate required parameters
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                throw new ArgumentException("First name and last name are required");
                
            Console.WriteLine($"Creating lead for: {firstName} {lastName}");
            Console.WriteLine($"Email: {email}");
            Console.WriteLine($"Phone: {mostRecentPhone}");
            
            if (UseMockImplementation)
            {
                // Mock implementation for testing without CRM SDK
                Console.WriteLine("Using MOCK implementation (no actual CRM connection)...");
                
                // Generate a mock lead ID
                string mockLeadId = Guid.NewGuid().ToString();
                
                // Log the lead creation
                Console.WriteLine("Mock lead creation for: " + firstName + " " + lastName);
                Console.WriteLine($"  firstname: {firstName}");
                Console.WriteLine($"  lastname: {lastName}");
                Console.WriteLine($"  address1_line1: {addressLine1}");
                Console.WriteLine($"  address1_city: {city}");
                Console.WriteLine($"  address1_stateorprovince: {state}");
                Console.WriteLine($"  address1_postalcode: {zipCode}");
                Console.WriteLine($"  emailaddress1: {email}");
                Console.WriteLine($"  rss_mostrecentphonenumber: {mostRecentPhone}");
                Console.WriteLine($"  telephone1: {telephone1}");
                Console.WriteLine($"  telephone2: {telephone2}");
                Console.WriteLine($"  mobilephone: {mobilePhone}");
                Console.WriteLine($"  vu_purchaselocationstate: {purchaseState}");
                Console.WriteLine($"  vu_purchaselocationcity: {purchaseCity}");
                Console.WriteLine($"  vu_purchaselocationzip: {purchaseZip}");
                Console.WriteLine($"  vu_subjectcity: {subjectCity}");
                Console.WriteLine($"  vu_subjectstate: {subjectState}");
                Console.WriteLine($"  vu_subjectzipcode: {subjectZipCode}");
                Console.WriteLine($"  vu_vurpurpose: {vu_vurpurpose}");
                Console.WriteLine($"  ownerid: {RealtyAgent}");
                Console.WriteLine($"  owningteam: {LeadTeamId}");
                Console.WriteLine("Lead would be created in CRM in a real implementation");
                
                // Simulate success
                Console.WriteLine($"Mock lead created successfully with ID: {mockLeadId}");
                
                return mockLeadId;
            }
            else
            {
                try
                {
#if !MOCK_SDK
                    // Initialize the CRM service
                    Console.WriteLine("Initializing CRM connection with token: {0}", ApplicationToken);
                    Console.WriteLine("Business Unit: {0}, Environment: {1}", BusinessUnitValue, CrmServiceEnvironment);
                    
                    var builder = new CrmCoreServiceBuilder(ApplicationToken);
                    var builderOptions = new CrmCoreServiceBuilderOptions(
                        (BusinessUnit)Enum.Parse(typeof(BusinessUnit), BusinessUnitValue ?? "CRM"), 
                        CrmServiceEnvironment);
                    
                    Console.WriteLine("Attempting to connect to CRM service...");
                    // Create a cancellation token with a timeout
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(30); // Set timeout on the HttpClient
                    
                    // Declare coreService outside the try block to maintain scope
                    Crm.CoreService.NuGet.Standard.Interfaces.IOrganizationService coreService;
                    
                    try {
                        Console.WriteLine("Building CRM service...");
                        coreService = builder.BuildService(httpClient, builderOptions);
                        Console.WriteLine("CRM service connection established successfully.");
                    }
                    catch (TaskCanceledException) {
                        Console.WriteLine("Connection to CRM timed out after 30 seconds.");
                        throw new TimeoutException("Connection to CRM timed out. Please check network connectivity and VPN status.");
                    }
                    catch (HttpRequestException ex) {
                        Console.WriteLine("HTTP request error when connecting to CRM: {0}", ex.Message);
                        throw new Exception("Failed to connect to CRM service. Please check network connectivity and VPN status.", ex);
                    }
                    catch (Exception ex) {
                        Console.WriteLine("Unexpected error when connecting to CRM: {0}", ex.Message);
                        throw new Exception("Unexpected error when connecting to CRM service.", ex);
                    }
                    
                    // Create the lead entity
                    var lead = new Crm.CoreService.NuGet.Standard.Models.Entity
                    {
                        LogicalName = "lead",
                        Attributes = new Crm.CoreService.NuGet.Standard.Models.AttributeCollection()
                    };
                    
                    // Set lead fields
                    lead["firstname"] = firstName;
                    lead["lastname"] = lastName;
                    lead["subject"] = $"Realty Lead - {firstName} {lastName}";
                    
                    // Address information
                    if (!string.IsNullOrEmpty(addressLine1))
                        lead["address1_line1"] = addressLine1;
                    if (!string.IsNullOrEmpty(city))
                        lead["address1_city"] = city;
                    if (!string.IsNullOrEmpty(state))
                        lead["address1_stateorprovince"] = state;
                    if (!string.IsNullOrEmpty(zipCode))
                        lead["address1_postalcode"] = zipCode;
                    
                    // Contact information
                    if (!string.IsNullOrEmpty(email))
                        lead["emailaddress1"] = email;
                    if (!string.IsNullOrEmpty(mostRecentPhone))
                        lead["rss_mostrecentphonenumber"] = mostRecentPhone;
                    if (!string.IsNullOrEmpty(telephone1))
                        lead["telephone1"] = telephone1;
                    if (!string.IsNullOrEmpty(telephone2))
                        lead["telephone2"] = telephone2;
                    if (!string.IsNullOrEmpty(mobilePhone))
                        lead["mobilephone"] = mobilePhone;
                    
                    // Purchase location information
                    if (!string.IsNullOrEmpty(purchaseState))
                        lead["vu_purchaselocationstate"] = purchaseState;
                    if (!string.IsNullOrEmpty(purchaseCity))
                        lead["vu_purchaselocationcity"] = purchaseCity;
                    if (!string.IsNullOrEmpty(purchaseZip))
                        lead["vu_purchaselocationzip"] = purchaseZip;
                    
                    // Subject location information
                    if (!string.IsNullOrEmpty(subjectCity))
                        lead["vu_subjectcity"] = subjectCity;
                    if (!string.IsNullOrEmpty(subjectState))
                        lead["vu_subjectstate"] = subjectState;
                    if (!string.IsNullOrEmpty(subjectZipCode))
                        lead["vu_subjectzipcode"] = subjectZipCode;
                    if (!string.IsNullOrEmpty(vu_vurpurpose) && int.TryParse(vu_vurpurpose, out int vurpurposeValue))
                        lead["vu_vurpurpose"] = new Crm.CoreService.NuGet.Standard.Models.OptionSetValue(vurpurposeValue);
                    
                    // Set ownership - use RealtyAgent GUID
                    Console.WriteLine($"Using RealtyAgent GUID: {RealtyAgent} for environment: {CrmServiceEnvironment}");
                    if (!string.IsNullOrEmpty(RealtyAgent) && Guid.TryParse(RealtyAgent, out Guid realtyAgentGuid))
                    {
                        lead["ownerid"] = new Crm.CoreService.NuGet.Standard.Models.EntityReference("systemuser", realtyAgentGuid);
                    }
                    
                    // Set team ownership if configured
                    if (!string.IsNullOrEmpty(LeadTeamId) && Guid.TryParse(LeadTeamId, out Guid teamGuid))
                    {
                        lead["owningteam"] = new Crm.CoreService.NuGet.Standard.Models.EntityReference("team", teamGuid);
                    }
                    
                    // Create the lead in CRM
                    Console.WriteLine("Creating lead in CRM...");
                    Guid leadId = coreService.Create(lead);
                    
                    Console.WriteLine($"Lead created successfully with ID: {leadId}");
                    return leadId.ToString();
#else
                    throw new NotSupportedException("Real CRM implementation is not available when compiled with MOCK_SDK");
#endif
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error creating lead in CRM: {0}", ex.Message);
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", ex.InnerException.Message);
                    }
                    throw new Exception("Failed to create lead in CRM", ex);
                }
            }
        }
        
        /// <summary>
        /// Sets up test data for CRM lead creation
        /// </summary>
        public static void SetupCrmLeadTestData()
        {
            // Generate random test leads and create them
            for (int i = 1; i <= 3; i++)
            {
                var testData = GenerateTestLeadData();
                var leadId = CreateCrmLead(testData.FirstName, testData.LastName, testData.Address1_Line1, 
                    testData.Address1_City, testData.Address1_StateOrProvince, testData.Address1_PostalCode, 
                    testData.EmailAddress1, testData.Vu_MostRecentPhoneNumber, testData.Telephone2, 
                    testData.Telephone3, testData.MobilePhone, testData.Vu_PurchaseLocationState, 
                    testData.Address1_City, testData.Vu_PurchaseLocationZip, testData.Address1_City, "MO", "00920", "827230000");
                
                Console.WriteLine($"Created test lead {i} with ID: {leadId}");
            }
        }
    }
}
