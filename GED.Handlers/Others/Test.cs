using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration;
using System.Timers;
using System.IO;
using System.IO.Compression;
using GED.Handlers;
using GED.Tools;
//using IntegrationAvenant.Properties;
using System.Web;
using System.Data.SqlClient;

namespace GED.Handlers
{
    public class Test
    {


        public int IntegrerCAT_BIS()
        {
            int nbDoc = 0;

            try
            {
                string pathTmp = @"C:\Users\alaghouaouta\Desktop\TEST_INTEGRATION\CATtemp\"; //ConfigurationManager.AppSettings["CheminTmpAvenantCAT"];
                if (!Directory.Exists(pathTmp))
                    Directory.CreateDirectory(pathTmp);

                string pathArchive = @"C:\Users\alaghouaouta\Desktop\TEST_INTEGRATION\Archive\"; //ConfigurationManager.AppSettings["CheminArchiveAvenantCAT"];
                if (!Directory.Exists(pathArchive))
                    Directory.CreateDirectory(pathArchive);

                string pathRejet = @"C:\Users\alaghouaouta\Desktop\TEST_INTEGRATION\Rejet\"; //ConfigurationManager.AppSettings["CheminRejetAvenantCAT"];
                if (!Directory.Exists(pathRejet))
                    Directory.CreateDirectory(pathRejet);

                string path = @"C:\Users\alaghouaouta\Desktop\TEST_INTEGRATION\"; // ConfigurationManager.AppSettings["CheminAvenantCAT"];
                string avCAT = "GDNO01P_20??????????"; //ConfigurationManager.AppSettings["FormatFichierAvenantCAT"];

                string pathAvCAT = @"C:\Users\alaghouaouta\Desktop\TEST_INTEGRATION\CAT\Tests\"; // ConfigurationManager.AppSettings["CheminDepotAvenantCAT"];
                if (!Directory.Exists(pathAvCAT))
                    Directory.CreateDirectory(pathAvCAT);

                //serviceLog.WriteEntry("Recherche de nouveaux fichiers CAT à intégrer");
                List<FichierAvenantZIP> listeFichierAvCAT = RechercheFichierAvenant(path, "", avCAT, "CAT");

                //decompression des fichiers avenant
                if (listeFichierAvCAT.Count > 0)
                    //serviceLog.WriteEntry("Décompression des archives des situations: " + listeFichierAvCAT.Count.ToString());

                foreach (FichierAvenantZIP fich in listeFichierAvCAT)
                {
                    string cheminTo = pathTmp + fich.NomFichier;

                    try
                    {
                        ZipFile.ExtractToDirectory(fich.CheminFichier, cheminTo);
                        fich.DezipOK = true;
                    }
                    catch (Exception ex)
                    {
                        //serviceLog.WriteEntry("Erreur : Impossible de dézipper le fichier " + fich.CheminFichier, EventLogEntryType.Error);

                        if (!RejetAvenant.Existe(fich.NomFichier, "", "ArchiveCorrompue"))
                            RejetAvenant.Ajouter(fich.NomFichier, "", "ArchiveCorrompue", "Impossible de décompresser l'archive");

                        File.Move(fich.CheminFichier, pathRejet + fich.NomFichier);
                    }

                    if (fich.DezipOK)
                        File.Move(fich.CheminFichier, pathArchive + fich.NomFichier);
                    //File.Delete(fich.CheminFichier);
                }

                //traitement des fichiers
                string nameRepAv = Path.GetFileNameWithoutExtension(avCAT);
                string[] listeRepAvCAT = System.IO.Directory.GetDirectories(pathTmp, nameRepAv);
                Array.Sort(listeRepAvCAT, StringComparer.InvariantCulture);

                /* mail pdfs*/
                DataTable OSTCatMailDT = new DataTable();
                OSTCatMailDT.Columns.Add("CodeCompagnie", typeof(string));
                OSTCatMailDT.Columns.Add("NumContrat", typeof(string));
                OSTCatMailDT.Columns.Add("IDTypeAvenant", typeof(int));
                OSTCatMailDT.Columns.Add("chemin", typeof(string));
                OSTCatMailDT.Columns.Add("codeRetour", typeof(int));
                /* end mail pdfs*/

                foreach (string rep in listeRepAvCAT)
                {
                    DirectoryInfo di = new DirectoryInfo(rep);
                    string[] listeFichierPDF = System.IO.Directory.GetFiles(rep, "*.pdf");
                    Array.Sort(listeFichierPDF, StringComparer.InvariantCulture);


                    //PDF
                    foreach (string fichierPDF in listeFichierPDF)
                    {
                        string nom = Path.GetFileNameWithoutExtension(fichierPDF);
                        string typeDoc = "";

                        try
                        {
                            int codeRetour = Avenant.Exist(di.Name, nom, "", "NI");
                            if (codeRetour <= 0)
                            {
                                Avenant av = new Avenant();
                                string[] colonne = nom.Split('_');
                                typeDoc = colonne[1];
                                /*###################################################*/
                                av.CodeCompagnie = "CAT";
                                av.NumAvenant = nom;
                                av.NumContrat = colonne[4];
                                av.TypeAvenant = colonne[1]; // prendre le code avec code insertion
                                av.NumLot = di.Name;

                                if (!String.IsNullOrWhiteSpace(av.NumContrat) && !String.IsNullOrWhiteSpace(av.TypeAvenant))
                                {
                                    string date = colonne[5];
                                    av.DateAvenant = new DateTime(Convert.ToInt32(date.Substring(0, 4)), Convert.ToInt32(date.Substring(4, 2)), Convert.ToInt32(date.Substring(6, 2)));

                                    av.Actif = true;

                                    string nomFichier = av.GetNomFichierAvenant("NI");
                                    // focntion qui alimente la base de données.
                                    codeRetour = av.Ajouter(fichierPDF, true, nomFichier, "NI");



                                    /*#########*/
                                    int IDTypeDoc = TypeDocument.GetIDTypeDocumentCompagnie(av.CodeCompagnie, av.TypeAvenant);
                                    OSTCatMailDT.Rows.Add(av.CodeCompagnie, av.NumContrat, IDTypeDoc, av.NumLot, codeRetour);
                                    //var = av.NumAvenant; destiné bdd ? 
                                    /*#########*/


                                    /*if (codeRetour > 0)
                                    {
                                        string idContratSF = Contrat.FindIdContratSF(av.NumContrat, "NI");
                                        if (!string.IsNullOrWhiteSpace(idContratSF))// à rajouter => le type doc est à exporter vers SF
                                            av.Indexer("Portefeuille", idContratSF, "", "NI");
                                    }*/
                                }
                                else
                                    codeRetour = -3;
                            }

                            if (codeRetour > 0)
                            {
                                FileInfo ff = new FileInfo(fichierPDF);

                                if (!System.IO.Directory.Exists(pathAvCAT + "\\" + di.Name))
                                    System.IO.Directory.CreateDirectory(pathAvCAT + "\\" + di.Name);

                                string fichierDest = pathAvCAT + "\\" + di.Name + "\\" + ff.Name;
                                if (System.IO.File.Exists(fichierDest))
                                    System.IO.File.Delete(fichierPDF);
                                else
                                    System.IO.File.Move(fichierPDF, fichierDest);

                                nbDoc++;
                            }
                            else if (codeRetour == 0)
                            {
                                //serviceLog.WriteEntry("Impossible d'insérer en base le document " + nom, EventLogEntryType.Error);
                                RejetAvenant.Ajouter(di.Name, nom, "AjoutAvenant", "Impossible d'insérer le fichier PDF de l'avenant en base de donnée");
                            }
                            else if (codeRetour == -1)
                            {
                                //serviceLog.WriteEntry("Le type de document " + typeDoc + " n'est pas référencé dans la table de transcodage CAT", EventLogEntryType.Warning);
                                if (!RejetAvenant.Existe(di.Name, nom, "TypeDocument"))
                                    RejetAvenant.Ajouter(di.Name, nom, "TypeDocument", "Le fichier PDF n'est pas référencé dans la table de transcodage de la compagnie");
                            }
                            else if (codeRetour == -2)
                            {
                                //log: Doc pas actif ou à effacer
                                System.IO.File.Delete(fichierPDF);
                            }
                            else if (codeRetour == -3)
                            {
                                //serviceLog.WriteEntry("Le numéro du contrat ou le type du document n'est pas renseigné dans le nom du PDF", EventLogEntryType.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            //serviceLog.WriteEntry("Erreur lors du traitement du PDF: " + ex.Message, EventLogEntryType.Error);
                        }
                    }

                    if (System.IO.Directory.GetFiles(rep, "*.pdf").Count() == 0)
                    {
                        if (GED.Tools.Tools.DirectoryCopy(rep, pathAvCAT + "\\" + di.Name, true))
                        {
                            try
                            {
                                System.IO.Directory.Delete(rep, true);
                            }
                            catch (Exception ex)
                            {
                                RejetAvenant.Ajouter("Nortia.IntegrationAvenant", "", "Service", "Impossible de suprimer le répertoire " + rep + ": " + ex.Message);
                            }
                        }
                    }
                }
                //envoie mail ost
                OSTCatMailSender(OSTCatMailDT, 42);
            }
            catch (Exception ex)
            {
                //serviceLog.WriteEntry("Erreur IntegrerCAT(): " + ex.Message, EventLogEntryType.Error);
                Console.WriteLine(ex.Message);
            }

            return nbDoc;
        }

        private void OSTCatMailSender(DataTable dt, int idTypeDocument)
        {
            //extract avenants OST
            DataRow[] record = dt.Select("IDTypeAvenant =" + idTypeDocument.ToString()); //coderetour est ignorée
            if (record.Length != 0)
            {
                // build html body
                string body = HttpUtility.HtmlAttributeEncode(
                                "<!DOCTYPE HTML>"
                              + "<html>"
                              + "<head>"
                              + "<meta charset=\"UTF - 8\">"
                              + "<title>Avenants OST</title>"
                              + "<style>table{text-align: center;} div{margin : 50px;} </style>"
                              + "</head>"
                              + "<body>"
                              + "<strong>Liste des avenants : opération sur titres reçus: </strong>"
                              + "<div>"
                              + "<table border=\"1\" cellpadding=\"0\" cellspacing=\"0\" width=\"200px\" style=\"border - collapse:collapse; \">"
                              + "<tr>"
                              + "<th>Compagnie</th>"
                              + "<th>Numero de Contrat</th>"
                              + "<th>Dossier d'Emplacement</th>"
                              + "</tr>");
                foreach (DataRow r in record)
                {
                         body += "<tr>"
                              + "<th>" + r[0] + "</th>"
                              + "<th>" + r[1] + "</th>"
                              + "<th>" + r[3] + "</th>"
                              + "</tr>";
                }
                         body += "</table>"
                              + "</div>"
                              + "<div> ce mail a été généré automatiquement! </div>"
                              + "<body>"
                              + "</html>";


                // insert into mailEngine
                SqlConnection cn = new SqlConnection("data source = 192.168.1.2\\SQL2005DEV; Database = BDDTest; Uid = sa; password = NICKEL2000;");
                string query = "INSERT INTO mail.[MailEngine] ([From_Mail],[To_Mail],[Body_Mail],[Subject_Mail],[TEK_dateCreation]) "
                                + " VALUES('alaghouaouta@nortia.fr','anass.laghouaouta@gmail.fr',@Body_Mail,'subject','" + DateTime.Now + "');";

                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.Parameters.AddWithValue("@Body_Mail", (object)HttpUtility.HtmlDecode(body) ?? DBNull.Value);
                cn.Open();
                cmd.ExecuteScalar();
                cn.Close();

            }

            

        }



        public List<FichierAvenantZIP> RechercheFichierAvenant(string cheminRecherche, string formatRepertoire, string formatFichier, string compagnie)
        {
            List<FichierAvenantZIP> listeFichiersAv = new List<FichierAvenantZIP>();

            try
            {
                if (formatRepertoire.Trim() == "")
                {
                    string[] listeFichier = System.IO.Directory.GetFiles(cheminRecherche, formatFichier);
                    foreach (string fichier in listeFichier)
                    {
                        if (!GED.Tools.Tools.FileIsOpen(fichier))
                        {
                            string fichierName = Path.GetFileNameWithoutExtension(fichier);

                            int annee = 0;
                            int mois = 0;
                            int jour = 0;
                            int heure = 0;
                            int minute = 0;

                            if (compagnie == "SPI")
                            {
                                annee = Convert.ToInt32(fichierName.Substring(6, 4));
                                mois = Convert.ToInt32(fichierName.Substring(10, 2));
                                jour = Convert.ToInt32(fichierName.Substring(12, 2));
                                heure = 0;
                                minute = 0;
                            }
                            else if (compagnie == "LUX")
                            {
                                annee = Convert.ToInt32(fichierName.Substring(10, 4));
                                mois = Convert.ToInt32(fichierName.Substring(14, 2));
                                jour = Convert.ToInt32(fichierName.Substring(16, 2));
                                heure = Convert.ToInt32(fichierName.Substring(18, 2));
                                minute = Convert.ToInt32(fichierName.Substring(20, 2));
                            }
                            else if (compagnie == "CARDIF")
                            {
                                annee = Convert.ToInt32(fichierName.Substring(13, 4));
                                mois = Convert.ToInt32(fichierName.Substring(17, 2));
                                jour = Convert.ToInt32(fichierName.Substring(19, 2));
                                heure = Convert.ToInt32(fichierName.Substring(21, 2));
                                minute = Convert.ToInt32(fichierName.Substring(23, 2));
                            }
                            else if (compagnie == "CAT")
                            {
                                annee = Convert.ToInt32(fichierName.Substring(8, 4));
                                mois = Convert.ToInt32(fichierName.Substring(12, 2));
                                jour = Convert.ToInt32(fichierName.Substring(14, 2));
                                heure = Convert.ToInt32(fichierName.Substring(16, 2));
                                minute = Convert.ToInt32(fichierName.Substring(18, 2));
                            }

                            FichierAvenantZIP ficSitu = new FichierAvenantZIP();
                            ficSitu.CheminFichier = fichier;
                            ficSitu.NomRepertoire = cheminRecherche;
                            ficSitu.NomFichier = fichierName;
                            ficSitu.DezipOK = false;
                            ficSitu.DateHeure = new DateTime(annee, mois, jour, heure, minute, 0);

                            listeFichiersAv.Add(ficSitu);
                        }
                    }
                }
                else
                {
                    string[] listeRep = System.IO.Directory.GetDirectories(cheminRecherche, formatRepertoire);
                    foreach (string rep in listeRep)
                    {
                        DirectoryInfo di = new DirectoryInfo(rep);
                        string repName = di.Name;

                        if (repName.Length == formatRepertoire.Length)
                        {
                            int annee = Convert.ToInt32(repName.Substring(0, 4));
                            int mois = Convert.ToInt32(repName.Substring(4, 2));
                            int jour = Convert.ToInt32(repName.Substring(6, 2));

                            string[] listeFichier = System.IO.Directory.GetFiles(rep, formatFichier);
                            foreach (string fichier in listeFichier)
                            {
                                if (!GED.Tools.Tools.FileIsOpen(fichier))
                                {
                                    string fichierName = Path.GetFileNameWithoutExtension(fichier);

                                    FichierAvenantZIP ficZip = new FichierAvenantZIP();
                                    ficZip.CheminFichier = fichier;
                                    ficZip.NomRepertoire = repName;
                                    ficZip.NomFichier = fichierName;
                                    ficZip.DezipOK = false;

                                    int heure = Convert.ToInt32(fichierName.Substring(fichierName.Length - 4, 2));
                                    int minute = Convert.ToInt32(fichierName.Substring(fichierName.Length - 2, 2));

                                    ficZip.DateHeure = new DateTime(annee, mois, jour, heure, minute, 0);

                                    listeFichiersAv.Add(ficZip);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //serviceLog.WriteEntry("Erreur RechercheFichierAvenant(\"" + cheminRecherche + "\",\"" + formatRepertoire + "\",\"" + formatFichier + "\"): " + ex.Message, EventLogEntryType.Error);
                listeFichiersAv = new List<FichierAvenantZIP>();
            }

            return listeFichiersAv.OrderBy(FichierAvenantZIP => FichierAvenantZIP.DateHeure).ToList();
        }







    }// end class
}
