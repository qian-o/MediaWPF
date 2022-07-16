#version 330

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D tex_y;
uniform sampler2D tex_u;
uniform sampler2D tex_v;

const mat4 YUV_TO_RGB_MATRIX = mat4(
    1.164384f, -0.000000f,  1.792741f, -0.972945f,
    1.164384f, -0.213249f, -0.532909f,  0.301483f,
    1.164384f,  2.112402f, -0.000000f, -1.133402f,
    0.000000f,  0.000000f,  0.000000f,  1.000000f);

void main()
{
    vec4 yuv;
    yuv.x = texture(tex_y, texCoord).x;
    yuv.y = texture(tex_u, texCoord).x;
    yuv.z = texture(tex_v, texCoord).x;
    yuv.w = 1.0f;
    outputColor = yuv * YUV_TO_RGB_MATRIX;
}