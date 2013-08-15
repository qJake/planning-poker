using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebSocketServer
{
    public class Client : IDisposable
    {
        public WebSocketConnection conn;
        public CardServer server;
        public Type serverType;

        public ClientInfo Info;

        public Client(CardServer parent, WebSocketConnection connection)
        {
            Info = new ClientInfo();
            server = parent;
            serverType = this.GetType();
            conn = connection;
            conn.OnMessage = ReceiveMessage;
            conn.OnOpen = Connected;
            conn.OnClose = Disconnected;
            Info.ID = Guid.NewGuid().ToString();
        }

        #region Client Methods

        private void Connected()
        {
#if DEBUG
            Console.WriteLine("Client " + Info.ID + " connected.");
#endif
        }

        private void Disconnected()
        {
#if DEBUG
            Console.WriteLine("Client " + Info.ID + " disconnected.");
#endif
            server.RemoveClient(this);
            server.BroadcastClientList();
        }

        private void ReceiveMessage(string message)
        {
#if DEBUG
            Console.WriteLine("Client " + Info.ID + " sent a message: " + message);
#endif

            ServerRequestInfo request = null;
            try
            {
                request = JsonConvert.DeserializeObject<ServerRequestInfo>(message);
            }
            catch
            {
                SendError("ReceiveMessage", "Server request was not in JSON format.");
                return;
            }

            MethodInfo method = serverType.GetMethods().DefaultIfEmpty(null).Where(m => m.Name.ToUpper() == request.MethodName.ToUpper()).SingleOrDefault();
            if (method != null && method.GetCustomAttributes(false).Where(a => a is WebSocketSecurityCallAttribute).Count() > 0)
            {
                if (server.Admins.Contains(this))
                {
                    // Security-enabled call
                    method.Invoke(this, request.MethodArguments);
                }
                else
                {
                    // Security failure
                    SendError("SecurityFailure", "You are not authorized to perform this function.");
                }
            }
            else if (method != null && method.GetCustomAttributes(false).Where(a => a is WebSocketCallAttribute).Count() > 0)
            {
                // Standard method call
                method.Invoke(this, request.MethodArguments);
            }
            else
            {
                Console.WriteLine("Client " + Info.ID + " has broadcast an invalid message: " + message);
            }
        }

        public void SendMessage(string command, params JProperty[] parameters)
        {
            if (conn.Socket.Connected)
            {
                conn.Send(new JObject(new JProperty("Command", command), new JProperty("Error", false), parameters).ToString());
            }
        }

        public void SendError(string source, string message)
        {
            if (conn.Socket.Connected)
            {
                conn.Send(new JObject(new JProperty("Source", source), new JProperty("Error", true), new JProperty("ErrorMessage", message)).ToString());
            }
        }

        #endregion

        #region Socket Calls

        [WebSocketCall]
        public void RegisterClient(string name, string spectator)
        {
            Info.Name = name;
            SendMessage("RegisterClient", new JProperty("ClientID", Info.ID));

            if (spectator == "1")
            {
                Info.IsSpectator = true;
            }

            server.BroadcastClientList();

            // Bring this client up to speed on the round state.
            SendMessage("GameState", new JProperty("Data", server.GetRoundState()));
        }

        [WebSocketCall]
        public void SwapSpectator()
        {
            Info.IsSpectator = !Info.IsSpectator;
            SendMessage("SpectatorSwap", new JProperty("IsSpectator", Info.IsSpectator));
            server.BroadcastClientList();

            // If there are no more non-spectators, end the round if there is one active.
            if (server.ActiveRound != null && server.Clients.All(c => c.Info.IsSpectator))
            {
                server.DiscardActiveRound();
                server.BroadcastError(null, "RoundStopped", "There are no more non-spectator players, so the current round has been automatically discarded.");
            }
            else
            {
                server.BroadcastGameState();
            }
        }

        [WebSocketCall]
        public void RegisterAdmin(string pass)
        {
            if (server.RegisterAdmin(this, pass))
            {
                Info.IsAdmin = true;
                SendMessage("RegisterAdmin");
                server.BroadcastClientList(); //Re-broadcast for the Admin role.
            }
            else
            {
                SendError("RegisterAdmin", "Invalid admin password, or admin already logged in.");
            }
        }

        [WebSocketCall]
        public void RegisterVote(string vote)
        {
            if (server.RegisterVote(this, vote))
            {
                SendMessage("RegisterVote");
            }
            else
            {
                SendError("RegisterVote", "Cannot vote at this time. You may have already voted, or a round may not be in progress.");
            }
        }
    
        [WebSocketCall]
        public void UndoVote()
        {
            if (server.UndoVote(this))
            {
                SendMessage("UndoVote");
            }
            else
            {
                SendError("UndoVote", "Unable to undo vote at this time.");
            }
        }

        #endregion

        #region Admin Functions

        [WebSocketSecurityCall]
        public void NewRoundRequest(string title)
        {
            if (!server.BeginNewRound(title))
            {
                SendError("NewRoundRequest", "Another round is already in progress, and must be finalized before starting a new round.");
            }
        }

        [WebSocketSecurityCall]
        public void TakeMajority()
        {
            if (server.ActiveRound == null)
            {
                SendError("TakeMajority", "There is no active round for which to take the majority on.");
                return;
            }
            string majority = Utilities.Majority(server.ActiveRound.Votes.Select(v => v.VoteValue).ToList());

            if (majority == null)
            {
                SendError("TakeMajority", "Cannot take a majority at this time. This usually means there is a tie.");
                return;
            }
            SendMessage("TakeMajority");
            server.ActiveRound.DecideVote(majority);
            server.BroadcastGameState();
        }

        [WebSocketSecurityCall]
        public void FinalizeVote(string vote)
        {
            if (server.ActiveRound == null)
            {
                SendError("FinalizeVote", "There is no active round for which to finalize the vote on.");
                return;
            }
            if (!CardSet.Default.Cards.Contains(vote))
            {
                SendError("FinalizeVote", "The value you entered does not match one of the current game cards.");
                return;
            }
            SendMessage("FinalizeVote");
            server.ActiveRound.DecideVote(vote);
            server.BroadcastGameState();
        }

        [WebSocketSecurityCall]
        public void RestartRound()
        {
            if (server.ActiveRound == null)
            {
                SendError("RestartRound", "There is no active round to restart.");
                return;
            }
            SendMessage("RestartRound");
            server.ActiveRound.Restart();
            server.BroadcastGameState();
        }

        [WebSocketSecurityCall]
        public void FlipCards()
        {
            if (server.ActiveRound == null || server.ActiveRound.Flipped)
            {
                SendError("FlipCards", "There is no active round for which cards have not already been flipped.");
                return;
            }
            SendMessage("FlipCards");
            server.ActiveRound.FlipCards();
            server.BroadcastGameState();
        }

        [WebSocketSecurityCall]
        public void DiscardActiveRound()
        {
            server.DiscardActiveRound();
        }

        [WebSocketSecurityCall]
        public void SortCards()
        {
            if (server.SortCards())
            {
                SendMessage("SortCards");
            }
            else
            {
                SendError("SortCards", "Unable to sort the cards at this time.");
            }
        }

        [WebSocketSecurityCall]
        public void GetCardList()
        {
            SendMessage("GetCardList", new JProperty("CardList", CardSet.Default.ToHumanString()));
        }

        [WebSocketSecurityCall]
        public void SetCards(string newCards)
        {
            if (server.ActiveRound != null)
            {
                SendError("SetCards", "You cannot modify card settings while a round is active.");
            }

            List<string> newCardList = new List<string>();

            string[] cards = newCards.Split(',');
            foreach (string card in cards)
            {
                if (!string.IsNullOrWhiteSpace(card))
                {
                    newCardList.Add(card.Trim());
                }
            }
            if (newCardList.Count < 2)
            {
                SendError("SetCards", "Not enough valid cards provided. You must provide at least two cards to add.");
                return;
            }

            CardSet.Default.ApplyNewCards(newCardList.ToArray());
            server.BroadcastGameState();
            SendMessage("SetCards");
        }

        [WebSocketSecurityCall]
        public void SetSetting(string key, string value)
        {
            server.UpdateSetting(key, value);
        }

        #endregion

        public void Dispose()
        {
            if (conn != null)
            {
                conn.Close(0);
                conn = null;
            }
            server = null;
            serverType = null;
            Info = null;
        }
    }
}
