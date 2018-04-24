using System;
using System.Collections.Generic;
using System.Text;
using static lib.RestURI;

namespace lib
{
    class RestPathParam
    {
        private string URI { get; set; } = "";
        private Dictionary<string,HTTPMethod>[] HTTPMethods = new Dictionary<string, HTTPMethod>[0];

        internal RestPathParam(){}

        internal void AddURI(string method, string URI, HTTPMethod callback)
        {                
            string[] newProperties = URI.Split("/");
            string[] oldProperties = this.URI.Split("/");
            int minLength = (newProperties.Length < oldProperties.Length) ? newProperties.Length : oldProperties.Length;
            for(int i = 0; i < minLength; i++)
            {
                if(oldProperties[i] != newProperties[i] && oldProperties[i] != "")
                    throw new ArgumentException("The path parameters for this URI have already been defined as something else");
            }
            if(newProperties.Length > HTTPMethods.Length)
            {
                Array.Resize(ref HTTPMethods, newProperties.Length);
            }
            HTTPMethods[newProperties.Length - 1].Add(method, callback);
        }

    }
}
