using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GED.Handlers
{
    public class DetailPiece
    {
        [JsonProperty(PropertyName = "nom")]
        public string nomFichier {get; set;}
        [JsonProperty(PropertyName = "type")]
        public string typeFicher { get; set;}
    }
}
