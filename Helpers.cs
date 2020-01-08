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
    class Helpers
    {

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
                //Random r = new Random();
                /*int elementsToDitchCount = input.Count() - maxCount;
                int[] elementsToDitch = new int[elementsToDitchCount];
                int indexToDitch = r.Next(0, input.Count() - 1);
                for(int i = 0; i < elementsToDitchCount; i++)
                {
                    while(Array.Exists(elementsToDitch, element => element == indexToDitch))
                    {
                        indexToDitch = r.Next(0, input.Count() - 1);
                    }
                    elementsToDitch[i] = indexToDitch;
                }*/

                T[] thinnedArray = new T[maxCount];
                //int newIndex = 0;
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
