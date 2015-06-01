using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace LR3 {
    public partial class LR : Form {

        protected const int ROOM_LENGTH = 30;
        protected const int ROOM_WIDTH = 30;
        protected const int ROOM_HEIGHT = 20;

        protected const int SEGMENTS_PER_PORTION = 20;

        protected bool isSceneLoaded = false;
       
        protected double Lambda = 0.5;

        protected double[,] r1x;
        protected double[,] r1y;
        protected double[,] r1z;
        protected double[,] r2x;
        protected double[,] r2y;
        protected double[,] r2z;

        protected Vector3d[,] portion1;
        protected Vector3d[,] portion2;

        protected static double[,] M = new double[4, 4] {
            { 1,  0,  0,  0},
            {-3,  3,  0,  0},
            { 3, -6,  3,  0},
            {-1,  3, -3,  1}
        };

        protected static double[,] MT = new double[4, 4] {
            { 1, -3,  3, -1},
            { 0,  3, -6,  3},
            { 0,  0,  3, -3},
            { 0,  0,  0,  1}
        };


        private double prevX = 0, prevY = 0;
        private double vAngle = 60;
        private double hAngle = 60;
        private double distance = 100;
        private double hAngleSpeed = 0.3;
        private double vAngleSpeed = 0.3;
        private double zoomSpeed = 10;
        protected bool IsMeshShown = false;

        public LR() {
            InitializeComponent();

            scene.MouseMove += scene_MouseMove;
            scene.KeyDown += scene_KeyDown;
            scene.MouseWheel += scene_MouseWheel;

            r1x = new double[4, 4];
            r1y = new double[4, 4];
            r1z = new double[4, 4];
            r2x = new double[4, 4];
            r2y = new double[4, 4];
            r2z = new double[4, 4];

            GenerateRandomPoints();
            NormalizeSecondPortion();
            ShowValues();
            CalcPortionsPoints();
        }




        #region Scene initialization

        private void scene_Load(object sender, EventArgs e) {
            isSceneLoaded = true;

            GL.ClearColor(scene.BackColor);
            GL.Enable(EnableCap.DepthTest);
            GL.ShadeModel(ShadingModel.Smooth);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            
            SetLighting();
            SetViewport();

            scene.Invalidate();
        }

        private void scene_Resize(object sender, EventArgs e) {
            if (!isSceneLoaded) {
                return;
            }
            SetViewport();
            scene.Invalidate();
        }

        private void SetViewport() {
            Matrix4 p = Matrix4.CreatePerspectiveFieldOfView((float)(90 * Math.PI / 180), scene.Width / scene.Height, 10, 1000);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref p);

            GL.Viewport(0, 0, scene.Width, scene.Height);
        }

        private void SetLighting() {
            float[] globalEmbient = { 0.2f, 0.2f, 0.2f, 1.0f };

            GL.Enable(EnableCap.Lighting);
            GL.LightModel(LightModelParameter.LightModelAmbient, globalEmbient);
            
            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);

            GL.Enable(EnableCap.Light0);
            
            float[] position = { 20.0f, 10.0f, 20.0f,  1.0f };
            float[] ambient  = { 0.01f, 0.01f, 0.01f,  1.0f };
            float[] diffuse  = {  0.7f,  0.7f,  0.7f,  1.0f };
            float[] specular = {  0.8f,  0.8f,  0.8f,  1.0f };
            float[] specrefl = {  0.8f,  0.8f,  0.8f,  1.0f };

            GL.Light(LightName.Light0, LightParameter.Position, position);
            GL.Light(LightName.Light0, LightParameter.Ambient, ambient);
            GL.Light(LightName.Light0, LightParameter.Diffuse, diffuse);
            GL.Light(LightName.Light0, LightParameter.Specular, specular);
            
            //GL.Light(LightName.Light0, LightParameter.ConstantAttenuation, 0);
            //GL.Light(LightName.Light0, LightParameter.LinearAttenuation, 0.01f);
            //GL.Light(LightName.Light0, LightParameter.QuadraticAttenuation, 0.000005f);
            
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, specrefl);
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, 64);
        }

        private void scene_Paint(object sender, PaintEventArgs e) {
            if (!isSceneLoaded) {
                return;
            }
            scene.MakeCurrent();

            GL.PushMatrix();
            GL.Enable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.Blend);

            SetPointOfView();
            RenderScene();
            scene.SwapBuffers();

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.AlphaTest);
            GL.PopMatrix();
        }

        private void SetPointOfView() {
            double vA = vAngle * Math.PI / 180;
            double hA = hAngle * Math.PI / 180;

            double x = distance * Math.Sin(vA) * Math.Cos(hA);
            double y = distance * Math.Cos(vA);
            double z = distance * Math.Sin(vA) * Math.Sin(hA);

            Matrix4 modelview = Matrix4.LookAt((float)x, (float)y, (float)z, 0, 0, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);

        }

        #endregion




        #region Calculations

        private double GetRandomShift(Random randomizer, double maxShift) {
            return randomizer.NextDouble() * maxShift * 2 - maxShift;
        }

        private void GenerateRandomPoints() {
            Random r = new Random();

            double step = 5;
            double maxShift = 2;

            for (int u = 0; u < 4; u++) {
                for (int v = 0; v < 4; v++) {
                    r1x[u, v] = -3.0 * step + u * step + GetRandomShift(r, maxShift);
                    r1z[u, v] = -1.5 * step + v * step + GetRandomShift(r, maxShift);
                    r1y[u, v] =                          GetRandomShift(r, maxShift * 5);

                    r2x[u, v] =               u * step + GetRandomShift(r, maxShift);
                    r2z[u, v] = -1.5 * step + v * step + GetRandomShift(r, maxShift);
                    r2y[u, v] =                          GetRandomShift(r, maxShift * 5);
                }
            }
        }

        private void NormalizeSecondPortion() {
            for (int i = 0; i < 4; i++) {
                r2x[0, i] = r1x[3, i];
                r2y[0, i] = r1y[3, i];
                r2z[0, i] = r1z[3, i];

                r2x[1, i] = Lambda * (r1x[3, i] - r1x[2, i]) + r2x[0, i];
                r2y[1, i] = Lambda * (r1y[3, i] - r1y[2, i]) + r2y[0, i];
                r2z[1, i] = Lambda * (r1z[3, i] - r1z[2, i]) + r2z[0, i];
            }
        }

        private double CalcFunction(double U, double V, double[,] B) {
            double[,] Us = new double[1, 4] { { 1, U, U * U, U * U * U } };
            double[,] VT = new double[4, 1] { { 1 }, { V }, { V * V }, { V * V * V } };

            double[,] step1 = MatrixHelper.Mult(Us, M);
            double[,] step2 = MatrixHelper.Mult(step1, B);
            double[,] step3 = MatrixHelper.Mult(step2, MT);
            double[,] step4 = MatrixHelper.Mult(step3, VT);

            return step4[0, 0];
        }

        private void CalcPortionsPoints() {
            int points = SEGMENTS_PER_PORTION + 1;
            double step = 1.0d / (double)SEGMENTS_PER_PORTION;

            portion1 = new Vector3d[points, points];
            portion2 = new Vector3d[points, points];

            double x, y, z;

            for (int u = 0; u < points; u++) {
                for (int v = 0; v < points; v++) {
                    x = CalcFunction(step * (double)u, step * (double)v, r1x);
                    y = CalcFunction(step * (double)u, step * (double)v, r1y);
                    z = CalcFunction(step * (double)u, step * (double)v, r1z);
                    portion1[u, v] = new Vector3d(x, y, z);

                    x = CalcFunction(step * (double)u, step * (double)v, r2x);
                    y = CalcFunction(step * (double)u, step * (double)v, r2y);
                    z = CalcFunction(step * (double)u, step * (double)v, r2z);
                    portion2[u, v] = new Vector3d(x, y, z);
                }
            }
        }

        private Vector3d CalcNormal(Vector3d v1, Vector3d v2, Vector3d v3) {
            Vector3d dir = Vector3d.Cross(v2 - v1, v3 - v1);
            return Vector3d.Normalize(dir);
        }

        #endregion




        #region Visualization

        private void RenderCone() {
            int segments = 10;      // Higher numbers improve quality 
            double radius = 0.5d;
            int height = 5;

            List<Vector3d> vertices = new List<Vector3d>();
            for (double i = 0; i < segments; i++) {
                double theta = (i / (segments - 1)) * 2 * Math.PI;
                vertices.Add(new Vector3d() {
                    X = (double)(radius * Math.Cos(theta)),
                    Y = (double)(-height),
                    Z = (double)(radius * Math.Sin(theta)),
                });
            }

            Vector3d top = new Vector3d(0, 0, 0);
            GL.Begin(PrimitiveType.TriangleFan);
                GL.Vertex3(top);
                for (int i = 0; i < segments; i++) {
                    GL.Vertex3(vertices[i]);
                }
            GL.End();
        }

        private void RenderWall() {
            Vector3d normal = CalcNormal(
                new Vector3d( ROOM_LENGTH, -ROOM_HEIGHT, ROOM_WIDTH),
                new Vector3d(ROOM_LENGTH, -ROOM_HEIGHT, -ROOM_WIDTH),
                new Vector3d(-ROOM_LENGTH, -ROOM_HEIGHT, -ROOM_WIDTH)
            );

            GL.Color3(Color.White);
            GL.Begin(PrimitiveType.Quads);
                GL.Normal3(normal);

                GL.Vertex3( ROOM_LENGTH, -ROOM_HEIGHT,  ROOM_WIDTH);
                GL.Vertex3( ROOM_LENGTH, -ROOM_HEIGHT, -ROOM_WIDTH);
                GL.Vertex3(-ROOM_LENGTH, -ROOM_HEIGHT, -ROOM_WIDTH);
                GL.Vertex3(-ROOM_LENGTH, -ROOM_HEIGHT,  ROOM_WIDTH);
            GL.End();
        }

        private void RenderMesh() {
            GL.LineWidth(1f);
            float transp = 0.1f;
            float major = 1.0f;
            float minor = 0.3f;
            int step = 5;

            GL.Begin(PrimitiveType.Lines);
                #region XY
                GL.Color4(major, minor, minor, transp);
                for (int i = 0; i <= ROOM_LENGTH; i += step) {
                    GL.Vertex3( i, -ROOM_HEIGHT, 0);
                    GL.Vertex3( i,  ROOM_HEIGHT, 0);
                    GL.Vertex3(-i, -ROOM_HEIGHT, 0);
                    GL.Vertex3(-i,  ROOM_HEIGHT, 0);
                }
                for (int i = 0; i <= ROOM_HEIGHT; i += step) {
                    GL.Vertex3(-ROOM_LENGTH,  i, 0);
                    GL.Vertex3( ROOM_LENGTH,  i, 0);
                    GL.Vertex3(-ROOM_LENGTH, -i, 0);
                    GL.Vertex3( ROOM_LENGTH, -i, 0);
                }
                #endregion

                #region XZ
                GL.Color4(minor, minor, major, transp);
                for (int i = 0; i <= ROOM_LENGTH; i += step) {
                    GL.Vertex3( i, 0, -ROOM_WIDTH);
                    GL.Vertex3( i, 0,  ROOM_WIDTH);
                    GL.Vertex3(-i, 0, -ROOM_WIDTH);
                    GL.Vertex3(-i, 0,  ROOM_WIDTH);
                }
                for (int i = 0; i <= ROOM_WIDTH; i += step) {
                    GL.Vertex3(-ROOM_LENGTH, 0,  i);
                    GL.Vertex3( ROOM_LENGTH, 0,  i);
                    GL.Vertex3(-ROOM_LENGTH, 0, -i);
                    GL.Vertex3( ROOM_LENGTH, 0, -i);
                }
                #endregion

                #region YZ
                GL.Color4(minor, major, minor, transp);
                for (int i = 0; i <= ROOM_HEIGHT; i += step) {
                    GL.Vertex3(0,  i, -ROOM_WIDTH);
                    GL.Vertex3(0,  i,  ROOM_WIDTH);
                    GL.Vertex3(0, -i, -ROOM_WIDTH);
                    GL.Vertex3(0, -i,  ROOM_WIDTH);
                }
                for (int i = 0; i <= ROOM_WIDTH; i += step) {
                    GL.Vertex3(0, -ROOM_HEIGHT,  i);
                    GL.Vertex3(0,  ROOM_HEIGHT,  i);
                    GL.Vertex3(0, -ROOM_HEIGHT, -i);
                    GL.Vertex3(0,  ROOM_HEIGHT, -i);
                }
                #endregion
            GL.End();
        }

        private void RenderAxis() {
            GL.Disable(EnableCap.Lighting);

            GL.LineWidth(1);

            GL.Color3(Color.Red);
            GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(-ROOM_LENGTH, 0, 0);
                GL.Vertex3( ROOM_LENGTH, 0, 0);
            GL.End();
            GL.PushMatrix();
                GL.Translate(-ROOM_LENGTH, 0, 0);
                GL.Rotate(90, 0, 0, 1);
                RenderCone();
            GL.PopMatrix();
            
            GL.Color3(Color.LimeGreen);
            GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(0, -ROOM_HEIGHT, 0);
                GL.Vertex3(0,  ROOM_HEIGHT, 0);
            GL.End();
            GL.PushMatrix();
                GL.Translate(0, ROOM_HEIGHT, 0);
                RenderCone();
            GL.PopMatrix();

            GL.Color3(Color.Blue);
            GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(0, 0, -ROOM_WIDTH);
                GL.Vertex3(0, 0,  ROOM_WIDTH);
            GL.End();
            GL.PushMatrix();
                GL.Translate(0, 0, -ROOM_WIDTH);
                GL.Rotate(-90, 1, 0, 0);
                RenderCone();
            GL.PopMatrix();
            
            GL.Enable(EnableCap.Lighting);
        }

        private void RenderPortionSegment(int u, int v, Vector3d[,] portion, Color color) {
            Vector3d normal = CalcNormal(
                portion[u, v],
                portion[u, v - 1],
                portion[u - 1, v - 1]
            );

            GL.Color4(color);
            GL.Begin(PrimitiveType.Quads);
                GL.Normal3(normal);

                GL.Vertex3(portion[u, v]);
                GL.Vertex3(portion[u, v - 1]);
                GL.Vertex3(portion[u - 1, v - 1]);
                GL.Vertex3(portion[u - 1, v]);
            GL.End();

            GL.LineWidth(1.5f);
            GL.Color4(Color.Black);
            GL.Begin(PrimitiveType.LineStrip);
                GL.Vertex3(portion[u, v]);
                GL.Vertex3(portion[u, v - 1]);
                GL.Vertex3(portion[u - 1, v - 1]);
                GL.Vertex3(portion[u - 1, v]);
                GL.Vertex3(portion[u, v]);
            GL.End();
        }

        private void RenderPortionFrame(double[,] x, double[,] y, double[,] z, Color color) {
            GL.Disable(EnableCap.Lighting);

            GL.LineWidth(1.5f);
            GL.Color4(color);
            for (int u = 1; u < 4; u++) {
                for (int v = 1; v < 4; v++) {
                    GL.Begin(PrimitiveType.LineStrip);
                        GL.Vertex3(new Vector3d(x[u,v], y[u,v], z[u,v]));
                        GL.Vertex3(new Vector3d(x[u, v - 1], y[u, v - 1], z[u, v - 1]));
                        GL.Vertex3(new Vector3d(x[u - 1, v - 1], y[u - 1, v - 1], z[u - 1, v - 1]));
                        GL.Vertex3(new Vector3d(x[u - 1, v], y[u - 1, v], z[u - 1, v]));
                        GL.Vertex3(new Vector3d(x[u, v], y[u, v], z[u, v]));
                    GL.End();
                }
            }

            GL.PointSize(8); ;
            GL.Color4(color);
            GL.Begin(PrimitiveType.Points);
                for (int u = 0; u < 4; u++) {
                    for (int v = 0; v < 4; v++) {
                        GL.Vertex3(x[u,v], y[u,v], z[u,v]);
                    }
                }
            GL.End();

            GL.Enable(EnableCap.Lighting);
        }

        private void RenderPortions() {
            Color blue = Color.FromArgb(255, 213, 0);
            Color yellow = Color.FromArgb(0, 91, 187);

            if (IsMeshShown) {
                RenderPortionFrame(r1x, r1y, r1z, Color.Purple);
                RenderPortionFrame(r2x, r2y, r2z, Color.Purple);

                blue = Color.FromArgb(175, 255, 213, 0);
                yellow = Color.FromArgb(175, 0, 91, 187);
            }

            int pointsCount = SEGMENTS_PER_PORTION + 1;
            for (int u = 1; u < pointsCount; u++) {
                for (int v = 1; v < pointsCount; v++) {
                    RenderPortionSegment(u, v, portion1, yellow);
                    RenderPortionSegment(u, v, portion2, blue);
                }
            }
        }

        private void RenderScene() {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.PushMatrix();

            RenderWall();
            RenderAxis();
            RenderPortions();
            RenderMesh();

            GL.PopMatrix();

        }

        #endregion




        #region UI


        #region Camera
        
        void scene_MouseWheel(object sender, MouseEventArgs e) {
            if (e.Delta > 0) {
                Zoom(-1);
            } else {
                Zoom(1);
            }
        }

        void scene_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.W) {
                Zoom(-1);
            } else if (e.KeyCode == Keys.S) {
                Zoom(1);
            }
        }

        private void Zoom(int multiplier) {
            distance += zoomSpeed * multiplier;
            if (distance < 35) {
                distance = 35;
            } else if (distance > 256) {
                distance = 256;
            }
            scene.Invalidate();
        }

        void scene_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button == System.Windows.Forms.MouseButtons.Left) {

                hAngle += hAngleSpeed * (e.X - prevX);
                vAngle -= vAngleSpeed * (e.Y - prevY);

                if (vAngle > 179) vAngle = 179;
                else if (vAngle < 1) vAngle = 1;

                scene.Invalidate();
            }

            prevX = e.X;
            prevY = e.Y;
        }

        #endregion


        private void ReadValues() {
            Decimal x, y, z;
            String controlNameTpl = "r{0}{1}{2}{3}Fld";
            NumericUpDown ctrl;
            for (int i = 0; i < 4; i++) {
                for (int j = 0; j < 4; j++) {
                    ctrl = leftPanel.Controls[String.Format(controlNameTpl, 1, i, j, "x")] as NumericUpDown;
                    r1x[i, j] = (double)ctrl.Value;
                    
                    ctrl = leftPanel.Controls[String.Format(controlNameTpl, 1, i, j, "y")] as NumericUpDown;
                    r1y[i, j] = (double)ctrl.Value;
                    
                    ctrl = leftPanel.Controls[String.Format(controlNameTpl, 1, i, j, "z")] as NumericUpDown;
                    r1z[i, j] = (double)ctrl.Value;


                    ctrl = rightPanel.Controls[String.Format(controlNameTpl, 2, i, j, "x")] as NumericUpDown;
                    r2x[i, j] = (double)ctrl.Value;

                    ctrl = rightPanel.Controls[String.Format(controlNameTpl, 2, i, j, "y")] as NumericUpDown;
                    r2y[i, j] = (double)ctrl.Value;

                    ctrl = rightPanel.Controls[String.Format(controlNameTpl, 2, i, j, "z")] as NumericUpDown;
                    r2z[i, j] = (double)ctrl.Value;
                }
            }
        }


        private void ShowValues() {
            String controlNameTpl = "r{0}{1}{2}{3}Fld";
            NumericUpDown ctrl;
            for (int i = 0; i < 4; i++) {
                for (int j = 0; j < 4; j++) {
                    ctrl = leftPanel.Controls[String.Format(controlNameTpl, 1, i, j, "x")] as NumericUpDown;
                    ctrl.Value = (decimal)r1x[i, j];

                    ctrl = leftPanel.Controls[String.Format(controlNameTpl, 1, i, j, "y")] as NumericUpDown;
                    ctrl.Value = (decimal)r1y[i, j];

                    ctrl = leftPanel.Controls[String.Format(controlNameTpl, 1, i, j, "z")] as NumericUpDown;
                    ctrl.Value = (decimal)r1z[i, j];


                    ctrl = rightPanel.Controls[String.Format(controlNameTpl, 2, i, j, "x")] as NumericUpDown;
                    ctrl.Value = (decimal)r2x[i, j];

                    ctrl = rightPanel.Controls[String.Format(controlNameTpl, 2, i, j, "y")] as NumericUpDown;
                    ctrl.Value = (decimal)r2y[i, j];

                    ctrl = rightPanel.Controls[String.Format(controlNameTpl, 2, i, j, "z")] as NumericUpDown;
                    ctrl.Value = (decimal)r2z[i, j];
                }
            }
        }

        private void buildBtn_Click(object sender, EventArgs e) {
            ReadValues();
            NormalizeSecondPortion();
            ShowValues();
            CalcPortionsPoints();
            scene.Invalidate();
        }

        private void generatebtn_Click(object sender, EventArgs e) {
            GenerateRandomPoints();
            NormalizeSecondPortion();
            ShowValues();
            CalcPortionsPoints();
            scene.Invalidate();
        }

        private void showMeshChBox_CheckedChanged(object sender, EventArgs e) {
            IsMeshShown = showMeshChBox.Checked;
            scene.Invalidate();
        }

        #endregion

        

    }
}

