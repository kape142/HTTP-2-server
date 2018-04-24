using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using lib.Frames;
using lib.HTTPObjects;
using Http2.Hpack;


namespace lib.Streams
{
    class StreamHandler
    {
        private List<HTTP2Stream> OutgoingStreams = new List<HTTP2Stream>();
        private List<HTTP2Stream> IncomingStreams = new List<HTTP2Stream>();
        private Queue<HTTP2Frame> framesToSend = new Queue<HTTP2Frame>();
        object lockFramesToSend = new object();
        internal HandleClient owner; 
        private bool sendFramesThreadAlive = true;
        int streamIdTracker = 2;

        public StreamHandler(HandleClient client)
        {
            owner = client;
            IncomingStreams.Add(new HTTP2Stream(0));
        }

        internal void StartSendThread()
        {
            Thread t = new Thread(SendThread);
            t.Start();
        } 

        private async void SendThread()
        {
            while (sendFramesThreadAlive)
            {
                if(framesToSend.Count > 0)
                {
                    // send
                    HTTP2Frame frametosend = null;
                    lock (this)
                    {
                     frametosend = framesToSend.Dequeue();
                    }
                    await Task.Run(() => owner.WriteFrame(frametosend));
                }
                else
                {
                    Thread.Sleep(100); // todo bedre løsning
                }
            }
        }

        internal void SendFrame(HTTP2Frame frame)
        {
            lock (lockFramesToSend)
            {
                framesToSend.Enqueue(frame);
            }
        }

        public void AddStreamToOutgoing(HTTP2Stream stream)
        {
            if (stream.Dependency == 0)
            {
                OutgoingStreams.Add(stream);
            }
            else 
            {
                HTTP2Stream parent = findStreamById(stream.Dependency, OutgoingStreams,false);
                if(parent != null){
                    parent.dependencies.Add(stream);
                }
            }
            
        }

        public void AddStreamToIncomming(HTTP2Stream stream)
        {
            IncomingStreams.Add(stream);
        }

        public void closeStream(HTTP2Stream inStream)
        {
            foreach(HTTP2Stream stream in inStream.dependencies)
            {
                AddStreamToOutgoing(stream);
            }
        }

        public HTTP2Stream findStreamById(uint id,List<HTTP2Stream> streams, bool remove)
        {
            foreach(HTTP2Stream stream in streams)
            {
                if (stream.Id == id)
                {
                    if (remove) { streams.Remove(stream); }
                    return stream;
                }
                else
                {
                    return findStreamById(id, stream.dependencies, remove);
                }
            }
            return null;
        }

        void updateStream(HTTP2Stream stream)
        {
            HTTP2Stream streamToUpdate = findStreamById(stream.Id, OutgoingStreams, (stream.Dependency != 0));
            streamToUpdate.Dependency = stream.Dependency;
            streamToUpdate.Weight = stream.Weight;
            AddStreamToOutgoing(streamToUpdate);
        }

        //Todo: implement weighthandeling to get priorities right
        // void getFramesFromStreams()
        // {
        //     foreach(HTTP2Stream stream in OutgoingStreams)
        //     {
        //         framesToSend.Enqueue(stream.Frames.Dequeue());
        //     }
        // }

        internal bool IncomingStreamExist(int streamId)
        {
            return IncomingStreams.Exists(x => x.Id == streamId);
        }

        internal HTTP2Stream GetIncommingStreams(int streamId)
        {
            return IncomingStreams.Find(x => x.Id == streamId);
        }

