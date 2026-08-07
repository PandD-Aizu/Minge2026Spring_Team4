// SavePointの点灯フレーム専用ピクセルシェーダー
Texture2D       g_texture0 : register(t0);
SamplerState    g_sampler0 : register(s0);

namespace s3d
{
	struct PSInput
	{
		float4 position : SV_POSITION;
		float4 color    : COLOR0;
		float2 uv       : TEXCOORD0;
	};
}

cbuffer PSConstants2D : register(b0)
{
	float4 g_colorAdd;
	float4 g_sdfParam;
	float4 g_sdfOutlineColor;
	float4 g_sdfShadowColor;
	float4 g_internal;
}

cbuffer SavePointGlow : register(b1)
{
	float4 g_glow; // x: 経過時間, y: 発光強度, z: 発光のみの描画
}

float LightMask(float3 color)
{
	const float brightness = dot(color, float3(0.2126, 0.7152, 0.0722));
	const float maxChannel = max(color.r, max(color.g, color.b));
	const float minChannel = min(color.r, min(color.g, color.b));
	const float saturation = maxChannel - minChannel;

	// 点灯部分は青とシアン
	return smoothstep(0.45, 0.85, brightness)
		* smoothstep(0.08, 0.35, saturation);
}

float4 PS(s3d::PSInput input) : SV_TARGET
{
	const float4 texColor = g_texture0.Sample(g_sampler0, input.uv);
	const float2 texel = float2(1.0 / 32.0, 1.0 / 32.0);

	float glow = 0.0;
	[unroll]
	for (int y = -2; y <= 2; ++y)
	{
		[unroll]
		for (int x = -2; x <= 2; ++x)
		{
			const float2 offset = float2(x, y) * texel;
			const float3 sampleColor = g_texture0.Sample(g_sampler0, input.uv + offset).rgb;
			const float distanceWeight = 1.0 - (length(float2(x, y)) / 3.0);
			glow += LightMask(sampleColor) * max(distanceWeight, 0.0);
		}
	}

	const float pulse = 0.85 + 0.25 * sin(g_glow.x * 8.0);
	const float glowAmount = saturate(glow / 2.5) * g_glow.y * pulse;

	const float2 lampCenter = (g_glow.z > 0.5) ? float2(0.75, 0.50) : float2(0.75, 0.30);
	const float2 lampDistance = (input.uv - lampCenter) * float2(2.0, 1.0);
	const float lampRadius = length(lampDistance);
	const float phase = frac(g_glow.x * 1.35);
	const float spread = 1.0 - abs(phase * 2.0 - 1.0);
	const float sunRadius = lerp(0.06, 0.48, spread);
	const float sunDisk = 1.0 - smoothstep(sunRadius * 0.65, sunRadius, lampRadius);
	const float centerFlare = 1.0 - smoothstep(0.0, 0.20, lampRadius);

	const float angle = atan2(lampDistance.y, lampDistance.x);
	const float rays = pow(saturate(0.5 + 0.5 * cos(angle * 12.0 + g_glow.x * 1.5)), 5.0);
	const float radialGlow = sunDisk * (0.70 + rays * 0.30) + centerFlare * 0.35;
	const float3 glowColor = float3(0.25, 0.75, 1.0) * (glowAmount + radialGlow * g_glow.y);

	if (g_glow.z > 0.5)
	{
		const float3 sunColor = float3(0.10, 0.65, 1.0) * (0.45 + radialGlow * 1.35);
		return float4(sunColor, saturate(radialGlow * 0.72));
	}

	float4 result = texColor * input.color;
	result.rgb += glowColor;
	result.a = max(result.a, saturate(glowAmount * 0.55 + radialGlow * g_glow.y * 0.35));
	return result + g_colorAdd;
}
