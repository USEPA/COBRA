using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cobra_console.units
{
    public class FileOrTable
    {
        public bool usetable;
        public string filename;
        public int dataidx;
    }

    public class Nodedesc
    {
        public int level;
        public Int64? tier1;
        public Int64? tier2;
        public Int64? tier3;
    }

    public class LocationDesc
    {
        public string statename;
        public string countyname;
        public string statefips;
        public string countyfips;
        public long sourceidx;
        public long stid;
        public long cyid;
    }

    public class Emissionssummary
    {
        public double NOx = 0;
        public double SO2 = 0;
        public double NH3 = 0;
        public double PM25 = 0;
        public double VOC = 0;
    }


}
