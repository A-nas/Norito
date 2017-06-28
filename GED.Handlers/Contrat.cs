using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace GED.Handlers
{
    public class Contrat
    {
        public int Id { get; set; }
        public String Num { get; set; }
        public DateTime DateCreation { get; set; }
        public string Souscripteur { get; set; }
        public string CleSalesForce { get; set; }

        public Contrat() { }
        public Contrat(int id, string num, DateTime date, string nom)
        {
            Id = id;
            Num = num;
            if (date != DateTime.MinValue) DateCreation = date;
            Souscripteur = nom;
        }

        public Contrat(int id, string num, DateTime date, string nom, string cleSalesForce)
        {
            Id = id;
            Num = num;
            if (date != DateTime.MinValue) DateCreation = date;
            Souscripteur = nom;
            CleSalesForce = cleSalesForce;
        }

        public static Contrat Get(int id)
        {
            var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
            const string sql = @"SELECT   c.PK
                                          ,c.DATECREATION
                                          ,c.FKAPPORTEUR
                                          ,c.NCONTRAT
                                          ,c.FKSOUSCRIPTEUR
                                          ,ISNULL(s.nom, '') + ' ' + ISNULL(s.prenom) AS nomcomplet
                                FROM	  Nortiaca.dbo.CA_CONTRAT c
		                                  LEFT OUTER JOIN Nortiaca.dbo.CA_SOUSCRIPTEUR s ON c.FKSOUSCRIPTEUR = s.pk       
                                WHERE     pk = @id";

            con.Open();
            var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;

            var dr = cmd.ExecuteReader();
            if (dr.HasRows)
            {
                dr.Read();
                var num = (!string.IsNullOrEmpty(dr["NCONTRAT"].ToString())) ? dr["NCONTRAT"].ToString() : "";
                var date = (!string.IsNullOrEmpty(dr["DATECREATION"].ToString())) ? DateTime.Parse(dr["DATECREATION"].ToString()) : DateTime.MinValue;
                var nom = (!string.IsNullOrEmpty(dr["nomcomplet"].ToString())) ? dr["nomcomplet"].ToString() : "";

                dr.Close();
                con.Close();

                return new Contrat(id, num, date, nom);
            }

            con.Close();

            return null;
        }

        public static List<Contrat> GetForApporteur(int id)
        {
            var result = new List<Contrat>();

            var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
            const string sql = @"SELECT   c.PK
                                          ,c.DATECREATION
                                          ,c.FKAPPORTEUR
                                          ,c.NCONTRAT
                                          ,c.FKSOUSCRIPTEUR
                                          ,ISNULL(s.nom, '') + ' ' + ISNULL(s.prenom, '') AS nomcomplet
                                          ,c.CleSalesForce
                                FROM	  Nortiaca.dbo.CA_CONTRAT c
		                                  LEFT OUTER JOIN Nortiaca.dbo.CA_SOUSCRIPTEUR s ON c.FKSOUSCRIPTEUR = s.pk 
                                WHERE     FKAPPORTEUR = @id AND c.CleSalesForce is not null";

            con.Open();
            var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;

            var dr = cmd.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    var num = (!string.IsNullOrEmpty(dr["NCONTRAT"].ToString())) ? dr["NCONTRAT"].ToString() : "";
                    var date = (!string.IsNullOrEmpty(dr["DATECREATION"].ToString())) ? DateTime.Parse(dr["DATECREATION"].ToString()) : DateTime.MinValue;
                    var nom = (!string.IsNullOrEmpty(dr["nomcomplet"].ToString())) ? dr["nomcomplet"].ToString() : "";
                    var idContrat = (!string.IsNullOrEmpty(dr["PK"].ToString())) ? int.Parse(dr["Pk"].ToString()) : 0;
                    var CleSalesForce = (!string.IsNullOrEmpty(dr["CleSalesForce"].ToString())) ? dr["CleSalesForce"].ToString() : "";

                    var contrat = new Contrat(idContrat, num, date, nom, CleSalesForce);

                    result.Add(contrat);
                }
            }

            con.Close();

            return result;
        }

        public static string FindIdContratSF(string numContrat, string entite = "NSAS")
        {
            string idSF = "";

            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnODS_SF"].ConnectionString))
                {
                    string sql = "";
                    if(entite == "NSAS")
                        sql="SELECT TOP 1 ID FROM NortiaContract__c WHERE [IsDeleted]='false' AND Name=@numContrat";
                    else
                        sql = "SELECT TOP 1 ID FROM [dbo].[Portefeuille__c] WHERE [IsDeleted]='false' AND Name=@numContrat";

                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@numContrat", (object)numContrat ?? DBNull.Value);

                    con.Open();
                    idSF = (cmd.ExecuteScalar() ?? "").ToString();
                    con.Close();
                }
            }
            catch (Exception)
            {
                idSF = "";
            }

            return idSF;
        }
    }

    public class ContratAvenant
    {
        public int Id { get; set; }
        public int ID_Apporteur { get; set; }
        public string NumContrat { get; set; }
        public string Libelle { get; set; }
        public string CleSalesForce { get; set; }

        public ContratAvenant()
        {
        }

         public static ContratAvenant GetContratIdSF(string ID_CompteSF)
         {
             ContratAvenant ct = new ContratAvenant();

             try
             {
                 string sql = @"SELECT [INV_DIM_Compte].[ID_Compte],[INV_DIM_Compte].[ID_Conseiller],[INV_DIM_Compte].[Num_Compte],[INV_DIM_Compte].[Libelle_Long]
                                FROM [dbo].[INV_DIM_Compte]
                                WHERE [INV_DIM_Compte].[CleSalesForce]=@IDSF";

                 using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnDW_NI"].ConnectionString))
                 {
                     SqlCommand cmd = new SqlCommand(sql, con);
                     cmd.Parameters.AddWithValue("@IDSF", (object)ID_CompteSF ?? DBNull.Value);

                     con.Open();
                     using (SqlDataReader dr = cmd.ExecuteReader())
                     {
                         if (dr.Read())
                         {
                             int ind = dr.GetOrdinal("ID_Compte");
                             if (dr.IsDBNull(ind))
                                 ct.ID_Apporteur = 0;
                             else
                                 ct.ID_Apporteur = dr.GetInt32(ind);

                             ind = dr.GetOrdinal("ID_Conseiller");
                             if (dr.IsDBNull(ind))
                                 ct.ID_Apporteur = 0;
                             else
                                 ct.ID_Apporteur = dr.GetInt32(ind);

                             ct.NumContrat = dr["Num_Compte"].ToString();

                             ct.Libelle = dr["Libelle_Long"].ToString();

                             ct.CleSalesForce = ID_CompteSF;
                         }
                         dr.Close();
                     }
                     con.Close();
                 }

             }
             catch (Exception)
             {
                 ct = new ContratAvenant();
             }

             return ct;
         }
    }
}
