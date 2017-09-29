using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using GED.Handlers;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using TallComponents.PDF;
using System.IO;
using System.Threading;
using System.Text;
using System.Web.UI;
using TallComponents.PDF.JavaScript;
using TallComponents.PDF.Forms.Fields;
using TallComponents.PDF.Annotations.Widgets;
using TallComponents.PDF.Actions;
using TallComponents.PDF.Navigation;
using TallComponents.Web;
using System.Xml.Linq;
using GED.Tools;
// my usings
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using GED.Handlers;
// only for debugging
using System.Web.Script.Serialization;

namespace GenerationProd
{
    /// <summary>
    /// Description résumée de Service1
    /// </summary>
    //OLD
    //[WebService(Namespace = "http://62.161.183.114:1519/GenerationProd.asmx")]   //QUALIF
    //[WebService(Namespace = "http://62.161.183.114:8064/GenerationProd.asmx")]     //PROD

    //NEW
    //[WebService(Namespace = "http://dev-extranet.nortia.fr/GenerationProd/GenerationProd.asmx")]      //DEV
    [WebService(Namespace = "http://qualif-extranet.nortia.fr/GenerationProd/GenerationProd.asmx")]   //QUALIF
    //[WebService(Namespace = "http://extranet.nortia.fr/GenerationProd/GenerationProd.asmx")]   //PROD
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Pour autoriser l'appel de ce service Web depuis un script à l'aide d'ASP.NET AJAX, supprimez les marques de commentaire de la ligne suivante. 
    // [System.Web.Script.Services.ScriptService]
    public class GenerationProd : System.Web.Services.WebService
    {
        //unique méthode du web service
        [WebMethod(Description = "Génère la prod Nortia")]
        public GenererProdResponse GenererProd(string IDProd, string codeCompagnie, List<Acte> listeActe, string typeEnvoi = "", bool genererProdActe = false, string classification = "")
        {
            GenererProdResponse retour = new GenererProdResponse();
            retour.IDProd = IDProd;

            try
            {
                Log.Trace(IDProd, Log.MESSAGE_INFO, "Appel recut");
                Log.Trace(IDProd, Log.MESSAGE_INFO, "nb Acte: " + (listeActe == null ? 0 : listeActe.Count).ToString());

                string listeCompagnie = ConfigurationManager.AppSettings["listeCompagnie"];
                string[] lesCompagnies = listeCompagnie.Split(';');

                if (!lesCompagnies.Contains(codeCompagnie))
                    throw new Exception("Code compagnie inconnu");

                /*
                if (codeCompagnie != "AEP" && codeCompagnie != "LMP" && codeCompagnie != "SPI" && codeCompagnie != "LMEP" && codeCompagnie != "CAR" && codeCompagnie != "CNP")
                    throw new Exception("Code compagnie inconnu");*/

                Thread thread = new Thread(() => GenererProduction(IDProd, codeCompagnie, listeActe, typeEnvoi, genererProdActe, classification));
                thread.Name = "must debbug this";
                thread.Start();

                retour.codeRetour = 0;
                retour.message = "Appel recut";
            }
            catch (Exception ex)
            {
                retour.codeRetour = 1;
                retour.message = ex.Message;

                Log.Trace(IDProd, Log.MESSAGE_ERROR, ex.Message);
            }

            return retour;
        }

        //class servant à retourner la réponse du web service
        public class GenererProdResponse
        {
            public string IDProd;
            public int codeRetour;
            public string message;
        }

