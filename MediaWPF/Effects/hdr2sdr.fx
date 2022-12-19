sampler2D input : register(s0);

float toneP1 : register(C1);
float toneP2 : register(C2);
float contrast : register(C3);
float brightness : register(C4);

// HDR to SDR color convert (Thanks to KODI community https://github.com/thexai/xbmc)
static const float ST2084_m1 = 2610.0f / (4096.0f * 4.0f);
static const float ST2084_m2 = (2523.0f / 4096.0f) * 128.0f;
static const float ST2084_c1 = 3424.0f / 4096.0f;
static const float ST2084_c2 = (2413.0f / 4096.0f) * 32.0f;
static const float ST2084_c3 = (2392.0f / 4096.0f) * 32.0f;

static const float4x4 bt2020tobt709color =
{
	{  1.660490f, -0.124550f, -0.018150f, 0.000000f },
	{ -0.587641f,  1.132899f, -0.100578f, 0.000000f },
	{ -0.072849f, -0.008349f,  1.118729f, 0.000000f },
	{  0.000000f,  0.000000f,  0.000000f, 1.000000f }
};

float3 inversePQ(float3 x)
{
	x = pow(max(x, 0.0f), 1.0f / ST2084_m2);
	x = max(x - ST2084_c1, 0.0f) / (ST2084_c2 - ST2084_c3 * x);
	x = pow(x, 1.0f / ST2084_m1);
	return x;
}

float3 hable(float3 x)
{
	const float A = 0.15f;
	const float B = 0.5f;
	const float C = 0.1f;
	const float D = 0.2f;
	const float E = 0.02f;
	const float F = 0.3f;
	return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
}

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 color;
	color = tex2D(input , uv.xy);

	// BT2020 -> BT709
	color.rgb = pow(max(0.0, color.rgb), 2.4f);
	color.rgb = max(0.0, mul(color, bt2020tobt709color).rgb);
	color.rgb = pow(color.rgb, 1.0f / 2.2f);

	color.rgb = inversePQ(color.rgb);
	color.rgb *= toneP1;
	color.rgb = hable(color.rgb * toneP2) / hable(toneP2);
	color.rgb = pow(color.rgb, 1.0f / 2.2f);

	color *= contrast * 2.0f;
	color += brightness - 0.5f;

	return color;
}