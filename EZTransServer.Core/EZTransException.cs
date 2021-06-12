using System;

namespace EZTransServer.Core
{
    public class EztransException : Exception
    {
        public EztransException(string message) : base(message) { }
    }
}
