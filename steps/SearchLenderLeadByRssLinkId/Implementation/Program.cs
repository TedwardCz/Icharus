using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#if !MOCK_SDK
// Using CRM Core Service namespaces
using Crm.CoreService.NuGet.Standard;
using Crm.CoreService.NuGet.Standard.Enums;
using Crm.CoreService.NuGet.Standard.Interfaces;
using Crm.CoreService.NuGet.Standard.Models;
// Alias Microsoft SDK types to avoid conflicts
using MsQuery = Microsoft.Xrm.Sdk.Query;
using MsOptionSetValue = Microsoft.Xrm.Sdk.OptionSetValue;
#endif

namespace Icharus
{
    public class Program
    {
        // Configuration values from centralized config files
        private static string ApplicationToken = string.Empty;
        private static string BusinessUnitValue = string.Empty;
        private static string CrmServiceEnvironment = string.Empty;
        
        // Available environments
        private const string ENV_DEV = "DEV";
        private const string ENV_UAT = "UAT";
        private const string ENV_PROD = "PROD";
        
        // Mock implementation flag
        private static bool UseMockImplementation => 
#if MOCK_SDK
            true;
#else
            false;
#endif

        public static void Main(string[] args)
        {
            string environment = "";
            string rssLinkId = "";
            string outputFormat = "human";
            
            try
            {
                // Parse command line arguments
                var parsedArgs = ParseArguments(args);
                
                if (parsedArgs.ContainsKey("help") || parsedArgs.ContainsKey("h"))
                {
                    ShowHelp();
                    return;
                }
                
                // Validate required arguments
                if (!parsedArgs.ContainsKey("env"))
                {
                    Console.WriteLine("Error: --env parameter is required");
                    ShowHelp();
                    Environment.Exit(1);
                }
                
                if (!parsedArgs.ContainsKey("rsslinkid"))
                {
                    Console.WriteLine("Error: --rsslinkid parameter is required");
                    ShowHelp();
                    Environment.Exit(1);
                }
                
                environment = parsedArgs["env"];
                rssLinkId = parsedArgs["rsslinkid"];
                outputFormat = parsedArgs.ContainsKey("output-format") ? parsedArgs["output-format"].ToLower() : "human";
                
                // Validate environment
                if (environment != ENV_DEV && environment != ENV_UAT && environment != ENV_PROD)
                {
                    Console.WriteLine($"Error: Invalid environment '{environment}'. Must be DEV, UAT, or PROD");
                    Environment.Exit(1);
                }
                
                // Validate RSS Link ID format
                if (!Guid.TryParse(rssLinkId, out Guid rssLinkGuid))
                {
                    Console.WriteLine($"Error: Invalid RSS Link ID format '{rssLinkId}'. Must be a valid GUID");
                    Environment.Exit(1);
                }
                
                // Validate output format
                if (outputFormat != "human" && outputFormat != "json")
                {
                    Console.WriteLine($"Error: Invalid output format '{outputFormat}'. Must be 'human' or 'json'");
                    Environment.Exit(1);
                }
                
                if (outputFormat == "human")
                {
                    Console.WriteLine($"Searching for lender leads with RSS Link ID: {rssLinkId}");
                    Console.WriteLine($"Environment: {environment}");
                }
                
                // Load environment configuration
                LoadEnvironmentConfiguration(environment, outputFormat == "human");
                
                // Search for leads
                var startTime = DateTime.Now;
                var leads = SearchLeadsByRssLinkId(rssLinkGuid, outputFormat == "human");
                var executionTime = DateTime.Now - startTime;
                
                // Display results based on output format
                if (outputFormat == "json")
                {
                    OutputJson(leads, rssLinkGuid, environment, executionTime);
                }
                else
                {
                    DisplaySearchResults(leads, rssLinkGuid);
                    Console.WriteLine("Search completed successfully");
                }
            }
            catch (Exception ex)
            {
                if (outputFormat == "json")
                {
                    OutputJsonError(ex.Message, rssLinkId, environment);
                }
                else
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                Environment.Exit(1);
            }
        }
        
