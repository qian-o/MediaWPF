#version 330

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D tex_y;
uniform sampler2D tex_u;
uniform sampler2D tex_v;

const mat4 YUV_TO_RGB_MATRIX = mat4(
    1.1644, 0, 1.6853, -234.3559,
    1.1644, -0.1881, -0.6529, 89.0206,
    1.1646, 2.1501, 0.0000, -293.8542,
    0.0000, 0.0000, 0.0000, 1.0000);

const mat3 YUV2020_TO_YUV709_MATRIX = mat3(
    1.6605, -0.5876, -0.0728,
    -0.1246, 1.1329, -0.0083,
    -0.0182, -0.1006, 1.1187);

void main()
{
    vec3 yuv;
    yuv.x = texture2D(tex_y, texCoord).x;
    yuv.y = texture2D(tex_u, texCoord).x;
    yuv.z = texture2D(tex_v, texCoord).x;
    outputColor = vec4(yuv, 1.0) * YUV_TO_RGB_MATRIX;
}