using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;



namespace GED.Handlers
{
    //SINGLETON
    public class Production
    {
        //public static List<string> responses;
        private static Production refInstance;
        public Dictionary<string, string> TRANSTYPE;

        public static Production getInstance(){
            Production refInstance =  null;
            try {
                if (refInstance == null)
                {
                    refInstance = new Production();
                }
                return refInstance;
            } catch(Exception ex)
            {
                Console.WriteLine("exception throwed ==> {0}", ex.Message);
            }
            return refInstance;
        }

        private Production()
        {
            TRANSTYPE = new Dictionary<string, string>();

            SqlCommand cmd = new SqlCommand("SELECT CodeISIN,CodeSupport_Cie FROM TCO_ForcageSupportsCies", Definition.connexionQualifDW);
            Definition.connexionQualifDW.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                TRANSTYPE.Add(dr[0].ToString() , dr[1].ToString());
            Definition.connexionQualifDW.Close();
        }

        //DEPRECATED
        public async static Task<string[]> envoyerProd(List<IActe> actes){ // passer une liste d'actes SPI directement.
            int nombreActes = actes.Count();
            string[] response = new string[nombreActes];
            for(int i=0; i < nombreActes; i++) response[i] = (await actes[i].sendProd());
            return response;
        }


        public async Task<string[]> envoyerProd(List<Acte> actes)
        {
            int nombreActes = actes.Count();
            string[] response = new string[nombreActes];
            for (int i = 2; i < nombreActes; i++)
            {
                //Dynamic Dyspatching
                IActe acteprod = new Spirica(actes[i]);
                response[i] = (await acteprod.sendProd());
            }
            return response;
        }
    }
}
