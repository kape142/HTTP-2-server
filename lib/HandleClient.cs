using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lib
{
    class HandleClient
    {
        TcpClient tcpClient;
        StreamReader reader;
        StreamWriter writer;

        public void StartThreadForClient(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            reader = new StreamReader(tcpClient.GetStream());
            writer = new StreamWriter(tcpClient.GetStream());
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
                    Console.WriteLine(res.Data.ToString());
                    await Task.Run(() => WriteResponse(res));
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
            //await writer.WriteAsync(r.Data, 0, r.Data.Length);
            //await writer.FlushAsync();

            int bytesToSend = r.Data.Length;
            int packageSize = 100;

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
        }
    }

    
}
