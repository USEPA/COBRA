//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace cobra_console.units
{
    using System;
    using System.Collections.Generic;
    
    public partial class SYS_SummarizedEmissions
    {
        public long ID { get; set; }
        public Nullable<long> sourceindx { get; set; }
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
        public Nullable<double> DELTA_NOx { get; set; }
        public Nullable<double> DELTA_SO2 { get; set; }
        public Nullable<double> DELTA_NH3 { get; set; }
        public Nullable<double> DELTA_PM25 { get; set; }
        public Nullable<double> DELTA_VOC { get; set; }
        public string TIER1NAME { get; set; }
        public string TIER2NAME { get; set; }
        public string TIER3NAME { get; set; }
        public string FIPS { get; set; }
        public string STATENAME { get; set; }
        public string COUNTYNAME { get; set; }
    }
}
