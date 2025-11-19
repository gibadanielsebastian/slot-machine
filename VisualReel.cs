using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace LuckySpin
{
        public class VisualReel : Panel
        {
            private List<Symbol> _renderStrip;
            private float _currentPixelOffset;
            private float _targetPixelOffset;
            private float _currentSpeed;
            private float _symbolHeight = 100f; 
            
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            [Browsable(false)]
            public bool IsSpinning { get; private set; }
            
            public event EventHandler SpinComplete = delegate { };
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int TopVisualIndex { get; private set; }
    
            private const float BASE_SPEED = 2500f; 
            private const float BASE_ACCEL = 4000f;
            private Reel? _logicalReel; // Reference to the logical reel

            public VisualReel()
            {
                this.DoubleBuffered = true;
                this.BackColor = Color.Black;
                this.BorderStyle = BorderStyle.None;
                this.Margin = new Padding(0); 
                _renderStrip = new List<Symbol>();
            }
            
            public void SetLogicalReel(Reel reel) {
                _logicalReel = reel;
            }

            public void SetStaticView(int currentMiddleIndex)
            {
                if (_logicalReel == null) return;
                CalculateDimensions();
                
                int topVisualIndex = (currentMiddleIndex - 1 + _logicalReel.Strip.Count) % _logicalReel.Strip.Count;
                Symbol[] visibleSymbols = _logicalReel.GetSymbolsAtScreenPosition(topVisualIndex);

                _renderStrip.Clear();
                _renderStrip.AddRange(visibleSymbols);
                _currentPixelOffset = 0; // The first symbol in renderStrip is at the top
                TopVisualIndex = topVisualIndex;
                Invalidate();
            }
    
            public void StartSpin(int targetMiddleIndex, int durationFactor)
            {
                if (IsSpinning || _logicalReel == null) return;
                CalculateDimensions();

                int targetTopIndex = (targetMiddleIndex - 1 + _logicalReel.Strip.Count) % _logicalReel.Strip.Count;

                // 1. Get current visible symbols (those currently drawn on screen)
                var currentVisualStrip = GetVisibleSymbols(3); // Get 3 visible symbols
                
                // 2. Generate random filler symbols (a few full rotations)
                int randomFillLength = 30 + (durationFactor * 5); // Ensure enough spins
                var randomFiller = new List<Symbol>();
                for (int i = 0; i < randomFillLength; i++) {
                    randomFiller.Add(_logicalReel.Strip[(_logicalReel.CurrentIndex + i) % _logicalReel.Strip.Count]);
                }
                
                // 3. Prepare the final stopping sequence (top, middle, bottom)
                Symbol[] finalSymbols = _logicalReel.GetSymbolsAtScreenPosition(targetTopIndex);

                // Construct the full render strip for animation
                _renderStrip.Clear();
                _renderStrip.AddRange(currentVisualStrip); // Start from where it currently is
                _renderStrip.AddRange(randomFiller);      // Add random symbols to simulate spinning
                _renderStrip.AddRange(finalSymbols);      // Add the final symbols

                _currentPixelOffset = 0; // Start rendering from the beginning of this new strip
                
                // Calculate the pixel offset where the targetTopIndex will be at the visual top of the display
                _targetPixelOffset = (currentVisualStrip.Count + randomFiller.Count) * _symbolHeight;

                _currentSpeed = 0;
                IsSpinning = true;
            }
    
            public void UpdateFrame(float dt)
            {
                if (!IsSpinning) return;
                
                float scaleFactor = _symbolHeight / 100f; 
                float maxSpeed = BASE_SPEED * scaleFactor;
                float accel = BASE_ACCEL * scaleFactor;
    
                float distanceRemaining = _targetPixelOffset - _currentPixelOffset;
                float brakingDistance = _symbolHeight * 8;
    
                if (distanceRemaining > brakingDistance)
                {
                    _currentSpeed += accel * dt;
                    if (_currentSpeed > maxSpeed) _currentSpeed = maxSpeed;
                }
                else
                {
                    float brakeProgress = distanceRemaining / brakingDistance;
                    _currentSpeed = maxSpeed * brakeProgress;
                    
                    if (_currentSpeed < 150f * scaleFactor) _currentSpeed = 150f * scaleFactor;
    
                    if (distanceRemaining < 1f)
                    {
                        FinishSpin();
                        return;
                    }
                }
    
                _currentPixelOffset += _currentSpeed * dt;
                Invalidate();
            }
    
            private void FinishSpin()
            {
                _currentPixelOffset = _targetPixelOffset;
                // Calculate TopVisualIndex from the end of _renderStrip, where finalSymbols start
                // The logical Reel's CurrentIndex (middle) maps to the middle symbol of finalSymbols
                // So the top symbol of finalSymbols is (CurrentIndex - 1)
                TopVisualIndex = (_logicalReel!.CurrentIndex - 1 + _logicalReel.Strip.Count) % _logicalReel.Strip.Count;
                IsSpinning = false;
                SpinComplete?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
    
            protected override void OnResize(EventArgs eventargs)
            {
                base.OnResize(eventargs);
                
                float oldHeight = _symbolHeight;
                CalculateDimensions();
                float newHeight = _symbolHeight;
    
                if (oldHeight > 0 && newHeight > 0)
                {
                    float ratio = newHeight / oldHeight;
                    _currentPixelOffset *= ratio;
                    
                    if (IsSpinning)
                    {
                        _targetPixelOffset *= ratio;
                        _currentSpeed *= ratio;
                    }
                }
                Invalidate();
            }
    
            private List<Symbol> GetVisibleSymbols(int count)
            {
                var list = new List<Symbol>();
                if (_renderStrip.Count == 0 || _logicalReel == null) return list;
    
                int firstIndex = (int)Math.Floor(_currentPixelOffset / _symbolHeight);
                
                for(int i=0; i<count; i++)
                {
                    int idx = firstIndex + i;
                    if (idx < _renderStrip.Count && idx >= 0) 
                        list.Add(_renderStrip[idx]);
                    else 
                        list.Add(new Symbol(SymbolType.Cherry)); // Fallback if out of bounds
                }
                return list;
            }
    
            private void CalculateDimensions()
            {
                if (this.Height > 0)
                    _symbolHeight = (float)this.Height / 3f;
                else
                    _symbolHeight = 100f;
            }
    
            protected override void OnPaint(PaintEventArgs e)
            {
                if (this.Height <= 0) return;
                if (_symbolHeight != this.Height / 3f) CalculateDimensions();
    
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
    
                if (_renderStrip == null || _renderStrip.Count == 0) return;
    
                int firstIndex = (int)Math.Floor(_currentPixelOffset / _symbolHeight);
                float fineOffset = _currentPixelOffset % _symbolHeight;
    
                float imgSize = Math.Min(this.Width, _symbolHeight) - 10;
                float xPos = (this.Width - imgSize) / 2;
    
                for (int i = 0; i < 5; i++)
                {
                    int idx = firstIndex + i;
                    if (idx >= 0 && idx < _renderStrip.Count)
                    {
                        var sym = _renderStrip[idx];
                        Image? img = sym.Sprite;
    
                        if (img != null)
                        {
                            float yPos = (i * _symbolHeight) - fineOffset;
                            RectangleF destRect = new RectangleF(xPos, yPos + 5, imgSize, imgSize);
                            
                            g.DrawImage(
                                img, 
                                destRect, 
                                new RectangleF(0, 0, img.Width, img.Height), 
                                GraphicsUnit.Pixel
                            );
                        }
                    }
                }
            }
        }
    }