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
                
                environment = parsedArgs["env"].ToUpper();
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
                    Console.WriteLine($"Searching for Realty opportunities with RSS Link ID: {rssLinkId}");
                    Console.WriteLine($"Environment: {environment}");
                }
                
                // Load environment configuration
                LoadEnvironmentConfiguration(environment, outputFormat == "human");
                
                // Search for opportunities
                var startTime = DateTime.Now;
                var opportunities = SearchOpportunitiesByRssLinkId(rssLinkGuid, outputFormat == "human");
                var executionTime = DateTime.Now - startTime;
                
                // Display results based on output format
                if (outputFormat == "json")
                {
                    OutputJson(opportunities, rssLinkGuid, environment, executionTime);
                }
                else
                {
                    DisplaySearchResults(opportunities, rssLinkGuid, environment, executionTime);
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
            Console.WriteLine("SearchRealtyOpportunityByRssLinkId - Search for realty opportunities by RSS Link ID");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  SearchRealtyOpportunityByRssLinkId.exe --env <environment> --rsslinkid <guid>");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  --env             Environment (DEV, UAT, or PROD)");
            Console.WriteLine("  --rsslinkid       RSS Link ID (GUID format)");
            Console.WriteLine("  --output-format   Output format (default: human, json)");
            Console.WriteLine("  --help, -h        Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  SearchRealtyOpportunityByRssLinkId.exe --env UAT --rsslinkid 12345678-1234-1234-1234-123456789abc");
            Console.WriteLine("  SearchRealtyOpportunityByRssLinkId.exe --env PROD --rsslinkid 87654321-4321-4321-4321-cba987654321");
            Console.WriteLine("  SearchRealtyOpportunityByRssLinkId.exe --env UAT --rsslinkid 12345678-1234-1234-1234-123456789abc --output-format json");
        }
        
        /// <summary>
        /// Data model for opportunity search results
        /// </summary>
        public class OpportunitySearchResult
        {
            public Guid OpportunityId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string CustomerName { get; set; } = string.Empty;
            public Guid CustomerGuid { get; set; }
            public Guid RssLinkId { get; set; }
            public DateTime CreatedOn { get; set; }
            public string StatusCode { get; set; } = string.Empty;
            public decimal EstimatedValue { get; set; }
            public DateTime EstimatedCloseDate { get; set; }
        }
        
        /// <summary>
        /// Loads environment configuration from centralized config files
        /// </summary>
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
                
                // Read configuration values for REALTY (not Lender)
                ApplicationToken = config.AppSettings.Settings["RealtyApplicationToken"]?.Value;
                BusinessUnitValue = config.AppSettings.Settings["RealtyBusinessUnit"]?.Value;
                CrmServiceEnvironment = config.AppSettings.Settings["RealtyCrmServiceEnvironment"]?.Value;
                
                // Validate required configuration values
                if (string.IsNullOrEmpty(ApplicationToken))
                    throw new ConfigurationErrorsException("RealtyApplicationToken not found in configuration");
                    
                if (string.IsNullOrEmpty(BusinessUnitValue))
                    throw new ConfigurationErrorsException("RealtyBusinessUnit not found in configuration");
                    
                if (string.IsNullOrEmpty(CrmServiceEnvironment))
                    throw new ConfigurationErrorsException("RealtyCrmServiceEnvironment not found in configuration");

                if (verbose)
                {
                    Console.WriteLine("Configuration loaded successfully:");
                    Console.WriteLine($"  Application Token: {ApplicationToken.Substring(0, 8)}...");
                    Console.WriteLine($"  Business Unit: {BusinessUnitValue}");
                    Console.WriteLine($"  CRM Environment: {CrmServiceEnvironment}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load configuration for environment '{environment}': {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Searches for opportunities by RSS Link ID using either mock or real CRM implementation
        /// </summary>
        private static List<OpportunitySearchResult> SearchOpportunitiesByRssLinkId(Guid rssLinkId, bool verbose = true)
        {
            if (UseMockImplementation)
            {
                return SearchOpportunitiesByRssLinkIdMock(rssLinkId, verbose);
            }
            else
            {
                return SearchOpportunitiesByRssLinkIdCrm(rssLinkId, verbose);
            }
        }
        
        /// <summary>
        /// Mock implementation for testing without CRM connection
        /// </summary>
        private static List<OpportunitySearchResult> SearchOpportunitiesByRssLinkIdMock(Guid rssLinkId, bool verbose = true)
        {
            if (verbose)
                Console.WriteLine("Using MOCK implementation (no actual CRM connection)...");
            
            // Generate mock results for testing
            var mockResults = new List<OpportunitySearchResult>();
            
            // Simulate finding 1-3 opportunities
            var random = new Random();
            int opportunityCount = random.Next(1, 4);
            
            for (int i = 0; i < opportunityCount; i++)
            {
                mockResults.Add(new OpportunitySearchResult
                {
                    OpportunityId = Guid.NewGuid(),
                    Name = $"Mock Realty Opportunity {i + 1}",
                    CustomerName = $"Mock Customer {i + 1}",
                    CustomerGuid = Guid.NewGuid(),
                    RssLinkId = rssLinkId,
                    CreatedOn = DateTime.Now.AddDays(-random.Next(1, 30)),
                    StatusCode = i == 0 ? "Open" : "In Progress",
                    EstimatedValue = random.Next(100000, 500000),
                    EstimatedCloseDate = DateTime.Now.AddDays(random.Next(30, 90))
                });
            }
            
            if (verbose)
                Console.WriteLine($"Mock search completed. Generated {mockResults.Count} opportunity(s)");
            
            return mockResults;
        }
        
        /// <summary>
        /// Real CRM implementation for searching opportunities
        /// </summary>
        private static List<OpportunitySearchResult> SearchOpportunitiesByRssLinkIdCrm(Guid rssLinkId, bool verbose = true)
        {
            try
            {
#if !MOCK_SDK
                // Initialize the CRM service
                if (verbose)
                {
                    Console.WriteLine("Initializing CRM connection...");
                    Console.WriteLine($"  Environment: {CrmServiceEnvironment}");
                    Console.WriteLine($"  Business Unit: {BusinessUnitValue}");
                }
                
                var builder = new CrmCoreServiceBuilder(ApplicationToken);
                var builderOptions = new CrmCoreServiceBuilderOptions(
                    (BusinessUnit)Enum.Parse(typeof(BusinessUnit), BusinessUnitValue), 
                    CrmServiceEnvironment);
                
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                Crm.CoreService.NuGet.Standard.Interfaces.IOrganizationService coreService;
                
                try 
                {
                    coreService = builder.BuildService(httpClient, builderOptions);
                    if (verbose)
                        Console.WriteLine("CRM connection established successfully");
                }
                catch (TaskCanceledException) 
                {
                    throw new TimeoutException("Connection to CRM timed out. Please check network connectivity and VPN status.");
                }
                catch (HttpRequestException ex) 
                {
                    throw new Exception("Failed to connect to CRM service. Please check network connectivity and VPN status.", ex);
                }
                catch (Exception ex) 
                {
                    throw new Exception("Unexpected error when connecting to CRM service.", ex);
                }
                
                // Create query to search for opportunities by RSS Link ID
                if (verbose)
                    Console.WriteLine("Building search query...");
                var query = new QueryExpression("opportunity");
                
                // Add columns to retrieve
                query.ColumnSet = new ColumnSet(
                    "opportunityid",
                    "name",
                    "customerid",
                    "rss_rsslinkid",
                    "createdon",
                    "statuscode",
                    "estimatedvalue",
                    "estimatedclosedate"
                );
                
                // Add filter for RSS Link ID (stored as string in CRM)
                query.Criteria = new FilterExpression(LogicalOperator.And);
                query.Criteria.AddCondition("rss_rsslinkid", ConditionOperator.Equal, rssLinkId.ToString());
                
                // Add ordering
                query.AddOrder("createdon", OrderType.Descending);
                
                if (verbose)
                    Console.WriteLine($"Executing search query for RSS Link ID: {rssLinkId}");
                
                // Execute the query
                var response = coreService.RetrieveMultiple(query);
                
                if (verbose)
                    Console.WriteLine($"Query executed successfully. Found {response.Entities.Count} opportunity(s)");
                
                // Convert results to our model
                var results = new List<OpportunitySearchResult>();
                
                foreach (var entity in response.Entities)
                {
                    var result = new OpportunitySearchResult
                    {
                        OpportunityId = entity.Id,
                        Name = entity.GetAttributeValue<string>("name") ?? "",
                        CustomerName = entity.FormattedValues.ContainsKey("customerid") ? 
                                       entity.FormattedValues["customerid"] : 
                                       entity.GetAttributeValue<EntityReference>("customerid")?.Id.ToString() ?? "",
                        CustomerGuid = entity.GetAttributeValue<EntityReference>("customerid")?.Id ?? Guid.Empty,
                        RssLinkId = rssLinkId,
                        CreatedOn = entity.GetAttributeValue<DateTime>("createdon"),
                        StatusCode = entity.FormattedValues.ContainsKey("statuscode") ? entity.FormattedValues["statuscode"] : "",
                        EstimatedValue = entity.GetAttributeValue<Money>("estimatedvalue")?.Value ?? 0,
                        EstimatedCloseDate = entity.GetAttributeValue<DateTime>("estimatedclosedate")
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
                throw new Exception($"Failed to search opportunities in CRM: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Displays search results in human-readable format
        /// </summary>
        private static void DisplaySearchResults(List<OpportunitySearchResult> opportunities, Guid rssLinkId, string environment, TimeSpan executionTime)
        {
            Console.WriteLine();
            Console.WriteLine($"Search Results for RSS Link ID: {rssLinkId}");
            Console.WriteLine($"Environment: {environment}");
            Console.WriteLine($"Execution Time: {executionTime.TotalMilliseconds:F0}ms");
            Console.WriteLine($"Found {opportunities.Count} opportunity(s)");
            Console.WriteLine();
            
            if (opportunities.Count == 0)
            {
                Console.WriteLine("No opportunities found with the specified RSS Link ID.");
                return;
            }
            
            for (int i = 0; i < opportunities.Count; i++)
            {
                var opp = opportunities[i];
                Console.WriteLine($"Opportunity #{i + 1}:");
                Console.WriteLine($"  ID: {opp.OpportunityId}");
                Console.WriteLine($"  Name: {opp.Name}");
                Console.WriteLine($"  Customer: {opp.CustomerName}");
                Console.WriteLine($"  Customer GUID: {opp.CustomerGuid}");
                Console.WriteLine($"  Status: {opp.StatusCode}");
                Console.WriteLine($"  Estimated Value: ${opp.EstimatedValue:N2}");
                Console.WriteLine($"  Estimated Close Date: {opp.EstimatedCloseDate:yyyy-MM-dd}");
                Console.WriteLine($"  Created: {opp.CreatedOn:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// Outputs results in JSON format
        /// </summary>
        private static void OutputJson(List<OpportunitySearchResult> opportunities, Guid rssLinkId, string environment, TimeSpan executionTime)
        {
            var result = new
            {
                success = true,
                rssLinkId = rssLinkId,
                environment = environment,
                executionTimeMs = (int)executionTime.TotalMilliseconds,
                count = opportunities.Count,
                opportunities = opportunities.Select(o => new
                {
                    opportunityId = o.OpportunityId,
                    name = o.Name,
                    customerName = o.CustomerName,
                    customerGuid = o.CustomerGuid,
                    statusCode = o.StatusCode,
                    estimatedValue = o.EstimatedValue,
                    estimatedCloseDate = o.EstimatedCloseDate.ToString("yyyy-MM-dd"),
                    createdOn = o.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToArray()
            };
            
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        }
        
        /// <summary>
        /// Outputs error in JSON format
        /// </summary>
        private static void OutputJsonError(string errorMessage, string rssLinkId, string environment)
        {
            var result = new
            {
                success = false,
                rssLinkId = rssLinkId,
                environment = environment,
                executionTimeMs = 0,
                count = 0,
                opportunities = new object[0],
                error = errorMessage
            };
            
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
