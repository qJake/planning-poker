using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebSocketServer
{
    /// <summary>
    /// Represents a single client connected to the game server. This class also handles the send/receive for the web socket connection.
    /// </summary>
    public class Client : IDisposable
    {
        /// <summary>
        /// Gets the underlying web socket connection.
        /// </summary>
        public WebSocketConnection conn { get; private set; }

        /// <summary>
        /// Gets the card server that this client is associated with.
        /// </summary>
        public CardServer server { get; private set; }

        /// <summary>
        /// The type of this instance of this class (cached for performance).
        /// </summary>
        private Type thisType;

        /// <summary>
        /// Gets information about this client (name, ID, etc).
        /// </summary>
        public ClientInfo Info { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Client class with the specified game server and underlying web socket connection.
        /// </summary>
        /// <param name="parent">The game server that this client is connected to.</param>
        /// <param name="connection">The underlying web socket connection that is associated with this client.</param>
        public Client(CardServer parent, WebSocketConnection connection)
        {
            Info = new ClientInfo();
            server = parent;
            thisType = this.GetType();

            conn = connection;

            // Web socket event hookup (not really events).
            conn.OnMessage = ReceiveMessage;
            conn.OnOpen = Connected;
            conn.OnClose = Disconnected;

            Info.ID = Guid.NewGuid().ToString();
        }

        #region Client Methods

        /// <summary>
        /// Event handler method for the "OnOpen" event from the web socket.
        /// </summary>
        private void Connected()
        {
#if DEBUG
            Console.WriteLine("Client " + Info.ID + " connected.");
#endif
        }

        /// <summary>
        /// Event handler method for the "OnClose" event from the web socket.
        /// </summary>
        private void Disconnected()
        {
#if DEBUG
            Console.WriteLine("Client " + Info.ID + " disconnected.");
#endif
            server.RemoveClient(this);
            server.BroadcastClientList();
        }

        /// <summary>
        /// Event handler method for the "OnMessage" event from the web socket.
        /// </summary>
        /// <param name="message">The message received from the browser/client.</param>
        private void ReceiveMessage(string message)
        {
#if DEBUG
            Console.WriteLine("Client " + Info.ID + " sent a message: " + message);
#endif

            ServerRequestInfo request = null;

            // Ensure the message is a JSON message.
            try
            {
                request = JsonConvert.DeserializeObject<ServerRequestInfo>(message);
            }
            catch
            {
                SendError("ReceiveMessage", "Server request was not in JSON format.");
                return;
            }

            // Attempt to find a valid method in this class to invoke.
            MethodInfo method = thisType.GetMethods().DefaultIfEmpty(null).Where(m => m.Name.ToUpper() == request.MethodName.ToUpper()).SingleOrDefault();
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

        /// <summary>
        /// Sends a message through the underlying web socket connection to the browser/client.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="parameters">The parameters to include with the command.</param>
        public void SendMessage(string command, params JProperty[] parameters)
        {
            if (conn.Socket.Connected)
            {
                conn.Send(new JObject(new JProperty("Command", command), new JProperty("Error", false), parameters).ToString());
            }
        }

        /// <summary>
        /// Sends an error through the underlying web socket connection to the browser/client.
        /// </summary>
        /// <param name="source">The source of the error.</param>
        /// <param name="message">The error message.</param>
        public void SendError(string source, string message)
        {
            if (conn.Socket.Connected)
            {
                conn.Send(new JObject(new JProperty("Source", source), new JProperty("Error", true), new JProperty("ErrorMessage", message)).ToString());
            }
        }

        #endregion

        #region Socket Calls

        /// <summary>
        /// Registers this client with the game server.
        /// </summary>
        /// <param name="name">The friendly name of the client.</param>
        /// <param name="spectator">Whether or not this client is initially a spectator or not (they can toggle this after connecting).</param>
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

        /// <summary>
        /// Swaps the spectator flag for this client, and updates any associated game logic accordingly.
        /// </summary>
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

        /// <summary>
        /// Attempts to register this client as a game admin with the specified password.
        /// </summary>
        /// <param name="pass">The user's attempted password.</param>
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

        /// <summary>
        /// Registers a vote with the game server from this client.
        /// </summary>
        /// <param name="vote">The card/vote to register.</param>
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
    
        /// <summary>
        /// Un-does a registered vote for this user for the active round.
        /// </summary>
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

        /// <summary>
        /// Starts a new round for the players.
        /// </summary>
        /// <param name="title">The title of the round.</param>
        [WebSocketSecurityCall]
        public void NewRoundRequest(string title)
        {
            if (!server.BeginNewRound(title))
            {
                SendError("NewRoundRequest", "Another round is already in progress, and must be finalized before starting a new round.");
            }
        }

        /// <summary>
        /// Finalizes the round by attempting to take the majority vote. If there is no clear majority, an error is returned.
        /// </summary>
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

        /// <summary>
        /// Finalizes the current round with the specified vote/card.
        /// </summary>
        /// <param name="vote">The card to "accept" and finalize with (will be displayed to each player).</param>
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

        /// <summary>
        /// Restarts the current round by discarding all votes and flipping the cards back over again.
        /// </summary>
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

        /// <summary>
        /// Flips the cards so that all players can see all cards. Users that did not vote will not appear in the list.
        /// </summary>
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

        /// <summary>
        /// Discards the current round completely.
        /// </summary>
        [WebSocketSecurityCall]
        public void DiscardActiveRound()
        {
            server.DiscardActiveRound();
        }

        /// <summary>
        /// Sorts the results in numeric ascending order and updates each client with the new order.
        /// </summary>
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

        /// <summary>
        /// Retrieves the stored card list on the server, so the user can edit it.
        /// </summary>
        [WebSocketSecurityCall]
        public void GetCardList()
        {
            SendMessage("GetCardList", new JProperty("CardList", CardSet.Default.ToHumanString()));
        }

        /// <summary>
        /// Updates the current cardset with the new cards the user entered.
        /// </summary>
        /// <param name="newCards">The set of new cards to use, each separated by a comma.</param>
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

        /// <summary>
        /// Sets the value of a global server setting.
        /// </summary>
        /// <param name="key">The key of the setting.</param>
        /// <param name="value">The value to set.</param>
        [WebSocketSecurityCall]
        public void SetSetting(string key, string value)
        {
            server.UpdateSetting(key, value);
        }

        #endregion

        /// <summary>
        /// Closes the web socket connection and disposes of this client.
        /// </summary>
        public void Dispose()
        {
            if (conn != null)
            {
                conn.Close(0);
                conn = null;
            }
            server = null;
            thisType = null;
            Info = null;
        }
    }
}
