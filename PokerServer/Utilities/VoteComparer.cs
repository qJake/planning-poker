using System.Collections.Generic;
using System.Linq;

namespace WebSocketServer
{
    /// <summary>
    /// Implements the <see cref="IComparer<T>" /> interface for the <see cref="Vote" /> class.
    /// </summary>
    public class VoteComparer : IComparer<Vote>
    {
        /// <summary>
        /// Stores the instance of the vote comparer.
        /// </summary>
        private static VoteComparer _instance;

        /// <summary>
        /// Stores the singleton instance of this VoteComparer.
        /// </summary>
        public static VoteComparer Default
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VoteComparer();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to facilitate singleton pattern.
        /// </summary>
        private VoteComparer() { }

        /// <summary>
        /// Compares two votes (as strings) to see which one is greater, or if they are equal.
        /// </summary>
        /// <param name="x">The first vote.</param>
        /// <param name="y">The second vote.</param>
        /// <returns>-1, 0, or 1 depending on the comparison result.</returns>
        public int Compare(Vote x, Vote y)
        {
            int xNum = 0;
            int yNum = 0;

            // Try to get a numeric value first. If these fail, they set the out values to 0.
            int.TryParse(x.VoteValue, out xNum);
            int.TryParse(y.VoteValue, out yNum);

            // If the parsed values are 0, set the value to the ASCII value, but add 1000 so they show up at the end of the list.
            if (xNum == 0) xNum = x.VoteValue.Take(1).First() + 1000;
            if (yNum == 0) yNum = y.VoteValue.Take(1).First() + 1000;

            // Return the sorting integer.
            if (xNum > yNum)
            {
                return 1;
            }
            else if (xNum < yNum)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
