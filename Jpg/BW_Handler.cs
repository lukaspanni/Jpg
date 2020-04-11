using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Jpg
{
    class BW_Handler
    {
        public delegate int Algorithm(int r, int g, int b);


        public Bitmap Image { get; set; }
        private Bitmap imageCopy;

        
        private static Algorithm[] algorithms = { (r, g, b) => (r + g + b) / 3,
                                        (r, g, b) => (int)(0.21 * r + 0.72 * g + 0.07 * b),
                                        Bw_Lightness };


        private static int Bw_Lightness(int r, int g, int b)
        {
            return (int)(0.5 * (Max(r, g, b) + Min(r, g, b)));
        }

        private static int Max(int r, int g, int b)
        {
            return Max(r, Max(g, b));
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        private static int Min(int r, int g, int b)
        {
            return Min(r, Min(g, b));
        }

        private static int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        public BW_Handler(string filePath)
        {
            Image = new Bitmap(filePath);
            imageCopy = Image.Clone(new Rectangle(0, 0, Image.Width, Image.Height), Image.PixelFormat);
        }

        public Task ConvertToBW(int algIndex)
        {
            ResetImage();
            return Task.Run(async () =>
            {
                Image = await ConvertToBW(Image.Clone(new Rectangle(0, 0, Image.Width, Image.Height), Image.PixelFormat), algorithms[algIndex]);   
            });
        }

        public static Task<Bitmap>[] ConvertWithAllAlgorithms(Bitmap image)
        {
            Task<Bitmap>[] bitmapTasks = new Task<Bitmap>[algorithms.Length];
            Rectangle copyRect = new Rectangle(0, 0, image.Width, image.Height);
            for (int i = 0; i < bitmapTasks.Length; i++)
            {
                bitmapTasks[i] = ConvertToBW(image.Clone(copyRect, image.PixelFormat), algorithms[i]);
            }
            return bitmapTasks;
        }

        private static Task<Bitmap> ConvertToBW(Bitmap image, Algorithm alg)
        {
            return Task.Run(() =>
            {
                if (image == null)
                {
                    return null;
                }
                BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);

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

        public void ResetImage()
        {
            Image = imageCopy.Clone(new Rectangle(0, 0, imageCopy.Width, imageCopy.Height), Image.PixelFormat);

        }

        public BitmapSource GetBitmapSource()
        {
            using (MemoryStream memory = new MemoryStream())
            {
                Image.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }


       

    }
}
