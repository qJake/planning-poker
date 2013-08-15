using System;

namespace WebSocketServer
{
    /// <summary>
    /// Specifies that this method is able to be invoked from any client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class WebSocketCallAttribute : Attribute { }
}
