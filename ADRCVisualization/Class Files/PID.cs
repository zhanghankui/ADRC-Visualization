using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADRCVisualization.Class_Files
{
    class PID
    {
        private double maxOutput;
        private double kp;
        private double ki;
        private double kd;
        private double integral;
        private double error;
        private double previousError;
        private double output;
        private DateTime time;

        public PID(double kp, double ki, double kd, double maxOutput)
        {
            this.kp = kp;
            this.ki = ki;
            this.kd = kd;
            this.maxOutput = maxOutput;

            time = DateTime.Now;
        }

        public double Calculate(double setpoint, double processVariable)
        {
            double POut, IOut, DOut, dt;
            
            DateTime currentTime = DateTime.Now;
            dt = currentTime.Subtract(time).TotalSeconds;

            if (dt > 0)
            {
                error = setpoint - processVariable;

                POut = kp * error;

                integral += error * dt;
                IOut = ki * integral;

                DOut = kd * ((error - previousError) / dt);

                output = Constrain(POut + IOut + DOut, -maxOutput, maxOutput);

                time = currentTime;
                previousError = error;
            }

            return output;
        }
        
        private double Constrain(double value, double minimum, double maximum)
        {
            if (value > maximum)
            {
                value = maximum;
            }
            else if (value < minimum)
            {
                value = minimum;
            }

            return value;
        }
    }
}
