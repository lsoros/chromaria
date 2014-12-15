using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Chromaria.SharpNeatLib.CPPNs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chromaria
{
    /// <summary>
    /// This class is the base class for any image that can rotate, namely Chromarians.
    /// </summary>
    public class RotateableImage : Image
    {
        #region instance variables

        public double Heading { get; set; } // In radians. Pi/2 -> The image is unrotated. The axis of rotation points east, consistent with non-XNA rotation. Use for rotation functions.
        public float XNAHeading { get; set; } // In radians. 0 -> The image is unrotated. The axis of rotation points north. Use for drawing.
        protected bool frozen { get; set; }
        protected Vector2 center { get; set; }

        #endregion

        /// <summary>
        /// Deafult constructor.
        /// </summary>
        /// <param name="bodyTexture">2D texture containing the image of the creature's body.</param>
        /// <param name="numSensors">Optional parameter for the number of discrete sensors the creature will have. If no value is specified, the creature will have 1 sensor.</param>
        /// <param name="x">X-coordinate for the creature's position in the world.</param>
        /// <param name="y">Y-coordinate for the creature's position in the world.</param>
        public RotateableImage(Texture2D bodyTexture, float x, float y, float headingInRadians, Simulator sim)
            : base("Creature", x, y, bodyTexture, sim)
        {
            // Note: depth is required because the creatures cannot be on the deepest layer.
            // The heading that is passed into the constructor should assume that 0 is north and pi/2 is east.
            XNAHeading = headingInRadians;
            Heading = XNAHeading + (Math.PI / 2.0);
            center = new Vector2(Texture.Width / 2, Texture.Height / 2);
        }

        /// <summary>
        /// Draws the sprite's texture to the screen at its given position.
        /// </summary>
        /// <param name="theSpriteBatch">The game engine's SpriteBatch, which handles the actual drawing.</param>
        public override void Draw(GameTime gameTime)
        {
            if (!frozen)
                Chromaria.Simulator.spriteBatch.Draw(Texture, Position + center, null, Color.White, XNAHeading, center, 1.0f, SpriteEffects.None, Depth);
        }

        /// <summary>
        /// Calculates the larger width and height required when the original texture is rotated.
        /// </summary>
        /// <returns>A vector <newWidth, newHeight>.</returns>
        public override RotationPacket getRotationPacket()
        {
            // Find the borders (unmaximized) of the creature's texture
            Vector2 NWcoord = new Vector2(this.Position.X, this.Position.Y);
            Vector2 NEcoord = new Vector2(this.Position.X + Texture.Width, this.Position.Y);
            Vector2 SWcoord = new Vector2(this.Position.X, this.Position.Y + Texture.Height);
            Vector2 SEcoord = new Vector2(this.Position.X + Texture.Width, this.Position.Y + Texture.Height);

            // Rotate the points to find the actual global coordinates of the creature's corners
            Vector2 center = new Vector2(Texture.Width / 2, Texture.Height / 2);
            Point rotatedNWcoord = MathHelpers.rotateAroundPoint(Convert.ToInt32(NWcoord.X), Convert.ToInt32(NWcoord.Y), Convert.ToInt32(NWcoord.X + center.X), Convert.ToInt32(NWcoord.Y + center.Y), Heading);
            Point rotatedNEcoord = MathHelpers.rotateAroundPoint(Convert.ToInt32(NEcoord.X), Convert.ToInt32(NEcoord.Y), Convert.ToInt32(NWcoord.X + center.X), Convert.ToInt32(NWcoord.Y + center.Y), Heading);
            Point rotatedSWcoord = MathHelpers.rotateAroundPoint(Convert.ToInt32(SWcoord.X), Convert.ToInt32(SWcoord.Y), Convert.ToInt32(NWcoord.X + center.X), Convert.ToInt32(NWcoord.Y + center.Y), Heading);
            Point rotatedSEcoord = MathHelpers.rotateAroundPoint(Convert.ToInt32(SEcoord.X), Convert.ToInt32(SEcoord.Y), Convert.ToInt32(NWcoord.X + center.X), Convert.ToInt32(NWcoord.Y + center.Y), Heading);

            // Use the rotated corners to find the min and max values along the x and y dimensions
            int minX = Math.Min(Math.Min(rotatedNWcoord.X, rotatedNEcoord.X), Math.Min(rotatedSWcoord.X, rotatedSEcoord.X));
            int minY = Math.Min(Math.Min(rotatedNWcoord.Y, rotatedNEcoord.Y), Math.Min(rotatedSWcoord.Y, rotatedSEcoord.Y));
            int maxX = Math.Max(Math.Max(rotatedNWcoord.X, rotatedNEcoord.X), Math.Max(rotatedSWcoord.X, rotatedSEcoord.X));
            int maxY = Math.Max(Math.Max(rotatedNWcoord.Y, rotatedNEcoord.Y), Math.Max(rotatedSWcoord.Y, rotatedSEcoord.Y));

            // Return the new width, height, NW coord, and SW coord of the bounding box now that the creature (including its sensor field) has been rotated
            return new RotationPacket(maxX - minX, maxY - minY, new Point(minX, minY), new Point(maxX, maxY));
        }

        /// <summary>
        /// Calculates the larger width and height required when the original texture is rotated.
        /// </summary>
        /// <returns>A vector <newWidth, newHeight>.</returns>
        public override RotationPacket getRotationPacketWithoutSensors(bool includeRotatedPixels = false)
        {
            // Find the borders (unmaximized) of the creature's texture
            Vector2 NWcoord = new Vector2(this.Position.X, this.Position.Y);
            Vector2 NEcoord = new Vector2(this.Position.X + Texture.Width, this.Position.Y);
            Vector2 SWcoord = new Vector2(this.Position.X, this.Position.Y + Texture.Height);
            Vector2 SEcoord = new Vector2(this.Position.X + Texture.Width, this.Position.Y + Texture.Height);

            // Rotate the points to find the actual global coordinates of the creature's corners
            Vector2 center = new Vector2(Texture.Width / 2, Texture.Height / 2);
            Point rotatedNWcoord = MathHelpers.rotateAroundPoint(Convert.ToInt32(NWcoord.X), Convert.ToInt32(NWcoord.Y), Convert.ToInt32(NWcoord.X + center.X), Convert.ToInt32(NWcoord.Y + center.Y), Heading);
            Point rotatedNEcoord = MathHelpers.rotateAroundPoint(Convert.ToInt32(NEcoord.X), Convert.ToInt32(NEcoord.Y), Convert.ToInt32(NWcoord.X + center.X), Convert.ToInt32(NWcoord.Y + center.Y), Heading);
            Point rotatedSWcoord = MathHelpers.rotateAroundPoint(Convert.ToInt32(SWcoord.X), Convert.ToInt32(SWcoord.Y), Convert.ToInt32(NWcoord.X + center.X), Convert.ToInt32(NWcoord.Y + center.Y), Heading);
            Point rotatedSEcoord = MathHelpers.rotateAroundPoint(Convert.ToInt32(SEcoord.X), Convert.ToInt32(SEcoord.Y), Convert.ToInt32(NWcoord.X + center.X), Convert.ToInt32(NWcoord.Y + center.Y), Heading);

            // Use the rotated corners to find the min and max values along the x and y dimensions
            int minX = Math.Min(Math.Min(rotatedNWcoord.X, rotatedNEcoord.X), Math.Min(rotatedSWcoord.X, rotatedSEcoord.X));
            int minY = Math.Min(Math.Min(rotatedNWcoord.Y, rotatedNEcoord.Y), Math.Min(rotatedSWcoord.Y, rotatedSEcoord.Y));
            int maxX = Math.Max(Math.Max(rotatedNWcoord.X, rotatedNEcoord.X), Math.Max(rotatedSWcoord.X, rotatedSEcoord.X));
            int maxY = Math.Max(Math.Max(rotatedNWcoord.Y, rotatedNEcoord.Y), Math.Max(rotatedSWcoord.Y, rotatedSEcoord.Y));

            // Return the new width, height, NW coord, and SW coord of the bounding box now that the creature (including its sensor field) has been rotated
            if (includeRotatedPixels)
            {
                // Calculate the size of the larger texture that we'll need
                int newWidth = maxX - minX;
                int newHeight = maxY - minY;
                int halfChangeInWidth = Convert.ToInt32(Math.Floor((newWidth - Texture.Width) / 2.0f));
                int halfChangeInHeight = Convert.ToInt32(Math.Floor((newHeight - Texture.Height) / 2.0f));
                int maxDimension = Math.Max(newWidth, newHeight);

                // Create a new array of pixels, initialized to transparent
                Color[] rotatedPixels = new Color[maxDimension * maxDimension];
                for (int x = 0; x < maxDimension; x++)
                {
                    for (int y = 0; y < maxDimension; y++)
                        rotatedPixels[x + (y * x)] = Color.Transparent;
                }

                // Loop through the new texture and find the corresponding pixels in the original texture
                Point unrotatedPoint;
                Point textureCenter = new Point(Texture.Width / 2, Texture.Height / 2);
                for (int rotatedX = 0; rotatedX < Texture.Width; rotatedX++)
                {
                    for (int rotatedY = 0; rotatedY < Texture.Height; rotatedY++)
                    {
                        // Find the (local) coordinates of the pixel after rotation based on the original texture's size
                        unrotatedPoint = MathHelpers.UnRotateAroundPoint(rotatedX, rotatedY, textureCenter.X, textureCenter.Y, XNAHeading);

                        // Convert the rotatedpoint into the corresponding index into the rotated pixel array (accounting for the size difference)
                        if (unrotatedPoint.X > -1 && unrotatedPoint.X < Texture.Width && unrotatedPoint.Y > -1 && unrotatedPoint.Y < Texture.Height)
                        {
                            rotatedPixels[(rotatedX + halfChangeInWidth) + (newWidth * (rotatedY + halfChangeInHeight))] = TextureAsColorArray[unrotatedPoint.X + (Texture.Width * unrotatedPoint.Y)];
                        }
                    }
                }

                // Return the rotated pixel array
                return new RotationPacket(newWidth, newHeight, new Point(minX, minY), new Point(maxX, maxY), rotatedPixels);
            }
            else
                return new RotationPacket(maxX - minX, maxY - minY, new Point(minX, minY), new Point(maxX, maxY));
        }
    }
}