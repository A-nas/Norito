using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// added
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
//config
using System.Configuration;


namespace GED.Handlers
{
     public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        List<string> pptActeNames = ConfigurationManager.AppSettings["propToSerialise"].Split(';').ToList<string>();
        //methode that return the list of properties to be serialised
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            properties =
                properties.Where(p => pptActeNames.Contains(p.PropertyName, StringComparer.OrdinalIgnoreCase)).ToArray();

            return properties;
        }
    }


}


