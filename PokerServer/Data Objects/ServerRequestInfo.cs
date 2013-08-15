
namespace WebSocketServer
{
    /// <summary>
    /// Specifies information about a request coming in to the server from a client.
    /// </summary>
    public class ServerRequestInfo
    {
        /// <summary>
        /// The method name to invoke.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Parameters to that method. The count of this array must match the count of parameters on the method being invoked.
        /// </summary>
        public string[] MethodArguments { get; set; }
    }
}
