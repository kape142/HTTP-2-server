using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lib
{
    class HandleClient
    {
        TcpClient tcpClient;
        SslStream sslStream;
        StreamReader reader;
        StreamWriter writer;

        public void StartThreadForClient(TcpClient tcpClient, X509Certificate2 certificate)
        {
            this.tcpClient = tcpClient;
            try
            {
                sslStream = new SslStream(tcpClient.GetStream(), false, App_CertificateValidation);
                sslStream.AuthenticateAsServer(certificate, false, SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
                reader = new StreamReader(sslStream);
                writer = new StreamWriter(sslStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Handshake failed... \n" + ex.ToString());
                tcpClient.Close();
                return;
            }

            Thread t = new Thread(StartReadingAsync);
            t.Start();

        }

        private async void StartReadingAsync()
        {
            while (true)
            {
                string s = await ReadStream();
                try
                {
                    Request req = new Request(s);
                    Console.WriteLine(req.ToString());
                    Response res = Response.From(req);
                    Console.WriteLine(res.ToString());
                    WriteResponse(res);
                    //await Task.Run(() => WriteResponse(res));
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex);
                    // Console.WriteLine("To small request recived");
                }
            }
        }

        private async Task<string> ReadStream()
        {
            string msg = "";
            while (reader.Peek() != -1)
            {
                msg += await reader.ReadLineAsync() + "\n";
            }
            return msg;
        }

        private void WriteResponse(Response r)
        {
            writer.Flush();
            writer.Write(r.ToString());
            writer.Flush();
            writer.Write(r.Data, 0, r.Data.Length);
            writer.Flush();
            

            /*
            int bytesToSend = r.Data.Length;
            int packageSize = 1200;

            while (bytesToSend > 0)
            {
                if (bytesToSend >= packageSize)
                {
                    writer.Write(r.Data, (r.Data.Length - bytesToSend), packageSize);
                    writer.Flush();
                    bytesToSend -= packageSize;
                }
                else
                {
                    writer.Write(r.Data, (r.Data.Length - bytesToSend), bytesToSend);
                    writer.Flush();
                    bytesToSend -= packageSize;
                }
            }
            writer.Flush();
            */
            
        }
        static bool App_CertificateValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
    // Skipping validation because of the use of test certificate
    

}
