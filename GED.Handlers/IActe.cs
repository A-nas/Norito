using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GED.Handlers
{
    public interface IActe
    {
        //string genJSON();
        Task<string> sendProd(); // fait l'appel au web service pour envoyer la prod
    }
}
