using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;


namespace Jpg
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BW_Handler bwHandler;
        private string filePath;
        private const string FILTER_STRING = "JPG-Bilder(*.JPG; *.JPEG)| *.JPG; *.JPEG|PNG-Bilder(*.PNG)| *.PNG";


        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = FILTER_STRING
            };
            if (fileDialog.ShowDialog(this) == true)
            {
                filePath = fileDialog.FileName;
                Title = fileDialog.SafeFileName;
                bwHandler = new BW_Handler(filePath);
                img.Source = bwHandler.GetBitmapSource();
            }
        }

        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            Task t = bwHandler.ConvertToBW(cBx.SelectedIndex);
            await t;
            img.Source = bwHandler.GetBitmapSource();
        }

        private void Re_Click(object sender, RoutedEventArgs e)
        {
            bwHandler.ResetImage();
            img.Source = bwHandler.GetBitmapSource();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveImage(bwHandler.Image, filePath.Insert(filePath.LastIndexOf('.'), "_bw"));
        }

        private async void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            //save all BWs in one Folder
            DirectoryInfo dir = Directory.CreateDirectory(filePath.Remove(filePath.LastIndexOf('.')));
            string path = Path.Combine(dir.FullName, Path.GetFileName(filePath));
            Task<Bitmap>[] tasks = BW_Handler.ConvertWithAllAlgorithms(bwHandler.Image);
            SaveImage(await tasks[0], path.Insert(path.LastIndexOf('.'), "_average"));
            SaveImage(await tasks[1], path.Insert(path.LastIndexOf('.'), "_luminosity"));
            SaveImage(await tasks[2], path.Insert(path.LastIndexOf('.'), "_lightness"));
            
        }

        private void SaveImage(Bitmap bitmap, string saveFilePath)
        {
            if (File.Exists(saveFilePath))
            {
                MessageBoxResult result = MessageBox.Show("Datei existiert bereits, anderen Pfad / Dateinamen auswählen", "Fehler", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }

                SaveFileDialog sfd = new SaveFileDialog
                {
                    InitialDirectory = System.IO.Path.GetDirectoryName(saveFilePath),
                    Filter = FILTER_STRING
                };
                if (sfd.ShowDialog() == true)
                {
                    saveFilePath = sfd.FileName;
                }
            }
            try
            {
                bitmap.Save(saveFilePath);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            MessageBox.Show("Datei gespeichert unter " + saveFilePath, "Datei gespeichert");
        }



    
    }
}
