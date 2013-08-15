
namespace WebSocketServer
{
    /// <summary>
    /// Represents a single vote from a single client.
    /// </summary>
    public class Vote
    {
        /// <summary>
        /// The ClientID of the client that voted.
        /// </summary>
        public string ClientID;

        /// <summary>
        /// The name of the client that voted (to prevent numerous ClientID->Client->Name lookups).
        /// </summary>
        public string ClientName;

        /// <summary>
        /// The vote that the client cast.
        /// </summary>
        public string VoteValue;

        /// <summary>
        /// Returns whether or not this vote was cast by the specified client by comparing the ID and name of this vote.
        /// </summary>
        /// <param name="c">The client to check against.</param>
        /// <returns>True if this vote was cast by the specified client, false otherwise.</returns>
        public bool EqualsClient(Client c)
        {
            return (ClientID == c.Info.ID && ClientName == c.Info.Name);
        }
    }
}
