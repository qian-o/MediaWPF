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

const mat3 BT2020_BT709 = mat3(
     1.6605, -0.5876, -0.0728,
    -0.1246,  1.1329, -0.0083,
    -0.0182, -0.1006,  1.1187);

void main()
{
    vec3 yuv;
    yuv.x = texture(tex_y, texCoord).x;
    yuv.y = texture(tex_u, texCoord).x;
    yuv.z = texture(tex_v, texCoord).x;
    vec3 rgb = (vec4(yuv, 1.0) * YUV_TO_RGB_MATRIX).xyz * BT2020_BT709;
    outputColor = vec4(rgb, 1);
}