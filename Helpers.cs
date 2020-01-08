using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace RGBScopizer
{
    static class Helpers
    {

        // Fisher-Yates algorithm AKA the Knuth Shuffle
        // Found here: https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
        static public void Shuffle<T>(this Random rng, T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        static public int[] CreateNumberSequence(int count)
        {
            int[] sequence = new int[count];
            for(int i = 0; i < count; i++)
            {
                sequence[i] = i;
            }
            return sequence;
        }

        static public Bitmap ResizeBitmapNN(Bitmap sourceBMP, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(sourceBMP, 0, 0, width, height);
            }
            return result;
        }


        // from: https://stackoverflow.com/questions/232395/how-do-i-sort-a-two-dimensional-rectangular-array-in-c
        public static void Sort<T>(T[][] data, int col)
        {
            Comparer<T> comparer = Comparer<T>.Default;
            Array.Sort<T[]>(data, (x, y) => comparer.Compare(x[col], y[col]));
        }

        public static T[] ThinArray<T>(T[] input, int maxCount){

            if(input.Count() <= maxCount)
            {
                return input;
            } else
            {
                T[] thinnedArray = new T[maxCount];
                for(int i=0; i < maxCount; i++)
                {
                    thinnedArray[i] = input[(int)Math.Round(((float)i/(maxCount-1))*(input.Count()-1))];
                }
                return thinnedArray;
            }
        }

        static public BitmapImage BitmapToImageSource(Bitmap bitmap)
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
    }
}
