using MediaWPF.Models.DriectX;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MediaWPF.Controls
{
    /// <summary>
    /// MediaDirectX.xaml 的交互逻辑
    /// </summary>
    public partial class MediaDirectX : UserControl
    {
        private double dpiScaleX = 1.0;
        private double dpiScaleY = 1.0;
        private TimeSpan _lastRenderTime;

        private readonly MediaHandleModel _media;

        public MediaDirectX(MediaHandleModel media)
        {
            InitializeComponent();

            _media = media;

            DataContext = _media;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void ConMedia_Loaded(object sender, RoutedEventArgs e)
        {
            PresentationSource presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource != null)
            {
                Matrix transformToDevice = presentationSource.CompositionTarget.TransformToDevice;
                dpiScaleX = transformToDevice.M11;
                dpiScaleY = transformToDevice.M22;
            }

            _media.Media_Loaded(dpiScaleX, dpiScaleY);

            imgMedia.Source = _media.D3DImage;
            imgMedia.SetBinding(EffectProperty, nameof(_media.Effect));
            imgMedia.SetBinding(WidthProperty, nameof(_media.VideoWidth));
            imgMedia.SetBinding(HeightProperty, nameof(_media.VideoHeight));
        }

        /// <summary>
        /// 渲染画面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            TimeSpan? currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if (currentRenderTime == _lastRenderTime)
            {
                return;
            }

            _lastRenderTime = currentRenderTime.Value;

            _media.RefreshImage();
        }
    }
}
