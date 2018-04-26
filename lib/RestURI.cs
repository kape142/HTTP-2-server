using System;
using System.Collections.Generic;
using System.Text;
using lib.HTTPObjects;

namespace lib
{
    public class RestURI
    {
        public static RestURI RestLibrary { get; } = new RestURI("");
        public delegate void HTTPMethod(Request req, IResponse res);
        internal String URI { get; }
        private Dictionary<String, RestURI> SubURIs = new Dictionary<string, RestURI>();
        private Dictionary<String, HTTPMethod> Methods = new Dictionary<string, HTTPMethod>();
        private RestPathParam PathParam { get; } = new RestPathParam();

        internal void AddURI(string method, string URI, HTTPMethod callback)
        {
            URI = CleanURI(URI);
            string[] path = URI.Split("/");
            string next = path[0];
            if (next.Equals(""))
            {
                Methods.Add(method, callback);
                return;
            }
            if (next.Substring(0, 1).Equals(":"))
            {
                PathParam.AddURI(method, URI.Substring(1), callback);
                return;
            }
            if (SubURIs.ContainsKey(path[0]))
            {
                RestURI subURI = SubURIs[path[0]];
                subURI.AddURI(method, NextPath(URI), callback);
            }
            else
            {
                RestURI subURI = new RestURI(path[0]);
                subURI.AddURI(method, NextPath(URI), callback);
                SubURIs.Add(path[0], subURI);
            }
        }

        private RestURI(String URI)
        {
            this.URI = CleanURI(URI);
        }

        internal void Execute(string method, string URI, Request req, IResponse res)
        {
            URI = CleanURI(URI);
            string[] path = URI.Split("/");
            string next = path[0];
            if (URI != "")
            {
                if (SubURIs.ContainsKey(next))
                    SubURIs[next].Execute(method, NextPath(URI), req, res);
                else
                {
                    if (PathParam.HasMethod(method, URI))
                        PathParam.Execute(method, URI, req, res);
                    else
                        throw new ArgumentException("No such method");
                }
            }
            else
            {
                if (Methods.ContainsKey(method))
                    Methods[method].Invoke(req, res);
                else
                    throw new ArgumentException("No such method");
            }
                
        }

        internal bool HasMethod(string method, string URI)
        {
            URI = CleanURI(URI);
            string[] path = URI.Split("/");
            string next = path[0];
            if (URI != "")
            {
                if (SubURIs.ContainsKey(next))
                    return SubURIs[next].HasMethod(method, NextPath(URI));
                else
                {
                    return PathParam.HasMethod(method, URI);
                }
            }
            else
            {
                return Methods.ContainsKey(method);
            }
        }

        private static string NextPath(string path)
        {
            int split = path.IndexOf("/");
            split = (split >= 0) ? split : path.Length-1;
            return path.Substring(split+1);
        }

        private string CleanURI(string uri)
        {
            if (uri.Length == 0)
                return uri;
            if (uri.Substring(0, 1) == "/")
                uri = uri.Substring(1);
            if (uri.Length == 0)
                return uri;
            if (uri.Substring(uri.Length - 1, 1) == "/")
                uri = uri.Substring(0, uri.Length - 1);
            return uri;
        }
    }
}
