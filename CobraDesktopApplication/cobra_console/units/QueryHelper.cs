using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace cobra_console.units
{
    public class QueryHelper : IDisposable
    {
        public List<string> messages = new List<string>();

        private SQLiteConnection _dbConn;
        private string _EF_connectionstring;

        public QueryHelper()
        {
            _dbConn = new SQLiteConnection(Globals.connectionstring);
            _EF_connectionstring = Globals.connectionstring_EF;
        }

        public void executeQuery(string query)
        {
            SQLiteCommand cmd = _dbConn.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
        }

        public DataTable getDataTable(string query)
        {
            DataTable table = new DataTable();
            using (SQLiteCommand cmd = new SQLiteCommand(query, _dbConn))
            using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
            {
                adapter.Fill(table);
            }
            return table;
        }

        private void clearTable(string tablename)
        {
            executeQuery("delete from " + tablename);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _dbConn.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~QueryHelper() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion



    }
}
