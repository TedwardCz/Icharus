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
        // Configuration values from centralized config files
        private static string ApplicationToken;
        private static string BusinessUnitValue;
        private static string CrmServiceEnvironment;
        
        // Team GUID for lead ownership
        private static string LeadTeamId;
        
        // Loan Officer GUID
        private static string LoanOfficer;
        
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
            Console.WriteLine("Icharus CRM Lead Creation Tool");
            Console.WriteLine("==============================");
            
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
            string vu_mostrecentphonenumber = parameters.vu_mostrecentphonenumber;
            string telephone2 = parameters.telephone2;
            string mobilephone = parameters.mobilephone;
            string telephone3 = parameters.telephone3;
            string vu_purchaselocationstate = parameters.vu_purchaselocationstate;
            string vu_purchaselocationcity = parameters.vu_purchaselocationcity;
            string vu_purchaselocationzip = parameters.vu_purchaselocationzip;

            // Only use random data for parameters that weren't provided
            if (string.IsNullOrEmpty(firstName))
                firstName = RandomName.FirstName();
            if (string.IsNullOrEmpty(lastName))
                lastName = RandomName.LastName();
            if (string.IsNullOrEmpty(address1_line1))
                address1_line1 = RandomAddress.Street();
            if (string.IsNullOrEmpty(address1_city))
                address1_city = RandomAddress.City();
            if (string.IsNullOrEmpty(address1_stateorprovince))
                address1_stateorprovince = "MO";
            if (string.IsNullOrEmpty(address1_postalcode))
                address1_postalcode = "00920";
            if (string.IsNullOrEmpty(emailaddress1))
                emailaddress1 = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com";
            if (string.IsNullOrEmpty(vu_mostrecentphonenumber))
                vu_mostrecentphonenumber = RandomPhoneNumber.WithAreaCode();
            if (string.IsNullOrEmpty(telephone2))
                telephone2 = RandomPhoneNumber.WithAreaCode();
            if (string.IsNullOrEmpty(mobilephone))
                mobilephone = RandomPhoneNumber.WithAreaCode();
            if (string.IsNullOrEmpty(telephone3))
                telephone3 = RandomPhoneNumber.WithAreaCode();
            if (string.IsNullOrEmpty(vu_purchaselocationstate))
                vu_purchaselocationstate = "MO";
            if (string.IsNullOrEmpty(vu_purchaselocationcity))
                vu_purchaselocationcity = RandomAddress.City();
            if (string.IsNullOrEmpty(vu_purchaselocationzip))
                vu_purchaselocationzip = "00920";
            
            // Load configuration for the specified environment
            LoadEnvironmentConfiguration(environment);
            
            Console.WriteLine($"Using environment: {environment}");
            
            // Validate configuration settings
            if (!ValidateConfigSettings())
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }
            
            // Diagnostic output to verify configuration
            Console.WriteLine($"Environment: {CrmServiceEnvironment}, LoanOfficer: {LoanOfficer}");
            
            // Ensure correct LoanOfficer GUID for each environment
            EnsureCorrectLoanOfficerGuid();
            Console.WriteLine($"Using LoanOfficer GUID: {LoanOfficer} for environment: {CrmServiceEnvironment}");
            
            // Automatically create a single lead
            Console.WriteLine($"Creating lead for {firstName} {lastName}");
            
            // Create a single lead
            Guid leadId = CreateCrmLead(
                firstName,
                lastName,
                address1_line1,
                address1_city,
                address1_stateorprovince,
                address1_postalcode,
                emailaddress1,
                vu_mostrecentphonenumber,
                telephone2,
                mobilephone,
                telephone3,
                vu_purchaselocationstate,
                vu_purchaselocationcity,
                vu_purchaselocationzip
            );
            
            // Copy lead ID to clipboard
            string leadIdString = leadId.ToString();
            try
            {
                ClipboardService.SetText(leadIdString);
                Console.WriteLine($"Lead created successfully with ID: {leadId} (copied to clipboard)");
            }
            catch (Exception clipboardEx)
            {
                Console.WriteLine($"Lead created successfully with ID: {leadId}");
                Console.WriteLine($"Note: Could not copy to clipboard: {clipboardEx.Message}");
            }
        }
        
        /// <summary>
        /// Class to hold all lead parameters
        /// </summary>
        public class LeadParameters
        {
            public string Environment { get; set; } = DEFAULT_ENVIRONMENT;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string address1_line1 { get; set; } = string.Empty;
            public string address1_city { get; set; } = string.Empty;
            public string address1_stateorprovince { get; set; } = string.Empty;
            public string address1_postalcode { get; set; } = string.Empty;
            public string emailaddress1 { get; set; } = string.Empty;
            public string vu_mostrecentphonenumber { get; set; } = string.Empty;
            public string telephone2 { get; set; } = string.Empty;
            public string mobilephone { get; set; } = string.Empty;
            public string telephone3 { get; set; } = string.Empty;
            public string vu_purchaselocationstate { get; set; } = string.Empty;
            public string vu_purchaselocationcity { get; set; } = string.Empty;
            public string vu_purchaselocationzip { get; set; } = string.Empty;
        }
        
        /// <summary>
        /// Parses command line arguments for environment and lead information
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>LeadParameters object containing all lead parameters</returns>
        private static LeadParameters ParseCommandLineArgs(string[] args)
        {
            var parameters = new LeadParameters();
            
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                
                // Parse environment parameter
                if (arg == "--env" || arg == "-e")
                {
                    if (i + 1 < args.Length)
                    {
                        string envArg = args[i + 1].ToUpper();
                        if (envArg == ENV_DEV || envArg == ENV_UAT || envArg == ENV_PROD)
                        {
                            parameters.Environment = envArg;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Unknown environment '{envArg}'. Using {DEFAULT_ENVIRONMENT} instead.");
                        }
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse first name parameter
                else if (arg == "--firstname" || arg == "-f")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.FirstName = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse last name parameter
                else if (arg == "--lastname" || arg == "-l")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.LastName = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse address parameter
                else if (arg == "--address1_line1")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.address1_line1 = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse city parameter
                else if (arg == "--address1_city")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.address1_city = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse state parameter
                else if (arg == "--address1_stateorprovince")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.address1_stateorprovince = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse zip code parameter
                else if (arg == "--address1_postalcode")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.address1_postalcode = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse email parameter
                else if (arg == "--emailaddress1")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.emailaddress1 = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse primary phone parameter
                else if (arg == "--vu_mostrecentphonenumber")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.vu_mostrecentphonenumber = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse home phone parameter
                else if (arg == "--telephone2")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.telephone2 = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse mobile phone parameter
                else if (arg == "--mobilephone")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.mobilephone = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse other phone parameter
                else if (arg == "--telephone3")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.telephone3 = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse purchase location state parameter
                else if (arg == "--vu_purchaselocationstate")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.vu_purchaselocationstate = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse purchase location city parameter
                else if (arg == "--vu_purchaselocationcity")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.vu_purchaselocationcity = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse purchase location zip parameter
                else if (arg == "--vu_purchaselocationzip")
                {
                    if (i + 1 < args.Length)
                    {
                        parameters.vu_purchaselocationzip = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
            }
            
            // Generate email if not provided
            if (parameters.emailaddress1 == null)
            {
                parameters.emailaddress1 = $"{parameters.FirstName.ToLower()}.{parameters.LastName.ToLower()}@example.com";
            }
            
            return parameters;
        }
        
        /// <summary>
        /// Loads configuration settings for the specified environment
        /// </summary>
        /// <param name="environment">The environment to load (DEV, UAT, or PROD)</param>
        private static void LoadEnvironmentConfiguration(string environment)
        {
            // First load settings from the centralized base config
            ApplicationToken = ConfigurationManager.AppSettings["LenderApplicationToken"];
            BusinessUnitValue = ConfigurationManager.AppSettings["LenderBusinessUnit"];
            CrmServiceEnvironment = ConfigurationManager.AppSettings["LenderCrmServiceEnvironment"];
            LeadTeamId = ConfigurationManager.AppSettings["LenderLeadAdmin"];
            LoanOfficer = ConfigurationManager.AppSettings["LenderLoanOfficer"];
            
            // Always override with environment-specific settings from centralized config
            CrmServiceEnvironment = environment;
            
            // Load environment-specific settings from centralized config location
            string centralConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "app.env.config", $"App.{environment}.config");
            
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
                                    case "LenderApplicationToken":
                                        ApplicationToken = value;
                                        Console.WriteLine($"Updated ApplicationToken to: {value}");
                                        break;
                                    case "LenderBusinessUnit":
                                        BusinessUnitValue = value;
                                        Console.WriteLine($"Updated BusinessUnitValue to: {value}");
                                        break;
                                    case "LenderCrmServiceEnvironment":
                                        CrmServiceEnvironment = value;
                                        Console.WriteLine($"Updated CrmServiceEnvironment to: {value}");
                                        break;
                                    case "LenderLeadAdmin":
                                        LeadTeamId = value;
                                        Console.WriteLine($"Updated LeadTeamId to: {value}");
                                        break;
                                    case "LenderLoanOfficer":
                                        LoanOfficer = value;
                                        Console.WriteLine($"Updated LoanOfficer to: {value}");
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
            
            // Ensure the correct LoanOfficer GUID is used for each environment
            EnsureCorrectLoanOfficerGuid();
        }
        
        /// <summary>
        /// Ensures the correct LoanOfficer GUID is used for each environment
        /// </summary>
        private static void EnsureCorrectLoanOfficerGuid()
        {
            // Validate that LoanOfficer GUID is configured properly
            if (string.IsNullOrEmpty(LoanOfficer))
            {
                Console.WriteLine($"Warning: LoanOfficer GUID is not configured for {CrmServiceEnvironment} environment.");
                return;
            }

            // Validate that LoanOfficer is a valid GUID format
            if (!Guid.TryParse(LoanOfficer, out _))
            {
                Console.WriteLine($"Warning: LoanOfficer value '{LoanOfficer}' is not a valid GUID format for {CrmServiceEnvironment} environment.");
                return;
            }

            // LoanOfficer GUID is now loaded from centralized app config files
            Console.WriteLine($"Using LoanOfficer GUID from config: {LoanOfficer} for {CrmServiceEnvironment} environment.");
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
                Console.WriteLine("Error: ApplicationToken is missing in configuration");
                isValid = false;
            }
            
            // Check BusinessUnitValue
            if (string.IsNullOrEmpty(BusinessUnitValue))
            {
                Console.WriteLine("Error: BusinessUnit is missing in configuration");
                isValid = false;
            }
            else
            {
                // Validate that BusinessUnitValue can be parsed as a BusinessUnit enum
                try
                {
                    Enum.Parse(typeof(BusinessUnit), BusinessUnitValue);
                }
                catch
                {
                    Console.WriteLine($"Error: BusinessUnit value '{BusinessUnitValue}' is not valid");
                    isValid = false;
                }
            }
            
            // Check CrmServiceEnvironment
            if (string.IsNullOrEmpty(CrmServiceEnvironment))
            {
                Console.WriteLine("Error: CrmServiceEnvironment is missing in configuration");
                isValid = false;
            }
            
            // Check LeadTeamId
            if (string.IsNullOrEmpty(LeadTeamId))
            {
                Console.WriteLine("Error: _vuhlLeadAdmin is missing in configuration");
                isValid = false;
            }
            else
            {
                // Validate that LeadTeamId is a valid GUID
                try
                {
                    Guid.Parse(LeadTeamId);
                }
                catch
                {
                    Console.WriteLine($"Error: _vuhlLeadAdmin value '{LeadTeamId}' is not a valid GUID");
                    isValid = false;
                }
            }
            
            // Check LoanOfficer
            if (string.IsNullOrEmpty(LoanOfficer))
            {
                Console.WriteLine("Error: LoanOfficer is missing in configuration");
                isValid = false;
            }
            else
            {
                // Validate that LoanOfficer is a valid GUID
                try
                {
                    Guid.Parse(LoanOfficer);
                }
                catch
                {
                    Console.WriteLine($"Error: LoanOfficer value '{LoanOfficer}' is not a valid GUID");
                    isValid = false;
                }
            }
            
            if (isValid)
            {
                Console.WriteLine("Configuration validation successful.");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Creates a new lead in CRM using the CRM SDK or mock implementation
        /// </summary>
        public static Guid CreateCrmLead(
            string firstName,
            string lastName,
            string addressLine1,
            string city,
            string state,
            string zipCode,
            string email,
            string mostRecentPhone,
            string homePhone,
            string mobilePhone,
            string otherPhone,
            string purchaseState,
            string purchaseCity,
            string purchaseZip)
        {
            // Validate required parameters
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name is required", nameof(lastName));
                
            if (string.IsNullOrWhiteSpace(addressLine1))
                throw new ArgumentException("Address line 1 is required", nameof(addressLine1));
                
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City is required", nameof(city));
                
            if (string.IsNullOrWhiteSpace(state))
                throw new ArgumentException("State is required", nameof(state));
                
            if (string.IsNullOrWhiteSpace(zipCode))
                throw new ArgumentException("Zip code is required", nameof(zipCode));
                
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));
                
            if (string.IsNullOrWhiteSpace(mostRecentPhone))
                throw new ArgumentException("Most recent phone is required", nameof(mostRecentPhone));
            
            Console.WriteLine("Creating lead for: {0} {1}", firstName, lastName);
            Console.WriteLine("Email: {0}", email);
            Console.WriteLine("Phone: {0}", mostRecentPhone);
            
            Guid leadId;
            
            if (UseMockImplementation)
            {
                // Mock implementation for testing without CRM SDK
                Console.WriteLine("Using MOCK implementation (no actual CRM connection)...");
                
                // Log the lead properties
                Console.WriteLine("Mock lead created with the following properties:");
                Console.WriteLine($"  firstname: {firstName}");
                Console.WriteLine($"  lastname: {lastName}");
                Console.WriteLine($"  address1_line1: {addressLine1}");
                Console.WriteLine($"  address1_city: {city}");
                Console.WriteLine($"  address1_stateorprovince: {state}");
                Console.WriteLine($"  address1_postalcode: {zipCode}");
                Console.WriteLine($"  emailaddress1: {email}");
                Console.WriteLine($"  vu_mostrecentphonenumber: {mostRecentPhone}");
                Console.WriteLine($"  telephone1: {homePhone}");
                Console.WriteLine($"  mobilephone: {mobilePhone}");
                Console.WriteLine($"  telephone2: {otherPhone}");
                Console.WriteLine($"  vu_purchaselocationstate: {purchaseState}");
                Console.WriteLine($"  vu_purchaselocationcity: {purchaseCity}");
                Console.WriteLine($"  vu_purchaselocationzip: {purchaseZip}");
                Console.WriteLine($"  ownerid: {LoanOfficer}");
                
                // Generate a random GUID for the lead ID
                leadId = Guid.NewGuid();
                Console.WriteLine($"Mock lead created with ID: {leadId}");
                
                // Copy mock lead ID to clipboard
                try
                {
                    ClipboardService.SetText(leadId.ToString());
                    Console.WriteLine($"Lead ID copied to clipboard");
                }
                catch (Exception clipboardEx)
                {
                    Console.WriteLine($"Note: Could not copy to clipboard: {clipboardEx.Message}");
                }
                
                return leadId;
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
                        (BusinessUnit)Enum.Parse(typeof(BusinessUnit), BusinessUnitValue), 
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
                        Attributes = new Crm.CoreService.NuGet.Standard.Models.AttributeCollection
                        {
                            { "firstname", firstName },
                            { "lastname", lastName },
                            { "address1_line1", addressLine1 },
                            { "address1_city", city },
                            { "address1_stateorprovince", state },
                            { "address1_postalcode", zipCode },
                            { "emailaddress1", email },
                            { "vu_mostrecentphonenumber", mostRecentPhone },
                            { "telephone2", homePhone },
                            { "mobilephone", mobilePhone },
                            { "telephone3", otherPhone },
                            { "ownerid", new Crm.CoreService.NuGet.Standard.Models.EntityReference("systemuser", Guid.Parse(LoanOfficer)) },
                            { "vu_purchaselocationstate", purchaseState },
                            { "vu_purchaselocationcity", purchaseCity },
                            { "vu_purchaselocationzip", purchaseZip },
                            { "leadsourcecode", new Crm.CoreService.NuGet.Standard.Models.OptionSetValue(100000001) },
                            { "vu_leadformuniqueidentifier", Guid.NewGuid().ToString() }
                        }
                    };
                    
                    // Create the lead in CRM
                    Guid createdLeadId = coreService.Create(lead);
                    Console.WriteLine("Lead created successfully in CRM");
                    return createdLeadId;
#else
                    throw new NotSupportedException("Real CRM implementation is not available when compiled with MOCK_SDK");
#endif
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error creating lead in CRM: {0}", ex.Message);
                    throw new Exception("Failed to create lead in CRM", ex);
                }
            }
        }
        
        /// <summary>
        /// Sets up test data for CRM regular leads
        /// </summary>
        public static void SetupCrmRegularLeadTestData()
        {
            // Generate random data for testing
            var random = new Random();
            var phoneNumber = $"{random.Next(100, 999)}{random.Next(100, 999)}{random.Next(1000, 9999)}";
            
            // Create multiple test leads
            for (int i = 1; i <= 5; i++)
            {
                var leadId = CreateCrmLead(
                    $"TestUser{i}",
                    $"TestLastName{i}",
                    $"{random.Next(100, 999)} Test Street",
                    "Test City",
                    "MO",
                    $"{random.Next(10000, 99999)}",
                    $"testuser{i}@example.com",
                    phoneNumber,
                    phoneNumber,
                    phoneNumber,
                    phoneNumber,
                    "MO",
                    "Test Purchase City",
                    $"{random.Next(10000, 99999)}"
                );
                
                Console.WriteLine($"Created test lead {i} with ID: {leadId}");
            }
        }
        /// </summary>
        private static void DisplayUsage()
        {
            Console.WriteLine("Usage: dotnet run [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help, -h                     Display this help message");
            Console.WriteLine("  --env, -e <environment>        Set the environment (DEV, UAT, PROD). Default: UAT");
            Console.WriteLine();
            Console.WriteLine("Contact Information:");
            Console.WriteLine("  --firstname, -f <firstName>    Set the lead's first name. Default: Random");
            Console.WriteLine("  --lastname, -l <lastName>      Set the lead's last name. Default: Random");
            Console.WriteLine("  --emailaddress1 <email>        Set the lead's email. Default: firstname.lastname@example.com");
            Console.WriteLine("  --vu_mostrecentphonenumber <phone>  Set the lead's primary phone. Default: Random");
            Console.WriteLine("  --telephone2 <phone>           Set the lead's home phone. Default: Random");
            Console.WriteLine("  --mobilephone <phone>          Set the lead's mobile phone. Default: Random");
            Console.WriteLine("  --telephone3 <phone>           Set the lead's other phone. Default: Random");
            Console.WriteLine();
            Console.WriteLine("Address Information:");
            Console.WriteLine("  --address1_line1 <address>     Set the lead's street address. Default: Random");
            Console.WriteLine("  --address1_city <city>         Set the lead's city. Default: Random");
            Console.WriteLine("  --address1_stateorprovince <state>  Set the lead's state. Default: CA");
            Console.WriteLine("  --address1_postalcode <zipCode>  Set the lead's zip code. Default: 00920");
            Console.WriteLine("  --vu_purchaselocationstate <state>  Set the lead's purchase location state. Default: MO");
            Console.WriteLine("  --vu_purchaselocationcity <city>   Set the lead's purchase location city. Default: Random");
            Console.WriteLine("  --vu_purchaselocationzip <zipCode>  Set the lead's purchase location zip code. Default: 00920");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run");
            Console.WriteLine("  dotnet run --env DEV");
            Console.WriteLine("  dotnet run --firstname Billy --lastname Smith");
            Console.WriteLine("  dotnet run --env PROD --firstname Jane --lastname Doe --address1_city \"Chicago\" --address1_stateorprovince IL");
            Console.WriteLine("  dotnet run --address1_line1 \"456 Oak Ave\" --address1_city Boston --address1_stateorprovince MA --address1_postalcode 02108");
        }
    }
}
