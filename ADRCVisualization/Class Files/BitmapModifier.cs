using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADRCVisualization.Class_Files
{
    static class BitmapModifier
    {
        public static Bitmap CombineImages(Bitmap bmp1, Bitmap bmp2)
        {
            Bitmap target = new Bitmap(bmp1.Width, bmp1.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(target);
            g.CompositingMode = CompositingMode.SourceOver; // this is the default, but just to be clear

            g.DrawImage(bmp1, 0, 0);
            g.DrawImage(bmp2, 0, 0);

            return target;
        }

        public static Bitmap RotateImage(Bitmap b, float angle)
        {
            if (!float.IsNaN(angle))
            {
                Bitmap returnBitmap = new Bitmap(b.Width, b.Height);
                Graphics g = Graphics.FromImage(returnBitmap);

                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.TranslateTransform((float)b.Width / 2, (float)b.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-(float)b.Width / 2, -(float)b.Height / 2);
                g.DrawImage(b, 0, 0, b.Width, b.Height);  //My Final Solution :3

                return returnBitmap;
            }
            else
            {
                return b;
            }
        }
    }
}
