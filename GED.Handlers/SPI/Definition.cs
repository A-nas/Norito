using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
// a enlever
using System.Windows.Forms;
using System.Configuration;

namespace GED.Handlers
{
    public static class Definition
    {
        //public static readonly SqlConnection connexionProd = new SqlConnection("data source=192.168.1.5\\DW;Database=Nortiaca_MEDIA;Uid=sa;password=NICKEL2000;");
        public static readonly SqlConnection connexionQualif = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
    }
}
