using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebSocketServer;

namespace PokerServer.Utilities
{
    internal static class Logger
    {
        public static void WriteToLog(string filename, string data)
        {
            using (StreamWriter file = new StreamWriter(filename, true))
            {
                file.WriteLine(data);
            }
        }

        public static void WriteToLog(Round round)
        {
            StringBuilder sb = new StringBuilder("================")
                .AppendLine()
                .AppendLine(string.Format("Round topic : \"{0}\"", round.Title))
                .AppendLine("Votes:")
                .Append(ParceVotes(round.Votes))
                .AppendLine(string.Format("--> Estimation: {0}", round.Decision))
                .AppendLine("================");
            WriteToLog(Path.Combine(Environment.GetLogicalDrives()[1], "PokerLog", string.Format("{0}.{1}", DateTime.Today.ToShortDateString(), "txt")), sb.ToString());
        }

        private static string ParceVotes(List<Vote> votes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Vote vote in votes)
            {
                sb.AppendLine(string.Format("{0} - {1}", vote.ClientName, vote.VoteValue));
            }
            return sb.ToString();
        }
    }
}
