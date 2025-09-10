using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace myQueueRssAttachAgent
{
    public static class AttachAgentService
    {
        private static string? _logFilePath;
        
        /// <summary>
        /// Initializes the log file for the current session
        /// </summary>
        private static void InitializeLogging()
        {
            try
            {
                // Get the output directory path (same logic as TakeScreenshot)
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string icharusRoot = exeDirectory;
                for (int i = 0; i < 7; i++)
                {
                    icharusRoot = Directory.GetParent(icharusRoot)?.FullName;
                    if (icharusRoot == null) break;
                }
                
                string outputDirectory;
                if (icharusRoot != null)
                {
                    outputDirectory = Path.Combine(icharusRoot, "output", "logs");
                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }
                }
                else
                {
                    outputDirectory = Directory.GetCurrentDirectory();
                }
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                _logFilePath = Path.Combine(outputDirectory, $"{timestamp}_attach_agent.log");
                
                // Write initial log entry
                LogMessage($"=== Attach Agent Automation Log Started ===");
                LogMessage($"Session started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                LogMessage($"Log file: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to initialize logging: {ex.Message}");
                _logFilePath = null;
            }
        }
        
        /// <summary>
        /// Logs a message to both console and file
        /// </summary>
        private static void LogMessage(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            
            // Always write to console
            Console.WriteLine(message);
            
            // Write to file if logging is initialized
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    File.AppendAllText(_logFilePath, timestampedMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to write to log file: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// Executes the attach agent automation for the specified opportunity
        /// </summary>
        /// <param name="opportunityId">The GUID of the opportunity to attach agent to</param>
        /// <param name="city">City to populate in the form</param>
        /// <param name="state">State to populate in the form</param>
        /// <param name="zipCode">Zip code to populate in the form</param>
        /// <param name="baseUrl">Base URL for the application</param>
        /// <param name="useMockImplementation">Whether to use mock implementation</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool ExecuteAttachAgent(Guid opportunityId, string city, string state, string zipCode, string baseUrl, bool useMockImplementation)
        {
            // Initialize logging for this session
            InitializeLogging();
            
            if (useMockImplementation)
            {
                return ExecuteMockAttachAgent(opportunityId, city, state, zipCode);
            }
            
            IWebDriver driver = null;
            
            try
            {
                LogMessage("Starting Attach Agent automation...");
                LogMessage($"Parameters: OpportunityId={opportunityId}, City={city}, State={state}, ZipCode={zipCode}");
                
                // Setup Chrome WebDriver
                driver = SetupWebDriver();
                
                // Navigate to opportunity page
                string opportunityUrl = $"{baseUrl.TrimEnd('/')}/opportunity/{opportunityId}";
                LogMessage($"Navigating to: {opportunityUrl}");
                driver.Navigate().GoToUrl(opportunityUrl);
                
                // Wait for page to load and handle OKTA redirects
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                wait.Until(d => d.Url.Contains(opportunityId.ToString()));
                LogMessage("Page loaded successfully");
                
                // Click Attach Agent tab
                LogMessage("Looking for Attach Agent tab...");
                var attachAgentTab = wait.Until(d => 
                {
                    var elements = d.FindElements(By.XPath("//*[contains(text(), 'Attach Agent')]"));
                    return elements.FirstOrDefault(e => e.Displayed && e.Enabled);
                });
                
                if (attachAgentTab == null)
                {
                    LogMessage("ERROR: Attach Agent tab not found");
                    return false;
                }
                
                LogMessage("Clicking Attach Agent tab...");
                attachAgentTab.Click();
                Thread.Sleep(3000); // Wait for form to load
                
                // Take screenshot of form
                TakeScreenshot(driver, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_attach_agent_form");
                
                // Populate form fields
                bool formPopulated = PopulateForm(driver, city, state, zipCode);
                if (!formPopulated)
                {
                    LogMessage("ERROR: Failed to populate form");
                    return false;
                }
                
                // Submit form and wait for results
                bool formSubmitted = SubmitFormAndWaitForResults(driver);
                if (!formSubmitted)
                {
                    LogMessage("ERROR: Failed to submit form or get results");
                    return false;
                }
                
                // Attach the agent
                bool agentAttached = AttachAgentFromResults(driver);
                if (!agentAttached)
                {
                    LogMessage("ERROR: Failed to attach agent");
                    return false;
                }
                
                LogMessage("Agent attachment completed successfully");
                LogMessage("=== Attach Agent Automation Completed Successfully ===");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: Exception during attach agent automation: {ex.Message}");
                LogMessage($"Stack trace: {ex.StackTrace}");
                
                // Take failure screenshot
                if (driver != null)
                {
                    TakeScreenshot(driver, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_attach_agent_failure");
                }
                
                return false;
            }
            finally
            {
                // Clean up WebDriver
                try
                {
                    driver?.Quit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing WebDriver: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Mock implementation for testing without actual browser automation
        /// </summary>
        private static bool ExecuteMockAttachAgent(Guid opportunityId, string city, string state, string zipCode)
        {
            Console.WriteLine("=== MOCK ATTACH AGENT EXECUTION ===");
            Console.WriteLine($"Opportunity ID: {opportunityId}");
            Console.WriteLine($"City: {city}");
            Console.WriteLine($"State: {state}");
            Console.WriteLine($"Zip Code: {zipCode}");
            Console.WriteLine("Mock: Navigating to opportunity page...");
            Console.WriteLine("Mock: Clicking Attach Agent tab...");
            Console.WriteLine("Mock: Populating form fields...");
            Console.WriteLine("Mock: Submitting form...");
            Console.WriteLine("Mock: Waiting for agent results...");
            Console.WriteLine("Mock: Attaching agent...");
            Console.WriteLine("Mock: Agent attachment completed successfully");
            return true;
        }
        
        /// <summary>
        /// Sets up Chrome WebDriver with appropriate options
        /// </summary>
        private static IWebDriver SetupWebDriver()
        {
            Console.WriteLine("Setting up Chrome WebDriver...");
            
            // Setup ChromeDriver using WebDriverManager with auto-detection
            new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
            
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("--no-sandbox");
            chromeOptions.AddArguments("--disable-dev-shm-usage");
            chromeOptions.AddArguments("--window-size=1920,1080");
            chromeOptions.AddArguments("--disable-application-cache");
            chromeOptions.AddArguments("--disable-extensions");
            chromeOptions.AddArguments("--disable-popup-blocking");
            chromeOptions.AddArguments("--remote-debugging-port=9222");
            
            // Set detach option to prevent immediate closing
            chromeOptions.AddUserProfilePreference("detach", true);
            
            var driver = new ChromeDriver(chromeOptions);
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            
            Console.WriteLine("Chrome WebDriver setup completed");
            return driver;
        }
        
        /// <summary>
        /// Populates the attach agent form with the specified values
        /// </summary>
        private static bool PopulateForm(IWebDriver driver, string city, string state, string zipCode)
        {
            try
            {
                Console.WriteLine("Populating form fields...");
                
                // Update City field
                Console.WriteLine($"Setting city to: {city}");
                var cityInput = driver.FindElement(By.XPath("//input[@type='text']"));
                if (cityInput.Displayed && cityInput.Enabled)
                {
                    cityInput.Clear();
                    cityInput.SendKeys(city);
                    Console.WriteLine($"✓ City updated to {city}");
                }
                
                // Update State dropdown
                Console.WriteLine($"Setting state to: {state}");
                var stateDropdown = driver.FindElement(By.TagName("select"));
                if (stateDropdown.Displayed && stateDropdown.Enabled)
                {
                    var selectElement = new SelectElement(stateDropdown);
                    Console.WriteLine($"Current state: {selectElement.SelectedOption.Text}");
                    selectElement.SelectByValue(state);
                    Console.WriteLine($"✓ State changed to {state}");
                }
                
                // Update Zip Code field
                Console.WriteLine($"Setting zip code to: {zipCode}");
                var zipInputs = driver.FindElements(By.XPath("//input[@type='text']"));
                var zipInput = zipInputs.LastOrDefault(); // Assuming zip is the last text input
                
                if (zipInput != null && zipInput.Displayed)
                {
                    var currentZip = zipInput.GetAttribute("value");
                    Console.WriteLine($"Current zip code: {currentZip}");
                    
                    if (currentZip != zipCode)
                    {
                        zipInput.Clear();
                        zipInput.SendKeys(zipCode);
                        Console.WriteLine($"✓ Zip code updated to {zipCode}");
                    }
                    else
                    {
                        Console.WriteLine($"✓ Zip code already correct ({zipCode})");
                    }
                }
                
                // Take screenshot after form population
                TakeScreenshot(driver, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_form_populated");
                
                Console.WriteLine("Form population completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error populating form: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Submits the form and waits for agent results to load
        /// </summary>
        private static bool SubmitFormAndWaitForResults(IWebDriver driver)
        {
            try
            {
                Console.WriteLine("Looking for form submission button...");
                
                // Look for the "Request Agents" button
                var submitButton = driver.FindElement(By.XPath("//button[@data-vu-button='Request Agents']"));
                if (submitButton.Displayed && submitButton.Enabled)
                {
                    Console.WriteLine($"Found submission button: '{submitButton.Text}'");
                    Console.WriteLine("Clicking submission button...");
                    submitButton.Click();
                    Console.WriteLine("✓ Submission button clicked");
                    
                    // Wait for agent results to load
                    Console.WriteLine("Waiting for agent results to load...");
                    var resultsWait = new WebDriverWait(driver, TimeSpan.FromSeconds(300));
                    
                    resultsWait.Until(d => 
                    {
                        try
                        {
                            var agentResults = d.FindElements(By.XPath("//table//tr[contains(., 'Agent')] | //div[contains(@class, 'agent') or contains(text(), 'Agent')]"));
                            return agentResults.Any(r => r.Displayed);
                        }
                        catch
                        {
                            return false;
                        }
                    });
                    
                    Console.WriteLine("✓ Agent results loaded successfully");
                    
                    // Take screenshot of results
                    TakeScreenshot(driver, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_agent_results");
                    
                    return true;
                }
                else
                {
                    Console.WriteLine("Submission button found but not clickable");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error submitting form: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Attaches an agent from the search results
        /// </summary>
        private static bool AttachAgentFromResults(IWebDriver driver)
        {
            try
            {
                Console.WriteLine("Looking for 'Attach Agent' button in results...");
                
                // Add small delay for UI stability
                Thread.Sleep(2000);
                
                var attachAgentButton = driver.FindElement(By.XPath("//button[@data-vu-button='Attach Agent']"));
                if (attachAgentButton.Displayed && attachAgentButton.Enabled)
                {
                    Console.WriteLine($"Found 'Attach Agent' button: '{attachAgentButton.Text}'");
                    Console.WriteLine("Clicking 'Attach Agent' button...");
                    attachAgentButton.Click();
                    Console.WriteLine("✓ Agent attachment button clicked");
                    
                    // Wait for attachment to process
                    Thread.Sleep(3000);
                    
                    // Wait for success message
                    Console.WriteLine("Waiting for success message...");
                    var successWait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                    
                    var successMessage = successWait.Until(d => 
                    {
                        try
                        {
                            var attachedContainer = d.FindElement(By.XPath("//div[contains(@class, 'attached-agent-container')]//h3[contains(@class, 'attached-title') and text()='Attached']"));
                            return attachedContainer.Displayed ? attachedContainer : null;
                        }
                        catch
                        {
                            return null;
                        }
                    });
                    
                    if (successMessage != null)
                    {
                        Console.WriteLine("✓ Success message 'Attached' found!");
                        
                        // Try to get agent name
                        try
                        {
                            var agentName = driver.FindElement(By.XPath("//span[contains(@class, 'agent-name')]"));
                            Console.WriteLine($"✓ Agent attached: {agentName.Text}");
                        }
                        catch
                        {
                            Console.WriteLine("✓ Agent attached (name not found in DOM)");
                        }
                        
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Success message not found within timeout");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("'Attach Agent' button found but not clickable");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error attaching agent: {ex.Message}");
                return false;
            }
            finally
            {
                // Always take a screenshot of the final state, regardless of success or failure
                TakeScreenshot(driver, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_attach_agent_final_state");
            }
        }
        
        /// <summary>
        /// Takes a screenshot and saves it with the specified filename
        /// </summary>
        private static void TakeScreenshot(IWebDriver driver, string fileName)
        {
            try
            {
                // Get the output directory path (7 levels up from executable to Icharus root, then to output folder)
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string icharusRoot = exeDirectory;
                for (int i = 0; i < 7; i++)
                {
                    icharusRoot = Directory.GetParent(icharusRoot)?.FullName;
                    if (icharusRoot == null)
                    {
                        Console.WriteLine("Warning: Could not locate Icharus root directory, saving screenshot to current directory");
                        break;
                    }
                }
                
                string outputDirectory;
                if (icharusRoot != null)
                {
                    outputDirectory = Path.Combine(icharusRoot, "output", "logs");
                    
                    // Create output directory if it doesn't exist
                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                        Console.WriteLine($"Created output directory: {outputDirectory}");
                    }
                }
                else
                {
                    outputDirectory = Directory.GetCurrentDirectory();
                }
                
                var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                var fullFileName = Path.Combine(outputDirectory, $"{fileName}.png");
                screenshot.SaveAsFile(fullFileName);
                Console.WriteLine($"Screenshot saved: {fullFileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to take screenshot: {ex.Message}");
            }
        }
    }
}
