using System.Windows.Forms;
using System.Drawing;
using System;
using System.Text;

namespace GameTestBed
{
    public partial class GameForm : Form
    {
        private bool isGameRunning;
        private PointF playerWorldLocation;
        private double playerWorldOrientation;
        private PointF playerDirectionVector;
        private PointF playerStrafeVector;
        private RectangleF wallRect;
        private PointF wallWorldLocation;
        private KeyStatus keyStatus;

        public Image ScreenBuffer { get; }

        public GameForm()
        {
            InitializeComponent();

            isGameRunning = false;

            Text = "Game Form";

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            BackColor = Color.Black;

            SetClientSizeCore(1280, 480);
            StartPosition = FormStartPosition.Manual;

            ScreenBuffer = new Bitmap(ClientSize.Width, ClientSize.Height,
                CreateGraphics());
        }

        static void Main()
        {
            GameForm gameForm = new GameForm();

            gameForm.Location = new Point(50, 50);
            gameForm.Show();

            gameForm.DoInitialize();
            gameForm.isGameRunning = true;

            while (gameForm.isGameRunning)
            {
                gameForm.DoUpdate();
                gameForm.DoRender();
                Application.DoEvents();
            }

            gameForm.Close();            
        }

        private void DoInitialize()
        {
            keyStatus = new KeyStatus();

            playerWorldLocation = new PointF(200.0f, 200.0f);
            playerWorldOrientation = 0;
            playerDirectionVector = new PointF(0.0f, 1.0f);
            playerStrafeVector = new PointF(1.0f, 0.0f);

            wallRect = new RectangleF(new PointF(-100.0f, 100.0f), new SizeF(200.0f, 25.0f));
            wallWorldLocation = new PointF(200.0f, 300.0f);
        }

        private void DoUpdate()
        {
            if (keyStatus.Up)
            {
                playerWorldLocation.X += (playerDirectionVector.X * 0.5f);
                playerWorldLocation.Y += (playerDirectionVector.Y * 0.5f);
            }
            if (keyStatus.Down)
            {
                playerWorldLocation.X -= (playerDirectionVector.X * 0.5f);
                playerWorldLocation.Y -= (playerDirectionVector.Y * 0.5f);
            }
            if (keyStatus.StrafeLeft)
            {
                playerWorldLocation.X -= (playerStrafeVector.X * 0.5f);
                playerWorldLocation.Y -= (playerStrafeVector.Y * 0.5f);
            }
            if (keyStatus.StrafeRight)
            {
                playerWorldLocation.X += (playerStrafeVector.X * 0.5f);
                playerWorldLocation.Y += (playerStrafeVector.Y * 0.5f);
            }
            if (keyStatus.Left)
                playerWorldOrientation -= 0.01;
            if (keyStatus.Right)
                playerWorldOrientation += 0.01;

            if (playerWorldOrientation < 0)
                playerWorldOrientation = (2 * Math.PI) + playerWorldOrientation;
            else if (playerWorldOrientation > (2 * Math.PI))
                playerWorldOrientation = playerWorldOrientation - (2 * Math.PI);

            playerDirectionVector = Rotate2D(new PointF(0.0f, 1.0f), playerWorldOrientation);
            playerStrafeVector = Rotate2D(new PointF(1.0f, 0.0f), playerWorldOrientation);
        }

