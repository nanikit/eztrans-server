namespace EztransServer.Core {
  public class EztransNotFoundException : EztransException {
    public EztransNotFoundException(string message) : base($"EZTrans does not found. {message}") { }
  }
}
