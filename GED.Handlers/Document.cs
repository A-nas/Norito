using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;

namespace GED.Handlers
{
    public class SplitDocument
    {
        public int ID_Type_Document { get; set; }
        public string NomType { get; set; }
        public string Pages { get; set; }
        public List<int> ListePages { get; set; }
        public bool Original { get; set; }
        public string nomOriginal { get; set; }
    }

    public class DocumentView
    {
        public int ID_Document { get; set; }
        public string NomDocument { get; set; }
        public string TypeDocument { get; set; }
        public string SousTypeDocument { get; set; }
        public int NbPage { get; set; }
        public string Applicatif { get; set; }
        public string TypeObjet { get; set; }
        public string ID_Objet { get; set; }
        public string CleDocumentExterne { get; set; }
        public string URL_Affichage { get; set; }
        public DateTime? DateAcquisition { get; set; }
        public DateTime? DateEffet { get; set; }
        public bool Original { get; set; }
        public bool Publier { get; set; }
    }

    public class IndexationDocument
    {
        public int ID_DocumentNortia { get; set; } //ID_Document de la GED
        public string ID_DocumentSalesForce { get; set; } //ID_document dans Salesforce (Id DocumentActe)
        public string TypeDocument { get; set; }//Type du document (ex: CNI)
        public int ID_PliNortia { get; set; }//ID_Pli dans la GED

        [JsonProperty(PropertyName = "type")]
        public string TypeActe { get; set; }//Type de l'acte du document (ex: Arbitrage)
        public string TypeObjetAvenant { get; set; } //type de l'objet où le document est rattaché "Acte" ou "Contrat"
        public string ID_PliSalesForce { get; set; } //ID_Pli dans salesforce (Id de l'Acte)
        public bool VisibleNOL { get; set; }//le document est-il visible sur le NOL ou N+?
    }

    public class IntegrationDocument
    {
        public int Index { get; set; }//index du document dans le tableau
        public string ID_ActeSalesforce { get; set; }//ID (clé Salesforce) de l'acte du document
        public string NomDocument { get; set; }//nom du document (sans l'extension)
        public string ExtensionDocument { get; set; }//Extension du document (sans l'extension)
        public string TypeActe { get; set; }//Type de l'acte du document (ex: Arbitrage)
        public string TypeDocument { get; set; }//Type du document (ex: CNI)
        public bool VisibleNOL { get; set; }//le document est-il visible sur le NOL ou N+?
        public byte[] DataDocument { get; set; }// document sous la forme d'un tableau de byte
    }

    public class DocumentProduction
    {
        public int ID_DocumentNortia { get; set; }
        public string ID_DocumentSalesForce { get; set; }
        public int NbPage { get; set; }
    }

    public class RetourIndexationDocument
    {
        public int CodeRetour { get; set; }
        public string Message { get; set; }
    }

    public class Document
    {
        public int ID_Document { get; set; }
        public int ID_Type_Document { get; set; }
        public int ID_Pli { get; set; }
        public string NomType { get; set; }
        public byte[] Datas { get; set; }
        public string Extension { get; set; }
        public string Nom { get; set; }
        public int NbPage { get; set; }
        public bool Original { get; set; }
        public bool Type_GenereUnPli { get; set; } //le document fait-il parti d'un type de document à envoyé et indéxé dans SalesForce?
        public bool Type_EnvoyerSF { get; set; }
        public bool VisibleNOL { get; set; }
        public string CleSalesForce { get; set; }

        public Document()
        {
        }

        public Document(int idDocument)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            SqlDataReader dr = null;

