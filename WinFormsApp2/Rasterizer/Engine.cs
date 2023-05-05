using System.Numerics;

namespace PrismGraphics.Rasterizer;

/// <summary>
/// The <see cref="Engine"/> class - It is used to render 3D scenes onto a 2D canvas.
/// <list type="table">
/// <item>See also: <seealso cref="Triangle"/></item>
/// <item>See also: <seealso cref="Camera"/></item>
/// <item>See also: <seealso cref="Light"/></item>
/// <item>See also: <seealso cref="Mesh"/></item>
/// </list>
/// </summary>
public class Engine
{
	/// <summary>
	/// Creates a new instance of the <see cref="Engine"/> class.
	/// </summary>
	/// <param name="Width">Width (in pixels) of the canvas.</param>
	/// <param name="Height">Height (in pixels) of the canvas.</param>
	/// <param name="FOV">The FOV (Field Of View) that the camera will have.</param>
	public Engine(ushort Width, ushort Height, float FOV)
	{
		this.Height = Height;
		this.Width = Width;

		SkyColor = Color.Blue;
		Lights = new();
		Objects = new();
		Camera = new(FOV);
		Gravity = 1f;
		Zoom = 0f;
	}

	#region Methods

	/// <summary>
	/// Renders the whole scene onto the internal canvas.
	/// You must draw this object as an image on another canvas to show the output.
	/// </summary>
	public void Render(Graphics g)
	{
        Graphics = g;
		// Create the camera rotation matrix - This is separate from the object rotation.
		Quaternion CameraQ = Camera.GetRotationQuaternion();

		// Create a new projection value. This is equivalent to using a projection matrix, but it is easier.
		float Translator = (float)(Width / 2 / Math.Tan((Camera.FOV + Zoom) / 2 * 0.0174532925)); // 0.0174532925 == pi / 180

		// Set the sky color - Make sure to adjust for ambiant color aswell.
		//Clear(Color.Normalize(SkyColor) * Camera.Ambient);

        Graphics.Clear(Color.FromArgb(SkyColor.A / 255 * Camera.Ambient.A, SkyColor.R / 255 * Camera.Ambient.A, SkyColor.G / 255 * Camera.Ambient.A, SkyColor.B / 255 * Camera.Ambient.A));

		// Calculate Objects - Loops over all triangle in every mesh.
		foreach (Mesh M in Objects)
		{
			// Check if the mesh has physics.
			if (M.HasPhysics)
			{
				// Apply physics.
				M.Step(Gravity, 1.0f);
			}

			// Create a rotation matrix for the mesh - Separate from camera rotation.
			Matrix4x4 Rotation = M.GetRotationMatrix();

			foreach (Triangle T in M.Triangles)
			{
				// Create a temporary instance of the triangle that can be modified.
				Triangle Temp = T;

				Temp = Triangle.Transform(Temp, Rotation); // Apply object rotation - Separate from camera rotation.
				Temp = Triangle.Translate(Temp, M.Position); // Apply object translation - The position it is in 3D.
				Temp = Triangle.Transform(Temp, CameraQ); // Apply camera rotation - Rotates the entire world as one mesh.
				Temp = Triangle.Translate(Temp, Camera.Position); // Apply camera position - Adjusts the world as one mesh.
				Temp = Triangle.Translate(Temp, Translator); // Apply perspective translation - Applies a 3D effect.

				// Check if the triangle doesn't need to be drawn.
				if (Temp.GetNormal() < 0)
				{
					// Moves everything to center - Most 3D renderers do this.
					Temp = Triangle.Center(Temp, Width, Height);

					// Normalize lighting & apply ambiance.
                    Temp.Color = Color.FromArgb(Temp.Color.A / 255, Temp.Color.R / 255, Temp.Color.G / 255, Temp.Color.B / 255);
                    Temp.Color = Color.FromArgb(Temp.Color.A * Camera.Ambient.A, Temp.Color.R * Camera.Ambient.A, Temp.Color.G * Camera.Ambient.A, Temp.Color.B * Camera.Ambient.A);

                    // Rasterize the triangle.
                    DrawFilledTriangle(Temp);
				}
			}
		}
	}
    public void DrawFilledTriangle(Triangle Triangle)
    {
        Triangle.P1 *= 16;
        Triangle.P2 *= 16;
        Triangle.P3 *= 16;

        // Deltas
        Vector3 D12 = Triangle.P1 - Triangle.P2;
        Vector3 D23 = Triangle.P2 - Triangle.P3;
        Vector3 D31 = Triangle.P3 - Triangle.P1;

        // Fixed-point deltas
        int FDX12 = (int)D12.X << 4;
        int FDX23 = (int)D23.X << 4;
        int FDX31 = (int)D31.X << 4;

        int FDY12 = (int)D12.Y << 4;
        int FDY23 = (int)D23.Y << 4;
        int FDY31 = (int)D31.Y << 4;

        // Bounding rectangle
        Vector3 Min = Vector3.Min(Vector3.Min(Triangle.P1, Triangle.P2), Triangle.P3);
        Vector3 Max = Vector3.Max(Vector3.Max(Triangle.P1, Triangle.P2), Triangle.P3);

        // Some math things - Idk what they do but they are needed.
        Min.X = (int)(Min.X + 0xF) >> 4;
        Min.Y = (int)(Min.Y + 0xF) >> 4;
        Min.Z = (int)(Min.Z + 0xF) >> 4;
        Max.X = (int)(Max.X + 0xF) >> 4;
        Max.Y = (int)(Max.Y + 0xF) >> 4;
        Max.Z = (int)(Max.Z + 0xF) >> 4;

        // Half-edge constants
        int C1 = (int)(D12.Y * Triangle.P1.X - D12.X * Triangle.P1.Y);
        int C2 = (int)(D23.Y * Triangle.P2.X - D23.X * Triangle.P2.Y);
        int C3 = (int)(D31.Y * Triangle.P3.X - D31.X * Triangle.P3.Y);

        // Correct for fill convention
        if (D12.Y < 0 || (D12.Y == 0 && D12.X > 0)) C1++;
        if (D23.Y < 0 || (D23.Y == 0 && D23.X > 0)) C2++;
        if (D31.Y < 0 || (D31.Y == 0 && D31.X > 0)) C3++;

        int CY1 = (int)(C1 + D12.X * ((int)Min.Y << 4) - D12.Y * ((int)Min.X << 4));
        int CY2 = (int)(C2 + D23.X * ((int)Min.Y << 4) - D23.Y * ((int)Min.X << 4));
        int CY3 = (int)(C3 + D31.X * ((int)Min.Y << 4) - D31.Y * ((int)Min.X << 4));

        for (int Y = (int)Min.Y; Y < Max.Y; Y++)
        {
            // Don't draw outside of the screen.
            if (Y >= Height || Y < 0)
            {
                continue;
            }

            int CX1 = CY1;
            int CX2 = CY2;
            int CX3 = CY3;

            for (int X = (int)Min.X; X < Max.X; X++)
            {
                // Don't draw outside of the screen.
                if (X >= Width || X < 0)
                {
                    continue;
                }

                if (CX1 > 0 && CX2 > 0 && CX3 > 0)
                {
                    Rectangle g = new(X, Y, 1, 1);
                    Graphics.FillRectangle(new SolidBrush(Triangle.Color), g);
                    //this[X, Y] = Triangle.Color;
                }

                CX1 -= FDY12;
                CX2 -= FDY23;
                CX3 -= FDY31;
            }

            CY1 += FDX12;
            CY2 += FDX23;
            CY3 += FDX31;
        }
    }

    #endregion

    #region Fields

    public List<Light> Lights;
	public List<Mesh> Objects;
	public Color SkyColor;
	public Camera Camera;
    public Graphics Graphics;
    public float Gravity;
	public float Zoom;
	public ushort Width;
	public ushort Height;
	#endregion
}