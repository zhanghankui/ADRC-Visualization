using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADRCVisualization.Class_Files
{
    public class KalmanFilter
    {
        private double gain;
        private double filteredValue;


        public KalmanFilter(double gain)
        {
            this.gain = gain;
        }
        
        public double Filter(params double[] values)
        {
            int i = 0;
            double sum = 0;
            double avg;
            double gainInverse = (1 - gain);

            foreach (double value in values)
            {
                sum += value;
                i++;
            }

            avg = sum / i;

            filteredValue = (gain * filteredValue) + (gainInverse * avg);

            return filteredValue;
        }
    }
}
