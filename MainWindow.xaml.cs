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
        private Bitmap shapeSrc = null;

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

        private void LoadShape_btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images (.png,.jpg,.tif,.tiff)|*.png;*.jpg;*.tif;*.tiff";

            if (ofd.ShowDialog() == true)
            {
                Image image = Image.FromFile(ofd.FileName);
                shapeSrc = (Bitmap)image;
                shape_img.Source = Helpers.BitmapToImageSource((Bitmap)image);

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

            int[] redColumns = null, greenColumns = null, blueColumns = null;

            // Draw pixels
            if (redSrc != null)
            {
                redResized = new Bitmap(redSrc, targetWidth, 255);
                //pixels = RGBScopize(pixels, bmpData.Stride, targetWidth,targetHeight, redResized, R);
                redColumns = RGBScopizeColumns(bmpData.Stride, targetWidth,targetHeight, redResized, R);
            }
            if (greenSrc != null)
            {
                greenResized = new Bitmap(greenSrc, targetWidth, 255);
                //pixels = RGBScopize(pixels, bmpData.Stride, targetWidth, targetHeight, greenResized, G);
                greenColumns = RGBScopizeColumns(bmpData.Stride, targetWidth, targetHeight, greenResized, G);
            }
            if (blueSrc != null)
            {
                blueResized = new Bitmap(blueSrc, targetWidth, 255);
                //pixels = RGBScopize(pixels, bmpData.Stride, targetWidth, targetHeight, blueResized, B);
                blueColumns = RGBScopizeColumns(bmpData.Stride, targetWidth, targetHeight, blueResized, B);
            }
            /*
            if(shapeSrc != null)
            {
                shapeResized = new Bitmap(shapeSrc, targetWidth, targetHeight);
                pixels = RGBShape(pixels, bmpData.Stride, targetWidth, targetHeight, shapeResized);
            }*/
            Bitmap shapeResized = shapeSrc == null ? null : new Bitmap(shapeSrc, targetWidth, targetHeight);
            pixels = RGBDrawColumns(pixels, bmpData.Stride, targetWidth, targetHeight, shapeResized, redColumns, greenColumns, blueColumns);


            Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);

            result.UnlockBits(bmpData);

            final_img.Source = Helpers.BitmapToImageSource((Bitmap)result);

            btnSave.IsEnabled = true;

        }


        private byte[] RGBDrawColumns(byte[] target, int stride, int width, int height, Bitmap src, int[] redColumns, int[] greenColumns, int[] blueColumns)
        {
            // Copy source data into byte array
            BitmapData srcBmpData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] srcData = new byte[srcBmpData.Stride * srcBmpData.Height];
            Marshal.Copy(srcBmpData.Scan0, srcData, 0, srcData.Length);

            int srcR, srcG, srcB, srcA;

            int[][] columnRedTarget;
            int[][] columnGreenTarget;
            int[][] columnBlueTarget;

            int[][] columnRed = new int[src.Height][];
            int[][] columnGreen = new int[src.Height][];
            int[][] columnBlue = new int[src.Height][];

            int redCount, greenCount, blueCount;
            int redIndex, greenIndex, blueIndex;
            int countToDraw, valuetoDraw;

            for (int x = 0; x < src.Width; x++)
            {
                redCount = 0;
                greenCount = 0;
                blueCount = 0;
                redIndex = 0;
                greenIndex = 0;
                blueIndex = 0;
                countToDraw = 0;
                valuetoDraw = 0;
                // First count total count of pixels to draw
                for (int y = 0; y < 255; y++)
                {
                    redCount += redColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_ACTIVE] == 1 ? redColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_COUNT] : 0;
                    greenCount += greenColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_ACTIVE] == 1 ? greenColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_COUNT] : 0;
                    blueCount += blueColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_ACTIVE] == 1 ? blueColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_COUNT] : 0;
                }

                //Now create arrays
                columnRedTarget = new int[redCount][];
                columnGreenTarget = new int[greenCount][];
                columnBlueTarget = new int[blueCount][];

                for (int y = 0; y < 255; y++)
                {
                    // Red
                    countToDraw = redColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_ACTIVE] == 1 ? redColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_COUNT] : 0;
                    valuetoDraw = redColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_VALUE];
                    for(int i = 0; i < countToDraw; i++)
                    {
                        columnRedTarget[redIndex] = new int[] { valuetoDraw, redIndex };
                        redIndex++;
                    }

                    // Green
                    countToDraw = greenColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_ACTIVE] == 1 ? greenColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_COUNT] : 0;
                    valuetoDraw = greenColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_VALUE];
                    for(int i = 0; i < countToDraw; i++)
                    {
                        columnGreenTarget[greenIndex] = new int[] { valuetoDraw, greenIndex };
                        greenIndex++;
                    }

                    // Green
                    countToDraw = blueColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_ACTIVE] == 1 ? blueColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_COUNT] : 0;
                    valuetoDraw = blueColumns[y * targetWidth * 4 + x * 4 + COLUMNDATA_VALUE];
                    for(int i = 0; i < countToDraw; i++)
                    {
                        columnBlueTarget[blueIndex] = new int[] { valuetoDraw, blueIndex };
                        blueIndex++;
                    }
                }

                for (int y = 0; y < src.Height; y++)
                {
                    srcR = srcData[y * srcBmpData.Stride + x * 4 + R];
                    srcG = srcData[y * srcBmpData.Stride + x * 4 + G];
                    srcB = srcData[y * srcBmpData.Stride + x * 4 + B];
                    srcA = srcData[y * srcBmpData.Stride + x * 4 + A];


                    columnRed[y] = new int[] { srcR, y };
                    columnGreen[y] = new int[] { srcG, y };
                    columnBlue[y] = new int[] { srcB, y };

                    //columnRedTarget[y] = new int[] { target[y * stride + x * 4 + R], y };
                    //columnGreenTarget[y] = new int[] { target[y * stride + x * 4 + G], y };
                    //columnBlueTarget[y] = new int[] { target[y * stride + x * 4 + B], y };
                }
                // TODO Option to randomize columns before sorting.

                Helpers.Sort(columnRed, 0);
                Helpers.Sort(columnGreen, 0);
                Helpers.Sort(columnBlue, 0);
                Helpers.Sort(columnRedTarget, 0);
                Helpers.Sort(columnGreenTarget, 0);
                Helpers.Sort(columnBlueTarget, 0);

                columnRedTarget = Helpers.ThinArray(columnRedTarget, src.Height);
                columnGreenTarget = Helpers.ThinArray(columnGreenTarget, src.Height);
                columnBlueTarget = Helpers.ThinArray(columnBlueTarget, src.Height);


                // I'm looping through height, but really just through the sorted arrays. They just happen to have the same size.
                int targetPlace, targetColor;
                for (int notY = 0; notY < src.Height; notY++)
                {
                    // Basically we correspond the brightness-ordered pixels in each column. The column belonging to the shape dictates the place, the column belonging to the target the intensity.
                    if(notY < redCount)
                    {
                        targetPlace = columnRed[notY][1];
                        targetColor = columnRedTarget[notY][0];
                        target[targetPlace * stride + x * 4 + R] = (byte)targetColor;
                        target[targetPlace * stride + x * 4 + A] = 255;
                    }

                    if (notY < greenCount)
                    {
                        targetPlace = columnGreen[notY][1];
                        targetColor = columnGreenTarget[notY][0];
                        target[targetPlace * stride + x * 4 + G] = (byte)targetColor;
                        target[targetPlace * stride + x * 4 + A] = 255;
                    }
                    if (notY < blueCount)
                    {
                        targetPlace = columnBlue[notY][1];
                        targetColor = columnBlueTarget[notY][0];
                        target[targetPlace * stride + x * 4 + B] = (byte)targetColor;
                        target[targetPlace * stride + x * 4 + A] = 255;
                    }
                }
            }




            // Unlock source bitmap again.
            src.UnlockBits(srcBmpData);

            return target;
        }

        private byte[] RGBShape(byte[] target, int stride, int width, int height, Bitmap src)
        {
            // Copy source data into byte array
            BitmapData srcBmpData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] srcData = new byte[srcBmpData.Stride * srcBmpData.Height];
            Marshal.Copy(srcBmpData.Scan0, srcData, 0, srcData.Length);

            int srcR, srcG, srcB, srcA;

            int[][] columnRed = new int[src.Height][];
            int[][] columnGreen = new int[src.Height][];
            int[][] columnBlue = new int[src.Height][];

            int[][] columnRedTarget = new int[src.Height][];
            int[][] columnGreenTarget = new int[src.Height][];
            int[][] columnBlueTarget = new int[src.Height][];

            for (int x = 0; x < src.Width; x++)
            {
                for (int y = 0; y < src.Height; y++)
                {
                    srcR = srcData[y * srcBmpData.Stride + x * 4 + R];
                    srcG = srcData[y * srcBmpData.Stride + x * 4 + G];
                    srcB = srcData[y * srcBmpData.Stride + x * 4 + B];
                    srcA = srcData[y * srcBmpData.Stride + x * 4 + A];

                    
                    columnRed[y] = new int[]{ srcR,y};
                    columnGreen[y] = new int[] { srcG,y};
                    columnBlue[y] = new int[] { srcB, y};

                    columnRedTarget[y] = new int[] { target[y * stride + x * 4 + R], y };
                    columnGreenTarget[y] = new int[] { target[y * stride + x * 4 + G], y };
                    columnBlueTarget[y] = new int[] { target[y * stride + x * 4 + B], y };
                }
                Helpers.Sort(columnRed, 0);
                Helpers.Sort(columnGreen, 0);
                Helpers.Sort(columnBlue, 0);
                Helpers.Sort(columnRedTarget, 0);
                Helpers.Sort(columnGreenTarget, 0);
                Helpers.Sort(columnBlueTarget, 0);

                // I'm looping through height, but really just through the sorted arrays. They just happen to have the same size.
                int targetPlace, targetColor;
                for (int notY = 0; notY < src.Height; notY++)
                {
                    // Basically we correspond the brightness-ordered pixels in each column. The column belonging to the shape dictates the place, the column belonging to the target the intensity.
                    targetPlace = columnRed[notY][1];
                    targetColor = columnRedTarget[notY][0];
                    target[targetPlace * stride + x * 4 + R] = (byte)targetColor;
                    target[targetPlace * stride + x * 4 + A] = 255;

                    targetPlace = columnGreen[notY][1];
                    targetColor = columnGreenTarget[notY][0];
                    target[targetPlace * stride + x * 4 + G] = (byte)targetColor;
                    target[targetPlace * stride + x * 4 + A] = 255;

                    targetPlace = columnBlue[notY][1];
                    targetColor = columnBlueTarget[notY][0];
                    target[targetPlace * stride + x * 4 + B] = (byte)targetColor;
                    target[targetPlace * stride + x * 4 + A] = 255;
                }
            }




            // Unlock source bitmap again.
            src.UnlockBits(srcBmpData);

            return target;
        }
        
        const int COLUMNDATA_VALUE = 0;
        const int COLUMNDATA_COUNT = 1;
        const int COLUMNDATA_ACTIVE = 2;

        private int[] RGBScopizeColumns(int stride, int width, int height, Bitmap src, int channel = R)
        {
            int[] result = new int[targetWidth*255*4]; // This will be 4 values per pixel. first value is x position, second is actual value, third value is amount of such pixels to draw(intensity), fourth is basically a bool, saying whether the pixel is active (0 or 1)

            // Copy source data into byte array
            BitmapData srcBmpData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] srcData = new byte[srcBmpData.Stride * srcBmpData.Height];
            Marshal.Copy(srcBmpData.Scan0, srcData, 0, srcData.Length);

            int srcR, srcG, srcB, srcA;
            float posX, posY; // Position as a ratio 0 <= pos <= 1
            int destX, intensity;
            Random rnd = new Random();
            int iterCount, i;

            float srcIntensityHere;

            int index = 0;
            int offset = 0;

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    srcR = srcData[y * srcBmpData.Stride + x * 4 + R];
                    srcG = srcData[y * srcBmpData.Stride + x * 4 + G];
                    srcB = srcData[y * srcBmpData.Stride + x * 4 + B];
                    srcA = srcData[y * srcBmpData.Stride + x * 4 + A];

                    srcIntensityHere = (srcR + srcG + srcB) / 3;

                    // Pixel is active, drawit
                    //posX = (float)x / (src.Width - 1);
                    posY = (float)y / (src.Height - 1);

                    // posX remains x-position in final image
                    // posY becomes intensity (since that determines y-position in scope)
                    //destX = x; // destX = (int)Math.Round(posX * (width - 1)); (old method when source wasn't automatically resized to target)
                    intensity = (int)Math.Round((1 - posY) * 255);

                    iterCount = (int)Math.Round(Math.Pow(srcIntensityHere / 255, gamma) * maxIntensity);

                    offset = y * targetWidth * 4 + x * 4;
                    result[offset + COLUMNDATA_VALUE] = intensity;
                    result[offset + COLUMNDATA_COUNT] = iterCount;
                    result[offset + COLUMNDATA_ACTIVE] = 1;

                    if (srcA > 127 && srcIntensityHere >= srcThreshold)
                    {

                        result[offset + COLUMNDATA_ACTIVE] = 1;
                    } else
                    {

                        result[offset + COLUMNDATA_ACTIVE] = 0;
                    }
                }
            }




            // Unlock source bitmap again.
            src.UnlockBits(srcBmpData);

            return result;
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

            for(int y = 0; y < src.Height; y++)
            {
                for(int x = 0; x < src.Width; x++)
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
