using Gossamer.Gfx.Text;
using Gossamer.Utilities;

using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Gfx;

record struct Command(uint VertexOffset, uint IndexOffset, uint IndexCount, PixelBuffer? Texture, int Font);
record struct CommandBatch(int FirstCommandIndex, int CommandCount, PixelBuffer? Surface);

public enum ImageFilter
{
    Nearest,
    Linear,
}

public enum ImageFit
{
    None,
    Center,
    Fill,
    FillAspect,
}

public class Gfx2DCommandBuffer
{
    const int InitialArraySize = 8192 * 4;

    readonly Vector2[] temp_points = new Vector2[InitialArraySize];
    readonly Vector2[] temp_normals = new Vector2[InitialArraySize];
    readonly Vector2[] scratchVertices = new Vector2[InitialArraySize];
    int scratchVertexCount;

    Vertex2D[] vertices = new Vertex2D[InitialArraySize];
    int frameVertexCount;
    ushort[] indices = new ushort[InitialArraySize];
    int frameIndexCount;

    int commandsCount;
    int batchCount;
    Command[] commands = new Command[32];

    CommandBatch[] batches = new CommandBatch[8];

    bool batchInProgress = false;
    int batchFirstCommandIndex;
    int batchCommandCount;

    public void Reset()
    {
        frameVertexCount = 0;
        frameIndexCount = 0;
        commandsCount = 0;
        batchCount = 0;
        scratchVertexCount = 0;
    }

    internal Gfx2D.Statistics GetStatistics()
    {
        return new Gfx2D.Statistics(commandsCount, frameIndexCount / 3);
    }

    internal ReadOnlySpan<Vertex2D> GetVertices()
    {
        return vertices.AsSpan(0, frameVertexCount);
    }

    internal ReadOnlySpan<ushort> GetIndices()
    {
        return indices.AsSpan(0, frameIndexCount);
    }

    internal ReadOnlySpan<CommandBatch> GetBatches()
    {
        return batches.AsSpan(0, batchCount);
    }

    internal void GetBatchData(CommandBatch batch, out ReadOnlySpan<Command> commands)
    {
        commands = this.commands.AsSpan(batch.FirstCommandIndex, batch.CommandCount);
    }

    public void BeginBatch()
    {
        ThrowInvalidOperationIf(batchInProgress);

        batchInProgress = true;
        batchFirstCommandIndex = commandsCount;
        batchCommandCount = 0;

        BeginCommand();
    }

    public void EndBatch(PixelBuffer? surface = null)
    {
        ThrowInvalidOperationIfNot(batchInProgress);
        batchInProgress = false;

        ArrayUtilities.Reserve(ref batches, batchCount + 1);

        batches[batchCount] = new CommandBatch(batchFirstCommandIndex, batchCommandCount, surface);
        batchCount++;
    }

    ref Command BeginCommand()
    {
        ArrayUtilities.Reserve(ref commands, commandsCount + 1);

        Command command = new((uint)frameVertexCount, (uint)frameIndexCount, 0, default, default);
        commands[commandsCount++] = command;

        batchCommandCount++;

        return ref GetCurrentCommand();
    }

    ref Command GetCurrentCommand()
    {
        ThrowInvalidOperationIfNot(batchInProgress);

        return ref commands[commandsCount - 1];
    }

    public void DrawImage(PixelBuffer image, Vector2 targetPosition, Vector2 targetExtent, ImageFit imageFit)
    {
        ThrowInvalidOperationIfNot(batchInProgress);

        ref Command newCommand = ref BeginCommand();
        newCommand.Texture = image;

        Vector2 imageExtent = new(image.Width, image.Height);
        Vector2 uv0 = new(0, 0);
        Vector2 uv1 = new(1, 1);

        Vector2 finalImagePosition = targetPosition;
        Vector2 finalImageExtent = targetExtent;

        if (imageFit == ImageFit.None)
        {
            finalImagePosition = targetPosition;
            finalImageExtent = imageExtent;
        }
        else if (imageFit == ImageFit.Fill)
        {
            finalImagePosition = targetPosition;
            finalImageExtent = targetExtent;
        }
        else if (imageFit == ImageFit.FillAspect)
        {
            bool horizontal = imageExtent.X > imageExtent.Y;
            float scale = horizontal
                        ? targetExtent.X / imageExtent.X
                        : targetExtent.Y / imageExtent.Y;
            finalImageExtent = imageExtent * scale;

            Vector2 offset = (targetExtent - finalImageExtent) * 0.5f;
            finalImagePosition = targetPosition + offset;
        }
        else if (imageFit == ImageFit.Center)
        {
            Vector2 offset = (targetExtent - imageExtent) * 0.5f;
            finalImagePosition = targetPosition + offset;
            finalImageExtent = imageExtent;
        }

        PushQuadUV(finalImagePosition, finalImagePosition + finalImageExtent, uv0, uv1, Color.White);
    }

