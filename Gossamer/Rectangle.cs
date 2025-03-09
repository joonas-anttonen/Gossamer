namespace Gossamer;

/// <summary>
/// Describes a rectangle.
/// </summary>
/// <param name="Left">Left boundary.</param>
/// <param name="Top">Top boundary.</param>
/// <param name="Right">Right boundary.</param>
/// <param name="Bottom">Bottom boundary.</param>
public readonly record struct Rectangle(float Left, float Top, float Right, float Bottom)
{
    public float Width => Right - Left;
    public float Height => Bottom - Top;

    public Vector2 Position => new(Left, Top);
    public Vector2 Size => new(Width, Height);
    public Vector2 Center => new(Left + (Width / 2f), Top + (Height / 2f));

    public bool Contains(Vector2 p) => (Left <= p.X) && (Top <= p.Y) && (Right >= p.X) && (Bottom >= p.Y);

    public Rectangle CenterOn(Vector2 p) => FromXYWH(p.X - Width / 2f, p.Y - Height / 2f, Width, Height);
    public Rectangle CenterOn(Rectangle r) => FromXYWH(r.Center.X - Width / 2f, r.Center.Y - Height / 2f, Width, Height);
    public Rectangle Crop(float l, float t, float r, float b) => new(Left + l, Top + t, Right - r, Bottom - b);
    public Rectangle Scale(Vector2 v) => new(Left, Top, Left + Width * v.X, Top + Height * v.Y);
    public Rectangle Scale(float x, float y) => new(Left, Top, Left + Width * x, Top + Height * y);
    public Rectangle Move(Vector2 v) => FromXYWH(Left + v.X, Top + v.Y, Width, Height);
    public Rectangle Move(float x, float y) => FromXYWH(Left + x, Top + y, Width, Height);
    public Rectangle Clamp(Rectangle other) => new(MathF.Max(Left, other.Left), MathF.Max(Top, other.Top), MathF.Min(Right, other.Right), MathF.Min(Bottom, other.Bottom));

    public static Rectangle FromPositionSize(Vector2 position, Vector2 size) => new(position.X, position.Y, position.X + size.X, position.Y + size.Y);
    public static Rectangle FromXYWH(float x, float y, float w, float h) => new(x, y, x + w, y + h);
    public static Rectangle FromLTRB(float l, float t, float r, float b) => new(l, t, r, b);
}
