using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Media;

namespace Lion_Game
{
    public partial class Form1 : Form
    {
        private PictureBox mouse;
        private List<PictureBox> lions = new();
        private List<PictureBox> cheeses = new();
        private List<PictureBox> obstacles = new();
        private List<PictureBox> bushes = new();
        private System.Windows.Forms.Timer lionTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer mouseTimer = new System.Windows.Forms.Timer();
        private HashSet<Keys> pressedKeys = new();
        private int cheeseCollected = 0;
        private int mouseHealth = 2;
        private static int highScore = 0;
        private Random rand = new();
        private const int MoveStep = 5;
        private const int LionStep = 2;
        private Label highScoreLabel;
        private Label healthLabel;
        private PictureBox healthIcon;
        private PictureBox heartDrop; // Heart drop on the map
        private PictureBox ammoDrop;
        private int ammoCount = 0;
        private PictureBox ammoIcon;
        private Label ammoCountLabel;
        private bool hasAmmo = false; // Not used anymore, replaced by ammoCount
        private System.Windows.Forms.Timer ammoDropTimer = new System.Windows.Forms.Timer();
        private PictureBox firedAmmo;
        private PictureBox stunnedLion;
        private DateTime lionStunEndTime;
        private System.Windows.Forms.Timer firedAmmoTimer = new System.Windows.Forms.Timer();
        private DateTime gameStartTime;
        private System.Windows.Forms.Timer bossTimer = new System.Windows.Forms.Timer();
        private PictureBox boss;
        private int bossHealth = 10;
        private Label bossHealthLabel;
        private System.Windows.Forms.Timer bossAmmoDropTimer = new System.Windows.Forms.Timer();
        private PictureBox bossAmmoDrop;
        private int bossAmmoCount = 0;
        private Label bossAmmoCountLabel;
        private PictureBox bossAmmoIcon;
        private PictureBox firedBossAmmo;
        private bool bossActive = false;
        private System.Windows.Forms.Timer bossMoveTimer = new System.Windows.Forms.Timer(); // Add this line
        private DateTime lastBossHitTime = DateTime.MinValue;
        private const int BossHitCooldownMs = 1000; // 1 second cooldown between hits
        private const int MaxMouseHealth = 5;
        private const int MaxBossHealth = 10;
        private SoundPlayer bossMusicPlayer;

        public Form1()
        {
            InitializeComponent();
            InitGame();
        }

        private void InitGame()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Green;
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;

            // High score label
            highScoreLabel = new Label
            {
                Text = $"High Score: {highScore}",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(10, 10)
            };
            this.Controls.Add(highScoreLabel);

            // Ammo icon
            ammoIcon = new PictureBox
            {
                Image = Properties.Resources.ammobox,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(30, 30),
                Location = new Point(10, 50),
                BackColor = Color.Transparent
            };
            this.Controls.Add(ammoIcon);

            // Ammo count label (to the right of the icon)
            ammoCountLabel = new Label
            {
                Text = $"x {ammoCount}",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.Yellow,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(ammoIcon.Right + 5, 55)
            };
            this.Controls.Add(ammoCountLabel);

            // Health icon
            healthIcon = new PictureBox
            {
                Image = Properties.Resources.healt,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(30, 30),
                Location = new Point(10, 90),
                BackColor = Color.Transparent
            };
            this.Controls.Add(healthIcon);

            // Health label (to the right of the icon)
            healthLabel = new Label
            {
                Text = $"Health: {mouseHealth}",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(healthIcon.Right + 5, 95)
            };
            this.Controls.Add(healthLabel);

            // Mouse - spawn at random location
            Point mouseSpawn = GetRandomLocation(30, 30);
            mouse = new PictureBox
            {
                Image = Properties.Resources.Mouse,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(30, 30),
                Location = mouseSpawn,
                BackColor = Color.Transparent
            };
            this.Controls.Add(mouse);

            // Place obstacles
            PlaceObstacles();

