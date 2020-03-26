using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace MemoQ.VideoPreview
{
    /// <summary>
    /// Interaction logic for MissingFontDialogView.xaml
    /// </summary>
    public partial class MissingFontDialogView : Window
    {
        public MissingFontDialogView(MissingFontDialogViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void buttonYesClicked(object sender, RoutedEventArgs e)
        {
            (DataContext as MissingFontDialogViewModel).InstallFont = true;
            this.Close();
        }

        private void buttonNoClicked(object sender, RoutedEventArgs e)
        {
            (DataContext as MissingFontDialogViewModel).InstallFont = false;
            this.Close();
        }

        private void DoNotAskAgainCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            (DataContext as MissingFontDialogViewModel).IgnorePermamently = true;
        }

        private void DoNotAskAgainCheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            (DataContext as MissingFontDialogViewModel).IgnorePermamently = false;
        }
    }
}
