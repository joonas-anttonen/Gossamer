using Gossamer.External.Vulkan;
using Gossamer.Utilities;

using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Backend.Shaders;

record ShaderPackage(Dictionary<string, ShaderPackage.ShaderProgram> Pipelines)
{
    public record ShaderStage(uint Stage, string EntryPoint, long Offset, long Size);
    public record ShaderProgram(string Name, ShaderStage[] Stages);

    /// <summary>
    /// Deserializes a shader package from a stream.
    /// </summary>
    /// <param name="stream"/>
    /// <exception cref="InvalidDataException"/>
    public static Dictionary<string, GfxPipelineShader> Deserialize(Stream stream)
    {
        using BinaryReader reader = new(stream);

        // Json chunk
        uint jsonChunkLength = reader.ReadUInt32();
        uint jsonChunkType = reader.ReadUInt32();
        ThrowInvalidDataIf(jsonChunkType != 1, "Invalid json chunk type.");

        byte[] jsonChunkData = reader.ReadBytes((int)jsonChunkLength);

        // Bytecode chunk
        uint bytecodeChunkLength = reader.ReadUInt32();
        uint bytecodeChunkType = reader.ReadUInt32();
        ThrowInvalidDataIf(bytecodeChunkType != 2, "Invalid bytecode chunk type.");

        ShaderPackage packageDefinition = JsonUtilities.Deserialize<ShaderPackage>(jsonChunkData);
        byte[] packageBytecode = reader.ReadBytes((int)bytecodeChunkLength);

        Dictionary<string, GfxPipelineShader> shaderPrograms = new(capacity: packageDefinition.Pipelines.Count);
        foreach (var (shaderProgramName, shaderProgramDefinition) in packageDefinition.Pipelines)
        {
            GfxPipelineShader.Stage[] stages = new GfxPipelineShader.Stage[shaderProgramDefinition.Stages.Length];

            for (int i = 0; i < shaderProgramDefinition.Stages.Length; i++)
            {
                ShaderStage stageDefinition = shaderProgramDefinition.Stages[i];
                byte[] stageBytecode = new byte[stageDefinition.Size];
                Array.Copy(packageBytecode, stageDefinition.Offset, stageBytecode, 0, stageDefinition.Size);

                stages[i] = new GfxPipelineShader.Stage(
                    StageType: (VkShaderStage)stageDefinition.Stage,
                    Entrypoint: new SafeNativeString(stageDefinition.EntryPoint),
                    Code: stageBytecode);
            }

            shaderPrograms[shaderProgramName] = new GfxPipelineShader(shaderProgramName, stages);
        }

        return shaderPrograms;
    }
}