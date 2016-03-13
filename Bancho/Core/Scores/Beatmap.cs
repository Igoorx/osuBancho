using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osuBancho.Core.Scores
{
    class Beatmap
    {
        public readonly string hash;
        public readonly int id;
        public Beatmap(DataRow dataRow)
        {
            hash = (string) dataRow["file_md5"];
            id = (int) dataRow["id"];
        }
    }
}
