using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Chromaria
{
    /// <summary>
    /// This class contains some auxilliary functions for converting between degrees and radians, polar and cartesian values, scaling between ranges, and rotating points.
    /// </summary>
    public class MathHelpers
    {
        /// <summary>
        /// Rotate a point around its center by some number of radians.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="angleInRadians"></param>
        /// <returns></returns>
        public static Point rotatePoint(int x, int y, double angleInRadians)
        {
            // Calculated using the following formulas:
            // x' = x*cos(angle) - y*sin(angle)
            // y' = x*sin(angle) + y*cos(angle) 

            // Convert the XNA radians to real world radians (see http://stackoverflow.com/questions/3527531/xna-question-about-rotation/3527725#3527725)
            double lostPrecision = 0.000000043711390063094768;
            double convertedAngleInRadians = (2 * Math.PI) - angleInRadians + lostPrecision;

            // NOTE: all sine calls here have been negated to deal with the XNA radian conversion!
            int newX = (int)Math.Floor(Convert.ToDouble(x) * Math.Cos(convertedAngleInRadians) - Convert.ToDouble(y) * -Math.Sin(convertedAngleInRadians));
            int newY = (int)Math.Floor(Convert.ToDouble(x) * -Math.Sin(convertedAngleInRadians) + Convert.ToDouble(y) * Math.Cos(convertedAngleInRadians));
            return new Point(newX, newY);
        }

        /// <summary>
        /// Rotate a point around an arbitrary point by some number of radians.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="originX"></param>
        /// <param name="originY"></param>
        /// <param name="angleInRadians"></param>
        /// <returns></returns>
        public static Point rotateAroundPoint(int x, int y, int originX, int originY, double angleInRadians)
        {
            // If you rotate point (px, py) around point (ox, oy) by angle theta you'll get:
            // p'x = cos(theta) * (px-ox) - sin(theta) * (py-oy) + ox
            // p'y = sin(theta) * (px-ox) + cos(theta) * (py-oy) + oy
            // So, to rotate around the creature's center, originX should be width/2 and originY should be height/2.
            // Note that this might get weird for an odd number of pixels in the image's row/columns. 

            // Convert the XNA radians to real world radians (see http://stackoverflow.com/questions/3527531/xna-question-about-rotation/3527725#3527725)
            double lostPrecision = 0.000000043711390063094768;
            double convertedAngleInRadians = (2 * Math.PI) - angleInRadians + lostPrecision;

            // NOTE: all sine calls here have been negated to deal with the XNA radian conversion!
            int newX = (int)Math.Ceiling(Math.Cos(convertedAngleInRadians) * Convert.ToDouble(x - originX) - (-Math.Sin(convertedAngleInRadians)) * Convert.ToDouble(y - originY) + originX);
            int newY = (int)Math.Ceiling(-Math.Sin(convertedAngleInRadians) * Convert.ToDouble(x - originX) + Math.Cos(convertedAngleInRadians) * Convert.ToDouble(y - originY) + originY);
            return new Point(newX, newY);
        }

        /// <summary>
        /// Rotates a point around an arbitary point, but in the opposite direction.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="originX"></param>
        /// <param name="originY"></param>
        /// <param name="angleInRadians"></param>
        /// <returns></returns>
        public static Point UnRotateAroundPoint(int x, int y, int originX, int originY, double angleInRadians)
        {
            angleInRadians = -angleInRadians;
            return rotateAroundPoint(x, y, originX, originY, angleInRadians);
        }

        /// <summary>
        /// Converts an angle measurement from degrees to radians.
        /// </summary>
        /// <param name="angleInDegrees"></param>
        /// <returns></returns>
        public static float convertToRadians(int angleInDegrees)
        {
            return (float)(angleInDegrees * (Math.PI / 180.0));
        }

        /// <summary>
        /// Converts and angle measurement from radians to degrees.
        /// </summary>
        /// <param name="angleInRadians"></param>
        /// <returns></returns>
        public static double convertToDegrees(double angleInRadians)
        {
            return (double)(angleInRadians * (180.0 / Math.PI));
        }

        /// <summary>
        /// Converts polar coordinates (r,theta) to (x,y). 
        /// R comes in as degrees, which must be converted to radians before processing.
        /// </summary>
        public static Point ConvertToCartesian(int r, int theta)
        {
            double radians = theta * (Math.PI / 180.0);
            Point XY = new Point();
            XY.X = Convert.ToInt32(Math.Floor(Math.Cos(radians) * r + (Chromaria.Simulator.pixelHeight / 2)));
            XY.Y = Convert.ToInt32(Math.Floor(Math.Sin(radians) * r + (Chromaria.Simulator.pixelHeight / 2)));
            return XY;
        }

        /// <summary>
        /// Converts polar coordinates (r,theta) to (x,y). 
        /// R comes in as radians.
        /// </summary>
        public static Point ConvertToCartesian(int r, float radians)
        {
            Point XY = new Point();
            XY.X = Convert.ToInt32((float)Math.Cos(radians) * r + (Chromaria.Simulator.pixelHeight / 2));
            XY.Y = Convert.ToInt32((float)Math.Sin(radians) * r + (Chromaria.Simulator.pixelHeight / 2));
            return XY;
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
        public static double Scale(double value, double min, double max, double scaledMin, double scaledMax)
        {
            return (((scaledMax - scaledMin) * (value - min)) / (max - min)) + scaledMin;
        }

        /// <summary>
        /// Scales a value which is expected to be in some range to some other range.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="scaledMin"></param>
        /// <param name="scaledMax"></param>
        /// <returns></returns>
        public static float Scale(float value, double min, double max, double scaledMin, double scaledMax)
        {
            return (float)((((scaledMax - scaledMin) * (value - min)) / (max - min)) + scaledMin);
        }
    }
}
