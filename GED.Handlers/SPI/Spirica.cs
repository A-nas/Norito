using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// JSON & DB CONNEXION
using System.Data.SqlClient;
using System.Configuration;
using Newtonsoft.Json;
using System.Data;
// envoie SFTP
using Renci.SshNet;
using System.IO;
using System.Threading;
// a effecer apres
using System.Windows.Forms;
// call web service (not tested yet)
using System.Net.Http;
using System.Net.Http.Headers;
// for converting Object into byte[]
using System.Runtime.Serialization.Formatters.Binary;



namespace GED.Handlers
{
    // a revoir apres il y'a du code a optimiser *****
    public class Spirica : Acte, IActe
    {
        // proprietés suplementaires
        [JsonProperty(PropertyName = "support_saisie", Order = 5)]
        private static string supsaisie = "bo";
        [JsonProperty(PropertyName = "pieces", Order = 7)]
        public List<DetailPiece> pieces = new List<DetailPiece>();
        [JsonProperty(PropertyName = "date_signature", Order = 2)]
        public string dateDeSignature;
        List<binaries> binaires;

        // generate JSON string for the current instance
        private string genJson() {
            JsonSerializerSettings jsonSetting = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new ShouldSerializeContractResolver()
            };
            return JsonConvert.SerializeObject(this, jsonSetting);
        }


        /*
        // fetch for all files including name,extension,binaries for the current (this) "NumContrat"
        private List<binaries> fetchPieces() {

            List<binaries> bins = new List<binaries>();
            SqlConnection con = Definition.connexion;
            SqlCommand cmd = new SqlCommand("select [Nom],[Extension],[Datas] from pli where CleSalesForce = @id_contrat", con);
            cmd.Parameters.AddWithValue("@id_contrat", (Object)this.ReferenceInterne ?? DBNull.Value);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                bins.Add(new binaries
                {
                    nomFichie = reader[0].ToString(),
                    extention = reader[1].ToString(),
                    ficheirPDF = (byte[])reader[2]
                });
            }
            reader.Close();
            con.Close();
            return bins;
        }*/



        /*
        //cette focntion se base sur le jeux de tests crée, elle attache des pieces statiques
        private List<binaries> fetchPiecesTest() {

            List<binaries> bins = new List<binaries>();
            // attachement des fichier de bases
            bins.Add(new binaries
            {
                nomFichie = "demande",
                extention = ".pdf",
                ficheirPDF = File.ReadAllBytes(@"C:\Users\alaghouaouta\Desktop\LastTestUntilRefactoring\demande.pdf")
            });
            bins.Add(new binaries
            {
                nomFichie = "dossier_arbitrage",
                extention = ".pdf",
                ficheirPDF = File.ReadAllBytes(@"C:\Users\alaghouaouta\Desktop\LastTestUntilRefactoring\demande.pdf")
            });
            // attachement d'avenant en cas d'avenant attaché
            if (this.ReferenceInterne == "TEST_FINAL01" || this.ReferenceInterne == "TEST_FINAL02") {
                bins.Add(new binaries
                {
                    nomFichie = "avenant_support",
                    extention = ".pdf",
                    ficheirPDF = File.ReadAllBytes(@"C:\Users\alaghouaouta\Desktop\LastTestUntilRefactoring\demande.pdf")
                });
            }
            return bins;
        }*/


