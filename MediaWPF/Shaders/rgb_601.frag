#version 330

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D tex_y;
uniform sampler2D tex_u;
uniform sampler2D tex_v;

void main()
{
    vec3 yuv;
    vec3 rgb;
    yuv.x = texture(tex_y, texCoord).r;
    yuv.y = texture(tex_u, texCoord).r - 0.5;
    yuv.z = texture(tex_v, texCoord).r - 0.5;
    rgb = mat3( 1.164, 1.164, 1.164,
                0.0, -0.392, 2.016,
                1.596, -0.812, 0.0) * yuv;
    outputColor = vec4(rgb, 1);
}