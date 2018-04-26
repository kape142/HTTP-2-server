using System;
using System.Collections.Generic;
using System.Text;
using lib.HTTPObjects;
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
                Array.Resize(ref HTTPMethods, newProperties.Length);
            if (HTTPMethods[newProperties.Length - 1] == null)
                HTTPMethods[newProperties.Length - 1] = new Dictionary<string, HTTPMethod>();
            HTTPMethods[newProperties.Length - 1].Add(method, callback);
            this.URI = (newProperties.Length > oldProperties.Length) ? URI : this.URI;
        }

        internal void Execute(string method, string URI, Request req, IResponse res)
        {
            string[] path = URI.Split("/");
            int length = path.Length;
            string[] keyPath = this.URI.Split("/");
            if(HTTPMethods[length-1] == null || !HTTPMethods[length-1].ContainsKey(method))
                throw new ArgumentException($"The method {method} has not yet been defined for this path");
            for(int i = 0; i < length; i++)
            {
                req.AddParams((keyPath[i], path[i]));
            }
            HTTPMethods[length - 1][method].Invoke(req, res);
        }

        internal bool HasMethod(string method, string URI)
        {
            string[] path = URI.Split("/");
            int length = path.Length;
            string[] keyPath = this.URI.Split("/");
            if (length > HTTPMethods.Length || HTTPMethods[length - 1] == null || !HTTPMethods[length - 1].ContainsKey(method))
                return false;
            else
                return true;
        }

    }
}
