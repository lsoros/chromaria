// BehaviorType.cs created with MonoDevelop
// User: joel at 1:44 PM 8/5/2009
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
namespace Chromaria.SharpNeatLib
{
    public class BehaviorType
    {        
        public List<double> behaviorList;
		public double[] objectives;
        public BehaviorType()
        {
         
        }
        public BehaviorType(BehaviorType copyFrom)
        {
            if(copyFrom.behaviorList!=null)
            behaviorList = new List<double>(copyFrom.behaviorList);
        }
    }
    
    public static class BehaviorDistance
    {
        public static double Distance(BehaviorType v1, BehaviorType v2)
        {
           double behavioralDistance = 0.0;

           if (v1 != null && v2 != null)
           {
               // Loop through each triple in the behavior vector
               for (int k = 0; k < v1.behaviorList.Count; k += 4)
               {
                   // Position component
                   behavioralDistance += Chromaria.Simulator.positionWeight * Scale(EuclideanDistance(v1.behaviorList[k], v1.behaviorList[k + 1], v2.behaviorList[k], v2.behaviorList[k + 1]), 0.0, Chromaria.Simulator.maxDistance, 0.0, 1.0);

                   // Heading component
                   behavioralDistance += Chromaria.Simulator.headingWeight * Math.Abs(Scale(v1.behaviorList[k + 3], -Math.PI, Math.PI, 0.0, 1.0) - Scale(v2.behaviorList[k + 3], -Math.PI, Math.PI, 0.0, 1.0));

                   // Planting component
                   behavioralDistance += Chromaria.Simulator.plantingWeight * (Math.Abs(v1.behaviorList[k + 2] - v2.behaviorList[k + 2]));
               }
           }
           return behavioralDistance;
        }

        public static double EuclideanDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        /// <summary>
        /// Scales a value which is expected to be in some range to some other range.
        /// </summary>
        /// <param name="value">Value to be scaled</param>
        /// <param name="min">Minimum of original range</param>
        /// <param name="max">Maximum of original range</param>
        /// <param name="scaledMin">Minimum of desired range</param>
        /// <param name="scaledMax">Maximum of desired range</param>
        /// <returns>The original value scaled to be in the desired range</returns>
        private static double Scale(double value, double min, double max, double scaledMin, double scaledMax)
        {
            return (((scaledMax - scaledMin) * (value - min)) / (max - min)) + scaledMin;
        }
    }
}