        private void DoRender()
        {
            Graphics g;
            Point ptViewportLocation = new Point();
            Size viewportSize = new Size(400, 400);
            PointF pt = new PointF();
            PointF ptA = new PointF();
            PointF ptB = new PointF();

            g = Graphics.FromImage(ScreenBuffer);
            g.Clear(Color.Black);

            //
            // View fixed world top down ...
            //

            ptViewportLocation = new Point(10, 10);
            g.DrawRectangle(Pens.Red, new Rectangle(ptViewportLocation, viewportSize));

            ptA = playerWorldLocation;
            ptB = playerDirectionVector;
            ptB.X = (ptB.X * 20) + playerWorldLocation.X;
            ptB.Y = (ptB.Y * 20) + playerWorldLocation.Y;
            ptA.X += ptViewportLocation.X;
            ptA.Y = (viewportSize.Height - ptA.Y) + ptViewportLocation.Y;
            ptB.X += ptViewportLocation.X;
            ptB.Y = (viewportSize.Height - ptB.Y) + ptViewportLocation.Y;
            g.DrawEllipse(Pens.Yellow, new RectangleF(ptA.X - 10, ptA.Y - 10, 20, 20));
            g.DrawLine(Pens.Red, ptA, ptB);

            // Draw a wall ...
            // Translate to World Coordinates ...
            pt.X = (wallRect.X + wallWorldLocation.X);
            pt.Y = wallWorldLocation.Y;
            // Draw in Screen Coordinates ...
            pt.X += ptViewportLocation.X;
            pt.Y = (viewportSize.Height - pt.Y) + ptViewportLocation.Y;
            g.DrawLine(Pens.Yellow, pt.X, pt.Y, (wallRect.Width + pt.X), pt.Y);

            //
            // View fixed to player top down ...
            //

            ptViewportLocation = new Point(420, 10);
            g.DrawRectangle(Pens.Red, new Rectangle(ptViewportLocation, viewportSize));

            ptA = new Point(viewportSize.Width / 2, viewportSize.Height / 2);
            ptB = new PointF(ptA.X, ptA.Y - 20.0f);
            ptA.X += ptViewportLocation.X;
            ptA.Y += ptViewportLocation.Y;
            ptB.X += ptViewportLocation.X;
            ptB.Y += ptViewportLocation.Y;
            g.DrawEllipse(Pens.Yellow, new RectangleF(ptA.X - 10, ptA.Y - 10, 20, 20));
            g.DrawLine(Pens.Red, ptA, ptB);

            // Draw a wall ...
            // Translate to World Coordintates ...
            ptA.X = (wallRect.X + wallWorldLocation.X);
            ptA.Y = wallWorldLocation.Y;
            ptB.X = (ptA.X + wallRect.Width);
            ptB.Y = wallWorldLocation.Y;
            // Translate to Player Coordinates ...
            ptA.X -= playerWorldLocation.X;
            ptA.Y -= playerWorldLocation.Y;
            ptA = Rotate2D(ptA, -playerWorldOrientation);
            ptB.X -= playerWorldLocation.X;
            ptB.Y -= playerWorldLocation.Y;
            ptB = Rotate2D(ptB, -playerWorldOrientation);
            // Draw in Screen / Viewport Coordinates ...
            ptA.X += ptViewportLocation.X + (viewportSize.Width / 2);
            ptA.Y = ((viewportSize.Height - ptA.Y) - (viewportSize.Height / 2)) + ptViewportLocation.Y;
            ptB.X += ptViewportLocation.X + (viewportSize.Width / 2);
            ptB.Y = ((viewportSize.Height - ptB.Y) - (viewportSize.Height / 2)) + ptViewportLocation.Y;
            g.DrawLine(Pens.Yellow, ptA, ptB);

            // View fixed to player 3d perspective ...
            // Project to 2D viewpoint based on player ...
            // Treat Y axis as Z from now on ...

            float focalLength = 50.0f;

            ptViewportLocation = new Point(830, 10);
            g.DrawRectangle(Pens.Red, new Rectangle(ptViewportLocation, new Size(400, 400)));
            g.DrawLine(Pens.Green, new Point(830, 210), new Point(1230, 210));

            // Draw a wall ...
            // Translate to World Coordintates ...
            ptA.X = (wallRect.X + wallWorldLocation.X);
            ptA.Y = wallWorldLocation.Y; // Z coordinate
            ptB.X = ptA.X + wallRect.Width;
            ptB.Y = wallWorldLocation.Y; // Z coordinate

            // Translate to Player Coordinates ...
            ptA.X -= playerWorldLocation.X;
            ptA.Y -= playerWorldLocation.Y; // Z coordinate
            ptA = Rotate2D(ptA, -playerWorldOrientation);
            ptB.X -= playerWorldLocation.X;
            ptB.Y -= playerWorldLocation.Y; // Z coordinate
            ptB = Rotate2D(ptB, -playerWorldOrientation);

            PointF ptViewA = new Point();
            PointF ptViewB = new Point();
            // Handle viewport clippling ...
            if (ptA.Y > 0 || ptB.Y > 0)
            {
                // If the line crosses the player's viewplane, clip it.
                PointF iA = Intersect2D(ptA, ptB, new PointF(0, 0), new PointF(-20, 5));
                PointF iB = Intersect2D(ptA, ptB, new PointF(0, 0), new PointF(20, 5));

                if (ptA.Y <= 0)
                {
                    if (iA.Y > 0)
                    {
                        ptA.X = iA.X;
                        ptA.Y = iA.Y;
                    }
                    else
                    {
                        ptA.X = iB.X;
                        ptA.Y = iB.Y;
                    }
                }

                if (ptB.Y <= 0)
                {
                    if (iA.Y > 0)
                    {
                        ptB.X = iA.X;
                        ptB.Y = iA.Y;
                    }
                    else
                    {
                        ptB.X = iB.X;
                        ptB.Y = iB.Y;
                    }
                }

                // Project on viewport ...
                // Remember that Y is Z now ...
                ptViewA.X = ptA.X * (float)(focalLength / ptA.Y);
                ptViewA.Y = (-wallRect.Height) * (float)(focalLength / ptA.Y);
                ptViewB.X = ptB.X * (float)(focalLength / ptB.Y);
                ptViewB.Y = (-wallRect.Height) * (float)(focalLength / ptB.Y);

                // Render it ...
                // Oh and adjust for fact we are in player coordinates,
                // although should have done that above ... duh!
                // No were not! We are in viewport co-ordinates!
                ptViewA.X += ptViewportLocation.X + (viewportSize.Width / 2);
                ptViewA.Y += ptViewportLocation.Y + (viewportSize.Height / 2);
                ptViewB.X += ptViewportLocation.X + (viewportSize.Width / 2);
                ptViewB.Y += ptViewportLocation.Y + (viewportSize.Height / 2);
                g.DrawLine(Pens.Yellow, ptViewA, ptViewB);
            }

            // Draw a top of wall ...
            // Translate to World Coordintates ...
            ptA.X = (wallRect.X + wallWorldLocation.X);
            ptA.Y = wallWorldLocation.Y; // Z coordinate
            ptB.X = ptA.X + wallRect.Width;
            ptB.Y = wallWorldLocation.Y; // Z coordinate

            // Translate to Player Coordinates ...
            ptA.X -= playerWorldLocation.X;
            ptA.Y -= playerWorldLocation.Y; // Z coordinate
            ptA = Rotate2D(ptA, -playerWorldOrientation);
            ptB.X -= playerWorldLocation.X;
            ptB.Y -= playerWorldLocation.Y; // Z coordinate
            ptB = Rotate2D(ptB, -playerWorldOrientation);

            // Handle viewport clippling ...
            if (ptA.Y > 0 || ptB.Y > 0)
            {
                // If the line crosses the player's viewplane, clip it.
                PointF iA = Intersect2D(ptA, ptB, new PointF(0, 0), new PointF(-20, 5));
                PointF iB = Intersect2D(ptA, ptB, new PointF(0, 0), new PointF(20, 5));

                if (ptA.Y <= 0)
                {
                    if (iA.Y > 0)
                    {
                        ptA.X = iA.X;
                        ptA.Y = iA.Y;
                    }
                    else
                    {
                        ptA.X = iB.X;
                        ptA.Y = iB.Y;
                    }
                }

                if (ptB.Y <= 0)
                {
                    if (iA.Y > 0)
                    {
                        ptB.X = iA.X;
                        ptB.Y = iA.Y;
                    }
                    else
                    {
                        ptB.X = iB.X;
                        ptB.Y = iB.Y;
                    }
                }

                // Project on viewport ...
                // Remember that Y is Z now ...
                ptViewA = new Point();
                ptViewB = new Point();
                ptViewA.X = ptA.X * (float)(focalLength / ptA.Y);
                ptViewA.Y = (wallRect.Height) * (float)(focalLength / ptA.Y);
                ptViewB.X = ptB.X * (float)(focalLength / ptB.Y);
                ptViewB.Y = (wallRect.Height) * (float)(focalLength / ptB.Y);

                // Render it ...
                // Oh and adjust for fact we are in player coordinates,
                // although should have done that above ... du5h!
                ptViewA.X += ptViewportLocation.X + (viewportSize.Width / 2);
                ptViewA.Y += ptViewportLocation.Y + (viewportSize.Height / 2);
                ptViewB.X += ptViewportLocation.X + (viewportSize.Width / 2);
                ptViewB.Y += ptViewportLocation.Y + (viewportSize.Height / 2);
                g.DrawLine(Pens.Red, ptViewA, ptViewB);
            }
            
            //StringBuilder sb = new StringBuilder();
            //sb.Append("Radians: ");
            //sb.Append(playerWorldOrientation.ToString("0.00"));
            //sb.Append(" ");
            //sb.Append(pt);
            //sb.Append(" ");
            //sb.Append(playerWorldLocation);
            //sb.Append(" Length: ");
            //sb.Append((Math.Sqrt(Math.Pow(pt.X, 2) + Math.Pow(pt.Y, 2))).ToString("0.00"));

            //Text = sb.ToString();

            Invalidate();
        }

