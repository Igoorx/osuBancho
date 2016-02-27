using System;
using System.Data;
using MySql.Data.MySqlClient;
using osuBancho.Database.Adapter;
using osuBancho.Database.Interfaces;

namespace osuBancho.Database
{
    public sealed class DatabaseConnection : IDatabaseClient
    {
        private readonly IQueryAdapter _adapter;
        private readonly MySqlConnection _con;

        public DatabaseConnection(string ConnectionStr)
        {
            this._con = new MySqlConnection(ConnectionStr);
            this._adapter = new NormalQueryReactor(this);
        }

        public void Dispose()
        {
            if (this._con.State == ConnectionState.Open)
            {
                this._con.Close();
            }

            this._con.Dispose();
            GC.SuppressFinalize(this);
        }

        public void connect()
        {
            this.Open();
        }

        public void disconnect()
        {
            this.Close();
        }

        public IQueryAdapter GetQueryReactor()
        {
            return this._adapter;
        }

        public void prepare()
        {
            // nothing here
        }

        public void reportDone()
        {
            Dispose();
        }

        public MySqlCommand createNewCommand()
        {
            return _con.CreateCommand();
        }

        public void Open()
        {
            if (_con.State == ConnectionState.Closed)
            {
                try
                {
                    _con.Open();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void Close()
        {
            if (_con.State == ConnectionState.Open)
            {
                _con.Close();
            }
        }
    }
}