            // Lions
            lions.Clear();
            for (int i = 0; i < 3; i++)
            {
                var lion = new PictureBox
                {
                    Image = Properties.Resources.lion,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(40, 40),
                    Location = GetRandomLionLocation(40, 40),
                    BackColor = Color.Transparent
                };
                lions.Add(lion);
                this.Controls.Add(lion);
            }

            // Spawn initial cheese
            SpawnCheese();

            // Lion AI Timer
            lionTimer.Interval = 50;
            lionTimer.Tick += LionTimer_Tick;
            lionTimer.Start();

            // Mouse movement timer
            mouseTimer.Interval = 20;
            mouseTimer.Tick += MouseTimer_Tick;
            mouseTimer.Start();

            // Ammo drop timer
            ammoDropTimer.Interval = 10000; // 10 seconds
            ammoDropTimer.Tick += AmmoDropTimer_Tick;
            ammoDropTimer.Start();

            // Fired ammo timer
            firedAmmoTimer.Interval = 20;
            firedAmmoTimer.Tick += FiredAmmoTimer_Tick;

            // Mouse click event
            this.MouseDown += Form1_MouseDown;

            gameStartTime = DateTime.Now;
            bossTimer.Interval = 1000; // check every second
            bossTimer.Tick += BossTimer_Tick;
            bossTimer.Start();

            // Boss health label (top right)
            bossHealthLabel = new Label
            {
                Text = $"Boss Health: {bossHealth}",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.Magenta,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(this.ClientSize.Width - 220, 10)
            };
            this.Controls.Add(bossHealthLabel);
            bossHealthLabel.BringToFront();

            // Boss ammo icon and count (top right, under boss health)
            bossAmmoIcon = new PictureBox
            {
                Image = Properties.Resources.boos_ammo,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(30, 30),
                Location = new Point(this.ClientSize.Width - 220, 50),
                BackColor = Color.Transparent
            };
            this.Controls.Add(bossAmmoIcon);

            bossAmmoCountLabel = new Label
            {
                Text = $"x {bossAmmoCount}",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.Magenta,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(bossAmmoIcon.Right + 5, 55)
            };
            this.Controls.Add(bossAmmoCountLabel);

            bossAmmoIcon.BringToFront();
            bossAmmoCountLabel.BringToFront();
        }
       

        private void PlaceObstacles()
        {
            // Place 13 trees
            for (int i = 0; i < 13; i++)
            {
                var tree = new PictureBox
                {
                    Image = Properties.Resources.tree,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(40, 40),
                    Location = GetRandomLocation(40, 40),
                    BackColor = Color.Transparent
                };
                obstacles.Add(tree);
                this.Controls.Add(tree);
                tree.BringToFront();
            }

            // Place 15 rocks
            for (int i = 0; i < 15; i++)
            {
                var rock = new PictureBox
                {
                    Image = Properties.Resources.rock,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(35, 35),
                    Location = GetRandomLocation(35, 35),
                    BackColor = Color.Transparent
                };
                obstacles.Add(rock);
                this.Controls.Add(rock);
                rock.BringToFront();
            }

            // Place 3 bushes (not obstacles)
            bushes.Clear();
            for (int i = 0; i < 3; i++)
            {
                var bush = new PictureBox
                {
                    Image = Properties.Resources.bush,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(38, 38),
                    Location = GetRandomLocation(38, 38),
                    BackColor = Color.Transparent
                };
                bushes.Add(bush);
                this.Controls.Add(bush);
                bush.BringToFront();
            }

            highScoreLabel.BringToFront();
            ammoIcon.BringToFront();
            ammoCountLabel.BringToFront();
            healthIcon.BringToFront();
            healthLabel.BringToFront();
        }




