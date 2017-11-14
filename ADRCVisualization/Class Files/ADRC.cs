using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADRCVisualization.Class_Files
{
    class ADRC//ActiveDisturbanceRejectionControl
    {
        private TrackingDifferentiator TrackingDifferentiator;
        private ExtendedStateObserver ExtendedStateObserver;
        private NonlinearCombiner NonlinearCombiner;

        private DateTime dateTime;

        private double amplificationCoefficient;
        private double dampingCoefficient;
        private double precisionCoefficient;//0.2
        private double samplingPeriod;//0.05
        private double plantCoefficient;//b0 approximation
        private double precisionModifier;
        private double maxOutput;

        private double output;

        public ADRC(double amplificationCoefficient, double dampingCoefficient, double plantCoefficient, double precisionModifier, double maxOutput)
        {
            this.amplificationCoefficient = amplificationCoefficient;
            this.dampingCoefficient = dampingCoefficient;
            this.plantCoefficient = plantCoefficient;
            this.precisionModifier = precisionModifier;
            this.maxOutput = maxOutput;

            TrackingDifferentiator = new TrackingDifferentiator(amplificationCoefficient);
            ExtendedStateObserver = new ExtendedStateObserver(false);
            NonlinearCombiner = new NonlinearCombiner(amplificationCoefficient, dampingCoefficient);

            dateTime = DateTime.Now;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="setpoint"></param>
        /// <param name="pv"></param>
        /// <returns></returns>
        public double Calculate(double setpoint, double processVariable)
        {
            //samplingPeriod = DateTime.Now.Subtract(dateTime).TotalSeconds + 0.08;

            //Console.WriteLine(DateTime.Now.Subtract(dateTime).TotalSeconds);

            samplingPeriod = 0.5;

            if (samplingPeriod > 0)
            {
                precisionCoefficient = samplingPeriod * precisionModifier;

                Tuple<double, double> td = TrackingDifferentiator.Track(setpoint, samplingPeriod);//double input
                Tuple<double, double, double> eso = ExtendedStateObserver.ObserveState(samplingPeriod, td, output, plantCoefficient, processVariable);//double u, double y, double b0

                output = NonlinearCombiner.Combine(td, plantCoefficient, eso, precisionCoefficient);

                //Console.WriteLine(output);

                dateTime = DateTime.Now;
            }

            return Constrain(output, -maxOutput, maxOutput);
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
