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
using System.IO;
using System.Runtime.InteropServices;
using FFTWSharp;

namespace ADRCVisualization
{
    public partial class Form1 : Form
    {
        private double anglePID;
        private double angleADRC;
        private double outputPID;
        private double outputADRC;
        private double PIDCalculationSetpoint;
        private double ADRCCalculationSetpoint;
        private InvertedPendulum invertedPendulumPID;
        private InvertedPendulum invertedPendulumADRC;
        private PID pid;
        private ADRC adrc;
        private DateTime dateTime;
        private bool correctionState;
        private StreamWriter adrcFileWriter = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\ADRCData.csv");
        private StreamWriter pidFileWriter = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\PIDData.csv");
        private int adrcCounter = 0;
        private int pidCounter = 0;

        private double SetPoint = 0;
        private double StartPoint = 30;
        private double PendulumLength = 2;
        private double WaitTimeForPID = 5;
        private double RunTime = 40;
        private double NoiseFactor = 0;
        private bool initializeFeedbackControllers = false;

        private double kp = 0.36;
        private double ki = 0;
        private double kd = 0.18;

        private double r = 80;
        private double c = 500;
        private double b = 0.5;
        private double hModifier = 0.005;

        private double maxOutput = 1000;
        private fftwf fftwf;

        List<float> ADRCOutput = new List<float>();
        List<float> PIDOutput = new List<float>();

        List<float> ADRCAngle = new List<float>();
        List<float> PIDAngle = new List<float>();

        private System.Timers.Timer t;
        private System.Timers.Timer t2;
        private System.Timers.Timer t3;

        public Form1()
        {
            InitializeComponent();

            dateTime = DateTime.Now;
            correctionState = false;

            adrcFileWriter.WriteLine("r,c,b,h");
            adrcFileWriter.WriteLine(r + "," + c + "," + b + "," + hModifier);

            adrcFileWriter.WriteLine();

            pidFileWriter.WriteLine("KP,KI,KD");
            pidFileWriter.WriteLine(kp + "," + ki + "," + kd);

            pidFileWriter.WriteLine();


            chart3.Series[0].Points.Add(0);
            chart4.Series[0].Points.Add(0);

            chart3.ChartAreas[0].AxisY.Maximum = maxOutput;
            chart3.ChartAreas[0].AxisY.Minimum = -maxOutput;

            chart4.ChartAreas[0].AxisY.Maximum = maxOutput;
            chart4.ChartAreas[0].AxisY.Minimum = -maxOutput;

            StartTimers();
        }

        private async void StopTimers()
        {
            await Task.Delay((int)RunTime * 1000);
            
            this.BeginInvoke((Action)(() =>
            {
                t.Stop();
                t2.Stop();
                t3.Stop();
            }));
        }

        private async void StartTimers()
        {
            await Task.Delay(50);

            this.BeginInvoke((Action)(() =>
            {
                invertedPendulumPID = new InvertedPendulum(StartPoint, PendulumLength);//start angle, arm length
                invertedPendulumADRC = new InvertedPendulum(StartPoint, PendulumLength);//start angle, arm length
                
                t = new System.Timers.Timer
                {
                    Interval = 50, //In milliseconds here
                    AutoReset = true //Stops it from repeating
                };
                t.Elapsed += new ElapsedEventHandler(SetPictureBoxAngle);
                t.Start();

                t2 = new System.Timers.Timer
                {
                    Interval = 0.1, //In milliseconds here
                    AutoReset = true //Stops it from repeating
                };
                t2.Elapsed += new ElapsedEventHandler(ChangeAngle);
                t2.Start();
                
                t3 = new System.Timers.Timer
                {
                    Interval = 50, //In milliseconds here
                    AutoReset = true //Stops it from repeating
                };
                t3.Elapsed += new ElapsedEventHandler(UpdateFourierTransforms);
                t3.Start();
            }));
        }

        public void UpdateFourierTransforms(object sender, ElapsedEventArgs e)
        {
            if (!(DateTime.Now.Subtract(dateTime).TotalSeconds > RunTime))
            {
                this.BeginInvoke((Action)(() =>
                {
                    if (PIDOutput.ToArray().Length > 20)
                    {
                        chart3.Series[0].Points.Clear();
                        chart4.Series[0].Points.Clear();

                        chart3.Series[1].Points.Clear();
                        chart4.Series[1].Points.Clear();

                        //List<float> tempADRCOutput = new List<float>();
                        //List<float> tempPIDOutput = new List<float>();

                        float[] pidFFTW = CalculateFFTW(PIDOutput.ToArray());
                        float[] adrcFFTW = CalculateFFTW(ADRCOutput.ToArray());

                        float[] pidAngleFFTW = CalculateFFTW(PIDAngle.ToArray());
                        float[] adrcAngleFFTW = CalculateFFTW(ADRCAngle.ToArray());

                        foreach (float freq in pidFFTW)
                        {
                            chart3.Series[0].Points.Add(freq);
                        }

                        foreach (float freq in adrcFFTW)
                        {
                            chart4.Series[0].Points.Add(freq);
                        }

                        foreach (float freq in pidAngleFFTW)
                        {
                            chart3.Series[1].Points.Add(freq);
                        }

                        foreach (float freq in adrcAngleFFTW)
                        {
                            chart4.Series[1].Points.Add(freq);
                        }
                    }

                }));
            }
        }
        
