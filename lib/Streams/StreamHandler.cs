using System;
using System.Collections.Generic;
using System.Text;
using lib.HTTPObjects;

namespace lib.Streams
{
    class StreamHandler
    {
        List<HTTP2Stream> OutgoingStreams;
        List<HTTP2Stream> IncommingStreams;
        Queue<HTTP2Frame> framesToSend;

        public StreamHandler()
        {
            IncommingStreams.Add(new HTTP2Stream(0));
        }

        public void addStream(HTTP2Stream stream)
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

        public void closeStream(HTTP2Stream inStream)
        {
            foreach(HTTP2Stream stream in inStream.dependencies)
            {
                addStream(stream);
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
            addStream(streamToUpdate);
        }

        //Todo: implement weighthandeling to get priorities right
        void getFramesFromStreams()
        {
            foreach(HTTP2Stream stream in OutgoingStreams)
            {
                framesToSend.Enqueue(stream.Frames.Dequeue());
            }
        }

        internal bool Exist(int streamId)
        {
            return IncommingStreams.Exists(x => x.Id == streamId);
        }

        void AddIncomingFrame(int id, HTTP2Frame frame)
        {
            IncommingStreams.Find(x => x.Id == id).Frames.Enqueue(frame);
        }

        // en annen metode har funnet ut at
        void EndOfIncomingStream(int streamID)
        {
            int index = IncommingStreams.FindIndex(x => x.Id == streamID);
            HTTP2Stream currentstream = IncommingStreams[index];
            IncommingStreams.RemoveAt(index);
            OutgoingStreams.Add(currentstream);

            // concatinate the payloads
            //HTTP2Frame[] frames = // hent rammer

                 // først switch på type
                 // så if på flag


            // viss end er satt så behandle requesten
            /*switch (frame.Type)
            {
                case HTTP2Frame.DATA:
                    break;
                case HTTP2Frame.HEADERS:
                    

                    break;
                case HTTP2Frame.PRIORITY_TYPE:
                    break;
                case HTTP2Frame.RST_STREAM:
                    break;
                case HTTP2Frame.SETTINGS:
                    break;
                case HTTP2Frame.PUSH_PROMISE:
                    break;
                case HTTP2Frame.PING:
                    break;
                case HTTP2Frame.GOAWAY:
                    break;
                case HTTP2Frame.WINDOW_UPDATE:
                    break;
                case HTTP2Frame.CONTINUATION:
                    break;
                default:
                    break;
            }*/

        }
    }

}
