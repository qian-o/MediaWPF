using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using System.Text;

namespace MediaWPF.Models.OpenGL
{
    public class MediaHDR : MediaBaseModel
    {
        private ushort[] _bufferY, _bufferU, _bufferV;

        public MediaHDR(string file) : base(file, true)
        {

        }

        public override uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, IntPtr pitches, IntPtr lines)
        {
            Console.WriteLine(Marshal.PtrToStringAnsi(chroma));

            byte[] bytes = Encoding.ASCII.GetBytes("I0AL");
            for (int i = 0; i < bytes.Length; i++)
            {
                Marshal.WriteByte(chroma, i, bytes[i]);
            }

            int[] pitche = { (int)width * 2, (int)width, (int)width };
            int[] line = { (int)height, (int)height / 2, (int)height / 2 };

            Marshal.Copy(pitche, 0, pitches, pitche.Length);
            Marshal.Copy(line, 0, lines, pitche.Length);

            _bufferY = new ushort[(int)width * (int)height];
            _bufferU = new ushort[(int)width * (int)height / 4];
            _bufferV = new ushort[(int)width * (int)height / 4];

            sizeY = _bufferY.Length * 2;
            sizeU = _bufferU.Length * 2;
            sizeV = _bufferV.Length * 2;

            planeY = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferY, 0);
            planeU = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferU, 0);
            planeV = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferV, 0);

            return base.VideoFormat(ref opaque, chroma, ref width, ref height, pitches, lines);
        }

        public override void Display()
        {
            if (IsInitTexture)
            {
                // Y
                GL.BindBuffer(BufferTarget.PixelPackBuffer, id_y);
                GL.BufferSubData(BufferTarget.PixelPackBuffer, IntPtr.Zero, sizeY, planeY);
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, id_y);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_y);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, VideoWidth, VideoHeight, PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
                GL.Uniform1(textureUniformY, 0);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

                // U
                GL.BindBuffer(BufferTarget.PixelPackBuffer, id_u);
                GL.BufferSubData(BufferTarget.PixelPackBuffer, IntPtr.Zero, sizeU, planeU);
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, id_u);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_u);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, VideoWidth / 2, VideoHeight / 2, PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
                GL.Uniform1(textureUniformU, 1);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

                // V
                GL.BindBuffer(BufferTarget.PixelPackBuffer, id_v);
                GL.BufferSubData(BufferTarget.PixelPackBuffer, IntPtr.Zero, sizeV, planeV);
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, id_v);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_v);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, VideoWidth / 2, VideoHeight / 2, PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
                GL.Uniform1(textureUniformV, 2);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }
            else
            {
                if (_bufferY != null && _bufferU != null && _bufferV != null)
                {
                    id_y = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, id_y);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16, VideoWidth, VideoHeight, 0, PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                    id_u = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, id_u);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16, VideoWidth / 2, VideoHeight / 2, 0, PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                    id_v = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, id_v);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16, VideoWidth / 2, VideoHeight / 2, 0, PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                    buffer_y = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, buffer_y);
                    GL.BufferData(BufferTarget.ArrayBuffer, sizeY, IntPtr.Zero, BufferUsageHint.StreamCopy);

                    buffer_u = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, buffer_u);
                    GL.BufferData(BufferTarget.ArrayBuffer, sizeU, IntPtr.Zero, BufferUsageHint.StreamCopy);

                    buffer_v = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, buffer_v);
                    GL.BufferData(BufferTarget.ArrayBuffer, sizeV, IntPtr.Zero, BufferUsageHint.StreamCopy);

                    IsInitTexture = true;
                }
            }
        }
    }
}