        public void ChangeAngle(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Subtract(dateTime).TotalSeconds > WaitTimeForPID)
            {
                if (!initializeFeedbackControllers)
                {
                    pid = new PID(kp, ki, kd, maxOutput);
                    adrc = new ADRC(r, c, b, hModifier, maxOutput);

                    if (angleADRC > 180)
                    {
                        ADRCCalculationSetpoint = SetPoint + 360;
                    }
                    else
                    {
                        ADRCCalculationSetpoint = SetPoint;
                    }

                    if (anglePID > 180)
                    {
                        PIDCalculationSetpoint = SetPoint + 360;
                    }
                    else
                    {
                        PIDCalculationSetpoint = SetPoint;
                    }
                    
                    initializeFeedbackControllers = true;
                }

                correctionState = true;
                outputPID = pid.Calculate(PIDCalculationSetpoint, anglePID);
                outputADRC = adrc.Calculate(ADRCCalculationSetpoint, angleADRC);

                pidFileWriter.WriteLine(pidCounter + "," + outputPID + "," + anglePID);
                adrcFileWriter.WriteLine(adrcCounter + "," + outputADRC + "," + angleADRC);

                pidCounter++;
                adrcCounter++;

                ADRCAngle.Add((float)angleADRC);
                PIDAngle.Add((float)anglePID);
                
                ADRCOutput.Add((float)outputADRC);
                PIDOutput.Add((float)outputPID);
            }

            Random rand = new Random();

            double noise = rand.NextDouble() * NoiseFactor * (rand.Next(0, 1) * 2 - 1);

            double tempAnglePID = (invertedPendulumPID.Step(-outputPID) * 180 / Math.PI + noise);// % 360;
            double tempAngleADRC = (invertedPendulumADRC.Step(-outputADRC) * 180 / Math.PI + noise);// % 360;
            
            //tempAnglePID = double.IsNaN(tempAnglePID) ? 0 : tempAnglePID;
            //tempAngleADRC = double.IsNaN(tempAngleADRC) ? 0 : tempAngleADRC;

            anglePID = tempAnglePID;
            angleADRC = tempAngleADRC;

            //anglePID = tempAnglePID < 0 ? tempAnglePID + 360 : tempAnglePID;
            //angleADRC = tempAngleADRC < 0 ? tempAngleADRC + 360 : tempAngleADRC;
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
                    chart1.Series[2].Points.Add(SetPoint);
                    chart1.Series[3].Points.Add(SetPoint + 360);

                    chart2.Series[0].Points.Add(outputPID);
                    chart2.Series[1].Points.Add(outputADRC);

                    chart1.Series[0].Color = Color.DarkGreen;
                    chart1.Series[1].Color = Color.Red;
                    chart1.Series[2].Color = Color.Magenta;

                    chart2.Series[0].Color = Color.DarkGreen;
                    chart2.Series[1].Color = Color.Red;

                    Bitmap PID = RotateImage(new Bitmap(Path.GetFullPath(@"..\..\PID.png")), (float)anglePID + 180f);
                    Bitmap ADRC = RotateImage(new Bitmap(Path.GetFullPath(@"..\..\ADRC.png")), (float)angleADRC + 180f);

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

        public float[] CalculateFFTW(float[] data)
        {

            IntPtr fplan1, pin, pout;
            float[] fin, fout;
            int n;
            
            //data = FetchData();

            n = data.Length;// 16000;

            fftwf = new fftwf();

            pin = fftwf.malloc(n * 8);
            pout = fftwf.malloc(n * 8);
            fin = new float[n];
            fout = new float[n];

            fplan1 = fftwf.dft_r2c_1d(n, pin, pout, fftw_flags.Estimate);

            fin = data;//for (int i = 0; i < n * 2; i++) fin[i] = (float)Math.Sin(i * Math.PI / 180);// i % 5;
            fout = data;//for (int i = 0; i < n * 2; i++) fout[i] = (float)Math.Sin(i * Math.PI / 180);// i % 5;

            Marshal.Copy(fin, 0, pin, n);
            Marshal.Copy(fout, 0, pout, n);

            fftwf.execute(fplan1);

            Marshal.Copy(pout, fout, 0, n);
            
            fftwf.free(pin);
            fftwf.free(pout);
            fftwf.destroy_plan(fplan1);

            return fout;
        }
    }
}
