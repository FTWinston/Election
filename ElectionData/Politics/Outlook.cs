using System.Collections.Generic;
using System.Linq;

namespace ElectionData.Politics
{
    public class Outlook
    {
        public Outlook(IEnumerable<Issue> issues)
        {
            PositionsByPriority = new List<Position>(
                issues.Select(issue => new Position(issue, 0))
            );

            PositionsByIssue = new Dictionary<Issue, Position>(PositionsByPriority.Count);

            foreach (var position in PositionsByPriority)
            {
                PositionsByIssue.Add(position.Issue, position);
            }
        }

        List<Position> PositionsByPriority { get; }
        Dictionary<Issue, Position> PositionsByIssue { get; }

        /// <summary>
        /// Determine how closely-aligned this outlook considers another one to be.
        /// Note that this is non-commutative: approval might not be reciprocated!
        /// </summary>
        /// <param name="other">The outlook to compare this one to</param>
        /// <returns>A number between 0 and 1</returns>
        public float SimilarityWith(Outlook other)
        {
            float total = 0;
            float maxPossibleValue = 0;
            float priorityScale = PositionsByPriority.Count;

            foreach (var localPosition in PositionsByPriority)
            {
                var otherPosition = other.PositionsByIssue[localPosition.Issue];

                if (otherPosition != null)
                {
                    total += localPosition.SimilarityWith(otherPosition.Value) * priorityScale;
                }

                maxPossibleValue += priorityScale;
                priorityScale--;
            }

            return total / maxPossibleValue;
        }
    }
}
