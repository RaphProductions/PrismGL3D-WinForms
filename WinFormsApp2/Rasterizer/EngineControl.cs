using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrismGraphics.Rasterizer
{
    public class EngineControl : Panel
    {
        public Engine Engine { get; set; }
        public bool ShowFPS { get; set; } = false;
        public float FOV { get; set; } = 90;
        public System.Windows.Forms.Timer RenderTimer { get; private set; }

        Graphics gr;
        Bitmap bmp;
        Graphics gr2;

        int frame;
        int fps;
        int d;

        public EngineControl()
        {
            gr = CreateGraphics();
            bmp = new(Width, Height);
            gr2 = Graphics.FromImage(bmp);
            Engine = new((ushort)Width, (ushort)Height, FOV);

            Resize += this_Resize;

            System.Windows.Forms.Timer t = new() { Interval = 1 };
            t.Tick += T_Tick;
            RenderTimer = t;
            
            if (!DesignMode)
                t.Start();
        }

        private void T_Tick(object? sender, EventArgs e)
        {
            if (DesignMode)
                RenderTimer.Stop();

            Engine.Render(gr2);

            if (ShowFPS)
                gr2.DrawString($"{fps} FPS", new("Segoe UI", 9f), new SolidBrush(Color.White), 10, 10);

            gr.DrawImage(bmp, 0, 0);

            if (d != DateTime.Now.Second)
            {
                fps = frame;
                frame = 0;
                d = DateTime.Now.Second;
            }
            frame++;
        }

        private void this_Resize(object? sender, EventArgs e)
        {
            gr = CreateGraphics();
            bmp = new(Width, Height);
            gr2 = Graphics.FromImage(bmp);
            Engine = new((ushort)Width, (ushort)Height, FOV);
        }
    }
}
