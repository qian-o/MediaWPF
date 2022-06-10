#version 330

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D tex_y;
uniform sampler2D tex_u;
uniform sampler2D tex_v;

const mat4 YUV_TO_RGB_MATRIX = mat4(
    1.167808f * 64, -0.000000f * 64,  1.683611f * 64, -0.915688f,
    1.167808f * 64, -0.187877f * 64, -0.652337f * 64,  0.347458f,
    1.167808f * 64,  2.148072f * 64, -0.000000f * 64, -1.148145f,
    0.000000f * 64,  0.000000f * 64,  0.000000f * 64,  1.000000f);

void main()
{
    vec3 yuv;
    yuv.x = texture(tex_y, texCoord).x;
    yuv.y = texture(tex_u, texCoord).x;
    yuv.z = texture(tex_v, texCoord).x;
    outputColor = vec4(yuv, 1.0) * YUV_TO_RGB_MATRIX;
}