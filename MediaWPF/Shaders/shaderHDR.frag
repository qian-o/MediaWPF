#version 330

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D tex_y;
uniform sampler2D tex_u;
uniform sampler2D tex_v;

const mat4 YUV_TO_RGB_MATRIX = mat4(
    1.1643835616, 0, 1.7927410714, -0.9729450750,
    1.1643835616, -0.2132486143, -0.5329093286, 0.3014826655,
    1.1643835616, 2.1124017857, 0, -1.1334022179,
    0, 0, 0, 1);

const mat3 YUV2020_TO_YUV709_MATRIX = mat3(
    1.6605, -0.5876, -0.0728,
    -0.1246, 1.1329, -0.0083,
    -0.0182, -0.1006, 1.1187);

void main()
{
    vec3 yuv2020;
    vec3 yuv709;
    yuv2020.x = texture2D(tex_y, texCoord).x;
    yuv2020.y = texture2D(tex_u, texCoord).x;
    yuv2020.z = texture2D(tex_v, texCoord).x;
    yuv709 = yuv2020 * YUV2020_TO_YUV709_MATRIX;
    vec4 rgba = vec4(yuv2020, 1.0) * YUV_TO_RGB_MATRIX;
    vec3 rgb = vec3(rgba.x,rgba.y,rgba.z)* YUV2020_TO_YUV709_MATRIX;
    outputColor = vec4(rgb, 1.0);
}