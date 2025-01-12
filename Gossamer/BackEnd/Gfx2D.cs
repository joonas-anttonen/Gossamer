using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Gossamer.Collections;
using Gossamer.External.Vulkan;
using Gossamer.Utilities;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Backend;

class Gfx2D(Gfx gfx) : IDisposable
{
    readonly Gfx gfx = gfx;

    readonly GfxFontCache fontCache = new();

    bool renderingInitialized;

    DisplayParameters? parameters;

    VkSampler drawSampler;

    GfxPipeline? pipeline;
    GfxPipeline? compositionPipeline;

    PixelBuffer? backBuffer;

    MemoryBuffer<PerCommandData>? uniformBuffer;
    MemoryBuffer<Vertex2D>? vertexBuffer;
    MemoryBuffer<ushort>? indexBuffer;

    readonly Dictionary<GfxFont, int> fontTextureIndices = [];
    PixelBuffer[] fontTextures = [];

    bool frameInProgress = false;
    bool batchInProgress = false;

    const int InitialArraySize = 8192 * 4;

    readonly Vector2[] temp_points = new Vector2[InitialArraySize];
    readonly Vector2[] temp_normals = new Vector2[InitialArraySize];
    readonly Vector2[] scratchVertices = new Vector2[InitialArraySize];
    int scratchVertexCount;

    Vertex2D[] vertices = new Vertex2D[InitialArraySize];
    int frameVertexCount;
    ushort[] indices = new ushort[InitialArraySize];
    int frameIndexCount;

    record struct Command(uint VertexOffset, uint IndexOffset, uint IndexCount, PixelBuffer? Texture, GfxFont? Font, Vector3 Color);
    int commandsCount;
    Command[] commands = new Command[128];

    [StructLayout(LayoutKind.Sequential)]
    readonly struct Vertex2D(Vector2 position, Vector2 uv, Color color)
    {
        public static readonly Vector2 DefaultUV = new(-1, -1);

        public readonly Vector2 Position = position;
        public readonly Vector2 UV = uv;
        public readonly Vector4 Color = color.ToVector4();
    }

    [StructLayout(LayoutKind.Sequential)]
    readonly struct PerCommandData(Vector2 scale, Vector2 translation, Vector3 color)
    {
        public readonly Vector2 Scale = scale;
        public readonly Vector2 Translation = translation;
        public readonly Vector3 Color = color;
        readonly float _padding;
    }

    ref Command BeginCommand()
    {
        Command command = new((uint)frameVertexCount, (uint)frameIndexCount, 0, default, default, Color.Black.ToVector3());

        ArrayUtilities.Reserve(ref commands, commandsCount + 1);

        commands[commandsCount++] = command;

        return ref GetCurrentCommand();
    }

    ref Command GetCurrentCommand()
    {
        Assert(batchInProgress);

        return ref commands[commandsCount - 1];
    }

