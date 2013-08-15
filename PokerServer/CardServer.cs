using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fleck;
using Newtonsoft.Json.Linq;

namespace WebSocketServer
{
    /// <summary>
    /// Implements a single Poker Server instance, with various game functions / game data storage.
    /// </summary>
    public class CardServer
    {
        /// <summary>
        /// The current list of connected clients.
        /// </summary>
        public List<Client> Clients { get; private set; }

        /// <summary>
        /// The list of current and past rounds that have bene played.
        /// </summary>
        public List<Round> Rounds { get; private set; }

        /// <summary>
        /// The current list of logged-in game admins.
        /// </summary>
        public List<Client> Admins { get; private set; }

        /// <summary>
        /// The admin password.
        /// </summary>
        public string AdminPassword { get; private set; }

        /// <summary>
        /// The settings store.
        /// </summary>
        public Dictionary<string, string> Settings { get; private set; }

        /// <summary>
        /// Helper property to retrieve the current active round from the list of rounds. A round is "active" if it does not have a decision.
        /// Due to game rules, there should only be one Active Round at a time, otherwise an exception is thrown when accessing this property.
        /// </summary>
        public Round ActiveRound
        {
            get
            {
                return Rounds.DefaultIfEmpty(null)
                             .Where(r => r != null && string.IsNullOrWhiteSpace(r.Decision))
                             .SingleOrDefault();
            }
        }

        /// <summary>
        /// Initializes a new instance of the CardServer class, with the specified admin password.
        /// </summary>
        /// <param name="adminPass">The admin password for this game.</param>
        public CardServer(string adminPass)
        {
            Clients = new List<Client>();
            Admins = new List<Client>();
            Rounds = new List<Round>();
            AdminPassword = adminPass;
            Settings = new Dictionary<string, string>();
        }

        #region Server Functions

        /// <summary>
        /// Accepts a new WebSocketConnection object and creates a new game <see cref="Client" /> based on it.
        /// </summary>
        /// <param name="conn">The connection to use for the new client.</param>
        public void NewClient(WebSocketConnection conn)
        {
            Clients.Add(new Client(this, conn));
        }

        /// <summary>
        /// Removes the specified client from the game, usually due to a disconnect. This also cleans up their votes and other game data.
        /// </summary>
        /// <param name="c">The client to remove.</param>
        public void RemoveClient(Client c)
        {
            // Remove this client's votes if they have any.
            if (ActiveRound != null)
            {
                ActiveRound.Votes.RemoveAll(v => v.ClientID == c.Info.ID && v.ClientName == c.Info.Name);
            }

            if (Admins.Contains(c))
            {
                Admins.Remove(c);
            }
            // Some race case is going on here... we need to check if there's even anything to remove.
            if (Clients.Count > 0)
            {
                Clients.Remove(c);
            }

            c = null;

            if (Clients.All(cl => cl.Info.IsSpectator) && ActiveRound != null)
            {
                DiscardActiveRound();
                BroadcastError(c, "RoundStopped", "There are no more non-spectator players, so the current round has been automatically discarded.");
            }

            // Check that every person hasn't already voted.
            CheckVoteCount();
        }

