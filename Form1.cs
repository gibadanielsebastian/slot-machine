using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Media;
using System.IO;

namespace LuckySpin

{
    public partial class Form1 : Form
    {
        private SlotMachineEngine? _game;
        private List<VisualReel>? _visualReels;
        private System.Windows.Forms.Timer? _gameLoopTimer;
        private Stopwatch? _stopwatch;
        private Label? _lblCredits;
        private Label? _lblStatus;
        private Label? _lblBet;
        private Button? _btnSpin;
        private SoundPlayer? _audioPlayer;
        private Panel? _footerPanel;
        private Button? _btnUp;
        private Button? _btnDown;
        private int _reelsStoppedCount = 0;

        // --- New Screen Grid and Win Message Cycling ---
        private Symbol?[,] _screenGrid = new Symbol?[5, 3]; // [reelIndex, rowIndex (0=Top, 1=Middle, 2=Bottom)]
        private List<WinningLine> _currentWinningLines = new List<WinningLine>();
        private System.Windows.Forms.Timer _winMessageTimer;
        private int _currentWinMessageIndex = -1;
        private int _totalWinAmount = 0;


        public Form1()
        {
            InitializeComponent();
            ResourceManager.LoadResources();
            SetupResponsiveUI();
            
            _audioPlayer = new SoundPlayer();
            _stopwatch = new Stopwatch();
            _gameLoopTimer = new System.Windows.Forms.Timer();
            _gameLoopTimer.Interval = 16;
            _gameLoopTimer.Tick += GameLoop_Tick;

            _winMessageTimer = new System.Windows.Forms.Timer();
            _winMessageTimer.Interval = 750; // Display each message for 0.75 seconds
            _winMessageTimer.Tick += WinMessageTimer_Tick;
        }

        private void WinMessageTimer_Tick(object? sender, EventArgs e)
        {
            if (_lblStatus == null || _game == null) return;

            if (_currentWinningLines.Count == 0)
            {
                _winMessageTimer.Stop();
                _lblStatus.Text = "No luck this time.";
                return;
            }

            _currentWinMessageIndex++;
            // Cycle through individual winning lines, then show total win, then repeat
            if (_currentWinMessageIndex > _currentWinningLines.Count)
            {
                _currentWinMessageIndex = 0; // Loop back to the first winning line
            }

            if (_currentWinMessageIndex == _currentWinningLines.Count) // Display total win message once after cycling all lines
            {
                _lblStatus.Text = $"WIN! {_totalWinAmount} Credits on {_currentWinningLines.Count} lines.";
            }
            else
            {
                // Show individual winning line message
                var line = _currentWinningLines[_currentWinMessageIndex];
                _lblStatus.Text = $"WIN! {line.WinAmount} Credits on line {line.LineIndex + 1} ({line.MatchCount}x {line.SymbolType})";
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.PerformLayout();
            InitializeGame();
            PositionFooterControls();
        }

        private void InitializeGame()
        {
            _game = new SlotMachineEngine(2000);
            _game.OnCreditsChanged += (credits) => { if (_lblCredits != null) _lblCredits.Text = $"CREDITS: {credits}"; };
            _game.OnGameMessage += (msg) => { if (_lblStatus != null) _lblStatus.Text = msg; };
            _game.OnSoundRequest += PlaySoundEffect;

            if (_visualReels != null && _game != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    _visualReels[i].SetLogicalReel(_game.Reels[i]);
                    // Populate _screenGrid and set static view using GetSymbolsAtScreenPosition
                    // _game.Reels[i].CurrentIndex is the middle index
                    Symbol[] initialSymbols = _game.Reels[i].GetSymbolsAtScreenPosition(_game.Reels[i].CurrentIndex);
                    _visualReels[i].SetStaticView(_game.Reels[i].CurrentIndex);
                    
                    _screenGrid[i, 0] = initialSymbols[0]; // Top
                    _screenGrid[i, 1] = initialSymbols[1]; // Middle
                    _screenGrid[i, 2] = initialSymbols[2]; // Bottom
                }
            }
            UpdateUI();
        }

