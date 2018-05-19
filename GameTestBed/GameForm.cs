using System.Windows.Forms;
using System.Drawing;
using System;
using System.Text;

namespace GameTestBed
{
    //
    // Vector Support Objects
    //

    public struct Line { public PointF pt1; public PointF pt2; };

    public enum LineIntersectionResultType { Collinear, Parallel, Intersecting, NonIntersecting };

    public struct LineIntersectionResult2D
    {
        public LineIntersectionResultType resultType;
        public PointF intersectionPoint;

        public LineIntersectionResult2D(LineIntersectionResultType resultType, PointF intersectionPoint)
        {
            this.resultType = resultType;
            this.intersectionPoint = intersectionPoint;
        }
    }

    //
    // Game Form 
    //

    public partial class GameForm : Form
    {
        //
        // Variables and Stuff ...
        //
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
        private float fovDistance = 50.0f;

        private float fovNearDistance = 5.0f;
        private float fovFarDistance = 30.0f;
        private double fovAngleHalved = Math.PI / 4;
        private float focalLength = 0.0f;

        private const double FIXED_PLAYER_ANGLE = (Math.PI / 2); // 90 degrees
        //private const double FIXED_PLAYER_ANGLE = 0;

        private struct mapvector { public PointF pt; public Pen pen; public float height; public bool isPortal; };
        private mapvector[] map;

        //
        // Screen Support Objects
        //
        public Image ScreenBuffer { get; set; }

        //
        // Game Stuff
        //
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

            playerWorldLocation = new PointF(0.0f, 50.0f);
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
                new mapvector() { pt = new PointF(70, 90), pen = Pens.Yellow, height = 5 },
                new mapvector() { pt = new PointF(70, 20), pen = Pens.Violet, height = 5 },
                new mapvector() { pt = new PointF(20, 30), pen = Pens.Green, height = 5 },
                new mapvector() { pt = new PointF(20, 60), pen = Pens.Purple, height = 5, isPortal = true },
                new mapvector() { pt = new PointF(40, 90), pen = Pens.Orange, height = 5 },
                new mapvector() { pt = new PointF(70, 90), pen = Pens.Ivory, height = 5 }
            };

