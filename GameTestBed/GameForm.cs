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
        
        private Image[] viewport;
        private Point ptViewportLocation = new Point(0, 0);
        private Size viewportSize = new Size(100, 100);
        private float fovDistance = 100.0f;

        private struct mapvector { public PointF pt; public Pen pen; public float height; };
        private mapvector[] map;

        public Image ScreenBuffer { get; set; }

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

            SetClientSizeCore(320, 200);
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

            playerWorldLocation = new PointF(50.0f, 50.0f);
            playerWorldOrientation = 0;
            playerDirectionVector = new PointF(0.0f, 1.0f);
            playerStrafeVector = new PointF(1.0f, 0.0f);

            wallRect = new RectangleF(new PointF(-100.0f, 100.0f), new SizeF(200.0f, 25.0f));
            wallWorldLocation = new PointF(200.0f, 300.0f);

            viewport = new Bitmap[3]
            { 
                new Bitmap(100, 100, CreateGraphics()), 
                new Bitmap(100, 100, CreateGraphics()),
                new Bitmap(100, 100, CreateGraphics())
            };

            map = new mapvector[6]
            {
                new mapvector() { pt = new PointF(70, 90), pen = Pens.Yellow, height = 200 },
                new mapvector() { pt = new PointF(70, 20), pen = Pens.Violet, height = 200 },
                new mapvector() { pt = new PointF(20, 30), pen = Pens.Green, height = 200 },
                new mapvector() { pt = new PointF(20, 60), pen = Pens.Purple, height = 100 },
                new mapvector() { pt = new PointF(40, 90), pen = Pens.Orange, height = 200 },
                new mapvector() { pt = new PointF(70, 90), pen = Pens.Ivory, height = 200 }
            };

            Size = new Size(640, 400);
        }

        private void DoUpdate()
        {
            if (keyStatus.Up)
            {
                playerWorldLocation.X += (playerDirectionVector.X * 0.2f);
                playerWorldLocation.Y += (playerDirectionVector.Y * 0.2f);
            }
            if (keyStatus.Down)
            {
                playerWorldLocation.X -= (playerDirectionVector.X * 0.2f);
                playerWorldLocation.Y -= (playerDirectionVector.Y * 0.2f);
            }
            if (keyStatus.StrafeLeft)
            {
                playerWorldLocation.X -= (playerStrafeVector.X * 0.2f);
                playerWorldLocation.Y -= (playerStrafeVector.Y * 0.2f);
            }
            if (keyStatus.StrafeRight)
            {
                playerWorldLocation.X += (playerStrafeVector.X * 0.2f);
                playerWorldLocation.Y += (playerStrafeVector.Y * 0.2f);
            }
            if (keyStatus.Left)
                playerWorldOrientation -= 0.02;
            if (keyStatus.Right)
                playerWorldOrientation += 0.02;

            if (keyStatus.FOVUp)
                fovDistance += 1.0f;
            if (keyStatus.FOVDown)
                fovDistance -= 1.0f;

            if (playerWorldOrientation < 0)
                playerWorldOrientation = (2 * Math.PI) + playerWorldOrientation;
            else if (playerWorldOrientation > (2 * Math.PI))
                playerWorldOrientation = playerWorldOrientation - (2 * Math.PI);

            playerDirectionVector = Rotate2D(new PointF(1.0f, 0.0f), playerWorldOrientation);
            playerStrafeVector = Rotate2D(new PointF(0.0f, 1.0f), playerWorldOrientation);

            Text = "FOV Distance: " + fovDistance.ToString("0.00");
        }

        private void DoRender()
        {
            Graphics g;

            g = Graphics.FromImage(ScreenBuffer);
            g.Clear(Color.Black);

            // The end coordinates for the line segment representing a "wall"
            float vx1 = 70; float vy1 = 20;
            float vx2 = 70; float vy2 = 170;

            // The coordinates of the player
            float px = playerWorldLocation.X;
            float py = playerWorldLocation.Y;
            double angle = playerWorldOrientation;

            //
            // Draw the absolute map
            //

            g = Graphics.FromImage(viewport[0]);
            g.Clear(Color.Black);

            for (int i = 0; i < map.Length - 1; i ++)
            {
                vx1 = map[i + 1].pt.X; vy1 = map[i + 1].pt.Y; vx2 = map[i].pt.X; vy2 = map[i].pt.Y;

                g.DrawLine(map[i].pen, vx1, vy1, vx2, vy2);
                g.DrawLine(Pens.White, px, py, (float)(Math.Cos(angle) * 5 + px), (float)(Math.Sin(angle) * 5 + py));
                g.DrawEllipse(Pens.White, px, py, 2, 2);
                double fovAngle = (1.22173 / 2); // 70 degrees, need to halve for some reason ...
                g.DrawLine(Pens.Ivory, px, py,
                    (float)(Math.Cos(angle + fovAngle) * 100 + px),
                    (float)(Math.Sin(angle + fovAngle) * 100 + py));
                g.DrawLine(Pens.Ivory, px, py,
                    (float)(Math.Cos(angle - fovAngle) * 100 + px),
                    (float)(Math.Sin(angle- fovAngle) * 100 + py));

            }

            //
            // Transform the vertices relative to the player
            //

            g = Graphics.FromImage(viewport[1]);
            g.Clear(Color.Black);

            for (int i = 0; i < map.Length - 1; i++)
            {
                vx1 = map[i + 1].pt.X; vy1 = map[i + 1].pt.Y; vx2 = map[i].pt.X; vy2 = map[i].pt.Y;

                float tx1 = vx1 - px; float ty1 = vy1 - py;
                float tx2 = vx2 - px; float ty2 = vy2 - py;

                // Rotate them around the player's view
                float tz1 = (float)(tx1 * Math.Cos(angle) + ty1 * Math.Sin(angle));
                float tz2 = (float)(tx2 * Math.Cos(angle) + ty2 * Math.Sin(angle));
                tx1 = (float)(tx1 * Math.Sin(angle) - ty1 * Math.Cos(angle));
                tx2 = (float)(tx2 * Math.Sin(angle) - ty2 * Math.Cos(angle));

                g.DrawLine(map[i].pen, (50 - tx1), (50 - tz1), (50 - tx2), (50 - tz2));
                g.DrawLine(Pens.White, 50, 50, 50, 45);
                g.DrawEllipse(Pens.White, 50, 50, 2, 2);
                g.DrawLine(Pens.Ivory, px, py,
                    (float)(Math.Cos((Math.PI / 4)) * 100 + 50),
                    (float)(Math.Sin((Math.PI / 4)) * 100 + 50));
                g.DrawLine(Pens.Ivory, px, py,
                    (float)(Math.Cos(-(Math.PI / 4)) * 100 + 50),
                    (float)(Math.Sin(-(Math.PI / 4)) * 100 + 50));
            }

            //
            // Draw the perspective-transformed map
            //                
            
            g = Graphics.FromImage(viewport[2]);
            g.Clear(Color.Black);

            for (int i = 0; i < map.Length - 1; i++)
            {
                vx1 = map[i + 1].pt.X; vy1 = map[i + 1].pt.Y; vx2 = map[i].pt.X; vy2 = map[i].pt.Y;

                float tx1 = vx1 - px; float ty1 = vy1 - py;
                float tx2 = vx2 - px; float ty2 = vy2 - py;

                // Rotate them around the player's view
                float tz1 = (float)(tx1 * Math.Cos(angle) + ty1 * Math.Sin(angle));
                float tz2 = (float)(tx2 * Math.Cos(angle) + ty2 * Math.Sin(angle));
                tx1 = (float)(tx1 * Math.Sin(angle) - ty1 * Math.Cos(angle));
                tx2 = (float)(tx2 * Math.Sin(angle) - ty2 * Math.Cos(angle));

                if (tz1 > 0 || tz2 > 0)
                {
                    PointF i1 = Intersect2D(tx1, tz1, tx2, tz2, -1.000f, 1.000f, -20, 5);
                    PointF i2 = Intersect2D(tx1, tz1, tx2, tz2, 1.000f, 1.000f, 20, 5);

                    float ix1 = i1.X; float iz1 = i1.Y;
                    float ix2 = i2.X; float iz2 = i2.Y;

                    if (tz1 <= 0) { if (iz1 > 0) { tx1 = ix1; tz1 = iz1; } else { tx1 = ix2; tz1 = iz2; } }
                    if (tz2 <= 0) { if (iz1 > 0) { tx2 = ix1; tz2 = iz1; } else { tx2 = ix2; tz2 = iz2; } }

                    float focalLength = fovDistance;
                    float x1 = -tx1 * focalLength / tz1; float y1a = -map[i].height / tz1; float y1b = map[i].height / tz1;
                    float x2 = -tx2 * focalLength / tz2; float y2a = -map[i].height / tz2; float y2b = map[i].height / tz2;

                    for (float x = x1; x <= x2; x++)
                    {
                        float ya = y1a + (x - x1) * (y2a - y1a) / (x2 - x1);
                        float yb = y1b + (x - x1) * (y2b - y1b) / (x2 - x1);

                        g.DrawLine(Pens.DarkGray, 50 + x, 0, 50 + x, 50 + -ya);     // Ceiling
                        g.DrawLine(Pens.Blue, 50 + x, 50 + yb, 50 + x, 140);        // Floor

                        g.DrawLine(map[i].pen, 50 + x, 50 + ya, 50 + x, 50 + yb);   // Wall
                    }

                    g.DrawLine(map[i].pen, 50 + x1, 50 + y1a, 50 + x2, 50 + y2a);   // top (1-2 b)
                    g.DrawLine(map[i].pen, 50 + x1, 50 + y1b, 50 + x2, 50 + y2b);   // bottom (1-2 b)
                    g.DrawLine(Pens.Red, 50 + x1, 50 + y1a, 50 + x1, 50 + y1b);     // left (1)
                    g.DrawLine(Pens.Red, 50 + x2, 50 + y2a, 50 + x2, 50 + y2b);     // right (2)
                }
            }

            g = Graphics.FromImage(ScreenBuffer);
            g.DrawImage(viewport[0], 5, 5);
            g.DrawImage(viewport[1], 110, 5);
            g.DrawImage(viewport[2], 215, 5);
            g.DrawRectangle(Pens.Red, new Rectangle(5, 5, 100, 100));
            g.DrawRectangle(Pens.Red, new Rectangle(110, 5, 100, 100));
            g.DrawRectangle(Pens.Red, new Rectangle(215, 5, 100, 100));

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

        private float CrossProduct2D(float x1, float y1, float x2, float y2)
        {
            return (x1 * y2 - y1 * x2);
        }

        private PointF Intersect2D(float x1, float y1, float x2, float y2,
            float x3, float y3, float x4, float y4)
        {
            float x, y, det;

            x = CrossProduct2D(x1, y1, x2, y2);
            y = CrossProduct2D(x3, y3, x4, y4);
            det = CrossProduct2D(x1 - x2, y1 - y2, x3 - x4, y3 - y4);
            x = CrossProduct2D(x, x1 - x2, y, x3 - x4) / det;
            y = CrossProduct2D(x, y1 - y2, y, y3 - y4) / det;

            return new PointF(x, y);
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
        public bool FOVUp { get; set; }
        public bool FOVDown { get; set; }

        public KeyStatus()
        {
            Up = Down = Left = Right = StrafeLeft = StrafeRight = false;
            FOVUp = FOVDown = false;
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
            if (e.KeyCode == Keys.N)
                FOVDown = status;
            if (e.KeyCode == Keys.M)
                FOVUp = status;
        }
    }
}
