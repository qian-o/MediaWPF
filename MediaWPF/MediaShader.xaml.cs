using LibVLCSharp.Shared;
using MediaWPF.Shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Wpf;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace MediaWPF
{
    /// <summary>
    /// MediaShader.xaml 的交互逻辑
    /// </summary>
    public partial class MediaShader : UserControl
    {
        #region shader
        private readonly float[] _vertices =
        {
            -1.0f, -1.0f, 0.0f, 0.0f, 1.0f,
             1.0f, -1.0f, 0.0f, 1.0f, 1.0f,
            -1.0f,  1.0f, 0.0f, 0.0f, 0.0f,
             1.0f,  1.0f, 0.0f, 1.0f, 0.0f
        };

        private int _vertexBufferObject;

        private int _vertexArrayObject;

        private Shader _shader;
        #endregion

        private Uri _uri;
        private LibVLC _lib;
        private Media _media;
        private MediaPlayer _mediaplayer;
        private int videoWidth;
        private int videoHeight;
        private byte[] _buffer;
        private IntPtr planeY, planeU, planeV;
        private int id_y, id_u, id_v;
        private int buffer_y, buffer_u, buffer_v;
        private int textureUniformY, textureUniformU, textureUniformV;
        private bool isInitTexture;

        public MediaShader()
        {
            InitializeComponent();

            GLWpfControlSettings gLWpfControlSettings = new()
            {
                RenderContinuously = false
            };
            glMedia.Start(gLWpfControlSettings);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // _uri = new(@"E:\BaiduNetdiskDownload\[A]ddiction _2160p_HDR_Extreme.mp4");
            // _uri = new(@"D:\BaiduNetdiskDownload\[A]ddiction _2160p.mp4");
            _uri = new(@"C:\Users\13247\Downloads\杜比视界\Sony_4K_Camp.mp4");
            // _uri = new(@"D:\BaiduNetdiskDownload\4K120帧HDR.mp4");
            // _uri = new(@"E:\BaiduNetdiskDownload\NARAKA  BLADEPOINT 3440p 60.mp4");
            _lib = new();
            _media = new(_lib, _uri, new string[] { "input-repeat=65535", "avcodec-hw=any" });
            _mediaplayer = new(_media);

            _mediaplayer.SetVideoFormatCallbacks(VideoFormat, null);
            _mediaplayer.SetVideoCallbacks(LockVideo, null, DisplayVideo);
            _mediaplayer.Play();
        }

        private void GlMedia_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int w = videoWidth;
            int h = videoHeight;
            while (w > glMedia.FrameBufferWidth || h > glMedia.FrameBufferHeight)
            {
                w = Convert.ToInt32(w * 0.9);
                h = Convert.ToInt32(h * 0.9);
            }

            GL.Viewport(glMedia.FrameBufferWidth / 2 - w / 2, glMedia.FrameBufferHeight / 2 - h / 2, w, h);
        }

        private void GlMedia_Loaded(object sender, RoutedEventArgs e)
        {
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StreamDraw);

            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            _shader.Use();

            textureUniformY = GL.GetUniformLocation(_shader.Handle, "tex_y");
            textureUniformU = GL.GetUniformLocation(_shader.Handle, "tex_u");
            textureUniformV = GL.GetUniformLocation(_shader.Handle, "tex_v");

            int vertexLocation = _shader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            int texCoordLocation = _shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        }

        private void GlMedia_Render(TimeSpan obj)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(_vertexArrayObject);
            Display();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        #region VLC解码
        public uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, IntPtr pitches, IntPtr lines)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("I420");
            for (var i = 0; i < bytes.Length; i++)
            {
                Marshal.WriteByte(chroma, i, bytes[i]);
            }

            if (_mediaplayer.Media is Media media)
            {
                foreach (MediaTrack track in media.Tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        VideoTrack trackInfo = track.Data.Video;
                        if (trackInfo.Width > 0 && trackInfo.Height > 0)
                        {
                            width = trackInfo.Width;
                            height = trackInfo.Height;
                            if (trackInfo.SarDen != 0)
                            {
                                width = width * trackInfo.SarNum / trackInfo.SarDen;
                            }
                        }

                        break;
                    }
                }
            }

            int[] pitche = { (int)width, (int)width / 2, (int)width / 2 };
            int[] line = { (int)height, (int)height / 2, (int)height / 2 };
            Marshal.Copy(pitche, 0, pitches, pitche.Length);
            Marshal.Copy(line, 0, lines, pitche.Length);

            videoWidth = (int)width;
            videoHeight = (int)height;

            _buffer = new byte[width * height * 12 / 8];
            opaque = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0);

            planeY = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0);
            planeU = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, videoWidth * videoHeight);
            planeV = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, videoWidth * videoHeight + videoWidth * videoHeight / 4);
            return 1;

        }

        public IntPtr LockVideo(IntPtr opaque, IntPtr planes)
        {
            IntPtr[] datas = { planeY, planeU, planeV };
            Marshal.Copy(datas, 0, planes, datas.Length);
            return IntPtr.Zero;
        }

        public void DisplayVideo(IntPtr opaque, IntPtr picture)
        {
            try
            {
                Dispatcher.Invoke(delegate
                {
                    glMedia.InvalidateVisual();
                });
            }
            catch (TaskCanceledException)
            {

            }
        }
        #endregion

        private void Display()
        {
            if (!isInitTexture && _buffer != null)
            {
                //Init Texture
                id_y = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id_y);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, videoWidth, videoHeight, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                id_u = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id_u);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, videoWidth / 2, videoHeight / 2, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                id_v = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id_v);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, videoWidth / 2, videoHeight / 2, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                buffer_y = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, buffer_y);
                GL.BufferData(BufferTarget.ArrayBuffer, videoWidth * videoHeight, IntPtr.Zero, BufferUsageHint.StreamCopy);
                planeY = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);

                buffer_u = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, buffer_u);
                GL.BufferData(BufferTarget.ArrayBuffer, videoWidth / 2 * videoHeight / 2, IntPtr.Zero, BufferUsageHint.StreamCopy);
                planeU = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);

                buffer_v = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, buffer_v);
                GL.BufferData(BufferTarget.ArrayBuffer, videoWidth / 2 * videoHeight / 2, IntPtr.Zero, BufferUsageHint.StreamCopy);
                planeV = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);

                isInitTexture = true;
            }
            if (isInitTexture)
            {
                unsafe
                {
                    // Y
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, id_y);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_y);
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, videoWidth, videoHeight, PixelFormat.Red, PixelType.UnsignedByte, new IntPtr((void*)null));
                    GL.Uniform1(textureUniformY, 0);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

                    // U
                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, id_u);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_u);
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, videoWidth / 2, videoHeight / 2, PixelFormat.Red, PixelType.UnsignedByte, new IntPtr((void*)null));
                    GL.Uniform1(textureUniformU, 1);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

                    // V
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, id_v);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_v);
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, videoWidth / 2, videoHeight / 2, PixelFormat.Red, PixelType.UnsignedByte, new IntPtr((void*)null));
                    GL.Uniform1(textureUniformV, 2);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
                }
            }
        }
    }
}
