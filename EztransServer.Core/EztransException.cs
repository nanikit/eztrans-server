using System;

namespace EztransServer.Core {
  public class EztransException : Exception {
    public EztransException(string message) : base(message) { }
  }
}
