
namespace WebSocketServer
{
    /// <summary>
    /// Specifies information about a single connected client.
    /// </summary>
    public class ClientInfo
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsSpectator { get; set; }
    }
}
