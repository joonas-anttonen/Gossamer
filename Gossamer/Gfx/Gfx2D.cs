using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Gossamer.Collections;
using Gossamer.External.Vulkan;
using Gossamer.Gfx.Presentation;
using Gossamer.Gfx.Text;
using Gossamer.Logging;
using Gossamer.Utilities;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Gfx;

[StructLayout(LayoutKind.Sequential)]
readonly struct Vertex2D(Vector2 position, Vector2 uv, Color color)
{
    public static readonly Vector2 DefaultUV = new(-1, -1);

    public readonly Vector2 Position = position;
    public readonly Vector2 UV = uv;
    public readonly Vector4 Color = color.ToVector4();
}

[StructLayout(LayoutKind.Sequential)]
readonly struct PerCommandData(Vector2 scale, Vector2 translation)
{
    public readonly Vector2 Scale = scale;
    public readonly Vector2 Translation = translation;
}

class Gfx2D(GfxCore gfx) : IDisposable
{
    public const int MaxVertices = 65536;

    public readonly record struct Statistics(int Commands, int Triangles);

    readonly Logger logger = Core.GetLogger(nameof(Gfx2D));

    readonly GfxCore gfx = gfx;

    readonly FontCollection fontCache = new();

    DisplayParameters? parameters;

    VkSampler nearestSampler;
    VkSampler linearSampler;

    GfxPipeline? mainPipeline;
    GfxPipeline? compositionPipeline;

    PixelBuffer? backBuffer;

    MemoryBuffer<Vertex2D>? vertexBuffer;
    MemoryBuffer<ushort>? indexBuffer;

    readonly Lock fontTexturesLock = new();
    PixelBuffer[] fontTextures = [];

    readonly ConcurrentObjectPool<Gfx2DCommandBuffer> commandBufferPool = new(initialCapacity: 2);
    readonly ConcurrentQueue<Gfx2DCommandBuffer> commandBufferQueue = new();
    Gfx2DCommandBuffer? currentCommandBuffer;

    Statistics frameStatistics;

    /// <summary>
    /// Gets the built-in font that is always available.
    /// </summary>
    public Font GetBuiltInFont()
    {
        return fontCache.GetBuiltInFont();
    }

    /// <summary>
    /// Gets a font by name and size. 
    /// If the font is not found, there is an attempt to create it. 
    /// If the font cannot be created, the built-in font is returned.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="size"></param>
    public Font GetFont(string name, int size)
    {
        if (!fontCache.TryGetFontOrDefault(name, size, out Font? font))
        {
            if (fontCache.TryCreateFont(name, size, out font))
            {
                InitializeFont(ThrowInvalidDataIfNull(font));
            }
            else
            {
                logger.Warning($"{name} not found and could not be created");
                font = GetBuiltInFont();
            }
        }

        return font ?? GetBuiltInFont();
    }

    /// <summary>
    /// Returns the statistics of the last rendered frame.
    /// </summary>
    public Statistics GetStatistics()
    {
        return frameStatistics;
    }

    /// <summary>
    /// Returns a free command buffer from the pool.
    /// </summary>
    public Gfx2DCommandBuffer GetCommandBuffer()
    {
        return commandBufferPool.Rent();
    }

    /// <summary>
    /// Submits a command buffer to the queue for processing.
    /// </summary>
    /// <param name="commandBuffer"></param>
    public void SubmitCommandBuffer(Gfx2DCommandBuffer commandBuffer)
    {
        commandBufferQueue.Enqueue(commandBuffer);
    }

    // FIXME: Fonts are being loaded in other threads, this is not thread safe at the moment. Simultanenous vkQueue use!
    void InitializeFont(Font font)
    {
        Font.Atlas fontAtlas = font.GetAtlas();

        PixelBuffer fontTexture = gfx.CreatePixelBuffer(
            fontAtlas.Pixels,
            width: (uint)fontAtlas.Size,
            height: (uint)fontAtlas.Size,
            format: GfxFormat.Rgba8,
            usage: GfxPixelBufferUsage.Sampled);

        using (fontTexturesLock.EnterScope())
        {
            ArrayUtilities.Append(ref fontTextures, fontTexture);
        }

        // Log font details
        logger.Debug($"{font.Name} [{font.Size}]");
    }

