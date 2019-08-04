using System;

namespace Bcgs.ZKTeco.BioMatrix
{
    public class ZkTecoClientException : Exception
    {
        public ZkTecoClientException()
        {
        }

        public ZkTecoClientException(string message)
            : base(message)
        {
        }

        public ZkTecoClientException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
