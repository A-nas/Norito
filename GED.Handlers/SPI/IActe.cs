using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GED.Handlers
{
    public interface IActe
    {
        // method to send PRODUCTION (generic)
        Task<string> sendProd();
        //Task<Dictionary<string,string>> sendProd(); 
    }
}