        private void GameLoop_Tick(object? sender, EventArgs e)
        {
            if (_stopwatch == null || _visualReels == null) return;
            
            float dt = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();
            if (dt > 0.1f) dt = 0.1f;

            foreach(var reel in _visualReels)
            {
                reel.UpdateFrame(dt);
            }
        }

        private void StartSpin()
        {
            if (_game == null || !_game.CanSpin() || _btnSpin == null || _stopwatch == null || _gameLoopTimer == null || _visualReels == null) return;

            _winMessageTimer.Stop();
            _currentWinningLines.Clear();
            _currentWinMessageIndex = -1;
            _totalWinAmount = 0;
            if (_lblStatus != null) _lblStatus.Text = "PRESS SPIN"; // Clear previous messages

            _btnSpin.Enabled = false;
            _reelsStoppedCount = 0;
            PlaySoundEffect("spin");
            
            _stopwatch.Restart();
            _gameLoopTimer.Start();

            for (int i = 0; i < 5; i++)
            {
                _game.Reels[i].SpinAndGetTargetIndex();
                // Pass CurrentIndex directly as the middle target, VisualReel calculates its own top target
                _visualReels[i].StartSpin(_game.Reels[i].CurrentIndex, i + 1); 
            }
        }
        
        private void Reel_SpinComplete(object? sender, EventArgs e)
        {
            _reelsStoppedCount++;
            PlaySoundEffect("stop");

            if (_reelsStoppedCount == 5)
            {
                _gameLoopTimer?.Stop();
                
                // --- Populate _screenGrid after all reels stop ---
                if (_game != null && _visualReels != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Symbol[] visibleSymbols = _game.Reels[i].GetSymbolsAtScreenPosition(_visualReels[i].TopVisualIndex);
                        _screenGrid[i, 0] = visibleSymbols[0]; // Top
                        _screenGrid[i, 1] = visibleSymbols[1]; // Middle
                        _screenGrid[i, 2] = visibleSymbols[2]; // Bottom
                    }
                }
                // --- Call CalculateWin with the new _screenGrid ---
                var result = _game?.CalculateWin(_screenGrid);
                _totalWinAmount = result?.TotalWin ?? 0;

                if (result != null && result.TotalWin > 0)
                {
                    _currentWinningLines = result.WinningLines;
                    _currentWinMessageIndex = -1; // Start with overall win message
                    _winMessageTimer.Start();
                } else {
                    if (_lblStatus != null) _lblStatus.Text = "No luck this time.";
                }

                if (_btnSpin != null) _btnSpin.Enabled = true;
            }
        }
        
        private void UpdateUI()
        {
            if (_game == null) return;
            if (_lblBet != null) _lblBet.Text = $"BET: {_game.CurrentBet}";
            if (_lblCredits != null) _lblCredits.Text = $"CREDITS: {_game.Credits}";
        }
        
        private void PlaySoundEffect(string name)
        {
            if (_audioPlayer == null) return;
            string path = Path.Combine(Application.StartupPath, "Sounds", $"{name}.wav");
            if (File.Exists(path)) { _audioPlayer.SoundLocation = path; _audioPlayer.Play(); }
        }

