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
        private static string directory = Directory.GetCurrentDirectory().ToString();
        private List<HTTP2Stream> OutgoingStreams = new List<HTTP2Stream>();
        private List<HTTP2Stream> IncomingStreams = new List<HTTP2Stream>();
        private Dictionary<uint, Action> SendBufferedDataList = new Dictionary<uint, Action>();
        private Queue<HTTP2Frame> framesToSend = new Queue<HTTP2Frame>();
        object lockFramesToSend = new object();
        internal HandleClient owner; 
        private bool sendFramesThreadAlive = true;
        
        int streamIdTracker = 2;

        public StreamHandler(HandleClient client)
        {
            owner = client;
            if (Server.UseDebugDirectory)
            {
                directory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.ToString();
            }
            IncomingStreams.Add(new HTTP2Stream(0, StreamState.Idle, owner.settings.InitialWindowSize));
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

        internal void SendFrameImmmediate(HTTP2Frame frame)
        {
            Task.Run(() => owner.WriteFrame(frame));
        }
        internal void SendFrame(HTTP2Frame frame)
        {
            if (OutgoingStreams.Find(x => x.Id == frame.StreamIdentifier)?.State == StreamState.Closed) return;
            if (IncomingStreams.Find(x => x.Id == frame.StreamIdentifier)?.State == StreamState.Closed) return;
            lock (lockFramesToSend)
            {
                framesToSend.Enqueue(frame);
            }
        }

        private void CancelSending()
        {
            lock (lockFramesToSend)
            {
                framesToSend.Clear();
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
                HTTP2Stream parent = FindStreamById(stream.Dependency, OutgoingStreams,false);
                if(parent != null){
                    parent.dependencies.Add(stream);
                }
            }
            
        }

        public void AddStreamToIncomming(HTTP2Stream stream)
        {
            IncomingStreams.Add(stream);
        }

        public void CloseStream(HTTP2Stream inStream)
        {
            foreach(HTTP2Stream stream in inStream.dependencies)
            {
                AddStreamToOutgoing(stream);
            }
        }

        public HTTP2Stream FindStreamById(uint id,List<HTTP2Stream> streams, bool remove)
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
                    var dependency = FindStreamById(id, stream.dependencies, remove);
                    if(dependency != null)
                    {
                        return dependency;
                    }
                }
            }
            return null;
        }

        void UpdateStream(HTTP2Stream stream)
        {
            HTTP2Stream streamToUpdate = FindStreamById(stream.Id, OutgoingStreams, (stream.Dependency != 0));
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
            return IncomingStreams.Exists(x => x.Id == (uint)streamId);
        }

        private void CloseStream(int streamId)
        {
            OutgoingStreams.FindAll(x => x.Id == streamId).ForEach( y => y.State = StreamState.Closed);
            IncomingStreams.FindAll(x => x.Id == streamId).ForEach( y => y.State = StreamState.Closed);
            lock (lockFramesToSend)
            {
                var frames = new List<HTTP2Frame>(framesToSend.ToArray());
                frames.RemoveAll(x => x.StreamIdentifier == streamId);
                framesToSend = new Queue<HTTP2Frame>(frames.ToArray());
            }
        }

        internal HTTP2Stream GetIncommingStreams(int streamId)
        {
            return IncomingStreams.Find(x => x.Id == streamId);
        }

        internal void AddIncomingFrame(HTTP2Frame frame)
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine("-----------------");
            switch (frame.Type)
            {
                case HTTP2Frame.DATA:
                    s.AppendLine("DATA frame received\n" + frame.ToString());
                    if(frame.StreamIdentifier == 0)
                    {
                        // todo svar med protocol error
                    }
                    GetIncommingStreams(frame.StreamIdentifier).Frames.Add(frame);
                    if (frame.FlagEndStream)
                    {
                        EndOfStream(frame.StreamIdentifier);
                        break;
                    }
                    DataPayload dp = frame.GetDataPayloadDecoded();
                    if (dp.Data != null) s.AppendLine(Encoding.ASCII.GetString(dp.Data));
                    break;
                case HTTP2Frame.HEADERS:
                    s.AppendLine("HEADERS frame received\n" + frame.ToString());
                    GetIncommingStreams(frame.StreamIdentifier).Frames.Add(frame);
                    if (frame.FlagEndStream)
                    {
                        EndOfStream(frame.StreamIdentifier);
                        break;
                    }
                    //s.AppendLine(frame.ToString());
                    break;
                case HTTP2Frame.PRIORITY_TYPE:
                    s.AppendLine("PRIORITY_TYPE frame received\n" + frame.ToString());
                    break;
                case HTTP2Frame.RST_STREAM:
                    s.AppendLine("RST_STREAM frame received\n" + frame.ToString());
                    RSTStreamPayload rst = frame.GetRSTStreamPayloadDecoded();
                    s.AppendLine($"Error code: {rst.ErrorCode} on stream: {frame.StreamIdentifier}");
                    CloseStream(frame.StreamIdentifier);
                    break;
                case HTTP2Frame.SETTINGS:
                    s.AppendLine("SETTINGS frame received\n" + frame.ToString());
                    if(frame.StreamIdentifier != 0)
                    {
                        // send protocol error
                        break;
                    }
                    SettingsPayload sp = frame.GetSettingsPayloadDecoded();
                    s.AppendLine(sp.ToString());
                    owner.settings.ApplySettings(sp.Settings);
                    if (frame.FlagAck) break;
                    SendFrame(new HTTP2Frame(0).AddSettingsPayload(new (ushort, uint)[0], true));
                    break;
                case HTTP2Frame.PUSH_PROMISE:
                    s.AppendLine("PUSH_PROMISE frame received\n" + frame.ToString());
                    break;
                case HTTP2Frame.PING:
                    s.AppendLine("PING frame received\n" + frame.ToString());
                    if (!frame.FlagAck)
                    {
                        SendFrameImmmediate(new HTTP2Frame(frame.StreamIdentifier).AddPingPayload(frame.Payload, true));
                    }
                    break;
                case HTTP2Frame.GOAWAY:
                    s.AppendLine("GOAWAY frame received\n" + frame.ToString());
                    GoAwayPayload gp = frame.GetGoAwayPayloadDecoded();
                    s.AppendLine(gp.ToString());
                    owner.Close();
                    break;
                case HTTP2Frame.WINDOW_UPDATE:
                    s.AppendLine("WINDOW_UPDATE frame received\n" + frame.ToString());
                    WindowUpdatePayload wup= frame.GetWindowUpdatePayloadDecoded();
                    s.AppendLine(wup.ToString());
                    IncreaseWindowSize((uint)frame.StreamIdentifier, (uint)wup.WindowSizeIncrement);
                    break;
                case HTTP2Frame.CONTINUATION:
                    s.AppendLine("CONTINUATION frame received\n" + frame.ToString());
                    GetIncommingStreams(frame.StreamIdentifier).Frames.Add(frame);
                    if (frame.FlagEndStream)
                    {
                        EndOfStream(frame.StreamIdentifier);
                        break;
                    }
                    break;
                default:
                    break;
            }
            s.AppendLine("-----------------");
            Console.WriteLine(s);
        }

        internal void IncreaseWindowSize(uint streamId, uint amount)
        {
            if (streamId == 0)
            {
                owner.windowSize += amount;
                Console.WriteLine($"Connection window size: {WindowUpdatePayload.ConvertToLargerUnit((int)owner.windowSize)} - increased");
                return;
            }
            HTTP2Stream stream = FindStreamById(streamId, IncomingStreams, false);
            if (stream == null)
                stream = FindStreamById(streamId, OutgoingStreams, false);
            if (stream != null)
            {
                stream.WindowSize += amount;
                Console.WriteLine($"Stream #{streamId} window size: {WindowUpdatePayload.ConvertToLargerUnit((int)stream.WindowSize)} - increased");
                if (SendBufferedDataList.ContainsKey(streamId))
                {
                    SendBufferedDataList[streamId]();
                    //SendBufferedDataList.Remove(streamId);
                    //Console.WriteLine($"Buffered data on stream #{streamId} removed");
                }
                    
                return;
            }
            IncomingStreams.Add(new HTTP2Stream(streamId, StreamState.Open, owner.settings.InitialWindowSize + amount));
            Console.WriteLine($"no such streamId #{streamId}- increase; creating it");
        }

        internal int ReduceWindowSize(uint streamId, uint amount)
        {
            if (streamId == 0)
            {
                int newsize = (int)owner.windowSize - (int)amount;
                owner.windowSize = (newsize >= 0) ? (uint)newsize : 0;
                Console.WriteLine($"Connection window size: {WindowUpdatePayload.ConvertToLargerUnit((int)owner.windowSize)} - reduced");
                return newsize;
            }
            HTTP2Stream stream = FindStreamById(streamId, IncomingStreams, false);
            if (stream == null)
                stream = FindStreamById(streamId, OutgoingStreams, false);
            if (stream != null)
            {
                int newsizeOwner = (int)owner.windowSize - (int)amount;
                owner.windowSize = (newsizeOwner >= 0) ? (uint)newsizeOwner : 0;
                Console.WriteLine($"Connection window size: {WindowUpdatePayload.ConvertToLargerUnit((int)owner.windowSize)} - reduced");
                int newsize = (int)stream.WindowSize - (int)amount;
                stream.WindowSize = (newsize >= 0) ? (uint)newsize : 0;
                Console.WriteLine($"Stream #{streamId} window size: {WindowUpdatePayload.ConvertToLargerUnit((int)stream.WindowSize)} - reduced");
                return Math.Min(newsize, newsizeOwner);
            }
            int newSize = (int)owner.settings.InitialWindowSize - (int)amount;
            uint newSizeUint= (newSize >= 0) ? (uint)newSize : 0;
            IncomingStreams.Add(new HTTP2Stream(streamId, StreamState.Open, newSizeUint));
            Console.WriteLine($"no such streamId #{streamId}- reduce; creating it");
            return newSize;
        }

        internal void BufferDataForWindowUpdate(uint streamId, byte[] data, string contentType)
        {
            HTTP2Stream stream = FindStreamById(streamId, IncomingStreams, false);
            if (stream == null)
                stream = FindStreamById(streamId, OutgoingStreams, false);
            if (stream != null)
            {
                if (SendBufferedDataList.ContainsKey(streamId))
                {
                    SendBufferedDataList.Remove(streamId);
                    Console.WriteLine($"Buffered data on stream #{streamId} removed");
                }
                    
                SendBufferedDataList.TryAdd(streamId, () =>
                {
                    Console.WriteLine("Sending buffered data");
                    HTTP2RequestGenerator.SendData(this, (int)streamId, data, contentType, true);
                });
                Console.WriteLine($"Data buffered on stream #{streamId}, window size: {WindowUpdatePayload.ConvertToLargerUnit((int)stream.WindowSize)}, " +
                    $"data size: {WindowUpdatePayload.ConvertToLargerUnit(data.Length)}");
            }
        }

        void EndOfStream(int streamID)
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine("--------------");
            s.AppendLine($"Headers for streamID {streamID}\n");
            int index = IncomingStreams.FindIndex(x => x.Id == streamID);
            HTTP2Stream currentstream = IncomingStreams[index];
            IncomingStreams.RemoveAt(index);
            OutgoingStreams.Add(currentstream);

            // concatinate the payloads
            HTTP2Frame[] headerAndContinuationFrames = currentstream.Frames.FindAll(x => x.Type == HTTP2Frame.HEADERS || x.Type == HTTP2Frame.CONTINUATION).ToArray();
            byte[] headerAndContinuationPayloades = HTTP2Frame.CombineHeaderPayloads(headerAndContinuationFrames);
            // decompress
            var headerBlockFragment = new ArraySegment<byte>(headerAndContinuationPayloades);
            List<HeaderField> lstheaders = new List<HeaderField>();
            DecoderExtensions.DecodeFragmentResult dencodeResult = owner.hpackDecoder.DecodeHeaderBlockFragment(headerBlockFragment, (uint)HTTP2Frame.MaxFrameSize, lstheaders); // todo max header size
            if(dencodeResult.Status != DecoderExtensions.DecodeStatus.Success)
            {
                CancelSending();
                s.AppendLine("Decompress errror: " + dencodeResult.Status);
                SendFrame(new HTTP2Frame(0).AddGoawayPayload(streamID, HTTP2Frame.COMPRESSION_ERROR, Encoding.ASCII.GetBytes("Decompress errror on serverside: " + dencodeResult.Status)));
                Thread.Sleep(1000);
                owner.Close();
                return;
            }
            foreach (var item in lstheaders)
            {
                s.AppendLine(item.Name + " " + item.Value);
            }
            string method = lstheaders.Find(x => x.Name == ":method").Value;
            string path = lstheaders.Find(x => x.Name == ":path").Value;
            string contentType = lstheaders.Find(x => x.Name == ":content-type").Value;
            string accept = lstheaders.Find(x => x.Name == ":accept").Value;
            string encoding = lstheaders.Find(x => x.Name == "accept-encoding").Value;

            if (RestURI.RestLibrary.HasMethod(method, path))
            {
                switch (method)
                {
                    case "GET":
                        Request getRequest = new Request(null, contentType);
                        Response getResponse = new Response(this, streamID);
                        RestURI.RestLibrary.Execute("GET", path, getRequest, getResponse);
                        break;
                    case "POST":
                        HTTP2Frame[] dataFrames = currentstream.Frames.FindAll(x => x.Type == HTTP2Frame.DATA).ToArray();
                        byte[] datapayload = HTTP2Frame.CombineDataPayloads(dataFrames);
                        Request postRequest = new Request(datapayload, contentType);
                        Response postResponse = new Response(this, streamID);
                        RestURI.RestLibrary.Execute("POST", path, postRequest, postResponse);
                        break;
                    default:
                        HTTP2RequestGenerator.SendMethodNotAllowed(this, streamID);
                        break;
                }
                return;
            }

            string file;
            if(path is null)
            {
                s.AppendLine("Emtpy path received");
                return;
            }
            else if (path == "" || path == "/")
            {
                file = CombinePath(directory, Server.DIR, "index.html");
            }
            else
            {
                file = CombinePath(directory, Server.DIR, path);
                s.AppendLine($"{directory} + {Server.DIR} + {path} = {file}");
            }
            HTTP2RequestGenerator.SendFile(this, streamID, file,encoding);
            s.AppendLine("--------------");
            Console.WriteLine(s);
        }

        internal async Task RespondWithFirstHTTP2(string url) //wtf
        {
            string file;
            if (url == ""||url.Contains("index.html"))
            {
                file = CombinePath(Environment.CurrentDirectory, Server.DIR, "index.html");
                HTTP2RequestGenerator.SendFile(this, 1, file,"");

                //Server Push simple 
                file = CombinePath(Environment.CurrentDirectory, Server.DIR, "style.css");
                if (File.Exists(file))
                {
                    streamIdTracker += 2;
                    HTTP2RequestGenerator.SendFile(this, streamIdTracker, file,"");
                }
                file = CombinePath(Environment.CurrentDirectory, Server.DIR, "script.js");
                if (File.Exists(file))
                {
                    streamIdTracker += 2;
                    HTTP2RequestGenerator.SendFile(this, streamIdTracker, file,"");
                }
            }
            else
            {
                file = CombinePath(Environment.CurrentDirectory , Server.DIR, url);
                HTTP2RequestGenerator.SendFile(this, streamIdTracker++, file,"");
            }
        }

        internal void Close()
        {
            sendFramesThreadAlive = false;
            OutgoingStreams = null;
            IncomingStreams = null;
            framesToSend = null;
        }

        internal static String CombinePath(params string[] path)
        {
            var last = path[path.Length - 1];
            if (Path.IsPathRooted(last))
            {
                last = last.TrimStart(Path.DirectorySeparatorChar);
                last = last.TrimStart(Path.AltDirectorySeparatorChar);
                path[path.Length - 1] = last;
            }
            return Path.Combine(path);
        }
    }

}