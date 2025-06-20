# COBRA
This repository contains all the codebases relevant to EPA's CO-Benefits Risk Assessment (COBRA) Health Impacts Screening and Mapping Tools

## County-level COBRA (v5)
County-level COBRA is the currently deployed production ready version of COBRA.

- The web tool for county-level cobra can be found at: https://cobra.epa.gov/
- The installer for the desktop tool for county-level cobra can be found at: https://www.epa.gov/cobra/forms/download-cobra-and-sign-updates

County-level COBRA has 3 code bases:
1. The Desktop application (County_COBRADesktopApplication). The data for desktop tool can be found within the installer that is downloaded from https://www.epa.gov/cobra/forms/download-cobra-and-sign-updates.
2. The Angular frontend for COBRA WEB (County_COBRAFrontend)
3. The C# Api for COBRA WEB (County_COBRAApi)
Data for COBRA WEB is accessed by the web application from a cloud hosted s3 bucket, but can be recproduced by grabbing data from the cobra desktop database.

## Census-level COBRA (v6)
Census-level COBRA is a beta version of the COBRA tool that calculates emissions and produces results on the census tract level (84,000+ census tracts) rather than just the county level (~3,000 counties). 
It contains analagous codebases to county-level COBRA, but due to the increased scale, requires much more data, storage, and memory to install and run.

1. Cobra Web's Frontend Angular Application (Census_COBRAFrontend)
2. The Cobra Web API (Census_COBRAApi)
3. The Cobra C# Desktop Application (Census_COBRADesktopApplication)
