using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// JSON & DB CONNEXION
using System.Data.SqlClient;
using System.Configuration;
using Newtonsoft.Json;
// envoie SFTP
using Renci.SshNet;
using System.IO;
using System.Threading;
// a effecer apres
using System.Windows.Forms;
// call web service (not tested yet)
using System.Net.Http;
using System.Net.Http.Headers;


namespace GED.Handlers
{
    public class Spirica : Acte,IActe
    {

        // generate JSON string for the current instance
        private string genJson(){
            JsonSerializerSettings jsonSetting = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new ShouldSerializeContractResolver()
            };
            return JsonConvert.SerializeObject(this, jsonSetting); // Replace(@"\", "");
        }


        // fetch for all files including name,extension,binaries for the current (this) "NumContrat"
        private List<binaries> fetchPieces(){

            List<binaries> bins = new List<binaries>();
            SqlConnection con = Definition.connexion;
            SqlCommand cmd = new SqlCommand("select [Nom],[Extension],[Datas] from pli where CleSalesForce = @id_contrat", con);
            cmd.Parameters.AddWithValue("@id_contrat", (Object) this.ReferenceInterne ?? DBNull.Value);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                bins.Add(new binaries
                {
                    nomFichie = reader[0].ToString(),
                    extention = reader[1].ToString(),
                    ficheirPDF = (byte[]) reader[2]
                });
            }
            reader.Close();
            con.Close();
            return bins;
        }


        //cette focntion se base sur le jeux de tests crée, elle attache des pieces statiques
        private List<binaries> fetchPiecesTest(){

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
            if (this.ReferenceInterne == "TEST_FINAL01" || this.ReferenceInterne == "TEST_FINAL02"){
                bins.Add(new binaries
                {
                    nomFichie = "avenant_support",
                    extention = ".pdf",
                    ficheirPDF = File.ReadAllBytes(@"C:\Users\alaghouaouta\Desktop\LastTestUntilRefactoring\demande.pdf")
            });
            }
            return bins;
        }


        // Async methode to call RESTful Sylvea API, this method return string type when the call is finished, TASK<string> else.
        public async Task<string> sendProd()
        {
            // preparing request HEADERS
            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient();
            byte[] Basic_Auth = Encoding.ASCII.GetBytes(Definition.id+":"+Definition.pass); // tester cet appel je vais pas y revenir une autre fois.
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Basic_Auth));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Charset", "UTF-8");
            //preparing request BODY
            MultipartFormDataContent requestContent = new MultipartFormDataContent();
            ByteArrayContent json = new ByteArrayContent(Encoding.UTF8.GetBytes(genJson())); // encodage a verifier apres !!
            json.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            requestContent.Add(json, "arbitrage");
            //List<binaries> bins = fetchPieces();
            List<binaries> bins = fetchPiecesTest();
            foreach (binaries bin in bins)
            {
                var binaryFile = new ByteArrayContent(bin.ficheirPDF);
                binaryFile.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                requestContent.Add(binaryFile, "file", bin.nomFichie + bin.extention );
            }
            //POST ASYNC CALL
            HttpResponseMessage message = await client.PostAsync(Definition.url+this.NumContrat+ "/arbitrages", requestContent);
            string content = await message.Content.ReadAsStringAsync();
            //We are waiting for JSON response !
            return content;
        }


        //#############################################################################################################################//


        // pointeur de fonction PreProcessInformation appelé quand l'envoie asynchrone est fini
        private delegate void finishTask(SftpClient cli, FileStream fs, IAsyncResult ar);

        public Spirica() { }

        // pour passse
        public Spirica(Acte acte){

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
            this.pieces = acte.pieces;

            }


        //passez par ce constructeur au moment de la generation (le cast ne marche pas)
        /* public Spirica(List<Acte> actes)
         {
             foreach(Acte a in actes)
             {
                 Spirica item = new Spirica();
                 item.NomType = a.NomType;
                 item.NomActeAdministratif = a.NomActeAdministratif;
                 item.ReferenceInterne = a.ReferenceInterne;
                 item.NomCompletSouscripteurs = a.NomCompletSouscripteurs;
                 item.NumContrat = a.NumContrat;
                 item.CodeApporteur = a.CodeApporteur;
                 item.NomApporteur = a.NomApporteur;
                 item.MontantBrut = a.MontantBrut;
                 item.TypeFrais = a.TypeFrais;
                 item.Frais = a.Frais;
                 item.ID_ProfilCompagnie = a.ID_ProfilCompagnie;
                 item.NomEnveloppe = a.NomEnveloppe;
                 item.ListeSupportDesinvestir = a.ListeSupportDesinvestir;
                 item.ListeSupportInvestir = a.ListeSupportInvestir;
                 item.ListeDocument = a.ListeDocument;
                 item.IsTraitementEdi = a.IsTraitementEdi;
                 item.DateCreation = a.DateCreation;
                 item.DateAcquisition = a.DateAcquisition;
                 item.Commentaire = a.Commentaire;
                 item.InvestissementImmediat = a.InvestissementImmediat;
                 item.Regul = a.Regul;

                 production.Add(item);
             }
             //fill missing data
             getDetailPiece();

     }*/

        /*
    // cherche les details des documents (nom et type) et rempli la liste d'objet Spirica.pieces
    private void getDetailPiece()
    {
        SqlConnection con = Definition.connexion;
        foreach(Spirica a in production)
        {
            List<int> id_docNortia = new List<int>();
            foreach (DocumentProduction r in a.ListeDocument) id_docNortia.Add(r.ID_DocumentNortia);
            var cmd = new SqlCommand("select nom,type_doc='type' from pli where ID_Pli in ({id_doc})");
            cmd.addArrayCommand(id_docNortia, "id_doc");
            cmd.Connection = con;
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                DetailPiece detPiece = new DetailPiece();
                detPiece.nomFichier = reader[0].ToString();
                detPiece.typeFicher = reader[1].ToString();
                a.pieces.Add(detPiece);
            }
            reader.Close();
            con.Close();
            id_docNortia.Clear();
        }

    }*/

        // genere la prod en Json
        /* public string genJSON() //OBSELETE
         {
                         JsonSerializerSettings jsonSetting = new JsonSerializerSettings
             {
                 // ces options sont valable aussi pour les sous objets
                 NullValueHandling = NullValueHandling.Ignore,
                 MissingMemberHandling = MissingMemberHandling.Ignore,
                 ContractResolver = new ShouldSerializeContractResolver()
             };
             return JsonConvert.SerializeObject(production, jsonSetting);
         }*/

        //envoie la prod PDF (pdfs separés) OBSELETE
        /*public void sendProd()
        {
            try
            {
                FileInfo f = new FileInfo(Definition.sourcePath + "\\test.pdf");
                SftpClient client = new SftpClient(Definition.sftpHost, 22, Definition.sftpUser, Definition.sftpPswd);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(60); // set auth timeout = 60 sec
                client.Connect();
                if (client.IsConnected)
                {
                    FileStream fstream = new FileStream(f.FullName, FileMode.Open);
                    Definition.getProgressBar().Invoke(
                                (MethodInvoker)delegate { Definition.getProgressBar().Maximum = (int)fstream.Length; });
                    finishTask funcPtr = new finishTask(PreProcessInformation);
                    IAsyncResult ar = client.BeginUploadFile(fstream, Definition.remotePath + "" + f.Name, ac => {
                        funcPtr(client, fstream,null); }
                    , client, UpdateProgresBar);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }   
        }*/

        //## UPDATE PROGRESS BAR ON FOREGROUD THREAD
        private void UpdateProgresBar(ulong uploaded)
        {   
            Definition.getProgressBar().Invoke((MethodInvoker)delegate { Definition.getProgressBar().Value = (int)uploaded; });
        }


        //METHOD AUTO CALLED WHEN CLIENT FINISH TO TRANSFER FILES (ASYNC TASK)
        private void PreProcessInformation(SftpClient cli, FileStream fs,IAsyncResult ar = null)
        {
            // do stuff when completed, basicly send a message or close the connexiox

            // free resources
            cli.Disconnect();
            fs.Close();
            Definition.getProgressBar().Invoke((MethodInvoker)delegate { Definition.getProgressBar().Value = 0; });
            MessageBox.Show("finished");
        }
        

        // show remote sftp files
        public static void showFiles(string dir = "//SPIRICA//" ){
            IEnumerable<string> str = null;

            FileInfo f = new FileInfo(Definition.sourcePath + "\\test.pdf");
            SftpClient client = new SftpClient(Definition.sftpHost, 22, Definition.sftpUser, Definition.sftpPswd);
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(60); // set auth timeout = 60 sec
            client.Connect();
            if (client.IsConnected)
            {
                
                str = client.ListDirectory(dir).Select(s => s.FullName);
            }

            foreach (var v in str)
            {
                MessageBox.Show(v);
            }
            
        }

    }
}

public class binaries
{
    public byte[] ficheirPDF;
    public string nomFichie;
    public string extention;
}