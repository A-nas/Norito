using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;

namespace GED.Handlers
{
    public class RepertoireGED
    {
        public int ID_Repertoire { get; set; }
        public string Nom { get; set; }
        public string Chemin { get; set; }
        public int ID_TypeSource { get; set; }
        public bool Original { get; set; }
        public int ID_File { get; set; }
        public string Categorie { get; set; }
        public bool EstActif { get; set; }

        public static List<RepertoireGED> ListeRepertoireAvenant(bool avecInactif = false)
        {
            List<RepertoireGED> listeRep = new List<RepertoireGED>();

            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString))
                {
                    string sql = @"SELECT ID_Repertoire, nom, Chemin, Original, ID_TypeSource, ID_File, EstActif
                        FROM Repertoire
                        WHERE Repertoire.Categorie='Avenant'";

                    if (!avecInactif)
                        sql += " AND  Repertoire.EstActif=1";

                    SqlCommand cmd = new SqlCommand(sql, con);

                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            RepertoireGED rep = new RepertoireGED();

                            rep.ID_Repertoire = Convert.ToInt32(dr["ID_Repertoire"].ToString());
                            rep.Nom = dr["nom"].ToString();
                            rep.Chemin = dr["Chemin"].ToString();
                            rep.ID_TypeSource = Convert.ToInt32(dr["ID_TypeSource"].ToString());
                            rep.Original = dr.GetBoolean(dr.GetOrdinal("Original"));
                            rep.ID_File = 0;
                            rep.Categorie = "Avenant";
                            rep.EstActif = dr.GetBoolean(dr.GetOrdinal("EstActif"));

                            listeRep.Add(rep);
                        }
                        dr.Close();
                    }
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                listeRep = new List<RepertoireGED>();
            }

            return listeRep;
        }
    }
}
