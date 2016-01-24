using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace PokerWebClient.api
{
    public partial class GetUserName : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();
            var obj = new JObject(new JProperty("userName", this.User.Identity.Name));
            Response.BinaryWrite(Encoding.UTF8.GetBytes(obj.ToString()));
            Response.End();
        }
    }
}