using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
// a enlever
using System.Windows.Forms;

namespace GED.Handlers
{
    public static class Definition
    {
        // #### CLASS TO HANDLE ALL MY TMP CONFIG VARS #### (a déplacé vers un fichier de configuration)

        //SPIRICA PARAMS
        public static readonly string url = "https://api-recette.spirica.fr/sylveaRS/v1/contrats/"; //113100096/arbitrages";
        public static readonly string id = "NORTIAWS";
        public static readonly string pass = "a*yixw9.8sq"; // auth is base64(id:pass)

        // SFTP PARAMS
        public static readonly string sftpHost = "home663743708.1and1-data.host"; //home663743708.1and1-data.host
        public static readonly string sftpPswd = "ananass123";
        public static readonly string sftpUser = "u87840885-bot";
        public static readonly string sourcePath = "C:\\Users\\alaghouaouta\\Desktop\\Spirica Doc";
        public static readonly string remotePath = "//SPIRICA//"; //sftp://u87840885@home663743708.1and1-data.host/SPIRICA

        // SPIRICA SERIALIZABLE PROPERTIES
        public static List<string> pptActeNames = new List<string> { "reference_externe", "desinvestissements", "reinvestissements", "pieces", "commentaire", "support_saisie", "code_support", "pourcentage", "montant", "nom", "type" };

        // DATABASE STRING
        public static readonly SqlConnection connexion = new SqlConnection("data source=192.168.1.2\\SQL2005DEV;Database=Nortiaca_MEDIA;Uid=sa;password=NICKEL2000;");

        
        //THIS IS FOR TEST
        private static ProgressBar progBar;
        public static ProgressBar getProgressBar()
        {
            return progBar;
        }
        public static void setProgressBar(ProgressBar pb)
        {
            progBar = pb;
        }

        //
        public static List<Acte> la = new List<Acte>();
        public static List<Acte> GetListeActes()
        {
            Acte a = new Acte();
            List<Acte> la = new List<Acte>();
            la.Add(new Acte
            {
                ReferenceInterne = "referenceInterne",
                Frais = 0,
                ListeSupportDesinvestir = new List<Repartition>(),
                ListeSupportInvestir = new List<Repartition>(),
                Commentaire = "ceci est un commentaire",
                ListeDocument = new List<DocumentProduction>(),

            });

            Repartition rep01 = new Repartition
            {
                CodeISIN = "support01",
                TypeRepartition = "%",
                ValeurRepartition = 50
            };

            Repartition rep02 = new Repartition
            {
                CodeISIN = "support02",
                TypeRepartition = "€",
                ValeurRepartition = 100000
            };

            a.ListeSupportDesinvestir = new List<Repartition>(); // à.completer.
           // a.ListeSupportDesinvestir.Add(r1);                   // ............
            a.ListeSupportInvestir = new List<Repartition>();    // ...par.la...
            //a.ListeSupportInvestir.Add(r2);                      // ............
            a.ListeDocument = new List<DocumentProduction>();    // ...suite....
            a.Commentaire = "ceci est un commentaire";

            DocumentProduction doc1 = new DocumentProduction();
            DocumentProduction doc2 = new DocumentProduction();
            doc1.ID_DocumentNortia = 325;
            doc2.ID_DocumentNortia = 477;
            a.ListeDocument.Add(doc1);
            a.ListeDocument.Add(doc2);

            //deuxiemmme acte

            Acte a2 = new Acte();
            a2.ReferenceInterne = "referenceInterne";
            a2.Frais = 0;
            Repartition r12 = new Repartition();
            r12.CodeISIN = "support01";
            r12.TypeRepartition = "%";
            r12.ValeurRepartition = 50;

            Repartition r22 = new Repartition();
            r22.CodeISIN = "support02";
            r22.TypeRepartition = "€";
            r22.ValeurRepartition = 100000;

            a2.ListeSupportDesinvestir = new List<Repartition>(); // à.completer.
            a2.ListeSupportDesinvestir.Add(r22);                   // ............
            a2.ListeSupportInvestir = new List<Repartition>();    // ...par.la...
            a2.ListeSupportInvestir.Add(r22);                      // ............
            a2.ListeDocument = new List<DocumentProduction>();    // ...suite....
            a2.Commentaire = "ceci est un commentaire";

            DocumentProduction doc11 = new DocumentProduction();
            DocumentProduction doc22 = new DocumentProduction();
            doc11.ID_DocumentNortia = 304;
            doc22.ID_DocumentNortia = 306;
            a2.ListeDocument.Add(doc11);
            a2.ListeDocument.Add(doc22);

            la.Add(a);
            la.Add(a2);

            return new List<Acte>();
        }
    }
}
