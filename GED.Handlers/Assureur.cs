using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace GED.Handlers
{
    public class Assureur
    {
        public int Id { get; set; }
        public String Nom { get; set; }
        public String CodeAssureur { get; set; }

        public Assureur() { }
        public Assureur(int id, string nom, string codeAssureur)
        {
            Id = id;
            Nom = nom;
            CodeAssureur = codeAssureur;
        }

        public static List<Assureur> Get()
        {
            var result = new List<Assureur>();

            var ocon = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);
            const string sql = @"SELECT [ID_Assureur]
                                        ,[Nom_Assureur]
                                        ,[Code_Assureur]
                                  FROM  [DWNortia].[dbo].[DIM_Assureurs]
                                  WHERE	ID_Assureur IS NOT NULL AND ID_Assureur <> ''
                                  ORDER BY  [Nom_Assureur] ";

            if (ocon.State != ConnectionState.Open) ocon.Open();
            var cmd = new SqlCommand(sql, ocon);

            var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                var id = int.Parse(dr["ID_Assureur"].ToString());
                var nom = (!string.IsNullOrEmpty(dr["Nom_Assureur"].ToString())) ? dr["Nom_Assureur"].ToString() : "";
                var codeAssureur = (!string.IsNullOrEmpty(dr["Code_Assureur"].ToString())) ? dr["Code_Assureur"].ToString() : "";

                result.Add(new Assureur(id, nom, codeAssureur));
            }
            dr.Close();
            ocon.Close();

            return result;
        }

    }
}
