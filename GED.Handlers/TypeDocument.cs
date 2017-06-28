using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace GED.Handlers
{
    public class TypeDocument
    {
        public int ID_Type_Document { get; set; }
        public string Nom { get; set; }
        public int Ordre { get; set; }
        public bool GenereUnPli { get; set; }
        public bool EnvoyerSF { get; set; }
        public string Categorie { get; set; }
        public int ID_Type_Document_CA { get; set; }

        public TypeDocument()
        {
        }

        public TypeDocument(int id, string nom, bool genereUnPli, bool envoyerSf, string categorie, int ordre)
        {
            ID_Type_Document = id;
            Nom = nom;
            GenereUnPli = genereUnPli;
            EnvoyerSF = envoyerSf;
            Categorie = categorie;
            Ordre = ordre;
        }

        public TypeDocument(int idTypeDoc, string entite = "NSAS")
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            SqlDataReader dr = null;

            try
            {
                if(entite == "NSAS")
                    con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;");
                else
                    con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;");

                strSQL = @"SELECT Nom, Ordre, GenereUnPli, EnvoyerSF, Categorie,ID_Type_Doc_CA
                            FROM Type_Document
                            WHERE ID_Type_Document = @idTypeDoc";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@idTypeDoc", (object)idTypeDoc ?? DBNull.Value);

                dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    ID_Type_Document = idTypeDoc;
                    Nom = dr["Nom"].ToString();

                    int ind = dr.GetOrdinal("Ordre");
                    if (dr.IsDBNull(ind))
                        Ordre = 0;
                    else
                        Ordre = dr.GetInt32(ind);

                    ind = dr.GetOrdinal("GenereUnPli");
                    if (dr.IsDBNull(ind))
                        GenereUnPli = false;
                    else
                        GenereUnPli = dr.GetBoolean(ind);

                    ind = dr.GetOrdinal("EnvoyerSF");
                    if (dr.IsDBNull(ind))
                        EnvoyerSF = false;
                    else
                        EnvoyerSF = dr.GetBoolean(ind);

                    Categorie = dr["Categorie"].ToString();

                    ind = dr.GetOrdinal("ID_Type_Doc_CA");
                    if (dr.IsDBNull(ind))
                        ID_Type_Document_CA = 0;
                    else
                        ID_Type_Document_CA = dr.GetInt32(ind);

                    dr.Close();
                }
                else
                    throw new Exception();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }
        }


        //focntion qui renvoie le type de docuement
        public static int GetIDTypeDocumentCompagnie(string codeCompagnie, string typeDocCompagnie, string entite = "NSAS")
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            string strSQL = "";
            SqlDataReader dr = null;

            int ID_TypeDocument=0;

            try
            {
                string strCon = "";
                if (entite == "NSAS")
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;";//ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString;
                else
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;"; //ConfigurationManager.ConnectionStrings["dsnGED_NI"].ConnectionString;

                con = new SqlConnection(strCon);
                strSQL = "select ID_Type_Document FROM dbo.Type_Document_Compagnie"
                + " where CodeCompagnie = @CodeCompagnie AND NomType_Comapagnie=@NomType_Comapagnie";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@CodeCompagnie", (object)codeCompagnie ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NomType_Comapagnie", (object)typeDocCompagnie ?? DBNull.Value);

                dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    int ind = dr.GetOrdinal("ID_Type_Document");
                    if (dr.IsDBNull(ind))
                        ID_TypeDocument = 0;
                    else
                        ID_TypeDocument = dr.GetInt32(ind);
                }
                else
                    ID_TypeDocument = -2;

                dr.Close();
            }
            catch (Exception ex)
            {
                ID_TypeDocument = -3;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            return ID_TypeDocument;
        }

        public static int GetIDTypeActe(string libelle, string entite = "NSAS")
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            string strSQL = "";
            SqlDataReader dr = null;

            int ID_TypeActe = 0;

            try
            {
                if (entite == "NSAS")
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                else
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED_NI"].ConnectionString);


                strSQL = "select ID_Type_Acte FROM dbo.[Type_Acte]"
                + " where nom=@libelle";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@libelle", (object)libelle ?? DBNull.Value);

                dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    int ind = dr.GetOrdinal("ID_Type_Acte");
                    if (dr.IsDBNull(ind))
                        ID_TypeActe = 0;
                    else
                        ID_TypeActe = dr.GetInt32(ind);
                }


                dr.Close();
            }
            catch (Exception ex)
            {
                ID_TypeActe = 0;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            return ID_TypeActe;
        }

        public static int GetIDTypeDocument(string libelle, string entite = "NSAS")
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            string strSQL = "";
            SqlDataReader dr = null;

            int ID_TypeDocument = 0;

            try
            {
                if (entite == "NSAS")
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                else
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED_NI"].ConnectionString);


                strSQL = "select ID_Type_Document FROM dbo.[Type_Document]"
                + " where Nom=@libelle";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@libelle", (object)libelle ?? DBNull.Value);

                dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    int ind = dr.GetOrdinal("ID_Type_Document");
                    if (dr.IsDBNull(ind))
                        ID_TypeDocument = 0;
                    else
                        ID_TypeDocument = dr.GetInt32(ind);
                }


                dr.Close();
            }
            catch (Exception ex)
            {
                ID_TypeDocument = 0;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            return ID_TypeDocument;
        }

        public static List<TypeDocument> GetAllAvenants()
        {
            var result = new List<TypeDocument>();
            var ocon = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);

            if (ocon.State != ConnectionState.Open) ocon.Open();

            var sql = @"SELECT *
                        FROM   [Nortiaca_MEDIA].[dbo].[Type_Document]
                        WHERE Nom LIKE '%Avenant%'
                        ORDER BY Ordre";

            var cmd = new SqlCommand(sql, ocon);
            var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                var td = new TypeDocument
                {
                    ID_Type_Document = int.Parse(dr["ID_Type_Document"].ToString()),
                    Categorie = (!string.IsNullOrEmpty(dr["Categorie"].ToString()) ? dr["Categorie"].ToString() : ""),
                    EnvoyerSF = dr["EnvoyerSF"].ToString() == "1",
                    GenereUnPli = dr["GenereUnPli"].ToString() == "1",
                    Nom = dr["Nom"].ToString(),
                    Ordre = (!string.IsNullOrEmpty(dr["Ordre"].ToString()) ? int.Parse(dr["Ordre"].ToString()) : 0),
                };
                result.Add(td);
            }


            dr.Close();
            ocon.Close();

            return result;
        } 
    }
}
