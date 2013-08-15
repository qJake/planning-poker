using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace WebSocketServerManager
{
    public partial class _Default : System.Web.UI.Page
    {
        public static List<Server> ActiveServerList = new List<Server>();

        protected void Page_Load(object sender, EventArgs ea)
        {
            Message.Visible = false; // Always reset this, and override below if necessary.
            Message.Text = "";

            if (!IsPostBack)
            {
                BindServerList();
            }
        }

        protected override void OnInit(EventArgs ea)
        {
            base.OnInit(ea);

            Refresh.Click += (_, __) =>
            {
                BindServerList();
            };

            Create.Click += (_, __) =>
            {
                CreateServer();
            };

            ServerContainer.ItemCommand += (s, e) =>
            {
                try
                {
                    Server sts = ActiveServerList.Where(se => se.Name == e.CommandArgument.ToString()).First();
                    if (ServerManager.ShutdownServer(sts.Pid))
                    {
                        Success("Server \"" + sts.Name + "\" has been shut down.");
                    }
                    else
                    {
                        Error("There was a problem shutting down the server \"" + sts.Name + "\".");
                    }
                }
                catch
                {
                    Error("Unable to locate a server with that name.");
                }

                BindServerList();
            };
        }

        private void BindServerList()
        {
            ActiveServerList = ServerManager.GetActiveServerList();

            ServerContainer.DataSource = ActiveServerList;
            ServerContainer.DataBind();
        }

        private void CreateServer()
        {
            if (ActiveServerList.Where(s => s.Name == ServerName.Text || s.Port == int.Parse(PortNumber.Text)).Any())
            {
                Error("Unable to create server: A server with the same name or port number is already running.");
                return;
            }

            ServerManager.StartServer(ServerName.Text, int.Parse(PortNumber.Text), Password1.Text);

            Thread.Sleep(500); // Give it a second to start up before we refresh the server list.

            BindServerList();

            ServerName.Text = "";
            PortNumber.Text = "";
            Password1.Text = "";
            Password2.Text = "";

            Success("The server has been started.");
        }

        private void Success(string message)
        {
            Message.Visible = true;
            Message.Text = message;
            Message.CssClass = "message good";
        }

        private new void Error(string message)
        {
            Message.Visible = true;
            Message.Text = message;
            Message.CssClass = "message bad";
        }
    }
}
