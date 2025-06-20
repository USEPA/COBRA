using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CobraCompute
{
    public class RedisConfig
    {
        public string URI { get; set; }
    }

    public class S3Config
    {
        public string endpoint { get; set; }
        public string bucket { get; set; }
        public string region { get; set; }
        public string accessKey { get; set; }
        public string secretKey { get; set; }
        public bool ssl { get; set; }
    }

    public class ModelConfig
    {
        public int emissionsdatayear { get; set; }
        public int populationdatayear { get; set; }
        public int srdatayear { get; set; }
        public int incidencedatayear { get; set; }
        public int valuationdatayear { get; set; }
    }


    public class EmissionsRecord
    {
        public int? ID { get; set; } //this may be repurposed
        public int? typeindx { get; set; }
        public int? sourceindx { get; set; }
        public int? stid { get; set; }
        public int? cyid { get; set; }
        public int? TIER1 { get; set; }
        public int? TIER2 { get; set; }
        public int? TIER3 { get; set; }
        public double? NOx { get; set; }
        public double? SO2 { get; set; }
        public double? NH3 { get; set; }
        public double SOA { get; set; }
        public double? PM25 { get; set; }
        public double? VOC { get; set; }
    }

    public class EmissionsRecord_Serializable
    {
        public int ID { get; set; } //this may be repurposed
        public int typeindx { get; set; }
        public int sourceindx { get; set; }
        public int stid { get; set; }
        public int cyid { get; set; }
        public int TIER1 { get; set; }
        public int TIER2 { get; set; }
        public int TIER3 { get; set; }
        public double NOx { get; set; }
        public double SO2 { get; set; }
        public double NH3 { get; set; }
        public double SOA { get; set; }
        public double PM25 { get; set; }
        public double VOC { get; set; }
    }

    public class srrecord
    {
        public int ID { get; set; }
        public int typeindx { get; set; }
        public int sourceindx { get; set; }
        public int destindx { get; set; }
        public double? c_PM25 { get; set; }
        public double? c_NO3 { get; set; }
        public double? c_SO4 { get; set; }
        //public double? tx_nh3 { get; set; }

        public double? c_O3V { get; set; }
        public double? c_O3N { get; set; }

    }

    public class srCensusrecord
    {
        public int ID { get; set; }
        public int sourceindx { get; set; }
        public int typeindx { get; set; }
        public int census_index { get; set; }
        public double? c_PM25 { get; set; }

    }

    public class CensusInfo
    {
        public string dest_tract { get; set; }
        public string FIPS { get; set; }
        public int? destindx { get; set; }
        public int IRA { get; set; }
        public int CJEST { get; set; }
        public double IRA_fraction { get; set; } = 0;
    }

    public class TribalInfo
    {
        public string name { get; set; }

        public double tribe_fraction { get; set; } = 0;
        public int population { get; set; } = 0;
    }


    public class CensusRecord
    {
        public int census_index { get; set; }
        public string dest_tract { get; set; }
        public string FIPS { get; set; }
        public int? destindx { get; set; }
        public int IRA { get; set; }
        public int CJEST { get; set; }
        public double IRA_fraction { get; set; } = 0;
    }

    public class TribalRecord
    {
        public string tract_id { get; set; }
        public string tribe_name { get; set; }
        public double tribe_fraction { get; set; }
        public int tribe_population { get; set; }
    }


    public class UserScenarioCore
    {
        public DateTime createdOn { get; set; }
        public bool isDirty { get; set; }
        public bool isEmissionsDataDirty { get; set; }
        public int Year { get; set; }
        public Guid Id { get; set; }
        public queueSubmission queueSubmission { get; set; }
    }


    public class UserScenario : UserScenarioCore
    {
        public DataTable EmissionsData;
        public List<Cobra_ResultDetail> Impacts;

    }

    class Result
    {
        public string TractID { get; set; }
        public long DestinationIndex { get; set; }
        public string Endpoint { get; set; }
        public double Value { get; set; }
    }
    public partial class Cobra_Destination
    {
        public long ID { get; set; }
        public Nullable<long> destindx { get; set; }
        public string tract_id { get; set; }
        public Nullable<double> BASE_NOx { get; set; }
        public Nullable<double> BASE_SO2 { get; set; }
        public Nullable<double> BASE_NH3 { get; set; }
        public Nullable<double> BASE_SOA { get; set; }
        public Nullable<double> BASE_PM25 { get; set; }
        public Nullable<double> BASE_VOC { get; set; }
        public Nullable<double> CTRL_NOx { get; set; }
        public Nullable<double> CTRL_SO2 { get; set; }
        public Nullable<double> CTRL_NH3 { get; set; }
        public Nullable<double> CTRL_SOA { get; set; }
        public Nullable<double> CTRL_PM25 { get; set; }
        public Nullable<double> CTRL_VOC { get; set; }
        public Nullable<double> F { get; set; }
        public Nullable<double> BASE_FINAL_PM { get; set; }
        public Nullable<double> CTRL_FINAL_PM { get; set; }
        public Nullable<double> DELTA_FINAL_PM { get; set; }
        public Nullable<double> BASE_FINAL_O3 { get; set; }
        public Nullable<double> CTRL_FINAL_O3 { get; set; }
        public Nullable<double> DELTA_FINAL_O3 { get; set; }

        public Nullable<double> IRA_fraction { get; set; }
        public Nullable<int> CJEST { get; set; }
        //dictionary with keys = tribe name / values = tribe population
        public Dictionary<string, double> Tribes { get; set; } = new Dictionary<string, double>();
    }
    public partial class Cobra_Valuation
    {
        public long ID { get; set; }
        public Nullable<long> CRFunctionID { get; set; }
        public string Endpoint { get; set; }
        public Nullable<double> PoolingWeight { get; set; }
        public string Seasonal_Metric { get; set; }
        public string Study_Author { get; set; }
        public Nullable<long> Study_Year { get; set; }
        public Nullable<long> Start_Age { get; set; }
        public Nullable<long> End_Age { get; set; }
        public string Function { get; set; }
        public Nullable<double> Beta { get; set; }
        public string Adjusted { get; set; }
        public Nullable<double> Parameter_1_Beta { get; set; }
        public Nullable<double> A { get; set; }
        public string Name_A { get; set; }
        public Nullable<double> B { get; set; }
        public string Name_B { get; set; }
        public Nullable<double> C { get; set; }
        public string Name_C { get; set; }
        public Nullable<long> Cases { get; set; }
        public string HealthEffect { get; set; }
        public string ValuationMethod { get; set; }
        public Nullable<double> Value { get; set; }
        public string ApplyDiscount { get; set; }
        public string IncidenceEndpoint { get; set; }
    }
    public partial class Cobra_POP
    {
        public long ID { get; set; }
        public Nullable<long> Year { get; set; }

        public string dest_tract { get; set; }
        public Nullable<long> DestinationID { get; set; }
        public Nullable<long> FIPS { get; set; }
        public Nullable<double> Age0 { get; set; }
        public Nullable<double> Age1 { get; set; }
        public Nullable<double> Age2 { get; set; }
        public Nullable<double> Age3 { get; set; }
        public Nullable<double> Age4 { get; set; }
        public Nullable<double> Age5 { get; set; }
        public Nullable<double> Age6 { get; set; }
        public Nullable<double> Age7 { get; set; }
        public Nullable<double> Age8 { get; set; }
        public Nullable<double> Age9 { get; set; }
        public Nullable<double> Age10 { get; set; }
        public Nullable<double> Age11 { get; set; }
        public Nullable<double> Age12 { get; set; }
        public Nullable<double> Age13 { get; set; }
        public Nullable<double> Age14 { get; set; }
        public Nullable<double> Age15 { get; set; }
        public Nullable<double> Age16 { get; set; }
        public Nullable<double> Age17 { get; set; }
        public Nullable<double> Age18 { get; set; }
        public Nullable<double> Age19 { get; set; }
        public Nullable<double> Age20 { get; set; }
        public Nullable<double> Age21 { get; set; }
        public Nullable<double> Age22 { get; set; }
        public Nullable<double> Age23 { get; set; }
        public Nullable<double> Age24 { get; set; }
        public Nullable<double> Age25 { get; set; }
        public Nullable<double> Age26 { get; set; }
        public Nullable<double> Age27 { get; set; }
        public Nullable<double> Age28 { get; set; }
        public Nullable<double> Age29 { get; set; }
        public Nullable<double> Age30 { get; set; }
        public Nullable<double> Age31 { get; set; }
        public Nullable<double> Age32 { get; set; }
        public Nullable<double> Age33 { get; set; }
        public Nullable<double> Age34 { get; set; }
        public Nullable<double> Age35 { get; set; }
        public Nullable<double> Age36 { get; set; }
        public Nullable<double> Age37 { get; set; }
        public Nullable<double> Age38 { get; set; }
        public Nullable<double> Age39 { get; set; }
        public Nullable<double> Age40 { get; set; }
        public Nullable<double> Age41 { get; set; }
        public Nullable<double> Age42 { get; set; }
        public Nullable<double> Age43 { get; set; }
        public Nullable<double> Age44 { get; set; }
        public Nullable<double> Age45 { get; set; }
        public Nullable<double> Age46 { get; set; }
        public Nullable<double> Age47 { get; set; }
        public Nullable<double> Age48 { get; set; }
        public Nullable<double> Age49 { get; set; }
        public Nullable<double> Age50 { get; set; }
        public Nullable<double> Age51 { get; set; }
        public Nullable<double> Age52 { get; set; }
        public Nullable<double> Age53 { get; set; }
        public Nullable<double> Age54 { get; set; }
        public Nullable<double> Age55 { get; set; }
        public Nullable<double> Age56 { get; set; }
        public Nullable<double> Age57 { get; set; }
        public Nullable<double> Age58 { get; set; }
        public Nullable<double> Age59 { get; set; }
        public Nullable<double> Age60 { get; set; }
        public Nullable<double> Age61 { get; set; }
        public Nullable<double> Age62 { get; set; }
        public Nullable<double> Age63 { get; set; }
        public Nullable<double> Age64 { get; set; }
        public Nullable<double> Age65 { get; set; }
        public Nullable<double> Age66 { get; set; }
        public Nullable<double> Age67 { get; set; }
        public Nullable<double> Age68 { get; set; }
        public Nullable<double> Age69 { get; set; }
        public Nullable<double> Age70 { get; set; }
        public Nullable<double> Age71 { get; set; }
        public Nullable<double> Age72 { get; set; }
        public Nullable<double> Age73 { get; set; }
        public Nullable<double> Age74 { get; set; }
        public Nullable<double> Age75 { get; set; }
        public Nullable<double> Age76 { get; set; }
        public Nullable<double> Age77 { get; set; }
        public Nullable<double> Age78 { get; set; }
        public Nullable<double> Age79 { get; set; }
        public Nullable<double> Age80 { get; set; }
        public Nullable<double> Age81 { get; set; }
        public Nullable<double> Age82 { get; set; }
        public Nullable<double> Age83 { get; set; }
        public Nullable<double> Age84 { get; set; }
        public Nullable<double> Age85 { get; set; }
        public Nullable<double> Age86 { get; set; }
        public Nullable<double> Age87 { get; set; }
        public Nullable<double> Age88 { get; set; }
        public Nullable<double> Age89 { get; set; }
        public Nullable<double> Age90 { get; set; }
        public Nullable<double> Age91 { get; set; }
        public Nullable<double> Age92 { get; set; }
        public Nullable<double> Age93 { get; set; }
        public Nullable<double> Age94 { get; set; }
        public Nullable<double> Age95 { get; set; }
        public Nullable<double> Age96 { get; set; }
        public Nullable<double> Age97 { get; set; }
        public Nullable<double> Age98 { get; set; }
        public Nullable<double> Age99 { get; set; }

        public double totalPop()
        {
            //sum all ages
            return (Age0 ?? 0)
                 + (Age1 ?? 0)
                 + (Age2 ?? 0)
                 + (Age3 ?? 0)
                 + (Age4 ?? 0)
                 + (Age5 ?? 0)
                 + (Age6 ?? 0)
                 + (Age7 ?? 0)
                 + (Age8 ?? 0)
                 + (Age9 ?? 0)
                 + (Age10 ?? 0)
                 + (Age11 ?? 0)
                 + (Age12 ?? 0)
                 + (Age13 ?? 0)
                 + (Age14 ?? 0)
                 + (Age15 ?? 0)
                 + (Age16 ?? 0)
                 + (Age17 ?? 0)
                 + (Age18 ?? 0)
                 + (Age19 ?? 0)
                 + (Age20 ?? 0)
                 + (Age21 ?? 0)
                 + (Age22 ?? 0)
                 + (Age23 ?? 0)
                 + (Age24 ?? 0)
                 + (Age25 ?? 0)
                 + (Age26 ?? 0)
                 + (Age27 ?? 0)
                 + (Age28 ?? 0)
                 + (Age29 ?? 0)
                 + (Age30 ?? 0)
                 + (Age31 ?? 0)
                 + (Age32 ?? 0)
                 + (Age33 ?? 0)
                 + (Age34 ?? 0)
                 + (Age35 ?? 0)
                 + (Age36 ?? 0)
                 + (Age37 ?? 0)
                 + (Age38 ?? 0)
                 + (Age39 ?? 0)
                 + (Age40 ?? 0)
                 + (Age41 ?? 0)
                 + (Age42 ?? 0)
                 + (Age43 ?? 0)
                 + (Age44 ?? 0)
                 + (Age45 ?? 0)
                 + (Age46 ?? 0)
                 + (Age47 ?? 0)
                 + (Age48 ?? 0)
                 + (Age49 ?? 0)
                 + (Age50 ?? 0)
                 + (Age51 ?? 0)
                 + (Age52 ?? 0)
                 + (Age53 ?? 0)
                 + (Age54 ?? 0)
                 + (Age55 ?? 0)
                 + (Age56 ?? 0)
                 + (Age57 ?? 0)
                 + (Age58 ?? 0)
                 + (Age59 ?? 0)
                 + (Age60 ?? 0)
                 + (Age61 ?? 0)
                 + (Age62 ?? 0)
                 + (Age63 ?? 0)
                 + (Age64 ?? 0)
                 + (Age65 ?? 0)
                 + (Age66 ?? 0)
                 + (Age67 ?? 0)
                 + (Age68 ?? 0)
                 + (Age69 ?? 0)
                 + (Age70 ?? 0)
                 + (Age71 ?? 0)
                 + (Age72 ?? 0)
                 + (Age73 ?? 0)
                 + (Age74 ?? 0)
                 + (Age75 ?? 0)
                 + (Age76 ?? 0)
                 + (Age77 ?? 0)
                 + (Age78 ?? 0)
                 + (Age79 ?? 0)
                 + (Age80 ?? 0)
                 + (Age81 ?? 0)
                 + (Age82 ?? 0)
                 + (Age83 ?? 0)
                 + (Age84 ?? 0)
                 + (Age85 ?? 0)
                 + (Age86 ?? 0)
                 + (Age87 ?? 0)
                 + (Age88 ?? 0)
                 + (Age89 ?? 0)
                 + (Age90 ?? 0)
                 + (Age91 ?? 0)
                 + (Age92 ?? 0)
                 + (Age93 ?? 0)
                 + (Age94 ?? 0)
                 + (Age95 ?? 0)
                 + (Age96 ?? 0)
                 + (Age97 ?? 0)
                 + (Age98 ?? 0)
                 + (Age99 ?? 0);
        }

        public double popat(long age)
        {
            double? result = 0d;
            switch (age)
            {
                case 0: result = this.Age0; break;
                case 1: result = this.Age1; break;
                case 2: result = this.Age2; break;
                case 3: result = this.Age3; break;
                case 4: result = this.Age4; break;
                case 5: result = this.Age5; break;
                case 6: result = this.Age6; break;
                case 7: result = this.Age7; break;
                case 8: result = this.Age8; break;
                case 9: result = this.Age9; break;
                case 10: result = this.Age10; break;
                case 11: result = this.Age11; break;
                case 12: result = this.Age12; break;
                case 13: result = this.Age13; break;
                case 14: result = this.Age14; break;
                case 15: result = this.Age15; break;
                case 16: result = this.Age16; break;
                case 17: result = this.Age17; break;
                case 18: result = this.Age18; break;
                case 19: result = this.Age19; break;
                case 20: result = this.Age20; break;
                case 21: result = this.Age21; break;
                case 22: result = this.Age22; break;
                case 23: result = this.Age23; break;
                case 24: result = this.Age24; break;
                case 25: result = this.Age25; break;
                case 26: result = this.Age26; break;
                case 27: result = this.Age27; break;
                case 28: result = this.Age28; break;
                case 29: result = this.Age29; break;
                case 30: result = this.Age30; break;
                case 31: result = this.Age31; break;
                case 32: result = this.Age32; break;
                case 33: result = this.Age33; break;
                case 34: result = this.Age34; break;
                case 35: result = this.Age35; break;
                case 36: result = this.Age36; break;
                case 37: result = this.Age37; break;
                case 38: result = this.Age38; break;
                case 39: result = this.Age39; break;
                case 40: result = this.Age40; break;
                case 41: result = this.Age41; break;
                case 42: result = this.Age42; break;
                case 43: result = this.Age43; break;
                case 44: result = this.Age44; break;
                case 45: result = this.Age45; break;
                case 46: result = this.Age46; break;
                case 47: result = this.Age47; break;
                case 48: result = this.Age48; break;
                case 49: result = this.Age49; break;
                case 50: result = this.Age50; break;
                case 51: result = this.Age51; break;
                case 52: result = this.Age52; break;
                case 53: result = this.Age53; break;
                case 54: result = this.Age54; break;
                case 55: result = this.Age55; break;
                case 56: result = this.Age56; break;
                case 57: result = this.Age57; break;
                case 58: result = this.Age58; break;
                case 59: result = this.Age59; break;
                case 60: result = this.Age60; break;
                case 61: result = this.Age61; break;
                case 62: result = this.Age62; break;
                case 63: result = this.Age63; break;
                case 64: result = this.Age64; break;
                case 65: result = this.Age65; break;
                case 66: result = this.Age66; break;
                case 67: result = this.Age67; break;
                case 68: result = this.Age68; break;
                case 69: result = this.Age69; break;
                case 70: result = this.Age70; break;
                case 71: result = this.Age71; break;
                case 72: result = this.Age72; break;
                case 73: result = this.Age73; break;
                case 74: result = this.Age74; break;
                case 75: result = this.Age75; break;
                case 76: result = this.Age76; break;
                case 77: result = this.Age77; break;
                case 78: result = this.Age78; break;
                case 79: result = this.Age79; break;
                case 80: result = this.Age80; break;
                case 81: result = this.Age81; break;
                case 82: result = this.Age82; break;
                case 83: result = this.Age83; break;
                case 84: result = this.Age84; break;
                case 85: result = this.Age85; break;
                case 86: result = this.Age86; break;
                case 87: result = this.Age87; break;
                case 88: result = this.Age88; break;
                case 89: result = this.Age89; break;
                case 90: result = this.Age90; break;
                case 91: result = this.Age91; break;
                case 92: result = this.Age92; break;
                case 93: result = this.Age93; break;
                case 94: result = this.Age94; break;
                case 95: result = this.Age95; break;
                case 96: result = this.Age96; break;
                case 97: result = this.Age97; break;
                case 98: result = this.Age98; break;
                case 99: result = this.Age99; break;
            }
            return result.GetValueOrDefault(0);
        }
    }
    public partial class Cobra_Incidence
    {
        public long ID { get; set; }
        public Nullable<long> Year { get; set; }
        public Nullable<long> DestinationID { get; set; }
        public Nullable<long> FIPS { get; set; }
        public string Endpoint { get; set; }
        public Nullable<double> Age0 { get; set; }
        public Nullable<double> Age1 { get; set; }
        public Nullable<double> Age2 { get; set; }
        public Nullable<double> Age3 { get; set; }
        public Nullable<double> Age4 { get; set; }
        public Nullable<double> Age5 { get; set; }
        public Nullable<double> Age6 { get; set; }
        public Nullable<double> Age7 { get; set; }
        public Nullable<double> Age8 { get; set; }
        public Nullable<double> Age9 { get; set; }
        public Nullable<double> Age10 { get; set; }
        public Nullable<double> Age11 { get; set; }
        public Nullable<double> Age12 { get; set; }
        public Nullable<double> Age13 { get; set; }
        public Nullable<double> Age14 { get; set; }
        public Nullable<double> Age15 { get; set; }
        public Nullable<double> Age16 { get; set; }
        public Nullable<double> Age17 { get; set; }
        public Nullable<double> Age18 { get; set; }
        public Nullable<double> Age19 { get; set; }
        public Nullable<double> Age20 { get; set; }
        public Nullable<double> Age21 { get; set; }
        public Nullable<double> Age22 { get; set; }
        public Nullable<double> Age23 { get; set; }
        public Nullable<double> Age24 { get; set; }
        public Nullable<double> Age25 { get; set; }
        public Nullable<double> Age26 { get; set; }
        public Nullable<double> Age27 { get; set; }
        public Nullable<double> Age28 { get; set; }
        public Nullable<double> Age29 { get; set; }
        public Nullable<double> Age30 { get; set; }
        public Nullable<double> Age31 { get; set; }
        public Nullable<double> Age32 { get; set; }
        public Nullable<double> Age33 { get; set; }
        public Nullable<double> Age34 { get; set; }
        public Nullable<double> Age35 { get; set; }
        public Nullable<double> Age36 { get; set; }
        public Nullable<double> Age37 { get; set; }
        public Nullable<double> Age38 { get; set; }
        public Nullable<double> Age39 { get; set; }
        public Nullable<double> Age40 { get; set; }
        public Nullable<double> Age41 { get; set; }
        public Nullable<double> Age42 { get; set; }
        public Nullable<double> Age43 { get; set; }
        public Nullable<double> Age44 { get; set; }
        public Nullable<double> Age45 { get; set; }
        public Nullable<double> Age46 { get; set; }
        public Nullable<double> Age47 { get; set; }
        public Nullable<double> Age48 { get; set; }
        public Nullable<double> Age49 { get; set; }
        public Nullable<double> Age50 { get; set; }
        public Nullable<double> Age51 { get; set; }
        public Nullable<double> Age52 { get; set; }
        public Nullable<double> Age53 { get; set; }
        public Nullable<double> Age54 { get; set; }
        public Nullable<double> Age55 { get; set; }
        public Nullable<double> Age56 { get; set; }
        public Nullable<double> Age57 { get; set; }
        public Nullable<double> Age58 { get; set; }
        public Nullable<double> Age59 { get; set; }
        public Nullable<double> Age60 { get; set; }
        public Nullable<double> Age61 { get; set; }
        public Nullable<double> Age62 { get; set; }
        public Nullable<double> Age63 { get; set; }
        public Nullable<double> Age64 { get; set; }
        public Nullable<double> Age65 { get; set; }
        public Nullable<double> Age66 { get; set; }
        public Nullable<double> Age67 { get; set; }
        public Nullable<double> Age68 { get; set; }
        public Nullable<double> Age69 { get; set; }
        public Nullable<double> Age70 { get; set; }
        public Nullable<double> Age71 { get; set; }
        public Nullable<double> Age72 { get; set; }
        public Nullable<double> Age73 { get; set; }
        public Nullable<double> Age74 { get; set; }
        public Nullable<double> Age75 { get; set; }
        public Nullable<double> Age76 { get; set; }
        public Nullable<double> Age77 { get; set; }
        public Nullable<double> Age78 { get; set; }
        public Nullable<double> Age79 { get; set; }
        public Nullable<double> Age80 { get; set; }
        public Nullable<double> Age81 { get; set; }
        public Nullable<double> Age82 { get; set; }
        public Nullable<double> Age83 { get; set; }
        public Nullable<double> Age84 { get; set; }
        public Nullable<double> Age85 { get; set; }
        public Nullable<double> Age86 { get; set; }
        public Nullable<double> Age87 { get; set; }
        public Nullable<double> Age88 { get; set; }
        public Nullable<double> Age89 { get; set; }
        public Nullable<double> Age90 { get; set; }
        public Nullable<double> Age91 { get; set; }
        public Nullable<double> Age92 { get; set; }
        public Nullable<double> Age93 { get; set; }
        public Nullable<double> Age94 { get; set; }
        public Nullable<double> Age95 { get; set; }
        public Nullable<double> Age96 { get; set; }
        public Nullable<double> Age97 { get; set; }
        public Nullable<double> Age98 { get; set; }
        public Nullable<double> Age99 { get; set; }
        public double incidenceat(long age)
        {
            double? result = 0d;
            switch (age)
            {
                case 0: result = this.Age0; break;
                case 1: result = this.Age1; break;
                case 2: result = this.Age2; break;
                case 3: result = this.Age3; break;
                case 4: result = this.Age4; break;
                case 5: result = this.Age5; break;
                case 6: result = this.Age6; break;
                case 7: result = this.Age7; break;
                case 8: result = this.Age8; break;
                case 9: result = this.Age9; break;
                case 10: result = this.Age10; break;
                case 11: result = this.Age11; break;
                case 12: result = this.Age12; break;
                case 13: result = this.Age13; break;
                case 14: result = this.Age14; break;
                case 15: result = this.Age15; break;
                case 16: result = this.Age16; break;
                case 17: result = this.Age17; break;
                case 18: result = this.Age18; break;
                case 19: result = this.Age19; break;
                case 20: result = this.Age20; break;
                case 21: result = this.Age21; break;
                case 22: result = this.Age22; break;
                case 23: result = this.Age23; break;
                case 24: result = this.Age24; break;
                case 25: result = this.Age25; break;
                case 26: result = this.Age26; break;
                case 27: result = this.Age27; break;
                case 28: result = this.Age28; break;
                case 29: result = this.Age29; break;
                case 30: result = this.Age30; break;
                case 31: result = this.Age31; break;
                case 32: result = this.Age32; break;
                case 33: result = this.Age33; break;
                case 34: result = this.Age34; break;
                case 35: result = this.Age35; break;
                case 36: result = this.Age36; break;
                case 37: result = this.Age37; break;
                case 38: result = this.Age38; break;
                case 39: result = this.Age39; break;
                case 40: result = this.Age40; break;
                case 41: result = this.Age41; break;
                case 42: result = this.Age42; break;
                case 43: result = this.Age43; break;
                case 44: result = this.Age44; break;
                case 45: result = this.Age45; break;
                case 46: result = this.Age46; break;
                case 47: result = this.Age47; break;
                case 48: result = this.Age48; break;
                case 49: result = this.Age49; break;
                case 50: result = this.Age50; break;
                case 51: result = this.Age51; break;
                case 52: result = this.Age52; break;
                case 53: result = this.Age53; break;
                case 54: result = this.Age54; break;
                case 55: result = this.Age55; break;
                case 56: result = this.Age56; break;
                case 57: result = this.Age57; break;
                case 58: result = this.Age58; break;
                case 59: result = this.Age59; break;
                case 60: result = this.Age60; break;
                case 61: result = this.Age61; break;
                case 62: result = this.Age62; break;
                case 63: result = this.Age63; break;
                case 64: result = this.Age64; break;
                case 65: result = this.Age65; break;
                case 66: result = this.Age66; break;
                case 67: result = this.Age67; break;
                case 68: result = this.Age68; break;
                case 69: result = this.Age69; break;
                case 70: result = this.Age70; break;
                case 71: result = this.Age71; break;
                case 72: result = this.Age72; break;
                case 73: result = this.Age73; break;
                case 74: result = this.Age74; break;
                case 75: result = this.Age75; break;
                case 76: result = this.Age76; break;
                case 77: result = this.Age77; break;
                case 78: result = this.Age78; break;
                case 79: result = this.Age79; break;
                case 80: result = this.Age80; break;
                case 81: result = this.Age81; break;
                case 82: result = this.Age82; break;
                case 83: result = this.Age83; break;
                case 84: result = this.Age84; break;
                case 85: result = this.Age85; break;
                case 86: result = this.Age86; break;
                case 87: result = this.Age87; break;
                case 88: result = this.Age88; break;
                case 89: result = this.Age89; break;
                case 90: result = this.Age90; break;
                case 91: result = this.Age91; break;
                case 92: result = this.Age92; break;
                case 93: result = this.Age93; break;
                case 94: result = this.Age94; break;
                case 95: result = this.Age95; break;
                case 96: result = this.Age96; break;
                case 97: result = this.Age97; break;
                case 98: result = this.Age98; break;
                case 99: result = this.Age99; break;
            }
            return result.GetValueOrDefault(0);
        }
    }
    public partial class Cobra_CR
    {
        public long ID { get; set; }
        public Nullable<long> FunctionID { get; set; }
        public string Endpoint { get; set; }
        public Nullable<double> PoolingWeight { get; set; }
        public string Seasonal_Metric { get; set; }
        public string Study_Author { get; set; }
        public Nullable<long> Study_Year { get; set; }
        public Nullable<long> Start_Age { get; set; }
        public Nullable<long> End_Age { get; set; }
        public string Function { get; set; }
        public Nullable<double> Beta { get; set; }
        public string Adjusted { get; set; }
        public Nullable<double> Parameter_1_Beta { get; set; }
        public Nullable<double> A { get; set; }
        public string Name_A { get; set; }
        public Nullable<double> B { get; set; }
        public string Name_B { get; set; }
        public Nullable<double> C { get; set; }
        public string Name_C { get; set; }
        public Nullable<long> Cases { get; set; }
        public string IncidenceEndpoint { get; set; }
    }
    public partial class Cobra_Result
    {
        public List<Cobra_ResultDetail> Impacts;
        public Cobra_ResultSummary Summary;
    }

    public partial class Custom_ResultDetail
    {
        public long ID { get; set; }
        public Nullable<long> destindx { get; set; }
        public Nullable<double> BASE_FINAL_PM { get; set; }
        public Nullable<double> CTRL_FINAL_PM { get; set; }
        public Nullable<double> DELTA_FINAL_PM { get; set; }
        public string FIPS { get; set; }
        public string STATE { get; set; }
        public string COUNTY { get; set; }

        public string tract_id { get; set; }
        /*public Nullable<double> BASE_FINAL_O3 { get; set; }
        public Nullable<double> CTRL_FINAL_O3 { get; set; }
        public Nullable<double> DELTA_FINAL_O3 { get; set; }*/


        /*public Nullable<double> C__Total_PM_Low_Value { get; set; }

        public Nullable<double> C__Total_PM_High_Value { get; set; }

        public Nullable<double> C__Total_O3_Low_Value { get; set; }

        public Nullable<double> C__Total_O3_High_Value { get; set; }*/
        public Nullable<double> C__Total_Health_Benefits_Low_Value { get; set; }
        public Nullable<double> C__Total_Health_Benefits_High_Value { get; set; }



        // Dictionary to store dynamic properties based of endpoint names provided
        private Dictionary<string, double> dynamicProperties = new Dictionary<string, double>();
        public void SetDynamicProperty(string key, double value, bool valuation)
        {
            //convert to expected format
            // Replace spaces with underscores
            string transformedKey = key.Replace(" ", "_");
            // Replace parentheses with underscores
            transformedKey = transformedKey.Replace("(", "_").Replace(")", "_");
            // Remove commas
            transformedKey = transformedKey.Replace(",", "");
            //remove periods
            transformedKey = transformedKey.Replace(".", "");


            //add C__ prefix for valuations
            if (valuation)
            {
                transformedKey = "C__" + transformedKey;
            }


            dynamicProperties[transformedKey] = value;
        }

        public double GetDynamicProperty(string key)
        {
            return dynamicProperties.TryGetValue(key, out double value) ? value : 0;
        }

        public IEnumerable<string> GetDynamicPropertyKeys()
        {
            return dynamicProperties.Keys;
        }

      
    }

    public class Custom_ResultDetailConverter : JsonConverter<Custom_ResultDetail>
    {
        public override void WriteJson(JsonWriter writer, Custom_ResultDetail value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            // Write regular properties
            writer.WritePropertyName(nameof(Custom_ResultDetail.ID));
            writer.WriteValue(value.ID);
            writer.WritePropertyName(nameof(Custom_ResultDetail.destindx));
            writer.WriteValue(value.destindx);
            writer.WritePropertyName(nameof(Custom_ResultDetail.BASE_FINAL_PM));
            writer.WriteValue(value.BASE_FINAL_PM);
            writer.WritePropertyName(nameof(Custom_ResultDetail.CTRL_FINAL_PM));
            writer.WriteValue(value.CTRL_FINAL_PM);
            writer.WritePropertyName(nameof(Custom_ResultDetail.DELTA_FINAL_PM));
            writer.WriteValue(value.DELTA_FINAL_PM);
            writer.WritePropertyName(nameof(Custom_ResultDetail.FIPS));
            writer.WriteValue(value.FIPS);
            writer.WritePropertyName(nameof(Custom_ResultDetail.STATE));
            writer.WriteValue(value.STATE);
            writer.WritePropertyName(nameof(Custom_ResultDetail.COUNTY));
            writer.WriteValue(value.COUNTY);
            writer.WritePropertyName(nameof(Custom_ResultDetail.C__Total_Health_Benefits_Low_Value));
            writer.WriteValue(value.C__Total_Health_Benefits_Low_Value);
            writer.WritePropertyName(nameof(Custom_ResultDetail.C__Total_Health_Benefits_High_Value));
            writer.WriteValue(value.C__Total_Health_Benefits_High_Value);

            // Write dynamic properties
            foreach (var kvp in value.GetDynamicPropertyKeys())
            {
                writer.WritePropertyName(kvp);
                writer.WriteValue(value.GetDynamicProperty(kvp));
            }

            writer.WriteEndObject();
        }

        public override Custom_ResultDetail ReadJson(JsonReader reader, Type objectType, Custom_ResultDetail existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    public partial class Cobra_ResultDetail
    {
        public long ID { get; set; }
        public Nullable<long> destindx { get; set; }
        public string tract_id { get; set; }

        public Nullable<double> BASE_FINAL_PM { get; set; }
        public Nullable<double> CTRL_FINAL_PM { get; set; }
        public Nullable<double> DELTA_FINAL_PM { get; set; }
        public Nullable<double> BASE_FINAL_O3 { get; set; }
        public Nullable<double> CTRL_FINAL_O3 { get; set; }
        public Nullable<double> DELTA_FINAL_O3 { get; set; }



        public Nullable<double> PM_HA_All_Respiratory { get; set; }

        public Nullable<double> PM_Acute_Myocardial_Infarction_Nonfatal { get; set; }

        public Nullable<double> PM_Minor_Restricted_Activity_Days { get; set; }

        public Nullable<double> PM_Mortality_All_Cause__low_ { get; set; }
        public Nullable<double> PM_Mortality_All_Cause__high_ { get; set; }

        public Nullable<double> PM_Infant_Mortality { get; set; }

        public Nullable<double> PM_Work_Loss_Days { get; set; }

        public Nullable<double> PM_Incidence_Lung_Cancer { get; set; }

        public Nullable<double> PM_Incidence_Hay_Fever_Rhinitis { get; set; }
        public Nullable<double> PM_Incidence_Asthma { get; set; }

        public Nullable<double> PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease { get; set; }
        public Nullable<double> PM_HA_Alzheimers_Disease { get; set; }
        public Nullable<double> PM_HA_Parkinsons_Disease { get; set; }
        public Nullable<double> PM_Incidence_Stroke { get; set; }

        public Nullable<double> PM_Incidence_Out_of_Hospital_Cardiac_Arrest { get; set; }

        public Nullable<double> PM_Asthma_Symptoms_Albuterol_use { get; set; }

        public Nullable<double> PM_HA_Respiratory2 { get; set; }


        public Nullable<double> PM_ER_visits_respiratory { get; set; }
        public Nullable<double> PM_ER_visits_All_Cardiac_Outcomes { get; set; }
        public Nullable<double> O3_ER_visits_respiratory { get; set; }
        public Nullable<double> O3_HA_All_Respiratory { get; set; }
        public Nullable<double> O3_Incidence_Hay_Fever_Rhinitis { get; set; }
        public Nullable<double> O3_Incidence_Asthma { get; set; }
        public Nullable<double> O3_Asthma_Symptoms_Chest_Tightness { get; set; }
        public Nullable<double> O3_Asthma_Symptoms_Cough { get; set; }
        public Nullable<double> O3_Asthma_Symptoms_Shortness_of_Breath { get; set; }
        public Nullable<double> O3_Asthma_Symptoms_Wheeze { get; set; }

        public Nullable<double> O3_ER_Visits_Asthma { get; set; }
        public Nullable<double> O3_School_Loss_Days { get; set; }
        public Nullable<double> O3_Mortality_Longterm_exposure { get; set; }
        public Nullable<double> O3_Mortality_Shortterm_exposure { get; set; }


        public Nullable<double> C__PM_Acute_Myocardial_Infarction_Nonfatal { get; set; }
        public Nullable<double> C__PM_Resp_Hosp_Adm { get; set; }
        public Nullable<double> C__PM_Minor_Restricted_Activity_Days { get; set; }
        public Nullable<double> C__PM_Mortality_All_Cause__low_ { get; set; }
        public Nullable<double> C__PM_Mortality_All_Cause__high_ { get; set; }


        public Nullable<double> C__PM_Infant_Mortality { get; set; }
        public Nullable<double> C__PM_Work_Loss_Days { get; set; }
        public Nullable<double> C__PM_Incidence_Lung_Cancer { get; set; }
        public Nullable<double> C__PM_Incidence_Hay_Fever_Rhinitis { get; set; }

        public Nullable<double> C__PM_Incidence_Asthma { get; set; }
        public Nullable<double> C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease { get; set; }
        public Nullable<double> C__PM_HA_Alzheimers_Disease { get; set; }
        public Nullable<double> C__PM_HA_Parkinsons_Disease { get; set; }
        public Nullable<double> C__PM_Incidence_Stroke { get; set; }
        public Nullable<double> C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest { get; set; }
        public Nullable<double> C__PM_Asthma_Symptoms_Albuterol_use { get; set; }
        public Nullable<double> C__PM_HA_Respiratory2 { get; set; }
        public Nullable<double> C__PM_ER_visits_respiratory { get; set; }

        public Nullable<double> C__PM_ER_visits_All_Cardiac_Outcomes { get; set; }
        public Nullable<double> C__O3_ER_visits_respiratory { get; set; }


        public Nullable<double> C__O3_HA_All_Respiratory { get; set; }
        public Nullable<double> C__O3_Incidence_Hay_Fever_Rhinitis { get; set; }
        public Nullable<double> C__O3_Incidence_Asthma { get; set; }
        public Nullable<double> C__O3_Asthma_Symptoms_Chest_Tightness { get; set; }
        public Nullable<double> C__O3_Asthma_Symptoms_Cough { get; set; }

        public Nullable<double> C__O3_Asthma_Symptoms_Shortness_of_Breath { get; set; }
        public Nullable<double> C__O3_Asthma_Symptoms_Wheeze { get; set; }


        public Nullable<double> C__O3_ER_Visits_Asthma { get; set; }
        public Nullable<double> C__O3_School_Loss_Days { get; set; }
        public Nullable<double> C__O3_Mortality_Longterm_exposure { get; set; }
        public Nullable<double> C__O3_Mortality_Shortterm_exposure { get; set; }

        /* from old cobra */
        public Nullable<double> C__Total_Health_Benefits_Low_Value { get; set; }
        public Nullable<double> C__Total_Health_Benefits_High_Value { get; set; }

        public Nullable<double> C__Total_PM_Low_Value { get; set; }
        public Nullable<double> C__Total_PM_High_Value { get; set; }
        public Nullable<double> C__Total_O3_Value { get; set; }
        public string FIPS { get; set; }
        public string STATE { get; set; }
        public string COUNTY { get; set; }
        /* disadvantaged community variables */
        public Nullable<double> IRA_fraction { get; set; }
        public Nullable<int> CJEST { get; set; }

        public Dictionary<string, double> TRIBES { get; set; }
    }
    public partial class Cobra_ResultSummary
    {
        public double TotalHealthBenefitsValue_low { get; set; }
        public double TotalHealthBenefitsValue_high { get; set; }
        public double TotalPMValue_low { get; set; }
        public double TotalPMValue_high { get; set; }
        public double TotalO3Value { get; set; }

        public double TotalPM_low { get; set; }
        public double TotalPM_high { get; set; }
        public double TotalO3 { get; set; }

        public double Mortality_All_Cause__low_ { get; set; } //PM low + O3 short and longterm exposure
        public double C__Mortality_All_Cause__low_ { get; set; }

        public double Mortality_All_Cause__high_ { get; set; } //PM high + O3 short and longterm exposure
        public double C__Mortality_All_Cause__high_ { get; set; }


        /****** MORTALITY BREAKDOWN ****/
        public double PM_Mortality_All_Cause__high_ { get; set; } //PM high + O3 short and longterm exposure
        public double C__PM_Mortality_All_Cause__high_ { get; set; }
        public double PM_Mortality_All_Cause__low_ { get; set; } //PM high + O3 short and longterm exposure
        public double C__PM_Mortality_All_Cause__low_ { get; set; }

        public double O3_Mortality_Longterm_exposure { get; set; }
        public double O3_Mortality_Shortterm_exposure { get; set; }
        public double C__O3_Mortality_Longterm_exposure { get; set; }
        public double C__O3_Mortality_Shortterm_exposure { get; set; }
        /****** END MORTALITY BREAKDOWN ****/



        //summary of grouped together health effects (if possible)
        public double Acute_Myocardial_Infarction_Nonfatal { get; set; } //PM Only
        public double C__Acute_Myocardial_Infarction_Nonfatal { get; set; }

        public double ER_Visits_Asthma { get; set; } //O3 Only
        public double C__ER_Visits_Asthma { get; set; }

        public double HA_All_Respiratory { get; set; } //PM +O3 + currently plan to group with PM_HA Respiratory2 in this summary
        public double C__HA_All_Respiratory { get; set; } //$ for ha all respiratory

        /****************************** HA ALL RESP BREAKDOWN *********************/
        public double PM_HA_All_Respiratory { get; set; }
        public double C__PM_HA_All_Respiratory { get; set; }
        public double O3_HA_All_Respiratory { get; set; }
        public double C__O3_HA_All_Respiratory { get; set; }
        /****************************** End HA ALL RESP BREAKDOWN *********************/

        public double Minor_Restricted_Activity_Days { get; set; } //PM Only
        public double C__Minor_Restricted_Activity_Days { get; set; }




        public double Infant_Mortality { get; set; } //PM Only
        public double C__Infant_Mortality { get; set; }
        public double Incidence_Lung_Cancer { get; set; } //PM Only
        public double C__Incidence_Lung_Cancer { get; set; } //PM Only

        public double HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease { get; set; } //PM Only
        public double C__HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease { get; set; }


        public double HA_Alzheimers_Disease { get; set; } //PM Only
        public double C__HA_Alzheimers_Disease { get; set; } //PM Only


        public double HA_Parkinsons_Disease { get; set; } //PM Only
        public double C__HA_Parkinsons_Disease { get; set; } //PM Only

        public double Incidence_Stroke { get; set; } //PM Only
        public double C__Incidence_Stroke { get; set; } //PM Only

        public double Incidence_Out_of_Hospital_Cardiac_Arrest { get; set; } //PM Only
        public double C__Incidence_Out_of_Hospital_Cardiac_Arrest { get; set; } //PM Only

        public double Incidence_Asthma { get; set; } //PM + O3
        public double C__Incidence_Asthma { get; set; }
        /************************* asthma breakdown ****************************************/
        public double PM_Incidence_Asthma { get; set; }
        public double C__PM_Incidence_Asthma { get; set; }
        public double O3_Incidence_Asthma { get; set; }
        public double C__O3_Incidence_Asthma { get; set; }
        /***************** asthma breakdown ******************/

        public double Asthma_Symptoms { get; set; } //PM Asthma Sympotms Albuterol + O3 Asthma Symptoms cough + shortness of breath + chest tightness + wheeze
        public double C__Asthma_Symptoms { get; set; }
        /*************** asthma symptoms breakdown ************/
        public double PM_Asthma_Symptoms_Albuterol_use { get; set; }
        public double O3_Asthma_Symptoms_Chest_Tightness { get; set; }
        public double O3_Asthma_Symptoms_Cough { get; set; }

        public double O3_Asthma_Symptoms_Shortness_of_Breath { get; set; }
        public double O3_Asthma_Symptoms_Wheeze { get; set; }


        public double C__O3_Asthma_Symptoms_Chest_Tightness { get; set; }
        public double C__O3_Asthma_Symptoms_Cough { get; set; }

        public double C__O3_Asthma_Symptoms_Shortness_of_Breath { get; set; }
        public double C__O3_Asthma_Symptoms_Wheeze { get; set; }
        public double C__PM_Asthma_Symptoms_Albuterol_use { get; set; }
        /* end asthma symptoms breakdown */


        public double Incidence_Hay_Fever_Rhinitis { get; set; } //PM + O3
        public double C__Incidence_Hay_Fever_Rhinitis { get; set; }

        /************* Hay fever breakdown ******/
        public double PM_Incidence_Hay_Fever_Rhinitis { get; set; }
        public double C__PM_Incidence_Hay_Fever_Rhinitis { get; set; }
        public double O3_Incidence_Hay_Fever_Rhinitis { get; set; }
        public double C__O3_Incidence_Hay_Fever_Rhinitis { get; set; }
        /************* End Hay fever breakdown ******/

        public double ER_visits_All_Cardiac_Outcomes { get; set; } //PM Only
        public double C__ER_visits_All_Cardiac_Outcomes { get; set; } //PM Only



        public double ER_visits_respiratory { get; set; } //PM + O3
        public double C__ER_visits_respiratory { get; set; } //PM + O3

        /************* ER RESP BREAKDOWN ******/
        public double PM_ER_visits_respiratory { get; set; }
        public double C__PM_ER_visits_respiratory { get; set; }
        public double O3_ER_visits_respiratory { get; set; }
        public double C__O3_ER_visits_respiratory { get; set; }
        /************* END ER RESP BREAKDOWN ******/

        public double School_Loss_Days { get; set; } //PM Only
        public double C__School_Loss_Days { get; set; } //O3 Only


        public double Work_Loss_Days { get; set; } //PM Only
        public double C__Work_Loss_Days { get; set; }

    }







    public partial class Cobra_Dict_State
    {
        public long ID { get; set; }
        public Nullable<long> SOURCEINDX { get; set; }
        public string FIPS { get; set; }
        public string STFIPS { get; set; }
        public string CNTYFIPS { get; set; }
        public string STNAME { get; set; }
        public string CYNAME { get; set; }
    }
    public partial class Cobra_Dict_Tier
    {
        public long ID { get; set; }
        public long TIER1 { get; set; }
        public long TIER2 { get; set; }
        public long TIER3 { get; set; }
        public string TIER1NAME { get; set; }
        public string TIER2NAME { get; set; }
        public string TIER3NAME { get; set; }
    }
    public partial class Cobra_Adjustment
    {
        public int? indx { get; set; }
        public double? F1 { get; set; }
    }
    public partial class Cobra_Voc2Soa
    {
        public int ID { get; set; }
        public int TIER1 { get; set; }
        public int TIER2 { get; set; }
        public int TIER3 { get; set; }
        public double FACTOR { get; set; }
    }
    public partial class CobraUpdateBundle
    {
        public Guid token;
        public EmissionsRecord[] emissions;
    }
    public class EmissionsDataRetrievalRequest
    {
        public Guid token { get; set; }
        public string[] fipscodes { get; set; }
        public string tiers { get; set; }
    }
    public class EmissionsSums
    {
        public DataTable baseline { get; set; }
        public DataTable control { get; set; }
    }

    public class Emissions
    {
        public double NOx { get; set; }
        public double SO2 { get; set; }
        public double NH3 { get; set; }
        public double SOA { get; set; }
        public double PM25 { get; set; }
        public double VOC { get; set; }
        public double O3N { get; set; }
    }

    public class EmissionsDataUpdateRequest
    {
        public string operationalMode { get; set; }
        public EmissionsDataRetrievalRequest spec { get; set; }
        public Emissions payload { get; set; }
    }

    public class RedisMatrix
    {
        public long Id { get; set; }
        public Matrix<double> Content { get; set; }
        public string Annotation { get; set; }

    }

    public partial class ImpactComputeRequest
    {
        public double delta_pm;
        public double base_pm;
        public double control_pm;
        public Cobra_POP population;
        public Cobra_Incidence[] incidence;
        public bool valat3;
    }

    public partial class CustomImpactComputeRequest : ImpactComputeRequest
    {
        public Cobra_CR_Core[] CustomCRFunctions;
        public Cobra_Valuation_Core[] CustomValuationFunctions;
    }

    public partial class Cobra_CR_Core
    {
        public string Endpoint { get; set; }
        public Nullable<double> PoolingWeight { get; set; }
        public string Seasonal_Metric { get; set; }
        public Nullable<long> Start_Age { get; set; }
        public Nullable<long> End_Age { get; set; }
        // cr function
        public string Function { get; set; }
        public Nullable<double> Beta { get; set; }
        public Nullable<double> A { get; set; }
        public Nullable<double> B { get; set; }
        public Nullable<double> C { get; set; }
        public string IncidenceEndpoint { get; set; }
    }

    public partial class Cobra_Valuation_Core
    {
        public string Endpoint { get; set; }
        public Nullable<double> PoolingWeight { get; set; }
        public string Seasonal_Metric { get; set; }
        public Nullable<long> Start_Age { get; set; }
        public Nullable<long> End_Age { get; set; }
        public string Function { get; set; }
        public Nullable<double> Beta { get; set; }
        public Nullable<double> A { get; set; }
        public Nullable<double> B { get; set; }
        public Nullable<double> C { get; set; }
        public Nullable<double> valat3pct { get; set; }
        public Nullable<double> valat7pct { get; set; }
        public string IncidenceEndpoint { get; set; }
    }
    public class ResetRequest
    {
        public Guid token { get; set; }
    }

    public class UpdatePacket
    {
        public double PM25 { get; set; }
        public double SO2 { get; set; }
        public double NOx { get; set; }
        public double NH3 { get; set; }
        public double VOC { get; set; }
        public string[] fipscodes { get; set; }
        public string[] tierselection { get; set; }
    }


    public class QueueElement
    {
        public string[] stateCountyBadgesList { get; set; }
        public string tier1Text { get; set; }
        public string tier2Text { get; set; }
        public string tier3Text { get; set; }
        public string PM25ri { get; set; }
        public string SO2ri { get; set; }
        public string NOXri { get; set; }
        public string NH3ri { get; set; }
        public string VOCri { get; set; }
        public float cPM25 { get; set; }
        public float cSO2 { get; set; }
        public float cNOX { get; set; }
        public float cNH3 { get; set; }
        public float cVOC { get; set; }
        public string PM25pt { get; set; }
        public string SO2pt { get; set; }
        public string NOXpt { get; set; }
        public string NH3pt { get; set; }
        public string VOCpt { get; set; }
        public string[] statetree_items_selected { get; set; }
        public string[] tiertree_items_selected { get; set; }
        public UpdatePacket updatePacket { get; set; }
    }

    public class AvertInput
    {
         public string annualGwhReduction { get; set;}

        public string hourlyMwReduction { get; set; }
        public string broadProgramReduction { get; set; }
        public string targetedProgramReduction { get; set; }
        public string topHours { get; set; }
        public string onshoreWind { get; set; }
        public string  offshoreWind { get; set; }
        public string utilitySolar { get; set; }
        public string rooftopSolar { get; set; }
        public string utilityStorage { get; set; }
        public string rooftopStorage { get; set; }
        public string maxAnnualDischargeCycles { get; set; }
        public string hybridEVs { get; set; }
        public string batteryEVs { get; set; }
        public string transitBuses { get; set; }
        public string schoolBuses { get; set; }
        public string evDeploymentLocation { get; set; }
        public string evModelYear { get; set; }
        public string iceReplacementVehicle { get; set; }

    }

    public class queueSubmission
    {
        public Guid token { get; set; }

        public string Description { get; set; }
        public QueueElement[] queueElements { get; set; }

        public string[] avertRegions { get; set; }

        public AvertInput avertInputs {get; set;}

    }


    // Class to map each record in slider_data.csv
    public class SliderRecord
    {
        public long ID { get; set; }
        public int? P_LOWINC { get; set; }
        public int? ENERGYBURDEN_LESS_6PCT { get; set; }
        public int? ENERGYBURDEN_GRTR_EQL_6PCT { get; set; }
        public int? ENERGYBURDEN_GRTR_EQL_10PCT { get; set; }
        public int? P_LIFEXPCT { get; set; }
        public int? P_PM25 { get; set; }
        public int? P_OZONE { get; set; }
    }

    // Class to hold only the slider data needed for JSON output
    public class SliderData
    {
        public int? P_LOWINC { get; set; }
        public int? ENERGYBURDEN_LESS_6PCT { get; set; }
        public int? ENERGYBURDEN_GRTR_EQL_6PCT { get; set; }
        public int? ENERGYBURDEN_GRTR_EQL_10PCT { get; set; }
        public int? P_LIFEXPCT { get; set; }
        public int? P_PM25 { get; set; }
        public int? P_OZONE { get; set; }
    }

    //custom sparse matrix class to help load SR_DP faster
    public class CSRSparseMatrix
    {
        private readonly int[] _rowPointers;
        private readonly int[] _columnIndices;
        private readonly double[] _values;
        private readonly int _numRows;
        private readonly int _numCols;

        public CSRSparseMatrix(int numRows, int numCols, int[] rowPointers, int[] columnIndices, double[] values)
        {
            _numRows = numRows;
            _numCols = numCols;
            _rowPointers = rowPointers;
            _columnIndices = columnIndices;
            _values = values;
        }

        public int RowCount => _numRows;
        public int ColumnCount => _numCols;
        public int NonZeroCount => _values.Length;

        // Method to retrieve a value at a specific row and column
        public double GetValue(int row, int col)
        {
            if (row < 0 || row >= _numRows) throw new ArgumentOutOfRangeException(nameof(row));
            if (col < 0 || col >= _numCols) throw new ArgumentOutOfRangeException(nameof(col));

            // Get the range for this row in the _columnIndices and _values arrays
            int rowStart = _rowPointers[row];
            int rowEnd = _rowPointers[row + 1];

            // Perform binary search within the row range for the specified column
            for (int i = rowStart; i < rowEnd; i++)
            {
                if (_columnIndices[i] == col)
                {
                    return _values[i];
                }
            }

            // If not found, return 0 (default for sparse matrices)
            return 0.0;
        }

        // Static method to load a CSRSparseMatrix from a binary file
        public static CSRSparseMatrix LoadFromBinary(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                // get matrix dimensions
                int numRows = reader.ReadInt32();
                int numCols = reader.ReadInt32();

                // number of non zero entries
                int nnz = reader.ReadInt32();

                // Read row pointers
                int[] rowPointers = new int[numRows + 1];
                for (int i = 0; i <= numRows; i++)
                {
                    rowPointers[i] = reader.ReadInt32();
                }

                // Read column indices
                int[] columnIndices = new int[nnz];
                for (int i = 0; i < nnz; i++)
                {
                    columnIndices[i] = reader.ReadInt32();
                }

                // Read values
                double[] values = new double[nnz];
                for (int i = 0; i < nnz; i++)
                {
                    values[i] = reader.ReadDouble();
                }

                // Return a new CSRSparseMatrix with the loaded data
                return new CSRSparseMatrix(numRows, numCols, rowPointers, columnIndices, values);
            }
        }


        // Optional: Method to iterate over non-zero values in a row
        public void ForEachNonZeroInRow(int row, Action<int, double> action)
        {
            if (row < 0 || row >= _numRows) throw new ArgumentOutOfRangeException(nameof(row));

            int rowStart = _rowPointers[row];
            int rowEnd = _rowPointers[row + 1];

            for (int i = rowStart; i < rowEnd; i++)
            {
                int col = _columnIndices[i];
                double value = _values[i];
                action(col, value);
            }
        }

        // Method to perform sparse matrix-vector multiplication
        public Vector<double> Multiply(Vector<double> vector)
        {
            if (vector.Count != _numCols)
            {
                throw new ArgumentException("Vector length must match the number of columns in the matrix.");
            }

            // create correctly sized result vector
            var result = Vector<double>.Build.Dense(_numRows);

            // multiply each row
            for (int row = 0; row < _numRows; row++)
            {
                double sum = 0.0;
                for (int j = _rowPointers[row]; j < _rowPointers[row + 1]; j++)
                {
                    int col = _columnIndices[j];
                    sum += _values[j] * vector[col];
                }
                result[row] = sum;
            }

            return result;
        }
    }

}