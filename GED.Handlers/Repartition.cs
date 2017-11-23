using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;


namespace GED.Handlers
{
    public class Repartition
    {



        public string CodeISIN { get; set; }
        public string TypeRepartition { get; set; } // % ou €
        public float ValeurRepartition { get; set; }


        // mon code
        [JsonProperty(PropertyName = "code_support")]
        public string code_support_ext;
        [JsonProperty(PropertyName = "code_profil")]
        public string code_profil;
        [JsonProperty(PropertyName = "pourcentage", Order = 1)]
        private float montant_per;
        [JsonProperty(PropertyName = "montant", Order = 2)]
        private float montant_euro;

        public bool ShouldSerializemontant_per()
        {
            if (TypeRepartition == "%") {
                montant_per = (int) ValeurRepartition;
                return true;
            }
            return false;
        }

        public bool ShouldSerializemontant_euro()
        {
            if (TypeRepartition == "%") return false;
            montant_euro = ValeurRepartition;
            return true;
        }

        // end mon code


        public Repartition()
        {
            CodeISIN = "";
            TypeRepartition = "";
            ValeurRepartition = 0;
        }
    }
}
