using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using GED.Tools.wsdl_enterprise_dev;
using GED.Tools.wsdl_docacquisiton_dev;
using GED.Tools.wsdl_enterprise_qualif;
using GED.Tools.wsdl_docacquisiton_qualif;
using GED.Tools.wsdl_enterprise;
using GED.Tools.wsdl_docacquisiton;

namespace GED.Handlers
{
    public class IndexationPli
    {
        public int ID_PliNortia { get; set; }
        public string ID_PliSaleForce { get; set; }
    }

    public class Pli
    {
        public int ID_Pli {get; set;}
        public int ID_Type_Acte { get; set; }
        public string NomType { get; set; }
        public byte[] Datas { get; set; }
        public string Extension { get; set; }
        public string Nom { get; set; }
        public int NbPage { get; set; }
        public DateTime DateAcquisition { get; set; }
        public string LoginSaisie { get; set; }
        public int ID_TypeSource { get; set; }
        public String NomSource { get; set; }
        public int ID_Pli_Origine { get; set; }
        public bool Original { get; set; }
        public Dictionary<int, Document> Documents { get; set; }
        public DateTime DateDebutSaisie { get; set; }
        public DateTime DateFinSaisie { get; set; }
        public int ID_File { get; set; }
        public string CleSalesForce { get; set; }
        public bool EnvoyerSF { get; set; }

        public Pli()
        {
            Documents = new Dictionary<int, Document>();
        }

        public Pli(int idPli)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            SqlDataReader dr=null;

            try
            {
                Documents = new Dictionary<int, Document>();

                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = "select Pli.ID_Type_Acte,isNull(Type_Acte.Nom,'') as NomType,Extension,Pli.Nom,NbPage,DateAcquisition,LoginSaisie,ID_Pli_Origine,TypeSource.ID_TypeSource,isNull(TypeSource.nom,'') as NomSource,Original,DateDebutSaisie,DateFinSaisie,ID_File,CleSalesforce,EnvoyerSF from Pli"
                    + " LEFT JOIN Type_Acte ON Pli.ID_Type_Acte=Type_Acte.ID_Type_Acte"
                    + " LEFT JOIN TypeSource ON Pli.ID_TypeSource=TypeSource.ID_TypeSource"
                + " where ID_Pli = @ID_Pli";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@ID_Pli", (object)idPli ?? DBNull.Value);

                dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    ID_Pli = idPli;

                    int ind = dr.GetOrdinal("ID_Type_Acte");
                    if (dr.IsDBNull(ind))
                        ID_Type_Acte = 0;
                    else
                        ID_Type_Acte = dr.GetInt32(ind);

                    ind = dr.GetOrdinal("ID_TypeSource");
                    if (dr.IsDBNull(ind))
                        ID_TypeSource = 0;
                    else
                        ID_TypeSource = dr.GetInt32(ind);

                    ind = dr.GetOrdinal("ID_File");
                    if (dr.IsDBNull(ind))
                        ID_File = 0;
                    else
                        ID_File = dr.GetInt32(ind);

                    NomType = dr["NomType"].ToString();
                    Extension = dr["Extension"].ToString();
                    Nom = dr["Nom"].ToString();
                    NbPage = dr.GetInt32(dr.GetOrdinal("NbPage"));

                    if(!dr.IsDBNull(dr.GetOrdinal("DateAcquisition")))
                        DateAcquisition=dr.GetDateTime(dr.GetOrdinal("DateAcquisition"));

                    NomSource = dr["NomSource"].ToString();

                    LoginSaisie = dr["LoginSaisie"].ToString();

                    ind = dr.GetOrdinal("ID_Pli_Origine");
                    if (dr.IsDBNull(ind))
                        ID_Pli_Origine = 0;
                    else
                        ID_Pli_Origine = dr.GetInt32(ind);

                    if (!dr.IsDBNull(dr.GetOrdinal("DateDebutSaisie")))
                        DateDebutSaisie = dr.GetDateTime(dr.GetOrdinal("DateDebutSaisie"));

                    if (!dr.IsDBNull(dr.GetOrdinal("DateFinSaisie")))
                        DateFinSaisie = dr.GetDateTime(dr.GetOrdinal("DateFinSaisie"));

                    ind = dr.GetOrdinal("Original");
                    if (dr.IsDBNull(ind))
                        Original = false;
                    else
                        Original = dr.GetBoolean(ind);

                    /*Object tmpDatas = dr["Datas"];
                    int taille = tmpDatas.ToString().Length;
                    Datas = new byte[taille];
                    Datas = (byte[])tmpDatas;
                    //Datas = (byte[])dr["Datas"];*/

                    CleSalesForce = dr["CleSalesForce"].ToString();

                    ind = dr.GetOrdinal("EnvoyerSF");
                    if (dr.IsDBNull(ind))
                        EnvoyerSF = false;
                    else
                        EnvoyerSF = dr.GetBoolean(ind);

                    dr.Close();

                    Datas = GetData(ID_Pli);

                    ChargerDocuments(ID_Pli);
                }
                else
                    throw new Exception("Impossible de trouver les informations sur ce pli");