        // Avoid spawning obstacles on top of the mouse or any lion
        private Point GetRandomLocation(int width, int height)
        {
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
            Point pt;
            Rectangle mouseRect = new Rectangle(mouse?.Location ?? new Point(0, 0), mouse?.Size ?? new Size(30, 30));
            bool collides;
            do
            {
                pt = new Point(rand.Next(0, screenBounds.Width - width), rand.Next(0, screenBounds.Height - height));
                Rectangle newRect = new Rectangle(pt, new Size(width, height));
                collides = mouseRect.IntersectsWith(newRect);
                foreach (var obs in obstacles)
                {
                    if (newRect.IntersectsWith(new Rectangle(obs.Location, obs.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
                foreach (var lion in lions)
                {
                    if (newRect.IntersectsWith(new Rectangle(lion.Location, lion.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
            } while (collides);
            return pt;
        }

        private Point GetRandomLionLocation(int width, int height)
        {
            // Avoid spawning lions on top of the mouse, obstacles, or other lions
            Point pt;
            Rectangle mouseRect = new Rectangle(mouse?.Location ?? new Point(0, 0), mouse?.Size ?? new Size(30, 30));
            bool collides;
            do
            {
                pt = new Point(rand.Next(0, this.ClientSize.Width - width), rand.Next(0, this.ClientSize.Height - height));
                Rectangle newRect = new Rectangle(pt, new Size(width, height));
                collides = mouseRect.IntersectsWith(newRect);
                foreach (var obs in obstacles)
                {
                    if (newRect.IntersectsWith(new Rectangle(obs.Location, obs.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
                foreach (var lion in lions)
                {
                    if (newRect.IntersectsWith(new Rectangle(lion.Location, lion.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
            } while (collides);
            return pt;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
            }
            pressedKeys.Add(e.KeyCode);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.KeyCode);
        }

        private void MouseTimer_Tick(object sender, EventArgs e)
        {
            var newPos = mouse.Location;
            if (pressedKeys.Contains(Keys.W)) newPos.Y -= MoveStep;
            if (pressedKeys.Contains(Keys.S)) newPos.Y += MoveStep;
            if (pressedKeys.Contains(Keys.A)) newPos.X -= MoveStep;
            if (pressedKeys.Contains(Keys.D)) newPos.X += MoveStep;

            // Keep mouse in bounds
            newPos.X = Math.Max(0, Math.Min(this.ClientSize.Width - mouse.Width, newPos.X));
            newPos.Y = Math.Max(0, Math.Min(this.ClientSize.Height - mouse.Height, newPos.Y));

            // Check collision with obstacles
            Rectangle newRect = new Rectangle(newPos, mouse.Size);
            bool collides = false;
            foreach (var obs in obstacles)
            {
                if (newRect.IntersectsWith(new Rectangle(obs.Location, obs.Size)))
                {
                    collides = true;
                    break;
                }
            }
            if (!collides)
                mouse.Location = newPos;
        }

        private void LionTimer_Tick(object sender, EventArgs e)
        {
            // If mouse is in a bush, lions stop chasing
            if (IsMouseInBush())
                return;

            // Move each lion towards the mouse
            foreach (var lion in lions)
            {
                // Skip movement if this lion is stunned
                if (lion == stunnedLion && DateTime.Now < lionStunEndTime)
                    continue;

                var lionPos = lion.Location;
                var mousePos = mouse.Location;

                // Try to move in X direction
                var tryLionPos = lionPos;
                if (lionPos.X < mousePos.X) tryLionPos.X += LionStep;
                if (lionPos.X > mousePos.X) tryLionPos.X -= LionStep;
                Rectangle tryLionRectX = new Rectangle(tryLionPos, lion.Size);
                bool collidesX = false;
                foreach (var obs in obstacles)
                {
                    if (tryLionRectX.IntersectsWith(new Rectangle(obs.Location, obs.Size)))
                    {
                        collidesX = true;
                        break;
                    }
                }
                // Prevent lions from entering bushes
                foreach (var bush in bushes)
                {
                    if (tryLionRectX.IntersectsWith(new Rectangle(bush.Location, bush.Size)))
                    {
                        collidesX = true;
                        break;
                    }
                }
                foreach (var otherLion in lions)
                {
                    if (otherLion != lion && tryLionRectX.IntersectsWith(new Rectangle(otherLion.Location, otherLion.Size)))
                    {
                        collidesX = true;
                        break;
                    }
                }
                if (!collidesX)
                    lionPos.X = tryLionPos.X;

                // Try to move in Y direction
                tryLionPos = lionPos;
                if (lionPos.Y < mousePos.Y) tryLionPos.Y += LionStep;
                if (lionPos.Y > mousePos.Y) tryLionPos.Y -= LionStep;
                Rectangle tryLionRectY = new Rectangle(tryLionPos, lion.Size);
                bool collidesY = false;
                foreach (var obs in obstacles)
                {
                    if (tryLionRectY.IntersectsWith(new Rectangle(obs.Location, obs.Size)))
                    {
                        collidesY = true;
                        break;
                    }
                }
                // Prevent lions from entering bushes
                foreach (var bush in bushes)
                {
                    if (tryLionRectY.IntersectsWith(new Rectangle(bush.Location, bush.Size)))
                    {
                        collidesY = true;
                        break;
                    }
                }
                foreach (var otherLion in lions)
                {
                    if (otherLion != lion && tryLionRectY.IntersectsWith(new Rectangle(otherLion.Location, otherLion.Size)))
                    {
                        collidesY = true;
                        break;
                    }
                }
                if (!collidesY)
                    lionPos.Y = tryLionPos.Y;

                lion.Location = lionPos;
            }

            // After the loop, if the stun time is over, clear the stunned lion
            if (stunnedLion != null && DateTime.Now >= lionStunEndTime)
                stunnedLion = null;

            // Check collision with cheese
            for (int i = cheeses.Count - 1; i >= 0; i--)
            {
                if (mouse.Bounds.IntersectsWith(cheeses[i].Bounds))
                {
                    this.Controls.Remove(cheeses[i]);
                    cheeses.RemoveAt(i);
                    cheeseCollected++;
                    if (cheeseCollected > highScore)
                        highScore = cheeseCollected;
                    highScoreLabel.Text = $"High Score: {highScore}";

                    // Drop a heart every 5 cheese, if not already present
                    if (cheeseCollected % 5 == 0 && cheeseCollected > 0 && heartDrop == null)
                    {
                        DropHeart();
                    }

                    SpawnCheese();
                }
            }

            // Check collision with heart
            if (heartDrop != null && mouse.Bounds.IntersectsWith(heartDrop.Bounds))
            {
                this.Controls.Remove(heartDrop);
                heartDrop = null;
                if (mouseHealth < 5)
                {
                    mouseHealth++;
                    healthLabel.Text = $"Health: {mouseHealth}";
                }
            }

            // Check collision with ammo drop
            if (ammoDrop != null && mouse.Bounds.IntersectsWith(ammoDrop.Bounds))
            {
                this.Controls.Remove(ammoDrop);
                ammoDrop = null;
                ammoCount++;
                ammoCountLabel.Text = $"x {ammoCount}";
            }

            // Check collision with boss ammo drop
            if (bossActive && bossAmmoDrop != null && mouse.Bounds.IntersectsWith(bossAmmoDrop.Bounds))
            {
                this.Controls.Remove(bossAmmoDrop);
                bossAmmoDrop = null;
                bossAmmoCount++;
                bossAmmoCountLabel.Text = $"x {bossAmmoCount}";
            }

            // Check collision with any lion
            foreach (var lion in lions)
            {
                if (mouse.Bounds.IntersectsWith(lion.Bounds))
                {
                    mouseHealth--;
                    healthLabel.Text = $"Health: {mouseHealth}";
                    if (mouseHealth <= 0)
                    {
                        EndGame(false);
                        return;
                    }
                    TeleportLion(lion);
                    break;
                }
            }
        }

        private void DropHeart()
        {
            heartDrop = new PictureBox
            {
                Image = Properties.Resources.healt,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(25, 25),
                Location = GetRandomLocation(25, 25),
                BackColor = Color.Transparent
            };
            this.Controls.Add(heartDrop);
            heartDrop.BringToFront();
            highScoreLabel.BringToFront();
            ammoIcon.BringToFront();
            ammoCountLabel.BringToFront();
            healthIcon.BringToFront();
            healthLabel.BringToFront();
        }

        private void TeleportLion(PictureBox lion)
        {
            // Teleport lion to a random location not overlapping obstacles, mouse, cheese, heart, or other lions
            Point pt;
            Rectangle mouseRect = new Rectangle(mouse.Location, mouse.Size);
            bool collides;
            do
            {
                pt = new Point(rand.Next(0, this.ClientSize.Width - lion.Width), rand.Next(0, this.ClientSize.Height - lion.Height));
                Rectangle lionRect = new Rectangle(pt, lion.Size);
                collides = mouseRect.IntersectsWith(lionRect);
                foreach (var obs in obstacles)
                {
                    if (lionRect.IntersectsWith(new Rectangle(obs.Location, obs.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
                foreach (var cheese in cheeses)
                {
                    if (lionRect.IntersectsWith(new Rectangle(cheese.Location, cheese.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
                if (heartDrop != null && lionRect.IntersectsWith(new Rectangle(heartDrop.Location, heartDrop.Size)))
                {
                    collides = true;
                }
                foreach (var otherLion in lions)
                {
                    if (otherLion != lion && lionRect.IntersectsWith(new Rectangle(otherLion.Location, otherLion.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
            } while (collides);
            lion.Location = pt;
        }

        private void SpawnCheese()
        {
            Point pt;
            Rectangle cheeseRect;
            bool collides;
            do
            {
                pt = new Point(rand.Next(0, this.ClientSize.Width - 20), rand.Next(0, this.ClientSize.Height - 20));
                cheeseRect = new Rectangle(pt, new Size(20, 20));
                collides = false;

                // Check obstacles
                foreach (var obs in obstacles)
                {
                    if (cheeseRect.IntersectsWith(new Rectangle(obs.Location, obs.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
                // Check lions
                foreach (var lion in lions)
                {
                    if (cheeseRect.IntersectsWith(new Rectangle(lion.Location, lion.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
                // Check mouse
                if (cheeseRect.IntersectsWith(new Rectangle(mouse.Location, mouse.Size)))
                {
                    collides = true;
                }
            } while (collides);

            var cheese = new PictureBox
            {
                Image = Properties.Resources.cheese,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(20, 20),
                Location = new Point(rand.Next(0, this.ClientSize.Width - 20), rand.Next(0, this.ClientSize.Height - 20)),
                BackColor = Color.Transparent
            };
            cheeses.Add(cheese);
            this.Controls.Add(cheese);
            cheese.BringToFront();
            highScoreLabel.BringToFront();
            ammoIcon.BringToFront();
            ammoCountLabel.BringToFront();
            healthIcon.BringToFront();
            healthLabel.BringToFront();
        }

        private void EndGame(bool win)
        {
            lionTimer.Stop();
            mouseTimer.Stop();
            ammoDropTimer.Stop();
            firedAmmoTimer.Stop();
            bossMoveTimer.Stop();
            bossAmmoDropTimer.Stop();
            bossTimer.Stop();

            if (cheeseCollected > highScore)
                highScore = cheeseCollected;

            string message = win
                ? $"You win!\nHigh Score: {highScore}"
                : $"The lion caught you!\nYour Score: {cheeseCollected}\nHigh Score: {highScore}";
            MessageBox.Show(message);

            // Optionally, you can reset the game or enable a restart button here.
            // Do NOT call Application.Exit();
        }

        private void AmmoDropTimer_Tick(object sender, EventArgs e)
        {
            if (ammoDrop == null && ammoCount == 0)
            {
                ammoDrop = new PictureBox
                {
                    Image = Properties.Resources.ammo, // Make sure you have an ammo image in resources
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(20, 20),
                    Location = GetRandomLocation(20, 20),
                    BackColor = Color.Transparent
                };
                this.Controls.Add(ammoDrop);
                ammoDrop.BringToFront();
                highScoreLabel.BringToFront();
                ammoIcon.BringToFront();
                ammoCountLabel.BringToFront();
                healthIcon.BringToFront();
                healthLabel.BringToFront();
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!bossActive && e.Button == MouseButtons.Left && ammoCount > 0 && firedAmmo == null)
            {
                // Find the nearest lion
                PictureBox targetLion = null;
                double minDist = double.MaxValue;
                foreach (var lion in lions)
                {
                    double dist = Math.Sqrt(Math.Pow(mouse.Left - lion.Left, 2) + Math.Pow(mouse.Top - lion.Top, 2));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        targetLion = lion;
                    }
                }
                if (targetLion != null)
                {
                    firedAmmo = new PictureBox
                    {
                        Image = Properties.Resources.ammo,
                        SizeMode = PictureBoxSizeMode.StretchImage,
                        Size = new Size(15, 15),
                        Location = new Point(mouse.Left + mouse.Width / 2 - 7, mouse.Top + mouse.Height / 2 - 7),
                        BackColor = Color.Transparent,
                        Tag = targetLion
                    };
                    this.Controls.Add(firedAmmo);
                    firedAmmo.BringToFront();
                    highScoreLabel.BringToFront();
                    ammoIcon.BringToFront();
                    ammoCountLabel.BringToFront();
                    healthIcon.BringToFront();
                    healthLabel.BringToFront();
                    ammoCount--;
                    ammoCountLabel.Text = $"x {ammoCount}";
                    stunnedLion = null;
                    firedAmmoTimer.Start();
                }
            }

            // Restore boss ammo firing logic
            if (bossActive && e.Button == MouseButtons.Left && bossAmmoCount > 0 && firedBossAmmo == null)
            {
                firedBossAmmo = new PictureBox
                {
                    Image = Properties.Resources.boos_ammo,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(20, 20),
                    Location = new Point(mouse.Left + mouse.Width / 2 - 10, mouse.Top + mouse.Height / 2 - 10),
                    BackColor = Color.Transparent
                };
                this.Controls.Add(firedBossAmmo);
                firedBossAmmo.BringToFront();
                bossHealthLabel?.BringToFront();
                bossAmmoIcon?.BringToFront();
                bossAmmoCountLabel?.BringToFront();
                bossAmmoCount--;
                bossAmmoCountLabel.Text = $"x {bossAmmoCount}";
                firedAmmoTimer.Tick -= FiredAmmoTimer_Tick; // Remove regular ammo handler
                firedAmmoTimer.Tick += FiredBossAmmoTimer_Tick; // Add boss ammo handler
                firedAmmoTimer.Start();
                return;
            }
        }

        private void FiredAmmoTimer_Tick(object sender, EventArgs e)
        {
            if (firedAmmo == null) return;
            PictureBox targetLion = firedAmmo.Tag as PictureBox;
            if (targetLion == null || !lions.Contains(targetLion))
            {
                this.Controls.Remove(firedAmmo);
                firedAmmo = null;
                firedAmmoTimer.Stop();
                return;
            }

            // Move ammo towards lion
            Point ammoCenter = new Point(firedAmmo.Left + firedAmmo.Width / 2, firedAmmo.Top + firedAmmo.Height / 2);
            Point lionCenter = new Point(targetLion.Left + targetLion.Width / 2, targetLion.Top + targetLion.Height / 2);
            double dx = lionCenter.X - ammoCenter.X;
            double dy = lionCenter.Y - ammoCenter.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 10)
            {
                // Hit!
                this.Controls.Remove(firedAmmo);
                firedAmmo = null;
                firedAmmoTimer.Stop();
                stunnedLion = targetLion;
                lionStunEndTime = DateTime.Now.AddSeconds(5);
                return;
            }
            // Move ammo step
            double step = 10.0;
            int moveX = (int)(step * dx / dist);
            int moveY = (int)(step * dy / dist);
            firedAmmo.Left += moveX;
            firedAmmo.Top += moveY;
        }

        private void BossTimer_Tick(object sender, EventArgs e)
        {
            if (!bossActive && (DateTime.Now - gameStartTime).TotalSeconds >= 60)
            {
                // Remove lions
                foreach (var lion in lions)
                {
                    this.Controls.Remove(lion);
                }
                lions.Clear();

                // Spawn boss
                boss = new PictureBox
                {
                    Image = Properties.Resources.boos,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(80, 80),
                    Location = GetRandomLocation(80, 80),
                    BackColor = Color.Transparent
                };
                this.Controls.Add(boss);
                boss.BringToFront();

                // Reset all your ammo when boss spawns
                ammoCount = 0;
                ammoCountLabel.Text = $"x {ammoCount}";

                // Play boss music
                try
                {       
                    bossMusicPlayer?.Stop();
                    bossMusicPlayer = new SoundPlayer(Properties.Resources.boss_muisc); // Ensure your resource is named 'boss_music'
                    bossMusicPlayer.PlayLooping();
                }
                catch { /* Handle missing resource or playback error if needed */ }

                // Start boss ammo drop timer
                bossAmmoDropTimer.Interval = 7000; // every 7 seconds
                bossAmmoDropTimer.Tick += BossAmmoDropTimer_Tick;
                bossAmmoDropTimer.Start();

                // Start boss movement timer
                bossMoveTimer.Interval = 50;
                bossMoveTimer.Tick += BossMoveTimer_Tick;
                bossMoveTimer.Start();

                bossActive = true;
                Invalidate(); // Ensure the health bar is drawn
            }
        }

        private void BossAmmoDropTimer_Tick(object sender, EventArgs e)
        {
            if (bossAmmoDrop == null && bossActive)
            {
                bossAmmoDrop = new PictureBox
                {
                    Image = Properties.Resources.boos_ammo,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(25, 25),
                    Location = GetRandomLocation(25, 25),
                    BackColor = Color.Transparent
                };
                this.Controls.Add(bossAmmoDrop);
                bossAmmoDrop.BringToFront();
                bossHealthLabel?.BringToFront();
                bossAmmoIcon?.BringToFront();
                bossAmmoCountLabel?.BringToFront();
            }
        }

        private void FiredBossAmmoTimer_Tick(object sender, EventArgs e)
        {
            if (firedBossAmmo == null || boss == null) return;

            // Move ammo towards boss
            Point ammoCenter = new Point(firedBossAmmo.Left + firedBossAmmo.Width / 2, firedBossAmmo.Top + firedBossAmmo.Height / 2);
            Point bossCenter = new Point(boss.Left + boss.Width / 2, boss.Top + boss.Height / 2);
            double dx = bossCenter.X - ammoCenter.X;
            double dy = bossCenter.Y - ammoCenter.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 15)
            {
                // Hit!
                this.Controls.Remove(firedBossAmmo);
                firedBossAmmo = null;
                firedAmmoTimer.Stop();
                bossHealth--;
                bossHealthLabel.Text = $"Boss Health: {bossHealth}";
                if (bossHealth <= 0)
                {
                    bossMoveTimer.Stop();
                    bossAmmoDropTimer.Stop();
                    this.Controls.Remove(boss);
                    MessageBox.Show("You defeated the boss! You wont be raped today ");
                    Application.Exit();
                }
                return;
            }
            // Move ammo step
            double step = 12.0;
            int moveX = (int)(step * dx / dist);
            int moveY = (int)(step * dy / dist);
            firedBossAmmo.Left += moveX;
            firedBossAmmo.Top += moveY;
        }

        private void BossMoveTimer_Tick(object sender, EventArgs e)
        {
            if (boss == null || mouse == null) return;

            var bossPos = boss.Location;
            var mousePos = mouse.Location;

            // Increase boss speed by increasing the step value (e.g., from 3 to 6)
            int step = 6;
            if (bossPos.X < mousePos.X) bossPos.X += step;
            if (bossPos.X > mousePos.X) bossPos.X -= step;
            if (bossPos.Y < mousePos.Y) bossPos.Y += step;
            if (bossPos.Y > mousePos.Y) bossPos.Y -= step;

            // Prevent boss from moving into obstacles
            Rectangle newRect = new Rectangle(bossPos, boss.Size);
            bool collides = false;
            foreach (var obs in obstacles)
            {
                if (newRect.IntersectsWith(new Rectangle(obs.Location, obs.Size)))
                {
                    collides = true;
                    break;
                }
            }
            if (!collides)
                boss.Location = bossPos;

            // Check collision with mouse, but only deal damage once per cooldown
            if (boss.Bounds.IntersectsWith(mouse.Bounds))
            {
                if ((DateTime.Now - lastBossHitTime).TotalMilliseconds > BossHitCooldownMs)
                {
                    mouseHealth -= 2;
                    if (mouseHealth < 0) mouseHealth = 0;
                    healthLabel.Text = $"Health: {mouseHealth}";
                    lastBossHitTime = DateTime.Now;

                    // Teleport boss after hit
                    TeleportBoss();

                    if (mouseHealth <= 0)
                    {
                        EndGame(false);
                    }
                }
            }
        }

        private void TeleportBoss()
        {
            if (boss == null) return;
            Point pt;
            bool collides;
            do
            {
                pt = new Point(rand.Next(0, this.ClientSize.Width - boss.Width), rand.Next(0, this.ClientSize.Height - boss.Height));
                Rectangle bossRect = new Rectangle(pt, boss.Size);
                collides = false;
                foreach (var obs in obstacles)
                {
                    if (bossRect.IntersectsWith(new Rectangle(obs.Location, obs.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
                // Avoid cheese, heart, and mouse
                foreach (var cheese in cheeses)
                {
                    if (bossRect.IntersectsWith(new Rectangle(cheese.Location, cheese.Size)))
                    {
                        collides = true;
                        break;
                    }
                }
                if (heartDrop != null && bossRect.IntersectsWith(new Rectangle(heartDrop.Location, heartDrop.Size)))
                {
                    collides = true;
                }
                if (mouse != null && bossRect.IntersectsWith(new Rectangle(mouse.Location, mouse.Size)))
                {
                    collides = true;
                }
            } while (collides);
            boss.Location = pt;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Health bar dimensions
            int barWidth = 300;
            int barHeight = 25;
            int barX = (this.ClientSize.Width - barWidth) / 2;
            int barY = 10;

            // Calculate current health width
            float healthPercent = Math.Max(0, Math.Min(1f, (float)mouseHealth / MaxMouseHealth));
            int healthWidth = (int)(barWidth * healthPercent);

            // Draw background (gray)
            e.Graphics.FillRectangle(Brushes.DarkGray, barX, barY, barWidth, barHeight);

            // Draw health (red)
            if (healthWidth > 0)
                e.Graphics.FillRectangle(Brushes.Red, barX, barY, healthWidth, barHeight);

            // Draw border
            e.Graphics.DrawRectangle(Pens.Black, barX, barY, barWidth, barHeight);

            // Draw the boss health bar only if the boss is active
            if (bossActive)
            {
                // Health bar dimensions
                int bossBarWidth = 400;
                int bossBarHeight = 30;
                int bossBarX = (this.ClientSize.Width - bossBarWidth) / 2;
                int bossBarY = 50;

                // Calculate current health width
                float bossHealthPercent = Math.Max(0, Math.Min(1f, (float)bossHealth / MaxBossHealth));
                int bossHealthWidth = (int)(bossBarWidth * bossHealthPercent);

                // Draw background (solid dark gray)
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(255, 64, 64, 64))) // Opaque dark gray
                {
                    e.Graphics.FillRectangle(bgBrush, bossBarX, bossBarY, bossBarWidth, bossBarHeight);
                }

                // Draw health (solid red)
                if (bossHealthWidth > 0)
                {
                    using (SolidBrush healthBrush = new SolidBrush(Color.Red)) // Opaque red
                    {
                        e.Graphics.FillRectangle(healthBrush, bossBarX, bossBarY, bossHealthWidth, bossBarHeight);
                    }
                }

                // Draw border
                e.Graphics.DrawRectangle(Pens.Black, bossBarX, bossBarY, bossBarWidth, bossBarHeight);
            }
        }

        private bool IsMouseInBush()
        {
            Rectangle mouseRect = new Rectangle(mouse.Location, mouse.Size);
            foreach (var bush in bushes)
            {
                if (mouseRect.IntersectsWith(new Rectangle(bush.Location, bush.Size)))
                    return true;
            }
            return false;
        }
    }
}
