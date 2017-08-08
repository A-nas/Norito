using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;



namespace GED.Handlers
{
    //SINGLETON (duplicata web service)
    public class Production
    {
        private static Production refInstance;
        public Dictionary<string, string> TRANSTYPE;


        //method to get the instance of class
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
                // we mustn't be here !
                Console.WriteLine("exception throwed ==> {0}", ex.Message);
            }
            return refInstance;
        }

        private Production()
        {
            TRANSTYPE = new Dictionary<string, string>();

            SqlCommand cmd = new SqlCommand("SELECT Code_ISIN , Code_Support FROM [dbo].[SUPPORT_TRANSTYPE]", Definition.connexionQualif); //TCO_ForcageSupportsCies (changer les nom de colonnes)
            Definition.connexionQualif.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                TRANSTYPE.Add(dr[0].ToString() , dr[1].ToString());
            Definition.connexionQualif.Close();
        }

        //DEPRECATED
        public async static Task<string[]> envoyerProd(List<IActe> actes){
            int nombreActes = actes.Count();
            string[] response = new string[nombreActes];
            for(int i=0; i < nombreActes; i++) response[i] = (await actes[i].sendProd());
            return response;
        }

        //** method to send a List of 'Acte'
        public async Task<string[]> envoyerProd(List<Acte> actes)
        {
            int nombreActes = actes.Count();
            string[] response = new string[nombreActes];
            for (int i = 0; i < nombreActes; i++)
            {
                //Dynamic Dyspatching
                IActe acteprod = new Spirica(actes[i]);
                response[i] = (await acteprod.sendProd());
            }
            return response;
        }
    }
}