        private void SetupResponsiveUI()
        {
            this.Text = "LuckySpin Casino";
            this.Size = new Size(1024, 768);
            this.MinimumSize = new Size(800, 600);
            this.BackColor = Color.FromArgb(25, 25, 30);
            this.WindowState = FormWindowState.Maximized;
            
            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 1;
            mainLayout.RowCount = 3;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 73F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 15F));
            this.Controls.Add(mainLayout);

            Label title = new Label();
            title.Text = "VEGAS JACKPOT";
            title.Font = new Font("Impact", 36, FontStyle.Italic);
            title.ForeColor = Color.Gold;
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Dock = DockStyle.Fill;
            mainLayout.Controls.Add(title, 0, 0);

            Panel reelsOuterPanel = new Panel();
            reelsOuterPanel.Dock = DockStyle.Fill;
            reelsOuterPanel.BackColor = Color.FromArgb(40, 0, 0);
            reelsOuterPanel.Padding = new Padding(20);
            mainLayout.Controls.Add(reelsOuterPanel, 0, 1);

            TableLayoutPanel reelsLayout = new TableLayoutPanel();
            reelsLayout.Dock = DockStyle.Fill;
            reelsLayout.ColumnCount = 5;
            reelsLayout.RowCount = 1;
            reelsLayout.BackColor = Color.Black;
            for (int i = 0; i < 5; i++) reelsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            _visualReels = new List<VisualReel>();
            for (int i = 0; i < 5; i++)
            {
                VisualReel vr = new VisualReel();
                vr.Dock = DockStyle.Fill;
                vr.SpinComplete += Reel_SpinComplete;
                reelsLayout.Controls.Add(vr, i, 0);
                _visualReels.Add(vr);
            }
            reelsOuterPanel.Controls.Add(reelsLayout);
            
            _footerPanel = new Panel();
            _footerPanel.Dock = DockStyle.Fill;
            mainLayout.Controls.Add(_footerPanel, 0, 2);

            _lblCredits = CreateLabel(_footerPanel, "CREDITS: 2000", 18);
            _lblBet = CreateLabel(_footerPanel, "BET: 20", 18);
            _lblStatus = CreateLabel(_footerPanel, "PRESS SPIN", 14);
            _lblStatus.AutoSize = false;
            _lblStatus.Dock = DockStyle.Bottom;
            _lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            _btnSpin = new Button();
            _btnSpin.Text = "SPIN";
            _btnSpin.BackColor = Color.Crimson;
            _btnSpin.ForeColor = Color.White;
            _btnSpin.Font = new Font("Arial Black", 24);
            _btnSpin.Size = new Size(200, 70);
            _footerPanel.Controls.Add(_btnSpin);
            _btnSpin.Click += (sender, e) => StartSpin();
            
            _btnUp = CreateButton(_footerPanel, "+", (s, e) => { _game?.ChangeBet(10); UpdateUI(); });
            _btnDown = CreateButton(_footerPanel, "-", (s, e) => { _game?.ChangeBet(-10); UpdateUI(); });

            _footerPanel.Resize += (s, e) => PositionFooterControls();
        }
        
        private void PositionFooterControls()
        {
            if (_footerPanel == null || _btnSpin == null || _btnUp == null || _btnDown == null || _lblBet == null || _lblCredits == null) return;
            
            _btnSpin.Left = (_footerPanel.Width - _btnSpin.Width) / 2;
            _btnSpin.Top = (_footerPanel.Height - _btnSpin.Height) / 2 - 10;
        
            _btnUp.Left = _btnSpin.Right + 20;
            _btnUp.Top = _btnSpin.Top + (_btnSpin.Height - _btnUp.Height) / 2;
        
            _btnDown.Left = _btnSpin.Left - _btnDown.Width - 20;
            _btnDown.Top = _btnSpin.Top + (_btnSpin.Height - _btnDown.Height) / 2;
        
            _lblBet.Location = new Point(_footerPanel.Width - _lblBet.Width - 50, 30);
            _lblCredits.Location = new Point(50, 30);
        }
        
        private Label CreateLabel(Control parent, string text, float size)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Segoe UI", size, FontStyle.Bold);
            l.ForeColor = Color.White;
            l.AutoSize = true;
            parent.Controls.Add(l);
            return l;
        }
        
        private Button CreateButton(Control parent, string text, EventHandler ev)
        {
            Button b = new Button();
            b.Text = text;
            b.Size = new Size(40, 40);
            b.BackColor = Color.FromArgb(64, 64, 64);
            b.ForeColor = Color.White;
            b.Font = new Font("Arial", 16, FontStyle.Bold);
            b.Click += ev;
            parent.Controls.Add(b);
            return b;
        }
    }
}