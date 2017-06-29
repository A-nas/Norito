using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GED.Handlers
{
    public class Production
    {
        public static List<string> responses;
        private static Production refInstance;

        public static Production getInstance(){
            if(refInstance == null){
                refInstance = new Production{};
            }
            return refInstance;        
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
            for (int i = 0; i < nombreActes; i++)
            {
                IActe acteprod = new Spirica(actes[i]);
                //IActe acteprod = (IActe) actes[i]; // cast avec du code 
                response[i] = (await acteprod.sendProd());
            }
            return response;
        }
    }
}
