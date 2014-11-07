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
    /// This class is used for game components that do not rotate, such as the world's background images.
    /// </summary>
    public class StaticImage : Image
    {
        Vector2 center;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="imageTexture"></param>
        /// <param name="sim"></param>
        public StaticImage(string type, float x, float y, Texture2D imageTexture, Simulator sim)
            : base(type, x, y, imageTexture, sim) 
        {
            // This class doesn't have much of a constructor and passes everything up to the Image constructor. 
            // The point of this class is really to add some special overrides of Image class functions.
            center = new Vector2(Texture.Width / 2, Texture.Height / 2);
        }

        /// <summary>
        /// Usually this function does nothing. However, in the main loop, it is called whenever a creature plants to copy its texture into the background for memory conservation.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (Simulator.updateBackground)
            {
                // Copy the background image to a new array so it can be modified
                Color[] newColorArray = TextureAsColorArray;

                // Loop through the just-planted creature and copy its non-transparent pixels into the background's color array
                Color pixel;
                int initialIndex = Simulator.planterRotationPacket.NWCoord.X + (Simulator.planterRotationPacket.NWCoord.Y * Simulator.regionWidth);
                for (int x = 0; x < Simulator.planterRotationPacket.NewWidth; x++)
                {
                    for (int y = 0; y < Simulator.planterRotationPacket.NewHeight; y++)
                    {
                        pixel = Simulator.planterRotationPacket.RotatedPixels[x + (y * Simulator.planterRotationPacket.NewWidth)];
                        if (pixel.A != 0)
                            newColorArray[initialIndex + (x + (y * Simulator.regionWidth))] = pixel;
                    }
                }

                // Set the background's texture and color array to the modified pixel array
                TextureAsColorArray = newColorArray;
                Texture.SetData(TextureAsColorArray);

                // Reset the update flag
                Simulator.updateBackground = false;
                Simulator.planterRotationPacket = null;
            }
        }

        /// <summary>
        /// Draws the texture without any rotation.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            Chromaria.Simulator.spriteBatch.Draw(Texture, Position + center, null, Color.White, 0, center, 1.0f, SpriteEffects.None, Depth);
        }

        /// <summary>
        /// Returns the regular old width, height, etc. because static images do not rotate. owever, leaving this version of the function out would require some hacky coding over in the SensorField.Update() function.
        /// </summary>
        /// <returns></returns>
        public override RotationPacket getRotationPacket()
        {
            Point position = VectorHelpers.asXNAPoint(Position);
            return new RotationPacket(Texture.Width, Texture.Height, position, new Point(position.X + Texture.Width, position.Y + Texture.Height));
        }

        /// <summary>
        /// Returns the regular old width, height, etc. because static images do not rotate. However, leaving this version of the function out would require some hacky coding over in the SensorField.Update() function.
        /// </summary>
        /// <param name="includeRotatedPixels"></param>
        /// <returns></returns>
        public override RotationPacket getRotationPacketWithoutSensors(bool includeRotatedPixels = false)
        {
            if (includeRotatedPixels)
                return new RotationPacket(Texture.Width, Texture.Height, VectorHelpers.asXNAPoint(Position), VectorHelpers.asXNAPoint(Position + new Vector2(Texture.Width, Texture.Height)), TextureAsColorArray);
            else
                return new RotationPacket(Texture.Width, Texture.Height, VectorHelpers.asXNAPoint(Position), VectorHelpers.asXNAPoint(Position + new Vector2(Texture.Width, Texture.Height)));
        }
    }
}
