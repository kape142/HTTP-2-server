using System;
using System.Collections.Generic;
using System.Text;

namespace lib
{
    internal class RestURI
    {
        public static RestURI RestLibrary { get; } = new RestURI();
        internal delegate void HTTPMethod(HTTP1Request req, Response res);
        internal String URI { get; }
        private Dictionary<String, RestURI> SubURIs = new Dictionary<string, RestURI>();
        private Dictionary<String, HTTPMethod> Methods = new Dictionary<string, HTTPMethod>();

        internal void AddURI(string method, string URI, HTTPMethod callback)
        {
            string[] path = URI.Split("/");
            if (path.Length > 1)
            {
                RestURI subURI = new RestURI(path[1]);
                string[] subPath = new string[path.Length - 1];
                Array.Copy(path, 1, subPath, 0, subPath.Length);
                subURI.AddURI(method, String.Concat(subPath), callback);
            }
            else
            {

            }           
        }

        private RestURI(String URI)
        {
            this.URI = URI;
        }
    }
}