            map = new mapvector[2]
            {
                new mapvector() { pt = new PointF(50, 50 + (50 / 2.0f)), pen = Pens.Yellow, height = 5.0f },
                new mapvector() { pt = new PointF(50, 50 - (50 / 2.0f)), pen = Pens.Yellow, height = 5.0f }
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
                fovAngleHalved += 0.01f;
            if (keyStatus.FOVDown)
                fovAngleHalved -= 0.01f;

            if (fovAngleHalved < 0) { fovAngleHalved = 0; };

            // Canvas Size = tan(theta / 2) * Distance to Canvas
            focalLength = (50.0f) / (float)Math.Tan(fovAngleHalved);
            fovFarDistance = focalLength;

            if (playerWorldOrientation < 0)
                playerWorldOrientation = (2 * Math.PI) + playerWorldOrientation;
            else if (playerWorldOrientation > (2 * Math.PI))
                playerWorldOrientation = playerWorldOrientation - (2 * Math.PI);

            playerDirectionVector = Rotate2D(new PointF(1.0f, 0.0f), playerWorldOrientation);
            playerStrafeVector = Rotate2D(new PointF(0.0f, 1.0f), playerWorldOrientation);
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

            // Intersection stuff
            LineIntersectionResult2D lir1;
            LineIntersectionResult2D lir2;
            Line AB = new Line();
            Line FOV_AB = new Line();
            Line FOV_CD = new Line();
                
            //
            // Draw the absolute map
            //

            g = Graphics.FromImage(viewport[0]);
            g.Clear(Color.Black);

            float fovNearX1 = (float)(Math.Cos(angle - fovAngleHalved) * fovNearDistance);
            float fovNearY1 = (float)(Math.Sin(angle - fovAngleHalved) * fovNearDistance);
            float fovNearX2 = (float)(Math.Cos(angle + fovAngleHalved) * fovNearDistance);
            float fovNearY2 = (float)(Math.Sin(angle + fovAngleHalved) * fovNearDistance);

            float fovFarX1 = (float)(Math.Cos(angle - fovAngleHalved) * fovFarDistance);
            float fovFarY1 = (float)(Math.Sin(angle - fovAngleHalved) * fovFarDistance);
            float fovFarX2 = (float)(Math.Cos(angle + fovAngleHalved) * fovFarDistance);
            float fovFarY2 = (float)(Math.Sin(angle + fovAngleHalved) * fovFarDistance);

            FOV_AB.pt1.X = fovNearX1 + px; FOV_AB.pt1.Y = fovNearY1 + py;
            FOV_AB.pt2.X = fovFarX1 + px; FOV_AB.pt2.Y = fovFarY1 + py;
            FOV_CD.pt1.X = fovNearX2 + px; FOV_CD.pt1.Y = fovNearY2 + py;
            FOV_CD.pt2.X = fovFarX2 + px; FOV_CD.pt2.Y = fovFarY2 + py;

            PointF n1 = Normalise(new PointF(-(FOV_AB.pt2.Y - FOV_AB.pt1.Y), (FOV_AB.pt2.X - FOV_AB.pt1.X)));
            PointF n2 = Normalise(new PointF((FOV_CD.pt2.Y - FOV_CD.pt1.Y), -(FOV_CD.pt2.X - FOV_CD.pt1.X)));

            for (int i = 0; i < map.Length - 1; i ++)
            {
                vx1 = map[i].pt.X; vy1 = map[i].pt.Y; vx2 = map[i + 1].pt.X; vy2 = map[i + 1].pt.Y;

                AB.pt1.X = vx1; AB.pt1.Y = vy1; AB.pt2.X = vx2; AB.pt2.Y = vy2;
                PointF? t = LineIntersection2D(AB, FOV_AB, n1);
                lir1 = Intersect2D(FOV_AB, AB);
                PointF? t2 = LineIntersection2D(AB, FOV_CD, n2);
                lir2 = Intersect2D(FOV_CD, AB);

                PointF nAB = Normalise(new PointF(-(AB.pt2.Y - AB.pt1.Y), (AB.pt2.X - AB.pt1.X)));
                float dp1 = DistanceToLine2D(FOV_AB, n1, AB.pt1);
                float dp2 = DistanceToLine2D(FOV_AB, n1, AB.pt2);
                float dp3 = DistanceToLine2D(FOV_CD, n2, AB.pt1);
                float dp4 = DistanceToLine2D(FOV_CD, n2, AB.pt2);

#if DEBUG
                PointF a, b;

                a = VectorAdd2D(n1, GetLineSegmentCenter(FOV_AB));
                b = VectorAdd2D(VectorScale2D(n1, 5), a);

                g.DrawLine(Pens.Green, a, b);

                a = VectorAdd2D(n2, GetLineSegmentCenter(FOV_CD));
                b = VectorAdd2D(VectorScale2D(n2, 5), a);

                g.DrawLine(Pens.Red, a, b);

                a = VectorAdd2D(nAB, GetLineSegmentCenter(AB));
                b = VectorAdd2D(VectorScale2D(nAB, 5), a);

                g.DrawLine(map[i].pen, a, b);
#endif
                PointF i1 = AB.pt1;
                PointF i2 = AB.pt2;
                if ((dp1 < 0 && dp2 < 0) || (dp3 < 0 && dp4 <0))
                    continue;

                if (lir1.resultType == LineIntersectionResultType.Intersecting)
                {
                    if (dp1 < 0 || dp2 < 0) // <= is probably not right?!? tollerance epsilon maybe??
                    {
                        i2 = lir1.intersectionPoint;
                    }
                    else
                    {
                        i1 = lir1.intersectionPoint;
                    }
                }

                if (lir2.resultType == LineIntersectionResultType.Intersecting)
                {
                    if (dp3 < 0 || dp4 < 0)
                    {
                        i1 = lir2.intersectionPoint;
                    }
                    else
                    {
                        i2 = lir2.intersectionPoint;
                    }
                }

                i1 = t == null ? AB.pt1 : (PointF)t;
                i2 = t2 == null ? AB.pt1 : (PointF)t2;

                // Draw Wall
                g.DrawLine(Pens.White, vx1, vy1, vx2, vy2);

                // Draw Intsection
                g.DrawLine(map[i].pen, i1, i2);

            }

            g.DrawLine(Pens.White, px, py, (float)(Math.Cos(angle) * 5 + px), (float)(Math.Sin(angle) * 5 + py));
            g.DrawEllipse(Pens.White, px - 1, py - 1, 2, 2);

            g.DrawLine(Pens.Ivory, fovNearX1 + px, fovNearY1 + py, fovFarX1 + px, fovFarY1 + py);
            g.DrawLine(Pens.Ivory, fovNearX2 + px, fovNearY2 + py, fovFarX2 + px, fovFarY2 + py);
            g.DrawLine(Pens.Ivory, fovNearX1 + px, fovNearY1 + py, fovNearX2 + px, fovNearY2 + py);
            g.DrawLine(Pens.Ivory, fovFarX1 + px, fovFarY1 + py, fovFarX2 + px, fovFarY2 + py);

            //
            // Transform the vertices relative to the player
            //

            g = Graphics.FromImage(viewport[1]);
            g.Clear(Color.Black);

            Graphics g3 = Graphics.FromImage(viewport[2]);
            g3.Clear(Color.Black);

            fovNearX1 = (float)(Math.Cos(FIXED_PLAYER_ANGLE - fovAngleHalved) * fovNearDistance);
            fovNearY1 = (float)(Math.Sin(FIXED_PLAYER_ANGLE - fovAngleHalved) * fovNearDistance);
            fovNearX2 = (float)(Math.Cos(FIXED_PLAYER_ANGLE + fovAngleHalved) * fovNearDistance);
            fovNearY2 = (float)(Math.Sin(FIXED_PLAYER_ANGLE + fovAngleHalved) * fovNearDistance);

            fovFarX1 = (float)(Math.Cos(FIXED_PLAYER_ANGLE - fovAngleHalved) * fovFarDistance);
            fovFarY1 = (float)(Math.Sin(FIXED_PLAYER_ANGLE - fovAngleHalved) * fovFarDistance);
            fovFarX2 = (float)(Math.Cos(FIXED_PLAYER_ANGLE + fovAngleHalved) * fovFarDistance);
            fovFarY2 = (float)(Math.Sin(FIXED_PLAYER_ANGLE + fovAngleHalved) * fovFarDistance);

            float xOffset = 50;
            float yOffset = 50;

            FOV_AB.pt1.X = fovNearX1; FOV_AB.pt1.Y = fovNearY1;
            FOV_AB.pt2.X = fovFarX1; FOV_AB.pt2.Y = fovFarY1;
            FOV_CD.pt1.X = fovNearX2; FOV_CD.pt1.Y = fovNearY2;
            FOV_CD.pt2.X = fovFarX2; FOV_CD.pt2.Y = fovFarY2;

            n1 = Normalise(new PointF(-(FOV_AB.pt2.Y - FOV_AB.pt1.Y), (FOV_AB.pt2.X - FOV_AB.pt1.X)));
            n2 = Normalise(new PointF((FOV_CD.pt2.Y - FOV_CD.pt1.Y), -(FOV_CD.pt2.X - FOV_CD.pt1.X)));

            for (int i = 0; i < map.Length - 1; i++)
            {
                vx1 = map[i].pt.X; vy1 = map[i].pt.Y; vx2 = map[i + 1].pt.X; vy2 = map[i + 1].pt.Y;

                float tx1 = vx1 - px; float ty1 = vy1 - py;
                float tx2 = vx2 - px; float ty2 = vy2 - py;

                // Rotate them around the player's view
                double a = angle;
                float tz1 = (float)(tx1 * Math.Cos(a) + ty1 * Math.Sin(a));
                float tz2 = (float)(tx2 * Math.Cos(a) + ty2 * Math.Sin(a));
                tx1 = (float)(tx1 * Math.Sin(a) - ty1 * Math.Cos(a));
                tx2 = (float)(tx2 * Math.Sin(a) - ty2 * Math.Cos(a));

                //float tz1 = ty1; float tz2 = ty2;

                // Clip to the view frustum
                //if (tz1 > 0 || tz2 > 0)
                {
                    AB.pt1.X = tx1; AB.pt1.Y = tz1;
                    AB.pt2.X = tx2; AB.pt2.Y = tz2;
                    AB.pt1.X = vx1; AB.pt1.Y = vy1; AB.pt2.X = vx2; AB.pt2.Y = vy2;
                    PointF? t = LineIntersection2D(AB, FOV_AB, n1);
                    lir1 = Intersect2D(FOV_AB, AB);
                    PointF? t2 = LineIntersection2D(AB, FOV_CD, n2);
                    lir2 = Intersect2D(FOV_CD, AB);

                    PointF nAB = Normalise(new PointF(-(AB.pt2.Y - AB.pt1.Y), (AB.pt2.X - AB.pt1.X)));
                    float dp1 = DistanceToLine2D(FOV_AB, n1, AB.pt1);
                    float dp2 = DistanceToLine2D(FOV_AB, n1, AB.pt2);
                    float dp3 = DistanceToLine2D(FOV_CD, n2, AB.pt1);
                    float dp4 = DistanceToLine2D(FOV_CD, n2, AB.pt2);

                    PointF i1 = AB.pt1;
                    PointF i2 = AB.pt2;
                    if ((dp1 < 0 && dp2 < 0) || (dp3 < 0 && dp4 < 0))
                        continue;

                    if (lir1.resultType == LineIntersectionResultType.Intersecting)
                    {
                        if (dp1 < 0 || dp2 < 0) // <= is probably not right?!? tollerance epsilon maybe??
                        {
                            i2 = lir1.intersectionPoint;
                        }
                        else
                        {
                            i1 = lir1.intersectionPoint;
                        }
                    }

                    if (lir2.resultType == LineIntersectionResultType.Intersecting)
                    {
                        if (dp3 < 0 || dp4 < 0)
                        {
                            i1 = lir2.intersectionPoint;
                        }
                        else
                        {
                            i2 = lir2.intersectionPoint;
                        }
                    }

                    i1 = t == null ? AB.pt1 : (PointF)t;
                    i2 = t2 == null ? AB.pt2 : (PointF)t2;

                    // Draw Wall
                    g.DrawLine(Pens.White, xOffset - tx1, yOffset - tz1, xOffset - tx2, yOffset - tz2);
                    //g.DrawLine(Pens.White, tx1, tz1, tx2, tz2);

                    // Draw Intsection
                    //i1.X = xOffset - i1.X; i1.Y = yOffset - i1.Y; i2.X = xOffset - i2.X; i2.Y = yOffset - i2.Y;
                    PointF sA = new PointF(xOffset - i1.X, yOffset - i1.Y);
                    PointF sB = new PointF(xOffset - i2.X, yOffset - i2.Y);
                    g.DrawLine(map[i].pen, sA, sB);
                    //g.DrawLine(map[i].pen, AB.pt1, AB.pt2);

                    // Do 3D (2.5D) render now ...

                    tx1 = i1.X; tz1 = i1.Y; tx2 = i2.X; tz2 = i2.Y;

                    //float focalLength = Math.Abs(fovNearY2);// + Math.Abs(fovNearY2);
                    float x1 = tx1 * focalLength / tz1;
                    float y1a = -map[i].height * focalLength / tz1; float y1b = map[i].height * focalLength / tz1;
                    float x2 = tx2 * focalLength / tz2;
                    float y2a = -map[i].height * focalLength / tz2; float y2b = map[i].height * focalLength / tz2;

                    for (float x = x1; x <= x2; x++)
                    {
                        float ya = y1a + (x - x1) * (y2a - y1a) / (x2 - x1);
                        float yb = y1b + (x - x1) * (y2b - y1b) / (x2 - x1);

                        if (map[i].isPortal)
                        {
                            g3.DrawLine(Pens.DarkGray, 50 + x, 0, 50 + x, 50 + -ya);     // Ceiling
                            g3.DrawLine(Pens.Blue, 50 + x, 50 + yb, 50 + x, 140);        // Floor
                            g3.DrawLine(Pens.Red, 50 + x, 50 + ya, 50 + x, 50 + yb);     // Portal
                        }
                        else
                        {
                            g3.DrawLine(Pens.DarkGray, 50 + x, 0, 50 + x, 50 + -ya);     // Ceiling
                            g3.DrawLine(Pens.Blue, 50 + x, 50 + yb, 50 + x, 140);        // Floor

                            g3.DrawLine(map[i].pen, 50 + x, 50 + ya, 50 + x, 50 + yb);   // Wall
                        }
                    }

                    g3.DrawLine(map[i].pen, 50 + x1, 50 + y1a, 50 + x2, 50 + y2a);   // top (1-2 b)
                    g3.DrawLine(map[i].pen, 50 + x1, 50 + y1b, 50 + x2, 50 + y2b);   // bottom (1-2 b)
                    g3.DrawLine(Pens.Red, 50 + x1, 50 + y1a, 50 + x1, 50 + y1b);     // left (1)
                    g3.DrawLine(Pens.Red, 50 + x2, 50 + y2a, 50 + x2, 50 + y2b);     // right (2)
                }
            }

            g.DrawLine(Pens.White, xOffset, yOffset, xOffset, yOffset - 5);
            g.DrawEllipse(Pens.White, xOffset -1, yOffset + 1, 2, 2);

            PointF ptA = new PointF(xOffset - FOV_AB.pt1.X, yOffset - FOV_AB.pt1.Y);
            PointF ptB = new PointF(xOffset - FOV_AB.pt2.X, yOffset - FOV_AB.pt2.Y);
            PointF ptC = new PointF(xOffset - FOV_CD.pt1.X, yOffset - FOV_CD.pt1.Y);
            PointF ptD = new PointF(xOffset - FOV_CD.pt2.X, yOffset - FOV_CD.pt2.Y);

            g.DrawLine(Pens.Ivory, ptA, ptB);
            g.DrawLine(Pens.Ivory, ptC, ptD);
            g.DrawLine(Pens.Ivory, ptA, ptC);
            g.DrawLine(Pens.Ivory, ptB, ptD);

            //g.DrawLine(Pens.Ivory, fovNearX1 + 50, fovNearY1 + 50, fovFarX1 + 50, fovFarY1 + 50);
            //g.DrawLine(Pens.Ivory, fovNearX2 + 50, fovNearY2 + 50, fovFarX2 + 50, fovFarY2 + 50);
            //g.DrawLine(Pens.Ivory, fovNearX1 + 50, fovNearY1 + 50, fovNearX2 + 50, fovNearY2 + 50);
            //g.DrawLine(Pens.Ivory, fovFarX1 + 50, fovFarY1 + 50, fovFarX2 + 50, fovFarY2 + 50);

            //
            // Draw the perspective-transformed map
            //                

            //g = Graphics.FromImage(viewport[2]);
            //g.Clear(Color.Black);

            //for (int i = 0; i < map.Length - 1; i++)
            //{
            //    vx1 = map[i + 1].pt.X; vy1 = map[i + 1].pt.Y; vx2 = map[i].pt.X; vy2 = map[i].pt.Y;

            //    float tx1 = vx1 - px; float ty1 = vy1 - py;
            //    float tx2 = vx2 - px; float ty2 = vy2 - py;

            //    // Rotate them around the player's view
            //    float tz1 = (float)(tx1 * Math.Cos(angle) + ty1 * Math.Sin(angle));
            //    float tz2 = (float)(tx2 * Math.Cos(angle) + ty2 * Math.Sin(angle));
            //    tx1 = (float)(tx1 * Math.Sin(angle) - ty1 * Math.Cos(angle));
            //    tx2 = (float)(tx2 * Math.Sin(angle) - ty2 * Math.Cos(angle));

            //    // Clip to the view frustum
            //    //if (tz1 > 0 || tz2 > 0)
            //    {
            //        AB.pt1.X = tx1; AB.pt1.Y = tz1;
            //        AB.pt2.X = tx2; AB.pt2.Y = tz2;
            //        lir1 = Intersect2D(AB, FOV_AB);
            //        lir2 = Intersect2D(AB, FOV_CD);

            //        PointF nAB = Normalise(new PointF(-(AB.pt2.Y - AB.pt1.Y), (AB.pt2.X - AB.pt1.X)));
            //        float dp1 = DistanceToLine2D(FOV_AB, n1, AB.pt1);
            //        float dp2 = DistanceToLine2D(FOV_AB, n1, AB.pt2);
            //        float dp3 = DistanceToLine2D(FOV_CD, n2, AB.pt1);
            //        float dp4 = DistanceToLine2D(FOV_CD, n2, AB.pt2);

            //        PointF i1 = AB.pt1;
            //        PointF i2 = AB.pt2;
            //        if ((dp1 < 0 && dp2 < 0) || (dp3 < 0 && dp4 < 0))
            //            continue;

            //        if (lir1.resultType == LineIntersectionResultType.Intersecting)
            //        {
            //            if (dp1 < 0)
            //            {
            //                i2 = lir1.intersectionPoint;
            //            }
            //            else
            //            {
            //                i1 = lir1.intersectionPoint;
            //            }
            //        }

            //        if (lir2.resultType == LineIntersectionResultType.Intersecting)
            //        {
            //            if (dp1 < 0)
            //            {
            //                i1 = lir2.intersectionPoint;
            //            }
            //            else
            //            {
            //                i2 = lir2.intersectionPoint;
            //            }
            //        }

            //        tx1 = i1.X; tz1 = i1.Y; tx2 = i2.X; tz2 = i2.Y;

            //        //float focalLength = Math.Abs(fovNearY2);// + Math.Abs(fovNearY2);
            //        float x1 = tx1 * focalLength / tz1;
            //        float y1a = -map[i].height * focalLength / tz1; float y1b = map[i].height * focalLength / tz1;
            //        float x2 = tx2 * focalLength / tz2;
            //        float y2a = -map[i].height * focalLength / tz2; float y2b = map[i].height * focalLength / tz2;

            //        for (float x = x1; x <= x2; x++)
            //        {
            //            float ya = y1a + (x - x1) * (y2a - y1a) / (x2 - x1);
            //            float yb = y1b + (x - x1) * (y2b - y1b) / (x2 - x1);

            //            if (map[i].isPortal)
            //            {
            //                g.DrawLine(Pens.DarkGray, 50 + x, 0, 50 + x, 50 + -ya);     // Ceiling
            //                g.DrawLine(Pens.Blue, 50 + x, 50 + yb, 50 + x, 140);        // Floor
            //                g.DrawLine(Pens.Red, 50 + x, 50 + ya, 50 + x, 50 + yb);     // Portal
            //            }
            //            else
            //            {
            //                g.DrawLine(Pens.DarkGray, 50 + x, 0, 50 + x, 50 + -ya);     // Ceiling
            //                g.DrawLine(Pens.Blue, 50 + x, 50 + yb, 50 + x, 140);        // Floor

            //                g.DrawLine(map[i].pen, 50 + x, 50 + ya, 50 + x, 50 + yb);   // Wall
            //            }
            //        }

            //        g.DrawLine(map[i].pen, 50 + x1, 50 + y1a, 50 + x2, 50 + y2a);   // top (1-2 b)
            //        g.DrawLine(map[i].pen, 50 + x1, 50 + y1b, 50 + x2, 50 + y2b);   // bottom (1-2 b)
            //        g.DrawLine(Pens.Red, 50 + x1, 50 + y1a, 50 + x1, 50 + y1b);     // left (1)
            //        g.DrawLine(Pens.Red, 50 + x2, 50 + y2a, 50 + x2, 50 + y2b);     // right (2)
            //    }
            //}

            g = Graphics.FromImage(ScreenBuffer);
            g.DrawImage(viewport[0], 5, 5);
            g.DrawImage(viewport[1], 110, 5);
            g.DrawImage(viewport[2], 215, 5);
            g.DrawRectangle(Pens.Red, new Rectangle(5, 5, 100, 100));
            g.DrawRectangle(Pens.Red, new Rectangle(110, 5, 100, 100));
            g.DrawRectangle(Pens.Red, new Rectangle(215, 5, 100, 100));

            Text = "FOV Distance: " + Math.Abs(focalLength).ToString("0.00");

            Invalidate();
        }

