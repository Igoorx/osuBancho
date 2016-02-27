using System;
using System.Data;
using MySql.Data.MySqlClient;
using osuBancho.Database.Interfaces;

namespace osuBancho.Database.Adapter
{
    public class QueryAdapter : IRegularQueryAdapter
    {
        protected IDatabaseClient client;
        protected MySqlCommand command;

        public bool dbEnabled = true;
        public QueryAdapter(IDatabaseClient Client)
        {
            client = Client;
        }

        public void AddParameter(string parameterName, object val)
        {
            command.Parameters.AddWithValue(parameterName, val);
        }

        public bool findsResult()
        {
            bool hasRows = false;
            try
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    hasRows = reader.HasRows;
                }
            }
            catch (Exception exception)
            {
                //Writer.LogQueryError(exception, command.CommandText);
                Console.Error.WriteLine(exception); //TODO: LoqQueryError
            }

            return hasRows;
        }

        public int getInteger()
        {
            int result = 0;
            try
            {
                object obj2 = command.ExecuteScalar();
                if (obj2 != null)
                {
                    int.TryParse(obj2.ToString(), out result);
                }
            }
            catch (Exception exception)
            {
                //Writer.LogQueryError(exception, command.CommandText);
                Console.Error.WriteLine(exception); //TODO: LoqQueryError
            }

            return result;
        }

        public DataRow getRow()
        {
            DataRow row = null;
            try
            {
                DataSet dataSet = new DataSet();
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                {
                    adapter.Fill(dataSet);
                }
                if ((dataSet.Tables.Count > 0) && (dataSet.Tables[0].Rows.Count == 1))
                {
                    row = dataSet.Tables[0].Rows[0];
                }
            }
            catch (Exception exception)
            {
                //Writer.LogQueryError(exception, command.CommandText);
                Console.Error.WriteLine(exception); //TODO: LoqQueryError
            }

            return row;
        }

        public string getString()
        {
            string str = string.Empty;
            try
            {
                object obj2 = command.ExecuteScalar();
                if (obj2 != null)
                {
                    str = obj2.ToString();
                }
            }
            catch (Exception exception)
            {
                //Writer.LogQueryError(exception, command.CommandText);
                Console.Error.WriteLine(exception); //TODO: LoqQueryError
            }

            return str;
        }

        public DataTable getTable()
        {
            var dataTable = new DataTable();
            if (!dbEnabled)
                return dataTable;

            try
            {
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }
            }
            catch (Exception exception)
            {
                //Writer.LogQueryError(exception, command.CommandText);
                Console.Error.WriteLine(exception); //TODO: LoqQueryError
            }

            return dataTable;
        }

        public void RunQuery(string query)
        {
            if (!dbEnabled)
                return;

            SetQuery(query);
            RunQuery();
        }

        public void SetQuery(string query)
        {
            command.Parameters.Clear();
            command.CommandText = query;
        }

        public void addParameter(string name, byte[] data)
        {
            command.Parameters.Add(new MySqlParameter(name, MySqlDbType.Blob, data.Length));
        }

        public long InsertQuery()
        {
            if (!dbEnabled)
                return 0;

            long lastInsertedId = 0L;
            try
            {
                command.ExecuteScalar();
                lastInsertedId = command.LastInsertedId;
            }
            catch (Exception exception)
            {
                //Writer.LogQueryError(exception, command.CommandText);
                Console.Error.WriteLine(exception); //TODO: LoqQueryError
            }
            return lastInsertedId;
        }

        public void RunQuery()
        {
            if (!dbEnabled)
                return;

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception exception)
            {
                //Writer.LogQueryError(exception, command.CommandText);
                Console.Error.WriteLine(exception); //TODO: LoqQueryError
            }
        }
    }
}