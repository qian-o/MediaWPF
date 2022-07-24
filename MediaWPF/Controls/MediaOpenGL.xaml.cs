using MediaWPF.Models.OpenGL;
using OpenTK.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MediaWPF.Controls
{
    /// <summary>
    /// MediaOpenGL.xaml 的交互逻辑
    /// </summary>
    public partial class MediaOpenGL : UserControl
    {
        private readonly MediaBaseModel _media;

        public MediaOpenGL(MediaBaseModel media)
        {
            InitializeComponent();

            _media = media;

            DataContext = _media;
            GLWpfControlSettings gLWpfControlSettings = new();
            glMedia.Start(gLWpfControlSettings);
        }

        private void ConMedia_Loaded(object sender, RoutedEventArgs e)
        {
            _media.Media_Loaded();

            glMedia.SetBinding(WidthProperty, nameof(_media.VideoWidth));
            glMedia.SetBinding(HeightProperty, nameof(_media.VideoHeight));
        }

        private void GlMedia_Loaded(object sender, RoutedEventArgs e)
        {
            _media.OpenGL_Loaded();
        }

        private void GlMedia_Render(TimeSpan obj)
        {
            _media.OpenGL_Render();
        }
    }
}
