using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;

namespace GED.Handlers
{
    public class RejetAvenant
    {
        public int ID_RejetAvenant { get; set; }
        public DateTime DateRejet { get; set; }
        public string Source { get; set; }
        public string NomFichier { get; set; }
        public string TypeRejet { get; set; }
        public string Message { get; set; }
        public bool RapportEnvoye { get; set; }


        public static bool Ajouter(string source, string nomFichier, string typeRejet, string message)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            bool retour=false;

            try
            {
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = @"INSERT INTO [RejetAvenant] (DateRejet,Source,NomFichier,TypeRejet,Message,RapportEnvoye)
                           VALUES(@DateRejet,@Source,@NomFichier,@TypeRejet,@Message,0)";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@DateRejet", (object)DateTime.Now ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Source", (object)source ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NomFichier", (object)nomFichier ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TypeRejet", (object)typeRejet ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Message", (object)message ?? DBNull.Value);

                int nbLigne = cmd.ExecuteNonQuery();

                if (nbLigne != 0)
                    retour = true;
                else
                    retour = false;
            }
            catch (Exception ex)
            {
                retour = false;
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return retour;
        }

        public static bool Existe(string source, string nomFichier, string typeRejet)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            bool retour = false;

            try
            {
                con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;"); //ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString
                strSQL = @"SELECT count(ID_RejetAvenant)
                            FROM RejetAvenant 
                            WHERE Source = @Source
                            AND NomFichier = @NomFichier
                            AND TypeRejet = @TypeRejet";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@Source", (object)source ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NomFichier", (object)nomFichier ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TypeRejet", (object)typeRejet ?? DBNull.Value);

                int nbLigne = (Int32)cmd.ExecuteScalar();

                if (nbLigne != 0)
                    retour = true;
                else
                    retour = false;
            }
            catch (Exception ex)
            {
                retour = false;
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return retour;
        }
    }
}