        private static Dictionary<string, string> ParseArguments(string[] args)
        {
            var parsedArgs = new Dictionary<string, string>();
            
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    string key = args[i].Substring(2).ToLower();
                    
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        parsedArgs[key] = args[i + 1];
                        i++; // Skip the value
                    }
                    else
                    {
                        parsedArgs[key] = "true"; // Flag without value
                    }
                }
            }
            
            return parsedArgs;
        }
        
        private static void ShowHelp()
        {
            Console.WriteLine("SearchLenderLeadByRssLinkId - Search for lender leads by RSS Link ID");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  SearchLenderLeadByRssLinkId.exe --env <environment> --rsslinkid <guid>");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  --env             Environment (DEV, UAT, or PROD)");
            Console.WriteLine("  --rsslinkid       RSS Link ID (GUID format)");
            Console.WriteLine("  --output-format   Output format (default: human, json)");
            Console.WriteLine("  --help, -h        Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  SearchLenderLeadByRssLinkId.exe --env UAT --rsslinkid 12345678-1234-1234-1234-123456789abc");
            Console.WriteLine("  SearchLenderLeadByRssLinkId.exe --env PROD --rsslinkid 87654321-4321-4321-4321-cba987654321");
            Console.WriteLine("  SearchLenderLeadByRssLinkId.exe --env UAT --rsslinkid 12345678-1234-1234-1234-123456789abc --output-format json");
        }
        
        private static void OutputJson(List<LeadSearchResult> leads, Guid searchedRssLinkId, string environment, TimeSpan executionTime)
        {
            var jsonOutput = new
            {
                success = true,
                executionTime = $"{executionTime.TotalSeconds:F1}s",
                environment = environment,
                searchCriteria = new
                {
                    rssLinkId = searchedRssLinkId.ToString()
                },
                results = leads.Select(lead => new
                {
                    leadId = lead.LeadId.ToString(),
                    firstName = lead.FirstName,
                    lastName = lead.LastName,
                    email = lead.Email,
                    phone = lead.Phone,
                    rssLinkId = lead.RssLinkId.ToString(),
                    createdOn = lead.CreatedOn.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    statusCode = lead.StatusCode
                }).ToArray(),
                count = leads.Count,
                errors = new string[0]
            };
            
            string json = System.Text.Json.JsonSerializer.Serialize(jsonOutput, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            Console.WriteLine(json);
        }
        
        private static void OutputJsonError(string errorMessage, string rssLinkId, string environment)
        {
            var jsonOutput = new
            {
                success = false,
                executionTime = "0.0s",
                environment = environment,
                searchCriteria = new
                {
                    rssLinkId = rssLinkId
                },
                results = new object[0],
                count = 0,
                errors = new[] { errorMessage }
            };
            
            string json = System.Text.Json.JsonSerializer.Serialize(jsonOutput, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            Console.WriteLine(json);
        }
        
        private static void LoadEnvironmentConfiguration(string environment, bool verbose = true)
        {
            try
            {
                // Load configuration from centralized config files
                string centralConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "app.env.config", $"App.{environment}.config");
                
                if (!File.Exists(centralConfigPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {centralConfigPath}");
                }
                
                if (verbose)
                    Console.WriteLine($"Loading configuration from: {centralConfigPath}");
                
                // Map the config file to the application configuration
                var configMap = new ExeConfigurationFileMap();
                configMap.ExeConfigFilename = centralConfigPath;
                var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                
                // Read configuration values
                ApplicationToken = config.AppSettings.Settings["LenderApplicationToken"]?.Value;
                BusinessUnitValue = config.AppSettings.Settings["LenderBusinessUnit"]?.Value;
                CrmServiceEnvironment = config.AppSettings.Settings["LenderCrmServiceEnvironment"]?.Value;
                
                // Validate required configuration values
                if (string.IsNullOrEmpty(ApplicationToken))
                    throw new ConfigurationErrorsException("LenderApplicationToken not found in configuration");
                    
                if (string.IsNullOrEmpty(BusinessUnitValue))
                    throw new ConfigurationErrorsException("LenderBusinessUnit not found in configuration");
                    
                if (string.IsNullOrEmpty(CrmServiceEnvironment))
                    throw new ConfigurationErrorsException("LenderCrmServiceEnvironment not found in configuration");
                
                if (verbose)
                {
                    Console.WriteLine($"Configuration loaded successfully for {environment} environment");
                    Console.WriteLine($"Business Unit: {BusinessUnitValue}");
                    Console.WriteLine($"CRM Environment: {CrmServiceEnvironment}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load configuration for environment '{environment}': {ex.Message}", ex);
            }
        }
        
        private static List<LeadSearchResult> SearchLeadsByRssLinkId(Guid rssLinkId, bool verbose = true)
        {
            if (verbose)
                Console.WriteLine($"Searching for leads with RSS Link ID: {rssLinkId}");
            
            if (UseMockImplementation)
            {
                return SearchLeadsByRssLinkIdMock(rssLinkId, verbose);
            }
            else
            {
                return SearchLeadsByRssLinkIdCrm(rssLinkId, verbose);
            }
        }
        
        private static List<LeadSearchResult> SearchLeadsByRssLinkIdMock(Guid rssLinkId, bool verbose = true)
        {
            if (verbose)
                Console.WriteLine("Using MOCK implementation (no actual CRM connection)...");
            
            // Generate mock results for testing
            var mockResults = new List<LeadSearchResult>();
            
            // Simulate finding 1-3 leads
            var random = new Random();
            int leadCount = random.Next(1, 4);
            
            for (int i = 0; i < leadCount; i++)
            {
                mockResults.Add(new LeadSearchResult
                {
                    LeadId = Guid.NewGuid(),
                    FirstName = $"MockFirst{i + 1}",
                    LastName = $"MockLast{i + 1}",
                    Email = $"mock{i + 1}@example.com",
                    Phone = $"555-000-{1000 + i}",
                    RssLinkId = rssLinkId,
                    CreatedOn = DateTime.Now.AddDays(-random.Next(1, 30)),
                    StatusCode = random.Next(1, 5)
                });
            }
            
            if (verbose)
                Console.WriteLine($"Mock search found {mockResults.Count} lead(s)");
            return mockResults;
        }
        
        private static List<LeadSearchResult> SearchLeadsByRssLinkIdCrm(Guid rssLinkId, bool verbose = true)
        {
            try
            {
#if !MOCK_SDK
                // Initialize the CRM service
                if (verbose)
                {
                    Console.WriteLine("Initializing CRM connection...");
                    Console.WriteLine($"Application Token: {ApplicationToken}");
                    Console.WriteLine($"Business Unit: {BusinessUnitValue}, Environment: {CrmServiceEnvironment}");
                }
                
                var builder = new CrmCoreServiceBuilder(ApplicationToken);
                var builderOptions = new CrmCoreServiceBuilderOptions(
                    (BusinessUnit)Enum.Parse(typeof(BusinessUnit), BusinessUnitValue), 
                    CrmServiceEnvironment);
                
                if (verbose)
                    Console.WriteLine("Attempting to connect to CRM service...");
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                Crm.CoreService.NuGet.Standard.Interfaces.IOrganizationService coreService;
                
                try 
                {
                    if (verbose)
                        Console.WriteLine("Building CRM service...");
                    coreService = builder.BuildService(httpClient, builderOptions);
                    if (verbose)
                        Console.WriteLine("CRM service connection established successfully.");
                }
                catch (TaskCanceledException) 
                {
                    if (verbose)
                        Console.WriteLine("Connection to CRM timed out after 30 seconds.");
                    throw new TimeoutException("Connection to CRM timed out. Please check network connectivity and VPN status.");
                }
                catch (HttpRequestException ex) 
                {
                    if (verbose)
                        Console.WriteLine($"HTTP request error when connecting to CRM: {ex.Message}");
                    throw new Exception("Failed to connect to CRM service. Please check network connectivity and VPN status.", ex);
                }
                catch (Exception ex) 
                {
                    if (verbose)
                        Console.WriteLine($"Unexpected error when connecting to CRM: {ex.Message}");
                    throw new Exception("Unexpected error when connecting to CRM service.", ex);
                }
                
                // Create query to search for leads by RSS Link ID
                if (verbose)
                    Console.WriteLine("Building search query...");
                var query = new QueryExpression("lead");
                
                // Add columns to retrieve
                query.ColumnSet = new ColumnSet(
                    "leadid",
                    "firstname", 
                    "lastname",
                    "emailaddress1",
                    "vu_mostrecentphonenumber",
                    "telephone1",
                    "mobilephone",
                    "vu_rsslinkid",
                    "createdon",
                    "statuscode"
                );
                
                // Add filter for RSS Link ID (stored as string in CRM)
                query.Criteria = new FilterExpression(LogicalOperator.And);
                query.Criteria.AddCondition("vu_rsslinkid", ConditionOperator.Equal, rssLinkId.ToString());
                
                // Add ordering
                query.AddOrder("createdon", OrderType.Descending);
                
                if (verbose)
                    Console.WriteLine($"Executing search query for RSS Link ID: {rssLinkId}");
                
                // Execute the query
                var response = coreService.RetrieveMultiple(query);
                
                if (verbose)
                    Console.WriteLine($"Query executed successfully. Found {response.Entities.Count} lead(s)");
                
                // Convert results to our model
                var results = new List<LeadSearchResult>();
                
                foreach (var entity in response.Entities)
                {
                    var result = new LeadSearchResult
                    {
                        LeadId = entity.Id,
                        FirstName = entity.GetAttributeValue<string>("firstname") ?? string.Empty,
                        LastName = entity.GetAttributeValue<string>("lastname") ?? string.Empty,
                        Email = entity.GetAttributeValue<string>("emailaddress1") ?? string.Empty,
                        Phone = entity.GetAttributeValue<string>("vu_mostrecentphonenumber") ?? 
                               entity.GetAttributeValue<string>("telephone1") ?? 
                               entity.GetAttributeValue<string>("mobilephone") ?? string.Empty,
                        RssLinkId = Guid.Parse(entity.GetAttributeValue<string>("vu_rsslinkid") ?? Guid.Empty.ToString()),
                        CreatedOn = entity.GetAttributeValue<DateTime>("createdon"),
                        StatusCode = entity.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0
                    };
                    
                    results.Add(result);
                }
                
                return results;
#else
                throw new NotSupportedException("Real CRM implementation is not available when compiled with MOCK_SDK");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching leads in CRM: {ex.Message}");
                throw new Exception("Failed to search leads in CRM", ex);
            }
        }
        
        private static void DisplaySearchResults(List<LeadSearchResult> leads, Guid searchedRssLinkId)
        {
            Console.WriteLine();
            Console.WriteLine("=== SEARCH RESULTS ===");
            Console.WriteLine($"RSS Link ID: {searchedRssLinkId}");
            Console.WriteLine($"Found {leads.Count} lead(s)");
            Console.WriteLine();
            
            if (leads.Count == 0)
            {
                Console.WriteLine("No leads found with the specified RSS Link ID.");
                return;
            }
            
            for (int i = 0; i < leads.Count; i++)
            {
                var lead = leads[i];
                Console.WriteLine($"Lead #{i + 1}:");
                Console.WriteLine($"  Lead ID: {lead.LeadId}");
                Console.WriteLine($"  Name: {lead.FirstName} {lead.LastName}");
                Console.WriteLine($"  Email: {lead.Email ?? "N/A"}");
                Console.WriteLine($"  Phone: {lead.Phone ?? "N/A"}");
                Console.WriteLine($"  RSS Link ID: {lead.RssLinkId}");
                Console.WriteLine($"  Created: {lead.CreatedOn:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Status Code: {lead.StatusCode}");
                
                if (i < leads.Count - 1)
                {
                    Console.WriteLine();
                }
            }
            
            // Copy first lead ID to clipboard if available
            if (leads.Count > 0)
            {
                try
                {
                    TextCopy.ClipboardService.SetText(leads[0].LeadId.ToString());
                    Console.WriteLine();
                    Console.WriteLine($"First lead ID copied to clipboard: {leads[0].LeadId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Note: Could not copy to clipboard: {ex.Message}");
                }
            }
        }
    }
    
    public class LeadSearchResult
    {
        public Guid LeadId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid RssLinkId { get; set; }
        public DateTime CreatedOn { get; set; }
        public int StatusCode { get; set; }
    }
}