    public void Create()
    {
        AssertIsNull(uniformBuffer);
        AssertIsNull(vertexBuffer);
        AssertIsNull(indexBuffer);

        vertexBuffer = gfx.CreateDynamicMemoryBuffer<Vertex2D>(length: vertices.Length, GfxMemoryBufferUsage.Vertex);
        indexBuffer = gfx.CreateDynamicMemoryBuffer<ushort>(length: indices.Length, GfxMemoryBufferUsage.Index);
        uniformBuffer = gfx.CreateDynamicMemoryBuffer<PerCommandData>(length: 1, GfxMemoryBufferUsage.Uniform);

        VkSamplerCreateInfo samplerCreateInfo = new(default)
        {
            AddressModeU = VkSamplerAddressMode.CLAMP_TO_BORDER,
            AddressModeV = VkSamplerAddressMode.CLAMP_TO_BORDER,
            AddressModeW = VkSamplerAddressMode.CLAMP_TO_BORDER,

            MinFilter = VkFilter.NEAREST,
            MagFilter = VkFilter.NEAREST,
            MipmapMode = VkSamplerMipmapMode.NEAREST,

            BorderColor = VkBorderColor.FLOAT_OPAQUE_WHITE,

            MaxAnisotropy = 1,
        };
        drawSampler = gfx.CreateSampler(samplerCreateInfo);

        unsafe
        {
            (GfxFont font, GfxFontAtlas fontAtlas) = fontCache.LoadFont("Iceland.ttf", 36, CharacterSet.Full);

            PixelBuffer fontTexture = gfx.CreatePixelBuffer(
                width: fontAtlas.Width,
                height: fontAtlas.Height,
                format: GfxFormat.Rgba8,
                usage: GfxPixelBufferUsage.Sampled | GfxPixelBufferUsage.TransferDst,
                aspect: GfxAspect.Color,
                samples: GfxSamples.X1);

            MemoryBuffer<byte> fontStagingBuffer = gfx.CreateDynamicMemoryBuffer<byte>(length: fontAtlas.Pixels.Length, GfxMemoryBufferUsage.TransferSrc);
            gfx.UpdateDynamicBuffer(fontStagingBuffer, fontAtlas.Pixels);

            GfxSingleCommand fontStagingCommand = gfx.BeginSingleCommand();

            gfx.PixelBufferBarrier(
                fontStagingCommand.CommandBuffer,
                pixelBuffer: fontTexture,
                srcLayout: VkImageLayout.UNDEFINED,
                dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL);

            VkBufferImageCopy bufferImageCopy = new()
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new()
                {
                    Aspect = VkImageAspect.COLOR,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },
                ImageOffset = new(0, 0, 0),
                ImageExtent = new(fontAtlas.Width, fontAtlas.Height, 1),
            };

            vkCmdCopyBufferToImage(fontStagingCommand.CommandBuffer, fontStagingBuffer.Buffer, fontTexture.Image, VkImageLayout.TRANSFER_DST_OPTIMAL, 1, &bufferImageCopy);

            gfx.PixelBufferBarrier(
                fontStagingCommand.CommandBuffer,
                pixelBuffer: fontTexture,
                srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
                dstLayout: VkImageLayout.SHADER_READ_ONLY_OPTIMAL);

            gfx.SubmitSingleCommand(fontStagingCommand);
            gfx.EndSingleCommand(fontStagingCommand);

            fontTextureIndices[font] = fontTextures.Length;
            ArrayUtilities.Append(ref fontTextures, fontTexture);

            gfx.DestroyMemoryBuffer(fontStagingBuffer);
        }
    }

    public void Dispose()
    {
        DestroyRendering();

        gfx.DestroySampler(drawSampler);
        drawSampler = default;

        foreach (PixelBuffer fontTexture in fontTextures)
            gfx.DestroyPixelBuffer(fontTexture);
        fontTextures = [];

        gfx.DestroyMemoryBuffer(uniformBuffer);
        uniformBuffer = default;

        gfx.DestroyMemoryBuffer(vertexBuffer);
        vertexBuffer = default;

        gfx.DestroyMemoryBuffer(indexBuffer);
        indexBuffer = default;
    }

    /// <summary>
    /// Begins a frame of rendering. Resets the internal state.
    /// </summary>
    public unsafe void BeginFrame()
    {
        Assert(!frameInProgress && !batchInProgress);
        Assert(renderingInitialized);

        GfxPresenter presenter = gfx.GetPresenter();
        PixelBuffer presentationBuffer = presenter.GetPresentationBuffer();

        bool needsCreate = backBuffer == null || backBuffer.Width != presentationBuffer.Width || backBuffer.Height != presentationBuffer.Height;
        if (needsCreate)
        {
            gfx.DestroyPixelBuffer(backBuffer);
            backBuffer = gfx.CreatePixelBuffer(
                width: presentationBuffer.Width * 1,
                height: presentationBuffer.Height * 1,
                format: presentationBuffer.Format,
                usage: GfxPixelBufferUsage.ColorAttachment | GfxPixelBufferUsage.Sampled | GfxPixelBufferUsage.TransferSrc | GfxPixelBufferUsage.TransferDst,
                aspect: GfxAspect.Color,
                samples: GfxSamples.X1
            );
            gfx.AssingName(backBuffer, StringUtilities.DebugName<Gfx2D>(nameof(backBuffer)));
        }

        frameInProgress = true;
        commandsCount = 0;
        frameVertexCount = 0;
        frameIndexCount = 0;
        scratchVertexCount = 0;

        VkCommandBuffer commandBuffer = presenter.GetCommandBuffer();

        AssertNotNull(backBuffer);

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: backBuffer,
            srcLayout: VkImageLayout.UNDEFINED,
            dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL);

        VkImageSubresourceRange clearRange = new()
        {
            AspectMask = VkImageAspect.COLOR,
            BaseMipLevel = 0,
            LevelCount = 1,
            BaseArrayLayer = 0,
            LayerCount = 1,
        };
        VkClearColorValue clearColor = VkClearColorValue.FromColor(Color.Transparent);
        vkCmdClearColorImage(commandBuffer, backBuffer.Image, VkImageLayout.TRANSFER_DST_OPTIMAL, &clearColor, 1, &clearRange);

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: backBuffer,
            srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            dstLayout: VkImageLayout.COLOR_ATTACHMENT_OPTIMAL);
    }

    /// <summary>
    /// Ends a frame of rendering. Flushes the vertex and index buffers to the GPU.
    /// </summary>
    public unsafe void EndFrame()
    {
        Assert(frameInProgress && !batchInProgress);
        AssertNotNull(vertexBuffer);
        AssertNotNull(indexBuffer);

        frameInProgress = false;

        if (frameVertexCount > 0)
        {
            Console.WriteLine($"Frame vertex count: {frameVertexCount}");

            gfx.UpdateDynamicBuffer(vertexBuffer, vertices, frameVertexCount);
            gfx.UpdateDynamicBuffer(indexBuffer, indices, frameIndexCount);
        }

        AssertNotNull(backBuffer);
        AssertNotNull(compositionPipeline);

        GfxPresenter presenter = gfx.GetPresenter();
        VkCommandBuffer commandBuffer = presenter.GetCommandBuffer();
        PixelBuffer presentBuffer = presenter.GetPresentationBuffer();

        /*if (true)
        {
            gfx.PixelBufferBarrier(
                commandBuffer,
                pixelBuffer: backBuffer,
                srcLayout: VkImageLayout.COLOR_ATTACHMENT_OPTIMAL,
                dstLayout: VkImageLayout.TRANSFER_SRC_OPTIMAL);

            VkImageBlit imageBlit = new()
            {
                Src = new()
                {
                    Aspect = VkImageAspect.COLOR,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },
                SrcOffsets1 = new((int)backBuffer.Width, (int)backBuffer.Height, 1),
                Dst = new()
                {
                    Aspect = VkImageAspect.COLOR,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },
                DstOffsets1 = new((int)presentBuffer.Width, (int)presentBuffer.Height, 1),
            };

            vkCmdBlitImage(commandBuffer, backBuffer.Image, VkImageLayout.TRANSFER_SRC_OPTIMAL, presentBuffer.Image, VkImageLayout.TRANSFER_DST_OPTIMAL, 1, &imageBlit, VkFilter.LINEAR);

            gfx.FIXME_OmegaBarrier(commandBuffer);
            return;
        }*/

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: presentBuffer,
            srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            dstLayout: VkImageLayout.COLOR_ATTACHMENT_OPTIMAL);

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: backBuffer,
            srcLayout: VkImageLayout.COLOR_ATTACHMENT_OPTIMAL,
            dstLayout: VkImageLayout.SHADER_READ_ONLY_OPTIMAL);

        VkRenderingAttachmentInfo colorAttachment = new(default)
        {
            ImageView = presentBuffer.View,
            ImageLayout = VkImageLayout.COLOR_ATTACHMENT_OPTIMAL,
            LoadOp = VkAttachmentLoadOp.LOAD,
            StoreOp = VkAttachmentStoreOp.STORE,
        };

        VkRenderingAttachmentInfo* colorAttachments = stackalloc VkRenderingAttachmentInfo[1] { colorAttachment };
        VkRenderingInfo renderingInfo = new(default)
        {
            RenderArea = new(new(0, 0), new(presentBuffer.Width, presentBuffer.Height)),
            ColorAttachmentCount = 1,
            ColorAttachments = colorAttachments,
            LayerCount = 1,
        };

        VkViewport viewport = new(0, 0, presentBuffer.Width, presentBuffer.Height, 0, 1);
        VkRect2D scissor = new(new(0, 0), new(presentBuffer.Width, presentBuffer.Height));

        vkCmdBeginRendering(commandBuffer, &renderingInfo);

        vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.GRAPHICS, compositionPipeline.Pipeline);

        vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
        vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

        VkWriteDescriptorSet* descriptorWrites = stackalloc VkWriteDescriptorSet[2];

        VkDescriptorImageInfo descriptorImageInfo = new()
        {
            ImageView = backBuffer.View,
            ImageLayout = VkImageLayout.SHADER_READ_ONLY_OPTIMAL,
        };
        descriptorWrites[0] = new(default)
        {
            DestinationBinding = 0,
            DescriptorType = VkDescriptorType.SAMPLED_IMAGE,
            DescriptorCount = 1,
            ImageInfo = &descriptorImageInfo,
        };

        VkDescriptorImageInfo descriptorImageInfo2 = new()
        {
            Sampler = drawSampler,
        };
        descriptorWrites[1] = new(default)
        {
            DestinationBinding = 1,
            DescriptorType = VkDescriptorType.SAMPLER,
            DescriptorCount = 1,
            ImageInfo = &descriptorImageInfo2,
        };

        vkCmdPushDescriptorSet(commandBuffer, VkPipelineBindPoint.GRAPHICS, compositionPipeline.Layout, 0, 2, descriptorWrites);
        vkCmdDraw(commandBuffer, 4, 1, 0, 0);

        vkCmdEndRendering(commandBuffer);

        gfx.FIXME_OmegaBarrier(commandBuffer);

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: presentBuffer,
            srcLayout: VkImageLayout.COLOR_ATTACHMENT_OPTIMAL,
            dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL);
    }

    /// <summary>
    /// Begins a batch of commands. This is used to group commands to a single render pass.
    /// </summary>
    public void BeginBatch()
    {
        Assert(frameInProgress && !batchInProgress);

        batchInProgress = true;
        BeginCommand();
    }

    public unsafe void EndBatch()
    {
        Assert(frameInProgress && batchInProgress);
        AssertNotNull(backBuffer);
        AssertNotNull(pipeline);
        AssertNotNull(vertexBuffer);
        AssertNotNull(indexBuffer);
        AssertNotNull(uniformBuffer);

        batchInProgress = false;

        GfxPipeline activePipeline = pipeline;

        PixelBuffer presentBuffer = backBuffer;

        VkCommandBuffer commandBuffer = gfx.GetPresenter().GetCommandBuffer();

        VkRenderingAttachmentInfo colorAttachment = new(default)
        {
            ImageView = presentBuffer.View,
            ImageLayout = VkImageLayout.COLOR_ATTACHMENT_OPTIMAL,
            LoadOp = VkAttachmentLoadOp.LOAD,
            StoreOp = VkAttachmentStoreOp.STORE,
            ClearValue = VkClearValue.FromColor(Color.Transparent),
        };

        VkRenderingAttachmentInfo* colorAttachments = stackalloc VkRenderingAttachmentInfo[1] { colorAttachment };
        VkRenderingInfo renderingInfo = new(default)
        {
            RenderArea = new(new(0, 0), new(presentBuffer.Width, presentBuffer.Height)),
            ColorAttachmentCount = 1,
            ColorAttachments = colorAttachments,
            LayerCount = 1,
        };

        VkViewport viewport = new(0, 0, presentBuffer.Width, presentBuffer.Height, 0, 1);
        VkRect2D scissor = new(new(0, 0), new(presentBuffer.Width, presentBuffer.Height));

        vkCmdBeginRendering(commandBuffer, &renderingInfo);

        vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.GRAPHICS, activePipeline.Pipeline);

        vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
        vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

        VkBuffer localVertexBuffer = vertexBuffer.Buffer;
        VkBuffer localIndexBuffer = indexBuffer.Buffer;
        ulong vertexBufferOffset = 0ul;
        vkCmdBindVertexBuffers(commandBuffer, 0, 1, &localVertexBuffer, &vertexBufferOffset);
        vkCmdBindIndexBuffer(commandBuffer, localIndexBuffer, 0, VkIndexType.UINT16);

        PerCommandData commandData;

        VkWriteDescriptorSet* descriptorWrites = stackalloc VkWriteDescriptorSet[2];

        for (int i = 0; i < commandsCount; i++)
        {
            ref Command command = ref commands[i];

            commandData = new(
                scale: new(2.0f / presentBuffer.Width, 2.0f / presentBuffer.Height),
                translation: new(-1.0f, -1.0f),
                color: command.Color
            );
            vkCmdPushConstants(commandBuffer, activePipeline.Layout, VkShaderStage.VERTEX | VkShaderStage.FRAGMENT, 0, (uint)Unsafe.SizeOf<PerCommandData>(), &commandData);

            PixelBuffer commandTexture = command.Texture ?? fontTextures[0];

            VkDescriptorImageInfo descriptorImageInfo = new()
            {
                ImageView = commandTexture.View,
                ImageLayout = VkImageLayout.SHADER_READ_ONLY_OPTIMAL,
            };
            descriptorWrites[0] = new(default)
            {
                DestinationBinding = 0,
                DescriptorType = VkDescriptorType.SAMPLED_IMAGE,
                DescriptorCount = 1,
                ImageInfo = &descriptorImageInfo,
            };

            VkDescriptorImageInfo descriptorImageInfo2 = new()
            {
                Sampler = drawSampler,
            };
            descriptorWrites[1] = new(default)
            {
                DestinationBinding = 1,
                DescriptorType = VkDescriptorType.SAMPLER,
                DescriptorCount = 1,
                ImageInfo = &descriptorImageInfo2,
            };

            vkCmdPushDescriptorSet(commandBuffer, VkPipelineBindPoint.GRAPHICS, activePipeline.Layout, 0, 2, descriptorWrites);
            vkCmdDrawIndexed(commandBuffer, command.IndexCount, 1, command.IndexOffset, 0, 0);
        }

        vkCmdEndRendering(commandBuffer);

        commandsCount = 0;
    }

    void DestroyRendering()
    {
        gfx.DestroyPixelBuffer(backBuffer);
        backBuffer = null;

        gfx.DestroyPipeline(pipeline);
        pipeline = null;

        gfx.DestroyPipeline(compositionPipeline);
        compositionPipeline = null;

        renderingInitialized = false;
    }

    public void InitializeRendering(DisplayParameters parameters)
    {
        this.parameters = parameters;

        DestroyRendering();

        var par = new GfxPipelineParameters(
            ShaderProgram: "Overlay",
            PushConstants: [
                new()
                {
                    StageFlags = VkShaderStage.VERTEX | VkShaderStage.FRAGMENT,
                    Size = (uint)Marshal.SizeOf<PerCommandData>()
                }
            ],
            Layout: [
                new()
                {
                    Binding = 0,
                    DescriptorCount = 1,
                    DescriptorType = VkDescriptorType.SAMPLED_IMAGE,
                    Stages = VkShaderStage.FRAGMENT
                },
                new()
                {
                    Binding = 1,
                    DescriptorCount = 1,
                    DescriptorType = VkDescriptorType.SAMPLER,
                    Stages = VkShaderStage.FRAGMENT
                }
            ],
            InputTopology: VkPrimitiveTopology.TRIANGLE_LIST,
            CullMode: VkCullMode.NONE,
            FrontFace: VkFrontFace.COUNTER_CLOCKWISE,
            DepthTest: false,
            DepthWrite: false,
            DepthCompareOp: VkCompareOp.ALWAYS,
            Multisampling: false,
            InputBindings: [
                new()
                {
                    Stride = (uint)Marshal.SizeOf<Vertex2D>(),
                    InputRate = VkVertexInputRate.VERTEX
                },
            ],
            InputAttributes: [
                new()
                {
                    Location = 0,
                    Format = VkFormat.R32G32_SFLOAT,
                    Offset = (uint)Marshal.OffsetOf<Vertex2D>(nameof(Vertex2D.Position))
                },
                new()
                {
                    Location = 1,
                    Format = VkFormat.R32G32_SFLOAT,
                    Offset = (uint)Marshal.OffsetOf<Vertex2D>(nameof(Vertex2D.UV))
                },
                new()
                {
                    Location = 2,
                    Format = VkFormat.R32G32B32A32_SFLOAT,
                    Offset = (uint)Marshal.OffsetOf<Vertex2D>(nameof(Vertex2D.Color))
                },
            ],
            Attachments: [
                new()
                {
                    Format = VkFormat.B8G8R8A8_UNORM,
                    Blend = new()
                    {
                        BlendEnable  = 1,
                        // Premultiplied alpha
                        SrcColorBlendFactor = VkBlendFactor.ONE,
                        DstColorBlendFactor = VkBlendFactor.ONE_MINUS_SRC_ALPHA,
                        ColorBlendOp = VkBlendOp.ADD,
                        SrcAlphaBlendFactor = VkBlendFactor.ONE,
                        DstAlphaBlendFactor = VkBlendFactor.ONE_MINUS_SRC_ALPHA,
                        AlphaBlendOp = VkBlendOp.ADD,
                        ColorWriteMask = VkColorComponent.R | VkColorComponent.G | VkColorComponent.B | VkColorComponent.A
                    }
                }
            ]
        );

        pipeline = gfx.CreatePipeline(par);

        compositionPipeline = gfx.CreatePipeline(new GfxPipelineParameters(
            ShaderProgram: "Composition",
            PushConstants: [],
            Layout: [
                new()
                {
                    Binding = 0,
                    DescriptorCount = 1,
                    DescriptorType = VkDescriptorType.SAMPLED_IMAGE,
                    Stages = VkShaderStage.FRAGMENT
                },
                new()
                {
                    Binding = 1,
                    DescriptorCount = 1,
                    DescriptorType = VkDescriptorType.SAMPLER,
                    Stages = VkShaderStage.FRAGMENT
                }
            ],
            InputTopology: VkPrimitiveTopology.TRIANGLE_LIST,
            CullMode: VkCullMode.NONE,
            FrontFace: VkFrontFace.COUNTER_CLOCKWISE,
            DepthTest: false,
            DepthWrite: false,
            DepthCompareOp: VkCompareOp.ALWAYS,
            Multisampling: false,
            InputBindings: [],
            InputAttributes: [],
            Attachments: [
                new()
                {
                    Format = VkFormat.B8G8R8A8_UNORM,
                    Blend = new()
                    {
                        BlendEnable  = 1,
                        // Premultiplied alpha
                        SrcColorBlendFactor = VkBlendFactor.ONE,
                        DstColorBlendFactor = VkBlendFactor.ONE_MINUS_SRC_ALPHA,
                        ColorBlendOp = VkBlendOp.ADD,
                        SrcAlphaBlendFactor = VkBlendFactor.ONE,
                        DstAlphaBlendFactor = VkBlendFactor.ONE_MINUS_SRC_ALPHA,
                        AlphaBlendOp = VkBlendOp.ADD,
                        ColorWriteMask = VkColorComponent.R | VkColorComponent.G | VkColorComponent.B | VkColorComponent.A
                    }
                }
            ]
        ));

        renderingInitialized = true;
    }

    readonly ConcurrentObjectPool<TextLayout> textLayoutPool = new(initialCapacity: 16);

    int scratchRunesCount;
    readonly uint[] scratchRunes = new uint[1024];
    readonly Range[] scratchWordRanges = new Range[1024];

    public void DrawText(TextLayout layout, Vector2 position, Color color, Color backgroundColor)
    {
        Assert(frameInProgress && batchInProgress);

        ref Command newCommand = ref BeginCommand();
        newCommand.Font = layout.Font;
        newCommand.Color = backgroundColor.ToVector3();

        for (int i = 0; i < layout.GlyphCount; i++)
        {
            TextLayout.LayoutGlyph glyph = layout.Glyphs[i];
            Vector2 a = position + glyph.Position;
            Vector2 c = a + glyph.Size;

            PushQuadUV(a, c, glyph.UV0, glyph.UV1, color);
        }
    }

    public void DestroyTextLayout(TextLayout layout)
    {
        layout.Reset();
        textLayoutPool.Return(layout);
    }

    public TextLayout CreateTextLayout(ReadOnlySpan<char> text, Vector2 availableSize, bool wordWrap, GfxFont? font = default)
    {
        const float glyphHorizontalPadding = 4;

        Assert(scratchRunesCount == 0);

        font ??= fontCache.GetDefaultFont();
        GfxFont.Metrics fontMetrics = font.GetMetrics();

        TextLayout layout = textLayoutPool.Rent();
        layout.Font = font;

        float totalWidth = 0;
        float totalHeight = 0;

        float cursorY = fontMetrics.Ascender;

        int wordCountOnLine = 0;

        Rune spaceRune = new(' ');
        float spaceRuneWidth = font.GetGlyph((uint)spaceRune.Value).Width;

        Vector2 MeasureText(ReadOnlySpan<char> word)
        {
            float width = 0;
            float height = fontMetrics.Height;

            foreach (var rune in word.EnumerateRunes())
            {
                Glyph glyph = font.GetGlyph((uint)rune.Value);

                width += glyph.Width + glyphHorizontalPadding;
            }

            return new(width, height);
        }

        foreach (var line in text.EnumerateLines())
        {
            float remainingWidth = availableSize.X;

            int wordCount = line.Split(scratchWordRanges.AsSpan(), ' ', StringSplitOptions.None);
            ThrowNotSupportedIf(wordCount >= scratchWordRanges.Length, "Too many words in a line");

            for (int i = 0; i < wordCount; i++)
            {
                Range wordRange = scratchWordRanges[i];
                ReadOnlySpan<char> word = line[wordRange];

                Vector2 wordSize = MeasureText(word);

                if (i > 0)
                {
                    wordSize.X += spaceRuneWidth;
                }

                if (wordWrap && remainingWidth < wordSize.X)
                {
                    if (wordCountOnLine > 0)
                    {
                        ConsumeRunes(font, layout, cursorY);

                        remainingWidth = availableSize.X;
                        cursorY += fontMetrics.Height;

                        foreach (var rune in word.EnumerateRunes())
                        {
                            scratchRunes[scratchRunesCount++] = (uint)rune.Value;
                        }

                        wordCountOnLine = 1;
                        remainingWidth -= wordSize.X;
                    }
                    else
                    {
                        if (i > 0)
                            scratchRunes[scratchRunesCount++] = (uint)spaceRune.Value;
                        foreach (var rune in word.EnumerateRunes())
                        {
                            scratchRunes[scratchRunesCount++] = (uint)rune.Value;
                        }
                        ConsumeRunes(font, layout, cursorY);

                        remainingWidth = availableSize.X;
                        cursorY += fontMetrics.Height;
                        wordCountOnLine = 0;
                    }
                }
                else
                {
                    if (i > 0)
                        scratchRunes[scratchRunesCount++] = (uint)spaceRune.Value;
                    foreach (var rune in word.EnumerateRunes())
                    {
                        scratchRunes[scratchRunesCount++] = (uint)rune.Value;
                    }

                    remainingWidth -= wordSize.X;
                    wordCountOnLine++;
                }
            }

            if (scratchRunesCount > 0)
            {
                ConsumeRunes(font, layout, cursorY);
            }

            cursorY += fontMetrics.Height;
        }

        layout.Size = new(totalWidth, totalHeight);
        return layout;

        void ConsumeRunes(GfxFont font, TextLayout layout, float cursorY)
        {
            float cursorX = 0;

            foreach (ShapedGlyph shapedGlyph in font.ShapeText(scratchRunes.AsSpan(0, scratchRunesCount)))
            {
                float x = cursorX + shapedGlyph.XOffset + shapedGlyph.Glyph.BearingX;
                float y = cursorY + shapedGlyph.YOffset - shapedGlyph.Glyph.BearingY;

                // Expand total size of the layout based on the bottom right corner of the glyph
                float farX = x + shapedGlyph.Glyph.Width;
                float farY = y + shapedGlyph.Glyph.Height;
                totalWidth = Math.Max(totalWidth, farX);
                totalHeight = Math.Max(totalHeight, farY);

                bool isWithinBounds = x <= availableSize.X && y <= availableSize.Y;

                if (isWithinBounds)
                {
                    TextLayout.LayoutGlyph layoutGlyph = new(
                        new(x, y),
                        new(shapedGlyph.Glyph.Width, shapedGlyph.Glyph.Height),
                        new(shapedGlyph.Glyph.U0, shapedGlyph.Glyph.V0),
                        new(shapedGlyph.Glyph.U1, shapedGlyph.Glyph.V1),
                        Color.RedOrange);

                    layout.Append(layoutGlyph);
                }

                cursorX += shapedGlyph.XAdvance;
                if (shapedGlyph.XAdvance == 0)
                {
                    cursorX += shapedGlyph.Glyph.Width;
                }
            }

            scratchRunesCount = 0;
        }
    }

    public void DrawText(ReadOnlySpan<char> text, Vector2 position, Color color, Color backgroundColor, GfxFont? font = default)
    {
        font ??= fontCache.GetDefaultFont();

        ref Command newCommand = ref BeginCommand();
        newCommand.Font = font;
        newCommand.Color = backgroundColor.ToVector3();

        float cursorX = position.X;
        float cursorY = position.Y + font.GetMetrics().Ascender;

        foreach (ShapedGlyph shapedGlyph in font.ShapeText(text))
        {
            Glyph glyph = shapedGlyph.Glyph;

            float x = cursorX + shapedGlyph.XOffset + glyph.BearingX;
            float y = cursorY + shapedGlyph.YOffset - glyph.BearingY;

            PushQuadUV(new(x, y), new(x + glyph.Width, y + glyph.Height), new(glyph.U0, glyph.V0), new(glyph.U1, glyph.V1), color);

            cursorX += shapedGlyph.XAdvance;
            cursorY += shapedGlyph.YAdvance;

            if (shapedGlyph.XAdvance == 0)
            {
                cursorX += glyph.Width;
            }
        }
    }

    public void DrawCircle(Vector2 center, float radius, Color color, float thickness = 1.0f, bool useAntialiasing = true)
    {
        Assert(frameInProgress && batchInProgress);

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

    public void DrawRectangle(Vector2 a, Vector2 c, Color color, float thickness = 1.0f)
    {
        float half_thickness = thickness * 0.5f;

        PushQuadUV(new(a.X - half_thickness, a.Y - half_thickness), new(c.X + half_thickness, a.Y + half_thickness), Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
        PushQuadUV(new(a.X - half_thickness, c.Y - half_thickness), new(c.X + half_thickness, c.Y + half_thickness), Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
        PushQuadUV(new(a.X - half_thickness, a.Y + half_thickness), new(a.X + half_thickness, c.Y - half_thickness), Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
        PushQuadUV(new(c.X - half_thickness, a.Y + half_thickness), new(c.X + half_thickness, c.Y - half_thickness), Vertex2D.DefaultUV, Vertex2D.DefaultUV, color);
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