using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;

namespace GenerationProd
{
    public class Log
    {
        //constante Type message
        public const string MESSAGE_INFO = "Info";
        public const string MESSAGE_WARNING = "Warning";
        public const string MESSAGE_ERROR = "Error";

        public static void Trace(string id_ProdSF, string typeMessage, string message)
        {
            SqlConnection con = null;
            SqlCommand cmd;

            string strSQL = "";

            try
            {
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                con.Open();

                strSQL = "INSERT INTO GenerationProd_Log(Date_Log,ID_ProdSF,TypeMessage,Message) VALUES (@date_Log,@ID_ProdSF,@typeMessage,@message)";

                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@date_Log", (object)DateTime.Now);
                cmd.Parameters.AddWithValue("@ID_ProdSF", (object)id_ProdSF.Trim());
                cmd.Parameters.AddWithValue("@typeMessage", (object)typeMessage.Trim());
                cmd.Parameters.AddWithValue("@message", (object)message.Trim());

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (con != null) con.Close();
            }
        }
    }
}