using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        private static string? ApplicationToken;
        private static string? BusinessUnitValue;
        private static string? CrmServiceEnvironment;
        
        // Team GUID for lead ownership
        private static string? LeadTeamId;
        
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
        
        static void Main(string[] args)
        {
            Console.WriteLine("Icharus Realty CRM Lead Qualification Tool");
            Console.WriteLine("==========================================");
            
            // Parse command line arguments
            var parsedArgs = ParseCommandLineArgs(args);
            string environment = parsedArgs.Item1;
            Guid? leadGuid = parsedArgs.Item2;
            
            // Check if lead GUID was provided
            if (!leadGuid.HasValue)
            {
                Console.WriteLine("Error: Lead GUID is required.");
                Console.WriteLine("Usage: dotnet run [--env|-e <environment>] --lead|-l <leadGuid>");
                Console.WriteLine("Example: dotnet run --lead 12345678-1234-1234-1234-123456789012");
                Console.WriteLine("Example: dotnet run -e PROD -l 12345678-1234-1234-1234-123456789012");
                return;
            }
            
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
            
            Console.WriteLine($"Qualifying realty lead with ID: {leadGuid.Value}");
            
            // Qualify the lead using the provided GUID
            Guid resultId = QualifyCrmLead(leadGuid.Value);
            
            // Copy result ID to clipboard
            string resultIdString = resultId.ToString();
            try
            {
                ClipboardService.SetText(resultIdString);
                Console.WriteLine($"Realty lead qualified successfully with result ID: {resultId} (copied to clipboard)");
            }
            catch (Exception clipboardEx)
            {
                Console.WriteLine($"Realty lead qualified successfully with result ID: {resultId}");
                Console.WriteLine($"Note: Could not copy to clipboard: {clipboardEx.Message}");
            }
        }
        
        /// <summary>
        /// Parses the command line arguments for environment and lead GUID
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Tuple containing environment and lead GUID (if provided)</returns>
        private static Tuple<string, Guid?> ParseCommandLineArgs(string[] args)
        {
            string environment = DEFAULT_ENVIRONMENT;
            Guid? leadGuid = null;
            
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
                // Parse lead GUID parameter
                else if (arg == "--lead" || arg == "-l")
                {
                    if (i + 1 < args.Length)
                    {
                        string guidArg = args[i + 1];
                        try
                        {
                            leadGuid = Guid.Parse(guidArg);
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine($"Error: '{guidArg}' is not a valid GUID format.");
                        }
                        i++; // Skip the next argument as we've already processed it
                    }
                }
            }
            
            return new Tuple<string, Guid?>(environment, leadGuid);
        }
        
        /// <summary>
        /// Loads configuration settings for the specified environment
        /// </summary>
        /// <param name="environment">The environment to load (DEV, UAT, or PROD)</param>
        private static void LoadEnvironmentConfiguration(string environment)
        {
            // First load settings from the base App.config
            ApplicationToken = ConfigurationManager.AppSettings["RealtyApplicationToken"];
            BusinessUnitValue = ConfigurationManager.AppSettings["RealtyBusinessUnit"];
            CrmServiceEnvironment = ConfigurationManager.AppSettings["RealtyCrmServiceEnvironment"];
            LeadTeamId = ConfigurationManager.AppSettings["RealtyLeadAdmin"];
            
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
                    
                    Console.WriteLine($"Found {doc.SelectNodes("//appSettings/add")?.Count ?? 0} settings in environment config file");
                    
                    // Extract settings from the environment-specific config
                    XmlNodeList appSettings = doc.SelectNodes("//appSettings/add");
                    if (appSettings != null)
                    {
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
            if (string.IsNullOrEmpty(LeadTeamId))
            {
                Console.WriteLine("Error: RealtyLeadAdmin (Lead Team ID) is not configured.");
                isValid = false;
            }
            else if (!Guid.TryParse(LeadTeamId, out _))
            {
                Console.WriteLine($"Error: RealtyLeadAdmin value '{LeadTeamId}' is not a valid GUID");
                isValid = false;
            }
            
            if (isValid)
            {
                Console.WriteLine("Configuration validation successful.");
                Console.WriteLine($"Environment: {CrmServiceEnvironment}, BusinessUnit: {BusinessUnitValue}");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Qualifies a realty lead in CRM using the CRM SDK or mock implementation
        /// </summary>
        /// <param name="leadId">The GUID of the lead to qualify</param>
        /// <returns>The GUID of the qualified lead or resulting opportunity</returns>
        public static Guid QualifyCrmLead(Guid leadId)
        {
            // Validate required parameters
            if (leadId == Guid.Empty)
                throw new ArgumentException("Lead ID cannot be empty", nameof(leadId));
                
            Console.WriteLine($"Qualifying realty lead with ID: {leadId}");
            
            if (UseMockImplementation)
            {
                // Mock implementation for testing without CRM SDK
                Console.WriteLine("Using MOCK implementation (no actual CRM connection)...");
                
                // Log the lead qualification
                Console.WriteLine("Mock realty lead qualification for lead ID: " + leadId);
                Console.WriteLine("Realty lead would be qualified to an opportunity and contact in a real implementation");
                
                // Generate a random GUID for the result ID (simulating an opportunity ID)
                Guid resultId = Guid.NewGuid();
                Console.WriteLine($"Mock realty lead qualified with result ID: {resultId}");
                
                // Copy mock result ID to clipboard
                try
                {
                    ClipboardService.SetText(resultId.ToString());
                    Console.WriteLine($"Result ID copied to clipboard");
                }
                catch (Exception clipboardEx)
                {
                    Console.WriteLine($"Note: Could not copy to clipboard: {clipboardEx.Message}");
                }
                
                return resultId;
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
                    
                    // First, retrieve the lead entity to get its current state
                    Console.WriteLine($"Retrieving realty lead with ID: {leadId}");
                    
                    // Retrieve the lead entity using the CoreService API
                    var lead = coreService.Retrieve("lead", leadId, new Crm.CoreService.NuGet.Standard.Models.ColumnSet(true));
                    
                    if (lead == null)
                    {
                        throw new Exception($"Realty lead with ID {leadId} not found in CRM");
                    }
                    
                    Console.WriteLine("Realty lead found. Setting up qualification...");
                    
                    // Create a QualifyLead request using OrganizationRequest
                    // This will properly convert the lead into an opportunity and contact
                    Console.WriteLine("Creating QualifyLead request...");
                    
                    // Create the request as an OrganizationRequest with parameters
                    var qualifyLeadRequest = new Crm.CoreService.NuGet.Standard.Models.OrganizationRequest("QualifyLead");
                    
                    // Set the required parameters
                    qualifyLeadRequest.Parameters["LeadId"] = new Crm.CoreService.NuGet.Standard.Models.EntityReference("lead", leadId);
                    qualifyLeadRequest.Parameters["Status"] = new Crm.CoreService.NuGet.Standard.Models.OptionSetValue(3); // Qualified status
                    qualifyLeadRequest.Parameters["CreateOpportunity"] = true;
                    qualifyLeadRequest.Parameters["CreateAccount"] = true;
                    qualifyLeadRequest.Parameters["CreateContact"] = true;
                    
                    try
                    {
                        // Execute the qualification request
                        Console.WriteLine("Executing realty lead qualification request...");
                        var response = coreService.Execute(qualifyLeadRequest);
                        
                        // Get the created entities from the response
                        if (response.Results.ContainsKey("CreatedEntities") && 
                            response.Results["CreatedEntities"] is Crm.CoreService.NuGet.Standard.Models.EntityReferenceCollection createdEntities)
                        {
                            // Find the opportunity entity in the response
                            foreach (var entityRef in createdEntities)
                            {
                                if (entityRef.LogicalName == "opportunity")
                                {
                                    Console.WriteLine($"Realty lead qualified successfully. Created opportunity with ID: {entityRef.Id}");
                                    return entityRef.Id;
                                }
                            }
                        }
                        
                        // If we couldn't find the opportunity ID, return the lead ID
                        Console.WriteLine("Realty lead qualified successfully, but couldn't retrieve opportunity ID.");
                        return leadId;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during realty lead qualification: {ex.Message}");
                        throw;
                    }
#else
                    throw new NotSupportedException("Real CRM implementation is not available when compiled with MOCK_SDK");
#endif
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error qualifying realty lead in CRM: {0}", ex.Message);
                    throw new Exception("Failed to qualify realty lead in CRM", ex);
                }
            }
        }
        
        /// <summary>
        /// Sets up test data for CRM realty lead qualification
        /// </summary>
        public static void SetupCrmRealtyLeadTestData()
        {
            // Generate random test lead IDs and qualify them
            for (int i = 1; i <= 5; i++)
            {
                var testLeadId = Guid.NewGuid();
                var resultId = QualifyCrmLead(testLeadId);
                
                Console.WriteLine($"Qualified test realty lead {i} with ID: {testLeadId}, result ID: {resultId}");
            }
        }
    }
}