    public void DrawText(TextLayout layout, Vector2 position, Color color)
    {
        ThrowInvalidOperationIfNot(batchInProgress);
        ThrowInvalidOperationIfNull(layout.Font);

        ref Command newCommand = ref BeginCommand();
        newCommand.Font = layout.Font.Index;
        for (int i = 0; i < layout.GlyphCount; i++)
        {
            var glyph = layout.Glyphs[i];
            Vector2 a = position + glyph.Position;
            Vector2 c = a + glyph.Size;

            PushQuadUV(a, c, glyph.UV0, glyph.UV1, color);
        }
    }

    public void DrawCircle(Vector2 center, float radius, Color color, float thickness = 1.0f, bool useAntialiasing = true)
    {
        ThrowInvalidOperationIfNot(batchInProgress);

        if (radius <= 0.0f)
            return;

        int num_segments = (int)(radius * 2.0f * MathF.PI);
        if (num_segments < 2)
            num_segments = 2;
        if (num_segments > 512)
            num_segments = 512;

        float angle_step = 2.0f * MathF.PI / num_segments;

        for (int i = 0; i < num_segments; i++)
        {
            float a0 = i * angle_step;
            float a1 = (i + 1) * angle_step;

            Vector2 p0 = new(center.X + MathF.Cos(a0) * radius, center.Y + MathF.Sin(a0) * radius);
            Vector2 p1 = new(center.X + MathF.Cos(a1) * radius, center.Y + MathF.Sin(a1) * radius);

            scratchVertices[scratchVertexCount++] = p0;
            scratchVertices[scratchVertexCount++] = p1;

            PushPolyline(scratchVertices.AsSpan(0, scratchVertexCount), thickness, color, useAntialiasing, isClosed: false);

            scratchVertexCount = 0;
        }
    }

    public void DrawRectangle(Rectangle rectangle, Color color, float thickness = 1.0f)
    {
        DrawRectangle(rectangle.Position, rectangle.Position + rectangle.Size, color, thickness);
    }

