using System.Windows;
using System.Windows.Controls;

namespace MediaWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            UserControl media = button.Content.ToString() == "SDR" ? new MediaShader(txtFile.Text) : new MediaShaderHDR(txtFile.Text);
            grdMain.Children.Add(media);
        }
    }
}