        //
        // Vector Math Stuff
        //

        private PointF Rotate2D(PointF p, double angle)
        {
            float x = p.X;
            float y = p.Y;

            p.X = (float)((Math.Cos(angle) * x) - (Math.Sin(angle) * y));
            p.Y = (float)((Math.Sin(angle) * x) + (Math.Cos(angle) * y));

            return p;
        }

        private float DistanceToLine2D(Line ab, PointF n, PointF v)
        {
            return DotProduct2D(new PointF(v.X - ab.pt2.X, v.Y - ab.pt2.Y), n);
        }

        private PointF VectorAdd2D(PointF v, PointF w)
        {
            return new PointF(v.X + w.X, v.Y + w.Y);
        }

        private PointF VectorScale2D(PointF v, float scale)
        {
            return new PointF(v.X * scale, v.Y * scale);
        }

        private PointF Normalise(PointF v)
        {
            float length = (float)Math.Sqrt((v.X * v.X) + (v.Y * v.Y));
            return (new PointF(v.X / length, v.Y / length));
        }

        private float DotProduct2D(PointF v, PointF w)
        {
            return (v.X * w.X + v.Y * w.Y);
        }

        private float CrossProduct2D(PointF v, PointF w)
        {
            return (v.X * w.Y - v.Y * w.X);
        }

