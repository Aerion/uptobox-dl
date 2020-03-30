using System;
using System.Runtime.Serialization;

namespace UptoboxDl.UptoboxClient
{
    [Serializable]
    public class ClientException : Exception
    {
        public ClientException()
        {
        }

        public ClientException(string message) : base(message)
        {
        }

        public ClientException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ClientException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}