#version 330

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D tex_y;
uniform sampler2D tex_u;
uniform sampler2D tex_v;

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

void main()
{
    vec4 yuv;
    yuv.x = texture(tex_y, texCoord).x;
    yuv.y = texture(tex_u, texCoord).x;
    yuv.z = texture(tex_v, texCoord).x;
    yuv = yuv * 64;
    yuv.w = 1.0f;
    outputColor = yuv * YUV_TO_RGB_MATRIX * BT2020_BT709_MATRIX;
}