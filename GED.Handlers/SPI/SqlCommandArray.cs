using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;


namespace GED.Handlers
{
    public static class SqlCommandArray
    {
            // extention méthode to add an array of parameter to sql query
            public static SqlParameter[] addArrayCommand<T>(this SqlCommand cmd, IEnumerable<T> listeVal, string nomParam, char separator = ',')
            {
                List<SqlParameter> p = new List<SqlParameter>();
                List<string> names = new List<string>();
                int paramIndex = 0;
                foreach (var iter in listeVal)
                {
                    string paramName = string.Format("@{0}{1}", nomParam, paramIndex++);
                    names.Add(paramName);

                    p.Add(cmd.Parameters.AddWithValue(paramName, iter));
                }
                cmd.CommandText = cmd.CommandText.Replace("{" + nomParam + "}", string.Join(separator.ToString(), names));

                return p.ToArray();
            }
     
    }
}
