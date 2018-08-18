using MemoQ.PreviewInterfaces.Entities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MemoQ.VideoPreview
{
    /// <summary>
    /// Interaction logic for OpenControl.xaml
    /// </summary>
    public partial class OpenControl : UserControl
    {
        private Action<Document> playVideo;

        public OpenControl()
        {
            InitializeComponent();
        }

        public void SetPlayAction(Action<Document> action)
        {
            playVideo = action;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                var videoViewModel = (DataContext as VideoViewModel);
                var window = new AddMediaToDocumentWindow(videoViewModel.Document.Id, videoViewModel.Document.Name, videoViewModel.Document.Media);
                window.Topmost = Settings.Instance.AlwaysOnTop;
                var result = window.ShowDialog();
                if (result != true)
                    return;

                videoViewModel.CannotOpen = false;
                playVideo(Settings.Instance.DocumentByDocumentGuid[videoViewModel.Document.Id]);
            });
        }
    }
}
