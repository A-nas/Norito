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

        private Production(){}

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
            // if i pass TRANSTYPE TABLE here as method parameter, The context will depend on the company (unless TRANSTYPE table concerne all companies) # a voir aprés
                //Dynamic Dyspatching
                IActe acteprod = new Spirica(actes[i]);
                response[i] = (await acteprod.sendProd());
            }
            return response;
        }

    }
}
