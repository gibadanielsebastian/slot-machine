using System;
using System.Collections.Generic;
using System.Linq;

namespace LuckySpin
{
    public class WinningLine
    {
        public int LineIndex { get; set; }
        public SymbolType SymbolType { get; set; }
        public int MatchCount { get; set; }
        public int WinAmount { get; set; }
    }

    public class SpinResult
    {
        public int TotalWin { get; set; }
        public List<WinningLine> WinningLines { get; set; } = new List<WinningLine>();
    }

    public class SlotMachineEngine
    {
        private static readonly List<int[]> _payLines =
        new List<int[]>
        {
            new[] { 1, 1, 1, 1, 1 }, // Line 1: Middle
            new[] { 0, 0, 0, 0, 0 }, // Line 2: Top
            new[] { 2, 2, 2, 2, 2 }, // Line 3: Bottom
            new[] { 0, 1, 2, 1, 0 }, // Line 4: V-shape (top-middle-bottom-middle-top)
            new[] { 2, 1, 0, 1, 2 }, // Line 5: A-shape (bottom-middle-top-middle-bottom)
            new[] { 0, 0, 1, 2, 2 }, // Line 6
            new[] { 2, 2, 1, 0, 0 }, // Line 7
            new[] { 1, 0, 0, 0, 1 }, // Line 8
            new[] { 1, 2, 2, 2, 1 }, // Line 9
            new[] { 0, 1, 0, 1, 0 }  // Line 10
        };

        public static int[] GetPayLinePath(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _payLines.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineIndex), "Invalid payline index.");
            }
            return _payLines[lineIndex];
        }

        public int Credits { get; private set; }
        public int CurrentBet { get; private set; }
        public List<Reel> Reels { get; private set; }

        public event Action<int> OnCreditsChanged = delegate { };
        public event Action<string> OnGameMessage = delegate { };
        public event Action<string> OnSoundRequest = delegate { };

        public SlotMachineEngine(int startingCredits)
        {
            Credits = startingCredits;
            CurrentBet = 20; 
            Reels = new List<Reel>();
            for(int i=0; i<5; i++) Reels.Add(new Reel());
        }

        public void ChangeBet(int delta)
        {
            if (CurrentBet + delta >= 10 && CurrentBet + delta <= Credits)
            {
                CurrentBet += delta;
                OnGameMessage.Invoke($"Bet: {CurrentBet}");
                OnSoundRequest.Invoke("click");
            }
        }

        public bool CanSpin() => Credits >= CurrentBet;

        public SpinResult CalculateWin(Symbol?[,] screenGrid)
        {
            Credits -= CurrentBet;
            OnCreditsChanged.Invoke(Credits);

            var result = new SpinResult();
            if (screenGrid == null) return result; // Should not happen with new logic

            for (int lineIndex = 0; lineIndex < _payLines.Count; lineIndex++)
            {
                var line = _payLines[lineIndex];
                
                // Ensure first symbol is not null and exists
                if (screenGrid[0, line[0]] == null) continue;
                SymbolType firstSymbolType = screenGrid[0, line[0]]!.Type;
                
                int matchCount = 1;

                for (int reelIndex = 1; reelIndex < 5; reelIndex++)
                {
                    if (screenGrid[reelIndex, line[reelIndex]] == null) break;
                    
                    if (screenGrid[reelIndex, line[reelIndex]]!.Type == firstSymbolType)
                    {
                        matchCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (matchCount >= 3)
                {
                    Symbol? firstSymbolOnLine = screenGrid[0, line[0]];
                    if (firstSymbolOnLine == null) continue; // Should not happen

                    int payoutMultiplier = firstSymbolOnLine.Payouts.ContainsKey(matchCount) ? firstSymbolOnLine.Payouts[matchCount] : 0;
                    int winAmount = (CurrentBet / 10) * payoutMultiplier;
                    
                    if (winAmount < 1) winAmount = 1;

                    var winningLine = new WinningLine
                    {
                        LineIndex = lineIndex,
                        SymbolType = firstSymbolType,
                        MatchCount = matchCount,
                        WinAmount = winAmount
                    };
                    result.WinningLines.Add(winningLine);
                    result.TotalWin += winAmount;
                }
            }

            if (result.TotalWin > 0)
            {
                Credits += result.TotalWin;
                OnCreditsChanged.Invoke(Credits);

                bool isJackpot = result.WinningLines.Any(l => l.MatchCount == 5);
                OnSoundRequest.Invoke(isJackpot ? "jackpot" : "win");
                OnGameMessage.Invoke($"WIN! {result.TotalWin} Credits on {result.WinningLines.Count} lines.");
            }
            else
            {
                OnGameMessage.Invoke("No luck this time.");
            }

            return result;
        }
    }
}