using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Microsoft.Win32;


namespace Jpg
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate int algorithm(int r, int g, int b);

        private Bitmap image;
        private Bitmap imageCopy;
        private algorithm[] algorithms = { (r, g, b) => (r + g + b) / 3,
                                        (r, g, b) => (int)(0.21 * r + 0.72 * g + 0.07 * b),
                                        Bw_Lightness };
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
                image = new Bitmap(filePath);
                imageCopy = image.Clone(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), image.PixelFormat);
                img.Source = Convert(image);
            }
        }

        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (image == null) return;
            Reset();
            Task<Bitmap> t = BW(image, algorithms[cBx.SelectedIndex]);
            await t;
            img.Source = Convert(t.Result);
        }

        private void Re_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            img.Source = Convert(image);
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveImage(image, filePath.Insert(filePath.LastIndexOf('.'), "_bw"));
        }

        private async void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            //save all BWs in one Folder
            DirectoryInfo dir = Directory.CreateDirectory(filePath.Remove(filePath.LastIndexOf('.')));
            string path = System.IO.Path.Combine(dir.FullName, System.IO.Path.GetFileName(filePath));
            System.Drawing.Rectangle copyRect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            //Do Work parallel
            Task<Bitmap> t1 = BW(image.Clone(copyRect, image.PixelFormat), algorithms[0]);
            Task<Bitmap> t2 = BW(image.Clone(copyRect, image.PixelFormat), algorithms[1]);
            Task<Bitmap> t3 = BW(image.Clone(copyRect, image.PixelFormat), algorithms[2]);

            SaveImage(await t1, path.Insert(path.LastIndexOf('.'), "_average"));
            SaveImage(await t2, path.Insert(path.LastIndexOf('.'), "_luminosity"));
            SaveImage(await t3, path.Insert(path.LastIndexOf('.'), "_lightness"));

        }

        private static int Bw_Lightness(int r, int g, int b)
        {
            return (int)(0.5 * (Max(r, g, b) + Min(r, g, b)));
        }

        private static int Max(int r, int g, int b)
        {
            if (r >= g && r >= b) return r;
            if (g >= b && g >= r) return g;
            if (b >= r && b >= g) return b;
            return 0;
        }

        private static int Min(int r, int g, int b)
        {
            if (r <= g && r <= b) return r;
            if (g <= b && g <= r) return g;
            if (b <= r && b <= g) return b;
            return 0;
        }


        private void Reset()
        {
            image = imageCopy.Clone(new System.Drawing.Rectangle(0, 0, imageCopy.Width, imageCopy.Height), imageCopy.PixelFormat);
        }


        private void SaveImage(Bitmap bitmap, string saveFilePath)
        {
            if (File.Exists(saveFilePath))
            {
                MessageBoxResult result = MessageBox.Show("Datei existiert bereits, anderen Pfad / Dateinamen auswählen", "Fehler", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel) return;
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

        public BitmapSource Convert(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private Task<Bitmap> BW(Bitmap image, algorithm alg)
        {
            return Task.Run(() =>
            {
                if (image == null)
                {
                    return null;
                }
                BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);

                int bytesPerPixel = Bitmap.GetPixelFormatSize(image.PixelFormat) / 8;
                int byteCount = data.Stride * image.Height;
                byte[] pixels = new byte[byteCount];
                IntPtr ptrFirstPixel = data.Scan0;
                Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
                int heightInPixels = data.Height;
                int widthInBytes = data.Width * bytesPerPixel;

                Parallel.For(0, heightInPixels, y =>
                {
                    int currentLine = y * data.Stride;
                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        int r = pixels[currentLine + x + 2];
                        int g = pixels[currentLine + x + 1];
                        int b = pixels[currentLine + x];

                        // calculate new pixel value
                        int gray = alg(r, g, b);

                        pixels[currentLine + x] = (byte)gray;
                        pixels[currentLine + x + 1] = (byte)gray;
                        pixels[currentLine + x + 2] = (byte)gray;
                    }
                });

                // copy modified bytes back
                Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
                image.UnlockBits(data);
                return image;
            });
        }


    }
}
