using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebSocketServer
{
    /// <summary>
    /// Contains utility methods.
    /// </summary>
    public class Utilities
    {
        /// <summary>
        /// Calculates the majority vote based on a string list of votes. Includes support for cards/votes marked as '½'.
        /// </summary>
        /// <param name="list">A list of strings to calculate the majority for.</param>
        /// <returns>The majority value, or if there is a tie or no majority, <c>null</c>.</returns>
        public static string Majority(List<string> list)
        {
            var numericPattern = new Regex(@"^-?\d+$");
            var result = list.Where(x => x != null && (numericPattern.IsMatch(x) || x == "½"))
                             .GroupBy(x => x)
                             .OrderByDescending(pair => pair.Count());
            var resultList = result.ToList();
            if (resultList.Count == 0)
            {
                return null;
            }
            var majority = resultList[0].Key;
            if (result.Where(r => r.Count() == resultList[0].Count()).Count() > 1)
            {
                return null;
            }
            return majority;
        }
    }
}
