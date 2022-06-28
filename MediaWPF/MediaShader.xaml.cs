using LibVLCSharp.Shared;
using MediaWPF.Models.ViewModels;
using MediaWPF.Shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

        private readonly MdeiaViewModel viewModel;
        private readonly Stopwatch stopWatch = new();

        private LibVLC _lib;
        private Media _media;
        private MediaPlayer _mediaplayer;
        private byte[] _bufferY, _bufferU, _bufferV;
        private int sizeY, sizeU, sizeV;
        private IntPtr planeY, planeU, planeV;
        private int id_y, id_u, id_v;
        private int buffer_y, buffer_u, buffer_v;
        private int textureUniformY, textureUniformU, textureUniformV;

        public MediaShader(string path)
        {
            InitializeComponent();

            viewModel = DataContext as MdeiaViewModel;

            viewModel.Path = path;

            GLWpfControlSettings gLWpfControlSettings = new();
            glMedia.Start(gLWpfControlSettings);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (!string.IsNullOrEmpty(viewModel.Path))
                {
                    viewModel.FileName = new FileInfo(viewModel.Path).Name;
                    _lib = new();
                    _media = new(_lib, new Uri(viewModel.Path), new string[] { "input-repeat=65535" });
                    _mediaplayer = new(_media)
                    {
                        EnableHardwareDecoding = true
                    };
                    _mediaplayer.SetVideoFormatCallbacks(VideoFormat, null);
                    _mediaplayer.SetVideoCallbacks(LockVideo, null, DisplayVideo);
                    _mediaplayer.Mute = true;
                    _mediaplayer.Play();
                }
            }
        }

        private void GlMedia_Loaded(object sender, RoutedEventArgs e)
        {
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StreamDraw);

            string p = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Shaders");
            _shader = new Shader(Path.Combine(p, "shader.vert"), Path.Combine(p, "shaderSDR.frag"));

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
            GL.UseProgram(_shader.Handle);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(_vertexArrayObject);
            Display();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.UseProgram(0);
        }

        #region VLC解码
        private uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, IntPtr pitches, IntPtr lines)
        {
            Console.WriteLine(Marshal.PtrToStringAnsi(chroma));

            byte[] bytes = Encoding.ASCII.GetBytes("I420");
            for (int i = 0; i < bytes.Length; i++)
            {
                Marshal.WriteByte(chroma, i, bytes[i]);
            }

            int[] pitche = { (int)width, (int)width / 2, (int)width / 2 };
            int[] line = { (int)height, (int)height / 2, (int)height / 2 };

            Marshal.Copy(pitche, 0, pitches, pitche.Length);
            Marshal.Copy(line, 0, lines, pitche.Length);

            _bufferY = new byte[(int)width * (int)height];
            _bufferU = new byte[(int)width / 2 * (int)height / 2];
            _bufferV = new byte[(int)width / 2 * (int)height / 2];

            sizeY = _bufferY.Length;
            sizeU = _bufferU.Length;
            sizeV = _bufferV.Length;

            planeY = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferY, 0);
            planeU = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferU, 0);
            planeV = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferV, 0);

            if (_mediaplayer.Media is Media media)
            {
                foreach (MediaTrack track in media.Tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        Console.WriteLine(Encoding.ASCII.GetString(BitConverter.GetBytes(track.OriginalFourcc)));
                        Console.WriteLine(Encoding.ASCII.GetString(BitConverter.GetBytes(track.Codec)));
                        VideoTrack trackInfo = track.Data.Video;
                        if (trackInfo.Width > 0 && trackInfo.Height > 0)
                        {
                            width = trackInfo.Width;
                            height = trackInfo.Height;
                        }

                        break;
                    }
                }
            }

            viewModel.VideoWidth = (int)width;
            viewModel.VideoHeight = (int)height;

            return 1;
        }

        private IntPtr LockVideo(IntPtr opaque, IntPtr planes)
        {
            IntPtr[] datas = { planeY, planeU, planeV };
            Marshal.Copy(datas, 0, planes, datas.Length);
            return IntPtr.Zero;
        }

        public void DisplayVideo(IntPtr opaque, IntPtr picture)
        {
            stopWatch.Stop();
            viewModel.TimeConsuming = stopWatch.ElapsedMilliseconds;
            stopWatch.Restart();
        }
        #endregion

        private void Display()
        {
            if (!viewModel.IsInitTexture && _bufferY != null && _bufferU != null && _bufferV != null)
            {
                id_y = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id_y);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, viewModel.VideoWidth, viewModel.VideoHeight, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                id_u = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id_u);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, viewModel.VideoWidth / 2, viewModel.VideoHeight / 2, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                id_v = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id_v);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, viewModel.VideoWidth / 2, viewModel.VideoHeight / 2, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
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

                viewModel.IsInitTexture = true;
            }
            if (viewModel.IsInitTexture)
            {
                // Y
                GL.BindBuffer(BufferTarget.PixelPackBuffer, id_y);
                GL.BufferSubData(BufferTarget.PixelPackBuffer, IntPtr.Zero, sizeY, planeY);
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, id_y);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_y);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, viewModel.VideoWidth, viewModel.VideoHeight, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.Uniform1(textureUniformY, 0);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

                // U
                GL.BindBuffer(BufferTarget.PixelPackBuffer, id_u);
                GL.BufferSubData(BufferTarget.PixelPackBuffer, IntPtr.Zero, sizeU, planeU);
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, id_u);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_u);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, viewModel.VideoWidth / 2, viewModel.VideoHeight / 2, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.Uniform1(textureUniformU, 1);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

                // V
                GL.BindBuffer(BufferTarget.PixelPackBuffer, id_v);
                GL.BufferSubData(BufferTarget.PixelPackBuffer, IntPtr.Zero, sizeV, planeV);
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, id_v);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_v);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, viewModel.VideoWidth / 2, viewModel.VideoHeight / 2, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.Uniform1(textureUniformV, 2);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }
        }
    }
}
