using System.Collections.Generic;

namespace WebSocketServer
{
    /// <summary>
    /// Represents the set of cards in the game.
    /// </summary>
    public class CardSet
    {
        /// <summary>
        /// Where the instance is stored.
        /// </summary>
        public static CardSet _instance;

        /// <summary>
        /// Where the current cardset is stored.
        /// </summary>
        public static CardSet Default
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CardSet("½", "1", "2", "3", "5", "8", "13", "20", "30", "50", "100", "?");
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets the list of card values in the current cardset.
        /// </summary>
        public List<string> Cards { get; private set; }

        /// <summary>
        /// Private constructor. To create a new cardset, call ApplyNewCards().
        /// </summary>
        /// <param name="NewCards">The new cards to use.</param>
        private CardSet(params string[] NewCards)
        {
            Cards = new List<string>(NewCards);
        }

        /// <summary>
        /// Returns a comma-separated list of card values.
        /// </summary>
        public override string ToString()
        {
            return string.Join(",", Cards.ToArray());
        }

        /// <summary>
        /// Returns a comma-separated list of card values, with a space in-between.
        /// </summary>
        public string ToHumanString()
        {
            return string.Join(", ", Cards.ToArray());
        }

        /// <summary>
        /// Applies a new list of cards to the stored cardset.
        /// </summary>
        /// <param name="cardList">The new list of string values to use for the new cards.</param>
        public void ApplyNewCards(string[] cardList)
        {
            _instance = new CardSet(cardList);
        }
    }
}
