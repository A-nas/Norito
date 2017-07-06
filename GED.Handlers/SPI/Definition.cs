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
        public static List<string> pptActeNames = new List<string> { "reference_externe", "desinvestissements", "reinvestissements", "pieces", "commentaire", "support_saisie", "code_support", "pourcentage", "montant", "nom", "type", "date_signature" };

        // DATABASE STRING
        public static readonly SqlConnection connexion = new SqlConnection("data source=192.168.1.2\\SQL2005DEV;Database=Nortiaca_MEDIA;Uid=sa;password=NICKEL2000;");
        //public static readonly SqlConnection connexionProd = new SqlConnection("data source=192.168.1.5\\DW;Database=Nortiaca_MEDIA;Uid=sa;password=NICKEL2000;");
        public static readonly SqlConnection connexionQualif = new SqlConnection("data source=192.168.1.2\\SQL2005qualif;Database=Nortiaca_MEDIA;Uid=sa;password=NICKEL2000;");

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
            
            List<Acte> la = new List<Acte>();
            // sub object
            {
                Repartition rep01 = new Repartition
                {
                    CodeISIN = "NL0000235190",
                    TypeRepartition = "%",
                    ValeurRepartition = 10
                };

                Repartition rep02 = new Repartition
                {
                    CodeISIN = "FR0010220475",
                    TypeRepartition = "%",
                    ValeurRepartition = 60
                };

                Repartition rep03 = new Repartition
                {
                    CodeISIN = "LU0323134006",
                    TypeRepartition = "%",
                    ValeurRepartition = 40
                };

                DocumentProduction doc01 = new DocumentProduction
                {
                    ID_DocumentNortia = 890350,
                    ID_DocumentSalesForce = "idSalesForce"
                };

                DocumentProduction doc02 = new DocumentProduction
                {
                    ID_DocumentNortia = 890371,
                    ID_DocumentSalesForce = "idSalesForce"
                };
                //==
                DetailPiece piece01 = new DetailPiece
                {
                    nomFichier = "demande.pdf",
                    typeFicher = "demande_arbitrage" // penser a la modification des types
                };

                DetailPiece piece02 = new DetailPiece
                {
                    nomFichier = "dossier_arbitrage.pdf",
                    typeFicher = "dossier_arbitrage_signe_electroniquement" // penser a la modification des types
                };

                DetailPiece piece03 = new DetailPiece
                {
                    nomFichier = "avenant_support.pdf",
                    typeFicher = "avenant_support" // penser a la modification des types
                };
                //==

                la.Add(new Acte
                {
                    ReferenceInterne = "TEST_FINAL01",
                    Frais = 0f,
                    ListeSupportDesinvestir = { rep01 },
                    ListeSupportInvestir = { rep02, rep03 },
                    Commentaire = "un commentaire",
                    //pieces = { piece01, piece02, piece03 },
                    //ListeDocument = { doc01, doc02 }
                    NumContrat = "113100096"
                });
            }

            //deuxiemmme acte
            // sub object
            {
                Repartition rep01 = new Repartition
                {
                    CodeISIN = "CARPAT",
                    TypeRepartition = "%",
                    ValeurRepartition = 50
                };

                Repartition rep02 = new Repartition
                {
                    CodeISIN = "PATRIMMOCO",
                    TypeRepartition = "%",
                    ValeurRepartition = 50
                };

                DocumentProduction doc01 = new DocumentProduction
                {
                    ID_DocumentNortia = 111,
                    ID_DocumentSalesForce = "idSalesForce2"
                };

                DocumentProduction doc02 = new DocumentProduction
                {
                    ID_DocumentNortia = 222,
                    ID_DocumentSalesForce = "idSalesForce1"
                };

                //==
                DetailPiece piece01 = new DetailPiece
                {
                    nomFichier = "demande.pdf",
                    typeFicher = "demande_arbitrage" // penser a la modification des types
                };

                DetailPiece piece02 = new DetailPiece
                {
                    nomFichier = "dossier_arbitrage.pdf",
                    typeFicher = "dossier_arbitrage_signe_electroniquement" // penser a la modification des types
                };

                DetailPiece piece03 = new DetailPiece
                {
                    nomFichier = "avenant_support.pdf",
                    typeFicher = "avenant_support" // penser a la modification des types
                };
                //==
                la.Add(new Acte
                {
                    ReferenceInterne = "TEST_FINAL02",
                    ListeSupportInvestir = { rep01 , rep02 },
                    Commentaire = "un commentaire",
                    //pieces = { piece01, piece02, piece03 },
                    //ListeDocument = { doc01, doc02 },
                    NumContrat = "113100096"
                });
            }


            //3eme acte
            // sub object
            {
                Repartition rep01 = new Repartition
                {
                    CodeISIN = "PATRIMMOCO",
                    TypeRepartition = "%",
                    ValeurRepartition = 50
                };

                Repartition rep02 = new Repartition
                {
                    CodeISIN = "HAASGPIF100",
                    TypeRepartition = "%",
                    ValeurRepartition = 100
                };

                DocumentProduction doc01 = new DocumentProduction
                {
                    ID_DocumentNortia = 111,
                    ID_DocumentSalesForce = "idSalesForce2"
                };

                DocumentProduction doc02 = new DocumentProduction
                {
                    ID_DocumentNortia = 222,
                    ID_DocumentSalesForce = "idSalesForce1"
                };

                //==
                DetailPiece piece01 = new DetailPiece
                {
                    nomFichier = "demande.pdf",
                    typeFicher = "demande_arbitrage" // penser a la modification des types
                };

                DetailPiece piece02 = new DetailPiece
                {
                    nomFichier = "dossier_arbitrage.pdf",
                    typeFicher = "dossier_arbitrage_signe_electroniquement" // penser a la modification des types
                };
                //==
                la.Add(new Acte
                {
                    ReferenceInterne = "TEST_FINAL03",
                    ListeSupportInvestir = { rep02 },
                    ListeSupportDesinvestir = { rep01 },
                    Commentaire = "un commentaire",
                    //ListeDocument = { doc01, doc02 },
                    Frais = 0.5f,
                    //pieces = { piece01, piece02 },
                    NumContrat = "113100096"
                });
            }


            //4eme acte
            // sub object
            {
                Repartition rep01 = new Repartition
                {
                    CodeISIN = "FONDSGENERAL",
                    TypeRepartition = "%",
                    ValeurRepartition = 50
                };

                Repartition rep02 = new Repartition
                {
                    CodeISIN = "ACCOMO",
                    TypeRepartition = "%",
                    ValeurRepartition = 50
                };

                DocumentProduction doc01 = new DocumentProduction
                {
                    ID_DocumentNortia = 111,
                    ID_DocumentSalesForce = "idSalesForce2"
                };

                DocumentProduction doc02 = new DocumentProduction
                {
                    ID_DocumentNortia = 222,
                    ID_DocumentSalesForce = "idSalesForce1"
                };

                //==
                DetailPiece piece01 = new DetailPiece
                {
                    nomFichier = "demande.pdf",
                    typeFicher = "demande_arbitrage" // penser a la modification des types
                };

                DetailPiece piece02 = new DetailPiece
                {
                    nomFichier = "dossier_arbitrage.pdf",
                    typeFicher = "dossier_arbitrage_signe_electroniquement" // penser a la modification des types
                };
                //==
                la.Add(new Acte
                {
                    ReferenceInterne = "TEST_FINAL04",
                    ListeSupportInvestir = { rep01, rep02 },
                    Commentaire = "un commentaire",
                    //pieces = { piece01, piece02 },
                    //ListeDocument = { doc01, doc02 },
                    NumContrat = "113100096"
                });
            }

            return la;
        }
    }
}
