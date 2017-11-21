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

        /// <summary>
        /// Implementation of a kalman filter. Commonly used to smooth output of noisy functions
        /// </summary>
        /// <param name="gain">Kalman Gain</param>
        public KalmanFilter(double gain)
        {
            this.gain = gain;
        }
        
        /// <summary>
        /// Filters the input variable(s)
        /// </summary>
        /// <param name="values">Allows input of a single or multiple input values to be added to the filter</param>
        /// <returns>Returns filtered value</returns>
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
