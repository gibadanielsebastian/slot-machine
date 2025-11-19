using System;
using System.Collections.Generic;
using System.Drawing;

namespace LuckySpin
{
    public enum SymbolType { Cherry, Lemon, Grape, Bell, Horseshoe, Seven, Diamond }

    public class Symbol
    {
        private static readonly Dictionary<SymbolType, Dictionary<int, int>> _payouts = new Dictionary<SymbolType, Dictionary<int, int>>
        {
            { SymbolType.Seven, new Dictionary<int, int> { { 3, 20 }, { 4, 50 }, { 5, 200 } } },
            { SymbolType.Horseshoe, new Dictionary<int, int> { { 3, 15 }, { 4, 40 }, { 5, 150 } } },
            { SymbolType.Diamond, new Dictionary<int, int> { { 3, 10 }, { 4, 30 }, { 5, 100 } } },
            { SymbolType.Bell, new Dictionary<int, int> { { 3, 8 }, { 4, 20 }, { 5, 80 } } },
            { SymbolType.Grape, new Dictionary<int, int> { { 3, 5 }, { 4, 15 }, { 5, 50 } } },
            { SymbolType.Lemon, new Dictionary<int, int> { { 3, 3 }, { 4, 10 }, { 5, 30 } } },
            { SymbolType.Cherry, new Dictionary<int, int> { { 3, 2 }, { 4, 5 }, { 5, 20 } } }
        };

        public SymbolType Type { get; }
        public string Name { get; }
        public Dictionary<int, int> Payouts { get; }
        public Image? Sprite { get; set; }

        public Symbol(SymbolType type)
        {
            Type = type;
            Name = type.ToString();
            Payouts = _payouts[type];
            if (ResourceManager.Images.ContainsKey(type))
            {
                Sprite = ResourceManager.Images[type];
            }
        }
    }

    public class Reel
    {
        private readonly List<Symbol> _strip;
        private readonly Random _random;
        
        public List<Symbol> Strip => _strip;
        public int CurrentIndex { get; private set; }

        public Reel()
        {
            _random = new Random();
            _strip = new List<Symbol>
            {
                new Symbol(SymbolType.Cherry),
                new Symbol(SymbolType.Lemon),
                new Symbol(SymbolType.Grape),
                new Symbol(SymbolType.Cherry),
                new Symbol(SymbolType.Lemon),
                new Symbol(SymbolType.Cherry),
                new Symbol(SymbolType.Bell),
                new Symbol(SymbolType.Grape),
                new Symbol(SymbolType.Lemon),
                new Symbol(SymbolType.Cherry),
                new Symbol(SymbolType.Bell),
                new Symbol(SymbolType.Diamond),
                new Symbol(SymbolType.Horseshoe),
                new Symbol(SymbolType.Grape),
                new Symbol(SymbolType.Lemon),
                new Symbol(SymbolType.Seven),
                new Symbol(SymbolType.Diamond),
                new Symbol(SymbolType.Horseshoe),
                new Symbol(SymbolType.Bell),
                new Symbol(SymbolType.Cherry),
                new Symbol(SymbolType.Lemon),
                new Symbol(SymbolType.Grape)
            };
            CurrentIndex = _random.Next(_strip.Count);
        }

        public int SpinAndGetTargetIndex()
        {
            int move = _random.Next(10, 30);
            CurrentIndex = (CurrentIndex + move) % _strip.Count;
            return CurrentIndex;
        }

        // New method to get the 3 symbols visible on screen for a given top index
        public Symbol[] GetSymbolsAtScreenPosition(int topVisualIndex)
        {
            Symbol[] visible = new Symbol[3];
            visible[0] = _strip[topVisualIndex % _strip.Count]; // Top
            visible[1] = _strip[(topVisualIndex + 1) % _strip.Count]; // Middle
            visible[2] = _strip[(topVisualIndex + 2) % _strip.Count]; // Bottom
            return visible;
        }
    }
}