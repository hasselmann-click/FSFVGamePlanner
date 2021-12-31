//using FSFV.Gameplanner.Data.Model;
//using FSFV.Gamplanner.Data.Model;
//using Microsoft.EntityFrameworkCore.Internal;
//using System;
//using System.Collections.Generic;

//namespace FSFV.Gameplanner.Service.Fixtures
//{
//    public static class old_GameCreatorUtil

//    {

//        private const string TableValueSeparator = ".";
//        private const string TableValueSeparatorPattern = @"\" + TableValueSeparator;
//        private const string TableValueFiller = "X";

//        public static List<Game> GetFixtures(List<Team> teams)
//        {
//            var table = GenerateGameTable(teams);
//            return CreateGameList(teams, table);
//        }

//        /// <summary>
//        /// Creates all the possible games from the single leg (!) game table
//        /// </summary>
//        /// <param name="teams">The teams</param>
//        /// <param name="table">The single leg game table</param>
//        private static List<Game> CreateGameList(List<Team> teams, String[,] table)
//        {
//            int l = teams.Count;
//            int numGames = (l * (l - 1)) / 2;
//            var games = new List<Game>(numGames);
//            for (int i = 0; i < l; ++i)
//            {
//                for (int j = 0; j < l; ++j)
//                {
//                    String v = table[i, j];
//                    if (!string.IsNullOrEmpty(v))
//                    {
//                        String[] vSplit = v.Split(TableValueSeparatorPattern);
//                        if (vSplit.Length > 1)
//                        {
//                            games.Add(new Game { Home = teams[i], Away = teams[j], GameDay = Int32.Parse(vSplit[0]), GameDayOrder = Int32.Parse(vSplit[1]) });
//                        }
//                    }
//                }
//            }

//            return games;
//        }

//        /// <summary>
//        /// Generates a single leg game table. Each team plays each team once!
//        /// </summary>
//        private static String[,] GenerateGameTable(List<Team> teams) // TODO only make the count of teams, not a list. The teams themself are not interesting here. Then make this a util.
//        {
//            // [row,column]
//            int l = teams.Count;
//            String[,] table = new String[l, l];
//            for (int r = 0; r < l; ++r)
//            {
//                table[r, r] = TableValueFiller;
//            }

//            // day 1
//            int day = 1;
//            int match1 = 0;
//            for (int r = l - 1; r >= (l / 2); --r)
//            {
//                table[r, l - 1 - r] = day + TableValueSeparator + ++match1;
//            }

//            // follow up days
//            for (int d = ++day; d < l; ++d)
//            {
//                int startRow = l - d;
//                int r = startRow;
//                int prev_r = r;
//                int c = 0;
//                int prev_c = c;
//                int match = 0;
//                Team[] remaining = teams.ToArray();
//                // take the diagonal bottom up
//                while (!(r == c || (prev_r == c && prev_c == r)))
//                {

//                    table[r, c] = d + "." + ++match;
//                    remaining[r] = null;
//                    remaining[c] = null;

//                    prev_r = r--;
//                    prev_c = c++;
//                }

//                // cross reference
//                // reducing from the "top" to the bottom until a match is found
//                var crItems = new List<CrossReferenceItem>(l);
//                crossReference(l, table, d, match, remaining, crItems);

//                int stopper = l;
//                while (!(arrayContainsNullOnly(remaining) || stopper <= 1))
//                {
//                    crossReference(l, table, d, match + crItems.Count, remaining, crItems);
//                    stopper /= 2;
//                }

//                if (!arrayContainsNullOnly(remaining))
//                {
//                    throw new SystemException("Could not cross reference remaining teams: " + remaining);
//                }

//            }

//            // printTable(table);
//            return table;
//        }

//        private static bool arrayContainsNullOnly(Object[] arr)
//        {
//            foreach (var o in arr)
//            {
//                if (o != null)
//                    return false;
//            }
//            return true;
//        }

//        private static void crossReference(int l, String[,] table, int d, int match, Team[] remaining,
//                List<CrossReferenceItem> crItems)
//        {
//            int top = l - 1, bottom = 0;
//            int skipper = 0;
//            int crMatch = match;
//            Team[] crRemaining = new Team[remaining.Length];
//            for (int i = 0; i < remaining.Length; ++i)
//            {
//                crRemaining[i] = remaining[i];
//            }
//            while (top > bottom)
//            {
//                // @formatter:off
//                while (top >= 0 && crRemaining[top] == null) { --top; };
//                while (bottom <= l - 1 && crRemaining[bottom] == null) { ++bottom; };
//                // @formatter:on

//                if (top <= bottom)
//                {
//                    top = l - 1;
//                    ++bottom;
//                }
//                else if (string.IsNullOrEmpty(table[top, bottom]))
//                {
//                    CrossReferenceItem crItem = new CrossReferenceItem(top--, bottom++, d + "." + ++crMatch);
//                    crItems.Add(crItem);
//                    crRemaining[crItem.row] = null;
//                    crRemaining[crItem.column] = null;
//                }
//                else
//                {
//                    // match taken.
//                    if (!crItems.Any())
//                    { // if the first cross reference doesn't work, only reduce top and continue
//                        --top;
//                    }
//                    else
//                    { // else start over crossing while skipping the starting pairs which didn't work
//                        ++skipper;
//                        top = l - 1;
//                        bottom = 0;
//                        crMatch = match;
//                        for (int i = 0; i <= skipper; ++i)
//                        {
//                            // @formatter:off
//                            while (crRemaining[top] == null) { --top; };
//                            // @formatter:on
//                        }
//                        crItems.Clear();
//                        crRemaining = new Team[remaining.Length];
//                        for (int i = 0; i < remaining.Length; ++i)
//                        {
//                            crRemaining[i] = remaining[i];
//                        }
//                    }
//                }
//            }

//            for (int i = 0; i < l; ++i)
//            {
//                remaining[i] = crRemaining[i];
//            }

//            foreach (var cri in crItems)
//            {
//                table[cri.row, cri.column] = cri.value;
//                remaining[cri.row] = null;
//                remaining[cri.column] = null;
//            }
//        }

//        private class CrossReferenceItem
//        {
//            internal int row, column;
//            internal String value;

//            internal CrossReferenceItem(int row, int column, String value)
//            {
//                this.row = row;
//                this.column = column;
//                this.value = value;
//            }

//        }

//        // TODO remove or make util
//        private static void printTable(Object[,] table)
//        {
//            int l = table.Length;
//            for (int i = 0; i < l; ++i)
//            {
//                Console.Write("|");
//                for (int j = 0; j < l; ++j)
//                {
//                    Object v = table[i, j];
//                    Console.Write(v + "|");
//                }
//                Console.WriteLine();
//                for (int x = 0; x < l; ++x)
//                {
//                    Console.Write("-");
//                }
//                Console.WriteLine();
//            }
//        }





//    }
//}