        private PointF? LineIntersection2D(Line ab, Line cd, PointF n)
        {
            PointF p0 = ab.pt1;
            PointF p1 = ab.pt2;
            PointF d = cd.pt2;

            PointF p0_minus_d = new PointF(p0.X - d.X, p0.Y - d.Y);
            PointF p1_minus_p0 = new PointF(p1.X - p0.X, p1.Y - p0.Y);
            float t = (DotProduct2D(n, p0_minus_d) / DotProduct2D(VectorScale2D(n, -1), p1_minus_p0));

            if (t > 0 && t < 1)
                return new PointF(p0.X + (p1_minus_p0.X * t), p0.Y + (p1_minus_p0.Y * t));
            else
                return null;
        }

        private LineIntersectionResult2D Intersect2D(Line ab, Line cd)
        {
            PointF p = ab.pt1; PointF r = new PointF(ab.pt2.X - p.X, ab.pt2.Y - p.Y);
            PointF q = cd.pt1; PointF s = new PointF(cd.pt2.X - q.X, cd.pt2.Y - q.Y);

            float r_cross_s = CrossProduct2D(r, s);
            PointF q_minus_p = new PointF(q.X - p.X, q.Y - p.Y);
            float q_minus_p_cross_r = CrossProduct2D(q_minus_p, r);
            float q_minus_p_cross_s = CrossProduct2D(q_minus_p, s);

            float t = q_minus_p_cross_s / r_cross_s;
            float u = q_minus_p_cross_r / r_cross_s;

            if (r_cross_s == 0 && q_minus_p_cross_r == 0)
                return new LineIntersectionResult2D(LineIntersectionResultType.Collinear, new PointF());
            else if (r_cross_s == 0 && q_minus_p_cross_r != 0)
                return new LineIntersectionResult2D(LineIntersectionResultType.Parallel, new PointF());
            else if (r_cross_s != 0 && (t >= 0 && t <= 1) && (u >= 0 && u <= 1))
                return new LineIntersectionResult2D(LineIntersectionResultType.Intersecting,
                    new PointF(p.X + t * r.X, p.Y + t * r.Y));
            else
                return new LineIntersectionResult2D(LineIntersectionResultType.NonIntersecting, new PointF());
        }

        private PointF GetLineSegmentCenter(Line ab)
        {
            return (new PointF((ab.pt1.X + ab.pt2.X) / 2, (ab.pt1.Y + ab.pt2.Y) / 2));
        }

        //
        // Event Handlers
        //

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

    //
    // Key Press Support
    //

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
