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

        //** method to send a List of 'Acte'
        public async Task<Dictionary<string,string>> envoyerProd(List<Acte> actes)
        {
            int nombreActes = actes.Count();
            string[] response = new string[nombreActes];
            Dictionary<string, string> responses = new Dictionary<string,string>(); //#
            Dictionary<string, string> cresponse = new Dictionary<string, string>();
            for (int i = 0; i < nombreActes; i++){
            // if i pass TRANSTYPE TABLE here as method parameter, The context will depend on the company (unless TRANSTYPE table concerne all companies)
                //Dynamic Dyspatching
                IActe acteprod = new Spirica(actes[i]);
                cresponse = (await acteprod.sendProd()); // send one "Acte"
                responses.Add(cresponse.Keys.ElementAt(0),cresponse[cresponse.Keys.ElementAt(0)]); // get current element
            }
            updateSalesForce(responses);
            bool success = Spirica.getProdState(); 
            return responses;
        }// must return boolean // method should be implemented by IActe as well

        public void updateSalesForce(Dictionary<string,string> responses){
            //## this fucntion must after all manage exceptions in case if we can't connect to Force.com API
            // fetch for all actes data list
            // update list
            // save update
        }

    }
}
