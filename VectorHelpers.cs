using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Chromaria
{
    /// <summary>
    /// This class contains some helper functions which make dealing with XNA's Vector2 class easier.
    /// Mostly, these functions let you deal with vectors as if they were points, but with the benefit
    /// of still being able to use the vector math functions.
    /// </summary>
    class VectorHelpers
    {
        /// <summary>
        /// Rounds down the X and Y components of a vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector2 Floor(Vector2 vector)
        {
            return new Vector2((float)Math.Floor(vector.X), (float)Math.Floor(vector.Y));
        }

        /// <summary>
        /// Rounds down the X and Y components of a vector and also accounts for the position
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="shiftx"></param>
        /// <param name="shifty"></param>
        /// <returns></returns>
        public static Vector2 Floor(Vector2 vector, int shiftx, int shifty)
        {
            return new Vector2((float)Math.Floor(vector.X - shiftx), (float)Math.Floor(vector.Y - shifty));
        }

        /// <summary>
        /// Converts a Vector2 object to an equivalent Point. Note there is a potential loss of precision. This function should probably be used in conjunction with Floor().
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Point asXNAPoint(Vector2 vector)
        {
            return new Point((int)vector.X, (int)vector.Y);
        }
    }
}
