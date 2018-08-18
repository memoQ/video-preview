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
using System.Windows.Shapes;

namespace MemoQ.VideoPreview
{
    /// <summary>
    /// Interaction logic for AddMediaToDocumentWindow.xaml
    /// </summary>
    public partial class AddMediaToDocumentWindow : Window
    {
        private Guid docId;
        private string docName;
        private string media;

        public Document Document { get; private set; }

        public AddMediaToDocumentWindow(Guid id, string name, string path)
        {
            InitializeComponent();

            docId = id;
            docName = name;
            media = path;
            string title = $"Select video for \"{name}\"";
            Title = title;
            txtDocumentName.Text = title;
            tbMedia.Text = path;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.Multiselect = false;
            if (openDialog.ShowDialog() == true)
                tbMedia.Text = openDialog.FileName;
        }

        private bool btnOkClicked = false;

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            btnOkClicked = true;
            Document = new Document(docId, docName, tbMedia.Text);
            Settings.Instance.DocumentByDocumentGuid[docId] = Document;
            Settings.Instance.SaveSettings();
            DialogResult = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (btnOkClicked)
                return;

            // cancel
            Document = new Document(docId, docName, media);
            Settings.Instance.DocumentByDocumentGuid[docId] = Document;
            Settings.Instance.SaveSettings();
            DialogResult = false;
        }
    }
}
