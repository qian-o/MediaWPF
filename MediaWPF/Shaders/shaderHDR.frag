#version 330

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D tex_y;
uniform sampler2D tex_u;
uniform sampler2D tex_v;

uniform int isConvert;
uniform float toneP1;
uniform float toneP2;
uniform float contrast;
uniform float brightness;

const mat4 YUV_TO_RGB_MATRIX = mat4(
    1.167808f, -0.000000f,  1.683611f, -0.915688f,
    1.167808f, -0.187877f, -0.652337f,  0.347458f,
    1.167808f,  2.148072f, -0.000000f, -1.148145f,
    0.000000f,  0.000000f,  0.000000f,  1.000000f);

const mat4 BT2020_BT709_MATRIX = mat4(
     1.660490f, -0.587641f, -0.072849f,  0.000000f,
    -0.124550f,  1.132899f, -0.008349f,  0.000000f,
    -0.018150f, -0.100578f,  1.118729f,  0.000000f,
     0.000000f,  0.000000f,  0.000000f,  1.000000f);

// HDR to SDR color convert (Thanks to KODI community https://github.com/thexai/xbmc)
const float ST2084_m1 = 2610.0f / (4096.0f * 4.0f);
const float ST2084_m2 = (2523.0f / 4096.0f) * 128.0f;
const float ST2084_c1 = 3424.0f / 4096.0f;
const float ST2084_c2 = (2413.0f / 4096.0f) * 32.0f;
const float ST2084_c3 = (2392.0f / 4096.0f) * 32.0f;

vec3 inversePQ(vec3 x)
{
    x = pow(max(x, vec3(0.0f)), vec3(1.0f / ST2084_m2));
    x = max(x - ST2084_c1, 0.0f) / (ST2084_c2 - ST2084_c3 * x);
    x = pow(x, vec3(1.0f / ST2084_m1));
    return x;
}

vec3 hable(vec3 x)
{
    const float A = 0.15f;
    const float B = 0.5f;
    const float C = 0.1f;
    const float D = 0.2f;
    const float E = 0.02f;
    const float F = 0.3f;
    return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
}

void main()
{
    vec4 yuv;
    vec4 color;
    yuv.x = texture(tex_y, texCoord).x;
    yuv.y = texture(tex_u, texCoord).x;
    yuv.z = texture(tex_v, texCoord).x;
    yuv = yuv * 64.0f;
    yuv.w = 1.0f;
    color = yuv * YUV_TO_RGB_MATRIX * BT2020_BT709_MATRIX;

    if (isConvert == 1)
    {
        color.xyz = inversePQ(color.xyz);
        color.xyz *= toneP1;
        color.xyz = hable(color.xyz * vec3(toneP2)) / hable(vec3(toneP2));
        color.xyz = pow(color.xyz, vec3(1.0f / 2.2f));
    }

    color *= contrast * 2.0f;
    color += brightness - 0.5f;

    outputColor = color;
}