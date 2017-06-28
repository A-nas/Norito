using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace GED.Handlers
{
    public class Apporteur
    {
        public int Id { get; set; }
        public String Nom { get; set; }
        public String CleSalesForce { get; set; }

        public Apporteur() { }
        public Apporteur(int id, string nom, string cleSalesForce)
        {
            Id = id;
            Nom = nom;
            CleSalesForce = cleSalesForce;
        }

        public static Apporteur Get(string cleSalesForce)
        {
            var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
            const string sql = @"SELECT  app.[PART_AGENCE_ID]
		                                    ,app.[PART_AGENCE_AGENCE]
		                                    ,app.[PART_AGENCE_CleSalesForce]
                                    FROM	[Nortiaca].[dbo].[PART_AGENCE] app
                                    WHERE	app.[PART_AGENCE_CleSalesForce] = @cleSalesForce
		                                    AND app.PART_AGENCE_APPORTEUR = 1";

            con.Open();
            var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@cleSalesForce", cleSalesForce);

            var dr = cmd.ExecuteReader();
            if (dr.HasRows)
            {
                dr.Read();

                var id = int.Parse(dr["PART_AGENCE_ID"].ToString());
                var nom = (!string.IsNullOrEmpty(dr["PART_AGENCE_AGENCE"].ToString())) ? dr["PART_AGENCE_AGENCE"].ToString() : "";

                dr.Close();
                return new Apporteur(id, nom, cleSalesForce);
            }

            return null;
        }

    }

    public class ApporteurAvenant
    {
        public int ID_Apporteur { get; set; }
        public string Nom { get; set; }
        public string CleSalesForce { get; set; }

        public override int GetHashCode()
        {
            if (ID_Apporteur == null) return 0;
            return ID_Apporteur.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ApporteurAvenant other = obj as ApporteurAvenant;
            return other != null && other.ID_Apporteur == this.ID_Apporteur;
        }

        public ApporteurAvenant()
        {
        }

        public ApporteurAvenant(int id, string nom)
        {
            ID_Apporteur=id;
            Nom = nom;
        }

        public static ApporteurAvenant GetApporteurId(int ID_Apporteur)
        {
            ApporteurAvenant app = new ApporteurAvenant();

            try
            {
                string sql = @"SELECT [INV_DIM_Conseiller].[Raison_Sociale],[INV_DIM_Conseiller].[CleSalesForce]
                                FROM [dbo].[INV_DIM_Conseiller]
                                WHERE [INV_DIM_Conseiller].[ID_Conseiller]=@ID";

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnDW_NI"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@ID", (object)ID_Apporteur ?? DBNull.Value);

                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            app.ID_Apporteur = ID_Apporteur;
                            app.Nom = dr["Raison_Sociale"].ToString();
                            app.CleSalesForce = dr["CleSalesForce"].ToString();
                        }
                        dr.Close();
                    }
                    con.Close();
                }

            }
            catch (Exception)
            {
                app = new ApporteurAvenant();
            }

            return app;
        }

        public List<ContactApporteur> GetContact(string role = "")
        {
            List<ContactApporteur> listContact = new List<ContactApporteur>();

            try
            {
                string sql = "";
                if (String.IsNullOrWhiteSpace(role))
                {
                     sql=@"SELECT [INV_DIM_Conseiller_Contact].[ID_Contact],[INV_DIM_Conseiller_Contact].[Nom_Contact],[INV_DIM_Conseiller_Contact].[Prenom_Contact],[INV_DIM_Conseiller_Contact].[Civilite_contact],[INV_DIM_Conseiller_Contact].[Email_Contact],[INV_DIM_Conseiller_Contact].[CleSalesForce]
                            FROM [dbo].[INV_DIM_Conseiller_Contact]
                            WHERE [INV_DIM_Conseiller_Contact].[ID_Conseiller]=@ID
                                AND [INV_DIM_Conseiller_Contact].[TEK_dateSuppression] is null";
                }
                else
                {
                    sql = @"SELECT [INV_DIM_Conseiller_Contact].[ID_Contact],[INV_DIM_Conseiller_Contact].[Nom_Contact],[INV_DIM_Conseiller_Contact].[Prenom_Contact],[INV_DIM_Conseiller_Contact].[Civilite_contact],[INV_DIM_Conseiller_Contact].[Email_Contact],[INV_DIM_Conseiller_Contact].[CleSalesForce]
                            FROM [dbo].[INV_DIM_Conseiller_Contact]
                                INNER JOIN [dbo].[INV_DIM_Conseiller_Contact_Role] ON [INV_DIM_Conseiller_Contact_Role].[ID_Contact]=[INV_DIM_Conseiller_Contact].[ID_Contact]
                                INNER JOIN [dbo].[INV_DIM_Role_Contact] ON [INV_DIM_Role_Contact].[ID_Role_Contact]=[INV_DIM_Conseiller_Contact_Role].[ID_Role_Contact]
                            WHERE [INV_DIM_Conseiller_Contact].[ID_Conseiller]=@ID
                                AND [INV_DIM_Role_Contact].[Nom_Role_Contact]=@role
                                AND [INV_DIM_Conseiller_Contact].[TEK_dateSuppression] is null";
                }

                 using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnDW_NI"].ConnectionString))
                 {
                     SqlCommand cmd = new SqlCommand(sql, con);
                     cmd.Parameters.AddWithValue("@ID", (object)ID_Apporteur ?? DBNull.Value);
                     if (!String.IsNullOrWhiteSpace(role))
                         cmd.Parameters.AddWithValue("@role", (object)role ?? DBNull.Value);

                     con.Open();
                     using (SqlDataReader dr = cmd.ExecuteReader())
                     {
                         while (dr.Read())
                         {
                            ContactApporteur ct = new ContactApporteur();

                            int ind = dr.GetOrdinal("ID_Contact");
                            if (dr.IsDBNull(ind))
                                ct.ID_Contact = 0;
                            else
                                ct.ID_Contact = dr.GetInt32(ind);

                            ct.Nom = dr["Nom_Contact"].ToString();
                            ct.Prenom = dr["Prenom_Contact"].ToString();
                            ct.Civilite = dr["Civilite_contact"].ToString();
                            ct.Email = dr["Email_Contact"].ToString();
                            ct.CleSalesForce = dr["CleSalesForce"].ToString();

                            listContact.Add(ct);
                         }
                         dr.Close();
                     }
                     con.Close();
                 }
            }
            catch (Exception)
            {
                listContact = new List<ContactApporteur>();
            }

            return listContact;
        }

        public class ContactApporteur
        {
            public int ID_Contact { get; set; }
            public string Nom { get; set; }
            public string Prenom { get; set; }
            public string Civilite { get; set; }
            public string Email { get; set; }
            public string CleSalesForce { get; set; }
        }
    }
}
