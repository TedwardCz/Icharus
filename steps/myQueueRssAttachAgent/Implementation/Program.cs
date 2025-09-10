using System;
using System.Configuration;
using System.IO;
using System.Xml;

namespace myQueueRssAttachAgent
{
    class Program
    {
        // Configuration settings
        private static string ApplicationToken = string.Empty;
        private static string BaseUrl = string.Empty;
        private static string Environment = "UAT";
        private static bool UseMockImplementation = false;
        
        // Default environment
        private const string DEFAULT_ENVIRONMENT = "UAT";

        static void Main(string[] args)
        {
            Console.WriteLine("Icharus myQueue RSS Attach Agent Tool");
            Console.WriteLine("====================================");
            
            // Parse command line arguments
            var parsedArgs = ParseCommandLineArgs(args);
            string environment = parsedArgs.Item1;
            Guid? opportunityGuid = parsedArgs.Item2;
            string city = parsedArgs.Item3;
            string state = parsedArgs.Item4;
            string zipCode = parsedArgs.Item5;
            
            // Check if opportunity GUID was provided
            if (!opportunityGuid.HasValue)
            {
                Console.WriteLine("Error: Opportunity GUID is required.");
                Console.WriteLine("Usage: dotnet run [--env|-e <environment>] --opportunity|-o <opportunityGuid> [--city <city>] [--state <state>] [--zipcode <zipcode>]");
                Console.WriteLine("Example: dotnet run --opportunity 12345678-1234-1234-1234-123456789012");
                Console.WriteLine("Example: dotnet run -e UAT -o 12345678-1234-1234-1234-123456789012 --city Springfield --state KS --zipcode 00920");
                return;
            }
            
            // Load configuration for the specified environment
            LoadEnvironmentConfiguration(environment);
            
            Console.WriteLine($"Using environment: {environment}");
            Console.WriteLine($"Target opportunity: {opportunityGuid.Value}");
            Console.WriteLine($"City: {city}");
            Console.WriteLine($"State: {state}");
            Console.WriteLine($"Zip Code: {zipCode}");
            
            // Execute the attach agent automation
            bool success = AttachAgentService.ExecuteAttachAgent(opportunityGuid.Value, city, state, zipCode, BaseUrl, UseMockImplementation);
            
            if (success)
            {
                Console.WriteLine($"Agent attachment completed successfully for opportunity: {opportunityGuid.Value}");
            }
            else
            {
                Console.WriteLine($"Failed to attach agent for opportunity: {opportunityGuid.Value}");
                System.Environment.Exit(1);
            }
        }
        
        /// <summary>
        /// Parses the command line arguments for environment, opportunity GUID, and form parameters
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Tuple containing environment, opportunity GUID, city, state, and zip code</returns>
        private static Tuple<string, Guid?, string, string, string> ParseCommandLineArgs(string[] args)
        {
            string environment = DEFAULT_ENVIRONMENT;
            Guid? opportunityGuid = null;
            string city = "Springfield"; // Default city
            string state = "KS"; // Default state
            string zipCode = "00920"; // Default zip code
            
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                
                // Parse environment parameter
                if (arg == "--env" || arg == "-e")
                {
                    if (i + 1 < args.Length)
                    {
                        string envArg = args[i + 1].ToUpper();
                        if (envArg == "DEV" || envArg == "UAT" || envArg == "PROD")
                        {
                            environment = envArg;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: '{envArg}' is not a valid environment. Using default environment: {DEFAULT_ENVIRONMENT}");
                        }
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse opportunity parameter
                else if (arg == "--opportunity" || arg == "-o")
                {
                    if (i + 1 < args.Length)
                    {
                        string guidArg = args[i + 1];
                        if (Guid.TryParse(guidArg, out Guid parsedGuid))
                        {
                            opportunityGuid = parsedGuid;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: '{guidArg}' is not a valid GUID format.");
                        }
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse city parameter
                else if (arg == "--city")
                {
                    if (i + 1 < args.Length)
                    {
                        city = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse state parameter
                else if (arg == "--state")
                {
                    if (i + 1 < args.Length)
                    {
                        state = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
                // Parse zipcode parameter
                else if (arg == "--zipcode")
                {
                    if (i + 1 < args.Length)
                    {
                        zipCode = args[i + 1];
                        i++; // Skip the next argument as we've already processed it
                    }
                }
            }
            
            return new Tuple<string, Guid?, string, string, string>(environment, opportunityGuid, city, state, zipCode);
        }
        
        /// <summary>
        /// Loads configuration settings for the specified environment
        /// </summary>
        /// <param name="environment">The environment to load (DEV, UAT, or PROD)</param>
        private static void LoadEnvironmentConfiguration(string environment)
        {
            try
            {
                // Load environment-specific settings from centralized config location
                string centralConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "app.env.config", $"App.{environment}.config");
                
                // Normalize the path
                centralConfigPath = Path.GetFullPath(centralConfigPath);
                
                if (File.Exists(centralConfigPath))
                {
                    Console.WriteLine($"Loading environment configuration from: {centralConfigPath}");
                    
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
                                    case "myQueueRssBaseUrl":
                                        BaseUrl = value;
                                        Console.WriteLine($"Updated BaseUrl to: {value}");
                                        break;
                                }
                            }
                        }
                    }
                    
                    Console.WriteLine($"Configuration loaded successfully for {environment} environment.");
                    Console.WriteLine($"Final BaseUrl: {BaseUrl}");
                    Console.WriteLine($"UseMockImplementation: {UseMockImplementation}");
                }
                else
                {
                    Console.WriteLine($"Error: Environment configuration file not found at {centralConfigPath}");
                    throw new FileNotFoundException($"Configuration file not found: {centralConfigPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                throw;
            }
        }
        
    }
}
