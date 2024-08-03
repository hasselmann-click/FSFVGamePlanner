using System.Collections;

namespace FSFV.Gameplanner.Fixtures;

public static class GameCreatorUtil
{

    private const string TableValueSeparator = ".";
    private const string TableValueFiller = "X";

    /// <summary>
    /// Creates all the possible games from the single leg (!) game table
    /// </summary>
    /// <param name="teams">The teams</param>
    /// <param name="table">The single leg game table</param>
    public static List<Fixture> CreateGameList(List<string> teams, string[,] table)
    {
        int l = teams.Count;
        int numGames = l * (l - 1) / 2;
        var games = new List<Fixture>(numGames);
        for (int i = 0; i < l; ++i)
        {
            for (int j = 0; j < l; ++j)
            {
                string v = table[i, j];
                if (!string.IsNullOrEmpty(v) && v != TableValueFiller)
                {
                    string[] vSplit = v.Split(TableValueSeparator);
                    if (vSplit.Length > 1)
                    {
                        games.Add(new Fixture { Home = teams[i], Away = teams[j], GameDay = int.Parse(vSplit[0]), GameDayOrder = int.Parse(vSplit[1]) });
                    }
                }
            }
        }

        return games;
    }

    /// <summary>
    /// Generates a single leg game table for an even number of teams
    /// </summary>
    public static string[,] GenerateGameTable(int numberOfTeams)
    {
        // [row,column]
        int l = numberOfTeams;
        string[,] table = new string[l, l];
        for (int r = 0; r < l; ++r)
        {
            table[r, r] = TableValueFiller;
        }

        // day 1
        int day = 1;
        int match1 = 0;
        for (int r = l - 1; r >= l / 2; --r)
        {
            table[r, l - 1 - r] = day + TableValueSeparator + ++match1;
        }

        // follow up days
        for (int d = ++day; d < l; ++d)
        {
            int startRow = l - d;
            int r = startRow;
            int prev_r = r;
            int c = 0;
            int prev_c = c;
            int match = 0;
            BitArray remaining = new(numberOfTeams, true);

            // take the diagonal bottom up
            while (!(r == c || prev_r == c && prev_c == r))
            {

                table[r, c] = d + "." + ++match;
                remaining[r] = false;
                remaining[c] = false;

                prev_r = r--;
                prev_c = c++;
            }

            // cross reference
            // reducing from the "top" to the bottom until a match is found
            var crItems = new List<CrossReferenceItem>(l);
            CrossReference(l, table, d, match, remaining, crItems);

            int stopper = l;
            while (remaining.Cast<bool>().Contains(true) || stopper <= 1)
            {
                CrossReference(l, table, d, match + crItems.Count, remaining, crItems);
                stopper /= 2;
            }

            if (remaining.Cast<bool>().Contains(true))
            {
                throw new SystemException("Could not cross reference remaining teams: " + remaining);
            }

        }

        // printTable(table);
        return table;
    }

    private static void CrossReference(int l, string[,] table, int d, int match, BitArray remaining,
            List<CrossReferenceItem> crItems)
    {
        int top = l - 1, bottom = 0;
        int skipper = 0;
        int crMatch = match;
        BitArray crRemaining = new(remaining);
        while (top > bottom)
        {
            // @formatter:off
            while (top >= 0 && !crRemaining[top]) { --top; };
            while (bottom <= l - 1 && !crRemaining[bottom]) { ++bottom; };
            // @formatter:on

            if (top <= bottom)
            {
                top = l - 1;
                ++bottom;
            }
            else if (string.IsNullOrEmpty(table[top, bottom]))
            {
                CrossReferenceItem crItem = new(top--, bottom++, d + "." + ++crMatch);
                crItems.Add(crItem);
                crRemaining[crItem.row] = false;
                crRemaining[crItem.column] = false;
            }
            else
            {
                // match taken.
                if (crItems.Count == 0)
                { // if the first cross reference doesn't work, only reduce top and continue
                    --top;
                }
                else
                { // else start over crossing while skipping the starting pairs which didn't work
                    ++skipper;
                    top = l - 1;
                    bottom = 0;
                    crMatch = match;
                    for (int i = 0; i <= skipper; ++i)
                    {
                        // @formatter:off
                        while (!crRemaining[top]) { --top; };
                        // @formatter:on
                    }
                    crItems.Clear();
                    crRemaining = new BitArray(remaining);
                    for (int i = 0; i < remaining.Length; ++i)
                    {
                        crRemaining[i] = remaining[i];
                    }
                }
            }
        }

        for (int i = 0; i < l; ++i)
        {
            remaining[i] = crRemaining[i];
        }

        foreach (var cri in crItems)
        {
            table[cri.row, cri.column] = cri.value;
            remaining[cri.row] = false;
            remaining[cri.column] = false;
        }
    }

    private class CrossReferenceItem
    {
        internal int row, column;
        internal string value;

        internal CrossReferenceItem(int row, int column, string value)
        {
            this.row = row;
            this.column = column;
            this.value = value;
        }

    }

    public static string WriteTable(object[,] table)
    {
        using var writer = new StringWriter();
        for (int i = 0; i < table.GetLength(0); ++i)
        {
            writer.Write("|");
            for (int j = 0; j < table.GetLength(1); ++j)
            {
                object v = table[i, j];
                writer.Write(v + "|");
            }
            writer.WriteLine();
            for (int x = 0; x < table.GetLength(0); ++x)
            {
                writer.Write("-");
            }
            writer.WriteLine();
        }
        return writer.ToString();
    }

}
