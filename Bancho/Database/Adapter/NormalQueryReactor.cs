using System;
using osuBancho.Database.Interfaces;

namespace osuBancho.Database.Adapter
{
    public sealed class NormalQueryReactor : QueryAdapter, IQueryAdapter
    {
        public NormalQueryReactor(IDatabaseClient Client)
            : base(Client)
        {
            base.command = Client.createNewCommand();
        }

        public void Dispose()
        {
            base.command.Dispose();
            base.client.reportDone();
            GC.SuppressFinalize(obj: this);
        }
    }
}