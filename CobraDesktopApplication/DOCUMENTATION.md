# Things to install on machine in order to properly run/work with the cobra desktop application:
- Must install the appropriate windows set up from sqlite's website https://system.data.sqlite.org/index.html/doc/trunk/www/downloads-unsup.wiki that targets the .net 4.6 framework in order to take advantage of design time support and properly view the edmx file
in cobra console + click 'run custom tool' on the cobradataset.xsd. Whenever you update the database, if the design time support above is properly installed you should be able to easily update the edmx file and cobra.xsd files with the database updates by running the custom tool so that you can access your columns properly in the code.
- This project uses .NET4.6.2 and Telerik winforms 2025
- relative to this documentation file you must have a "Dotspatial" folder 1 folder above this level (../Dotspatial) with the binaries for the dotspatial library
- relative to this documentation file you must have a ../Progress folder with the binaries for Telerik winforms 2025
- The COBRA desktop application is only for use on windows machines

# Structure
- the COBRA.sln includes both COBRA (the main desktop application and gui) and cobra_console (for executing batch runs through the command line) 


# Data
- COBRA expects a cobra.db in the data folder but this is not tracked by git. This database can be obtained by downloading the official desktop installer for EPA COBRA's website and obtaining the cobra.db contained within the data folder once installed.

# Creating an Installer
1. Delete the bin folders for both COBRA and cobra_console
2. Build the solution in Release mode (make sure top of visual studio project shows Release x64)
    - right click on "Solution 'COBRA' (2 of 2 projects)" and select build
3. Download and install Inno Setup (I used version 6.2.2)
4. Open cobra/installer/COBRA.iss (should open in Inno Setup)
5. Assign version you want the installer to be
6. Set OutputBaseFilename=20230713_cobradotnetv5dot0dot0 (example for installer created on 7/13/2023 with app version 5.0.0)
7. Ensure that all filepaths under [Files] match the corresponding path on your machine
8. In the same open window (COBRA.iss) select Build -> Compile to build the installer, will take some time because COBRA is big
9. Once complete you can find the final installer with proper version in the cobra/installer/cobrainstaller folder





# Adding new files to the solution explorer to be included in the project
- If you add new files through file explorer and don't see them in your Visual Studio Pro solution explorer:
    - right click on the parent folder in the solution explorer -> Add -> Existing Item.. -> Select all files to be added to solution explorer in resulting file explorer window
- Make sure these files get copied to the build-output debug and release folders by editing the .csproj file (I like to open it in VSCode)