    public void DrawRectangle(Vector2 a, Vector2 c, Color color, float thickness = 1.0f)
    {
        float half_thickness = thickness * 0.5f;

        PushQuadUV(new(a.X - half_thickness, a.Y - half_thickness), new(c.X + half_thickness, a.Y + half_thickness), Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
        PushQuadUV(new(a.X - half_thickness, c.Y - half_thickness), new(c.X + half_thickness, c.Y + half_thickness), Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
        PushQuadUV(new(a.X - half_thickness, a.Y + half_thickness), new(a.X + half_thickness, c.Y - half_thickness), Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
        PushQuadUV(new(c.X - half_thickness, a.Y + half_thickness), new(c.X + half_thickness, c.Y - half_thickness), Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
    }

    public void FillRectangle(Rectangle rectangle, Color color)
    {
        PushQuadUV(rectangle.Position, rectangle.Position + rectangle.Size, Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
    }

    public void FillRectangle(Vector2 a, Vector2 c, Color color)
    {
        PushQuadUV(a, c, Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
    }

    void PushQuadUV(Vector2 a, Vector2 c, Vector2 a_uv, Vector2 c_uv, Color color)
    {
        ArrayUtilities.Reserve(ref vertices, frameVertexCount + 4);
        ArrayUtilities.Reserve(ref indices, frameIndexCount + 6);

        Vector2 b = new(c.X, a.Y);
        Vector2 d = new(a.X, c.Y);
        Vector2 b_uv = new(c_uv.X, a_uv.Y);
        Vector2 d_uv = new(a_uv.X, c_uv.Y);

        indices[frameIndexCount + 0] = (ushort)(frameVertexCount + 0);
        indices[frameIndexCount + 1] = (ushort)(frameVertexCount + 1);
        indices[frameIndexCount + 2] = (ushort)(frameVertexCount + 2);
        indices[frameIndexCount + 3] = (ushort)(frameVertexCount + 0);
        indices[frameIndexCount + 4] = (ushort)(frameVertexCount + 2);
        indices[frameIndexCount + 5] = (ushort)(frameVertexCount + 3);
        frameIndexCount += 6;

        vertices[frameVertexCount + 0] = new Vertex2D(a, a_uv, color);
        vertices[frameVertexCount + 1] = new Vertex2D(b, b_uv, color);
        vertices[frameVertexCount + 2] = new Vertex2D(c, c_uv, color);
        vertices[frameVertexCount + 3] = new Vertex2D(d, d_uv, color);
        frameVertexCount += 4;

        ref Command currentCommand = ref GetCurrentCommand();
        currentCommand.IndexCount += 6;
    }

    void PushPolyline(ReadOnlySpan<Vector2> points, float thickness, Color color, bool useAntialiasing, bool isClosed)
    {
        static void NormalizeOverZero(ref float VX, ref float VY)
        {
            float d2 = VX * VX + VY * VY;
            if (d2 > 0.0f)
            {
                float inv_len = 1.0f / MathF.Sqrt(d2);
                VX *= inv_len;
                VY *= inv_len;
            }
        }

        static void FixNormal(ref float VX, ref float VY)
        {
            float d2 = VX * VX + VY * VY;
            if (d2 > 0.000001f)
            {
                float inv_len2 = 1.0f / d2;
                if (inv_len2 > 100.0f)
                    inv_len2 = 100.0f;
                VX *= inv_len2;
                VY *= inv_len2;
            }
        }

        int points_count = points.Length;
        bool closed = isClosed;
        int count = closed ? points_count : points_count - 1;
        bool thick_line = thickness > 1.0f;

        int frameIndexCountStart = frameIndexCount;

        if (useAntialiasing)
        {
            float AA_SIZE = 1.0f;

            thickness = MathF.Max(thickness, 1.0f);

            int vtx_count = thick_line ? points_count * 4 : points_count * 3;
            int idx_count = thick_line ? count * 18 : count * 12;

            ArrayUtilities.Reserve(ref vertices, frameVertexCount + vtx_count);
            ArrayUtilities.Reserve(ref indices, frameIndexCount + idx_count);

            for (int i1 = 0; i1 < count; i1++)
            {
                int i2 = (i1 + 1) == points_count ? 0 : i1 + 1;
                float dx = points[i2].X - points[i1].X;
                float dy = points[i2].Y - points[i1].Y;
                NormalizeOverZero(ref dx, ref dy);
                temp_normals[i1].X = dy;
                temp_normals[i1].Y = -dx;
            }
            if (!closed)
                temp_normals[points_count - 1] = temp_normals[points_count - 2];

            if (!thick_line)
            {
                float half_draw_size = AA_SIZE;

                if (!closed)
                {
                    temp_points[0] = points[0] + temp_normals[0] * half_draw_size;
                    temp_points[1] = points[0] - temp_normals[0] * half_draw_size;
                    temp_points[(points_count - 1) * 2 + 0] = points[points_count - 1] + temp_normals[points_count - 1] * half_draw_size;
                    temp_points[(points_count - 1) * 2 + 1] = points[points_count - 1] - temp_normals[points_count - 1] * half_draw_size;
                }

                int idx1 = frameVertexCount;
                for (int i1 = 0; i1 < count; i1++)
                {
                    int i2 = (i1 + 1) == points_count ? 0 : i1 + 1;
                    int idx2 = ((i1 + 1) == points_count) ? frameVertexCount : (idx1 + 3);

                    float dm_x = (temp_normals[i1].X + temp_normals[i2].X) * 0.5f;
                    float dm_y = (temp_normals[i1].Y + temp_normals[i2].Y) * 0.5f;
                    FixNormal(ref dm_x, ref dm_y);
                    dm_x *= half_draw_size;
                    dm_y *= half_draw_size;

                    temp_points[i2 * 2 + 0].X = points[i2].X + dm_x;
                    temp_points[i2 * 2 + 0].Y = points[i2].Y + dm_y;
                    temp_points[i2 * 2 + 1].X = points[i2].X - dm_x;
                    temp_points[i2 * 2 + 1].Y = points[i2].Y - dm_y;

                    indices[frameIndexCount + 0] = (ushort)(idx2 + 0);
                    indices[frameIndexCount + 1] = (ushort)(idx1 + 0);
                    indices[frameIndexCount + 2] = (ushort)(idx1 + 2);
                    indices[frameIndexCount + 3] = (ushort)(idx1 + 2);
                    indices[frameIndexCount + 4] = (ushort)(idx2 + 2);
                    indices[frameIndexCount + 5] = (ushort)(idx2 + 0);
                    indices[frameIndexCount + 6] = (ushort)(idx2 + 1);
                    indices[frameIndexCount + 7] = (ushort)(idx1 + 1);
                    indices[frameIndexCount + 8] = (ushort)(idx1 + 0);
                    indices[frameIndexCount + 9] = (ushort)(idx1 + 0);
                    indices[frameIndexCount + 10] = (ushort)(idx2 + 0);
                    indices[frameIndexCount + 11] = (ushort)(idx2 + 1);
                    frameIndexCount += 12;

                    idx1 = idx2;
                }

                for (int i = 0; i < points_count; i++)
                {
                    vertices[frameVertexCount + 0] = new Vertex2D(points[i], Vertex2D.DefaultUV, color);
                    vertices[frameVertexCount + 1] = new Vertex2D(temp_points[i * 2 + 0], Vertex2D.DefaultUV, Color.Transparent);
                    vertices[frameVertexCount + 2] = new Vertex2D(temp_points[i * 2 + 1], Vertex2D.DefaultUV, Color.Transparent);
                    frameVertexCount += 3;
                }
            }
            else
            {
                float half_inner_thickness = (thickness - AA_SIZE) * 0.5f;

                if (!closed)
                {
                    int points_last = points_count - 1;
                    temp_points[0] = points[0] + temp_normals[0] * (half_inner_thickness + AA_SIZE);
                    temp_points[1] = points[0] + temp_normals[0] * half_inner_thickness;
                    temp_points[2] = points[0] - temp_normals[0] * half_inner_thickness;
                    temp_points[3] = points[0] - temp_normals[0] * (half_inner_thickness + AA_SIZE);
                    temp_points[points_last * 4 + 0] = points[points_last] + temp_normals[points_last] * (half_inner_thickness + AA_SIZE);
                    temp_points[points_last * 4 + 1] = points[points_last] + temp_normals[points_last] * half_inner_thickness;
                    temp_points[points_last * 4 + 2] = points[points_last] - temp_normals[points_last] * half_inner_thickness;
                    temp_points[points_last * 4 + 3] = points[points_last] - temp_normals[points_last] * (half_inner_thickness + AA_SIZE);
                }

                int idx1 = frameVertexCount;
                for (int i1 = 0; i1 < count; i1++)
                {
                    int i2 = (i1 + 1) == points_count ? 0 : (i1 + 1);
                    int idx2 = (i1 + 1) == points_count ? frameVertexCount : (idx1 + 4);

                    float dm_x = (temp_normals[i1].X + temp_normals[i2].X) * 0.5f;
                    float dm_y = (temp_normals[i1].Y + temp_normals[i2].Y) * 0.5f;
                    FixNormal(ref dm_x, ref dm_y);
                    float dm_out_x = dm_x * (half_inner_thickness + AA_SIZE);
                    float dm_out_y = dm_y * (half_inner_thickness + AA_SIZE);
                    float dm_in_x = dm_x * half_inner_thickness;
                    float dm_in_y = dm_y * half_inner_thickness;

                    temp_points[i2 * 4 + 0].X = points[i2].X + dm_out_x;
                    temp_points[i2 * 4 + 0].Y = points[i2].Y + dm_out_y;
                    temp_points[i2 * 4 + 1].X = points[i2].X + dm_in_x;
                    temp_points[i2 * 4 + 1].Y = points[i2].Y + dm_in_y;
                    temp_points[i2 * 4 + 2].X = points[i2].X - dm_in_x;
                    temp_points[i2 * 4 + 2].Y = points[i2].Y - dm_in_y;
                    temp_points[i2 * 4 + 3].X = points[i2].X - dm_out_x;
                    temp_points[i2 * 4 + 3].Y = points[i2].Y - dm_out_y;

                    indices[frameIndexCount + 0] = (ushort)(idx2 + 1);
                    indices[frameIndexCount + 1] = (ushort)(idx1 + 1);
                    indices[frameIndexCount + 2] = (ushort)(idx1 + 2);
                    indices[frameIndexCount + 3] = (ushort)(idx1 + 2);
                    indices[frameIndexCount + 4] = (ushort)(idx2 + 2);
                    indices[frameIndexCount + 5] = (ushort)(idx2 + 1);
                    indices[frameIndexCount + 6] = (ushort)(idx2 + 1);
                    indices[frameIndexCount + 7] = (ushort)(idx1 + 1);
                    indices[frameIndexCount + 8] = (ushort)(idx1 + 0);
                    indices[frameIndexCount + 9] = (ushort)(idx1 + 0);
                    indices[frameIndexCount + 10] = (ushort)(idx2 + 0);
                    indices[frameIndexCount + 11] = (ushort)(idx2 + 1);
                    indices[frameIndexCount + 12] = (ushort)(idx2 + 2);
                    indices[frameIndexCount + 13] = (ushort)(idx1 + 2);
                    indices[frameIndexCount + 14] = (ushort)(idx1 + 3);
                    indices[frameIndexCount + 15] = (ushort)(idx1 + 3);
                    indices[frameIndexCount + 16] = (ushort)(idx2 + 3);
                    indices[frameIndexCount + 17] = (ushort)(idx2 + 2);
                    frameIndexCount += 18;

                    idx1 = idx2;
                }

                for (int i = 0; i < points_count; i++)
                {
                    vertices[frameVertexCount + 0] = new Vertex2D(temp_points[i * 4 + 0], Vertex2D.DefaultUV, Color.Transparent);
                    vertices[frameVertexCount + 1] = new Vertex2D(temp_points[i * 4 + 1], Vertex2D.DefaultUV, color);
                    vertices[frameVertexCount + 2] = new Vertex2D(temp_points[i * 4 + 2], Vertex2D.DefaultUV, color);
                    vertices[frameVertexCount + 3] = new Vertex2D(temp_points[i * 4 + 3], Vertex2D.DefaultUV, Color.Transparent);
                    frameVertexCount += 4;
                }
            }
        }
        else
        {
            ArrayUtilities.Reserve(ref vertices, frameVertexCount + count * 4);
            ArrayUtilities.Reserve(ref indices, frameIndexCount + count * 6);

            for (int i1 = 0; i1 < count; i1++)
            {
                int i2 = (i1 + 1) == points_count ? 0 : i1 + 1;
                Vector2 p1 = points[i1];
                Vector2 p2 = points[i2];

                float dx = p2.X - p1.X;
                float dy = p2.Y - p1.Y;
                NormalizeOverZero(ref dx, ref dy);
                dx *= thickness * 0.5f;
                dy *= thickness * 0.5f;

                indices[frameIndexCount + 0] = (ushort)(frameVertexCount + 0);
                indices[frameIndexCount + 1] = (ushort)(frameVertexCount + 1);
                indices[frameIndexCount + 2] = (ushort)(frameVertexCount + 2);
                indices[frameIndexCount + 3] = (ushort)(frameVertexCount + 0);
                indices[frameIndexCount + 4] = (ushort)(frameVertexCount + 2);
                indices[frameIndexCount + 5] = (ushort)(frameVertexCount + 3);
                frameIndexCount += 6;

                vertices[frameVertexCount + 0] = new Vertex2D(new(p1.X + dy, p1.Y - dx), Vertex2D.DefaultUV, color);
                vertices[frameVertexCount + 1] = new Vertex2D(new(p2.X + dy, p2.Y - dx), Vertex2D.DefaultUV, color);
                vertices[frameVertexCount + 2] = new Vertex2D(new(p2.X - dy, p2.Y + dx), Vertex2D.DefaultUV, color);
                vertices[frameVertexCount + 3] = new Vertex2D(new(p1.X - dy, p1.Y + dx), Vertex2D.DefaultUV, color);
                frameVertexCount += 4;
            }
        }

        ref Command currentCommand = ref GetCurrentCommand();
        currentCommand.IndexCount += (uint)(frameIndexCount - frameIndexCountStart);
    }
}