            try
            {
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                strSQL = "select Doc.ID_Type_Document,Doc.ID_Pli, isNull(Type.Nom,'') as NomType, Doc.datas, Doc.extension, Doc.nom, Doc.nbPages, Doc.Original, isNull(Type.GenereUnPli,0) as Type_GenereUnPli, isNull(Type.EnvoyerSF,0) as Type_EnvoyerSF, Doc.CleSalesForce, Doc.VisibleNOL from dbo.CA_MEDIA Doc"
                    + " LEFT JOIN dbo.Type_Document Type ON Doc.ID_Type_Document=Type.ID_Type_Document"
                + " where pk = @ID_Doc";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@ID_Doc", (object)idDocument ?? DBNull.Value);

                dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    ID_Document =idDocument;

  
                    int ind = dr.GetOrdinal("ID_Type_Document");
                    if (dr.IsDBNull(ind))
                        ID_Type_Document = 0;
                    else
                        ID_Type_Document = dr.GetInt32(ind);

                    ind = dr.GetOrdinal("ID_Pli");
                    if (dr.IsDBNull(ind))
                        ID_Pli = 0;
                    else
                        ID_Pli = dr.GetInt32(ind);
         
                    NomType = dr["NomType"].ToString();
                
                    int taille = dr["Datas"].ToString().Length;
                    Datas = new byte[taille];
                    Datas = (byte[])dr["Datas"];
         
                    Extension = dr["Extension"].ToString();
          
                    Nom = dr["Nom"].ToString();

                    NbPage = dr.GetInt32(dr.GetOrdinal("nbPages"));

                    ind = dr.GetOrdinal("Original");
                    if (dr.IsDBNull(ind))
                        Original = false;
                    else
                        Original = dr.GetBoolean(ind);

                    ind = dr.GetOrdinal("Type_GenereUnPli");
                    if (dr.IsDBNull(ind))
                        Type_GenereUnPli = false;
                    else
                        Type_GenereUnPli = dr.GetBoolean(ind);

                    ind = dr.GetOrdinal("Type_EnvoyerSF");
                    if (dr.IsDBNull(ind))
                        Type_EnvoyerSF = false;
                    else
                        Type_EnvoyerSF = dr.GetBoolean(ind);

                    CleSalesForce = dr["CleSalesForce"].ToString();

                    ind = dr.GetOrdinal("VisibleNOL");
                    if (dr.IsDBNull(ind))
                        VisibleNOL = false;
                    else
                        VisibleNOL = dr.GetBoolean(ind);
                }
                else
                    throw new Exception("Impossible de trouver les informations sur ce document");

                dr.Close();
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



        public static RetourIndexationDocument Suprimer(IndexationDocument indexDoc)
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            RetourIndexationDocument retour= new RetourIndexationDocument();

            try
            {
                retour = new RetourIndexationDocument();

                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                con.Open();

                //Trace
                strSQL = @"INSERT INTO [TraceIndexation] ([message],[date]) VALUES(@message,@date)";

                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@message", (object)("Delete:"+indexDoc.ID_DocumentNortia + ";" + indexDoc.ID_DocumentSalesForce + ";" + indexDoc.TypeDocument + ";" + indexDoc.ID_PliNortia + ";" + indexDoc.ID_PliSalesForce + ";" + indexDoc.TypeActe + ";" + indexDoc.TypeObjetAvenant + ";" + indexDoc.VisibleNOL));
                cmd.Parameters.AddWithValue("@date", (object)DateTime.Now);
                cmd.ExecuteNonQuery();

                strSQL = @"UPDATE [CA_MEDIA] SET VisibleNOL=0,TEK_DateSuppressionSF=@DateSup WHERE pk = @ID_Doc;";
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@ID_Doc", (object)indexDoc.ID_DocumentNortia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateSup", (object)DateTime.Now);

                int nbLigne = cmd.ExecuteNonQuery();

                if (nbLigne != 0)
                {
                    retour.CodeRetour = 0;
                    retour.Message = "Success Delete";
                }
                else
                {
                    retour.CodeRetour = 2;
                    retour.Message = "L'identifiant Nortia \"" + indexDoc.ID_DocumentNortia.ToString() + "\" du Document n'existe pas dans la GED";
                }
            }
            catch (Exception ex)
            {
                retour.CodeRetour = 1;
                retour.Message = ex.Message;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            return retour;
        }

