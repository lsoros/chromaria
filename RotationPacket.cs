using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Chromaria
{
    /// <summary>
    /// Auxilliary class that bundles a bunch of information about a rotated texture's dimensions and position together.
    /// </summary>
    public class RotationPacket
    {
        public int NewWidth { get; set; }
        public int NewHeight { get; set; }
        public int MaxDimension { get; set; }
        public Point NWCoord { get; set; }
        public Point SECoord { get; set; }
        public Color[] RotatedPixels { get; set; }

        public RotationPacket() { }

        public RotationPacket(int width, int height, Point NWcoord, Point SEcoord, Color[] pixels = null)
        {
            NewWidth = width;
            NewHeight = height;
            MaxDimension = Math.Max(NewWidth, NewHeight);
            NWCoord = NWcoord;
            SECoord = SEcoord;
            RotatedPixels = pixels;
        }
    }
}