        protected async static void GenererProduction(string IDProd, string codeCompagnie, List<Acte> listeActe, string typeEnvoi = "", bool genererProdActe = false, string classification = "")
        {
            bool retour = false;
            string message = "";
            List<Acte> listeActeTraitementEdi = new List<Acte>();
            List<Acte> listeActePDF = new List<Acte>();

            try
            {
                Log.Trace(IDProd, Log.MESSAGE_INFO, "Début de la génération de la prod");
                Log.Trace(IDProd, Log.MESSAGE_INFO, "Option de la production: [codeCompagnie=" + codeCompagnie + "], [typeEnvoi=" + typeEnvoi + "], [genererProdActe=" + genererProdActe.ToString() + "]");

                DateTime laDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                foreach (Acte acte in listeActe)
                {
                    if (acte.IsTraitementEdi)
                        listeActeTraitementEdi.Add(acte);
                    else
                        listeActePDF.Add(acte);
                }

                if (listeActePDF.Count > 0)
                {
                    if (codeCompagnie == "SPI")
                    { // purger les champs remplies par la prod sprica XML
                        // build empty response object
                        Dictionary<string[], WsResponse> responses = new Dictionary<string[], WsResponse>();
                        foreach (Acte acte in listeActePDF)
                            responses.Add(
                                new string[] {
                                    acte.ReferenceInterne,
                                    acte.prodActeID },
                                new WsResponse { message = new string[] { " " } ,
                                    status_xml = " " } 
                                );
                        Production.getInstance().updateSalesForce(responses);
                    }

                    //Génération de la Prod PDF
                    if (!(genererProdActe ? GenererProdPDFActe(IDProd, codeCompagnie, laDate, listeActePDF, typeEnvoi, false, classification) : GenererProdPDF(IDProd, codeCompagnie, laDate, listeActePDF, typeEnvoi, false, classification)))
                        throw new Exception("Erreur lors de la génération de la production PDF (ID: " + IDProd.ToString() + ") pour la compagnie " + codeCompagnie.ToString());

                    //Génération du Recap PDF
                    if (!GenererRecap(IDProd, codeCompagnie, laDate, listeActePDF, typeEnvoi, false, genererProdActe, classification))
                        throw new Exception("Erreur lors de la génération du recap de production (ID: " + IDProd.ToString() + ") pour la compagnie " + codeCompagnie.ToString());
                }

                if (listeActeTraitementEdi.Count > 0)
                {
                    if (codeCompagnie == "SPI"){ // envoie web service   
                        List<string> ListSuccess = await Production.getInstance().envoyerProd(listeActeTraitementEdi);
                        if (ListSuccess.Count > 0){
                            List<Acte> ListActeSuccess = listeActeTraitementEdi.Where(x => ListSuccess.Contains(x.ReferenceInterne)).ToList(); // extract successful "ACTES"
                            if (!GenererProdXML(IDProd, codeCompagnie, laDate, ListActeSuccess, typeEnvoi, genererProdActe, classification))
                                throw new Exception("Erreur lors de la génération de la production XML EDI (ID: " + IDProd.ToString() + ") pour la compagnie " + codeCompagnie.ToString());
                        }else throw new Exception("Erreur lors de la génération de la production XML EDI (ID: " + IDProd.ToString() + ") pour la compagnie " + codeCompagnie.ToString() + " veuillez regarder les commentaires interne des actes envoyés ");

                    }
                    else
                    {
                        //Génération de l'XML
                        if (!GenererProdXML(IDProd, codeCompagnie, laDate, listeActeTraitementEdi, typeEnvoi, genererProdActe, classification))
                            throw new Exception("Erreur lors de la génération de la production XML EDI (ID: " + IDProd.ToString() + ") pour la compagnie " + codeCompagnie.ToString());

                        //Génération de la Prod PDF
                        if (!(genererProdActe ? GenererProdPDFActe(IDProd, codeCompagnie, laDate, listeActeTraitementEdi, typeEnvoi, true, classification) : GenererProdPDF(IDProd, codeCompagnie, laDate, listeActeTraitementEdi, typeEnvoi, true, classification)))
                            throw new Exception("Erreur lors de la génération de la production PDF EDI (ID: " + IDProd.ToString() + ") pour la compagnie " + codeCompagnie.ToString());

                        //Génération du Recap PDF
                        if (!GenererRecap(IDProd, codeCompagnie, laDate, listeActeTraitementEdi, typeEnvoi, true, genererProdActe, classification))
                            throw new Exception("Erreur lors de la génération du recap de production EDI (ID: " + IDProd.ToString() + ") pour la compagnie " + codeCompagnie.ToString());
                    }
                }

                retour = true;
                message = "Prod traitée avec succès";
            }
            catch (Exception ex)
            {
                retour = false;
                message = ex.Message;
            }

            if (EnvoyerReponseSF(IDProd, retour, message))
                Log.Trace(IDProd, Log.MESSAGE_INFO, "SF a bien recu la réponse");
            else
                Log.Trace(IDProd, Log.MESSAGE_WARNING, "SF n'a pas recu la réponse");
        }