        /// <summary>
        /// Attempts to register the specified client as an admin using the specified password. This checks against the server password, and will
        /// return true if the password was correct (and the client was elevated to an admin), otherwise it returns false.
        /// </summary>
        /// <param name="c">The client requesting administrative priveleges.</param>
        /// <param name="pass">The password that the client provided, to be checked against the actual password for the server.</param>
        /// <returns></returns>
        public bool RegisterAdmin(Client c, string pass)
        {
            if (pass == AdminPassword)
            {
                Admins.Add(c);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Broadcasts a JSON message to all clients. If an origin is specified, the message is not re-broadcast back to the origin.
        /// </summary>
        /// <param name="origin">Optionally, a client origin which the message will not be broadcast back to. Set to <c>null</c> to broadcast to every client.</param>
        /// <param name="message">The message to broadcast.</param>
        /// <param name="parameters">An array of <see cref="JProperty" /> objects to include as parameters with the message.</param>
        public void BroadcastMessage(Client origin, string message, params JProperty[] parameters)
        {
            if (origin != null)
            {
                Clients.Where(c => !c.Equals(origin)).ToList().ForEach(c => c.SendMessage(message, parameters));
            }
            else
            {
                Clients.ForEach(c => c.SendMessage(message, parameters));
            }
        }

        /// <summary>
        /// Broadcasts an error message to all clients. If an origin is specified, the message is not re-broadcast back to the origin.
        /// </summary>
        /// <param name="origin">Optionally, a client origin which the error will not be broadcast back to. Set to <c>null</c> to broadcast to every client.</param>
        /// <param name="source">The source of the error.</param>
        /// <param name="message">The error message to broadcast.</param>
        /// <param name="sendBack">Whether or not to send the error back to the origin client or not.</param>
        public void BroadcastError(Client origin, string source, string message, bool sendBack = false)
        {
            if (origin != null && !sendBack)
            {
                Clients.Where(c => !c.Equals(origin)).ToList().ForEach(c => c.SendError(source, message));
            }
            else
            {
                Clients.ForEach(c => c.SendError(source, message));
            }
        }

        /// <summary>
        /// Sends the most recent client list to all of the clients.
        /// </summary>
        public void BroadcastClientList()
        {
            List<JObject> ClientList = new List<JObject>();
            foreach (var c in Clients)
            {
                ClientList.Add(JObject.FromObject(c.Info));
            }

            BroadcastMessage(null, "ClientList", new JProperty("Clients", ClientList.ToArray()));

            // Whenever we broadcast the client list, also re-broadcast the game state (for UI updates).
            BroadcastGameState();
        }

        /// <summary>
        /// Helper method to update a setting with a value, or add it if it doesn't exist yet.
        /// </summary>
        /// <param name="name">The name (or key) of the setting.</param>
        /// <param name="value">The value of the setting.</param>
        public void UpdateSetting(string name, string value)
        {
            if(Settings.ContainsKey(name))
            {
                Settings[name] = value;
            }
            else
            {
                Settings.Add(name, value);
            }
        }

        /// <summary>
        /// Retrieves a setting, or returns the default value if the setting doesn't exist.
        /// </summary>
        /// <param name="key">The key of the setting to retrieve.</param>
        /// <param name="defaultValue">The default value, if the setting was not found in the store.</param>
        /// <returns>The setting, or the default value, if the setting was not found in the store.</returns>
        public string GetSetting(string key, string defaultValue)
        {
            if (Settings.ContainsKey(key))
            {
                return Settings[key];
            }
            else
            {
                return defaultValue;
            }
        }

        #endregion

        #region Game Functions

        /// <summary>
        /// Compiles the state of the current round and serializes it as a <see cref="JObject" />.
        /// </summary>
        /// <returns>A JSON object representing the current round state.</returns>
        public JObject GetRoundState()
        {
            List<JObject> RoundData = new List<JObject>();
            Rounds.ForEach(r => RoundData.Add(JObject.FromObject(r)));

            JObject Data = new JObject();
            Data.Add(new JProperty("CardSet", CardSet.Default.ToString()));
            Data.Add(new JProperty("RoundData", RoundData.ToArray()));

            return Data;
        }

        /// <summary>
        /// Broadcasts the current game state to each of the connected clients.
        /// </summary>
        public void BroadcastGameState()
        {
            BroadcastMessage(null, "GameState", new JProperty("Data", GetRoundState()));
        }

        /// <summary>
        /// Begins a new round.
        /// </summary>
        /// <param name="title">The title of the new round.</param>
        /// <returns>True on success, false if there is already an active round going.</returns>
        public bool BeginNewRound(string title)
        {
            if (ActiveRound != null)
            {
                return false;
            }
            
            Round r = new Round(title);
            r.OnCardsFlipped += AutoSortOnFlip;
            Rounds.Add(r);

            BroadcastGameState();
            return true;
        }

        /// <summary>
        /// Registers a new vote for the specified client, with the specified vote value.
        /// </summary>
        /// <param name="c">The client who voted.</param>
        /// <param name="vote">The value of the vote.</param>
        /// <returns>True if the vote was registered, false if a failure occurred (no active round, invalid vote value, client is spectator, client already voted).</returns>
        public bool RegisterVote(Client c, string vote)
        {
            if (ActiveRound == null || ActiveRound.Flipped)
            {
                return false;
            }
            if (!CardSet.Default.Cards.Contains(vote))
            {
                return false;
            }
            if (c.Info.IsSpectator)
            {
                return false;
            }

            bool result = ActiveRound.RegisterVote(new Vote() { ClientID = c.Info.ID, ClientName = c.Info.Name, VoteValue = vote });

            CheckVoteCount();

            if(result)
            {
                // If successful, broadcast the new state to the clients.
                BroadcastGameState();
            }
            return result;
        }

        /// <summary>
        /// Removes a vote that the client previously registered.
        /// </summary>
        /// <param name="c">The client whose vote to remove.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool UndoVote(Client c)
        {
            if (ActiveRound != null && ActiveRound.Votes.Where(v => v.EqualsClient(c)).Any())
            {
                if (ActiveRound.Votes.Count >= Clients.Count)
                {
                    return false;
                }
                else
                {
                    ActiveRound.UndoVote(c);
                    BroadcastGameState();
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sorts the cards from lowest to highest using the <see cref="VoteComparer" /> class.
        /// </summary>
        /// <returns>True on success, false if there are no cards to sort or there is no active round.</returns>
        public bool SortCards()
        {
            if (ActiveRound != null && string.IsNullOrWhiteSpace(ActiveRound.Decision) && ActiveRound.Flipped)
            {
                ActiveRound.Votes.Sort(VoteComparer.Default);
                BroadcastGameState();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if everyone who is logged in, and is playing, has voted. If so, after a 1s delay, the cards are flipped.
        /// </summary>
        private void CheckVoteCount()
        {
            // If every logged in non-spectator has voted, auto-flip the cards.
            if (ActiveRound != null && Clients.Where(cl => ActiveRound.Votes.Where(v => v.ClientID == cl.Info.ID).Any()).Count() == Clients.Where(c => !c.Info.IsSpectator).Count())
            {
                // Tell each client to lock its Undo button.
                Clients.ForEach(c => c.SendMessage("LockUndo"));

                // Wait a few moments, then flip the cards.
                new Thread(() => { Thread.Sleep(1000); ActiveRound.FlipCards(); BroadcastGameState(); }).Start();
            }
        }

        /// <summary>
        /// Discards the active round and all of its associted data.
        /// </summary>
        public void DiscardActiveRound()
        {
            if (ActiveRound != null)
            {
                Rounds.Remove(ActiveRound);
                BroadcastGameState();
            }
        }

        /// <summary>
        /// Helper method; when called, if the "AutoSort" setting is on, this will flip the cards.
        /// </summary>
        private void AutoSortOnFlip()
        {
            if (GetSetting("AutoSort", "0") == "1")
            {
                SortCards();
            }
        }

        #endregion
    }
}
