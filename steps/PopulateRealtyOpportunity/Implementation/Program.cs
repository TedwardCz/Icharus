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
using System.Xml.XPath;
using TextCopy;

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
            VUR,
            RSS
        }
#endif

#if !MOCK_SDK
        // Define BusinessUnit enum if needed for compilation
        private enum BusinessUnitLocal
        {
            CRM,
            VUHL,
            VUR,
            RSS
        }
#endif
        
        static void Main(string[] args)
        {
            Console.WriteLine("Icharus Realty CRM Opportunity Population Tool");
            Console.WriteLine("==============================================");
            
            // Parse command line arguments
            var parsedArgs = ParseCommandLineArgs(args);
            string environment = parsedArgs.Item1;
            Guid? opportunityGuid = parsedArgs.Item2;
            int closeProbability = parsedArgs.Item3;
            int brandValue = parsedArgs.Item4;
            string attachmentZipCode = parsedArgs.Item5;
            
            // Check if opportunity GUID was provided
            if (!opportunityGuid.HasValue)
            {
                Console.WriteLine("Error: Opportunity GUID is required.");
                Console.WriteLine("Usage: dotnet run [--env|-e <environment>] --opportunity|-o <opportunityGuid> [--closeprobability <probability>] [--rss_brand <brandValue>] [--rss_attachmentzipcode <zipcode>]");
                Console.WriteLine("Example: dotnet run --opportunity 12345678-1234-1234-1234-123456789012");
                Console.WriteLine("Example: dotnet run -e PROD -o 12345678-1234-1234-1234-123456789012 --closeprobability 90 --rss_brand 1 --rss_attachmentzipcode 00920");
                return;
            }
            
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
            
            Console.WriteLine($"Populating realty opportunity with ID: {opportunityGuid.Value} with close probability: {closeProbability}%, brand value: {brandValue}, and attachment zip code: {attachmentZipCode}");
            
            // Populate the opportunity using the provided GUID, close probability, brand value, and attachment zip code
            bool success = PopulateCrmOpportunity(opportunityGuid.Value, closeProbability, brandValue, attachmentZipCode);
            
            if (success)
            {
                // Copy opportunity ID to clipboard
                string opportunityIdString = opportunityGuid.Value.ToString();
                try
                {
                    ClipboardService.SetText(opportunityIdString);
                    Console.WriteLine($"Realty opportunity populated successfully with ID: {opportunityIdString} (copied to clipboard)");
                }
                catch (Exception clipboardEx)
                {
                    Console.WriteLine($"Realty opportunity populated successfully with ID: {opportunityIdString}");
                    Console.WriteLine($"Note: Could not copy to clipboard: {clipboardEx.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Failed to populate realty opportunity with ID: {opportunityGuid.Value}");
            }
        }
        
        /// <summary>
        /// Parses the command line arguments for environment, opportunity GUID, and opportunity parameters
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Tuple containing environment, opportunity GUID (if provided), close probability, brand value, and attachment zip code</returns>
        private static Tuple<string, Guid?, int, int, string> ParseCommandLineArgs(string[] args)
        {
            string environment = DEFAULT_ENVIRONMENT;
            Guid? opportunityGuid = null;
            int closeProbability = 75; // Default close probability
            int brandValue = 0; // Default brand value (0 = Realty Search Solutions default)
            string attachmentZipCode = "00920"; // Default attachment zip code
            
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
                            environment = envArg;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Unknown environment '{envArg}'. Using {DEFAULT_ENVIRONMENT} instead.");
                        }
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse opportunity GUID parameter
                else if (arg == "--opportunity" || arg == "-o")
                {
                    if (i + 1 < args.Length)
                    {
                        string guidArg = args[i + 1];
                        try
                        {
                            opportunityGuid = Guid.Parse(guidArg);
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine($"Error: '{guidArg}' is not a valid GUID format.");
                        }
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse close probability parameter
                else if (arg == "--closeprobability")
                {
                    if (i + 1 < args.Length)
                    {
                        string probArg = args[i + 1];
                        if (int.TryParse(probArg, out int probability) && probability >= 0 && probability <= 100)
                        {
                            closeProbability = probability;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: '{probArg}' is not a valid probability (0-100). Using default value of 75.");
                        }
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse rss_brand parameter
                else if (arg == "--rss_brand")
                {
                    if (i + 1 < args.Length)
                    {
                        string brandArg = args[i + 1];
                        if (int.TryParse(brandArg, out int brand) && brand >= 0 && brand <= 2)
                        {
                            brandValue = brand;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: '{brandArg}' is not a valid brand value. Using default value of 0 (Realty Search Solutions).");
                        }
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse rss_attachmentzipcode parameter
                else if (arg == "--rss_attachmentzipcode")
                {
                    if (i + 1 < args.Length)
                    {
                        string zipArg = args[i + 1];
                        if (!string.IsNullOrWhiteSpace(zipArg))
                        {
                            attachmentZipCode = zipArg;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: '{zipArg}' is not a valid zip code. Using default value of 00920.");
                        }
                        i++; // Skip the next argument as we've already processed it
                    }
                }
            }
            
            return new Tuple<string, Guid?, int, int, string>(environment, opportunityGuid, closeProbability, brandValue, attachmentZipCode);
        }
        
        /// <summary>
        /// Loads configuration settings for the specified environment
        /// </summary>
        /// <param name="environment">The environment to load (DEV, UAT, or PROD)</param>
        private static void LoadEnvironmentConfiguration(string environment)
        {
            // First load settings from the base App.config
            ApplicationToken = ConfigurationManager.AppSettings["RealtyApplicationToken"] ?? string.Empty;
            BusinessUnitValue = ConfigurationManager.AppSettings["RealtyBusinessUnit"] ?? string.Empty;
            CrmServiceEnvironment = ConfigurationManager.AppSettings["RealtyCrmServiceEnvironment"] ?? string.Empty;
            
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
                        foreach (XmlNode setting in appSettings)
                        {
                            string key = setting.Attributes["key"]?.Value;
                            string value = setting.Attributes["value"]?.Value;
                            
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                            {
                                // Update the corresponding setting
                                switch (key)
                                {
                                    case "RealtyApplicationToken":
                                        ApplicationToken = value;
                                        break;
                                    case "RealtyBusinessUnit":
                                        BusinessUnitValue = value;
                                        break;
                                    case "RealtyCrmServiceEnvironment":
                                        CrmServiceEnvironment = value;
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
            
            Console.WriteLine($"Loaded configuration: Token={ApplicationToken}, BusinessUnit={BusinessUnitValue}, Environment={CrmServiceEnvironment}");
        }
        
        /// <summary>
        /// Validates that all required configuration settings are present
        /// </summary>
        private static bool ValidateConfigSettings()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(ApplicationToken))
            {
                Console.WriteLine("Error: RealtyApplicationToken is not configured.");
                isValid = false;
            }
            if (string.IsNullOrEmpty(BusinessUnitValue))
            {
                Console.WriteLine("Error: RealtyBusinessUnit is not configured.");
                isValid = false;
            }
            if (string.IsNullOrEmpty(CrmServiceEnvironment))
            {
                Console.WriteLine("Error: RealtyCrmServiceEnvironment is not configured.");
                isValid = false;
            }
            
            if (isValid)
            {
                Console.WriteLine("Configuration validation successful.");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Populates a realty opportunity in CRM using the CRM SDK or mock implementation
        /// </summary>
        /// <param name="opportunityId">The GUID of the opportunity to populate</param>
        /// <param name="closeProbability">The close probability to set for the opportunity (0-100)</param>
        /// <param name="brandValue">The brand value to set (default: 0 - Realty Search Solutions)</param>
        /// <param name="attachmentZipCode">The attachment zip code to set (default: 00920)</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        public static bool PopulateCrmOpportunity(Guid opportunityId, int closeProbability, int brandValue = 0, string attachmentZipCode = "00920")
        {
            // Validate required parameters
            if (opportunityId == Guid.Empty)
                throw new ArgumentException("Opportunity ID cannot be empty", nameof(opportunityId));
                
            Console.WriteLine($"Populating realty opportunity with ID: {opportunityId}");
            
            if (UseMockImplementation)
            {
                // Mock implementation for testing without CRM SDK
                Console.WriteLine("Using MOCK implementation (no actual CRM connection)...");
                
                // Log the opportunity population
                Console.WriteLine("Mock realty opportunity population for opportunity ID: " + opportunityId);
                Console.WriteLine($"Setting close probability to {closeProbability}%");
                Console.WriteLine($"Setting brand value to {brandValue} (0 = Realty Search Solutions default)");
                Console.WriteLine($"Setting attachment zip code to {attachmentZipCode}");
                Console.WriteLine("Realty opportunity would be populated with data in a real implementation");
                
                // Simulate success
                Console.WriteLine($"Mock realty opportunity populated successfully");
                
                return true;
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
                        (BusinessUnit)Enum.Parse(typeof(BusinessUnit), BusinessUnitValue ?? "RSS"), 
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
                    
                    // First, retrieve the opportunity entity to get its current state
                    Console.WriteLine($"Retrieving realty opportunity with ID: {opportunityId}");
                    
                    // Retrieve the opportunity entity using the CoreService API
                    var opportunity = coreService.Retrieve("opportunity", opportunityId, new Crm.CoreService.NuGet.Standard.Models.ColumnSet(true));
                    
                    if (opportunity == null)
                    {
                        throw new Exception($"Realty opportunity with ID {opportunityId} not found in CRM");
                    }
                    
                    Console.WriteLine("Realty opportunity found. Setting up population...");
                    
                    // Create an entity to update with the new field values
                    var opportunityUpdate = new Crm.CoreService.NuGet.Standard.Models.Entity
                    {
                        LogicalName = "opportunity",
                        Id = opportunityId,
                        Attributes = new Crm.CoreService.NuGet.Standard.Models.AttributeCollection()
                    };
                    
                    // Set realty opportunity fields - customize these based on Realty CRM requirements
                    // Note: Opportunity name is not updated to maintain consistency across environments
                    
                    // Example: Set the estimated close date to 30 days from now
                    opportunityUpdate["estimatedclosedate"] = DateTime.Now.AddDays(30);
                    
                    // Example: Set the estimated revenue for realty transactions
                    var revenue = new Crm.CoreService.NuGet.Standard.Models.Money(350000.00m);
                    opportunityUpdate["estimatedvalue"] = revenue;
                    
                    // Set the close probability from parameter
                    opportunityUpdate["closeprobability"] = closeProbability;
                    
                    // Set the brand value from parameter (Realty-specific branding)
                    opportunityUpdate["rss_brand"] = new Crm.CoreService.NuGet.Standard.Models.OptionSetValue(brandValue);
                    
                    // Set the attachment zip code from parameter
                    opportunityUpdate["rss_attachmentzipcode"] = attachmentZipCode;
                    
                    // Example: Set the opportunity rating to "Hot" for realty opportunities
                    opportunityUpdate["opportunityratingcode"] = new Crm.CoreService.NuGet.Standard.Models.OptionSetValue(3); // 3 = Hot
                    
                    // Realty-specific fields (customize based on actual Realty CRM schema)
                    // Example: Property type, location, etc. - these would need to be mapped to actual CRM fields
                    
                    // Update the opportunity in CRM
                    Console.WriteLine("Updating realty opportunity with new data...");
                    coreService.Update(opportunityUpdate);
                    
                    Console.WriteLine("Realty opportunity updated successfully");
                    return true;
#else
                    throw new NotSupportedException("Real CRM implementation is not available when compiled with MOCK_SDK");
#endif
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error populating realty opportunity in CRM: {0}", ex.Message);
                    Console.WriteLine("Stack trace: {0}", ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", ex.InnerException.Message);
                    }
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Sets up test data for CRM realty opportunity population
        /// </summary>
        public static void SetupCrmRealtyOpportunityTestData()
        {
            // Generate random test opportunity IDs and populate them
            for (int i = 1; i <= 3; i++)
            {
                var testOpportunityId = Guid.NewGuid();
                var success = PopulateCrmOpportunity(testOpportunityId, 75, 0, "00920"); // Using default 75% close probability, brand 0 (RSS default), and zip code 00920
                
                Console.WriteLine($"Populated test realty opportunity {i} with ID: {testOpportunityId}, success: {success}");
            }
        }
    }
}
