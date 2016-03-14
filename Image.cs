using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chromaria
{
    /// <summary>
    /// This class is the base class for all graphics in the simulator.
    /// </summary>
    public class Image : DrawableGameComponent
    {
        // Image properties 
        public Texture2D Texture { get; set; }
        public Color[] TextureAsColorArray { get; set; }
        public Vector2 Position { get; set; } // upper-left coordinate, where the texture will be drawn
        public float Depth {get; set;}
        public string Type { get; set; }
        public int ID { get; set; }

        /// <summary>
        /// Image constructor
        /// </summary>
        /// <param name="x">Horizontal component of the image's Position vector.</param>
        /// <param name="y">Vertical component of the image's Position vector.</param>
        /// <param name="imageTexture">Optional parameter for the image's texture. If no texture is specified, it will be assigned a null value.</param>
        public Image(string type, float x, float y, Texture2D imageTexture, Simulator sim) : base(sim)
        {
            Position = new Vector2(x, y);
            Texture = imageTexture;
            TextureAsColorArray = new Color[Texture.Width * Texture.Height];
            Texture.GetData(TextureAsColorArray);
            Type = type;

            Depth = Chromaria.Simulator.nextDepth;
            Chromaria.Simulator.nextDepth -= 0.000001f;
            ID = Chromaria.Simulator.nextImageID;
            Chromaria.Simulator.nextImageID++;

            // Add the image to the simulator's components list, so its Draw() and Update() functions will be automatically called at each clock tick
            sim.Components.Add(this);
        }

        /// <summary>
        /// Loads the image's texture into the game engine's content manager.
        /// </summary>
        /// <param name="theContentManager"></param>
        /// <param name="spriteName"></param>
        public void loadContent(ContentManager theContentManager, string spriteName)
        {
            this.Texture = theContentManager.Load<Texture2D>(spriteName);
        }

        /// <summary>
        /// Gets a copy of the rotated texture as a pixel array.
        /// </summary>
        /// <returns></returns>
        public virtual Color[] getRotatedPixels()
        {
            // This version of the function will only be called if the image is not rotateable, in which case we just use the unrotated texture
            // This code would have been put in the StaticImage class, but you can't have a virtual non-void function.

            Color[] pixels = new Color[Texture.Height * Texture.Width];
            Texture.GetData(pixels);
            return pixels;
        }

        /// <summary>
        /// Counts the number of nontransparent (alpha > 0) pixels in the Image's Texture. 
        /// </summary>
        /// <returns>The number of nontransparent pixels in the Image's texture.</returns>
        public int countNontransparentPixels()
        {
            int numNonTransparentPixels = 0;

            // Loop through each pixel in the color array (initialized in the Image constructor) to 
            // find the pixels with nonzero alpha values
            foreach (Color pixel in TextureAsColorArray)
            {
                if (pixel.A != 0)
                    numNonTransparentPixels++;
            }

            return numNonTransparentPixels;
        }

        public virtual RotationPacket getRotationPacket() { return null; }
        public virtual RotationPacket getRotationPacketWithoutSensors(bool includeRotatedPixels) { return null; }
    }
}