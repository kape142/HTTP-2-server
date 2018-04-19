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
        StreamReader streamReader;
        StreamWriter streamWriter;
        BinaryReader binaryReader;
        bool HttpUpgraded = false;

        public void StartThreadForClient(TcpClient tcpClient, X509Certificate2 certificate = null)
        {
            this.tcpClient = tcpClient;
            try
            {
                if(certificate != null)
                {
                    sslStream = new SslStream(tcpClient.GetStream(), false, App_CertificateValidation);
                    sslStream.AuthenticateAsServer(certificate, false, SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
                    streamReader = new StreamReader(sslStream);
                    streamWriter = new StreamWriter(sslStream);
                    binaryReader = new BinaryReader(sslStream);
                }
                else
                {
                    streamReader = new StreamReader(tcpClient.GetStream());
                    streamWriter = new StreamWriter(tcpClient.GetStream());
                    binaryReader = new BinaryReader(tcpClient.GetStream());
                }
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
                string s = await ReadStreamToString();
                try
                {
                    HTTP1Request req = new HTTP1Request(s);
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

        private async Task<string> ReadStreamToString()
        {
            string msg = "";
            while (streamReader.Peek() != -1)
            {
                msg += await streamReader.ReadLineAsync() + "\n";
            }
            return msg;
        }

        private async Task<byte[]> ReadStreamToFrameBytes()
        {
            // todo
            byte[] blength = binaryReader.ReadBytes(3);
            

            while (streamReader.Peek() != -1)
            {

            }
            return null;
        }

        private void WriteResponse(Response r)
        {
            streamWriter.Flush();
            streamWriter.Write(r.ToString());
            streamWriter.Flush();
            streamWriter.Write(r.Data, 0, r.Data.Length);
            streamWriter.Flush();
            

            /****
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
        // Skipping validation because of the use of test certificate
        static bool App_CertificateValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
    
    

}
