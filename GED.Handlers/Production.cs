using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GED.Handlers
{
    public class Production
    {
        List<IActe> actes;
        List<string> responses;
        public static Production refInstance;

        public static Production instance(List<IActe> _actes){
            if(refInstance == null){
                refInstance = new Production{ actes = _actes };
            }
            return refInstance;        
        }

        public async static Task<string[]> envoyerProd(List<IActe> actes){ // passer une liste d'actes SPI directement.
            int nombreActes = actes.Count();
            string[] response = new string[nombreActes];
            for(int i=0; i < nombreActes; i++) response[i] = (await actes[i].sendProd());
            return response;
        }
    }
}
