Run the powershell script in the setup folder (setup.ps1).
Setup is complete when you have all successes and no failures. (This should work fine without any tinkering. Contact Ed Czebrinski if it does not.)
Run any powershell scripts in the tasks folder. Tasks default to the UAT environment. To run in another environment, use DEV or PROD. (Example: ".\CreateRealtyOpp.ps1 -Environment PROD")
If you run a test powershell script, it will generate output in the Icharus/Output directory.