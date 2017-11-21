using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADRCVisualization.Class_Files
{
    class InvertedPendulum
    {
        private DateTime date;
        private double velocity = 0;
        private double theta;
        private double radius;
        private double gravity = -9.81;
        private double mass;

        public InvertedPendulum(double theta, double radius, double mass)
        {
            date = DateTime.Now;
            this.theta = theta * Math.PI / 180;
            this.radius = radius;
            this.mass = mass;
        }

        public double Step(double force)//horizontal force
        {
            double arcDistance;
            double dT;
            double alpha;
            int arcDistanceSign;

            dT = DateTime.Now.Subtract(date).TotalSeconds;
            if (dT != 0)
            {
                arcDistance = velocity * dT;

                //Console.WriteLine(Math.Asin(Math.Abs(arcDistance) / radius));

                if (Math.Abs(arcDistance / radius) > 1)
                {
                    arcDistanceSign = Math.Sign(arcDistance);
                    arcDistance = Math.Abs(arcDistance);

                    double arcDistanceAdder = 0;

                    while (arcDistance / radius > 1)
                    {
                        //Console.WriteLine(arcDistance);

                        if (arcDistance / radius > 1)
                        {
                            arcDistanceAdder += Math.Asin(1);
                            arcDistance -= 1;
                        }
                        else
                        {
                            arcDistanceAdder += Math.Asin(arcDistance);
                            arcDistance -= arcDistance;
                        }
                    }

                    arcDistance = arcDistance * arcDistanceSign;
                }
                
                alpha = Math.Asin(arcDistance / radius);

                velocity = velocity + mass * gravity * Math.Sin(theta) * dT + force * dT;

                date = DateTime.Now;

                velocity *= 0.9985;//resistance

                theta -= alpha;

                //Console.WriteLine(force + " " + velocity + " " + arcDistance);
                //Console.WriteLine(theta + " " + radius + " " + dT + " " + arcDistance + " " + alpha + " " + velocity);
            }


            return theta;
        }
    }
}
