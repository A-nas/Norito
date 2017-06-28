﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// added
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;


namespace GED.Handlers
{
     public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public ShouldSerializeContractResolver() { }
        // a mettre dans definition
        List<string> pptActeNames = Definition.pptActeNames; // set of Spirica properties to Serialize

        //retourne des proprieté a de/serialiser                                                                                 /* add ppties */
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            properties = 
                properties.Where(p => pptActeNames.Contains(p.PropertyName, StringComparer.OrdinalIgnoreCase)).ToList(); // proprties a serialiser

            return properties;
        }
    }


}