        private PointF Rotate2D(PointF p, double angle)
        {
            float x = p.X;
            float y = p.Y;

            p.X = (float)((Math.Cos(angle) * x) - (Math.Sin(angle) * y));
            p.Y = (float)((Math.Sin(angle) * x) + (Math.Cos(angle) * y));

            return p;
        }

        private float CrossProduct2D(PointF a, PointF b)
        {
            return (a.X * b.Y - a.Y * b.X);
        }

        private PointF Intersect2D(PointF a1, PointF a2, PointF b1, PointF b2)
        {
            PointF pt = new Point();
            float det = 0.0f;

            pt.X = CrossProduct2D(a1, a2);
            pt.Y = CrossProduct2D(b1, b2);
            det = CrossProduct2D(new PointF(a1.X - a2.X, a1.Y - a2.Y), new PointF(b1.X - b2.X, b1.Y - b2.Y));
            pt.X = CrossProduct2D(new PointF(pt.X, a1.X - a2.X), new PointF(pt.Y, b1.X - b2.X)) / det;
            pt.Y = CrossProduct2D(new PointF(pt.X, a1.Y - a2.Y), new PointF(pt.Y, b1.Y - b2.Y)) / det;

            return pt;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.DrawImage(ScreenBuffer, 0, 0, ClientSize.Width, ClientSize.Height);

            base.OnPaint(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            isGameRunning = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.Escape:
                    isGameRunning = false;
                    break;
                default:
                    keyStatus.SetKeyStatus(e, true);
                    break;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            keyStatus.SetKeyStatus(e, false);

            base.OnKeyUp(e);
        }
    }

    public class KeyStatus
    {
        public bool Up { get; set; }
        public bool Down { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }
        public bool StrafeLeft { get; set; }
        public bool StrafeRight { get; set; }

        public KeyStatus()
        {
            Up = Down = Left = Right = StrafeLeft = StrafeRight = false;
        }

        public void SetKeyStatus(KeyEventArgs e, bool status)
        {
            if (e.KeyCode == Keys.W)
                Up = status;
            if (e.KeyCode == Keys.S)
                Down = status;
            if (e.KeyCode == Keys.A)
                Left = status;
            if (e.KeyCode == Keys.D)
                Right = status;
            if (e.KeyCode == Keys.K)
                StrafeLeft = status;
            if (e.KeyCode == Keys.L)
                StrafeRight = status;
        }
    }
}