        // Async methode to call RESTful Sylvea API, this method return string type when the call is finished, TASK<string> else.
        public async Task<string> sendProd(){

            // preparing request HEADERS
            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient();
            byte[] Basic_Auth = Encoding.ASCII.GetBytes(Definition.id + ":" + Definition.pass); // tester cet appel je vais pas y revenir une autre fois.
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Basic_Auth));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Charset", "UTF-8");
            //preparing request BODY
            MultipartFormDataContent requestContent = new MultipartFormDataContent();
            ByteArrayContent json = new ByteArrayContent(Encoding.UTF8.GetBytes(genJson())); // encodage a verifier apres !!
            json.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            requestContent.Add(json, "arbitrage");

            foreach (binaries bin in this.binaires)
            {
                var binaryFile = new ByteArrayContent(bin.ficheirPDF);
                binaryFile.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                requestContent.Add(binaryFile, "file", bin.nomFichie + bin.extention );
            }
            //POST ASYNC CALL
            HttpResponseMessage message = await client.PostAsync(Definition.url + this.NumContrat + "/arbitrages", requestContent);
            string content = await message.Content.ReadAsStringAsync();
            //here We are waiting until we get the JSON response !
            return content;
        }


        //#############################################################################################################################//

        /*
        // pointeur de fonction PreProcessInformation appelé quand l'envoie asynchrone est fini
        private delegate void finishTask(SftpClient cli, FileStream fs, IAsyncResult ar);*/

        public Spirica() { }

        // pour passse
        public Spirica(Acte acte) {

            this.NomType = acte.NomType;
            this.NomActeAdministratif = acte.NomActeAdministratif;
            this.ReferenceInterne = acte.ReferenceInterne;
            this.NomCompletSouscripteurs = acte.NomCompletSouscripteurs;
            this.NumContrat = acte.NumContrat;
            this.CodeApporteur = acte.CodeApporteur;
            this.NomApporteur = acte.NomApporteur;
            this.MontantBrut = acte.MontantBrut;
            this.TypeFrais = acte.TypeFrais;
            this.Frais = acte.Frais;
            this.ID_ProfilCompagnie = acte.ID_ProfilCompagnie;
            this.NomEnveloppe = acte.NomEnveloppe;
            this.ListeSupportDesinvestir = acte.ListeSupportDesinvestir;
            this.ListeSupportInvestir = acte.ListeSupportInvestir;
            this.ListeDocument = acte.ListeDocument;
            this.IsTraitementEdi = acte.IsTraitementEdi;
            this.DateCreation = acte.DateCreation;
            this.DateAcquisition = acte.DateAcquisition;
            this.Commentaire = acte.Commentaire;
            this.InvestissementImmediat = acte.InvestissementImmediat;
            this.Regul = acte.Regul;
            this.dateDeSignature = DateEnvoiProduction.ToString("dd/MM/yyyy");
            this.binaires = new List<binaries>();
            //alimenter les props manquées
            fillData();
        }

        private void fillData(){
            // remplissage de pieces
            int i = 0;
            List<DetailPiece> pieces = new List<DetailPiece>();
            int[] idDocs = new int[base.ListeDocument.Count()];
            foreach (DocumentProduction p in base.ListeDocument)
            {
                idDocs[i] = p.ID_DocumentNortia;
                i++;
            }


            if(idDocs.Length > 0)
            {
            var cmd = new SqlCommand("SELECT cam.nom [Nom de fichier] ,cam.datas [Fichier PDF binaire],tdt.code_type_document_externe [Type de Document] from type_document td "
                                     + "JOIN CA_MEDIA cam on cam.id_type_document=td.id_type_document "
                                     + "JOIN TYPE_DOC_TRANSTYPE tdt on tdt.code_type_document = td.ID_Type_Document "
                                     + "where cam.pk in ({ID_Document}) ", Definition.connexionQualif);

                cmd.addArrayCommand(idDocs, "ID_Document");
                Definition.connexionQualif.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        this.pieces.Add(new DetailPiece
                        {
                            nomFichier = reader[0].ToString(),
                            typeFicher = reader[2].ToString()
                        });
                        this.binaires.Add(new binaries
                        {
                            nomFichie = reader[0].ToString(),
                            ficheirPDF = (byte[])reader[1]
                        });
                    }
                    reader.Close();
                    Definition.connexionQualif.Close();
            }

            // end remplissage de pieces

            // transtypage de supports
            var instance = Production.getInstance();
            Dictionary<string,string> dicto = instance.TRANSTYPE;
            foreach (Repartition rep in base.ListeSupportDesinvestir.Concat(ListeSupportInvestir)) {
              if (dicto.ContainsKey(rep.CodeISIN)){
                        rep.code_support_ext = dicto[rep.CodeISIN];
                    }else{
                        rep.code_support_ext = rep.CodeISIN;
                    }
                }

            Definition.connexionQualif.Close();
            // end transtypage de supports
        }



    }
}

public class binaries
{
    public byte[] ficheirPDF;
    public string nomFichie;
    public string extention;
}