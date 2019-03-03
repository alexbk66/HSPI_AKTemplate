using System;
using System.Runtime.Serialization;

namespace HSPI_AKTemplate.Exceptions
{
    [Serializable]
    public class HspiConnectionException : HspiException
    {
        public HspiConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public HspiConnectionException()
        {
        }

        public HspiConnectionException(string message) : base(message)
        {
        }

        protected HspiConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}