using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Controls = System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Media = System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Shapes = System.Windows.Shapes;
using System.IO;
using System.Drawing;
using Microsoft.Win32;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RGBScopizer
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Parameters
        private int targetWidth = 1920;
        private int targetHeight = 1080;
        private int targetBitDepth = 8;
        private int maxIntensity = 10;
        private int srcThreshold = 1;

        const int B = 0;
        const int G = 1;
        const int R = 2;
        const int A = 3;

        // Data binding for parameter textboxes
        public int TargetHeight
        {
            get { return targetHeight; }
            set { targetHeight = value; }
        }
        public int TargetWidth
        {
            get { return targetWidth; }
            set { targetWidth = value; }
        }
        public int MaxIntensity
        {
            get { return maxIntensity; }
            set { maxIntensity = value; }
        }
        public int SrcThreshold
        {
            get { return srcThreshold; }
            set { srcThreshold = value; }
        }

        // Image Data
        private Bitmap redSrc = null, greenSrc = null, blueSrc = null;
        private Bitmap redResized = null, greenResized = null, blueResized = null;

        private void BtnDewit_Click(object sender, RoutedEventArgs e)
        {
            CalculateResult();
        }

        private Bitmap result;

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PNG Image | *.png";
            if(sfd.ShowDialog() == true)
            {
                result.Save(sfd.FileName);
            }

        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void LoadSrc_btn_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images (.png,.jpg,.tif,.tiff)|*.png;*.jpg;*.tif;*.tiff";

            if(ofd.ShowDialog() == true)
            {
                Image image = Image.FromFile(ofd.FileName);
                switch (((Controls.Button)sender).Name)
                {
                    case "loadRed_btn":
                        redSrc = (Bitmap)image;
                        red_img.Source = Helpers.BitmapToImageSource((Bitmap)image);
                        break;
                    case "loadGreen_btn":
                        greenSrc = (Bitmap)image;
                        green_img.Source = Helpers.BitmapToImageSource((Bitmap)image);
                        break;
                    case "loadBlue_btn":
                        blueSrc = (Bitmap)image;
                        blue_img.Source = Helpers.BitmapToImageSource((Bitmap)image);
                        break;

                }
            }

        }

        private void CalculateResult()
        {
            btnSave.IsEnabled = false;

            if(result != null)
            {
                result.Dispose();
            }
            result = new Bitmap(targetWidth, targetHeight,PixelFormat.Format32bppArgb);

            BitmapData bmpData = result.LockBits(new Rectangle(0, 0, targetWidth, targetHeight), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            byte[] pixels = new byte[bmpData.Stride * targetHeight];

            Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);

            // Make everything transparent at start

            for (int x = 0; x < targetWidth; x++)
            {
                for (int y = 0; y < targetHeight; y++)
                {

                    pixels[y * bmpData.Stride + x * 4 + A] = 0;
                }
            }


            // Draw pixels
            if (redSrc != null)
            {
                redResized = new Bitmap(redSrc, targetWidth, 255);
                pixels = RGBScopize(pixels, bmpData.Stride, targetWidth,targetHeight, redResized, R);
            }
            if (greenSrc != null)
            {
                greenResized = new Bitmap(greenSrc, targetWidth, 255);
                pixels = RGBScopize(pixels, bmpData.Stride, targetWidth, targetHeight, greenResized, G);
            }
            if (blueSrc != null)
            {
                blueResized = new Bitmap(blueSrc, targetWidth, 255);
                pixels = RGBScopize(pixels, bmpData.Stride, targetWidth, targetHeight, blueResized, B);
            }


            Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);

            result.UnlockBits(bmpData);

            final_img.Source = Helpers.BitmapToImageSource((Bitmap)result);

            btnSave.IsEnabled = true;

        }

        private byte[] RGBScopize(byte[] target, int stride, int width, int height, Bitmap src, int channel = R)
        {
            // Copy source data into byte array
            BitmapData srcBmpData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] srcData = new byte[srcBmpData.Stride * srcBmpData.Height];
            Marshal.Copy(srcBmpData.Scan0, srcData, 0, srcData.Length);

            int srcR, srcG, srcB, srcA;
            float posX, posY; // Position as a ratio 0 <= pos <= 1
            int destX, intensity, destY;
            Random rnd = new Random();
            int iterCount, i;
            
            float srcIntensityHere;

            for(int x = 0; x < src.Width; x++)
            {
                for(int y = 0; y < src.Height; y++)
                {
                    srcR = srcData[y * srcBmpData.Stride + x * 4 + R];
                    srcG = srcData[y * srcBmpData.Stride + x * 4 + G];
                    srcB = srcData[y * srcBmpData.Stride + x * 4 + B];
                    srcA = srcData[y * srcBmpData.Stride + x * 4 + A];

                    srcIntensityHere = (srcR + srcG + srcB) / 3;

                    // Pixel is active, drawit
                    if (srcA > 127 && srcIntensityHere >= srcThreshold)
                    {
                        posX = (float)x / (src.Width-1);
                        posY = (float)y / (src.Height-1);

                        // posX remains x-position in final image
                        // posY becomes intensity (since that determines y-position in scope)
                        destX = (int)Math.Round(posX * (width-1));
                        intensity = (int)Math.Round((1-posY) * 255);

                        iterCount = (int)Math.Round(srcIntensityHere / 255 * maxIntensity);

                        for (i = 0; i < iterCount; i++)
                        {
                            // destY is random
                            destY = rnd.Next(0, height - 1);

                            target[destY * stride + destX * 4 + channel] = (byte)intensity;
                            target[destY * stride + destX * 4 + A] = 255;
                        }
                    }
                }
            }




            // Unlock source bitmap again.
            src.UnlockBits(srcBmpData);

            return target;
        }

    }
}
