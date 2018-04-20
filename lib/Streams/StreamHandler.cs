using System;
using System.Collections.Generic;
using System.Text;
using lib.HTTPObjects;

namespace lib.Streams
{
    class StreamHandler
    {
        List<Stream> ActiveStreams;
        Queue<Frame> framesToSend;


        public void addStream(Stream stream)
        {
            if (stream.Dependency == 0)
            {
                ActiveStreams.Add(stream);
            }
            else 
            {
                Stream parent = findStreamById(stream.Dependency, ActiveStreams,false);
                if(parent != null){
                    parent.dependencies.Add(stream);
                }
            }
            
        }

        public void closeStream(Stream inStream)
        {
            foreach(Stream stream in inStream.dependencies)
            {
                addStream(stream);
            }
        }

        public Stream findStreamById(uint id,List<Stream> streams, bool remove)
        {
            foreach(Stream stream in streams)
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

        void updateStream(Stream stream)
        {
            Stream streamToUpdate = findStreamById(stream.Id, ActiveStreams, (stream.Dependency != 0));
            streamToUpdate.Dependency = stream.Dependency;
            streamToUpdate.Weight = stream.Weight;
            addStream(streamToUpdate);
        }

        //Todo: implement weighthandeling to get priorities right
        void getFramesFromStreams()
        {
            foreach(Stream stream in ActiveStreams)
            {
                framesToSend.Enqueue(stream.frames.Dequeue());
            }
        }
    }

}