        public static bool TraceIndexation(string message,string type, string entite = "NSAS")
        {
            bool retour = true;
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;

            try
            {
                strSQL = @"INSERT INTO [TraceIndexation] ([message],[date]) VALUES(@message,@date)";

                if (entite == "NSAS")
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                else
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED_NI"].ConnectionString);

                con.Open();

                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@message", (object)(entite+" - "+type+": "+ message));
                cmd.Parameters.AddWithValue("@date", (object)DateTime.Now);
                cmd.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                retour = false;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }
            return retour;
        }

        public static RetourIndexationDocument Indexer(IndexationDocument indexDoc, string entite = "NSAS")
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            RetourIndexationDocument retour= new RetourIndexationDocument();

            try
            {
                retour = new RetourIndexationDocument();
                
                //Trace
                string message = "ID_Doc: " + indexDoc.ID_DocumentNortia + "; ID_DocSF: " + indexDoc.ID_DocumentSalesForce + "; TypeDoc: " + indexDoc.TypeDocument + "; ID_Pli: " + indexDoc.ID_PliNortia + "; ID_PliSF: " + indexDoc.ID_PliSalesForce + "; TypeActe: " + indexDoc.TypeActe + "; ObjetSF: " + indexDoc.TypeObjetAvenant + "; VisibleNOL: " + indexDoc.VisibleNOL;
                TraceIndexation(message,"Indexation Update", entite);

                if (entite == "NSAS")
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                else
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED_NI"].ConnectionString);

                strSQL = "SELECT count(*) FROM CA_MEDIA WHERE pk = @ID_Doc AND ID_Pli=@ID_Pli";
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@ID_Pli", (object)indexDoc.ID_PliNortia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Doc", (object)indexDoc.ID_DocumentNortia ?? DBNull.Value);

                con.Open();
                int nbLigne = (int)cmd.ExecuteScalar();