                dr.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if(con!=null)
                    con.Close();
            }
        }

        public int Ajouter(string entite = "NSAS")
        {
            int idPli = 0;

            try
            {
                string strSQL = @"INSERT INTO Pli(ID_Type_Acte,Extension,Nom,NbPage,DateAcquisition,ID_TypeSource,Original,CleSalesForce,ID_File)
                    OUTPUT INSERTED.ID_Pli
                    VALUES(@ID_Type_Acte,@Extension,@Nom,@NbPage,@DateAcquisition,@ID_TypeSource,@Original,@CleSalesForce,@ID_File)";

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand(strSQL, con);

                    cmd.Parameters.AddWithValue("@ID_Type_Acte", (object)ID_Type_Acte ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Extension", (object)Extension ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nom", (object)Nom ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NbPage", (object)NbPage ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateAcquisition", (object)DateAcquisition ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ID_TypeSource", (object)ID_TypeSource ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Original", (object)Original ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ID_File", (object)ID_File ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CleSalesForce", (object)CleSalesForce ?? DBNull.Value);

                    con.Open();
                    idPli = (Int32)cmd.ExecuteScalar();
                    con.Close();
                }
            }
            catch (Exception)
            {
                idPli = 0;
                throw;
            }

            return idPli;
        }

        public byte[] GetData(int idPli)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            byte[] datas = null;

            try
            {
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = "select Datas from Pli where ID_Pli = @ID_Pli";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.Add("@ID_Pli", SqlDbType.Int).Value = ID_Pli;

                int taille = cmd.ExecuteScalar().ToString().Length;
                datas = new byte[taille];
                datas = (byte[])cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return datas;
        }

        private int ChargerDocuments(int idPli)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            SqlDataReader dr = null;

            try
            {
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = "select pk from dbo.CA_MEDIA"
                    + " where ID_Pli = @ID_Pli";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@ID_Pli", (object)idPli ?? DBNull.Value);

                dr = cmd.ExecuteReader();

                int idDoc=0;
                
                while(dr.Read())
                {
                    int ind = dr.GetOrdinal("pk");
                    if (dr.IsDBNull(ind))
                        idDoc = 0;
                    else
                        idDoc = dr.GetInt32(ind);

                    if (idDoc != 0)
                    {
                        Document leDoc=new Document(idDoc);
                        AjouterDocument(leDoc);
                    }
                }
                dr.Close();
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return Documents.Count;
        }

        public static int Indexer(int ID_Pli, string CleSalesForce)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            int retour;

            try
            {
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = "UPDATE Pli SET CleSalesForce = @CleSalesForce where ID_Pli = @ID_Pli";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@CleSalesForce", (object)CleSalesForce ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Pli", (object)ID_Pli ?? DBNull.Value);

                int nbLigne = cmd.ExecuteNonQuery();

                if (nbLigne != 0)
                {
                    retour = 0;
                }
                else
                {
                    retour = 2;
                }
            }
            catch (Exception ex)
            {
                retour = 1;
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return retour;
        }
        
        public bool EnvoyerSalesforce(GED.Tools.LDAPUser ldapUser)
        {
            bool retour = false;

            try
            {
                string loginSF = ConfigurationManager.AppSettings["loginSF"];
                string mdpSF = ConfigurationManager.AppSettings["mdpSF"];
                string envSF = ConfigurationManager.AppSettings["EnvironementSF"];
                string entite = ConfigurationManager.AppSettings["Entite"];
                string sIdSF = "";

                //Authentification
                if (envSF == "DEV")
                {
                    Tools.wsdl_enterprise_dev.SforceService bdAuth = new Tools.wsdl_enterprise_dev.SforceService();
                    Tools.wsdl_enterprise_dev.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                    sIdSF = lr.sessionId;
                }
                else if (envSF == "QUALIF")
                {
                    Tools.wsdl_enterprise_qualif.SforceService bdAuth = new Tools.wsdl_enterprise_qualif.SforceService();
                    Tools.wsdl_enterprise_qualif.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                    sIdSF = lr.sessionId;
                }
                else if (envSF == "DEMO")
                {
                    Tools.wsdl_enterprise_demo.SforceService bdAuth = new Tools.wsdl_enterprise_demo.SforceService();
                    Tools.wsdl_enterprise_demo.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                    sIdSF = lr.sessionId;
                }
                else if (envSF == "PROD")
                {
                    Tools.wsdl_enterprise.SforceService bdAuth = new Tools.wsdl_enterprise.SforceService();
                    Tools.wsdl_enterprise.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                    sIdSF = lr.sessionId;
                }



                //objet à envoyer
                if (envSF == "DEV")
                {
                    Tools.wsdl_docacquisiton_dev.DocAcquisitionInput docInput = new Tools.wsdl_docacquisiton_dev.DocAcquisitionInput();
                    //informations sur le pli
                    Tools.wsdl_docacquisiton_dev.Pli pliSF = new Tools.wsdl_docacquisiton_dev.Pli();
                    pliSF.PliReferenceId = ID_Pli.ToString();
                    pliSF.NomPli = Nom;
                    pliSF.origin = NomSource;
                    pliSF.DateAcquisition = DateAcquisition.ToUniversalTime();
                    pliSF.DateAcquisitionSpecified = true;
                    pliSF.userEmail = ldapUser.Mail;
                    pliSF.dateSaisie = DateDebutSaisie.ToUniversalTime();
                    pliSF.dateSaisieSpecified = true;
                    if (Original)
                        pliSF.OriginalCopie = "Original";
                    else
                        pliSF.OriginalCopie = "Copie";
                    pliSF.Type = NomType;
                    pliSF.entity = entite;
                    docInput.PliGED = pliSF;

                    //information sur les docs
                    docInput.DocsGED = new Tools.wsdl_docacquisiton_dev.DocumentGED[Documents.Count];
                    int i = 0;
                    foreach (GED.Handlers.Document leDoc in Documents.Values)
                    {
                        if (leDoc.Type_EnvoyerSF)
                        {
                            Tools.wsdl_docacquisiton_dev.DocumentGED docSF = new Tools.wsdl_docacquisiton_dev.DocumentGED();
                            docSF.DocReferenceId = leDoc.ID_Document.ToString();
                            docSF.nom = leDoc.Nom;
                            docSF.RefPliId = leDoc.ID_Pli.ToString();
                            if (leDoc.Original)
                                docSF.OriginalCopie = "Original";
                            else
                                docSF.OriginalCopie = "Copie";
                            docSF.OriginalCopie = pliSF.OriginalCopie;
                            docSF.Type = leDoc.NomType;

                            docSF.NbrePages = leDoc.NbPage;
                            docSF.NbrePagesSpecified = true;

                            docInput.DocsGED[i] = docSF;
                            i++;
                        }
                    }

                    //consomation du web service SF
                    Tools.wsdl_docacquisiton_dev.WS01DocumentAcquisitionService bdGED = new Tools.wsdl_docacquisiton_dev.WS01DocumentAcquisitionService();
                    bdGED.SessionHeaderValue = new Tools.wsdl_docacquisiton_dev.SessionHeader();
                    bdGED.SessionHeaderValue.sessionId = sIdSF;

                    Tools.wsdl_docacquisiton_dev.DocAcquisitionOutput docOutput = bdGED.DocAcquisition(docInput);

                    if (docOutput.ReturnCode != 0)//erreur
                    {
                        throw new Exception(docOutput.Error);
                    }

                    retour = true;
                }
                else if (envSF == "QUALIF")
                {
                    Tools.wsdl_docacquisiton_qualif.DocAcquisitionInput docInput = new Tools.wsdl_docacquisiton_qualif.DocAcquisitionInput();

                    //informations sur le pli
                    Tools.wsdl_docacquisiton_qualif.Pli pliSF = new Tools.wsdl_docacquisiton_qualif.Pli();
                    pliSF.PliReferenceId = ID_Pli.ToString();
                    pliSF.NomPli = Nom;
                    pliSF.origin = NomSource;
                    pliSF.DateAcquisition = DateAcquisition.ToUniversalTime();
                    pliSF.DateAcquisitionSpecified = true;
                    pliSF.userEmail = ldapUser.Mail;
                    pliSF.dateSaisie = DateDebutSaisie.ToUniversalTime();
                    pliSF.dateSaisieSpecified = true;
                    if (Original)
                        pliSF.OriginalCopie = "Original";
                    else
                        pliSF.OriginalCopie = "Copie";
                    pliSF.Type = NomType;
                    pliSF.entity = entite;
                    docInput.PliGED = pliSF;

                    //information sur les docs
                    docInput.DocsGED = new Tools.wsdl_docacquisiton_qualif.DocumentGED[Documents.Count];
                    int i = 0;
                    foreach (GED.Handlers.Document leDoc in Documents.Values)
                    {
                        if (leDoc.Type_EnvoyerSF)
                        {
                            Tools.wsdl_docacquisiton_qualif.DocumentGED docSF = new Tools.wsdl_docacquisiton_qualif.DocumentGED();
                            docSF.DocReferenceId = leDoc.ID_Document.ToString();
                            docSF.nom = leDoc.Nom;
                            docSF.RefPliId = leDoc.ID_Pli.ToString();
                            if (leDoc.Original)
                                docSF.OriginalCopie = "Original";
                            else
                                docSF.OriginalCopie = "Copie";
                            docSF.Type = leDoc.NomType;

                            docSF.NbrePages = leDoc.NbPage;
                            docSF.NbrePagesSpecified = true;

                            docInput.DocsGED[i] = docSF;
                            i++;
                        }
                    }

                    //consomation du web service SF
                    Tools.wsdl_docacquisiton_qualif.WS01DocumentAcquisitionService bdGED = new Tools.wsdl_docacquisiton_qualif.WS01DocumentAcquisitionService();
                    bdGED.SessionHeaderValue = new Tools.wsdl_docacquisiton_qualif.SessionHeader();
                    bdGED.SessionHeaderValue.sessionId = sIdSF;

                    Tools.wsdl_docacquisiton_qualif.DocAcquisitionOutput docOutput = bdGED.DocAcquisition(docInput);

                    if (docOutput.ReturnCode != 0)//erreur
                    {
                        throw new Exception(docOutput.Error);
                    }

                    retour = true;
                }
                else if (envSF == "DEMO")
                {
                    Tools.wsdl_docacquisiton_demo.DocAcquisitionInput docInput = new Tools.wsdl_docacquisiton_demo.DocAcquisitionInput();

                    //informations sur le pli
                    Tools.wsdl_docacquisiton_demo.Pli pliSF = new Tools.wsdl_docacquisiton_demo.Pli();
                    pliSF.PliReferenceId = ID_Pli.ToString();
                    pliSF.NomPli = Nom;
                    pliSF.origin = NomSource;
                    pliSF.DateAcquisition = DateAcquisition.ToUniversalTime();
                    pliSF.DateAcquisitionSpecified = true;
                    pliSF.userEmail = ldapUser.Mail;
                    pliSF.dateSaisie = DateDebutSaisie.ToUniversalTime();
                    pliSF.dateSaisieSpecified = true;
                    if (Original)
                        pliSF.OriginalCopie = "Original";
                    else
                        pliSF.OriginalCopie = "Copie";
                    pliSF.Type = NomType;
                    pliSF.entity = entite;
                    docInput.PliGED = pliSF;

                    //information sur les docs
                    docInput.DocsGED = new Tools.wsdl_docacquisiton_demo.DocumentGED[Documents.Count];
                    int i = 0;
                    foreach (GED.Handlers.Document leDoc in Documents.Values)
                    {
                        if (leDoc.Type_EnvoyerSF)
                        {
                            Tools.wsdl_docacquisiton_demo.DocumentGED docSF = new Tools.wsdl_docacquisiton_demo.DocumentGED();
                            docSF.DocReferenceId = leDoc.ID_Document.ToString();
                            docSF.nom = leDoc.Nom;
                            docSF.RefPliId = leDoc.ID_Pli.ToString();
                            if (leDoc.Original)
                                docSF.OriginalCopie = "Original";
                            else
                                docSF.OriginalCopie = "Copie";
                            docSF.Type = leDoc.NomType;

                            docSF.NbrePages = leDoc.NbPage;
                            docSF.NbrePagesSpecified = true;

                            docInput.DocsGED[i] = docSF;
                            i++;
                        }
                    }

                    //consomation du web service SF
                    Tools.wsdl_docacquisiton_demo.WS01DocumentAcquisitionService bdGED = new Tools.wsdl_docacquisiton_demo.WS01DocumentAcquisitionService();
                    bdGED.SessionHeaderValue = new Tools.wsdl_docacquisiton_demo.SessionHeader();
                    bdGED.SessionHeaderValue.sessionId = sIdSF;

                    Tools.wsdl_docacquisiton_demo.DocAcquisitionOutput docOutput = bdGED.DocAcquisition(docInput);

                    if (docOutput.ReturnCode != 0)//erreur
                    {
                        throw new Exception(docOutput.Error);
                    }

                    retour = true;
                }
                else if (envSF == "PROD")
                {
                    Tools.wsdl_docacquisiton.DocAcquisitionInput docInput = new Tools.wsdl_docacquisiton.DocAcquisitionInput();

                    //informations sur le pli
                    Tools.wsdl_docacquisiton.Pli pliSF = new Tools.wsdl_docacquisiton.Pli();
                    pliSF.PliReferenceId = ID_Pli.ToString();
                    pliSF.NomPli = Nom;
                    pliSF.origin = NomSource;
                    pliSF.DateAcquisition = DateAcquisition.ToUniversalTime();
                    pliSF.DateAcquisitionSpecified = true;
                    pliSF.userEmail = ldapUser.Mail;
                    pliSF.dateSaisie = DateDebutSaisie.ToUniversalTime();
                    pliSF.dateSaisieSpecified = true;
                    if (Original)
                        pliSF.OriginalCopie = "Original";
                    else
                        pliSF.OriginalCopie = "Copie";
                    pliSF.Type = NomType;
                    pliSF.entity = entite;
                    docInput.PliGED = pliSF;

                    //information sur les docs
                    docInput.DocsGED = new Tools.wsdl_docacquisiton.DocumentGED[Documents.Count];
                    int i = 0;
                    foreach (GED.Handlers.Document leDoc in Documents.Values)
                    {
                        if (leDoc.Type_EnvoyerSF)
                        {
                            Tools.wsdl_docacquisiton.DocumentGED docSF = new Tools.wsdl_docacquisiton.DocumentGED();
                            docSF.DocReferenceId = leDoc.ID_Document.ToString();
                            docSF.nom = leDoc.Nom;
                            docSF.RefPliId = leDoc.ID_Pli.ToString();
                            if (leDoc.Original)
                                docSF.OriginalCopie = "Original";
                            else
                                docSF.OriginalCopie = "Copie";
                            docSF.Type = leDoc.NomType;

                            docSF.NbrePages = leDoc.NbPage;
                            docSF.NbrePagesSpecified = true;

                            docInput.DocsGED[i] = docSF;
                            i++;
                        }
                    }

                    //consomation du web service SF
                    Tools.wsdl_docacquisiton.WS01DocumentAcquisitionService bdGED = new Tools.wsdl_docacquisiton.WS01DocumentAcquisitionService();
                    bdGED.SessionHeaderValue = new Tools.wsdl_docacquisiton.SessionHeader();
                    bdGED.SessionHeaderValue.sessionId = sIdSF;

                    Tools.wsdl_docacquisiton.DocAcquisitionOutput docOutput = bdGED.DocAcquisition(docInput);

                    if (docOutput.ReturnCode != 0)//erreur
                    {
                        throw new Exception(docOutput.Error);
                    }

                    retour = true;
                }
                else
                    retour = false;
            }
            catch (Exception ex)
            {
                retour = false;
            }
            finally
            {
            }

            return retour;
        }

        public static Pli CreerPli(Document doc, DateTime dateAcq, int idTypeSource, int idFile, bool original)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            Pli lePli = null;

            try
            {
                lePli = new Pli();

                lePli.Datas = doc.Datas;
                lePli.Extension = doc.Extension;
                lePli.Nom = doc.Nom;
                lePli.NbPage = doc.NbPage;
                lePli.ID_Pli_Origine = doc.ID_Pli;
                lePli.DateAcquisition = dateAcq;
                lePli.ID_TypeSource = idTypeSource;
                lePli.ID_File = idFile;
                lePli.Original = original;

                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = "INSERT INTO [Pli] ([datas],[extension],[nom],[nbPage],[DateAcquisition],[ID_TypeSource],[Original],[ID_File],[ID_Pli_Origine])"
                    + " VALUES(@datas,@extension,@nom,@nbPages,@DateAcquisition,@ID_TypeSource,@Original,@ID_File,@ID_Pli_Origine);"
                    + "SELECT CAST(SCOPE_IDENTITY() AS int)";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@datas", (object)lePli.Datas ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@extension", (object)lePli.Extension ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nom", (object)lePli.Nom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nbPages", (object)lePli.NbPage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateAcquisition", (object)lePli.DateAcquisition ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_TypeSource", (object)lePli.ID_TypeSource ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_File", (object)lePli.ID_File ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Original", (object)lePli.Original ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Pli_Origine", (object)lePli.ID_Pli_Origine ?? DBNull.Value);

                lePli.ID_Pli = (Int32)cmd.ExecuteScalar(); 

                return lePli;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {

            }


        }

        public static Pli CreerPli(Pli pli)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            Pli lePli = null;

            try
            {
                lePli = new Pli();

                lePli.Datas = pli.Datas;
                lePli.Extension = pli.Extension;
                lePli.Nom = pli.Nom;
                lePli.NbPage = pli.NbPage;
                lePli.ID_Pli_Origine = pli.ID_Pli;
                lePli.DateAcquisition = pli.DateAcquisition;
                lePli.ID_TypeSource = pli.ID_TypeSource;
                lePli.ID_File = pli.ID_File;
                lePli.Original = pli.Original;

                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = "INSERT INTO [Pli] ([datas],[extension],[nom],[nbPage],[DateAcquisition],[ID_TypeSource],[Original],[ID_File],[ID_Pli_Origine])"
                    + " VALUES(@datas,@extension,@nom,@nbPages,@DateAcquisition,@ID_TypeSource,@Original,@ID_File,@ID_Pli_Origine);"
                    + "SELECT CAST(SCOPE_IDENTITY() AS int)";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@datas", (object)lePli.Datas ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@extension", (object)lePli.Extension ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nom", (object)lePli.Nom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nbPages", (object)lePli.NbPage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateAcquisition", (object)lePli.DateAcquisition ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_TypeSource", (object)lePli.ID_TypeSource ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_File", (object)lePli.ID_File ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Original", (object)lePli.Original ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Pli_Origine", (object)lePli.ID_Pli_Origine ?? DBNull.Value);

                lePli.ID_Pli = (Int32)cmd.ExecuteScalar();

                return lePli;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {

            }


        }

        public void AjouterDocument(Document leDoc)
        {
            Documents.Add(leDoc.ID_Document, leDoc);
        }

        public void AffecterTypeActe(int id_TypeActe,string nomType="")
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;

            try
            {
                DateTime dateFinSaisie = new DateTime();
                dateFinSaisie = DateTime.Now;

                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = "UPDATE Pli SET ID_Type_Acte = @ID_Type_Acte, DateFinSaisie = @DateFinSaisie where ID_Pli = @ID_Pli";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@ID_Type_Acte", (object)id_TypeActe ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateFinSaisie", (object)dateFinSaisie ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Pli", (object)ID_Pli ?? DBNull.Value);

                int nbLigne = cmd.ExecuteNonQuery();

                if (nbLigne != 0)
                {
                    ID_Type_Acte = id_TypeActe;
                    NomType = nomType;
                    DateFinSaisie = dateFinSaisie;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null)
                    con.Close();
            }
        }

        public bool Modifier()
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            bool retour=false;

            try
            {
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = @"UPDATE Pli SET ID_Type_Acte=@ID_Type_Acte,Extension=@Extension,Nom=@Nom,NbPage=@NbPage,DateAcquisition=@DateAcquisition,
                LoginSaisie=@LoginSaisie,DateDebutSaisie=@DateDebutSaisie,DateFinSaisie=@DateFinSaisie,Original=@Original,
                ID_TypeSource=@ID_TypeSource,ID_Pli_Origine=@ID_Pli_Origine,ID_File=@ID_File,CleSalesForce = @CleSalesForce,EnvoyerSF=@EnvoyerSF
                where ID_Pli = @ID_Pli";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@ID_Type_Acte", (object)ID_Type_Acte ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Extension", (object)Extension ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Nom", (object)Nom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NbPage", (object)NbPage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateAcquisition", (object)DateAcquisition ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LoginSaisie", (object)LoginSaisie ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateDebutSaisie", (object)DateDebutSaisie ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateFinSaisie", (object)DateFinSaisie ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Original", (object)Original ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_TypeSource", (object)ID_TypeSource ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Pli_Origine", (object)ID_Pli_Origine ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_File", (object)ID_File ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CleSalesForce", (object)CleSalesForce ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EnvoyerSF", (object)EnvoyerSF ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Pli", (object)ID_Pli ?? DBNull.Value);

                int nbLigne = cmd.ExecuteNonQuery();
                if (nbLigne == 0)
                    throw new Exception("Impossible de modifier le Pli");

                retour = true;
            }
            catch (Exception ex)
            {
                retour = false;
            }

            return retour;
        }
    }
}
