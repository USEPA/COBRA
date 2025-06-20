using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cobra_console.units
{
    public partial class cobraEntities : DbContext
    {
        public cobraEntities(string connectionString) : base(connectionString)
        {
        }

    }
}