# How to Add New Health Effects to COBRA
- New Health Effects will have to be added to the database by updating the SYS_CR_INVENTORY, SYS_Valuation_INVENTORY, SYS_Incidence_inventory, and SYS_Results tables in the cobra.db SQLite database.
- You can then take advantage of design time support in order for the code to recognize the database updates and automatically apply them in the .edmx and .xsd files
(must have  https://system.data.sqlite.org/index.html/doc/trunk/www/downloads-unsup.wiki that targets the .net 4.6 framework in order to take advantage of design time support)


# How to Add New Health Effects to COBRA (manually, not recommended... but it's a tedious work around if you don't have the proper design time components installed)
## tldr version:
Search for occurences of an already existing endpoint like so: "Work Loss Days", "Work_Loss_Days", and "Work_x0020_Loss_x0020_Days" and mimic the structure scene in every occurence with every new healthpoint you want to add.
You need to do this for all files except for cobraDataSet.Designer.css -- ignore the occurences here. The cobraDataSet.Designer.css file will be taken care of once you have finished cobraDataSet.xsd and then you can right click on "Run Custom Tool" to generate the appropriate cobraDataSet.Designer.cs file 
## 1. Add new Health Effect to the Following Tables in the Database:
* SYS_CR
    * add new rows and fill in all columns with appropriate data, except for ID which auto-increments
    * be sure to assign unique functionID to each new health effect
    * ID is automatically assigned value
    * example SQL: for "PM Incidence Hay Fever Rhinitis":

    `INSERT INTO SYS_CR (FunctionID, Endpoint, PoolingWeight, Seasonal_Metric, Study_Author, Study_Year, Start_Age, End_Age, "Function", Beta, Adjusted, Parameter_1_Beta, A, Name_A, B, Name_B, C, Name_C, Cases, IncidenceEndpoint)  
VALUES(33,'Incidence Hay Fever Rhinitis', 1, '
', 'Parker et al.', 2009, 3, 17, '(1-(1/((1-Prevalence)*EXP(Beta*DeltaQ)+Prevalence)))*Prevalence*POP', 0.025464222,'No', 0.00961804,0,0,0,0,0,0,0,'Incidence Hay Fever Rhinitis');`

* SYS_CR_INVENTORY
    * add 2 new rows to the inventory table with same values as SYS_CR except one row has ID=1 and second row has ID=2
    * `INSERT INTO SYS_CR_INVENTORY (ID, FunctionID, Endpoint, PoolingWeight, Seasonal_Metric, Study_Author, Study_Year, Start_Age, End_Age, "Function", Beta, Adjusted, Parameter_1_Beta, A, Name_A, B, Name_B, C, Name_C, Cases, IncidenceEndpoint)
    VALUES(1, 33,'Incidence Hay Fever Rhinitis', 1, 'QuarterlyMean', 'Parker et al.', 2009, 3, 17, '(1-(1/((1-Prevalence)*EXP(Beta*DeltaQ)+Prevalence)))*Prevalence*POP', 0.025464222,'No', 0.00961804,0,0,0,0,0,0,0,'Incidence Hay Fever Rhinitis'),
    VALUES(2, 33,'Incidence Hay Fever Rhinitis', 1, 'QuarterlyMean', 'Parker et al.', 2009, 3, 17, '(1-(1/((1-Prevalence)*EXP(Beta*DeltaQ)+Prevalence)))*Prevalence*POP', 0.025464222,'No', 0.00961804,0,0,0,0,0,0,0,'Incidence Hay Fever Rhinitis');`

* SYS_VALUATION
    * ID is autogenerated, make sure CRFunctionID matches function ID assigned to health effect in SYS_CR
    * `INSERT INTO SYS_VALUATION (CRFunctionID, Endpoint, PoolingWeight, Seasonal_Metric, Study_Author, Study_Year, Start_Age, End_Age, "Function", Beta, Adjusted, Parameter_1_Beta, A, Name_A, B, Name_B, C, Name_C, Cases, HealthEffect, ValuationMethod, Value, ApplyDiscount, IncidenceEndpoint)
VALUES(33,'Incidence Hay Fever Rhinitis', 1, 'QuarterlyMean', 'Parker et al.', 2009, 3, 17, '(1-(1/((1-Prevalence)*EXP(Beta*DeltaQ)+Prevalence)))*Prevalence*POP', 0.025464222,'No', 0.00961804,0,0,0,0,0,0,0,'Incidence Hay Fever Rhinitis', null, 100, 200, 'Incidence Hay Fever Rhinitis');`
    
* SYS_VALUATION_INVENTORY
    * Enter 3 new rows with appropriate data but with ID=1 2 or 3
    * `INSERT INTO SYS_VALUATION_INVENTORY (CRFunctionID, Endpoint, PoolingWeight, Seasonal_Metric, Study_Author, Study_Year, Start_Age, End_Age, "Function", Beta, Adjusted, Parameter_1_Beta, A, Name_A, B, Name_B, C, Name_C, Cases, HealthEffect, ValuationMethod, Value, ApplyDiscount, IncidenceEndpoint)
VALUES(1, 33,'Incidence Hay Fever Rhinitis', 1, 'QuarterlyMean', 'Parker et al.', 2009, 3, 17, '(1-(1/((1-Prevalence)*EXP(Beta*DeltaQ)+Prevalence)))*Prevalence*POP', 0.025464222,'No', 0.00961804,0,0,0,0,0,0,0,'Incidence Hay Fever Rhinitis', null, 100, 200, 'Incidence Hay Fever Rhinitis'),
VALUES(2, 33,'Incidence Hay Fever Rhinitis', 1, 'QuarterlyMean', 'Parker et al.', 2009, 3, 17, '(1-(1/((1-Prevalence)*EXP(Beta*DeltaQ)+Prevalence)))*Prevalence*POP', 0.025464222,'No', 0.00961804,0,0,0,0,0,0,0,'Incidence Hay Fever Rhinitis', null, 100, 200, 'Incidence Hay Fever Rhinitis'),
VALUES(3, 33,'Incidence Hay Fever Rhinitis', 1, 'QuarterlyMean', 'Parker et al.', 2009, 3, 17, '(1-(1/((1-Prevalence)*EXP(Beta*DeltaQ)+Prevalence)))*Prevalence*POP', 0.025464222,'No', 0.00961804,0,0,0,0,0,0,0,'Incidence Hay Fever Rhinitis', null, 100, 200, 'Incidence Hay Fever Rhinitis');`

* SYS_INCIDENCE
    * ID is auto generated
    * add ~3000+ rows of incidence data for each county/Destination ID

* SYS_Incidence_INVENTORY
    * ID(1,2,3) corresponds to year selected (2016, 2023, or 2028)
    * ads ~6000+ rows of incidence data for each year index
    * example sql statement using mortality incidence for the new health effect, but should be changed to use the real incidence for the new health effect once that data is received:
        * `INSERT INTO SYS_Incidence_Inventory (ID,"Year",DestinationID,FIPS,Endpoint,Age0,Age1,Age2,Age3,Age4,Age5,Age6,Age7,Age8,Age9,Age10,Age11,Age12,Age13,Age14,Age15,Age16,Age17,Age18,Age19,Age20,Age21,Age22,Age23,Age24,Age25,Age26,Age27,Age28,Age29,Age30,Age31,Age32,Age33,Age34,Age35,Age36,Age37,Age38,Age39,Age40,Age41,Age42,Age43,Age44,Age45,Age46,Age47,Age48,Age49,Age50,Age51,Age52,Age53,Age54,Age55,Age56,Age57,Age58,Age59,Age60,Age61,Age62,Age63,Age64,Age65,Age66,Age67,Age68,Age69,Age70,Age71,Age72,Age73,Age74,Age75,Age76,Age77,Age78,Age79,Age80,Age81,Age82,Age83,Age84,Age85,Age86,Age87,Age88,Age89,Age90,Age91,Age92,Age93,Age94,Age95,Age96,Age97,Age98,Age99)
    SELECT ID,"Year",DestinationID,FIPS,'Incidence Hay Fever Rhinitis',Age0,Age1,Age2,Age3,Age4,Age5,Age6,Age7,Age8,Age9,Age10,Age11,Age12,Age13,Age14,Age15,Age16,Age17,Age18,Age19,Age20,Age21,Age22,Age23,Age24,Age25,Age26,Age27,Age28,Age29,Age30,Age31,Age32,Age33,Age34,Age35,Age36,Age37,Age38,Age39,Age40,Age41,Age42,Age43,Age44,Age45,Age46,Age47,Age48,Age49,Age50,Age51,Age52,Age53,Age54,Age55,Age56,Age57,Age58,Age59,Age60,Age61,Age62,Age63,Age64,Age65,Age66,Age67,Age68,Age69,Age70,Age71,Age72,Age73,Age74,Age75,Age76,Age77,Age78,Age79,Age80,Age81,Age82,Age83,Age84,Age85,Age86,Age87,Age88,Age89,Age90,Age91,Age92,Age93,Age94,Age95,Age96,Age97,Age98,Age99
    FROM SYS_Incidence_Inventory sii 
    WHERE sii.Endpoint = 'PM Mortality, All Cause'`


* SYS_RESULTS
    * Add 2 new columns for the new heath effect and valuation of that health effect


### 2. Add Health Effect to cobraDataSet.xsd file
- right click on xsd file and open in xml text editor
- add new health effect valuation as [$ health effect] to both totalHELOW and totalHEHIGH expressions
- also find all occurences of an already existing health effect to use as an example (i.e., PM Work Loss days and it's coresponding variable PM_Work_Loss_days) and create new variable for the health effect and add new health effect as appropriate to all sql commands and functions
- right click on xsd file and "run custom tool" to generate the appropriate cobraDataSet.Designer.cs file based of xsd file

## 3. Add Health Effect Column to final Results table in GUI
- once the datasetdesigner is updated update MainGUI.cs with approriate new rows for new health effect and it's valuation
- lung cancer example:

        `worksheet.Cell(rowcount, 45).Value = result.PM_Incidence_Lung_Cancer; //"PM Incidence Lung Cancer";
						worksheet.Cell(rowcount, 46).Value = result.___PM_Incidence_Lung_Cancer; // "$ PM Incidence Lung Cancer"`
- also updated MainGUI.designer.cs with new columns:
    - ` Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn129 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn130 = new Telerik.WinControls.UI.GridViewDecimalColumn();`
    -             gridViewDecimalColumn129.DataType = typeof(double);
            gridViewDecimalColumn129.EnableExpressionEditor = false;
            gridViewDecimalColumn129.FieldName = "PM Incidence Lung Cancer";
            gridViewDecimalColumn129.FormatString = "{0:0.####}";
            gridViewDecimalColumn129.HeaderText = "PM Incidence Lung Cancer";
            gridViewDecimalColumn129.IsAutoGenerated = true;
            gridViewDecimalColumn129.Name = "PM Incidence Lung Cancer";
            gridViewDecimalColumn129.Width = 130;
            gridViewDecimalColumn130.DataType = typeof(double);
            gridViewDecimalColumn130.EnableExpressionEditor = false;
            gridViewDecimalColumn130.FieldName = "$ PM Incidence Lung Cancer";
            gridViewDecimalColumn130.FormatString = "{0:###,###,##0.##}";
            gridViewDecimalColumn130.HeaderText = "$ PM Incidence Lung Cancer";
            gridViewDecimalColumn130.IsAutoGenerated = true;
            gridViewDecimalColumn130.Name = "$ PM Incidence Lung Cancer";
            gridViewDecimalColumn130.ThousandsSeparator = true;
            gridViewDecimalColumn130.Width = 140;
