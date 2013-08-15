using System;
using System.Collections.Generic;
using System.Linq;

namespace WebSocketServer
{
    /// <summary>
    /// Specifies information about a game round.
    /// </summary>
    public class Round
    {
        /// <summary>
        /// The list of votes cast by clients.
        /// </summary>
        public List<Vote> Votes { get; set; }

        /// <summary>
        /// The title of the round.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Whether or not the cards are flipped so that everyone can see them.
        /// </summary>
        public bool Flipped { get; set; }

        /// <summary>
        /// The final decision of the round, decided by an admin.
        /// </summary>
        public string Decision { get; set; }

        /// <summary>
        /// Occurs when the cards are flipped.
        /// </summary>
        public event Action OnCardsFlipped;

        /// <summary>
        /// Initializes a new instance of the Round class.
        /// </summary>
        /// <param name="title">The round title.</param>
        public Round(string title)
        {
            Title = title;
            Votes = new List<Vote>();
            Decision = "";
            Flipped = false;
        }

        /// <summary>
        /// Registers a new vote into this round.
        /// </summary>
        /// <param name="newVote">The new vote to register.</param>
        /// <returns>True on success, false if the vote is a duplicate and was not added.</returns>
        public bool RegisterVote(Vote newVote)
        {
            if (Votes.Where(v => v.ClientID == newVote.ClientID && v.ClientName == newVote.ClientName).Any())
            {
                return false;
            }
            Votes.Add(newVote);
            return true;
        }

        /// <summary>
        /// Sets the final decision on a round, and clears the votes.
        /// </summary>
        /// <param name="card">The card to mark as the "decision".</param>
        public void DecideVote(string card)
        {
            Decision = card;
            Votes.Clear(); // Remove this to send all votes from previous rounds to the client (possibly for later display).
        }

        /// <summary>
        /// Un-does a vote from a specific client. If the specified client didn't vote, this method does nothing.
        /// </summary>
        /// <param name="c">The client whose vote to remove.</param>
        public void UndoVote(Client c)
        {
            Votes.RemoveAll(v => v.EqualsClient(c));
        }

        /// <summary>
        /// Flips the cards so that all clients can see all votes.
        /// </summary>
        public void FlipCards()
        {
            Flipped = true;

            if (OnCardsFlipped != null)
            {
                OnCardsFlipped();
            }
        }

        /// <summary>
        /// Restarts the round by clearing all votes, un-flipping the cards, and clearing any final decision.
        /// </summary>
        public void Restart()
        {
            Votes.Clear();
            Flipped = false;
            Decision = "";
        }
    }
}
