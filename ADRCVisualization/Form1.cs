using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using ADRCVisualization.Class_Files;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ADRCVisualization
{
    public partial class Form1 : Form
    {
        private float anglePID;
        private float angleADRC;
        private double outputPID;
        private double outputADRC;
        private InvertedPendulum invertedPendulumPID;
        private InvertedPendulum invertedPendulumADRC;
        private PID pid;
        private ADRC adrc;
        private DateTime dateTime;
        private bool correctionState;

        private double SetPoint = 180;
        private double StartPoint = 150;
        private double PendulumLength = 2;
        private double WaitTimeForPID = 5.5;
        private double RunTime = 30;
        private double NoiseFactor = 0;
        private bool createInvertPendulum = false;

        public Form1()
        {
            InitializeComponent();

            dateTime = DateTime.Now;
            correctionState = false;

            StartTimers();
        }

        private async void StartTimers()
        {
            await Task.Delay(50);

            this.BeginInvoke((Action)(() =>
            {
                invertedPendulumPID = new InvertedPendulum(StartPoint, PendulumLength);//start angle, arm length
                invertedPendulumADRC = new InvertedPendulum(StartPoint, PendulumLength);//start angle, arm length
                
                System.Timers.Timer t = new System.Timers.Timer
                {
                    Interval = 50, //In milliseconds here
                    AutoReset = true //Stops it from repeating
                };
                t.Elapsed += new ElapsedEventHandler(SetPictureBoxAngle);
                t.Start();

                System.Timers.Timer t2 = new System.Timers.Timer
                {
                    Interval = 1, //In milliseconds here
                    AutoReset = true //Stops it from repeating
                };
                t2.Elapsed += new ElapsedEventHandler(ChangeAngle);
                t2.Start();
            }));
        }
        
        public void ChangeAngle(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Subtract(dateTime).TotalSeconds > WaitTimeForPID)
            {
                if (!createInvertPendulum)
                {
                    pid = new PID(0.3615, 0, 0.155, 1000);//0.155
                    //adrc = new ADRC(3.5, 0.069, 0.0059, 100, 1000);// r, c, , precisionCoefficient = samplingPeriod * precisionModifier, with dT at 1 sec

                    adrc = new ADRC(0.0823, 1.21, 0.0059, 100, 1000);

                    //adrc = new ADRC(10000, 0.01, 105, 4, 1000);

                    /*
                       3.5, 0.069, 0.0059, 100, dT = 1sec,
                       
                    */
                    createInvertPendulum = true;
                }

                correctionState = true;
                outputPID = pid.Calculate(SetPoint, anglePID);
                outputADRC = adrc.Calculate(SetPoint, angleADRC);
            }

            Random r = new Random();

            double noise = r.NextDouble() * NoiseFactor * (r.Next(0, 1) * 2 - 1);

            float tempAnglePID = (float)(invertedPendulumPID.Step(-outputPID) * 180 / Math.PI + noise) % 360;
            float tempAngleADRC = (float)(invertedPendulumADRC.Step(-outputADRC) * 180 / Math.PI + noise) % 360;

            tempAnglePID = float.IsNaN(tempAnglePID) ? 0 : tempAnglePID;
            tempAngleADRC = float.IsNaN(tempAngleADRC) ? 0 : tempAngleADRC;

            anglePID = tempAnglePID < 0 ? tempAnglePID + 360 : tempAnglePID;
            angleADRC = tempAngleADRC < 0 ? tempAngleADRC + 360 : tempAngleADRC;
        }
        
        public void SetPictureBoxAngle(object sender, ElapsedEventArgs e)
        {
            if (!(DateTime.Now.Subtract(dateTime).TotalSeconds > RunTime))
            {
                this.BeginInvoke((Action)(() =>
                {
                    label1.Text = "Correction State: " + correctionState.ToString();

                    chart1.Series[0].Points.Add(anglePID);
                    chart1.Series[1].Points.Add(angleADRC);
                    chart1.Series[2].Points.Add(180);

                    chart2.Series[0].Points.Add(outputPID);
                    chart2.Series[1].Points.Add(outputADRC);

                    chart1.Series[0].Color = Color.DarkGreen;
                    chart1.Series[1].Color = Color.Red;
                    chart1.Series[2].Color = Color.Magenta;

                    chart2.Series[0].Color = Color.DarkGreen;
                    chart2.Series[1].Color = Color.Red;

                    Bitmap PID = RotateImage(new Bitmap(System.IO.Path.GetFullPath(@"..\..\PID.png")), (float)anglePID);
                    Bitmap ADRC = RotateImage(new Bitmap(System.IO.Path.GetFullPath(@"..\..\ADRC.png")), (float)angleADRC);

                    pictureBox1.Image = CombineImages(PID, ADRC);

                    //pictureBox1.Refresh();
                }));
            }
        }

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