    public void Create()
    {
        ThrowInvalidOperationIf(vertexBuffer != null);
        ThrowInvalidOperationIf(indexBuffer != null);

        vertexBuffer = gfx.CreateDynamicMemoryBuffer<Vertex2D>(length: MaxVertices, GfxMemoryBufferUsage.Vertex);
        indexBuffer = gfx.CreateDynamicMemoryBuffer<ushort>(length: MaxVertices, GfxMemoryBufferUsage.Index);

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
        nearestSampler = gfx.CreateSampler(samplerCreateInfo);

        // Linear
        samplerCreateInfo.MinFilter = VkFilter.LINEAR;
        samplerCreateInfo.MagFilter = VkFilter.LINEAR;
        samplerCreateInfo.MipmapMode = VkSamplerMipmapMode.LINEAR;
        samplerCreateInfo.AddressModeU = VkSamplerAddressMode.CLAMP_TO_EDGE;
        samplerCreateInfo.AddressModeV = VkSamplerAddressMode.CLAMP_TO_EDGE;
        samplerCreateInfo.AddressModeW = VkSamplerAddressMode.CLAMP_TO_EDGE;
        samplerCreateInfo.BorderColor = VkBorderColor.FLOAT_OPAQUE_BLACK;
        linearSampler = gfx.CreateSampler(samplerCreateInfo);

        // Initialize the default font
        InitializeFont(fontCache.GetBuiltInFont());
    }

    public void Dispose()
    {
        DestroyRendering();

        gfx.DestroySampler(nearestSampler);
        nearestSampler = default;

        gfx.DestroySampler(linearSampler);
        linearSampler = default;

        foreach (PixelBuffer fontTexture in fontTextures)
            gfx.DestroyPixelBuffer(fontTexture);
        fontTextures = [];

        gfx.DestroyMemoryBuffer(vertexBuffer);
        vertexBuffer = default;

        gfx.DestroyMemoryBuffer(indexBuffer);
        indexBuffer = default;
    }

    unsafe void PrepareFrame(GfxPresenter presenter)
    {
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

        VkCommandBuffer commandBuffer = presenter.GetCommandBuffer();

        ThrowInvalidOperationIfNull(backBuffer);

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: presentationBuffer,
            srcLayout: VkImageLayout.UNDEFINED,
            dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL);

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

