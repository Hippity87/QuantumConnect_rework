using QuantumConnect.Core.GameEngine;
using QuantumConnect.Core.Models;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace QuantumConnect.Core.Algorithms
{
    public class LexBot : IAlgorithm
    {
        private const int WIDTH = 12;
        private const int HEIGHT = 8;
        private const int HALF = WIDTH / 2;
        private const int WIN_LENGTH = 6;
        private const int BOMB_COOLDOWN = 10;
        private const int EARLY_LIMIT = 30;

        private const int THRESHOLD_EARLY = 12;
        private const int THRESHOLD_MID = 8;
        private const int THRESHOLD_LATE = 6;
        private const int BOMB_BONUS_PER_DISC = 2;

        private static readonly int[] OPENING_COLS = { 4, 5, 6, 7, 8, 9 };
        private readonly Random rand = new();

        private static readonly List<long[]> LINE_MASKS = new();
        private static readonly List<int[][]> LINE_COORDS = new();

        static LexBot()
        {
            // Horizontal
            for (int r = 0; r < HEIGHT; r++)
                for (int c0 = 0; c0 <= WIDTH - WIN_LENGTH; c0++)
                    AddLine(r, c0, 0, 1);

            // Vertical
            for (int c = 0; c < WIDTH; c++)
                for (int r0 = 0; r0 <= HEIGHT - WIN_LENGTH; r0++)
                    AddLine(r0, c, 1, 0);

            // Diagonal (\\)
            for (int r0 = 0; r0 <= HEIGHT - WIN_LENGTH; r0++)
                for (int c0 = 0; c0 <= WIDTH - WIN_LENGTH; c0++)
                    AddLine(r0, c0, 1, 1);

            // Diagonal (/)
            for (int r0 = WIN_LENGTH - 1; r0 < HEIGHT; r0++)
                for (int c0 = 0; c0 <= WIDTH - WIN_LENGTH; c0++)
                    AddLine(r0, c0, -1, 1);
        }

        private static void AddLine(int r0, int c0, int dr, int dc)
        {
            long m0 = 0, m1 = 0;
            int[][] coords = new int[WIN_LENGTH][];
            for (int i = 0; i < WIN_LENGTH; i++)
            {
                int r = r0 + dr * i, c = c0 + dc * i;
                coords[i] = new int[] { r, c };
                int part = (c < HALF ? 0 : 1), cc = c % HALF;
                long bit = 1L << (cc * HEIGHT + r);
                if (part == 0) m0 |= bit; else m1 |= bit;
            }
            LINE_MASKS.Add(new long[] { m0, m1 });
            LINE_COORDS.Add(coords);
        }

        // State
        private long myBoard1, myBoard2, oppBoard1, oppBoard2;
        private int movesSinceBomb = BOMB_COOLDOWN;
        private bool bombThisTurn;
        private int turnCount = 0;
        private int startCol = WIDTH / 2;

        public int AccelerateQuark(QuantumField qf)
        {
            // Detect new match
            bool brandNew = true;
            for (int r = 0; r < HEIGHT && brandNew; r++)
                for (int c = 0; c < WIDTH; c++)
                    if (!qf.IsSpaceEmpty(r, c)) { brandNew = false; break; }

            if (brandNew)
            {
                turnCount = 0;
                movesSinceBomb = BOMB_COOLDOWN;
                startCol = OPENING_COLS[rand.Next(OPENING_COLS.Length)];
            }

            turnCount++;
            UpdateBitboards(qf);
            bombThisTurn = false;
            movesSinceBomb++;

            int[] lowest = ComputeLowest(qf);

            // Early phase blocking logic
            if (turnCount <= EARLY_LIMIT)
            {
                // 1) block any imminent 5-in-a-row anywhere
                for (int i = 0; i < LINE_MASKS.Count; i++)
                {
                    var mask = LINE_MASKS[i];
                    int oc = BitCount(oppBoard1 & mask[0]) + BitCount(oppBoard2 & mask[1]);
                    int mc = BitCount(myBoard1 & mask[0]) + BitCount(myBoard2 & mask[1]);
                    if (oc == WIN_LENGTH - 1 && mc == 0)
                    {
                        foreach (var rc in LINE_COORDS[i])
                        {
                            int rr = rc[0], cc = rc[1];
                            long b = 1L << ((cc % HALF) * HEIGHT + rr);
                            if (((oppBoard1 | oppBoard2 | myBoard1 | myBoard2) & b) == 0L && lowest[cc] == rr)
                                return cc;
                        }
                    }
                }
                // 2) preempt bottom-row 5-in-a-row
                for (int c0 = 0; c0 <= WIDTH - WIN_LENGTH; c0++)
                {
                    int oppCnt = 0, emp = -1;
                    for (int i = 0; i < WIN_LENGTH; i++)
                    {
                        int c = c0 + i;
                        if (!qf.IsSpaceEmpty(HEIGHT - 1, c) && !qf.BelongsToMe(HEIGHT - 1, c)) oppCnt++;
                        else if (qf.IsSpaceEmpty(HEIGHT - 1, c)) emp = c;
                    }
                    if (oppCnt == WIN_LENGTH - 1 && emp >= 0 && lowest[emp] == HEIGHT - 1) return emp;
                }
                // 3) preempt second-row 5-in-a-row
                for (int c0 = 0; c0 <= WIDTH - WIN_LENGTH; c0++)
                {
                    int oppCnt = 0, emp = -1;
                    for (int i = 0; i < WIN_LENGTH; i++)
                    {
                        int c = c0 + i;
                        if (!qf.IsSpaceEmpty(HEIGHT - 2, c) && !qf.BelongsToMe(HEIGHT - 2, c)) oppCnt++;
                        else if (qf.IsSpaceEmpty(HEIGHT - 2, c)) emp = c;
                    }
                    if (oppCnt == WIN_LENGTH - 1 && emp >= 0 && lowest[emp] == HEIGHT - 2) return emp;
                }
            }

            // Opening move
            if (turnCount == 1 && lowest[startCol] >= 0)
                return startCol;

            // Immediate win
            for (int c = 0; c < WIDTH; c++)
            {
                int r = lowest[c];
                if (r >= 0 && MakesWinningMove(myBoard1, myBoard2, c, r))
                    return c;
            }

            // Immediate block (bomb if profitable)
            var threats = new List<int>();
            for (int c = 0; c < WIDTH; c++)
            {
                int r = lowest[c];
                if (r >= 0 && MakesWinningMove(oppBoard1, oppBoard2, c, r))
                    threats.Add(c);
            }
            if (threats.Count > 0)
            {
                int fallback = threats[0];
                if (movesSinceBomb >= BOMB_COOLDOWN)
                {
                    foreach (var c in threats)
                    {
                        int r = lowest[c];
                        if (IsBombProfitable(c, r, lowest))
                        {
                            bombThisTurn = true;
                            movesSinceBomb = 0;
                            return c;
                        }
                    }
                }
                return fallback;
            }

            // Other 5-in-a-row threats
            for (int i = 0; i < LINE_MASKS.Count; i++)
            {
                var mask = LINE_MASKS[i];
                int oc = BitCount(oppBoard1 & mask[0]) + BitCount(oppBoard2 & mask[1]);
                int mc = BitCount(myBoard1 & mask[0]) + BitCount(myBoard2 & mask[1]);
                if (oc >= WIN_LENGTH - 1 && mc == 0)
                {
                    foreach (var rc in LINE_COORDS[i])
                    {
                        if (lowest[rc[1]] == rc[0])
                            return rc[1];
                    }
                }
            }

            // Safe-move filtering
            var safe = new bool[WIDTH];
            int safeCount = 0;
            for (int c = 0; c < WIDTH; c++)
            {
                int r = lowest[c];
                if (r >= 0 && IsSafeMove(c, r, lowest))
                {
                    safe[c] = true; safeCount++;
                }
            }
            if (safeCount == 0)
            {
                if (movesSinceBomb >= BOMB_COOLDOWN)
                {
                    int fb = PickHeuristicMove(lowest);
                    bombThisTurn = true; movesSinceBomb = 0;
                    return fb;
                }
                else return PickHeuristicMove(lowest);
            }

            // α–β minimax among safe
            int bestCol = 0, alpha = int.MinValue;
            for (int c = 0; c < WIDTH; c++)
            {
                if (!safe[c]) continue;
                int r = lowest[c];
                long sb1 = myBoard1, sb2 = myBoard2;
                int part = (c < HALF ? 0 : 1), cc = c % HALF;
                long b = 1L << (cc * HEIGHT + r);
                if (part == 0) sb1 |= b; else sb2 |= b;

                int[] low2 = (int[])lowest.Clone(); low2[c] = (r > 0 ? r - 1 : -1);
                int worst = MinValue(sb1, sb2, oppBoard1, oppBoard2, low2, alpha);
                if (worst > alpha) { alpha = worst; bestCol = c; }
            }

            // Fallback block
            for (int c = 0; c < WIDTH; c++)
            {
                int r = lowest[c];
                if (r >= 0 && MakesWinningMove(oppBoard1, oppBoard2, c, r))
                    return c;
            }
            return bestCol;
        }

        public bool UseSpecialMove(QuantumField qf)
        {
            return bombThisTurn;
        }

        // Utility methods
        private static int BitCount(long x) => (int)BitOperations.PopCount((ulong)x);

        private int MinValue(long sb1, long sb2, long ob1, long ob2, int[] low, int alpha)
        {
            int value = int.MaxValue;
            for (int c = 0; c < WIDTH; c++)
            {
                int r = low[c];
                if (r < 0) continue;
                long ob1p = ob1, ob2p = ob2;
                int part = (c < HALF ? 0 : 1), cc = c % HALF;
                long b = 1L << (cc * HEIGHT + r);
                if (part == 0) ob1p |= b; else ob2p |= b;
                if (CheckWin(ob1p, ob2p)) return int.MinValue;
                int score = Evaluate(sb1, sb2, ob1p, ob2p);
                if (score < value) value = score;
                if (value <= alpha) break;
            }
            return value;
        }

        private bool IsBombProfitable(int col, int row, int[] baseLow)
        {
            int thresh = (turnCount < 20 ? THRESHOLD_EARLY : (turnCount < 35 ? THRESHOLD_MID : THRESHOLD_LATE));
            int[] lowNB = (int[])baseLow.Clone();
            long sb1 = myBoard1, sb2 = myBoard2, ob1 = oppBoard1, ob2 = oppBoard2;
            ApplyDrop(ref sb1, ref sb2, lowNB, col, row);
            int evalNB = SimulateOpponentReply(sb1, sb2, ob1, ob2, lowNB);
            int[] lowB = (int[])baseLow.Clone();
            long sb1b = myBoard1, sb2b = myBoard2, ob1b = oppBoard1, ob2b = oppBoard2;
            ApplyDrop(ref sb1b, ref sb2b, lowB, col, row);
            int removed = CountRemovedOpp(oppBoard1, oppBoard2, col, row);
            DestroyNeighbors(ref sb1b, ref sb2b, ref ob1b, ref ob2b, col, row);
            lowB[col] = row - 1;
            int evalB = SimulateOpponentReply(sb1b, sb2b, ob1b, ob2b, lowB);
            return (evalB - evalNB + removed * BOMB_BONUS_PER_DISC) >= thresh;
        }

        private int SimulateOpponentReply(long sb1, long sb2, long ob1, long ob2, int[] low)
        {
            int worst = int.MaxValue;
            for (int c = 0; c < WIDTH; c++)
            {
                int r = low[c];
                if (r < 0) continue;
                long ob1p = ob1, ob2p = ob2;
                int part = (c < HALF ? 0 : 1), cc = c % HALF;
                long b = 1L << (cc * HEIGHT + r);
                if (part == 0) ob1p |= b; else ob2p |= b;
                if (CheckWin(ob1p, ob2p)) return int.MinValue;
                int score = Evaluate(sb1, sb2, ob1p, ob2p);
                if (score < worst) worst = score;
            }
            return worst;
        }

        private int CountRemovedOpp(long ob1, long ob2, int c0, int r0)
        {
            int cnt = 0;
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int r = r0 + dr, c = c0 + dc;
                    if (r < 0 || r >= HEIGHT || c < 0 || c >= WIDTH) continue;
                    int part = (c < HALF ? 0 : 1), cc = c % HALF;
                    long bit = 1L << (cc * HEIGHT + r);
                    if (((part == 0 ? ob1 : ob2) & bit) != 0) cnt++;
                }
            return cnt;
        }

        private void ApplyDrop(ref long b1, ref long b2, int[] low, int c, int r)
        {
            int part = (c < HALF ? 0 : 1), cc = c % HALF;
            long bit = 1L << (cc * HEIGHT + r);
            if (part == 0) b1 |= bit; else b2 |= bit;
            low[c] = (r > 0 ? r - 1 : -1);
        }

        private void DestroyNeighbors(ref long sb1, ref long sb2, ref long ob1, ref long ob2, int c0, int r0)
        {
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int r = r0 + dr, c = c0 + dc;
                    if (r < 0 || r >= HEIGHT || c < 0 || c >= WIDTH) continue;
                    int part = (c < HALF ? 0 : 1), cc = c % HALF;
                    long bit = 1L << (cc * HEIGHT + r);
                    sb1 &= ~bit; sb2 &= ~bit; ob1 &= ~bit; ob2 &= ~bit;
                }
        }

        private void UpdateBitboards(QuantumField qf)
        {
            myBoard1 = myBoard2 = oppBoard1 = oppBoard2 = 0L;
            var F = qf.GetField();
            for (int c = 0; c < WIDTH; c++)
                for (int r = 0; r < HEIGHT; r++)
                {
                    if (F[r, c] == SpaceOwnership.Empty) continue;
                    int part = (c < HALF ? 0 : 1), cc = c % HALF;
                    long bit = 1L << (cc * HEIGHT + r);
                    if (qf.BelongsToMe(r, c))
                    { if (part == 0) myBoard1 |= bit; else myBoard2 |= bit; }
                    else
                    { if (part == 0) oppBoard1 |= bit; else oppBoard2 |= bit; }
                }
        }

        private int[] ComputeLowest(QuantumField qf)
        {
            int[] low = new int[WIDTH];
            for (int c = 0; c < WIDTH; c++)
            {
                low[c] = -1;
                for (int r = HEIGHT - 1; r >= 0; r--)
                    if (qf.IsSpaceEmpty(r, c)) { low[c] = r; break; }
            }
            return low;
        }

        private bool IsSafeMove(int c, int r, int[] low)
        {
            long sb1 = myBoard1, sb2 = myBoard2;
            int part = (c < HALF ? 0 : 1), cc = c % HALF;
            long bit = 1L << (cc * HEIGHT + r);
            if (part == 0) sb1 |= bit; else sb2 |= bit;
            int[] low2 = (int[])low.Clone(); low2[c] = (r > 0 ? r - 1 : -1);
            for (int c2 = 0; c2 < WIDTH; c2++)
            {
                int r2 = low2[c2];
                if (r2 < 0) continue;
                if (MakesWinningMove(oppBoard1, oppBoard2, c2, r2)) return false;
            }
            return true;
        }

        private bool MakesWinningMove(long b1, long b2, int c, int r)
        {
            long bb1 = b1, bb2 = b2;
            int part = (c < HALF ? 0 : 1), cc = c % HALF;
            long bit = 1L << (cc * HEIGHT + r);
            if (part == 0) bb1 |= bit; else bb2 |= bit;
            foreach (var m in LINE_MASKS)
                if ((bb1 & m[0]) == m[0] && (bb2 & m[1]) == m[1])
                    return true;
            return false;
        }

        private bool CheckWin(long b1, long b2)
        {
            foreach (var m in LINE_MASKS)
                if ((b1 & m[0]) == m[0] && (b2 & m[1]) == m[1])
                    return true;
            return false;
        }

        private int Evaluate(long sb1, long sb2, long ob1, long ob2)
        {
            int score = 0;
            foreach (var m in LINE_MASKS)
            {
                int sc = BitCount(sb1 & m[0]) + BitCount(sb2 & m[1]);
                int oc = BitCount(ob1 & m[0]) + BitCount(ob2 & m[1]);
                if (sc > 0 && oc == 0) score += sc * sc;
                else if (oc > 0 && sc == 0) score -= oc * oc * 2;
            }
            return score;
        }

        private int PickHeuristicMove(int[] low)
        {
            int best = -1;
            int bestScore = int.MinValue;
            for (int c = 0; c < WIDTH; c++)
            {
                int r = low[c];
                if (r < 0) continue;
                int centerDist = Math.Abs(c - WIDTH / 2);
                int heightScore = r;
                int score = (10 - centerDist) * 3 + heightScore;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }
    }
}
