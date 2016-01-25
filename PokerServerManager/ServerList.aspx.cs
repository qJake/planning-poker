using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PokerServerManager
{
    public partial class ServerList : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();
            var obj = new JObject(new JProperty("machineName", ServerManager.MachineName), new JProperty("servers", JArray.FromObject(ServerManager.GetActiveServerList())));
            Response.BinaryWrite(Encoding.UTF8.GetBytes(obj.ToString()));
            Response.End();
        }
    }
}