                if (nbLigne != 0)
                {
                    //indexDoc.ID_PliNortia=0: Document Acte, sinon Document Avenant
                    if (indexDoc.ID_PliNortia != 0)
                    {
                        int idTypeActe = TypeDocument.GetIDTypeActe(indexDoc.TypeActe, entite);

                        strSQL = "UPDATE Pli SET ID_Type_Acte=@ID_Type_Acte, CleSalesForce = @CleSalesForce where ID_Pli = @ID_Pli";
                        cmd = new SqlCommand(strSQL, con);
                        cmd.Parameters.AddWithValue("@ID_Type_Acte", (object)idTypeActe ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CleSalesForce", (object)indexDoc.ID_PliSalesForce ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ID_Pli", (object)indexDoc.ID_PliNortia ?? DBNull.Value);

                        nbLigne = cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        strSQL = "UPDATE Avenant SET Objet_SF=@Objet_SF, CleSalesForce = @CleSalesForce where ID_Document = @ID_Document";
                        cmd = new SqlCommand(strSQL, con);
                        cmd.Parameters.AddWithValue("@Objet_SF", (object)indexDoc.TypeObjetAvenant ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CleSalesForce", (object)indexDoc.ID_PliSalesForce ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ID_Document", (object)indexDoc.ID_DocumentNortia ?? DBNull.Value);

                        nbLigne = cmd.ExecuteNonQuery();
                    }

                    //if ((indexDoc.ID_PliNortia != 0 && nbLigne != 0) || indexDoc.ID_PliNortia == 0)
                    if (nbLigne != 0)
                    {
                        int idTypeDoc = TypeDocument.GetIDTypeDocument(indexDoc.TypeDocument, entite);

                        if (idTypeDoc > 0)
                        {
                            if(entite=="NI")
                            {
                                strSQL = "UPDATE CA_MEDIA SET VisibleNOL=@VisibleNOL,ID_Type_Document=@ID_Type_Document,TEK_DateSuppressionSF=null where pk = @ID_Doc;";
                                cmd = new SqlCommand(strSQL, con);
                                cmd.Parameters.AddWithValue("@VisibleNOL", (object)indexDoc.VisibleNOL ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ID_Type_Document", (object)idTypeDoc ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ID_Doc", (object)indexDoc.ID_DocumentNortia ?? DBNull.Value);

                                nbLigne = cmd.ExecuteNonQuery();

                                strSQL = "SELECT Count(*) FROM IndexationDocument WHERE ID_Document=@ID_Document AND Applicatif='Salesforce' and Objet=@TypeObjetAvenant and Objet_ID=@ID_PliSalesForce";
                                cmd = new SqlCommand(strSQL, con);
                                cmd.Parameters.AddWithValue("@ID_Document", (object)indexDoc.ID_DocumentNortia);
                                cmd.Parameters.AddWithValue("@TypeObjetAvenant", (object)indexDoc.TypeObjetAvenant);
                                cmd.Parameters.AddWithValue("@ID_PliSalesForce", (object)indexDoc.ID_PliSalesForce);

                                int nb = (int)cmd.ExecuteScalar();
                                if (nb > 0)
                                    strSQL = "UPDATE IndexationDocument SET CleExterne=@ID_DocumentSalesForce WHERE ID_Document=@ID_Document AND Applicatif='Salesforce' and Objet=@TypeObjetAvenant and Objet_ID=@ID_PliSalesForce";
                              
                                else
                                    strSQL = "INSERT INTO IndexationDocument(ID_Document,Applicatif,Objet,Objet_ID,CleExterne) VALUES (@ID_Document,'Salesforce',@TypeObjetAvenant,@ID_PliSalesForce,@ID_DocumentSalesForce)";
                             
                                cmd = new SqlCommand(strSQL, con);
                                cmd.Parameters.AddWithValue("@ID_PliSalesForce", (object)indexDoc.ID_PliSalesForce ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ID_Document", (object)indexDoc.ID_DocumentNortia);
                                cmd.Parameters.AddWithValue("@TypeObjetAvenant", (object)indexDoc.TypeObjetAvenant);
                                cmd.Parameters.AddWithValue("@ID_DocumentSalesForce", (object)indexDoc.ID_DocumentSalesForce);
                                cmd.ExecuteNonQuery();
                            }
                            else if (entite=="NSAS")
                            {
                                string nomTable = "";
                                int idCA = 0;

                                if (indexDoc.TypeObjetAvenant == "Contrat")
                                {
                                    nomTable = "ca_contrat";
                                    idCA = GetID_Contrat(indexDoc.ID_PliSalesForce);
                                }
                                else
                                {
                                    int idTypeActe = TypeDocument.GetIDTypeActe(indexDoc.TypeActe);
                                    nomTable = GetTableCA(idTypeDoc, idTypeActe);
                                    if (nomTable != null && nomTable != "")
                                        idCA = GetID_Acte(nomTable, indexDoc.ID_PliSalesForce);
                                }

                                //strSQL = "UPDATE CA_MEDIA SET CleSalesForce = @CleSalesForce, Original=@Original, VisibleNOL=@VisibleNOL,ID_Type_Document=@ID_Type_Document,pkvalue=@pkvalue,[table]=@table where pk = @ID_Doc;";
                                strSQL = "UPDATE CA_MEDIA SET CleSalesForce = @CleSalesForce, VisibleNOL=@VisibleNOL,ID_Type_Document=@ID_Type_Document,pkvalue=@pkvalue,[table]=@table,TEK_DateSuppressionSF=null where pk = @ID_Doc;";

                                cmd = new SqlCommand(strSQL, con);
                                cmd.Parameters.AddWithValue("@CleSalesForce", (object)indexDoc.ID_DocumentSalesForce ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@VisibleNOL", (object)indexDoc.VisibleNOL ?? DBNull.Value);
                                //cmd.Parameters.AddWithValue("@Original", (object)indexDoc.Original ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ID_Type_Document", (object)idTypeDoc ?? DBNull.Value);
                                if (nomTable == null || nomTable.Trim() == "" || idCA == 0)
                                {
                                    cmd.Parameters.AddWithValue("@pkvalue", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@table",DBNull.Value);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@pkvalue", (object)idCA);
                                    cmd.Parameters.AddWithValue("@table", (object)nomTable);
                                }
                                cmd.Parameters.AddWithValue("@ID_Doc", (object)indexDoc.ID_DocumentNortia ?? DBNull.Value);


                                nbLigne = cmd.ExecuteNonQuery();

                                if (nbLigne != 0)
                                {
                                    try
                                    {
                                        int ID_TypeDoc_CA = 0;
                                        if (indexDoc.TypeObjetAvenant == "Contrat")
                                        {
                                            ID_TypeDoc_CA = GetTypeDocCA(idTypeDoc);
                                        }
                                        else
                                        {
                                            int idTypeActe = TypeDocument.GetIDTypeActe(indexDoc.TypeActe);
                                            ID_TypeDoc_CA = GetTypeDocCA(idTypeDoc, idTypeActe);
                                        }


                                        if (ID_TypeDoc_CA > 0)
                                        {
                                            if (con != null && con.State == ConnectionState.Open)
                                                con.Close();

                                            con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);

                                            strSQL = @"SELECT count(*) FROM [CA_TYPEDOCS_MEDIA] WHERE [fkmedia]= @fkmedia;";
                                            con.Open();
                                            cmd = new SqlCommand(strSQL, con);
                                            cmd.Parameters.AddWithValue("@fkmedia", (object)indexDoc.ID_DocumentNortia);

                                            int nb = (int)cmd.ExecuteScalar();
                                            if (nb > 0)
                                            {
                                                strSQL = @"UPDATE [CA_TYPEDOCS_MEDIA] set [fktypedocs]=@fktypedocs WHERE [fkmedia]= @fkmedia;";

                                                cmd = new SqlCommand(strSQL, con);
                                                cmd.Parameters.AddWithValue("@fktypedocs", (object)ID_TypeDoc_CA ?? DBNull.Value);
                                                cmd.Parameters.AddWithValue("@fkmedia", (object)indexDoc.ID_DocumentNortia);
                                            }
                                            else
                                            {
                                                strSQL = @"INSERT INTO [CA_TYPEDOCS_MEDIA] ([fktypedocs],[fkmedia],[date]) VALUES(@fktypedocs,@fkmedia,@date)";

                                                cmd = new SqlCommand(strSQL, con);
                                                cmd.Parameters.AddWithValue("@fktypedocs", (object)ID_TypeDoc_CA ?? DBNull.Value);
                                                cmd.Parameters.AddWithValue("@fkmedia", (object)indexDoc.ID_DocumentNortia ?? DBNull.Value);
                                                cmd.Parameters.AddWithValue("@date", (object)DateTime.Now);
                                            }
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception(ex.Message);
                                    }

                                    retour.CodeRetour = 0;
                                    retour.Message = "Success Indexation";
                                }
                                else
                                {
                                    retour.CodeRetour = 5;
                                    retour.Message = "L'identifiant Nortia \"" + indexDoc.ID_DocumentNortia.ToString() + "\" du Document n'existe pas dans la GED";
                                }
                            }
                        }
                        else
                        {
                            retour.CodeRetour = 4;
                            retour.Message = "Le type de document spécifié \"" + indexDoc.TypeDocument + "\" du Document n'existe pas dans la GED";
                        }
                    }
                    else
                    {
                        retour.CodeRetour = 3;
                        retour.Message = "L'identifiant Nortia \"" + indexDoc.ID_DocumentNortia.ToString() + "\" du Document n'est associé à aucun Pli";
                    }
                }
                else
                {
                    retour.CodeRetour = 2;
                    retour.Message = "L'identifiant du Pli Nortia \"" + indexDoc.ID_PliNortia.ToString()+"\" et l'identifiant du Document Nortia \""+indexDoc.ID_DocumentNortia.ToString() + "\" ne coresponde pas dans la GED";
                }
            }
            catch (Exception ex)
            {
                retour.CodeRetour = 1;
                retour.Message = ex.Message;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            if (retour.CodeRetour > 0)
            {
                string message =retour.Message+" - Valeurs: ID_Doc: "+ indexDoc.ID_DocumentNortia + "; ID_DocSF: " + indexDoc.ID_DocumentSalesForce + "; TypeDoc: " + indexDoc.TypeDocument + "; ID_Pli: " + indexDoc.ID_PliNortia + "; ID_PliSF: " + indexDoc.ID_PliSalesForce + "; TypeActe: " + indexDoc.TypeActe + "; ObjetSF: " + indexDoc.TypeObjetAvenant + "; VisibleNOL" + indexDoc.VisibleNOL;
                TraceIndexation(message, "Indexation Erreur", entite);
            }

            return retour;
        }

        public int Ajouter(int ID_TypeActe, string entite = "NSAS")
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            ID_Document = 0;

            try
            {
                string strCon = "";

                if (entite == "NSAS")
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;";
                else
                    strCon = "data source=192.168.1.2\\SQL2008r2qualif;Database=GEDNortia_Invest;Uid=sa;password=NICKEL2000;";

                con = new SqlConnection(strCon);
                strSQL = "INSERT INTO [CA_MEDIA] ([datas],[extension],[nom],[nbPages],[ID_Pli],[ID_Type_Document],Original,VisibleNOL)"
                    + " VALUES(@datas,@extension,@nom,@nbPages,@ID_Pli,@ID_Type_Document,@Original,@VisibleNOL);"
                    + "SELECT CAST(SCOPE_IDENTITY() AS int)";

                con.Open();
                cmd = new SqlCommand(strSQL, con);

                cmd.Parameters.AddWithValue("@datas", (object)Datas ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@extension", (object)Extension ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nom", (object)Nom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nbPages", (object)NbPage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Pli", (object)ID_Pli ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Type_Document", (object)ID_Type_Document ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Original", (object)Original ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@VisibleNOL", (object)VisibleNOL ?? DBNull.Value);

                ID_Document = (Int32)cmd.ExecuteScalar();

                if (entite == "NSAS" && ID_Document > 0)
                {
                    int ID_TypeDoc_CA = GetTypeDocCA(ID_Type_Document,ID_TypeActe);
                    if (ID_TypeDoc_CA > 0)
                    {
                        con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);

                        strSQL = @"INSERT INTO [CA_TYPEDOCS_MEDIA] ([fktypedocs],[fkmedia],[date]) VALUES(@fktypedocs,@fkmedia,@date)";

                        con.Open();
                        cmd = new SqlCommand(strSQL, con);
                        cmd.Parameters.AddWithValue("@fktypedocs", (object)ID_TypeDoc_CA ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@fkmedia", (object)ID_Document ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@date", (object)DateTime.Now ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                ID_Document = 0;
                throw;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            return ID_Document;
        }

        public static int GetTypeDocCA(int ID_TypeDocGED, int ID_TypeActe = 0)
        {
            SqlConnection con = null;

            int ID_TypeDoc_CA = 0;

            try
            {
                TypeDocument tpDoc = new TypeDocument(ID_TypeDocGED);

                if (tpDoc.Categorie == "Acte") //Acte
                {
                    string strSQL = "";
                    SqlCommand cmd = null;

                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                    con.Open();

                    if (ID_TypeActe == 0) //Avenant Acte
                    {
                        strSQL = @"SELECT Type_Acte_Type_Document.[ID_Type_Doc_CA]
                            FROM Type_Acte_Type_Document
                            WHERE Type_Acte_Type_Document.ID_Type_Document=@ID_TypeDoc";

                        cmd = new SqlCommand(strSQL, con);
                        cmd.Parameters.AddWithValue("@ID_TypeDoc", (object)ID_TypeDocGED ?? DBNull.Value);
                    }
                    else //Document Acte
                    {
                        strSQL = @"SELECT Type_Acte_Type_Document.[ID_Type_Doc_CA]
                               FROM Type_Acte_Type_Document
                               WHERE Type_Acte_Type_Document.ID_Type_Document=@ID_TypeDoc
                                   AND Type_Acte_Type_Document.ID_Type_Acte=@ID_TypeActe";

                        cmd = new SqlCommand(strSQL, con);
                        cmd.Parameters.AddWithValue("@ID_TypeActe", (object)ID_TypeActe ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ID_TypeDoc", (object)ID_TypeDocGED ?? DBNull.Value);
                    }
                    object nb = cmd.ExecuteScalar();
                    if(nb!=null)
                        ID_TypeDoc_CA = (Int32)nb;
                }
                else if (tpDoc.Categorie == "Contrat")//Contrat
                {
                    ID_TypeDoc_CA = tpDoc.ID_Type_Document_CA;
                }
                else
                {
                    throw new Exception("La catégorie du type de document n'existe pas");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            return ID_TypeDoc_CA;
        }

        public static string GetTableCA(int ID_TypeDocGED, int ID_TypeActe = 0)
        {
            SqlConnection con = null;

            string tableCA = "";

            try
            {
               TypeDocument tpDoc = new TypeDocument(ID_TypeDocGED);

               if (tpDoc.Categorie == "Acte") //Acte
               {
                   string strSQL = "";
                   SqlCommand cmd = null;

                   con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                   con.Open();

                   if (ID_TypeActe == 0)
                   {
                       strSQL = @"SELECT Type_Acte_Type_Document.[Table_CA]
                               FROM Type_Acte_Type_Document
                               WHERE Type_Acte_Type_Document.ID_Type_Document=@ID_TypeDoc";

                       cmd = new SqlCommand(strSQL, con);
                       cmd.Parameters.AddWithValue("@ID_TypeDoc", (object)ID_TypeDocGED ?? DBNull.Value);
                   }
                   else
                   {
                       strSQL = @"SELECT Type_Acte_Type_Document.[Table_CA]
                               FROM Type_Acte_Type_Document
                               WHERE Type_Acte_Type_Document.ID_Type_Document=@ID_TypeDoc
                                   AND Type_Acte_Type_Document.ID_Type_Acte=@ID_TypeActe";

                       cmd = new SqlCommand(strSQL, con);
                       cmd.Parameters.AddWithValue("@ID_TypeActe", (object)ID_TypeActe ?? DBNull.Value);
                       cmd.Parameters.AddWithValue("@ID_TypeDoc", (object)ID_TypeDocGED ?? DBNull.Value);
                   }

                   tableCA = (string)cmd.ExecuteScalar();
               }
               else if (tpDoc.Categorie == "Contrat")//Contrat
               {
                   tableCA = "ca_contrat";
               }
               else
               {
                   throw new Exception("La catégorie du type de document n'existe pas");
               }

            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            return tableCA;
        }

        public static int GetID_Acte(string nomTable, string cleSalesForce)
        {
            int idCA = 0;
            SqlCommand cmd = null;
            SqlConnection con = null;
            string sql = "";

            try
            {
                switch (nomTable)
                {
                    case "ca_contrat":
                        sql = "SELECT fkcontrat FROM CA_MOUVEMENT WHERE CleSalesForce=@CleSalesForce";
                        break;

                    case "ca_arbitrage":
                        sql = "SELECT ID FROM CA_ARBITRAGE WHERE CleSalesForce=@CleSalesForce";
                        break;

                    case "CA_MOUVEMENT":
                        sql = "SELECT pk FROM CA_MOUVEMENT WHERE CleSalesForce=@CleSalesForce";
                        break;

                    case "ca_mvt_prog":
                        sql = "SELECT pk FROM CA_MVT_PROG WHERE CleSalesForce=@CleSalesForce";
                        break;
                }

                if (sql != "")
                {
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);

                    con.Open();
                    cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@CleSalesForce", (object)cleSalesForce ?? DBNull.Value);

                    object id = cmd.ExecuteScalar();
                    if (id == DBNull.Value)
                        idCA = 0;
                    else
                        idCA = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                idCA = 0;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            return idCA;
        }

        public static int GetID_Contrat(string cleSalesForce)
        {
            int idContrat = 0;
            SqlCommand cmd = null;
            SqlConnection con = null;
            string sql = "";

            try
            {
                sql = "SELECT pk FROM CA_contrat WHERE CleSalesForce=@CleSalesForce";


                if (sql != "")
                {
                    con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);

                    con.Open();
                    cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@CleSalesForce", (object)cleSalesForce ?? DBNull.Value);

                    object id = cmd.ExecuteScalar();
                    if (id == DBNull.Value)
                        idContrat = 0;
                    else
                        idContrat = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                idContrat = 0;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }

            return idContrat;
        }
    }
}
