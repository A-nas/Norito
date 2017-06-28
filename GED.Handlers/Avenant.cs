using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Web;
using Ionic.Zip;
using iTextSharp.text.pdf;

namespace GED.Handlers
{
    public class Avenant
    {
        public int ID_Document { get; set; }
        public string CodeCompagnie { get; set; }
        public string NumAvenant { get; set; }
        public string TypeAvenant { get; set; }
        public string CodeApporteur { get; set; }
        public string NomApporteur { get; set; }
        public string NumContrat { get; set; }
        public string NomSouscripteur { get; set; }
        public DateTime DateAvenant { get; set; }
        public int NbPage { get; set; }
        public bool Actif { get; set; }
        public string ReferenceInterne { get; set; }
        public string NumLot { get; set; }

        public int Ajouter(string cheminFichierPDF, bool envoyerSF = false, string nomFichierNew = "", string entite = "NSAS")
        {
            SqlConnection con = null;
            //SqlTransaction trans=null;
            //string strSQLMedia = "";
            string strSQLAvenant = "";
            //SqlCommand cmdMedia = null;
            SqlCommand cmdAvenant = null;
            FileStream fs;
            Byte[] datas;

            ID_Document = 0;

            try
            {
                string typeAvenantDefaut = "A définir";//ConfigurationManager.AppSettings["TypeAvenantDefaut"];
                // lecture du ficheir pdf
                fs = File.Open(cheminFichierPDF, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                if (fs != null)
                {
                    fs.Position = 0;
                    datas = new BinaryReader(fs).ReadBytes(int.Parse(fs.Length.ToString()));
                    fs.Close();
                }
                else
                    throw new Exception();
                // nommer le fichier
                string nomFichier = "";
                if (nomFichierNew == "")
                    nomFichier = Path.GetFileNameWithoutExtension(cheminFichierPDF);
                else
                    nomFichier = nomFichierNew;
                // lire le nombre de pages
                PdfReader pdfReader = new PdfReader(cheminFichierPDF);
                NbPage = pdfReader.NumberOfPages;
                pdfReader.Close();
                // aller chercher le type de document ICI JE DOIS AGIR #########################
                int ID_Type_Document = TypeDocument.GetIDTypeDocumentCompagnie(CodeCompagnie, TypeAvenant, entite);
                if(ID_Type_Document>0) //type existant
                {
                    TypeDocument tpDoc = new TypeDocument(ID_Type_Document, entite);

                    //trans = con.BeginTransaction("GEDAjoutAvenant");
                    //System.Transactions.TransactionScope scope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew);

                    //insertion CA_Media
                    Document docAvenant = new Document();
                    docAvenant.Datas = datas;
                    docAvenant.Extension =".pdf";
                    docAvenant.Nom =nomFichier;
                    docAvenant.NbPage =NbPage;
                    docAvenant.ID_Pli =0;
                    docAvenant.ID_Type_Document=ID_Type_Document;
                    docAvenant.Original=true;
                    docAvenant.VisibleNOL = true;

                    ID_Document = docAvenant.Ajouter(0, entite);

                    if(ID_Document>0)
                    {
                        DateTime dateAcq=DateTime.Now;

                        //insertion Avenant
                        if(entite == "NSAS")
                            con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;");
                        else
                            con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;");

                        con.Open();

                        strSQLAvenant = "INSERT INTO [Avenant] ([CodeCompagnie],[NumAvenant],[ID_Document],[TypeAvenant],[NumContrat],[CodeApporteur],[NomApporteur],[NomSouscripteur],[DateAvenant],[Visible],ReferenceInterne,[EnvoyerSF],[DateAcquisition],[NumLot])"
                            + " VALUES(@CodeCompagnie,@NumAvenant,@ID_Document,@TypeAvenant,@NumContrat,@CodeApporteur,@NomApporteur,@NomSouscripteur,@DateAvenant,@Visible,@ReferenceInterne,@EnvoyerSF,@DateAcquisition,@NumLot);"
                            + "SELECT CAST(SCOPE_IDENTITY() AS int)";
                        
                        cmdAvenant = new SqlCommand(strSQLAvenant, con);
                        cmdAvenant.Parameters.AddWithValue("@CodeCompagnie", (object)CodeCompagnie ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@NumAvenant", (object)NumAvenant ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@ID_Document", (object)ID_Document ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@TypeAvenant", (object)TypeAvenant ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@NumContrat", (object)NumContrat ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@CodeApporteur", (object)CodeApporteur ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@NomApporteur", (object)NomApporteur ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@NomSouscripteur", (object)NomSouscripteur ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@DateAvenant", (object)DateAvenant ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@Visible", (object)Actif ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@ReferenceInterne", (object)ReferenceInterne ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@EnvoyerSF", (object)0 ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@DateAcquisition", (object)dateAcq ?? DBNull.Value);
                        cmdAvenant.Parameters.AddWithValue("@NumLot", (object)NumLot ?? DBNull.Value);

                        int ID_Avenant = (Int32)cmdAvenant.ExecuteScalar();

                        if (ID_Avenant > 0)
                        {
                            if (envoyerSF && tpDoc.EnvoyerSF)
                            {
                                if (entite == "NSAS")
                                {
                                    string envSF = ConfigurationManager.AppSettings["EnvironementSF"];
                                    string loginSF = ConfigurationManager.AppSettings["loginSF"];
                                    string mdpSF = ConfigurationManager.AppSettings["mdpSF"];

                                    string nomTypeActe = Avenant.GetNomTypeActe(tpDoc.ID_Type_Document);


                                    if (envSF == "DEV")
                                    {
                                        //identification
                                        Tools.wsdl_enterprise_dev.SforceService bdAuth = new Tools.wsdl_enterprise_dev.SforceService();
                                        Tools.wsdl_enterprise_dev.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                                        string sIdSF = lr.sessionId;

                                        bdAuth.Url = lr.serverUrl;
                                        bdAuth.SessionHeaderValue = new Tools.wsdl_enterprise_dev.SessionHeader();
                                        bdAuth.SessionHeaderValue.sessionId = sIdSF;

                                        //création de l'objet SF
                                        Tools.wsdl_enterprise_dev.sObject[] myObjectArr = new Tools.wsdl_enterprise_dev.sObject[1];

                                        Tools.wsdl_enterprise_dev.Avenant__c avSF = new Tools.wsdl_enterprise_dev.Avenant__c();
                                        avSF.Date_Acquisition__c = dateAcq;
                                        avSF.Date_Acquisition__cSpecified = true;
                                        avSF.Id_GED__c = ID_Document.ToString();
                                        avSF.Name = nomFichier;

                                        if (TypeAvenant == typeAvenantDefaut)
                                        {
                                            avSF.Type_Acte__c = nomTypeActe;
                                            avSF.Type_Document__c = tpDoc.Nom;
                                        }

                                        myObjectArr[0] = avSF;

                                        //envoi de l'objet SF
                                        Tools.wsdl_enterprise_dev.SaveResult[] ret = bdAuth.create(myObjectArr);

                                        if (ret[0].success)
                                        {
                                            string strSQLEnvoiSF = "UPDATE dbo.Avenant SET EnvoyerSF=1 WHERE ID_Document=@ID_Document";

                                            SqlCommand cmdEnvoiSF = new SqlCommand(strSQLEnvoiSF, con);
                                            cmdEnvoiSF.Parameters.AddWithValue("@ID_Document", (object)ID_Document ?? DBNull.Value);

                                            int retour = cmdEnvoiSF.ExecuteNonQuery();
                                            if (retour == 1)
                                                retour = 1;//trans.Commit();scope.Complete();
                                            else
                                                throw new Exception("Impossible de flaguer l'avenant comme envoyé à SF");
                                        }
                                        else
                                            throw new Exception("Impossible d'envoyer l'avenant à SF");
                                    }
                                    else if (envSF == "QUALIF")
                                    {
                                        //identification
                                        Tools.wsdl_enterprise_qualif.SforceService bdAuth = new Tools.wsdl_enterprise_qualif.SforceService();
                                        Tools.wsdl_enterprise_qualif.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                                        string sIdSF = lr.sessionId;

                                        bdAuth.Url = lr.serverUrl;
                                        bdAuth.SessionHeaderValue = new Tools.wsdl_enterprise_qualif.SessionHeader();
                                        bdAuth.SessionHeaderValue.sessionId = sIdSF;


                                        //création de l'objet SF
                                        Tools.wsdl_enterprise_qualif.sObject[] myObjectArr = new Tools.wsdl_enterprise_qualif.sObject[1];

                                        Tools.wsdl_enterprise_qualif.Avenant__c avSF = new Tools.wsdl_enterprise_qualif.Avenant__c();
                                        avSF.Date_Acquisition__c = dateAcq;
                                        avSF.Date_Acquisition__cSpecified = true;
                                        avSF.Id_GED__c = ID_Document.ToString();
                                        avSF.Name = nomFichier;

                                        if (TypeAvenant == typeAvenantDefaut)
                                        {
                                            avSF.Type_Acte__c = nomTypeActe;
                                            avSF.Type_Document__c = tpDoc.Nom;
                                        }

                                        myObjectArr[0] = avSF;

                                        //envoi de l'objet SF
                                        Tools.wsdl_enterprise_qualif.SaveResult[] ret = bdAuth.create(myObjectArr);

                                        if (ret[0].success)
                                        {
                                            string strSQLEnvoiSF = "UPDATE dbo.Avenant SET EnvoyerSF=1 WHERE ID_Document=@ID_Document";

                                            SqlCommand cmdEnvoiSF = new SqlCommand(strSQLEnvoiSF, con);
                                            cmdEnvoiSF.Parameters.AddWithValue("@ID_Document", (object)ID_Document ?? DBNull.Value);

                                            int retour = cmdEnvoiSF.ExecuteNonQuery();
                                            if (retour == 1)
                                                retour = 1;//trans.Commit();scope.Complete();
                                            else
                                                throw new Exception("Impossible de flaguer l'avenant comme envoyé à SF");
                                        }
                                        else
                                            throw new Exception("Impossible d'envoyer l'avenant à SF");
                                    }
                                    else if (envSF == "PROD")
                                    {
                                        //identification
                                        Tools.wsdl_enterprise.SforceService bdAuth = new Tools.wsdl_enterprise.SforceService();
                                        Tools.wsdl_enterprise.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                                        string sIdSF = lr.sessionId;

                                        bdAuth.Url = lr.serverUrl;
                                        bdAuth.SessionHeaderValue = new Tools.wsdl_enterprise.SessionHeader();
                                        bdAuth.SessionHeaderValue.sessionId = sIdSF;

                                        //création de l'objet SF
                                        Tools.wsdl_enterprise.sObject[] myObjectArr = new Tools.wsdl_enterprise.sObject[1];

                                        Tools.wsdl_enterprise.Avenant__c avSF = new Tools.wsdl_enterprise.Avenant__c();
                                        avSF.Date_Acquisition__c = dateAcq;
                                        avSF.Date_Acquisition__cSpecified = true;
                                        avSF.Id_GED__c = ID_Document.ToString();
                                        avSF.Name = nomFichier;

                                        if (TypeAvenant == typeAvenantDefaut)
                                        {
                                            avSF.Type_Acte__c = nomTypeActe;
                                            avSF.Type_Document__c = tpDoc.Nom;
                                        }

                                        myObjectArr[0] = avSF;

                                        //envoi de l'objet SF
                                        Tools.wsdl_enterprise.SaveResult[] ret = bdAuth.create(myObjectArr);

                                        if (ret[0].success)
                                        {
                                            string strSQLEnvoiSF = "UPDATE dbo.Avenant SET EnvoyerSF=1 WHERE ID_Document=@ID_Document";

                                            SqlCommand cmdEnvoiSF = new SqlCommand(strSQLEnvoiSF, con);
                                            cmdEnvoiSF.Parameters.AddWithValue("@ID_Document", (object)ID_Document ?? DBNull.Value);

                                            int retour = cmdEnvoiSF.ExecuteNonQuery();
                                            if (retour == 1)
                                                retour = 1;//trans.Commit();scope.Complete();
                                            else
                                                throw new Exception("Impossible de flaguer l'avenant comme envoyé à SF");
                                        }
                                        else
                                            throw new Exception("Impossible d'envoyer l'avenant à SF");
                                    }
                                }
                                else
                                {
                                    string idContratSF = Contrat.FindIdContratSF(NumContrat, entite);
                                    if (!string.IsNullOrWhiteSpace(idContratSF))
                                        Indexer("Portefeuille", idContratSF, "", entite);
                                }
                            }
                            /*else
                                scope.Complete();//trans.Commit();*/
                        }
                        else
                            throw new Exception("Erreur lors de l'insertion des informations de l'avenant dans la table 'Avenant'");
                    }
                    else
                        throw new Exception("Erreur lors de l'insertion du document dans la table 'CA_MEDIA'"); 
                }
                else if (ID_Type_Document == 0)//type non transcodé
                {
                    ID_Document = -1;
                }
                else if (ID_Type_Document == -1)//type à effacer
                {
                    ID_Document = -2;
                }
                else if (ID_Type_Document == -2)//nouveau type
                {
                    string strSQL = "INSERT INTO [Type_Document_Compagnie] ([CodeCompagnie],[NomType_Comapagnie],[ID_Type_Document])"
                        + " VALUES (@CodeCompagnie,@NomType_Comapagnie,@ID_Type_Document)";

                    if (entite == "NSAS")
                        con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;");
                    else
                        con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;");

                    con.Open();
                    SqlCommand cmd = new SqlCommand(strSQL, con);
                    cmd.Parameters.AddWithValue("@CodeCompagnie", (object)CodeCompagnie ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NomType_Comapagnie", (object)TypeAvenant ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ID_Type_Document", (object)0 ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                    ID_Document = -1;
                }
            }
            catch (Exception)
            {
                ID_Document = 0;

                /*try
                {
                    if(trans!=null)
                        trans.Rollback();
                }
                catch (Exception exRb)
                {

                }*/
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return ID_Document;
        }

        public static List<Avenant> GetAvenantNonIndexeSF(string entite = "NSAS")
        {
            List<Avenant> listAvenant = new List<Avenant>();

            try
            {
                string strCon = "";
                string sql = "";

                if (entite == "NSAS")
                {
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;";
                    sql = @"SELECT [Avenant].ID_Document, [Avenant].NumContrat, [Type_Document].Categorie as CategorieDocument, [Type_Document].Nom as TypeDocument, isnull([Type_Acte].Nom, '') as TypeActe,
                                isnull([Avenant].[ReferenceInterne], '') as RefInterneActe, isnull([Type_Acte].[Table_CA], '') as Table_CA, [CA_MEDIA].nom as NomFichier, [Avenant].DateAcquisition,[CA_MEDIA].visibleNOL 
                            FROM [dbo].[Avenant]
	                            INNER JOIN [dbo].[CA_MEDIA] ON [CA_MEDIA].[pk]=[Avenant].[ID_Document]
	                            INNER JOIN [dbo].[Type_Document] ON [Type_Document].[ID_Type_Document] = [CA_MEDIA].[ID_Type_Document]
	                            LEFT JOIN dbo.[Type_Acte_Type_Document] ON [Type_Acte_Type_Document].ID_Type_Document=[CA_MEDIA].ID_Type_Document
	                            LEFT JOIN dbo.[Type_Acte] ON [Type_Acte].ID_Type_Acte=[Type_Acte_Type_Document].ID_Type_Acte
                            WHERE ([Avenant].[Objet_SF] is null OR [Avenant].[CleSalesforce] is null)
	                            AND [CA_MEDIA].[TEK_DateSuppressionSF] is null
	                            AND [Type_Document].[EnvoyerSF]=1
	                            AND [Avenant].[EnvoyerSF]=0";
                }
                else
                {
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;";
                    sql = @"SELECT [Avenant].[ID_Document],[Avenant].[NumContrat] FROM [dbo].[Avenant]
		                        INNER JOIN [dbo].[CA_MEDIA] ON [CA_MEDIA].[pk]=[Avenant].[ID_Document]
                                INNER JOIN [dbo].[Type_Document] ON [Type_Document].[ID_Type_Document] = [CA_MEDIA].[ID_Type_Document]
	                        WHERE [Avenant].[ID_Document] NOT IN (SELECT DISTINCT [IndexationDocument].[ID_Document] FROM [dbo].[IndexationDocument] WHERE [IndexationDocument].[Applicatif]='Salesforce')
                                AND [Avenant].[ID_Document] NOT IN (SELECT DISTINCT [RejetDocument].[ID_Document] FROM [dbo].[RejetDocument] WHERE [RejetDocument].[Applicatif]='Salesforce')
                                AND [CA_MEDIA].[TEK_DateSuppressionSF] is null
                                AND [Avenant].[NumContrat] is not null
                                AND [Type_Document].[EnvoyerSF]=1";
                }

                using (SqlConnection con = new SqlConnection(strCon))
                {
                    SqlCommand cmd = new SqlCommand(sql, con);

                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Avenant av = new Avenant();

                            int ind = dr.GetOrdinal("ID_Document");
                            if (dr.IsDBNull(ind))
                                av.ID_Document = 0;
                            else
                                av.ID_Document = dr.GetInt32(ind);

                            av.NumContrat = dr["NumContrat"].ToString();

                            if (av.ID_Document > 0 && !String.IsNullOrWhiteSpace(av.NumContrat))
                                listAvenant.Add(av);
                        }
                        dr.Close();
                    }
                    con.Close();
                }
            }
            catch (Exception)
            {
                listAvenant = new List<Avenant>();
            }

            return listAvenant;
        }

        public static List<Avenant> GetAvenantNonIndexeCA()
        {
            List<Avenant> listAvenant = new List<Avenant>();

            try
            {
                string sql = @"SELECT Avenant.*
FROM dbo.Avenant
	INNER JOIN dbo.CA_MEDIA ON Avenant.ID_Document=CA_MEDIA.pk
WHERE ([table] is null OR rtrim(ltrim([table]))='' OR pkvalue is null)
	AND CA_MEDIA.[TEK_DateSuppressionSF] is null
	AND ([Avenant].[Objet_SF] is not null AND [Avenant].[CleSalesforce] is not NULL)
	AND [Avenant].[EnvoyerSF]=1
	order by Avenant.dateAvenant";
            }
            catch (Exception)
            {
                listAvenant = new List<Avenant>();
            }

            return listAvenant;

        }

        public bool Indexer(string typeObjet, string objetCleSalesForce, string docCleSalesForce="", string entite = "NSAS")
        {
            bool retour = false;

            try
            {
                string strCon = "";
                if (entite == "NSAS")
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;";
                else
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;";

                using (SqlConnection con = new SqlConnection(strCon))
                {
                    string sql = "SELECT count(*) FROM CA_MEDIA WHERE pk = @ID_Doc";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@ID_Doc", (object)ID_Document ?? DBNull.Value);

                    con.Open();
                    int nbLigne = (int)cmd.ExecuteScalar();
                    con.Close();
                    if (nbLigne != 0)
                    {

                        sql = "UPDATE Avenant SET Objet_SF=@Objet_SF, CleSalesForce = @CleSalesForce where ID_Document = @ID_Document";
                        cmd = new SqlCommand(sql, con);
                        cmd.Parameters.AddWithValue("@Objet_SF", (object)typeObjet ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CleSalesForce", (object)objetCleSalesForce ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ID_Document", (object)ID_Document ?? DBNull.Value);

                        con.Open();
                        nbLigne = cmd.ExecuteNonQuery();
                        con.Close();

                        if (nbLigne != 0)
                        {
                            if (entite == "NI")
                            {
                                sql = "SELECT Count(*) FROM IndexationDocument WHERE ID_Document=@ID_Document AND Applicatif='Salesforce' and Objet=@TypeObjetAvenant and Objet_ID=@ID_PliSalesForce";
                                cmd = new SqlCommand(sql, con);
                                cmd.Parameters.AddWithValue("@ID_Document", (object)ID_Document);
                                cmd.Parameters.AddWithValue("@TypeObjetAvenant", (object)typeObjet);
                                cmd.Parameters.AddWithValue("@ID_PliSalesForce", (object)objetCleSalesForce);

                                con.Open();
                                int nb = (int)cmd.ExecuteScalar();
                                con.Close();

                                if (nb > 0)
                                    sql = "UPDATE IndexationDocument SET CleExterne=@ID_DocumentSalesForce WHERE ID_Document=@ID_Document AND Applicatif='Salesforce' and Objet=@TypeObjetAvenant and Objet_ID=@ID_PliSalesForce";

                                else
                                    sql = "INSERT INTO IndexationDocument(ID_Document,Applicatif,Objet,Objet_ID,CleExterne) VALUES (@ID_Document,'Salesforce',@TypeObjetAvenant,@ID_PliSalesForce,@ID_DocumentSalesForce)";

                                cmd = new SqlCommand(sql, con);
                                cmd.Parameters.AddWithValue("@ID_PliSalesForce", (object)objetCleSalesForce ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ID_Document", (object)ID_Document);
                                cmd.Parameters.AddWithValue("@TypeObjetAvenant", (object)typeObjet);
                                cmd.Parameters.AddWithValue("@ID_DocumentSalesForce", (object)docCleSalesForce);

                                con.Open();
                                nbLigne=cmd.ExecuteNonQuery();
                                con.Close();

                                retour = (nbLigne>0);
                            }
                            else if (entite == "NSAS")
                            {
                                retour = false;
                            }

                        }
                        else
                        {
                            //aucun avenant associé à ce document
                            retour = false;
                        }
                    }
                    else
                    {
                        //pas de document associé à cet avenant
                        retour = false;
                    }
                }



            }
            catch (Exception)
            {
                retour = false;
            }

            return retour;
        }

        public static string GetNomTypeActe(int ID_TypeDoc)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;

            string NomTypeActe = "";

            try
            {
                con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;");

                strSQL = @"SELECT Type_Acte.Nom
                            FROM Type_Acte_Type_Document
                                LEFT JOIN dbo.Type_Acte ON Type_Acte_Type_Document.ID_Type_Acte=Type_Acte.ID_Type_Acte
                            WHERE Type_Acte_Type_Document.ID_Type_Document=@ID_TypeDoc";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@ID_TypeDoc", (object)ID_TypeDoc ?? DBNull.Value);

                NomTypeActe = (string)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return NomTypeActe;
        }

        public string GetNomFichierAvenant(string entite = "NSAS")
        {
            string nomFichier = "";

            try
            {
                int ID_Type_Document = TypeDocument.GetIDTypeDocumentCompagnie(CodeCompagnie, TypeAvenant, entite);
                if (ID_Type_Document > 0) //type existant
                {
                    TypeDocument tpDoc = new TypeDocument(ID_Type_Document,entite);

                    string nomSous="";
                    if (entite == "NSAS")
                    {
                        using (SqlConnection con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;"))
                        {
                            string strSQL = @"SELECT CASE WHEN [isPersonnePhysique]=1 then rtrim(ltrim(ca_souscripteur.nom)) else rtrim(ltrim(ca_souscripteur.societe))  END as nom
                                FROM dbo.ca_contrat
	                                INNER JOIN dbo.ca_souscripteur ON ca_contrat.fksouscripteur=ca_souscripteur.pk
                                WHERE NContrat=@numContrat";

                            con.Open();
                            SqlCommand cmd = new SqlCommand(strSQL, con);
                            cmd.Parameters.AddWithValue("@numContrat", (object)NumContrat ?? DBNull.Value);

                            nomSous = (string)cmd.ExecuteScalar();
                        }
                    }

                    if (!String.IsNullOrWhiteSpace(NumContrat))
                    {
                        if (String.IsNullOrWhiteSpace(nomSous))
                            nomFichier = NumContrat + "_" + tpDoc.Nom + "_" + DateAvenant.ToString("dd-MM-yyyy");
                        else
                            nomFichier = nomSous + "_" + NumContrat + "_" + tpDoc.Nom + "_" + DateAvenant.ToString("dd-MM-yyyy");
                    }
                    else
                        nomFichier = "";
                }
                nomFichier = Tools.Tools.ReplaceIllegalCharFromFileName(nomFichier);
            }
            catch (Exception ex)
            {
                nomFichier = "";
            }

            return nomFichier;
        }

        /*
        public static int Exist(string numLot, string NumAvenant)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;

            int idDoc = 0;

            try
            {
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = "SELECT ID_Document FROM [Avenant] WHERE NumAvenant=@NumAvenant AND NumLot=@NumLot";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@NumAvenant", (object)NumAvenant ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NumLot", (object)numLot ?? DBNull.Value);

                idDoc = (Int32)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                idDoc = 0;
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return idDoc;
        }
        */

        public static int Exist(string numLot, string numAvenant, string numContrat="", string entite = "NSAS")
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;

            int idDoc = 0;

            try
            {
                string strCon = "";
                if (entite == "NSAS")
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;";
                else
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;";

                con = new SqlConnection(strCon);

                if (numContrat.Trim() == "")
                {
                    strSQL = "SELECT ID_Document FROM [Avenant] WHERE NumAvenant=@NumAvenant AND NumLot=@NumLot";

                    cmd = new SqlCommand(strSQL, con);
                    cmd.Parameters.AddWithValue("@NumAvenant", (object)numAvenant ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NumLot", (object)numLot ?? DBNull.Value);
                }
                else
                {
                    strSQL = "SELECT ID_Document FROM [Avenant] WHERE NumAvenant=@NumAvenant AND NumContrat=@NumContrat AND NumLot=@NumLot";

                    cmd = new SqlCommand(strSQL, con);
                    cmd.Parameters.AddWithValue("@NumAvenant", (object)numAvenant ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NumContrat", (object)numContrat ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NumLot", (object)numLot ?? DBNull.Value);
                }

                
                con.Open();
                idDoc = (Int32)(cmd.ExecuteScalar()??0);
            }
            catch (Exception ex)
            {
                idDoc = 0;
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return idDoc;
        }

        public static int NbAvenantAutomatiqueNonEnvoye()
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            int nbAv = 0;

            try
            {
                con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;");
                strSQL = @"select Count(av.ID_Document)
                            FROM avenant av
	                            INNER JOIN dbo.CA_MEDIA doc ON doc.pk=av.ID_Document
	                            INNER JOIN dbo.Type_Document tp ON tp.ID_Type_Document = doc.ID_Type_Document
                            WHERE av.EnvoyerSF=0
                                AND (av.reprise is null or av.reprise=0)
                                AND tp.EnvoyerSF=1
                                AND av.CodeCompagnie<>'SCAN'";

                con.Open();
                cmd = new SqlCommand(strSQL, con);

                nbAv = (Int32)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                nbAv = 0;
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return nbAv;
        }


        /// <summary>
        /// Liste des avenants à exporter 
        /// </summary>
        /// <param name="appCleSf"></param>
        /// <param name="dateDebut"> </param>
        /// <param name="dateFin"> </param>
        /// <param name="typeDocs"> </param>
        /// <returns></returns>
        public static int GetDataExport(DateTime dateDebut, DateTime dateFin, List<int> typeDocs = null )
        {
            if (HttpContext.Current.Session["app"] == null)
                throw new Exception("non connecté");

            var ocon = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);

            var dt = new DataTable();
            DataRow row = null;
            dt.Columns.Add("nom", typeof(String));
            dt.Columns.Add("fk_media", typeof(int));
            dt.Columns.Add("tables", typeof(String));
            dt.Columns.Add("contrat", typeof(String));

            var sql = @"   SELECT	t.*, app.[PART_AGENCE_CleSalesForce], GedTyp.[ID_Type_Document]  
                           FROM    (
                                       SELECT * FROM dbo.Vue_AvenantsContrat_Contrat 
                                       UNION ALL 
                                       SELECT * FROM dbo.Vue_AvenantsContrat_Arbitrages 
                                       UNION ALL 
                                       SELECT * FROM dbo.Vue_AvenantsContrat_Mouvements 
                                       UNION ALL 
                                       SELECT * FROM dbo.Vue_AvenantsContrat_MvtsProg 
                                   ) t 
                                   INNER JOIN [Nortiaca].[dbo].[PART_AGENCE] app on t.fkapporteur=app.[PART_AGENCE_ID]
                                   INNER JOIN [Nortiaca_MEDIA].[dbo].[Type_Document] GedTyp on t.fktypedocs = GedTyp.[ID_Type_Doc_CA]
                            WHERE  t.[date] >= @dateDebut 
                                   AND t.[date] <= @dateFin
	                               AND app.[PART_AGENCE_CleSalesForce] = @appCleSf ";

            if (typeDocs != null && typeDocs.Any())
            {
                var list = typeDocs.Aggregate("", (current, d) => current + (d + ","));
                if (list.EndsWith(",")) list = list.Substring(0, list.Length - 1);
                sql += @"          AND GedTyp.[ID_Type_Document] in ( " + list + ")";
            }


            if (ocon.State != ConnectionState.Open) ocon.Open();
            var cmd = new SqlCommand(sql, ocon);
            cmd.Parameters.Add("@appCleSf", SqlDbType.NVarChar).Value = HttpContext.Current.Session["app"].ToString();
            cmd.Parameters.Add("@dateDebut", SqlDbType.DateTime).Value = dateDebut;
            cmd.Parameters.Add("@dateFin", SqlDbType.DateTime).Value = dateFin;

            var dr = cmd.ExecuteReader();
            int t;
            if (!dr.HasRows)
            {
                dr.Close();
                ocon.Close();
                return 0;
            }

            while (dr.Read())
            {
                row = dt.NewRow();
                row["nom"] = dr["nom"].ToString();
                if ((int.TryParse(row["nom"].ToString().Substring(0, 1), out t) & (row["nom"].ToString().Contains(".pdf"))))
                {
                    row["nom"] = dr["nomApp"] + "_" + row["nom"];
                }
                else
                {
                    row["nom"] = dr["nom"].ToString();
                }

                row["fk_media"] = int.Parse(dr["pk"].ToString());
                row["tables"] = dr["table"].ToString();
                row["contrat"] = dr["ncontrat"].ToString();
                dt.Rows.Add(row);
            }

            dr.Close();

            ocon.Close();
            HttpContext.Current.Session["fileTable"] = dt;
            return dt.Rows.Count;
        }

        public static void CreateZip()
        {
            if (HttpContext.Current.Session["app"] == null)
                throw new Exception("non connecté");

            var ocon = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;");
            string sql = "";

            //Création du Zip
            var s = new StringBuilder();
            var dt =(DataTable) HttpContext.Current.Session["fileTable"];
            
            dynamic nbrFile = dt.Rows.Count;
            int i = 0;
            var tempArray = new ArrayList();
            ocon.Open();
            dynamic zip = new ZipFile();

            while ((i < nbrFile))
            {
                sql = "SELECT datas FROM [Nortiaca_MEDIA].[dbo].[CA_MEDIA] WHERE pk=@pk";
                var cmd = new SqlCommand(sql, ocon);
                cmd.Parameters.Add("@pk", SqlDbType.Int).Value = dt.Rows[i]["fk_media"];
                var datas = (byte[])cmd.ExecuteScalar();

                var nomfile = dt.Rows[i]["nom"].ToString();
                nomfile = nomfile.Replace("/", "_");

                if (!nomfile.ToLower().EndsWith(".pdf"))
                    nomfile += ".pdf";

            testNom:
                if ((tempArray.IndexOf(nomfile) != -1))
                {
                    nomfile = nomfile.Substring(0, nomfile.Length - 4);
                    nomfile = nomfile + "_1" + ".pdf";
                    goto testNom;
                }

                tempArray.Add(nomfile);
                dynamic entry = new ZipEntry();

                entry = zip.AddEntry(nomfile, "\\", datas);
                i = i + 1;
            }

            ocon.Close();

            var date = DateTime.Now;
            var filename = "-" + date.Year
                            + "-" + (date.Month.ToString().Length > 1 ? date.Month.ToString() : "0" + date.Month)
                            + "-" + (date.Day.ToString().Length > 1 ? date.Day.ToString() : "0" + date.Month);

            HttpContext.Current.Response.ContentType = "application/zip";
            HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=extraction" + filename + ".zip");
            zip.Save(HttpContext.Current.Response.OutputStream);
            zip.Dispose();
        }

    }

    public class FichierAvenantZIP
    {
        public string CheminFichier { get; set; }
        public string NomRepertoire { get; set; }
        public string NomFichier { get; set; }
        public DateTime DateHeure { get; set; }
        public bool DezipOK { get; set; }
    }

    public class AvenantSender
    {
        public int ID_Avenant { get; set; }
        public string NomFichier { get; set; }
        public string NomTypeDocument { get; set; }
        public DateTime DateEffet { get; set; }

        public bool SetDateEnvoi(DateTime dateEnvoi)
        {
            bool retour = false;

            try
            {
                string sql = @"UPDATE [dbo].[Avenant] SET [DateEnvoi]=@dateEnvoi
                                WHERE [Avenant].[ID_Avenant]=@id";

                using (SqlConnection con = new SqlConnection("data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;"))
                {
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@dateEnvoi", (object)dateEnvoi ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", (object)ID_Avenant ?? DBNull.Value);

                    con.Open();
                    retour = (cmd.ExecuteNonQuery() > 0);
                    con.Close();
                }

                retour = true;
            }
            catch (Exception)
            {
                retour = false;
            }

            return retour;
        }
    }
}
