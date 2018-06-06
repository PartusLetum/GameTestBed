using System.Windows.Forms;
using System.Drawing;
using System;
using System.Text;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

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

        private float fovNearDistance = 1.0f;
        private float fovFarDistance = 100.0f;
        private double fovAngleHalved = Math.PI / 4;
        private float focalLength = 0.0f;

        private const double FIXED_PLAYER_ANGLE = (Math.PI / 2); // 90 degrees
        //private const double FIXED_PLAYER_ANGLE = 0;

        private struct mapvector { public PointF pt; public Pen pen; public float height; public bool isPortal; };
        private mapvector[] map;

        private PortalMap portalMap;

        //
        // Screen Support Objects
        //
        public Image ScreenBuffer { get; set; }

        //
        // Rendering support ...
        //
        public struct Point3d { public float x; public float y; public float z; };
        public struct Line3d { public Point3d pt1; public Point3d pt2; };

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

            fovNearDistance = 10.0f;
            fovFarDistance = 200.0f;

            viewport = new Bitmap[3]
            { 
                new Bitmap(100, 100, CreateGraphics()), 
                new Bitmap(100, 100, CreateGraphics()),
                new Bitmap(100, 100, CreateGraphics())
            };

            mapvector[] map1 = new mapvector[8]
            {
                new mapvector() { pt = new PointF(50, 90), pen = Pens.Green, height = 5 },
                new mapvector() { pt = new PointF(90, 70), pen = Pens.Red, height = 5 },
                new mapvector() { pt = new PointF(90, 60), pen = Pens.Blue, height = 5 },
                new mapvector() { pt = new PointF(50, 60), pen = Pens.Yellow, height = 5 },
                new mapvector() { pt = new PointF(50, 10), pen = Pens.Purple, height = 5 },
                new mapvector() { pt = new PointF(20, 30), pen = Pens.Orange, height = 5 },
                new mapvector() { pt = new PointF(30, 40), pen = Pens.Cyan, height = 5 },
                new mapvector() { pt = new PointF(50, 90), pen = Pens.Green, height = 5 }
            };

            mapvector[] map2 = new mapvector[6]
            {
                new mapvector() { pt = new PointF(70, 90), pen = Pens.Yellow, height = 5 },
                new mapvector() { pt = new PointF(70, 20), pen = Pens.Violet, height = 5 },
                new mapvector() { pt = new PointF(20, 30), pen = Pens.Green, height = 5 },
                new mapvector() { pt = new PointF(20, 60), pen = Pens.Purple, height = 5, isPortal = false },
                new mapvector() { pt = new PointF(40, 90), pen = Pens.Orange, height = 5 },
                new mapvector() { pt = new PointF(70, 90), pen = Pens.Ivory, height = 5 }
            };

            mapvector[] map3 = new mapvector[2]
            {
                new mapvector() { pt = new PointF(50, 50 + (50 / 2.0f)), pen = Pens.Yellow, height = 5.0f },
                new mapvector() { pt = new PointF(50, 50 - (50 / 2.0f)), pen = Pens.Yellow, height = 5.0f }
            };

            map = map1;

            portalMap = new PortalMap()
            {
                PlayerStartLocation = new PointF(0, 0),
                Sectors = new List<PortalMapSector>()
                {
                    new PortalMapSector()
                    {
                        LineSegments = new List<PortalMapLineSegment>()
                        {
                            new PortalMapLineSegment(new PointF(30, -30), new PointF(-30, -30)) { IsPortal = true },
                            new PortalMapLineSegment(new PointF(-30, -30), new PointF(-30, 30)),
                            new PortalMapLineSegment(new PointF(-30, 30), new PointF(30, 30)),
                            new PortalMapLineSegment(new PointF(30, 30), new PointF(30, -30))
                        }
                    },
                    new PortalMapSector()
                    {
                        LineSegments = new List<PortalMapLineSegment>()
                        {
                            new PortalMapLineSegment(new PointF(-30, -30), new PointF(30, -30)) { IsPortal = true },
                            new PortalMapLineSegment(new PointF(30, -30), new PointF(60, -60)),
                            new PortalMapLineSegment(new PointF(60, -60), new PointF(0, -60)),
                            new PortalMapLineSegment(new PointF(0, -60), new PointF(-30, -30))
                        }
                    }
                }
            };
            portalMap.PlayerSector = portalMap.Sectors[0];
            portalMap.Sectors[0].LineSegments[0].LinkedSector = portalMap.Sectors[1];
            portalMap.Sectors[1].LineSegments[0].LinkedSector = portalMap.Sectors[0];

            playerWorldLocation = portalMap.PlayerStartLocation;

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
                //fovFarDistance += 0.1f;
            if (keyStatus.FOVDown)
                fovAngleHalved -= 0.01f;
                //fovFarDistance -= 0.1f;

            if (fovAngleHalved < 0.1f) { fovAngleHalved = 0.1f; };
            if (fovAngleHalved > Math.PI / 3) { fovAngleHalved = Math.PI / 3; };

            // Canvas Size = tan(theta / 2) * Distance to Canvas
            focalLength = (50.0f) / (float)Math.Tan(fovAngleHalved);
            //fovFarDistance = focalLength;
            //fovFarDistance = 80.0f;

            if (playerWorldOrientation < 0)
                playerWorldOrientation = (2 * Math.PI) + playerWorldOrientation;
            else if (playerWorldOrientation > (2 * Math.PI))
                playerWorldOrientation = playerWorldOrientation - (2 * Math.PI);

            playerDirectionVector = Rotate2D(new PointF(1.0f, 0.0f), playerWorldOrientation);
            playerStrafeVector = Rotate2D(new PointF(0.0f, 1.0f), playerWorldOrientation);

            // Update player sector ...
            // Very brute force for now, just check for insidedness ...
            bool inSector = false;
            foreach(PortalMapSector sector in portalMap.Sectors)
            {
                inSector = true;
                foreach(PortalMapLineSegment edge in sector.LineSegments)
                {
                    if (DistanceToLine2D(edge.LineSegment, edge.Normal, playerWorldLocation) > 0) // !!! The normal is facing the wrong way !!!
                    {
                        inSector = false;
                        break;
                    }
                }
                if (inSector)
                {
                    portalMap.PlayerSector = sector;
                    break;
                }
            }

            //Console.WriteLine("{0}", playerWorldLocation.ToString());
            //Console.ReadKey();
        }

        private void DoRender()
        {
            Graphics g;

            g = Graphics.FromImage(ScreenBuffer);
            g.Clear(Color.Black);

            float xOffset = 50;
            float yOffset = 50;
            PointF screenOffset = new PointF(xOffset, yOffset);

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

            // Render stuff sorting and what not
            List<PortalMapLineSegment> sortedEdges = new List<PortalMapLineSegment>();

            PointF a, b;

            //
            // Draw the absolute map
            //

            g = Graphics.FromImage(viewport[0]);
            g.Clear(Color.Black);

            float length;

            length = (float)Math.Abs(fovNearDistance / Math.Cos(fovAngleHalved));
            float fovNearX1 = (float)(Math.Cos(angle - fovAngleHalved) * length);
            float fovNearY1 = (float)(Math.Sin(angle - fovAngleHalved) * length);
            float fovNearX2 = (float)(Math.Cos(angle + fovAngleHalved) * length);
            float fovNearY2 = (float)(Math.Sin(angle + fovAngleHalved) * length);

            length = (float)Math.Abs(fovFarDistance / Math.Cos(fovAngleHalved));
            float fovFarX1 = (float)(Math.Cos(angle - fovAngleHalved) * length);
            float fovFarY1 = (float)(Math.Sin(angle - fovAngleHalved) * length);
            float fovFarX2 = (float)(Math.Cos(angle + fovAngleHalved) * length);
            float fovFarY2 = (float)(Math.Sin(angle + fovAngleHalved) * length);

            FOV_AB.pt1.X = fovNearX1 + px; FOV_AB.pt1.Y = fovNearY1 + py;
            FOV_AB.pt2.X = fovFarX1 + px; FOV_AB.pt2.Y = fovFarY1 + py;
            FOV_CD.pt1.X = fovNearX2 + px; FOV_CD.pt1.Y = fovNearY2 + py;
            FOV_CD.pt2.X = fovFarX2 + px; FOV_CD.pt2.Y = fovFarY2 + py;

            PointF n1 = Normalise(new PointF(-(FOV_AB.pt2.Y - FOV_AB.pt1.Y), (FOV_AB.pt2.X - FOV_AB.pt1.X)));
            PointF n2 = Normalise(new PointF((FOV_CD.pt2.Y - FOV_CD.pt1.Y), -(FOV_CD.pt2.X - FOV_CD.pt1.X)));
            PointF n3 = Normalise(new PointF(-(FOV_AB.pt1.Y - FOV_CD.pt1.Y), (FOV_AB.pt1.X - FOV_CD.pt1.X)));
            PointF n4 = Normalise(new PointF((FOV_AB.pt2.Y - FOV_CD.pt2.Y), -(FOV_AB.pt2.X - FOV_CD.pt2.X)));

            for (int i = 0; i < map.Length - 1; i ++)
            {
                vx1 = map[i].pt.X; vy1 = map[i].pt.Y; vx2 = map[i + 1].pt.X; vy2 = map[i + 1].pt.Y;

                AB.pt1.X = vx1; AB.pt1.Y = vy1; AB.pt2.X = vx2; AB.pt2.Y = vy2;
                //Line? t = LineIntersection2D(AB, FOV_AB, n1);
                //t = LineIntersection2D(t, FOV_CD, n2);
                //t = LineIntersection2D(t, new Line() { pt1 = FOV_AB.pt1, pt2 = FOV_CD.pt1 }, n3);
                //t = LineIntersection2D(t, new Line() { pt1 = FOV_AB.pt2, pt2 = FOV_CD.pt2 }, n4);

                Line[] hull = 
                {
                    FOV_AB, new Line() { pt1 = FOV_AB.pt1, pt2 = FOV_CD.pt1 },
                    FOV_CD, new Line() { pt1 = FOV_AB.pt2, pt2 = FOV_CD.pt2 }
                };
                PointF[] normals = { n1, n3, n2, n4 };

                Line? t = LineIntersection2D(AB, hull, normals, 4);

                PointF nAB = Normalise(new PointF(-(AB.pt2.Y - AB.pt1.Y), (AB.pt2.X - AB.pt1.X)));

#if DEBUG
                a = VectorAdd2D(n1, GetLineSegmentCenter(FOV_AB));
                b = VectorAdd2D(VectorScale2D(n1, 5), a);

                g.DrawLine(Pens.Green, a, b);

                a = VectorAdd2D(n2, GetLineSegmentCenter(FOV_CD));
                b = VectorAdd2D(VectorScale2D(n2, 5), a);

                g.DrawLine(Pens.Red, a, b);

                a = VectorAdd2D(n3, GetLineSegmentCenter(new Line() { pt1 = FOV_AB.pt1, pt2 = FOV_CD.pt1 }));
                b = VectorAdd2D(VectorScale2D(n3, 5), a);

                g.DrawLine(Pens.Blue, a, b);

                a = VectorAdd2D(n4, GetLineSegmentCenter(new Line() { pt1 = FOV_AB.pt2, pt2 = FOV_CD.pt2 }));
                b = VectorAdd2D(VectorScale2D(n4, 5), a);

                g.DrawLine(Pens.Purple, a, b);

                a = VectorAdd2D(nAB, GetLineSegmentCenter(AB));
                b = VectorAdd2D(VectorScale2D(nAB, 5), a);

                g.DrawLine(map[i].pen, a, b);
#endif
                PointF i1 = t == null ? new Point() : (PointF)t?.pt1;
                PointF i2 = t == null ? new Point() : (PointF)t?.pt2;

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

            length = (float)Math.Abs(fovNearDistance / Math.Cos(fovAngleHalved));
            fovNearX1 = (float)(Math.Cos(FIXED_PLAYER_ANGLE - fovAngleHalved) * length);
            fovNearY1 = (float)(Math.Sin(FIXED_PLAYER_ANGLE - fovAngleHalved) * length);
            fovNearX2 = (float)(Math.Cos(FIXED_PLAYER_ANGLE + fovAngleHalved) * length);
            fovNearY2 = (float)(Math.Sin(FIXED_PLAYER_ANGLE + fovAngleHalved) * length);

            length = (float)Math.Abs(fovFarDistance / Math.Cos(fovAngleHalved));
            fovFarX1 = (float)(Math.Cos(FIXED_PLAYER_ANGLE - fovAngleHalved) * length);
            fovFarY1 = (float)(Math.Sin(FIXED_PLAYER_ANGLE - fovAngleHalved) * length);
            fovFarX2 = (float)(Math.Cos(FIXED_PLAYER_ANGLE + fovAngleHalved) * length);
            fovFarY2 = (float)(Math.Sin(FIXED_PLAYER_ANGLE + fovAngleHalved) * length);

            FOV_AB.pt1.X = fovNearX1; FOV_AB.pt1.Y = fovNearY1;
            FOV_AB.pt2.X = fovFarX1; FOV_AB.pt2.Y = fovFarY1;
            FOV_CD.pt1.X = fovNearX2; FOV_CD.pt1.Y = fovNearY2;
            FOV_CD.pt2.X = fovFarX2; FOV_CD.pt2.Y = fovFarY2;

            n1 = Normalise(new PointF(-(FOV_AB.pt2.Y - FOV_AB.pt1.Y), (FOV_AB.pt2.X - FOV_AB.pt1.X)));
            n2 = Normalise(new PointF((FOV_CD.pt2.Y - FOV_CD.pt1.Y), -(FOV_CD.pt2.X - FOV_CD.pt1.X)));
            n3 = Normalise(new PointF(-(FOV_AB.pt1.Y - FOV_CD.pt1.Y), (FOV_AB.pt1.X - FOV_CD.pt1.X)));
            n4 = Normalise(new PointF((FOV_AB.pt2.Y - FOV_CD.pt2.Y), -(FOV_AB.pt2.X - FOV_CD.pt2.X)));

            // Render Portal Map ...
            //RenderPortalSector(g3, playerWorldLocation, (float)playerWorldOrientation, FOV_AB, FOV_CD, n1, n2, n3, n4, screenOffset, portalMap.Sectors[1]);
            RenderPortalSector(Graphics.FromImage(viewport[2]), playerWorldLocation, (float)playerWorldOrientation, FOV_AB, FOV_CD, n1, n2, n3, n4, screenOffset, portalMap.PlayerSector);

            // Draw the Portal Map in top down 2D ...
            RenderPortalSector(Graphics.FromImage(viewport[1]), playerWorldLocation, (float)playerWorldOrientation, FOV_AB, FOV_CD, n1, n2, n3, n4, screenOffset, portalMap.PlayerSector, null, true);

            // Render player and frustum ...

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

            // Display all the viewports ...

            g = Graphics.FromImage(ScreenBuffer);
            g.DrawImage(viewport[0], 5, 5);
            g.DrawImage(viewport[1], 110, 5);
            g.DrawImage(viewport[2], 215, 5);
            g.DrawRectangle(Pens.Red, new Rectangle(5, 5, 100, 100));
            g.DrawRectangle(Pens.Red, new Rectangle(110, 5, 100, 100));
            g.DrawRectangle(Pens.Red, new Rectangle(215, 5, 100, 100));

            Text = "FOV Distance: " + Math.Abs(focalLength).ToString("0.00");

            //Invalidate();
            this.CreateGraphics().DrawImage(ScreenBuffer, 0, 0, ClientSize.Width, ClientSize.Height);
            //Refresh();
        }

        //
        // Recursive Portal Render stuff
        //
        public void RenderPortalSector(Graphics g, PointF viewLocation, float viewAngle, Line FOV_AB, Line FOV_CD, PointF n1, PointF n2, PointF n3, PointF n4, PointF screenOffset, PortalMapSector sector, PortalMapSector lastSector = null, bool topDown = false)
        {
            float px = viewLocation.X;
            float py = viewLocation.Y;

            foreach (PortalMapLineSegment edge in sector.LineSegments)
            {
                float vx1 = edge.LineSegment.pt1.X;
                float vy1 = edge.LineSegment.pt1.Y;
                float vx2 = edge.LineSegment.pt2.X;
                float vy2 = edge.LineSegment.pt2.Y;

                float tx1 = vx1 - px; float ty1 = vy1 - py;
                float tx2 = vx2 - px; float ty2 = vy2 - py;
                float angle = viewAngle;

                // Rotate them around the player's view
                float tz1 = (float)(tx1 * Math.Cos(angle) + ty1 * Math.Sin(angle));
                float tz2 = (float)(tx2 * Math.Cos(angle) + ty2 * Math.Sin(angle));
                tx1 = (float)(tx1 * Math.Sin(angle) - ty1 * Math.Cos(angle));
                tx2 = (float)(tx2 * Math.Sin(angle) - ty2 * Math.Cos(angle));

                Line AB = new Line();
                AB.pt1.X = tx1; AB.pt1.Y = tz1;
                AB.pt2.X = tx2; AB.pt2.Y = tz2;

                Line[] hull = { FOV_AB, new Line() { pt1 = FOV_AB.pt1, pt2 = FOV_CD.pt1 },
                                        FOV_CD, new Line() { pt1 = FOV_AB.pt2, pt2 = FOV_CD.pt2 } };
                PointF[] normals = { n1, n3, n2, n4 };

                Line? t = LineIntersection2D(AB, hull, normals, 4);

                PointF nAB = Normalise(new PointF((AB.pt2.Y - AB.pt1.Y), -(AB.pt2.X - AB.pt1.X)));

#if DEBUG
                PointF a, b = new PointF();
                a = VectorAdd2D(n1, GetLineSegmentCenter(FOV_AB));
                b = VectorAdd2D(VectorScale2D(n1, 5), a);

                g.DrawLine(Pens.Green, VectorSubtract2D(screenOffset, a), VectorSubtract2D(screenOffset, b));

                a = VectorAdd2D(n2, GetLineSegmentCenter(FOV_CD));
                b = VectorAdd2D(VectorScale2D(n2, 5), a);

                g.DrawLine(Pens.Red, VectorSubtract2D(screenOffset, a), VectorSubtract2D(screenOffset, b));

                a = VectorAdd2D(nAB, GetLineSegmentCenter(AB));
                b = VectorAdd2D(VectorScale2D(nAB, 5), a);

                g.DrawLine(new Pen(edge.LineColor), VectorSubtract2D(screenOffset, a), VectorSubtract2D(screenOffset, b));
#endif
                if (t != null)
                {
                    if (edge.IsPortal && edge.LinkedSector != lastSector) // Need to do view frustm culling first ....
                    {
                        // Should be adjusting frustum to portal, this could still lead to draw artifacts ... overlaps and such ...
                        RenderPortalSector(g, viewLocation, viewAngle, FOV_AB, FOV_CD, n1, n2, n3, n4, screenOffset, edge.LinkedSector, sector, topDown);
                    }

                    PointF i1 = t == null ? new Point() : (PointF)t?.pt1;
                    PointF i2 = t == null ? new Point() : (PointF)t?.pt2;

                    // Draw Wall
                    g.DrawLine(Pens.White, screenOffset.X - tx1, screenOffset.Y - tz1, screenOffset.X - tx2, screenOffset.Y - tz2);
                    //g.DrawLine(Pens.White, tx1, tz1, tx2, tz2);

                    // Draw Intsection
                    //i1.X = xOffset - i1.X; i1.Y = yOffset - i1.Y; i2.X = xOffset - i2.X; i2.Y = yOffset - i2.Y;
                    //g.DrawLine(map[i].pen, AB.pt1, AB.pt2);

                    // Do 3D (2.5D) render now ...
                    if (!topDown)
                    {

                        tx1 = i1.X; tz1 = i1.Y; tx2 = i2.X; tz2 = i2.Y;

                        // Render ...
                        float height = 5.0f;

                        //float focalLength = Math.Abs(fovNearY2);// + Math.Abs(fovNearY2);
                        float x1 = tx1 * focalLength / tz1;
                        float y1a = -height * focalLength / tz1; float y1b = height * focalLength / tz1;
                        float x2 = tx2 * focalLength / tz2;
                        float y2a = -height * focalLength / tz2; float y2b = height * focalLength / tz2;

                        for (float x = x1; x <= x2; x++)
                        {
                            float ya = y1a + (x - x1) * (y2a - y1a) / (x2 - x1);
                            float yb = y1b + (x - x1) * (y2b - y1b) / (x2 - x1);

                            float zDepth = (tz1 + (x - x1) * (tz2 - tz1) / (x2 - x1));
                            Color c;
                            if (!float.IsNaN(zDepth)) // Ah ... basically an edge with a delta x of 0 !! !! Bug! Crashing on Paralell to viewport ??? Also slow in this case ...
                            {
                                c = Color.FromArgb(
                                    (1 - zDepth > 1) ? edge.LineColor.R : (int)((1 - (zDepth / 100)) * edge.LineColor.R),
                                    (1 - zDepth > 1) ? edge.LineColor.G : (int)((1 - (zDepth / 100)) * edge.LineColor.G),
                                    (1 - zDepth > 1) ? edge.LineColor.B : (int)((1 - (zDepth / 100)) * edge.LineColor.B));
                            }
                            else
                            {
                                c = Color.Purple;
                            }
                            Pen p = new Pen(c);
                            //Pen p = map[i].pen;

                            bool drawFloorCeiling = true;
                            bool drawPortals = false;
                            if (edge.IsPortal && drawPortals)
                            {
                                Pen gp1 = new Pen(new LinearGradientBrush(new Point(0, 50), new Point(0, 0),
                                    Color.Black, Color.DarkGray));
                                Pen gp2 = new Pen(new LinearGradientBrush(new Point(0, 0), new Point(0, 50),
                                    Color.Black, Color.Blue));
                                if (drawFloorCeiling)
                                {
                                    g.DrawLine(gp1, 50 - x, 0, 50 - x, 50 + -ya);              // Ceiling
                                    g.DrawLine(gp2, 50 - x, 50 + yb, 50 - x, 140);             // Floor
                                }

                                g.DrawLine(Pens.Red, 50 - x, 50 + ya, 50 - x, 50 + yb);   // Wall
                            }
                            else if (!edge.IsPortal)
                            {
                                Pen gp1 = new Pen(new LinearGradientBrush(new Point(0, 50), new Point(0, 0),
                                    Color.Black, Color.DarkGray));
                                Pen gp2 = new Pen(new LinearGradientBrush(new Point(0, 0), new Point(0, 50),
                                    Color.Black, Color.Blue));
                                if (drawFloorCeiling)
                                {
                                    g.DrawLine(gp1, 50 - x, 0, 50 - x, 50 + -ya);              // Ceiling
                                    g.DrawLine(gp2, 50 - x, 50 + yb, 50 - x, 140);             // Floor
                                }

                                g.DrawLine(p, 50 - x, 50 + ya, 50 - x, 50 + yb);   // Wall

                                //Graphics.FromImage(ScreenBuffer).DrawImage(viewport[2], 215, 5);
                                //this.CreateGraphics().DrawImage(ScreenBuffer, 0, 0, ClientSize.Width, ClientSize.Height);
                            }
                        }

                        g.DrawLine(new Pen(edge.LineColor), screenOffset.X - x1, screenOffset.Y + y1a, screenOffset.X - x2, screenOffset.Y + y2a);   // top (1-2 b)
                        g.DrawLine(new Pen(edge.LineColor), screenOffset.X - x1, screenOffset.Y + y1b, screenOffset.X - x2, screenOffset.Y + y2b);   // bottom (1-2 b)
                        g.DrawLine(Pens.Red, screenOffset.X - x1, screenOffset.Y + y1a, screenOffset.X - x1, screenOffset.Y + y1b);     // left (1)
                        g.DrawLine(Pens.Red, screenOffset.X - x2, screenOffset.Y + y2a, screenOffset.X - x2, screenOffset.Y + y2b);     // right (2)                
                    }

                    // Draw top down map ...
                    // Cool to draw this on top of 3d view !
                    if (topDown)
                    {
                        // Draw unclipped ...
                        g.DrawLine(Pens.Red, screenOffset.X - tx1, screenOffset.Y - tz1, screenOffset.X - tx2, screenOffset.Y - tz2);
                        // Draw clipped ...
                        PointF sA = VectorSubtract2D(screenOffset, i1);
                        PointF sB = VectorSubtract2D(screenOffset, i2);
                        g.DrawLine(new Pen(edge.LineColor), sA, sB);
                    }
                }
            }

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

        private PointF VectorSubtract2D(PointF v, PointF w)
        {
            return new PointF(v.X - w.X, v.Y - w.Y);
        }

        private PointF VectorScale2D(PointF v, float scale)
        {
            return new PointF(v.X * scale, v.Y * scale);
        }

        static public PointF Normalise(PointF v)
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

        private Line LineIntersection2D(Line ab, Line cd, PointF n)
        {
            PointF p0 = ab.pt1;
            PointF p1 = ab.pt2;
            PointF p = cd.pt2;

            PointF p0_minus_p = new PointF(p0.X - p.X, p0.Y - p.Y);
            PointF p1_minus_p0 = new PointF(p1.X - p0.X, p1.Y - p0.Y);
            float t = (DotProduct2D(n, p0_minus_p) / DotProduct2D(VectorScale2D(n, -1), p1_minus_p0));

            if (t > 0 && t < 1)
            {

                if (DotProduct2D(n, p0_minus_p) < 0)
                    return new Line() { pt1 = new PointF(p0.X + (p1_minus_p0.X * t), p0.Y + (p1_minus_p0.Y * t)), pt2 = ab.pt2 };
                else
                    return new Line() { pt1 = ab.pt1, pt2 = new PointF(p0.X + (p1_minus_p0.X * t), p0.Y + (p1_minus_p0.Y * t)) };
            }
            else
            {
                return ab;
            }
        }

        private Line? LineIntersection2D(Line ab, Line[] hull, PointF[] n, int hullSize)
        {
            float[] d = new float[hullSize];
            float[] t = new float[hullSize];
            float[] a_minus_p_dot_n = new float[hullSize];
            float[] b_minus_p_dot_n = new float[hullSize];

            float tE = 0; float tL = 1;

            PointF p1_minus_p0;

            // Do trivial reject and precalc dots ...
            for (int i = 0; i < hullSize; i ++)
            {
                a_minus_p_dot_n[i] = DotProduct2D(VectorSubtract2D(ab.pt1, hull[i].pt2), n[i]);
                b_minus_p_dot_n[i] = DotProduct2D(VectorSubtract2D(ab.pt2, hull[i].pt2), n[i]);

                if (a_minus_p_dot_n[i] < 0 && b_minus_p_dot_n[i] < 0)
                {
                    return null;
                }
            }

            for (int i = 0; i < hullSize; i ++)
            {
                PointF p0 = ab.pt1;
                PointF p1 = ab.pt2;
                PointF p = hull[i].pt2;

                PointF p0_minus_p = new PointF(p0.X - p.X, p0.Y - p.Y);
                p1_minus_p0 = new PointF(p1.X - p0.X, p1.Y - p0.Y);
                t[i] = (DotProduct2D(n[i], p0_minus_p) / DotProduct2D(VectorScale2D(n[i], -1), p1_minus_p0));

                d[i] = DotProduct2D(n[i], new PointF(p1.X - p0.X, p1.Y - p0.Y));

                if (t[i] > 0 && t[i] < 1)
                {
                    if (d[i] < 0)
                        tL = Math.Min(tL, t[i]);
                    else
                        tE = Math.Max(tE, t[i]);
                }
            }

            p1_minus_p0 = new PointF(ab.pt2.X - ab.pt1.X, ab.pt2.Y - ab.pt1.Y);
            return new Line()
            {
                pt1 = new PointF(ab.pt1.X + (p1_minus_p0.X * tE), ab.pt1.Y + (p1_minus_p0.Y * tE)),
                pt2 = new PointF(ab.pt1.X + (p1_minus_p0.X * tL), ab.pt1.Y + (p1_minus_p0.Y * tL))
            };
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

        static public PointF Normal(Line AB)
        {
            return Normalise(new PointF(-(AB.pt2.Y - AB.pt1.Y), (AB.pt2.X - AB.pt1.X)));
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

    //
    // Portal Map Support
    //

    public class PortalMap
    {
        public List<PortalMapSector> Sectors { get; set; }
        public PointF PlayerStartLocation { get; set; }
        public PortalMapSector PlayerSector { get; set; }
    }

    public class PortalMapSector
    {
        public List<PortalMapLineSegment> LineSegments { get; set; }
        public float Height { get; set; } = 5.0f;
    }

    public class PortalMapLineSegment
    {
        public Line LineSegment { get; set; }
        public Color LineColor { get; set; } = Color.LightGray;
        public bool IsPortal { get { return isPortal; } set { isPortal = value;  if(value == true) LineColor = Color.DarkRed; } }
        public PointF Normal { get; private set; }
        public PortalMapSector LinkedSector { get; set; }

        private bool isPortal = false;

        public PortalMapLineSegment(PointF a, PointF b)
        {
            LineSegment = new Line() { pt1 = a, pt2 = b };
            Normal = GameForm.Normal(LineSegment);
            LinkedSector = null;
        }

        public PortalMapLineSegment(PointF a, PointF b, Color c, bool isPortal = false, PortalMapSector linkedSector = null)
        {
            LineSegment = new Line() { pt1 = a, pt2 = b };
            Normal = GameForm.Normal(LineSegment);
            LineColor = c;
            IsPortal = isPortal;
            LinkedSector = linkedSector;
        }
    }
}
