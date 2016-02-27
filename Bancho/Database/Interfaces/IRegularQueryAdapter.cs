using System.Data;

namespace osuBancho.Database.Interfaces
{
    public interface IRegularQueryAdapter
    {
        void AddParameter(string name, object query);
        bool findsResult();
        int getInteger();
        DataRow getRow();
        string getString();
        DataTable getTable();
        void RunQuery(string query);
        void SetQuery(string query);
    }
}