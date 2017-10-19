using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// JSON & DB CONNEXION
using System.Data.SqlClient;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Windows.Forms;
// WEB SERVICE INCLUDES
using System.Net.Http;
using System.Net.Http.Headers;
//Tall Components & merging libs
using iTextSharp.text.pdf;
using System.IO;



namespace GED.Handlers
{
   public class Spirica : Acte, IActe
    {
        // Additional properties
        [JsonProperty(PropertyName = "support_saisie", Order = 5)]
        private static string supsaisie = "bo";
        [JsonProperty(PropertyName = "pieces", Order = 7)]
        private List<DetailPiece> pieces;
        [JsonProperty(PropertyName = "date_signature", Order = 2)]
        private string dateDeSignature;
        [JsonProperty(PropertyName = "incompressible_assureur_deroge")]
        private bool incompressible;
        List<binaries> binaires;

        // Properties to manage/save the state and data of the multi instance of the current class
        private static Dictionary<string, string> TRANSTYPE = null;
        private static Dictionary<string, string> ANOMALIES = null;
        private static bool isSuccess = false; // back to sha 222b9a98e149a349f920aa17e8c71e38f2ab20a5 for boolean return
        private static List<string> SuccessActes = new List<string>(); // list of 'Acte' IDs that return success code (2xx HTTP)

        public static List<string> getListSuccess(){
            return SuccessActes;
        }
        public static bool getProdState(){
            return isSuccess;
        }

        private void getSupports()
        {
            TRANSTYPE = new Dictionary<string, string>();
            // we select distinct code_isin to ensure we get only one isin code
            SqlCommand cmd = new SqlCommand("SELECT code_isin, code_support +';'+  [Type] FROM"
                + "(SELECT code_isin, code_support, ROW_NUMBER() OVER(PARTITION BY code_isin ORDER BY code_isin DESC) rn, [Type] "
                + "from[dbo].[SUPPORT_TRANSTYPE] "
                + "WHERE code_isin is not null) sub_query "
                + "WHERE rn = 1 ", Definition.connexionQualif); //TCO_ForcageSupportsCies (changer les nom de colonnes)

            Definition.connexionQualif.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                TRANSTYPE.Add(dr[0].ToString(), dr[1].ToString());
            dr.Close();


            Definition.connexionQualif.Close();
        }

        private void getAnomalies()
        {
            ANOMALIES = new Dictionary<string, string>();
            // we select distinct code_anomalie to ensure we get only one code per description
            SqlCommand cmd = new SqlCommand("SELECT code_anomalie, descriptif_anomalie FROM ( "
                + "	SELECT 	code_anomalie, "
                + " descriptif_anomalie, "
                + " ROW_NUMBER() OVER(PARTITION BY code_anomalie ORDER BY code_anomalie DESC) rn "
                + " FROM [dbo].[ANOMALIE_TRANSTYPE] "
                + " WHERE code_anomalie is not null "
                + " )sub_query WHERE rn = 1 ", Definition.connexionQualif);
            Definition.connexionQualif.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                ANOMALIES.Add(dr[0].ToString(), dr[1].ToString());
            dr.Close();
            Definition.connexionQualif.Close();
        }

        //method that serialise the current object (this) into a JSON flow (this method rely on the ShouldSerialiseContratResolver Class) 
        private string genJson() {
            JsonSerializerSettings jsonSetting = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new ShouldSerializeContractResolver()
            };
            //insert database ## ♣
            SqlCommand cmd = new SqlCommand("INSERT INTO GenerationProd_Log(Date_Log,ID_ProdSF,TypeMessage,Message) VALUES (@date_Log,@ID_ProdSF,@typeMessage,@message)", Definition.connexionQualif);
            cmd.Parameters.AddWithValue("@date_Log", (object)DateTime.Now);
            cmd.Parameters.AddWithValue("@ID_ProdSF", (object)" --- ");
            cmd.Parameters.AddWithValue("@typeMessage", (object)"VARDEBUG");
            cmd.Parameters.AddWithValue("@message", (object)JsonConvert.SerializeObject(this, jsonSetting));
            Definition.connexionQualif.Open();
            cmd.ExecuteNonQuery();
            Definition.connexionQualif.Close();
            // end insert datatbse ##
            return JsonConvert.SerializeObject(this, jsonSetting);
        }


