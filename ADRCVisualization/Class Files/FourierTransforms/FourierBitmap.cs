using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADRCVisualization.Class_Files
{
    class FourierBitmap
    {
        private Bitmap bmp;
        private float maxOutput;
        private int x;
        private int y;

        public FourierBitmap(int x, int y, float maxOutput)
        {
            this.x = x;
            this.y = y;
            this.maxOutput = maxOutput;
            
            bmp = new Bitmap(1, y);
        }
        
        public Bitmap Calculate2DFourierTransform(float[] data)
        {
            int[] Adjusted = new int[y];

            if (y / data.Length > 1)
            {
                float value = 0;

                //Tested: Less data than pixels
                for (int i = 0; i < Adjusted.Length - (Adjusted.Length / data.Length); i += Adjusted.Length / data.Length)// 500 / 100 = 5
                {
                    float incrementingAmount = 0;

                    for (int k = 0; k < Adjusted.Length / data.Length; k++)
                    {
                        int tempIndexData = (i / (Adjusted.Length / data.Length));

                        if (tempIndexData >= data.Length)
                        {
                            //Console.WriteLine(tempVar + " " + data.Length);
                            break;
                        }
                        
                        int tempIndexAdjusted = i + k;

                        if (tempIndexAdjusted >= Adjusted.Length)
                        {
                            //Console.WriteLine(tempIndex + " " + Adjusted.Length);
                            break;
                        }

                        //Linear interpolation between data points, smooths out image
                        if (tempIndexData < data.Length - 1)
                        {
                            incrementingAmount = (data[tempIndexData + 1] - data[tempIndexData]) / (Adjusted.Length / data.Length);
                        }

                        value += incrementingAmount;

                        Adjusted[tempIndexAdjusted] = (int)value;// (int)data[tempIndexData];
                    }
                }
            }
            else
            {
                //Not Tested: More data than pixels
                for (int i = 0; i < y; i++)
                {
                    Adjusted[i] = (int)data[(int)(i * (data.Length / y))];
                }
            }

            Bitmap bmpColumn = new Bitmap(1, y);

            for (int i = 0; i < y; i++)
            {
                bmpColumn.SetPixel(0, i, ReturnColorFromGradient(Adjusted[i]));
            }

            bmp = BitmapModifier.MergeBitmaps(bmp, bmpColumn);

            return new Bitmap(bmp, new Size(x, y));
        }
        
        private Color ReturnColorFromGradient(float amplitude)
        {
            //zero is no energy, transparent
            //maxOutput is High
            //-maxOutput is High

            //Linear knots, no curve/smooth transitions
            //White -> Orange -> Blue -> Transparent

            //Absolute value of amplitude

            //255,255,255,255 -> 255,255,137,44 -> 255,44,119,255 -> 0,0,0,0
            
            //A: 255->255->255->0
            //R: 255->255->44 ->0
            //G: 255->137->119->0
            //B: 255->44 ->255->0

            amplitude = Math.Abs(amplitude);

            if (amplitude > maxOutput)
            {
                amplitude = maxOutput;
            }

            if (amplitude > maxOutput * 2 / 3)
            {//255,255,255,255 -> 255,255,137,44
                int a = 255;
                int r = 255;
                int g = (int)ScaleVariable(amplitude, (int)maxOutput, (int)(maxOutput * 2 / 3), 200, 137);
                int b = (int)ScaleVariable(amplitude, (int)maxOutput, (int)(maxOutput * 2 / 3), 120,  44);

                //Console.WriteLine(amplitude + " >2/3 " + a + " " + r + " " + g + " " + b);

                return Color.FromArgb(a, r, g, b);
            }
            else if (amplitude <= maxOutput * 2 / 3 && amplitude > maxOutput * 1 / 3)
            {//255,255,137,44 -> 255,44,119,255
                int a = (int)ScaleVariable(amplitude, (int)(maxOutput * 2 / 3), (int)(maxOutput * 1 / 3), 255, 200);
                int r = (int)ScaleVariable(amplitude, (int)(maxOutput * 2 / 3), (int)(maxOutput * 1 / 3), 255,  44);
                int g = (int)ScaleVariable(amplitude, (int)(maxOutput * 2 / 3), (int)(maxOutput * 1 / 3), 137, 119);
                int b = (int)ScaleVariable(amplitude, (int)(maxOutput * 2 / 3), (int)(maxOutput * 1 / 3), 44, 255);

                //Console.WriteLine(amplitude + " <2/3 1/3> " + a + " " + r + " " + g + " " + b);

                return Color.FromArgb(a, r, g, b);
            }
            else// if (amplitude <= maxOutput * 1 / 3)
            {//255,44,119,255 -> 0,0,0,0
                int a = (int)ScaleVariable(amplitude, (int)(maxOutput * 1 / 3), 0, 200, 0);
                int r = (int)ScaleVariable(amplitude, (int)(maxOutput * 1 / 3), 0, 44, 0);
                int g = (int)ScaleVariable(amplitude, (int)(maxOutput * 1 / 3), 0, 119, 0);
                int b = (int)ScaleVariable(amplitude, (int)(maxOutput * 1 / 3), 0, 255, 0);

                //Console.WriteLine(amplitude + " >1/3 " + a + " " + r + " " + g + " " + b);

                return Color.FromArgb(a, r, g, b);
            }
        }
        
        private float ScaleVariable(float datapoint, int OldMax, int OldMin, int NewMax, int NewMin)
        {
            return ((NewMax - NewMin) * (datapoint - OldMin) / (OldMax - OldMin)) + NewMin;
        }
    }
}
