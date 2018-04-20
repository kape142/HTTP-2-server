using System;
using System.Collections.Generic;
using System.Text;

namespace lib.Streams
{
    class StreamHandler
    {
        List<Stream> ActiveStreams;

        public void addStream(Stream stream)
        {
            if (stream.Dependency == 0)
            {
                ActiveStreams.Add(stream);
            }
            else 
            {
                Stream parent = findStreamById(stream.Dependency, ActiveStreams);
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
        public Stream findStreamById(uint id,List<Stream> streams)
        {
            foreach(Stream stream in streams)
            {
                if (stream.Id == id)
                {
                    return stream;
                }
                else
                {
                    return findStreamById(id, stream.dependencies);
                }
            }
            return null;
        }
    }
}
