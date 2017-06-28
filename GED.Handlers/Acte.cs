using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using Newtonsoft.Json;

namespace GED.Handlers
{
    public class Acte
    {
        public string NomType { get; set; }
        public string NomActeAdministratif { get; set; }
        [JsonProperty(PropertyName = "ne", Order = 1)]
        public string ReferenceInterne { get; set; }
        public string NomCompletSouscripteurs { get; set; }
        public string NumContrat { get; set; }
        public string CodeApporteur { get; set; }
        public string NomApporteur { get; set; }
        public float MontantBrut { get; set; }
        public string TypeFrais { get; set; } // % ou €

        [JsonProperty(PropertyName = "taux_frais_deroge")] // set order here
        public float Frais { get; set; }
        public string ID_ProfilCompagnie { get; set; }
        public string NomEnveloppe { get; set; }

        [JsonProperty(PropertyName = "desinvestissements", Order = 3)]
        public List<Repartition> ListeSupportDesinvestir { get; set; }
        [JsonProperty(PropertyName = "reinvestissements", Order = 4)]
        public List<Repartition> ListeSupportInvestir { get; set; }
        public List<DocumentProduction> ListeDocument { get; set; }
        public bool IsTraitementEdi { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime DateAcquisition { get; set; }
        [JsonProperty(PropertyName = "commentaire", Order = 6)]
        public string Commentaire { get; set; }
        public bool InvestissementImmediat { get; set; }
        public bool Regul { get; set; }

        // proprietes a alimentés
        [JsonProperty(PropertyName = "support_saisie", Order = 5)]
        private string supsaisie = "bo";
        [JsonProperty(PropertyName = "pieces", Order = 7)]
        public List<DetailPiece> pieces = new List<DetailPiece>();


        public Acte()
        {
            NomActeAdministratif = "";
            ListeSupportDesinvestir = new List<Repartition>();
            ListeSupportInvestir = new List<Repartition>();
            ListeDocument = new List<DocumentProduction>();
            ID_ProfilCompagnie = "";
            IsTraitementEdi = false;
            InvestissementImmediat = false;
            Commentaire = "";
            Regul = false;
        }

        public string Get_ID_ProfilCompagnieCA()
        {
            SqlConnection con = null;
            string strSQL = "";
            SqlCommand cmd = null;

            string ID_ProfilCompagnieCA = "";

            try
            {
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);
                strSQL = "SELECT ID_Profil FROM tek.ProfilXML_AEP WHERE Nom_EnveloppeSF=@Nom_EnveloppeSF";

                con.Open();
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@Nom_EnveloppeSF", (object)NomEnveloppe ?? DBNull.Value);

                ID_ProfilCompagnieCA = (string)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                ID_ProfilCompagnieCA = "";
            }
            finally
            {
                if (con != null)
                    con.Close();
            }

            return ID_ProfilCompagnieCA;
        }

        public int NbPage()
        {
            int nbPage = 0;

            try
            {
                foreach (DocumentProduction doc in ListeDocument)
                    nbPage += doc.NbPage;
            }
            catch (Exception ex) // runtime exception are not throwed, why ?
            {
            }

            return nbPage;
        }

    }
}
