struct PerCommand
{
	float2 Scale;
	float2 Translation;
	float3 Color;
};

[[vk::push_constant]] PerCommand command;

[[vk::binding(0, 0)]] Texture2D commandTexture;
[[vk::binding(1, 0)]] SamplerState commandSampler;

struct vertex_input
{
	float2 Position : POSITION0;
	float2 UV : TEXCOORD0;
	float4 Color : COLOR0;
};

struct fragment_input
{
	float4 Position : SV_POSITION;
	float2 UV : TEXCOORD0;
	float4 Color : COLOR0;
};

[shader("vertex")]
fragment_input vertex(vertex_input input, in uint vertexIndex : SV_VertexID)
{
    fragment_input output = (fragment_input)0;
    output.Position = float4(input.Position * command.Scale + command.Translation, 0.0, 1.0);
	output.UV = input.UV;
	output.Color = input.Color;
	return output;
}

[shader("pixel")]
float4 fragment(fragment_input input) : SV_TARGET
{
	float4 geometryColor = input.Color;
	float4 textureColor = commandTexture.Sample(commandSampler, input.UV);

    float r = textureColor.r * geometryColor.r * geometryColor.a + command.Color.r * (1.0 - textureColor.r * geometryColor.a);
    float g = textureColor.g * geometryColor.g * geometryColor.a + command.Color.g * (1.0 - textureColor.g * geometryColor.a);
    float b = textureColor.b * geometryColor.b * geometryColor.a + command.Color.b * (1.0 - textureColor.b * geometryColor.a);
	float a = all(textureColor.rgb == float3(0, 0, 0)) ? 0 : textureColor.a * geometryColor.a;

	return float4(r, g, b, a);
}