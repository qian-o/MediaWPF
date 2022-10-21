using MediaWPF.Common;
using MediaWPF.Controls;
using MediaWPF.Models.DriectX;
using MediaWPF.Models.OpenGL;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MediaWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TimeSpan _lastRenderTime;

        public MainWindow()
        {
            InitializeComponent();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (e is RenderingEventArgs rendering)
            {
                if (rendering.RenderingTime == _lastRenderTime)
                {
                    return;
                }
                double time = (rendering.RenderingTime - _lastRenderTime).TotalMilliseconds;
                Console.WriteLine($"耗时：{time}，帧率：{1000 / time}");

                _lastRenderTime = rendering.RenderingTime;
            }
        }

        #region OpenGL
        private async void MitOpenFileOpenGL_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = ClassHelper.pathFilter
            };
            if (openFileDialog.ShowDialog() == true)
            {
                grdLoading.Visibility = Visibility.Visible;

                MediaBaseModel mediaBase = null;
                await Task.Run(delegate
                {
                    mediaBase = MediaBaseModel.GetMediaBase(openFileDialog.FileName);
                });

                MediaOpenGL mediaOpenGL = new(mediaBase);
                brdMedia.Child = mediaOpenGL;
                brdMedia.ContextMenu = null;

                txbName.Text = mediaBase.VideoFileInfo.Name;
                txbDisplay.Text = "OpenGL";

                grdLoading.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        #region DirectX
        private async void MitOpenFileDirectX_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = ClassHelper.pathFilter
            };
            if (openFileDialog.ShowDialog() == true)
            {
                grdLoading.Visibility = Visibility.Visible;

                bool hdr = false;
                await Task.Run(delegate
                {
                    hdr = ClassHelper.JudgeHdrVideo(openFileDialog.FileName);
                });
                MediaHandleModel mediaHandle = new(openFileDialog.FileName, hdr);

                MediaDirectX mediaDirectX = new(mediaHandle);
                brdMedia.Child = mediaDirectX;
                brdMedia.ContextMenu = null;

                txbName.Text = mediaHandle.VideoFileInfo.Name;
                txbDisplay.Text = "DirectX";

                grdLoading.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        #region Skia
        private void MitOpenFileSkia_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = ClassHelper.pathFilter
            };
            if (openFileDialog.ShowDialog() == true)
            {
                grdLoading.Visibility = Visibility.Visible;

                MediaSkia mediaSkia = new(openFileDialog.FileName);
                brdMedia.Child = mediaSkia;
                brdMedia.ContextMenu = null;

                txbName.Text = openFileDialog.SafeFileName;
                txbDisplay.Text = "Skia";

                grdLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void MitDirectShowSkia_Click(object sender, RoutedEventArgs e)
        {
            grdLoading.Visibility = Visibility.Visible;

            MediaSkia mediaSkia = new("dshow://");
            brdMedia.Child = mediaSkia;
            brdMedia.ContextMenu = null;

            txbName.Text = "DirectShow";
            txbDisplay.Text = "Skia";

            grdLoading.Visibility = Visibility.Collapsed;
        }
        #endregion

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                Thickness thickness = SystemParameters.WindowResizeBorderThickness;
                grdMain.Margin = new Thickness(thickness.Left + 4, thickness.Top + 4, thickness.Right + 4, thickness.Bottom + 4);
            }
            else
            {
                grdMain.Margin = new Thickness(0);
            }
        }

        private void BtnMin_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnState_Click(object sender, RoutedEventArgs e)
        {
            ResizeMode = WindowState == WindowState.Normal ? ResizeMode.NoResize : ResizeMode.CanResize;
            WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GrdMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DoubleAnimation doubleAnimation = new()
            {
                To = grdControl.Opacity == 1 ? 0 : 1,
                Duration = new TimeSpan(0, 0, 0, 0, 300)
            };
            grdControl.BeginAnimation(OpacityProperty, doubleAnimation);
        }

        private void GrdMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
            else
            {
                if (grdControl.Opacity == 0)
                {
                    DoubleAnimation doubleAnimation = new()
                    {
                        To = 1,
                        Duration = new TimeSpan(0, 0, 0, 0, 300)
                    };
                    grdControl.BeginAnimation(OpacityProperty, doubleAnimation);
                }
            }
        }

        private void GrdMain_MouseLeave(object sender, MouseEventArgs e)
        {
            if (grdControl.Opacity == 1)
            {
                DoubleAnimation doubleAnimation = new()
                {
                    To = 0,
                    Duration = new TimeSpan(0, 0, 0, 0, 300)
                };
                grdControl.BeginAnimation(OpacityProperty, doubleAnimation);
            }
        }
    }
}
