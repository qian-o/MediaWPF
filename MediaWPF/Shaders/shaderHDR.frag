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

void main()
{
    vec3 yuv;
    vec3 rgb; 
    yuv.x = texture(tex_y, texCoord).x * 56636 / 4;
    yuv.y = texture(tex_u, texCoord).x * 56636 / 4;
    yuv.z = texture(tex_v, texCoord).x * 56636 / 4;
    rgb.x = yuv.x + 1.4746 * (yuv.z - 128);
    rgb.y = yuv.x - (0.1645 * (yuv.y - 128) - 0.5713 * (yuv.z - 128));
    rgb.z = yuv.x + (1.881 * (yuv.y - 128));
    rgb = rgb / 256;
    outputColor = vec4(rgb, 1.0);
}