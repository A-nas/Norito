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



namespace GED.Handlers
{
   public class Spirica : Acte, IActe
    {
        // Additional properties
        [JsonProperty(PropertyName = "support_saisie", Order = 5)]
        private static string supsaisie = "bo";
        [JsonProperty(PropertyName = "pieces", Order = 7)]
        public List<DetailPiece> pieces = new List<DetailPiece>();
        [JsonProperty(PropertyName = "date_signature", Order = 2)]
        public string dateDeSignature;
        List<binaries> binaires;

        // Properties to manage/save the state and data of the multi instance of the current class
        private static Dictionary<string, string> TRANSTYPE = null;
        private static bool isSuccess = false;

        public static bool getProdState(){
            return isSuccess;
        }

        private void getSupports()
        {
            TRANSTYPE = new Dictionary<string, string>();
            // we select distinct code_isin to ensure we get only one isin code
            SqlCommand cmd = new SqlCommand("SELECT * from "
                + "(SELECT code_isin, code_support, ROW_NUMBER() OVER(PARTITION BY code_isin ORDER BY code_isin DESC) rn "
                + "from[dbo].[SUPPORT_TRANSTYPE] "
                + ") sub_query "
                + "WHERE rn = 1 ", Definition.connexionQualif); //TCO_ForcageSupportsCies (changer les nom de colonnes)

            Definition.connexionQualif.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                TRANSTYPE.Add(dr[0].ToString(), dr[1].ToString());
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
            //insert database ##
            SqlCommand cmd = new SqlCommand("INSERT INTO GenerationProd_Log(Date_Log,ID_ProdSF,TypeMessage,Message) VALUES (@date_Log,@ID_ProdSF,@typeMessage,@message)", Definition.connexionQualif);
            cmd.Parameters.AddWithValue("@date_Log", (object)DateTime.Now);
            cmd.Parameters.AddWithValue("@ID_ProdSF", (object)" --- ");
            cmd.Parameters.AddWithValue("@typeMessage", (object)"VARDEBUG");
            cmd.Parameters.AddWithValue("@message", (object)JsonConvert.SerializeObject(this, jsonSetting)+"*"+base.DateEnvoiProduction.ToString() + " " + base.DateAcquisition.ToString() + " " + base.DateCreation.ToString());
            Definition.connexionQualif.Open();
            cmd.ExecuteNonQuery();
            Definition.connexionQualif.Close();
            // end insert datatbse ##
            return JsonConvert.SerializeObject(this, jsonSetting);
        }


        // Attach and send the current production (**add elec signature)
        public async Task<Dictionary<string,WsResponse>> sendProd(){

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

            foreach (binaries bin in this.binaires)
            {
                var binaryFile = new ByteArrayContent(bin.ficheirPDF);
                binaryFile.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                requestContent.Add(binaryFile, "file", bin.nomFichie );
            }
            //POST ASYNC CALL
            HttpResponseMessage message = await client.PostAsync(Definition.url + this.NumContrat + "/arbitrages", requestContent); // must be extracted
            string returnMessages = await message.Content.ReadAsStringAsync();
            Dictionary<string, WsResponse> response = new Dictionary<string, WsResponse>();
            response.Add(this.ReferenceInterne, new WsResponse { message = getMessage(returnMessages,message),
                                                                 status_xml = getStatusXml(message) } 
                        );
            // 1 acte success => prod success
            isSuccess = message.IsSuccessStatusCode;
            //waiting until we get the JSON response !
            return response;
        }

        private string[] getMessage(string returnMessages,HttpResponseMessage message){

            if (message.StatusCode == System.Net.HttpStatusCode.NotFound) return new string[] { "Acte introuvable" };
            if (message.IsSuccessStatusCode){
                return new string[] { "Acte envoyé avec succés" };
            }
            else{
                // loop for all error messages
                JToken[] Jobj = JObject.Parse(returnMessages)["anomalies"].ToArray();
                string[] messages = new string[Jobj.Length];
                for(int i=0 ; i < Jobj.Length ; i++){
                    string subJobj = Jobj[i].ToString();
                    string errorMsg =
                      "\n- " + (((JObject.Parse(subJobj)["type"] == null)) ? String.Empty : "Erreur de Type : " + JObject.Parse(subJobj)["type"].ToString()) +
                             ((JObject.Parse(subJobj)["categorie"] == null) ? String.Empty : " catégorie : " + JObject.Parse(subJobj)["categorie"].ToString()) +
                             ((JObject.Parse(subJobj)["commentaire"] == null) ? String.Empty : " commentaire : " + JObject.Parse(subJobj)["commentaire"].ToString()) +
                             "\n\n";

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
            this.dateDeSignature = acte.DateEnvoiProduction.ToLocalTime().ToString("dd/MM/yyyy"); // sans le prefix 'acte' on prend la propriete de this.base.dateEnvoieEnProd
            this.binaires = new List<binaries>();

            if (TRANSTYPE == null) getSupports();
            fillData();
        }


        //this method will fill additional properties
        private void fillData(){

            int[] idDocs = base.ListeDocument.Select(x => x.ID_DocumentNortia).ToArray();

            if (idDocs.Length > 0)
            {
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
            }
            // end remplissage de pieces

            // transtypage de supports
            var instance = Production.getInstance(); // ##a supprimer ?##
            Dictionary<string,string> dicto = TRANSTYPE; // ### a optimiser ###
            foreach (Repartition rep in base.ListeSupportDesinvestir.Concat(ListeSupportInvestir)) {
              if (dicto.ContainsKey(rep.CodeISIN)){
                        rep.code_support_ext = dicto[rep.CodeISIN];
                    }else{
                    // les codes ISIN ne passent pas pour l'instant
                        rep.code_support_ext = rep.CodeISIN;
                        throw new Exception("impossible de trouver le code support du support :" + rep.CodeISIN);
                    }
                }
            Definition.connexionQualif.Close();
        }

    }
}

public class binaries
{
    public byte[] ficheirPDF;
    public string nomFichie;
}

public class WsResponse // mettre dans SPI ou depalcer vers production
{
    public string[] message;
    public string status_xml;
}