        // Attach and send the current production
        public async Task<Dictionary<string[],WsResponse>> sendProd(){

            // preparing request HEADER
            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient();
            byte[] Basic_Auth = Encoding.ASCII.GetBytes(Definition.id + ":" + Definition.pass); // tester cet appel je vais pas y revenir une autre fois.
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Basic_Auth));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Charset", "UTF-8");
            //preparing request BODY
            MultipartFormDataContent requestContent = new MultipartFormDataContent();
            ByteArrayContent json = new ByteArrayContent(Encoding.UTF8.GetBytes(genJson())); // encodage a verifier apres !!
            json.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json"); // can be configured
            requestContent.Add(json, "arbitrage");

            foreach (binaries bin in this.binaires){ // this can be optimised we have the files dupliated on bianries List Class and pieces List Class
                var binaryFile = new ByteArrayContent(bin.ficheirPDF);
                binaryFile.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                requestContent.Add(binaryFile, "file", bin.nomFichie );
            }
            //POST ASYNC CALL
            HttpResponseMessage message = await client.PostAsync(Definition.url + this.NumContrat + "/arbitrages", requestContent); // must be extracted
            string returnMessages = await message.Content.ReadAsStringAsync();
            Dictionary<string[], WsResponse> response = new Dictionary<string[], WsResponse>();
            response.Add(new string[] { this.ReferenceInterne , this.prodActeID },
                         new WsResponse { message = getMessage(returnMessages,message),
                                          status_xml = getStatusXml(message)
                                        }
                        );
            // 1 acte success => prod success
            if (message.IsSuccessStatusCode){
                SuccessActes.Add(this.ReferenceInterne);
                isSuccess = true;
            }
            //waiting until we get the JSON response !
            return response;
        }

        private string[] getMessage(string returnMessages,HttpResponseMessage message){

            if (message.StatusCode == System.Net.HttpStatusCode.NotFound) return new string[] { "Acte introuvable\n" };

            if (message.IsSuccessStatusCode){
                return new string[] { "Acte envoyé avec succés\n" };
            }
            else{
                // loop for all error messages
                JToken[] Jobj = JObject.Parse(returnMessages)["anomalies"].ToArray();
                string[] messages = new string[Jobj.Length];
                for(int i=0 ; i < Jobj.Length ; i++){
                    string subJobj = Jobj[i].ToString();
                    // may throw exception in case if new 'Anomalies' types are added (*)
                    string errorMsg =
                    "\n- " + (((JObject.Parse(subJobj)["type"] == null)) ? String.Empty : ANOMALIES[JObject.Parse(subJobj)["type"].ToString()]) +
                             //((JObject.Parse(subJobj)["categorie"] == null) ? String.Empty : " ,catégorie : " + JObject.Parse(subJobj)["categorie"].ToString()) + uncomment to have the catégorie detail
                             ((JObject.Parse(subJobj)["commentaire"] == null) ? String.Empty : " . " + JObject.Parse(subJobj)["commentaire"].ToString()) +
                             "\n";

                messages[i] = errorMsg;
                }
                return messages;
            }
        }

        private string getStatusXml(HttpResponseMessage message){
            return message.IsSuccessStatusCode ? "Accepté":"Rejeté";
        }

        public Spirica() { }

        // Consutructor used to cast ACTE => SPIRICA
        public Spirica(Acte acte) {
            this.pieces = new List<DetailPiece>();
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
            this.dateDeSignature = acte.DateEnvoiProduction.ToLocalTime().ToString("dd/MM/yyyy");
            this.binaires = new List<binaries>();
            this.isSigned = acte.isSigned;
            this.prodActeID = acte.prodActeID;
            //if (this.Frais == 0) incompressible = true;
            if (TRANSTYPE == null) getSupports();
            if (ANOMALIES == null) getAnomalies();
            fillData();
        }


        //this method will fill additional properties
        private void fillData()
        {

            int[] idDocs = base.ListeDocument.Select(x => x.ID_DocumentNortia).ToArray();

            if (idDocs.Length > 0)
            {
                // parameters are not escaped *** must be changed
                var cmd = new SqlCommand("SELECT cam.nom [Nom de fichier] ,cam.datas [Fichier PDF binaire],tdt.code_type_document_externe, cam.extension [Type de Document] from type_document td "
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
                        nomFichier = reader[0].ToString() + "" + reader[3].ToString(),
                        typeFicher = reader[2].ToString()
                    });
                    this.binaires.Add(new binaries
                    {
                        nomFichie = reader[0].ToString() + "" + reader[3].ToString(),
                        ficheirPDF = (byte[])reader[1]
                    });
                }
                reader.Close();
                Definition.connexionQualif.Close();

                if (this.isSigned)
                {
                    //work only if bianries documents are PDF files
                    string signedFileName = "Document_signe_Electroniquement.pdf";
                    string TypeFichier = "dossier_arbitrage_signe_electroniquement";

                    this.pieces.Add(new DetailPiece
                    {
                        nomFichier = signedFileName,
                        typeFicher = TypeFichier
                    });
                    this.binaires.Add(
                        mergeDoc(binaires, signedFileName)
                    );
                }
            }
            // end remplissage de pieces

            // transtypage de supports
            foreach (Repartition rep in base.ListeSupportDesinvestir.Concat(ListeSupportInvestir))
            {
                if (TRANSTYPE.ContainsKey(rep.CodeISIN))
                {
                     if (TRANSTYPE[rep.CodeISIN].Split(';')[1] == "SUPPORT") // spit it here
                         rep.code_support_ext = TRANSTYPE[rep.CodeISIN].Split(';')[0];
                     else
                         rep.code_profil = TRANSTYPE[rep.CodeISIN].Split(';')[0];
                 }
                 else
                     rep.code_support_ext = rep.CodeISIN;
            }
      }

        // method to merge List<binaries> into one binary (bianires class)
        private binaries mergeDoc(List<binaries> SignedBinaries, string mergedFileName){

                MemoryStream memoStream = new MemoryStream();
                iTextSharp.text.Document doc = new iTextSharp.text.Document();
                PdfSmartCopy copy = new PdfSmartCopy(doc, memoStream);
                doc.Open();

                List<byte[]> ListOfPDFS = SignedBinaries.Select(x => x.ficheirPDF).ToList();
                //Loop through each byte array (each iteration represent a single PDF)
                foreach (byte[] pdf in ListOfPDFS){
                    PdfReader pdfReader = new PdfReader(pdf);
                    copy.AddDocument(pdfReader);
                }
                doc.Close();

                return new binaries{
                    ficheirPDF = memoStream.ToArray(),
                    nomFichie = mergedFileName
                };
         }

        public bool ShouldSerializeincompressible(){
            if (Frais == 0) {
                incompressible = true;
                    return true;
            }
            return false;
        }


    }
}

public class binaries
{
    public byte[] ficheirPDF;
    public string nomFichie;
}

public class WsResponse // "pas sa place ici" ==> deplacer vers Production ou suppimer l'objet et crée un type anonyme
{
    public string[] message;
    public string status_xml;
}