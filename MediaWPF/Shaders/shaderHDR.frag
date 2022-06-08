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
    yuv.x = texture2D(tex_y, texCoord).x;
    yuv.y = texture2D(tex_u, texCoord).x;
    yuv.z = texture2D(tex_v, texCoord).x;
    outputColor = vec4(yuv, 1.0) * YUV_TO_RGB_MATRIX;
}