        protected static bool GenererProdPDF(string IDProd, string codeCompagnie, DateTime dateGeneration, List<Acte> listeActe, string typeEnvoi = "", bool TraitementEdi = false, string classification = "")
        {
            TallComponents.PDF.Document pdfProd = new TallComponents.PDF.Document();
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            bool retour = false;
            int nbPage = 0;
            string refActe = "";
            string etape = "";

            try
            {
                Log.Trace(IDProd, Log.MESSAGE_INFO, "Génération de la Prod PDF, nb acte: " + listeActe.Count);

                string archivePath = ConfigurationManager.AppSettings["envoiProdArchivePath"];
                string path = ConfigurationManager.AppSettings["envoiProdPath"];

                string completPath = path + (String.IsNullOrWhiteSpace(classification) ? "" : (@"\" + classification.Trim()) + @"\");
                string completArchivePath = archivePath + codeCompagnie + (String.IsNullOrWhiteSpace(classification) ? "" : (@"\" + classification.Trim())) + @"\";

                if (!Directory.Exists(completPath))
                    Directory.CreateDirectory(completPath);

                if (!Directory.Exists(completArchivePath))
                    Directory.CreateDirectory(completArchivePath);

                List<DocumentProduction> ListeDocument = new List<DocumentProduction>();
                foreach (Acte acte in listeActe)
                {
                    //intercallaire de l'acte
                    refActe = acte.ReferenceInterne;
                    etape = "Génération de l'intercallaire de l'acte";
                    TallComponents.PDF.Document pdfInter = new TallComponents.PDF.Document(GenererIntercallaireActe(acte));
                    pdfProd.Pages.AddRange(pdfInter.Pages.CloneToArray());
                    nbPage++;
                    etape = "Ajout Bookmark sur l'intercallaire";
                    AjouterBookmark(pdfProd, nbPage, acte.ReferenceInterne);

                    //documents de l'acte
                    foreach (DocumentProduction doc in acte.ListeDocument)
                    {
                        etape = "Recuperation d'un document de l'acte: " + doc.ID_DocumentNortia;
                        con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                        strSQL = "select Datas from CA_MEDIA where pk = @ID_Document";

                        con.Open();
                        cmd = new SqlCommand(strSQL, con);
                        cmd.Parameters.Add("@ID_Document", SqlDbType.Int).Value = doc.ID_DocumentNortia;
                        cmd.CommandTimeout = 0;
                        int taille = cmd.ExecuteScalar().ToString().Length;
                        byte[] datas = new byte[taille];
                        datas = (byte[])cmd.ExecuteScalar();
                        TallComponents.PDF.Document pdfDoc = new TallComponents.PDF.Document(new MemoryStream(datas));

                        pdfProd.Pages.AddRange(pdfDoc.Pages.CloneToArray());
                        nbPage += pdfDoc.Pages.Count;
                    }
                    etape = "Fin récupération des documents";

                    //investissement immédiat de l'acte
                    if (acte.InvestissementImmediat)
                    {
                        etape = "Acte avec investissement immediat";
                        Log.Trace(IDProd, Log.MESSAGE_INFO, "Generation Lettre Investissement Immediat");
                        TallComponents.PDF.Document pdfInvest = new TallComponents.PDF.Document(GenererInvestissementImmediatActe(acte, codeCompagnie));
                        pdfProd.Pages.AddRange(pdfInvest.Pages.CloneToArray());
                        nbPage++;
                    }
                }
                etape = "Fin lecture des actes";
                refActe = "";

                /*if (TraitementEdi)
                    name = "ProdXml_" + codeCompagnie  + "_" + dateGeneration.ToString("yyyyMMddHHmm") + ".pdf";
                else
                    name = "Prod_" + codeCompagnie  + "_" + dateGeneration.ToString("yyyyMMddHHmm") + ".pdf";*/

                string name = "Prod" + (TraitementEdi ? "Xml" : "") + "_" + codeCompagnie + (String.IsNullOrWhiteSpace(typeEnvoi) ? "" : ("_" + typeEnvoi.Trim())) + "_" + dateGeneration.ToString("yyyyMMddHHmm") + ".pdf";

                using (FileStream file = new FileStream(completPath + name, FileMode.Create, FileAccess.Write))
                {
                    etape = "Ecriture du PDF";
                    pdfProd.Write(file);
                }

                etape = "Copie du fichier du PDF";
                File.Copy(completPath + name, completArchivePath + name, true);

                etape = "";
                retour = true;
            }
            catch (Exception ex)
            {
                retour = false;
                string etapeMessage = "";
                if (refActe == "")
                    etapeMessage = " Etape: " + etape;
                else
                    etapeMessage = " Acte: " + refActe + " - Etape: " + etape;

                Log.Trace(IDProd, Log.MESSAGE_ERROR, ex.Message + etapeMessage);
            }
            return retour;
        }

        protected static bool GenererProdPDFActe(string IDProd, string codeCompagnie, DateTime dateGeneration, List<Acte> listeActe, string typeEnvoi = "", bool TraitementEdi = false, string classification = "")
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;
            bool retour = false;

            string refActe = "";
            string etape = "";

            try
            {
                Log.Trace(IDProd, Log.MESSAGE_INFO, "Génération de la Prod PDF, nb acte: " + listeActe.Count);

                string archivePath = ConfigurationManager.AppSettings["envoiProdArchivePath"];
                string path = ConfigurationManager.AppSettings["envoiProdPath"];

                string dirName = "Prod" + (TraitementEdi ? "Xml" : "") + "_" + codeCompagnie + (String.IsNullOrWhiteSpace(typeEnvoi) ? "" : ("_" + typeEnvoi.Trim())) + "_" + dateGeneration.ToString("yyyyMMddHHmm");
                string completPath = path + (String.IsNullOrWhiteSpace(classification) ? "" : (@"\" + classification.Trim()) + @"\") + dirName + @"\";
                string completArchivePath = archivePath + codeCompagnie + (String.IsNullOrWhiteSpace(classification) ? "" : (@"\" + classification.Trim())) + @"\" + dirName + @"\";

                if (!Directory.Exists(completPath))
                    Directory.CreateDirectory(completPath);

                if (!Directory.Exists(completArchivePath))
                    Directory.CreateDirectory(completArchivePath);

                List<DocumentProduction> ListeDocument = new List<DocumentProduction>();
                foreach (Acte acte in listeActe)
                {
                    TallComponents.PDF.Document pdfProdActe = new TallComponents.PDF.Document();
                    int nbPage = 0;

                    //intercallaire de l'acte
                    refActe = acte.ReferenceInterne;
                    etape = "Génération de l'intercallaire de l'acte";
                    TallComponents.PDF.Document pdfInter = new TallComponents.PDF.Document(GenererIntercallaireActe(acte));
                    pdfProdActe.Pages.AddRange(pdfInter.Pages.CloneToArray());
                    nbPage++;

                    //documents de l'acte
                    foreach (DocumentProduction doc in acte.ListeDocument)
                    {
                        etape = "Recuperation d'un document de l'acte: " + doc.ID_DocumentNortia;
                        con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnGED"].ConnectionString);
                        strSQL = "select Datas from CA_MEDIA where pk = @ID_Document";

                        con.Open();
                        cmd = new SqlCommand(strSQL, con);
                        cmd.Parameters.Add("@ID_Document", SqlDbType.Int).Value = doc.ID_DocumentNortia;
                        cmd.CommandTimeout = 0;
                        int taille = cmd.ExecuteScalar().ToString().Length;
                        byte[] datas = new byte[taille];
                        datas = (byte[])cmd.ExecuteScalar();
                        TallComponents.PDF.Document pdfDoc = new TallComponents.PDF.Document(new MemoryStream(datas));

                        pdfProdActe.Pages.AddRange(pdfDoc.Pages.CloneToArray());
                        nbPage += pdfDoc.Pages.Count;
                    }
                    etape = "Fin récupération des documents";

                    //investissement immédiat de l'acte
                    if (acte.InvestissementImmediat)
                    {
                        etape = "Acte avec investissement immediat";
                        Log.Trace(IDProd, Log.MESSAGE_INFO, "Generation Lettre Investissement Immediat");
                        TallComponents.PDF.Document pdfInvest = new TallComponents.PDF.Document(GenererInvestissementImmediatActe(acte, codeCompagnie));
                        pdfProdActe.Pages.AddRange(pdfInvest.Pages.CloneToArray());
                        nbPage++;
                    }

                    string fileName = "Prod" + (TraitementEdi ? "Xml" : "") + "_" + codeCompagnie + (String.IsNullOrWhiteSpace(typeEnvoi) ? "" : ("_" + typeEnvoi.Trim())) + "_" + refActe + "_" + dateGeneration.ToString("yyyyMMddHHmm") + ".pdf";
                    using (FileStream file = new FileStream(completPath + fileName, FileMode.Create, FileAccess.Write))
                    {
                        etape = "Ecriture du PDF";
                        pdfProdActe.Write(file);
                    }

                    if (File.Exists(completPath + fileName))
                        File.Copy(completPath + fileName, completArchivePath + fileName, true);
                }
                etape = "Fin lecture des actes";
                refActe = "";

                etape = "";
                retour = true;
            }
            catch (Exception ex)
            {
                retour = false;
                string etapeMessage = "";
                if (refActe == "")
                    etapeMessage = " Etape: " + etape;
                else
                    etapeMessage = " Acte: " + refActe + " - Etape: " + etape;

                Log.Trace(IDProd, Log.MESSAGE_ERROR, ex.Message + etapeMessage);
            }
            return retour;
        }

        protected static bool GenererProdXML(string IDProd, string codeCompagnie, DateTime dateGeneration, List<Acte> listeActe, string typeEnvoi = "", bool genererProdActe = false, string classification = "")
        {
            bool retour = false;
            XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));

            try
            {
                Log.Trace(IDProd, Log.MESSAGE_INFO, "Génération de la Prod XML AEP");

                // gen xml header
                string archivePath = ConfigurationManager.AppSettings["envoiProdArchivePath"];
                string path = ConfigurationManager.AppSettings["envoiProdPath"];

                string dirName = "ProdXml" + "_" + codeCompagnie + (String.IsNullOrWhiteSpace(typeEnvoi) ? "" : ("_" + typeEnvoi.Trim())) + "_" + dateGeneration.ToString("yyyyMMddHHmm");
                string completPath = path + (String.IsNullOrWhiteSpace(classification) ? "" : (@"\" + classification.Trim()) + @"\") + (genererProdActe ? (dirName + @"\") : "");
                string completArchivePath = archivePath + codeCompagnie + (String.IsNullOrWhiteSpace(classification) ? "" : (@"\" + classification.Trim())) + @"\" + (genererProdActe ? (dirName + @"\") : "");

                if (!Directory.Exists(completPath))
                    Directory.CreateDirectory(completPath);

                if (!Directory.Exists(completArchivePath))
                    Directory.CreateDirectory(completArchivePath);


                string partner_id = ConfigurationManager.AppSettings["XML_AEP_partner_id"];
                string partner_name = ConfigurationManager.AppSettings["XML_AEP_partner_name"];
                string partner_contact = ConfigurationManager.AppSettings["XML_AEP_partner_contact"];
                string return_mail = ConfigurationManager.AppSettings["XML_AEP_return_mail"];
                string operation_type = ConfigurationManager.AppSettings["XML_AEP_operation_type"];
                string incoming_type = ConfigurationManager.AppSettings["XML_AEP_incoming_type"];
                string status = ConfigurationManager.AppSettings["XML_AEP_status"];
                string type_pr100 = ConfigurationManager.AppSettings["XML_AEP_type_pr100"];
                string type_montant = ConfigurationManager.AppSettings["XML_AEP_type_montant"];
                string invest_allocation_type = ConfigurationManager.AppSettings["XML_AEP_invest_allocation_type"];
                string invest_allocation_value = ConfigurationManager.AppSettings["XML_AEP_invest_allocation_value"];
                string invest_modality = ConfigurationManager.AppSettings["XML_AEP_invest_modality"];

                XElement xml_root = new XElement("root");
                XElement xml_demand_list = new XElement("demand_list");

                foreach (Acte acte in listeActe)
                {
                    XElement xml_demand = new XElement("demand");

                    //rubrique "technical"
                    XElement xml_technical = new XElement("technical");
                    xml_technical.Add(
                        new XElement("partner_id", partner_id), //Identifiant du partenaire
                        new XElement("partner_name", partner_name), //Nom du partenaire
                        new XElement("partner_contact", partner_contact), //Contact chez le partenaire
                        new XElement("demand_date", dateGeneration.ToString("yyyy-MM-dd")), //Date d'émission de la demande (jour J)
                        new XElement("effect_date", GED.Tools.Tools.GetAjouterJourOuvrable(dateGeneration, 1).ToString("yyyy-MM-dd")), //Date d'effet de la demande (jour J+1)
                        new XElement("return_mail", return_mail), //Email pour envoi des retours
                        new XElement("operation_type", operation_type), //Type d'opération (48:Arbitrage)
                        new XElement("operation_id", acte.ReferenceInterne), //Identifiant partenaire opération (Reference Interne)
                        new XElement("incoming_type", incoming_type) //Type de Format (1:XML)
                    );


                    //rubrique "data"
                    //support à desinvestire
                    XElement desinv_fund_list = new XElement("desinv_fund_list");
                    foreach (Repartition support in acte.ListeSupportDesinvestir)
                    {
                        XElement xml_desinv_fund = new XElement("desinv_fund");
                        xml_desinv_fund.Add(new XElement("fund_id", support.CodeISIN)); //Identifiant du support
                                                                                        //xml_desinv_fund.Add(new XElement("allocation_type",allocation_type)); //Type de répartition (15:%)
                                                                                        //xml_desinv_fund.Add(new XElement("allocation_value", support.ValeurRepartition)); //Valeur de la répartition
                        xml_desinv_fund.Add(new XElement("allocation_type", (support.TypeRepartition == "%" ? type_pr100 : type_montant))); //Type de répartition (15:%, 16:montant)
                        xml_desinv_fund.Add(new XElement("allocation_value", (support.TypeRepartition == "%" ? (support.ValeurRepartition / 100).ToString().Replace(',', '.') : support.ValeurRepartition.ToString().Replace(',', '.')))); //Valeur de la répartition
                        desinv_fund_list.Add(xml_desinv_fund);
                    }

                    //support à investir
                    XElement fund_alloc_list = new XElement("fund_alloc_list");
                    foreach (Repartition support in acte.ListeSupportInvestir)
                    {
                        XElement xml_fund_alloc = new XElement("fund_alloc");
                        xml_fund_alloc.Add(new XElement("fund_id", support.CodeISIN)); //Identifiant du support
                                                                                       //xml_fund_alloc.Add(new XElement("allocation_type", allocation_type)); //Type de répartition (15:%)
                                                                                       //xml_fund_alloc.Add(new XElement("allocation_value", support.ValeurRepartition)); //Valeur de la répartition
                        xml_fund_alloc.Add(new XElement("allocation_type", (support.TypeRepartition == "%" ? type_pr100 : type_montant))); //Type de répartition (15:%, 16:montant)
                        xml_fund_alloc.Add(new XElement("allocation_value", (support.TypeRepartition == "%" ? (support.ValeurRepartition / 100).ToString().Replace(',', '.') : support.ValeurRepartition.ToString().Replace(',', '.')))); //Valeur de la répartition
                        fund_alloc_list.Add(xml_fund_alloc);
                    }

                    XElement xml_data = new XElement("data");
                    xml_data.Add(
                        new XElement("policy_id", acte.NumContrat), //N° de Contrat
                        new XElement("status", status), //Statut de la demande
                        new XElement("signature_date", acte.DateCreation.AddDays(-1).ToString("yyyy-MM-dd")), //Date de signature de la demande (jour J-1)
                        new XElement("receipt_date", acte.DateAcquisition.ToString("yyyy-MM-dd")), //Date de réception de la demande
                        new XElement("simple_switch",
                            new XElement("effect_date", GED.Tools.Tools.GetAjouterJourOuvrable(dateGeneration, 1).ToString("yyyy-MM-dd")), //Date d'effet arbitrage (jour J+1)
                            new XElement("charge_type", (acte.TypeFrais == "%" ? type_pr100 : type_montant)), //Type de frais (15:%, 16:montant)
                            new XElement("charge_value", (acte.TypeFrais == "%" ? (acte.Frais / 100).ToString().Replace(',', '.') : acte.Frais.ToString().Replace(',', '.'))), //Valeur des frais
                            new XElement("match_tag_list",
                                new XElement("match_tag",
                                    new XElement("desinv_prof_list",
                                        new XElement("desinv_prof",
                                            //new XElement("prd_profile_id", acte.ID_ProfilCompagnie), //Identifiant du profil de gestion chez la compagnie
                                            new XElement("prd_profile_id", acte.Get_ID_ProfilCompagnieCA()), //Identifiant du profil de gestion chez la compagnie
                                            desinv_fund_list //supports à desinvestir
                                        )
                                    ),
                                    new XElement("invest_prof_list",
                                        new XElement("invest_prof",
                                            //new XElement("prd_profile_id", acte.ID_ProfilCompagnie), //Identifiant du profil de gestion chez la compagnie
                                            new XElement("prd_profile_id", acte.Get_ID_ProfilCompagnieCA()), //Identifiant du profil de gestion chez la compagnie
                                            new XElement("allocation_type", invest_allocation_type), //Type de répartition (15:%)
                                            new XElement("allocation_value", invest_allocation_value), //Pourcentage de répartition (1:100%)
                                            new XElement("invest_modality", invest_modality), //Modalité d'investissement (21:Par Ligne de Support)
                                            fund_alloc_list //support à investire
                                        )
                                    )
                                )
                            )
                        )
                    );

                    xml_demand.Add(xml_technical, xml_data);
                    xml_demand_list.Add(xml_demand);
                }

                xml_root.Add(xml_demand_list);
                doc.Add(xml_root);

                //string name = "XML_" + codeCompagnie + "_" + dateGeneration.ToString("yyyyMMddHHmmss") + ".xml";
                string name = "XML_" + codeCompagnie + (String.IsNullOrWhiteSpace(typeEnvoi) ? "" : ("_" + typeEnvoi.Trim())) + "_" + dateGeneration.ToString("yyyyMMddHHmm") + ".xml";
                using (var writer = new System.Xml.XmlTextWriter(completPath + name, new UTF8Encoding(false)))
                {
                    doc.Save(writer); //UTF8 sans BOM
                }

                File.Copy(completPath + name, completArchivePath + name, true);

                retour = true;
            }
            catch (Exception ex)
            {
                retour = false;
                Log.Trace(IDProd, Log.MESSAGE_ERROR, ex.Message);
            }

            return retour;

        }













        protected static bool GenererRecap(string IDProd, string codeCompagnie, DateTime dateGeneration, List<Acte> listeActe, string typeEnvoi = "", bool TraitementEdi = false, bool genererProdActe = false, string classification = "")
        {
            bool retour = false;

            try
            {
                Log.Trace(IDProd, Log.MESSAGE_INFO, "Génération du fichier de Recap");

                string archivePath = ConfigurationManager.AppSettings["envoiProdArchivePath"];
                string path = ConfigurationManager.AppSettings["envoiProdPath"];

                string dirName = "Prod" + (TraitementEdi ? "Xml" : "") + "_" + codeCompagnie + (String.IsNullOrWhiteSpace(typeEnvoi) ? "" : ("_" + typeEnvoi.Trim())) + "_" + dateGeneration.ToString("yyyyMMddHHmm");
                string completPath = path + (String.IsNullOrWhiteSpace(classification) ? "" : (@"\" + classification.Trim()) + @"\") + (genererProdActe ? (dirName + @"\") : "");
                string completArchivePath = archivePath + codeCompagnie + (String.IsNullOrWhiteSpace(classification) ? "" : (@"\" + classification.Trim())) + @"\" + (genererProdActe ? (dirName + @"\") : "");

                if (!Directory.Exists(completPath))
                    Directory.CreateDirectory(completPath);

                if (!Directory.Exists(completArchivePath))
                    Directory.CreateDirectory(completArchivePath);

                string htmlRecap = Resources.RecapProd;

                int nbPageRecap = 0;
                string recap = "";
                foreach (Acte acte in listeActe)
                {
                    recap += "<tr>";
                    recap += "  <td>" + acte.ReferenceInterne + "</td>";
                    recap += "  <td>" + acte.NomCompletSouscripteurs + "</td>";
                    recap += "  <td>" + acte.NumContrat + "</td>";
                    recap += "  <td>" + acte.NomType + "</td>";
                    recap += "  <td>" + acte.NomEnveloppe + "</td>";
                    recap += "  <td>" + acte.NomApporteur + " (" + acte.CodeApporteur + ")" + "</td>";
                    recap += "  <td>" + acte.MontantBrut.ToString("0.00") + "€" + "</td>";
                    recap += "  <td>" + acte.Frais.ToString("0.00") + acte.TypeFrais + "</td>";
                    recap += "  <td>" + acte.NomActeAdministratif + "</td>";
                    if (acte.InvestissementImmediat)
                        recap += "  <td>" + (acte.ListeDocument.Count + 1).ToString() + "</td>";
                    else
                        recap += "  <td>" + acte.ListeDocument.Count.ToString() + "</td>";
                    if (acte.InvestissementImmediat)
                        recap += "  <td>" + (acte.NbPage() + 1).ToString() + "</td>";
                    else
                        recap += "  <td>" + acte.NbPage().ToString() + "</td>";
                    if (acte.Regul)
                        recap += "  <td>Régul</td>";
                    else
                        recap += "  <td></td>";
                    recap += "</tr>";

                    nbPageRecap += acte.NbPage();
                }

                htmlRecap = htmlRecap.Replace("#Recap", recap);
                htmlRecap = htmlRecap.Replace("#nbPageRecap", nbPageRecap.ToString());

                TallComponents.Web.Pdf.Document pdfRecap = new TallComponents.Web.Pdf.Document();
                pdfRecap.DefaultPageSize = new TallComponents.Web.PageSize(TallComponents.Web.PageSize.A4.Height, TallComponents.Web.PageSize.A4.Width);

                /*if (TraitementEdi)
                    name = "RecapXml_" + codeCompagnie + "_" + dateGeneration.ToString("yyyyMMddHHmm") + ".pdf";
                else
                    name = "Recap_" + codeCompagnie + "_" + dateGeneration.ToString("yyyyMMddHHmm") + ".pdf";*/

                string name = "Recap" + (TraitementEdi ? "Xml" : "") + "_" + codeCompagnie + (String.IsNullOrWhiteSpace(typeEnvoi) ? "" : ("_" + typeEnvoi.Trim())) + "_" + dateGeneration.ToString("yyyyMMddHHmm") + ".pdf";
                using (FileStream file = new FileStream(completPath + name, FileMode.Create, FileAccess.Write))
                {
                    pdfRecap.Write(htmlRecap, file);
                }

                File.Copy(completPath + name, completArchivePath + name, true);

                retour = true;
            }
            catch (Exception ex)
            {
                retour = false;
                Log.Trace(IDProd, Log.MESSAGE_ERROR, ex.Message + "" + ex.InnerException.Message);
            }

            return retour;
        }

        protected static Stream GenererInvestissementImmediatActe(Acte acte, string codeCompagnie)
        {
            MemoryStream myStream = new MemoryStream();

            try
            {
                MemoryStream ms = new MemoryStream(Resources.Investimmediat);
                TallComponents.PDF.Document docInter = new TallComponents.PDF.Document(ms);

                //Activate the javascript engine, so format actions will be executed.
                docInter.ScriptBehavior = ScriptBehavior.Format;

                string compagnie = codeCompagnie;
                string adresse1 = "";
                string adresse2 = "";

                switch (codeCompagnie)
                {
                    case "LMP":
                        compagnie = "LA MONDIALE";
                        adresse1 = "32, avenue Emile Zola";
                        adresse2 = "TSA 61022 MONS EN BAROEUL";

                        break;

                    case "LMEP":
                        compagnie = "LA MONDIALE EUROPARTNER SA";
                        adresse1 = "BP2122";
                        adresse2 = "L-1021 LUXEMBOURG";

                        break;
                    case "AEP":
                        compagnie = "AEP – Assurance Epargne Pension";
                        adresse1 = "76, rue de la Victoire";
                        adresse2 = "75009 PARIS";

                        break;
                    case "CNP":
                        compagnie = "CNP Assurances";
                        adresse1 = "4, place Raoul Dautry";
                        adresse2 = "75716 PARIS Cedex 15";

                        break;
                    case "SPI":
                        compagnie = "SPIRICA";
                        adresse1 = "31, rue Falguière";
                        adresse2 = "75015 PARIS";

                        break;

                    case "IWI":
                        compagnie = "IWI International Wealth Insurer S.A.";
                        adresse1 = "2, rue Nicolas Bové";
                        adresse2 = "L-1253 LUXEMBOURG";

                        break;
                }

                TextField myField = docInter.Fields["Compagnie"] as TextField;
                myField.Value = compagnie;

                myField = docInter.Fields["DateDemande"] as TextField;
                myField.Value = DateTime.Now.ToLongDateString();

                myField = docInter.Fields["Adresse1"] as TextField;
                myField.Value = adresse1;

                myField = docInter.Fields["Adresse2"] as TextField;
                myField.Value = adresse2;

                myField = docInter.Fields["NomEnveloppe"] as TextField;
                myField.Value = acte.NomEnveloppe;

                myField = docInter.Fields["NomPrenomSouscripteur"] as TextField;
                myField.Value = acte.NomCompletSouscripteurs + ".";

                //Flatten all form-data with the current value, except text-field field-name.
                foreach (Field field in docInter.Fields)
                {
                    foreach (Widget widget in field.Widgets)
                    {
                        widget.Persistency = WidgetPersistency.Flatten;
                    }
                }

                docInter.Write(myStream);
                myStream.Position = 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return myStream;
        }

        protected static Stream GenererIntercallaireActe(Acte acte)
        {
            MemoryStream myStream = new MemoryStream();

            try
            {
                MemoryStream ms = new MemoryStream(Resources.IntercalaireProd);
                TallComponents.PDF.Document docInter = new TallComponents.PDF.Document(ms);

                //Activate the javascript engine, so format actions will be executed.
                docInter.ScriptBehavior = ScriptBehavior.Format;

                TextField myField = docInter.Fields["LitRef"] as TextField;
                myField.Value = acte.ReferenceInterne;

                myField = docInter.Fields["LitStatut"] as TextField;
                if (acte.Regul)
                    myField.Value = "Régularisation";
                else
                    myField.Value = "Nouvel acte";

                myField = docInter.Fields["LitSouscripteur"] as TextField;
                myField.Value = acte.NomCompletSouscripteurs;

                myField = docInter.Fields["LitNcontrat"] as TextField;
                myField.Value = acte.NumContrat;

                myField = docInter.Fields["LitTypeMvt"] as TextField;
                myField.Value = acte.NomType;

                myField = docInter.Fields["LitProduit"] as TextField;
                myField.Value = acte.NomEnveloppe;

                myField = docInter.Fields["LitApporteur"] as TextField;
                myField.Value = acte.NomApporteur + " (" + acte.CodeApporteur + ")";

                myField = docInter.Fields["LitMontantBrut"] as TextField;
                myField.Value = acte.MontantBrut.ToString("0.00") + "€";

                myField = docInter.Fields["LitFrais"] as TextField;
                myField.Value = acte.Frais.ToString("0.00") + acte.TypeFrais;

                myField = docInter.Fields["LitTypeMvtAdmin"] as TextField;
                myField.Value = acte.NomActeAdministratif;

                myField = docInter.Fields["LitNbDoc"] as TextField;
                if (acte.InvestissementImmediat)
                    myField.Value = (acte.ListeDocument.Count + 1).ToString();
                else
                    myField.Value = acte.ListeDocument.Count.ToString();

                myField = docInter.Fields["LitNbPage"] as TextField;
                if (acte.InvestissementImmediat)
                    myField.Value = (acte.NbPage() + 1).ToString();
                else
                    myField.Value = acte.NbPage().ToString();

                myField = docInter.Fields["LitCommentaire"] as TextField;
                myField.Value = acte.Commentaire;

                //Flatten all form-data with the current value, except text-field field-name.
                foreach (Field field in docInter.Fields)
                {
                    foreach (Widget widget in field.Widgets)
                    {
                        widget.Persistency = WidgetPersistency.Flatten;
                    }
                }

                docInter.Write(myStream);
                myStream.Position = 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return myStream;
        }

        protected static bool AjouterBookmark(TallComponents.PDF.Document docPDF, int page, string nom)
        {
            bool retour = false;

            try
            {
                InternalDestination destination = new InternalDestination();
                destination.Page = docPDF.Pages[page - 1];
                destination.PageDisplay = PageDisplay.FitEntire;

                GoToAction action = new GoToAction(destination);
                Bookmark bookmark = new Bookmark(nom);
                bookmark.Actions.Add(action);

                docPDF.Bookmarks.Add(bookmark);

                retour = true;
            }
            catch (Exception ex)
            {
                retour = false;
            }

            return retour;
        }

        protected static bool EnvoyerReponseSF(string IDProd, bool retour, string message)
        {
            bool envoyer = false;
            string loginSF = ConfigurationManager.AppSettings["loginSF"];
            string mdpSF = ConfigurationManager.AppSettings["mdpSF"];
            string environementSF = ConfigurationManager.AppSettings["EnvironementSF"];
            string sIdSF = "";

            try
            {
                Log.Trace(IDProd, Log.MESSAGE_INFO, "Envoi de la reponse à SF");

                //Reponse à envoyer
                if (environementSF == "DEV")
                {
                    GED.Tools.wsdl_productionReturn_dev.ProductionStatusInput response = new GED.Tools.wsdl_productionReturn_dev.ProductionStatusInput();
                    response.ProductionId = IDProd;
                    if (retour)
                        response.ProductionStatus = "Success";
                    else
                        response.ProductionStatus = "Error";
                    response.ProductionMessage = message;

                    //identification
                    GED.Tools.wsdl_enterprise_dev.SforceService bdAuth = new GED.Tools.wsdl_enterprise_dev.SforceService();
                    GED.Tools.wsdl_enterprise_dev.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                    sIdSF = lr.sessionId;

                    //consomation du web service SF
                    GED.Tools.wsdl_productionReturn_dev.WS02ProductionReturnGEDtoSFDCService retProd = new GED.Tools.wsdl_productionReturn_dev.WS02ProductionReturnGEDtoSFDCService();
                    retProd.SessionHeaderValue = new GED.Tools.wsdl_productionReturn_dev.SessionHeader();
                    retProd.SessionHeaderValue.sessionId = sIdSF;

                    envoyer = (bool)retProd.ProductionReturn(response);
                }
                else if (environementSF == "QUALIF")
                {
                    GED.Tools.wsdl_productionReturn_qualif.ProductionStatusInput response = new GED.Tools.wsdl_productionReturn_qualif.ProductionStatusInput();
                    response.ProductionId = IDProd;
                    if (retour)
                        response.ProductionStatus = "Success";
                    else
                        response.ProductionStatus = "Error";
                    response.ProductionMessage = message;

                    //identification
                    GED.Tools.wsdl_enterprise_qualif.SforceService bdAuth = new GED.Tools.wsdl_enterprise_qualif.SforceService();
                    GED.Tools.wsdl_enterprise_qualif.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                    sIdSF = lr.sessionId;

                    //consomation du web service SF
                    GED.Tools.wsdl_productionReturn_qualif.WS02ProductionReturnGEDtoSFDCService retProd = new GED.Tools.wsdl_productionReturn_qualif.WS02ProductionReturnGEDtoSFDCService();
                    retProd.SessionHeaderValue = new GED.Tools.wsdl_productionReturn_qualif.SessionHeader();
                    retProd.SessionHeaderValue.sessionId = sIdSF;

                    envoyer = (bool)retProd.ProductionReturn(response);
                }
                else if (environementSF == "DEMO")
                {
                    GED.Tools.wsdl_productionReturn_demo.ProductionStatusInput response = new GED.Tools.wsdl_productionReturn_demo.ProductionStatusInput();
                    response.ProductionId = IDProd;
                    if (retour)
                        response.ProductionStatus = "Success";
                    else
                        response.ProductionStatus = "Error";
                    response.ProductionMessage = message;

                    //identification
                    GED.Tools.wsdl_enterprise_demo.SforceService bdAuth = new GED.Tools.wsdl_enterprise_demo.SforceService();
                    GED.Tools.wsdl_enterprise_demo.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                    sIdSF = lr.sessionId;

                    //consomation du web service SF
                    GED.Tools.wsdl_productionReturn_demo.WS02ProductionReturnGEDtoSFDCService retProd = new GED.Tools.wsdl_productionReturn_demo.WS02ProductionReturnGEDtoSFDCService();
                    retProd.SessionHeaderValue = new GED.Tools.wsdl_productionReturn_demo.SessionHeader();
                    retProd.SessionHeaderValue.sessionId = sIdSF;

                    envoyer = (bool)retProd.ProductionReturn(response);
                }
                else if (environementSF == "PROD")
                {
                    GED.Tools.wsdl_productionReturn.ProductionStatusInput response = new GED.Tools.wsdl_productionReturn.ProductionStatusInput();
                    response.ProductionId = IDProd;
                    if (retour)
                        response.ProductionStatus = "Success";
                    else
                        response.ProductionStatus = "Error";
                    response.ProductionMessage = message;

                    //identification
                    GED.Tools.wsdl_enterprise.SforceService bdAuth = new GED.Tools.wsdl_enterprise.SforceService();
                    GED.Tools.wsdl_enterprise.LoginResult lr = bdAuth.login(loginSF, mdpSF);
                    sIdSF = lr.sessionId;


                    //consomation du web service SF
                    GED.Tools.wsdl_productionReturn.WS02ProductionReturnGEDtoSFDCService retProd = new GED.Tools.wsdl_productionReturn.WS02ProductionReturnGEDtoSFDCService();
                    retProd.SessionHeaderValue = new GED.Tools.wsdl_productionReturn.SessionHeader();
                    retProd.SessionHeaderValue.sessionId = sIdSF;

                    envoyer = (bool)retProd.ProductionReturn(response);
                }
                else
                    envoyer = false;
            }
            catch (Exception ex)
            {
                envoyer = false;
                Log.Trace(IDProd, Log.MESSAGE_ERROR, ex.Message);
            }

            return envoyer;
        }
    }
}