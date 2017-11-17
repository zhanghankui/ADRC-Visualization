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
        private StreamWriter adrcFileWriter;
        private StreamWriter pidFileWriter;
        private int adrcCounter = 0;
        private int pidCounter = 0;

        private double SetPoint = 1;
        private double StartPoint = 30;
        private double PendulumLength = 2;
        private double WaitTimeForPID = 5;
        private double RunTime = 60;
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

        List<float> ADRCOutput = new List<float>();
        List<float> PIDOutput = new List<float>();

        List<float> ADRCAngle = new List<float>();
        List<float> PIDAngle = new List<float>();

        private System.Timers.Timer t;
        private System.Timers.Timer t2;
        private System.Timers.Timer t3;

        private FourierBitmap PIDFourierBitmap;
        private FourierBitmap ADRCFourierBitmap;

        private float FourierTolerance = 1f;

        private BackgroundWorker backgroundWorker;


        public Form1()
        {
            InitializeComponent();

            dateTime = DateTime.Now;
            correctionState = false;

            InitializeFileWriters();
            
            chart3.Series[0].Points.Add(0);
            chart4.Series[0].Points.Add(0);

            chart3.ChartAreas[0].AxisY.Maximum = maxOutput;
            chart3.ChartAreas[0].AxisY.Minimum = -maxOutput;

            chart4.ChartAreas[0].AxisY.Maximum = maxOutput;
            chart4.ChartAreas[0].AxisY.Minimum = -maxOutput;

            PIDFourierBitmap = new FourierBitmap(710, 350, (float)maxOutput);
            ADRCFourierBitmap = new FourierBitmap(710, 350, (float)maxOutput);


            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_CalculateFourierTransforms);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker_ChangeFourierTransforms);

            StartTimers();
            StopTimers();
        }

        private void InitializeFileWriters()
        {
            adrcFileWriter = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\ADRCData.csv");
            pidFileWriter = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\PIDData.csv");

            adrcFileWriter.WriteLine("r,c,b,h");
            adrcFileWriter.WriteLine(r + "," + c + "," + b + "," + hModifier);

            adrcFileWriter.WriteLine();

            pidFileWriter.WriteLine("KP,KI,KD");
            pidFileWriter.WriteLine(kp + "," + ki + "," + kd);

            pidFileWriter.WriteLine();
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
                    Interval = 60, //In milliseconds here
                    AutoReset = true //Stops it from repeating
                };
                t.Elapsed += new ElapsedEventHandler(SetInvertedPendulumAngle);
                t.Start();

                t2 = new System.Timers.Timer
                {
                    Interval = 1, //In milliseconds here
                    AutoReset = true //Stops it from repeating
                };
                t2.Elapsed += new ElapsedEventHandler(ChangeAngle);
                t2.Start();
                
                t3 = new System.Timers.Timer
                {
                    Interval = 150, //In milliseconds here
                    AutoReset = true //Stops it from repeating
                };
                t3.Elapsed += new ElapsedEventHandler(UpdateFourierTransforms);
                t3.Start();
            }));
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
        
        /*
         * Why FFT?
         * -Displays change of frequency of pendulum, slowing/speeding up
         * -Displays noise effectively from output
         * -Displays switching frequency of feedback controller
         * 
         */
        public void UpdateFourierTransforms(object sender, ElapsedEventArgs e)
        {
            if (!(DateTime.Now.Subtract(dateTime).TotalSeconds > RunTime))
            {
                this.BeginInvoke((Action)(() =>
                {
                    if (PIDOutput.ToArray().Length > 1)//FourierTransform.FourierMemory / 1.25)
                    {
                        chart3.Series[0].Points.Clear();
                        chart4.Series[0].Points.Clear();

                        chart3.Series[1].Points.Clear();
                        chart4.Series[1].Points.Clear();

                        while (!backgroundWorker.IsBusy)
                        {
                            try
                            {
                                backgroundWorker.RunWorkerAsync();
                                break;
                            }
                            catch(Exception ex)
                            {

                            }
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

                if (ADRCAngle.ToArray().Length > FourierTransform.FourierMemory)
                {
                    ADRCAngle.RemoveAt(0);
                    PIDAngle.RemoveAt(0);
                    ADRCOutput.RemoveAt(0);
                    PIDOutput.RemoveAt(0);
                }
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
        
        public void SetInvertedPendulumAngle(object sender, ElapsedEventArgs e)
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

                    Bitmap PID = BitmapModifier.RotateImage(new Bitmap(Path.GetFullPath(@"..\..\PID.png")), (float)anglePID + 180f);
                    Bitmap ADRC = BitmapModifier.RotateImage(new Bitmap(Path.GetFullPath(@"..\..\ADRC.png")), (float)angleADRC + 180f);

                    pictureBox1.Image = BitmapModifier.CombineImages(PID, ADRC);
                }));
            }
        }

        private void BackgroundWorker_CalculateFourierTransforms(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            
            float[] pidFFTW = FourierTransform.CalculateFFTW(PIDOutput.ToArray());
            float[] adrcFFTW = FourierTransform.CalculateFFTW(ADRCOutput.ToArray());

            float[] pidAngleFFTW = FourierTransform.CalculateFFTW(PIDAngle.ToArray());
            float[] adrcAngleFFTW = FourierTransform.CalculateFFTW(ADRCAngle.ToArray());

            e.Result = new float[4][] { pidFFTW, adrcFFTW, pidAngleFFTW, adrcAngleFFTW};
        }
        private void BackgroundWorker_ChangeFourierTransforms(object sender, RunWorkerCompletedEventArgs e)
        {
            
            foreach (float freq in ((float[][])(e.Result))[0])
            {
                chart3.Series[0].Points.Add(freq);
            }

            foreach (float freq in ((float[][])(e.Result))[1])
            {
                chart4.Series[0].Points.Add(freq);
            }

            foreach (float freq in ((float[][])(e.Result))[2])
            {
                chart3.Series[1].Points.Add(freq);
            }

            foreach (float freq in ((float[][])(e.Result))[3])
            {
                chart4.Series[1].Points.Add(freq);
            }

            double pidFFTWStdDev = MathFunctions.CalculateStdDev(Array.ConvertAll(((float[][])(e.Result))[0], x => (double)x).AsEnumerable());
            double adrcFFTWStdDev = MathFunctions.CalculateStdDev(Array.ConvertAll(((float[][])(e.Result))[1], x => (double)x).AsEnumerable());

            if (pidFFTWStdDev > FourierTolerance)
            {
                pidPictureBox.Image = PIDFourierBitmap.Calculate2DFourierTransform(((float[][])(e.Result))[0]);
            }

            if (adrcFFTWStdDev > FourierTolerance)
            {
                adrcPictureBox.Image = ADRCFourierBitmap.Calculate2DFourierTransform(((float[][])(e.Result))[1]);
            }
        }
    }
}