        internal void AddIncomingFrame(HTTP2Frame frame)
        {
            switch (frame.Type)
            {
                case HTTP2Frame.DATA:
                    Console.WriteLine("DATA frame recived\n" + frame.ToString());
                    break;
                case HTTP2Frame.HEADERS:
                    Console.WriteLine("HEADERS frame recived\n" + frame.ToString());
                    GetIncommingStreams(frame.StreamIdentifier).Frames.Add(frame);
                    if (frame.FlagEndHeaders)
                    {
                        EndOfHeaders(frame.StreamIdentifier);
                        break;
                    }
                    Console.WriteLine(frame.ToString());
                    break;
                case HTTP2Frame.PRIORITY_TYPE:
                    Console.WriteLine("PRIORITY_TYPE frame recived\n" + frame.ToString());
                    break;
                case HTTP2Frame.RST_STREAM:
                    Console.WriteLine("RST_STREAM frame recived\n" + frame.ToString());
                    break;
                case HTTP2Frame.SETTINGS:
                    Console.WriteLine("SETTINGS frame recived\n" + frame.ToString());
                    if(frame.StreamIdentifier != 0)
                    {
                        // send protocol error
                        break;
                    }
                    if (frame.FlagAck) break;
                    SendFrame(new HTTP2Frame(0).AddSettingsPayload(new (ushort, uint)[0], true));
                    break;
                case HTTP2Frame.PUSH_PROMISE:
                    Console.WriteLine("PUSH_PROMISE frame recived\n" + frame.ToString());
                    break;
                case HTTP2Frame.PING:
                    Console.WriteLine("PING frame recived\n" + frame.ToString());
                    break;
                case HTTP2Frame.GOAWAY:
                    Console.WriteLine("GOAWAY frame recived\n" + frame.ToString());
                    GoAwayPayload gp = frame.GetGoAwayPayloadDecoded();
                    Console.WriteLine(gp.ToString());
                    break;
                case HTTP2Frame.WINDOW_UPDATE:
                    Console.WriteLine("WINDOW_UPDATE frame recived\n" + frame.ToString());
                    break;
                case HTTP2Frame.CONTINUATION:
                    Console.WriteLine("CONTINUATION frame recived\n" + frame.ToString());
                    GetIncommingStreams(frame.StreamIdentifier).Frames.Add(frame);
                    if (frame.FlagEndHeaders)
                    {
                        EndOfHeaders(frame.StreamIdentifier);
                        break;
                    }
                    break;
                default:
                    break;
            }
            // IncomingStreams.Find(x => x.Id == frame.StreamIdentifier).Frames.Enqueue(frame);
            // if (frame.FlagEndHeaders)
            // {
            //     // sette sammen fragmentene og svare
            //     IncomingStreams.Find(x => x.Id == frame.StreamIdentifier).EndOfHeaders();
            // }
        }

        // en annen metode har funnet ut at
        void EndOfHeaders(int streamID)
        {
            int index = IncomingStreams.FindIndex(x => x.Id == streamID);
            HTTP2Stream currentstream = IncomingStreams[index];
            IncomingStreams.RemoveAt(index);
            OutgoingStreams.Add(currentstream);

            // concatinate the payloads
            HTTP2Frame[] headers = currentstream.Frames.FindAll(x => x.Type == HTTP2Frame.HEADERS || x.Type == HTTP2Frame.CONTINUATION).ToArray();
            byte[] payloads = HTTP2Frame.CombineHeaderPayloads(headers);
            // decompress
            var headerBlockFragment = new ArraySegment<byte>(payloads);
            byte[] decompressedHeaders = new byte[HTTP2Frame.MaxFrameSize];
            List<HeaderField> lstheaders = new List<HeaderField>();
            var dencodeResult = owner.hpackDecoder.DecodeHeaderBlockFragment(headerBlockFragment, (uint)HTTP2Frame.MaxFrameSize, lstheaders); // todo max header size
            foreach (var item in lstheaders)
            {
                Console.WriteLine(item.Name + " " + item.Value);
            }
            string method = lstheaders.Find(x => x.Name == ":method").Value;
            string path = lstheaders.Find(x => x.Name == ":path").Value;

            string file;
            if (path is null || path == "" || path == "/")
            {
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\index.html";
            }
            else
            {
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\" + path;
            }
            HTTPRequestHandler.SendFile(this, streamID, file);

        }

        internal async Task RespondWithFirstHTTP2(string url)
        {
            string file;
            if (url == ""||url.Contains("index.html"))
            {
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\index.html";
                HTTPRequestHandler.SendFile(this, 1, file);
                
                //Server Push simple
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\style.css";
                if (File.Exists(file))
                {
                    streamIdTracker += 2;
                    HTTPRequestHandler.SendFile(this, streamIdTracker, file);
                }
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\script.js";
                if (File.Exists(file))
                {
                    streamIdTracker += 2;
                    HTTPRequestHandler.SendFile(this, streamIdTracker, file);
                }
            }
            else
            {
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\" + url;
                HTTPRequestHandler.SendFile(this, streamIdTracker++, file);
            }
        }

        internal void Close()
        {
            sendFramesThreadAlive = false;
            OutgoingStreams = null;
            IncomingStreams = null;
            framesToSend = null;
        }
    }

}