using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cobra_console.units
{
    class EmissionsRecord
    {
        public Nullable<long> typeindx { get; set; }
        public Nullable<long> sourceindx { get; set; }
        public Nullable<long> stid { get; set; }
        public Nullable<long> cyid { get; set; }
        public Nullable<long> TIER1 { get; set; }
        public Nullable<long> TIER2 { get; set; }
        public Nullable<long> TIER3 { get; set; }
        public Nullable<double> NOx { get; set; }
        public Nullable<double> SO2 { get; set; }
        public Nullable<double> NH3 { get; set; }
        public Nullable<double> SOA { get; set; }
        public Nullable<double> PM25 { get; set; }
        public Nullable<double> VOC { get; set; }
    }

    class Core_EmissionsRecord
    {
        public Nullable<long> typeindx { get; set; }
        public Nullable<long> sourceindx { get; set; }
        public Nullable<long> stid { get; set; }
        public Nullable<long> cyid { get; set; }
        public Nullable<long> TIER1 { get; set; }
        public Nullable<long> TIER2 { get; set; }
        public Nullable<long> TIER3 { get; set; }
        public Nullable<double> NOx { get; set; }
        public Nullable<double> SO2 { get; set; }
        public Nullable<double> NH3 { get; set; }
        public Nullable<double> PM25 { get; set; }
        public Nullable<double> VOC { get; set; }
    }

    //FIPS STATE   COUNTY TIER1NAME   NOx_REDUCTIONS_TONS SO2_REDUCTIONS_TONS PM25_REDUCTIONS_TONS

    public class AvertRec
    {
        public Nullable<long> FIPS { get; set; }
        public string STATE { get; set; }
        public string COUNTY { get; set; }
        public string TIER1NAME { get; set; }
        public Nullable<double> NOx_REDUCTIONS_TONS { get; set; }
        public Nullable<double> SO2_REDUCTIONS_TONS { get; set; }
        public Nullable<double> PM25_REDUCTIONS_TONS { get; set; }
        public Nullable<double> VOCS_REDUCTIONS_TONS { get; set; }
        public Nullable<double> NH3_REDUCTIONS_TONS { get; set; }
    }

    public sealed class AvertRecMap : ClassMap<AvertRec>
    {
        public AvertRecMap()
        {
            Map(m => m.FIPS);
            Map(m => m.STATE);
            Map(m => m.COUNTY);
            Map(m => m.TIER1NAME);
            Map(m => m.NOx_REDUCTIONS_TONS);
            Map(m => m.SO2_REDUCTIONS_TONS);
            Map(m => m.PM25_REDUCTIONS_TONS);
            Map(m => m.VOCS_REDUCTIONS_TONS).Optional();
            Map(m => m.NH3_REDUCTIONS_TONS).Optional();
        }
    }



    class Core_ScenarioEmissionsRecord
    {
        public Nullable<long> typeindx { get; set; }
        public Nullable<long> sourceindx { get; set; }
        public Nullable<long> stid { get; set; }
        public Nullable<long> cyid { get; set; }
        public Nullable<long> TIER1 { get; set; }
        public Nullable<long> TIER2 { get; set; }
        public Nullable<long> TIER3 { get; set; }
        public Nullable<double> BASE_NOx { get; set; }
        public Nullable<double> BASE_SO2 { get; set; }
        public Nullable<double> BASE_NH3 { get; set; }
        public Nullable<double> BASE_PM25 { get; set; }
        public Nullable<double> BASE_VOC { get; set; }
        public Nullable<double> CTRL_NOx { get; set; }
        public Nullable<double> CTRL_SO2 { get; set; }
        public Nullable<double> CTRL_NH3 { get; set; }
        public Nullable<double> CTRL_PM25 { get; set; }
        public Nullable<double> CTRL_VOC { get; set; }
    }

    class Core_Population
    {
        public Nullable<long> Year { get; set; }
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
    }

    class Core_Incidence
    {
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
    }

    class Core_CR
    {
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

    class Core_Valuation
    {
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

}
