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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RGBScopizer
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        // Parameters
        private int targetWidth = 1920;
        private int targetHeight = 1080;
        //private int targetBitDepth = 8;
        private int maxIntensity = 20;
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

        public enum Mode
        {
            Random,
            Ordered,
            Shape
        }

        Mode thisMode = Mode.Random;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Data binding for parameter textboxes
        public Mode ThisMode
        {
            get { return thisMode; }
            set {
                if (value != thisMode)
                {
                    thisMode = value;
                    NotifyPropertyChanged();
                }}
        }
        public bool shapeRandomizationStage=false;

        public bool ShapeRandomizationStage
        {
            get { return shapeRandomizationStage; }
            set { shapeRandomizationStage = value; }
        }
        public bool shapeSeondOrderStage = false;

        public bool ShapeSeondOrderStage
        {
            get { return shapeSeondOrderStage; }
            set { shapeSeondOrderStage = value; }
        }
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
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
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
                shape_radiobtn.IsEnabled = true;
                ThisMode = Mode.Shape;
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
                redColumns = RGBScopizeColumns(bmpData.Stride, targetWidth,targetHeight, redResized, R);
            } else
            {
                redColumns = new int[targetWidth * 255 * 3]; // just empty, who cares.
            }
            if (greenSrc != null)
            {
                greenResized = new Bitmap(greenSrc, targetWidth, 255);
                greenColumns = RGBScopizeColumns(bmpData.Stride, targetWidth, targetHeight, greenResized, G);
            }
            else
            {
                greenColumns = new int[targetWidth * 255 * 3]; // just empty, who cares.
            }
            if (blueSrc != null)
            {
                blueResized = new Bitmap(blueSrc, targetWidth, 255);
                blueColumns = RGBScopizeColumns(bmpData.Stride, targetWidth, targetHeight, blueResized, B);
            }
            else
            {
                blueColumns = new int[targetWidth * 255 * 3]; // just empty, who cares.
            }
            Bitmap shapeResized = shapeSrc == null ? null : new Bitmap(shapeSrc, targetWidth, targetHeight);
            pixels = RGBDrawColumns(pixels, bmpData.Stride, targetWidth, targetHeight, shapeResized, redColumns, greenColumns, blueColumns);


            Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);

            result.UnlockBits(bmpData);

            int littleSizeX = (int)Math.Ceiling((double)targetWidth / blockSizeX);
            int littleSizeY = (int)Math.Ceiling((double)targetHeight / blockSizeY);
            result = Helpers.ResizeBitmapNN(result, littleSizeX, littleSizeY);
            result = Helpers.ResizeBitmapNN(result,littleSizeX*blockSizeX,littleSizeY*blockSizeY);
            result = result.Clone(new Rectangle(0, 0, targetWidth, targetHeight), result.PixelFormat);

            final_img.Source = Helpers.BitmapToImageSource((Bitmap)result);

            btnSave.IsEnabled = true;

        }


        private byte[] RGBDrawColumns(byte[] target, int stride, int width, int height, Bitmap shape, int[] redColumns, int[] greenColumns, int[] blueColumns)
        {
            // Copy source data into byte array
            if(thisMode != Mode.Shape)
            {
                shape = new Bitmap(1, 1);
            }
            
            BitmapData srcBmpData = shape.LockBits(new Rectangle(0, 0, shape.Width, shape.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] srcData = new byte[srcBmpData.Stride * srcBmpData.Height];
            Marshal.Copy(srcBmpData.Scan0, srcData, 0, srcData.Length);
            

            int srcR, srcG, srcB, srcA;

            int[][] columnRedTarget;
            int[][] columnGreenTarget;
            int[][] columnBlueTarget;

            int[][] columnRed = new int[targetHeight][];
            int[][] columnGreen = new int[targetHeight][];
            int[][] columnBlue = new int[targetHeight][];

            int redCount, greenCount, blueCount;
            int redIndex, greenIndex, blueIndex;
            int countToDraw, valuetoDraw;

            Random r = new Random();

            for (int x = 0; x < targetWidth; x++)
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
                    redCount += redColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_ACTIVE] == 1 ? redColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_COUNT] : 0;
                    greenCount += greenColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_ACTIVE] == 1 ? greenColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_COUNT] : 0;
                    blueCount += blueColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_ACTIVE] == 1 ? blueColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_COUNT] : 0;
                }

                //Now create arrays
                columnRedTarget = new int[redCount][];
                columnGreenTarget = new int[greenCount][];
                columnBlueTarget = new int[blueCount][];

                for (int y = 0; y < 255; y++)
                {
                    // Red
                    countToDraw = redColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_ACTIVE] == 1 ? redColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_COUNT] : 0;
                    valuetoDraw = redColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_VALUE];
                    for(int i = 0; i < countToDraw; i++)
                    {
                        columnRedTarget[redIndex] = new int[] { valuetoDraw, redIndex };
                        redIndex++;
                    }

                    // Green
                    countToDraw = greenColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_ACTIVE] == 1 ? greenColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_COUNT] : 0;
                    valuetoDraw = greenColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_VALUE];
                    for(int i = 0; i < countToDraw; i++)
                    {
                        columnGreenTarget[greenIndex] = new int[] { valuetoDraw, greenIndex };
                        greenIndex++;
                    }

                    // Green
                    countToDraw = blueColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_ACTIVE] == 1 ? blueColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_COUNT] : 0;
                    valuetoDraw = blueColumns[y * targetWidth * 3 + x * 3 + COLUMNDATA_VALUE];
                    for(int i = 0; i < countToDraw; i++)
                    {
                        columnBlueTarget[blueIndex] = new int[] { valuetoDraw, blueIndex };
                        blueIndex++;
                    }
                }
                if (thisMode == Mode.Shape)
                {
                    for (int y = 0; y < targetHeight; y++)
                    {
                        srcR = srcData[y * srcBmpData.Stride + x * 4 + R];
                        srcG = srcData[y * srcBmpData.Stride + x * 4 + G];
                        srcB = srcData[y * srcBmpData.Stride + x * 4 + B];
                        srcA = srcData[y * srcBmpData.Stride + x * 4 + A];


                        columnRed[y] = new int[] { srcR, y };
                        columnGreen[y] = new int[] { srcG, y };
                        columnBlue[y] = new int[] { srcB, y };

                    }
                    if (shapeRandomizationStage)
                    {
                        Helpers.Shuffle(r,columnRed);
                        Helpers.Shuffle(r,columnGreen);
                        Helpers.Shuffle(r,columnBlue);
                        Helpers.Shuffle(r, columnRedTarget);
                        Helpers.Shuffle(r, columnGreenTarget);
                        Helpers.Shuffle(r, columnBlueTarget);
                    }

                    if (shapeSeondOrderStage)
                    {
                        Helpers.Sort(columnRed, 0, 1);
                        Helpers.Sort(columnGreen, 0, 1);
                        Helpers.Sort(columnBlue, 0, 1);
                    }
                    else
                    {
                        Helpers.Sort(columnRed, 0);
                        Helpers.Sort(columnGreen, 0);
                        Helpers.Sort(columnBlue, 0);
                    }

                }
                // TODO Option to randomize columns before sorting.

                Helpers.Sort(columnRedTarget, 0);
                Helpers.Sort(columnGreenTarget, 0);
                Helpers.Sort(columnBlueTarget, 0);

                
                columnRedTarget = Helpers.ThinArray(columnRedTarget, targetHeight);
                columnGreenTarget = Helpers.ThinArray(columnGreenTarget, targetHeight);
                columnBlueTarget = Helpers.ThinArray(columnBlueTarget, targetHeight);


                int[] randomDictionaryR = new int[1];
                int[] randomDictionaryG = new int[1];
                int[] randomDictionaryB = new int[1];
                if (thisMode == Mode.Random)
                {
                    randomDictionaryR = Helpers.CreateNumberSequence(targetHeight);
                    randomDictionaryG = Helpers.CreateNumberSequence(targetHeight);
                    randomDictionaryB = Helpers.CreateNumberSequence(targetHeight);
                    Helpers.Shuffle(r, randomDictionaryR);
                    Helpers.Shuffle(r, randomDictionaryG);
                    Helpers.Shuffle(r, randomDictionaryB);
                }

                // I'm looping through height, but really just through the sorted arrays. They just happen to have the same size.
                int targetPlace, targetColor;
                for (int notY = 0; notY < targetHeight; notY++)
                {
                    // Basically we correspond the brightness-ordered pixels in each column. The column belonging to the shape dictates the place, the column belonging to the target the intensity.
                    if(notY < redCount)
                    {
                        switch (thisMode)
                        {
                            case Mode.Shape:
                                targetPlace = columnRed[notY][1];
                                break;
                            case Mode.Random:
                                targetPlace = randomDictionaryR[notY];
                                break;
                            case Mode.Ordered:
                            default:
                                targetPlace = notY;
                                break;
                        }
                        targetColor = columnRedTarget[notY][0];
                        target[targetPlace * stride + x * 4 + R] = (byte)targetColor;
                        target[targetPlace * stride + x * 4 + A] = 255;
                    }

                    if (notY < greenCount)
                    {
                        switch (thisMode)
                        {
                            case Mode.Shape:
                                targetPlace = columnGreen[notY][1];
                                break;
                            case Mode.Random:
                                targetPlace = randomDictionaryG[notY];
                                break;
                            case Mode.Ordered:
                            default:
                                targetPlace = notY;
                                break;
                        }
                        targetColor = columnGreenTarget[notY][0];
                        target[targetPlace * stride + x * 4 + G] = (byte)targetColor;
                        target[targetPlace * stride + x * 4 + A] = 255;
                    }
                    if (notY < blueCount)
                    {
                        switch (thisMode)
                        {
                            case Mode.Shape:
                                targetPlace = columnBlue[notY][1];
                                break;
                            case Mode.Random:
                                targetPlace = randomDictionaryB[notY];
                                break;
                            case Mode.Ordered:
                            default:
                                targetPlace = notY;
                                break;
                        }
                        targetColor = columnBlueTarget[notY][0];
                        target[targetPlace * stride + x * 4 + B] = (byte)targetColor;
                        target[targetPlace * stride + x * 4 + A] = 255;
                    }
                }
            }




            // Unlock source bitmap again.
            shape.UnlockBits(srcBmpData);

            return target;
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(shapeRandomizationStage.ToString());
        }

        const int COLUMNDATA_VALUE = 0;
        const int COLUMNDATA_COUNT = 1;
        const int COLUMNDATA_ACTIVE = 2;

        public event PropertyChangedEventHandler PropertyChanged;

        private int[] RGBScopizeColumns(int stride, int width, int height, Bitmap src, int channel = R)
        {
            int[] result = new int[targetWidth*255*3]; // This will be 4 values per pixel. first value is x position, second is actual value, third value is amount of such pixels to draw(intensity), fourth is basically a bool, saying whether the pixel is active (0 or 1)

            // Copy source data into byte array
            BitmapData srcBmpData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] srcData = new byte[srcBmpData.Stride * srcBmpData.Height];
            Marshal.Copy(srcBmpData.Scan0, srcData, 0, srcData.Length);

            int srcR, srcG, srcB, srcA;
            float posY; // Position as a ratio 0 <= pos <= 1
            int intensity;
            Random rnd = new Random();
            int iterCount;

            float srcIntensityHere;

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
                    posY = (float)y / (src.Height - 1);

                    // posX remains x-position in final image
                    // posY becomes intensity (since that determines y-position in scope)
                    intensity = (int)Math.Round((1 - posY) * 255);

                    iterCount = (int)Math.Round(Math.Pow(srcIntensityHere / 255, gamma) * maxIntensity);

                    offset = y * targetWidth * 3 + x * 3;
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

    }
}
