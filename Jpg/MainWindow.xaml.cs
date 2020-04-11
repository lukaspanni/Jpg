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
        private BW_Handler[] bwHandlers;
        private string[] filePaths;
        private const string FILTER_STRING = "JPG-Bilder(*.JPG; *.JPEG)| *.JPG; *.JPEG|PNG-Bilder(*.PNG)| *.PNG";


        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = FILTER_STRING,
                Multiselect = true
            };
            if (fileDialog.ShowDialog(this) == true)
            {
                filePaths = fileDialog.FileNames;
                Title = fileDialog.SafeFileName;
                bwHandlers = new BW_Handler[filePaths.Length];
                for (var i = 0; i < filePaths.Length; i++)
                {
                    bwHandlers[i] = new BW_Handler(filePaths[i]);
                }
                img.Source = bwHandlers[0].GetBitmapSource();
            }
        }

        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            Task[] tasks = new Task[bwHandlers.Length];
            for (int i = 0; i < bwHandlers.Length; i++)
            {
                tasks[i] = bwHandlers[i].ConvertToBW(cBx.SelectedIndex);
            }

            foreach (Task task in tasks)
            {
                await task;
            }
            img.Source = bwHandlers[0].GetBitmapSource();
        }

        private void Re_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < bwHandlers.Length; i++)
            {
                bwHandlers[i].ResetImage();
            }
            img.Source = bwHandlers[0].GetBitmapSource();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < bwHandlers.Length; i++)
            {
                SaveImage(bwHandlers[i].Image, filePaths[i].Insert(filePaths[i].LastIndexOf('.'), "_bw"));

            }        }

        private async void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < filePaths.Length; i++)
            {
                //save all BWs in one Folder
                DirectoryInfo dir = Directory.CreateDirectory(filePaths[i].Remove(filePaths[i].LastIndexOf('.')));
                string path = Path.Combine(dir.FullName, Path.GetFileName(filePaths[i]));
                Task<Bitmap>[] tasks = BW_Handler.ConvertWithAllAlgorithms(bwHandlers[i].Image);
                SaveImage(await tasks[0], path.Insert(path.LastIndexOf('.'), "_average"));
                SaveImage(await tasks[1], path.Insert(path.LastIndexOf('.'), "_luminosity"));
                SaveImage(await tasks[2], path.Insert(path.LastIndexOf('.'), "_lightness")); 
            }

            MessageBox.Show("Alle gespeichert", "Erfolgreich", MessageBoxButton.OK);

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
        }



    
    }
}
