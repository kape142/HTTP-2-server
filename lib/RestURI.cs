using System;
using System.Collections.Generic;
using System.Text;

namespace lib
{
    internal class RestURI
    {
        public static RestURI RestLibrary { get; } = new RestURI("");
        internal delegate void HTTPMethod(HTTP1Request req, Response res);
        internal String URI { get; }
        private Dictionary<String, RestURI> SubURIs = new Dictionary<string, RestURI>();
        private Dictionary<String, HTTPMethod> Methods = new Dictionary<string, HTTPMethod>();
        private RestPathParam PathParam { get; } = new RestPathParam();

        internal void AddURI(string method, string URI, HTTPMethod callback)
        {
            string[] path = URI.Split("/");
            string next = path[0];
            if (next.Equals(""))
            {
                Methods.Add(method, callback);
                return;
            }
            if (next.Substring(0, 1).Equals(":"))
            {
                PathParam.AddURI(method, NextPath(URI), callback);
                return;
            }
            if (path.Length > 1)
            {
                RestURI subURI = new RestURI(path[1]);
                subURI.AddURI(method, NextPath(URI), callback);
                SubURIs.Add(path[1], subURI);
                return;
            }
            throw new Exception("I don't think this should happen??");  
        }

        private RestURI(String URI)
        {
            this.URI = URI;
        }



        private static string NextPath(string path)
        {
            int split = path.IndexOf("/");
            return path.Substring(split+1);
        }
    }
}
