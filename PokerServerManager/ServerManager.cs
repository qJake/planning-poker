using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace PokerServerManager
{
    public class ServerManager
    {
        public static string ServerLocation;
        public static string ServerName;
        public static string MachineName;

        static ServerManager()
        {
            ServerLocation = ConfigurationManager.AppSettings["SocketServerLocation"];
            ServerName = ConfigurationManager.AppSettings["SocketServerName"];
            MachineName = ConfigurationManager.AppSettings["MachineName"];
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static List<Server> GetActiveServerList()
        {
            List<Server> serverList = new List<Server>();
            List<int> pids = new List<int>();

            foreach (Process p in Process.GetProcessesByName(ServerName))
            {
                pids.Add(p.Id);
            }

            foreach (int pid in pids)
            {
                using (NamedPipeClientStream npc = new NamedPipeClientStream(".", "wss" + pid, PipeDirection.In))
                {
                    try
                    {
                        npc.Connect(1500);
                        using (StreamReader sr = new StreamReader(npc))
                        {
                            string data = sr.ReadToEnd();
                            string[] chunks = data.Split(':');
                            if (chunks.Length != 2) continue;
                            serverList.Add(new Server() { Name = chunks[0], Port = int.Parse(chunks[1]), Pid = pid });
                        }
                    }
                    catch (TimeoutException)
                    {
                        continue; // Since this one timed out, we don't care about it, so move on to the next one.
                    }
                }
            }

            return serverList;
        }

        public static void StartServer(string name, int port, string pass)
        {
            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Path.Combine(ServerLocation, ServerName + ".exe"),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    Arguments = name + " " + port + " " + pass,
                    UseShellExecute = true
                }
            };
            p.Start();
        }

        public static bool ShutdownServer(int pid)
        {
            try
            {
                Process p = Process.GetProcessById(pid);

                try
                {
                    p.Kill();
                    return true;
                }
                catch
                {
                    return false; // Couldn't kill it for some reason.
                }
            }
            catch (ArgumentException)
            {
                return false; // Invalid pid.
            }
        }
    }
}