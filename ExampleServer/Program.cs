using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using lib;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //Creating the certificate
            var serverCertificate = new X509Certificate2("Certificate/TcpTLSServer_TemporaryKey.pfx", "1234");
            //Creating the server
            Server server = new Server(serverCertificate); // serverCertificate);

            //test Get method
            server.Get("testurl", (req, res) => {
                res.Send("get from test url");
            });

            //test Post method
            server.Post("testurl", (req, res) => {
                res.Send($"post from test url, {req.BodyAsString()}");
            });

            //test sending JSON object
            server.Get("jsonobject", (req, res) =>
             {
                 res.Send("{ \"name\":\"Jone\", \"age\":39, \"car\":null }");
             });

            server.Get("artikler/:kategori/artikkelid", (req, res) =>
            {
                int artikkelID = Int32.Parse(req.Params["artikkelid"]);
                string kategori = req.Params["kategori"];
                res.Send(Database.HentArtikkelFraDatabase(kategori, artikkelID));
            });

            server.Use("WebApp");

           
            //Server starts listening to port, and responding to webpage.
            server.Listen(443);


        }
        private static class Database
        {
            public static string HentArtikkelFraDatabase(string kategori, int artikkelID)
            {
                return $"Dette er artikkel #{artikkelID} i kategorien {kategori}, fersk fra databasen";
            }
        }
    }
}
