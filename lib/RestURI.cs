using System;
using System.Collections.Generic;
using System.Text;
using lib.HTTPObjects;

namespace lib
{
    internal class RestURI
    {
        public static RestURI RestLibrary { get; } = new RestURI("");
        internal delegate void HTTPMethod(Request req, Response res);
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
                if (SubURIs.ContainsKey(path[1]))
                {
                    RestURI subURI = SubURIs[path[1]];
                    subURI.AddURI(method, NextPath(URI), callback);
                }
                else
                {
                    RestURI subURI = new RestURI(path[1]);
                    subURI.AddURI(method, NextPath(URI), callback);
                    SubURIs.Add(path[1], subURI);
                }
                return;
            }
            throw new Exception("­I­ ­d­o­n­'­­­­­­t­­ ­­­t­­­h­­­i­­­n­­­k­­­­ ­­­t­­­h­­­i­­­s­­­ ­­­s­­­h­­­o­u­l­d­ ­­­h­­­­­a­p­­­­­­­­­­­­­­­­­­­p­e­n­­­­­­?­?­");  
        }

        private RestURI(String URI)
        {
            this.URI = URI;
        }

        internal void Execute(string method, string URI, Request req, Response res)
        {
            string[] path = URI.Split("/");
            if (URI != "")
            {
                SubURIs[URI.Split("/")[0]].Execute(method, NextPath(URI), req, res);
                return;
            }
            if (Methods.ContainsKey(method))
                Methods[method].Invoke(req, res);
            else
                PathParam.Execute(method, URI, req, res);

        }

        private static string NextPath(string path)
        {
            int split = path.IndexOf("/");
            split = (split >= 0) ? split : path.Length-1;
            return path.Substring(split+1);
        }
    }
}
