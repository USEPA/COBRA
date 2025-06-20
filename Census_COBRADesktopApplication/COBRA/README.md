# EPA - COBRA

## Resources
- [Database Backups (tbd)](TBD) from production
- [Developers Checklist/Resources 9tbd)](tbd)


## Instructions
1. Install Git for Windows: https://git-scm.com/download/win 
2. Install Atlassian SourceTree for GUI interface with Git for Windows or iOS: https://www.sourcetreeapp.com/ (optional, but I highly recommend it)
3. Install Visual Studio Community 2019 with (Individual components tab in Visual Studio Community 2017 installer):
   - **.NET**
     - .NET Framework 4.6 targeting pack
   - **Code tools**
     - NuGet package manager
   - **Development activities**
     - C# and Visual Basic
   - **Others**
     - GitHub extension for Visual Studio
4. Clone this repository from GitHub: `git clone https://savellie@bitbucket.org/abtassociates/cobra.git`
   - Get latest development code: `git checkout master`
5. Install SQLite client to view/edit the database instances `COBRA\data\cobra.db` 
    - see https://www.sqlite.org/quickstart.html
    - (alt options) [DBeaver.io](https://dbeaver.io/)
    - (alt options) [DbVisualizer](https://www.dbvis.com/download/)
6. Setup/Restore Baseline database and Inputs
    - get copy of most recent deliverable and install (e.g., 20220824_cobradotnetv4dot1dot5_debugfiledatetimeindexed.exe when installed in _C:\Program Files\COBRA\_)
      - SQLite DB: _data\cobra.db_
      - CSV Inputs Files (not in repo)
        - Emissions: _input files\emissions_
        - Default Data:
          - Functions: _input files\default data\default_2023_CR_functions.csv_
          - other: _input files\default data\default_2023_*.csv_
        - Scenarios: _input files\emissionscenarios\2023_50PCTReduction_scenario_sample.csv_
    - apply sql statements to modify *TBD* (if any)
7. In order to use the external libraries place two folder aboves
    - Dotspatial.zip
    - Progress.zip for Telerik packages
8. Open the `../COBRA.sln` file in the main folder with Visual Studio.
9. Visual Studio usually checks and automatically restore all NuGet packages on opening or starting the application.
   - if the auto-restore fails then open the NuGet Console Manager to either press the `Restore` button or enter on the command line `PM> Update-Package -reinstall`
10. Start the application in Visual Studio, by selecting "Debug" "x64" "COBRA" then press "Start" button
  
## Notes
- We are not including NuGet packages in version control. Instead, use NuGet Package Restore to restore packages prior to running a build in Visual Studio (should happen automatically on build/start).
- We do not including `.vs\*` settings nor `bin\` or `obj\` build files
