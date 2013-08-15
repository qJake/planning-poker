using System;

namespace WebSocketServer
{
    /// <summary>
    /// Specifies that this method is able to be called from an admin client only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class WebSocketSecurityCallAttribute : Attribute { }
}
