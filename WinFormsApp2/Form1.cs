using PrismGraphics.Rasterizer;

namespace WinFormsApp2
{
    public partial class Form1 : Form
    {
        Mesh c;

        public Form1()
        {
            InitializeComponent();

            c = Mesh.GetCube(100, 100, 10);
            engineControl1.Engine.Objects.Add(c);
            engineControl1.RenderTimer.Tick += RenderTimer_Tick;
            engineControl1.Resize += EngineControl1_Resize;
        }

        private void EngineControl1_Resize(object? sender, EventArgs e)
        {
            engineControl1.Engine.Objects.Add(c);
        }

        private void RenderTimer_Tick(object? sender, EventArgs e)
        {
            c.TestLogic(0.01f);
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            engineControl1.Engine.Camera.FOV = trackBar1.Value;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            engineControl1.Engine.SkyColor = Color.FromArgb(255, trackBar2.Value, engineControl1.Engine.SkyColor.G, engineControl1.Engine.SkyColor.B);
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            engineControl1.Engine.SkyColor = Color.FromArgb(255, engineControl1.Engine.SkyColor.R, trackBar3.Value, engineControl1.Engine.SkyColor.B);
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            engineControl1.Engine.SkyColor = Color.FromArgb(255, engineControl1.Engine.SkyColor.R, engineControl1.Engine.SkyColor.G, trackBar4.Value);
        }
    }
}