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
        //private int targetBitDepth = 8;
        private int maxIntensity = 5;
        private int srcThreshold = 1;
        private float gamma = 2.2f;

        // Blocksize
        // Experiments (based on JPEG)
        // 16x16 is extremely robust at high compression
        // 4x4 is last one with really good quality at lossless, but falls apart very quick (tested with JPEG, might be better with H264)
        // 8x8 also falls apart but you can at least still recognize something
        private int blockSizeX = 4; 
        private int blockSizeY = 4;

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
        public int BlockSizeX
        {
            get { return blockSizeX; }
            set { blockSizeX = value; }
        }
        public int BlockSizeY
        {
            get { return blockSizeY; }
            set { blockSizeY = value; }
        }
        public float Gamma
        {
            get { return gamma; }
            set { gamma = value; }
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

        private void LoadRGB_btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images (.png,.jpg,.tif,.tiff)|*.png;*.jpg;*.tif;*.tiff";

            if (ofd.ShowDialog() == true)
            {
                Bitmap image = (Bitmap) Image.FromFile(ofd.FileName);

                redSrc = new Bitmap(image.Width,image.Height);
                greenSrc = new Bitmap(image.Width,image.Height);
                blueSrc = new Bitmap(image.Width,image.Height);

                BitmapData imageBmp = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                BitmapData redBmp = redSrc.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                BitmapData greenBmp = greenSrc.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                BitmapData blueBmp = blueSrc.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                byte[] imageData = new byte[imageBmp.Stride * imageBmp.Height];
                byte[] redData = new byte[redBmp.Stride * redBmp.Height];
                byte[] greenData = new byte[greenBmp.Stride * greenBmp.Height];
                byte[] blueData = new byte[blueBmp.Stride * blueBmp.Height];

                Marshal.Copy(imageBmp.Scan0, imageData, 0, imageData.Length);
                Marshal.Copy(redBmp.Scan0, redData, 0, redData.Length);
                Marshal.Copy(greenBmp.Scan0, greenData, 0, greenData.Length);
                Marshal.Copy(blueBmp.Scan0, blueData, 0, blueData.Length);


                byte myValueR,myValueG,myValueB,myValueA;
                for(int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {

                        myValueR = imageData[y * imageBmp.Stride + x * 4 + R];
                        myValueG = imageData[y * imageBmp.Stride + x * 4 + G];
                        myValueB = imageData[y * imageBmp.Stride + x * 4 + B];
                        myValueA = imageData[y * imageBmp.Stride + x * 4 + A];

                        redData[y * imageBmp.Stride + x * 4 + R] = myValueR;
                        redData[y * imageBmp.Stride + x * 4 + G] = myValueR;
                        redData[y * imageBmp.Stride + x * 4 + B] = myValueR;
                        redData[y * imageBmp.Stride + x * 4 + A] = myValueA;
                        greenData[y * imageBmp.Stride + x * 4 + R] = myValueG;
                        greenData[y * imageBmp.Stride + x * 4 + G] = myValueG;
                        greenData[y * imageBmp.Stride + x * 4 + B] = myValueG;
                        greenData[y * imageBmp.Stride + x * 4 + A] = myValueA;
                        blueData[y * imageBmp.Stride + x * 4 + R] = myValueB;
                        blueData[y * imageBmp.Stride + x * 4 + G] = myValueB;
                        blueData[y * imageBmp.Stride + x * 4 + B] = myValueB;
                        blueData[y * imageBmp.Stride + x * 4 + A] = myValueA;
                    }
                }


                Marshal.Copy(redData, 0, redBmp.Scan0, redData.Length);
                Marshal.Copy(greenData, 0, greenBmp.Scan0, greenData.Length);
                Marshal.Copy(blueData, 0, blueBmp.Scan0, blueData.Length);

                image.Dispose();
                redSrc.UnlockBits(redBmp);
                greenSrc.UnlockBits(redBmp);
                blueSrc.UnlockBits(redBmp);

                red_img.Source = Helpers.BitmapToImageSource(redSrc);
                green_img.Source = Helpers.BitmapToImageSource(greenSrc);
                blue_img.Source = Helpers.BitmapToImageSource(blueSrc);

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
            int xC, yC;
            
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

                        iterCount = (int)Math.Round(Math.Pow(srcIntensityHere / 255,gamma) * maxIntensity);

                        // Round down for color subsampling
                        destX = blockSizeX * (int)Math.Floor((float)destX / blockSizeX);

                        for (i = 0; i < iterCount; i++)
                        {
                            // destY is random
                            destY = rnd.Next(0, height - 1);

                            int destYpre = destY; // for debugging

                            // Round down for color subsampling
                            destY = blockSizeY * (int)Math.Floor((float)destY / blockSizeY);

                            for (xC = 0; xC < blockSizeX; xC++)
                            {
                                if(destX+xC > width - 1)
                                {
                                    continue;
                                }
                                for (yC = 0; yC < BlockSizeY; yC++)
                                {
                                    if (destY + yC > height - 1)
                                    {
                                        continue;
                                    }

                                    target[(destY + yC) * stride + (destX + xC) * 4 + channel] = (byte)intensity;
                                    target[(destY + yC) * stride + (destX + xC) * 4 + A] = 255;
                                }
                            }
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