        clearColor = VkClearColorValue.FromColor(Color.Black);
        vkCmdClearColorImage(commandBuffer, presentationBuffer.Image, VkImageLayout.TRANSFER_DST_OPTIMAL, &clearColor, 1, &clearRange);

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: backBuffer,
            srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            dstLayout: VkImageLayout.COLOR_ATTACHMENT_OPTIMAL);
    }

    unsafe void FinishFrame(GfxPresenter presenter)
    {
        VkCommandBuffer commandBuffer = presenter.GetCommandBuffer();
        PixelBuffer presentBuffer = presenter.GetPresentationBuffer();

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: presentBuffer,
            srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            dstLayout: VkImageLayout.COLOR_ATTACHMENT_OPTIMAL);

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: ThrowInvalidOperationIfNull(backBuffer),
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

        ThrowInvalidOperationIfNull(compositionPipeline);
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
            Sampler = nearestSampler,
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

        gfx.FullBarrier(commandBuffer);

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: presentBuffer,
            srcLayout: VkImageLayout.COLOR_ATTACHMENT_OPTIMAL,
            dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL);
    }

    /// <summary>
    /// Renders a frame.
    /// </summary>
    /// <param name="presenter"></param>
    public unsafe void Render(GfxPresenter presenter)
    {
        // Check if we have a pending command buffer to process
        if (commandBufferQueue.TryDequeue(out Gfx2DCommandBuffer? pendingCommandBuffer))
        {
            // If we have a current command buffer, reset it and return it to the pool
            if (currentCommandBuffer != null)
            {
                currentCommandBuffer.Reset();
                commandBufferPool.Return(currentCommandBuffer);
            }

            currentCommandBuffer = pendingCommandBuffer;

            if (!commandBufferQueue.IsEmpty && commandBufferQueue.Count > 1)
            {
                logger.Warning($"Lag [{commandBufferQueue.Count}]");
            }
        }

        PrepareFrame(presenter);

        // Maybe there's no work to do? Usually should only happen during startup.
        if (currentCommandBuffer != null)
        {
            ThrowInvalidOperationIfNull(backBuffer);
            ThrowInvalidOperationIfNull(vertexBuffer);
            ThrowInvalidOperationIfNull(indexBuffer);

            VkCommandBuffer commandBuffer = presenter.GetCommandBuffer();

            gfx.UpdateDynamicBuffer(vertexBuffer, currentCommandBuffer.GetVertices());
            gfx.UpdateDynamicBuffer(indexBuffer, currentCommandBuffer.GetIndices());

            frameStatistics = currentCommandBuffer.GetStatistics();

            ReadOnlySpan<CommandBatch> commandBatches = currentCommandBuffer.GetBatches();
            for (int i = 0; i < commandBatches.Length; i++)
            {
                CommandBatch batch = commandBatches[i];
                currentCommandBuffer.GetBatchData(batch, out ReadOnlySpan<Command> commands);

                RecordBatch(commandBuffer, commands, backBuffer);
            }
        }

        FinishFrame(presenter);
    }

    unsafe void RecordBatch(VkCommandBuffer commandBuffer, ReadOnlySpan<Command> commands, PixelBuffer renderBuffer)
    {
        ThrowInvalidOperationIfNull(backBuffer);
        ThrowInvalidOperationIfNull(mainPipeline);
        ThrowInvalidOperationIfNull(vertexBuffer);
        ThrowInvalidOperationIfNull(indexBuffer);

        GfxPipeline activePipeline = mainPipeline;

        VkRenderingAttachmentInfo colorAttachment = new(default)
        {
            ImageView = renderBuffer.View,
            ImageLayout = VkImageLayout.COLOR_ATTACHMENT_OPTIMAL,
            LoadOp = VkAttachmentLoadOp.LOAD,
            StoreOp = VkAttachmentStoreOp.STORE,
            ClearValue = VkClearValue.FromColor(Color.Transparent),
        };

        VkRenderingAttachmentInfo* colorAttachments = stackalloc VkRenderingAttachmentInfo[1] { colorAttachment };
        VkRenderingInfo renderingInfo = new(default)
        {
            RenderArea = new(new(0, 0), new(renderBuffer.Width, renderBuffer.Height)),
            ColorAttachmentCount = 1,
            ColorAttachments = colorAttachments,
            LayerCount = 1,
        };

        VkViewport viewport = new(0, 0, renderBuffer.Width, renderBuffer.Height, 0, 1);
        VkRect2D scissor = new(new(0, 0), new(renderBuffer.Width, renderBuffer.Height));

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

        for (int i = 0; i < commands.Length; i++)
        {
            ref readonly Command command = ref commands[i];

            commandData = new(
                scale: new(2.0f / renderBuffer.Width, 2.0f / renderBuffer.Height),
                translation: new(-1.0f, -1.0f)
            );
            vkCmdPushConstants(commandBuffer, activePipeline.Layout, VkShaderStage.VERTEX | VkShaderStage.FRAGMENT, 0, (uint)Unsafe.SizeOf<PerCommandData>(), &commandData);

            PixelBuffer commandTexture;
            VkSampler commandSampler;

            using (fontTexturesLock.EnterScope())
            {
                if (command.Texture != null)
                {
                    commandTexture = command.Texture;
                    commandSampler = linearSampler;
                }
                else if (command.Font >= 0)
                {
                    commandTexture = fontTextures[command.Font];
                    commandSampler = nearestSampler;
                }
                else
                {
                    commandTexture = fontTextures[0];
                    commandSampler = nearestSampler;
                }
            }

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
                Sampler = commandSampler,
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

        gfx.FullBarrier(commandBuffer);
    }

    void DestroyRendering()
    {
        gfx.DestroyPixelBuffer(backBuffer);
        backBuffer = null;

        gfx.DestroyPipeline(mainPipeline);
        mainPipeline = null;

        gfx.DestroyPipeline(compositionPipeline);
        compositionPipeline = null;
    }

    public void InitializeRendering(DisplayParameters parameters)
    {
        this.parameters = parameters;

        DestroyRendering();

        VkPipelineColorBlendAttachmentState straightAlphaBlend = new()
        {
            BlendEnable = 1,
            SrcColorBlendFactor = VkBlendFactor.SRC_ALPHA,
            DstColorBlendFactor = VkBlendFactor.ONE_MINUS_SRC_ALPHA,
            ColorBlendOp = VkBlendOp.ADD,
            SrcAlphaBlendFactor = VkBlendFactor.ONE,
            DstAlphaBlendFactor = VkBlendFactor.ONE,
            AlphaBlendOp = VkBlendOp.ADD,
            ColorWriteMask = VkColorComponent.R | VkColorComponent.G | VkColorComponent.B | VkColorComponent.A
        };

        mainPipeline = gfx.CreatePipeline(new GfxPipelineParameters(
            ShaderProgram: gfx.GetShaderProgram("built-in-2d"),
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
                    Blend = straightAlphaBlend,
                }
            ]
        ));

        compositionPipeline = gfx.CreatePipeline(new GfxPipelineParameters(
            ShaderProgram: gfx.GetShaderProgram("built-in-2d-composition"),
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
                    Blend = straightAlphaBlend,
                }
            ]
        ));
    }
}