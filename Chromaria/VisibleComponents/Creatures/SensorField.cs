using Chromaria.Modes;
using Chromaria.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chromaria.VisibleComponents.Creatures
{
    public class SensorField
    {
        #region instance variables
        #region RGB Arrays
        /// <summary>
        /// Perceived amount of red color in the sensor field.
        /// </summary>
        public double[,] R;

        /// <summary>
        /// Perceived amount of green color in the sensor field.
        /// </summary>
        public double[,] G;

        /// <summary>
        /// Perceived amount of blue color in the sensor field.
        /// </summary>
        public double[,] B;
        #endregion

        /// <summary>
        /// Defines the amount of detail which the sensor perceieves. A value of x means the sensor
        /// will perceieve every x pixels (ex. if the Resolution is 2, then the sensor will perceieve
        /// every other pixel). Pixel-perfect perception is achieved at the (minimum) value of 1.
        /// </summary>
        public int ResolutionX { get; set; }

        /// <summary>
        /// Defines the amount of detail which the sensor perceieves. A value of x means the sensor
        /// will perceieve every x pixels (ex. if the Resolution is 2, then the sensor will perceieve
        /// every other pixel). Pixel-perfect perception is achieved at the (minimum) value of 1.
        /// </summary>
        public int ResolutionY { get; set; }

        /// <summary>
        /// Defines the length of each sensor field segment, measured in pixels. The sensor field is 
        /// created by connecting NumSegments segments of length SegmentLength.
        /// </summary>
        public int SegmentLength { get; set; }

        public Point[,] SensorArray;

        public bool sensedSomething = false;
        #endregion

        /// <summary>
        /// Sensor constructor. Gives pixel-perfect resolution for a 10x10 square sensor field.
        /// </summary>
        /// <param name="x">X component of the sensor's position on the sprite.</param>
        /// <param name="y">Y component of the sensor's position on the sprite.</param>
        public SensorField(Simulator sim) : this(1, 1, 4, 10, sim) { }

        /// <summary>
        /// Sensor constructor. Gives pixel-perfect resolution for some manually-specified shape.
        /// </summary>
        /// <param name="x">X component of the sensor's position on the sprite.</param>
        /// <param name="y">Y component of the sensor's position on the sprite.</param>
        /// <param name="segNum">Defines the shape of the sensor field. Minimum is 3 (defining a triangular field), with arbitrarily 
        /// large values approximating a circular field.  </param>
        /// <param name="segLength">Defines the length of each sensor field segment. The sensor field is created by connecting NumSegments 
        /// segments of length SegmentLength.</param>
        public SensorField(int segNum, int segLength, Simulator sim) : this(1, 1, segNum, segLength, sim) { }

        /// <summary>
        /// Sensor constructor.
        /// </summary>
        /// <param name="positionOnSpriteX">X component of the sensor's position on the sprite.</param>
        /// <param name="positionOnSpriteY">Y component of the sensor's position on the sprite.</param>
        /// <param name="spriteResX">Defines the amount of detail which the sensor perceieves. A value of x means the sensor
        /// will perceieve every x pixels (ex. if the Resolution is 2, then the sensor will perceieve
        /// every other pixel). Pixel-perfect resolution is achieved at the (minimum) value of 1.</param>
        /// <param name="spriteResY">Defines the amount of detail which the sensor perceieves. A value of x means the sensor
        /// will perceieve every x pixels (ex. if the Resolution is 2, then the sensor will perceieve
        /// every other pixel). Pixel-perfect resolution is achieved at the (minimum) value of 1.</param>
        /// <param name="segNum">Defines the shape of the sensor field. Minimum is 3 (defining a triangular field), with arbitrarily 
        /// large values approximating a circular field.  </param>
        /// <param name="segLength">Defines the length of each sensor field segment. The sensor field is created by connecting NumSegments 
        /// segments of length SegmentLength.</param>
        public SensorField(int spriteResX, int spriteResY, int segNum, int segLength, Simulator sim)
        {
            // Note: the anchor point already accounts for maximization
            if (spriteResX > 0)
                ResolutionX = spriteResX;
            else
                ResolutionX = 1;
            if (spriteResY > 0)
                ResolutionY = spriteResY;
            else
                ResolutionY = 1;
            SegmentLength = segLength;

            // Initalize each of the color component arrays
            R = new double[segLength,segLength];
            G = new double[segLength,segLength];
            B = new double[segLength,segLength];
            for (int x=0; x<segLength; x++)
            {
                for (int y=0; y<segLength; y++)
                {
                    R[x,y] = 1;
                    G[x,y] = 1;
                    B[x,y] = 1;
                }
            }
            
            // Initialize the resources for the sensor visualization
            Simulator.FieldTexture = new Texture2D(sim.GraphicsDevice, SegmentLength * ResolutionX, SegmentLength * ResolutionY);
            Color[] sensorPixels = new Color[Simulator.FieldTexture.Width * Simulator.FieldTexture.Height];

            // Initialize the main functional component of the sensor field
            SensorArray = new Point[SegmentLength, SegmentLength];

            // Loop through each of the pixels in the texture
            for (int y = 0; y < SegmentLength * ResolutionY; y++)
            {
                for (int x = 0; x < SegmentLength * ResolutionX; x++)
                {
                    if (x % ResolutionX == 0  || y % ResolutionY == 0)
                        SensorArray[x / ResolutionX, y / ResolutionY] = new Point(x, y);
                    
                    // If the pixel coordinates fall outside the translated area, make them transparent. 
                    if (x % ResolutionX != 0  || y % ResolutionY != 0)
                        sensorPixels[x + (Simulator.FieldTexture.Width * y)] = Color.Transparent;

                    // Otherwise, give the pixel a solid color.
                    else
                    {
                        sensorPixels[x + (Simulator.FieldTexture.Width * y)] = Color.DarkViolet;
                        SensorArray[x / ResolutionX, y / ResolutionY] = new Point(x, y);
                    }
                }
            }

            // Copy the sensor pixels into a texture (which will only be used for onscreen visualization)
            Simulator.FieldTexture.SetData(sensorPixels);

            // Initialize the texture for onscreen visualization of sensor content, which may or may not actually be used
            Simulator.sensorContentsTexture = new Texture2D(sim.GraphicsDevice, SegmentLength * ResolutionX, SegmentLength * ResolutionY);
        }

        /// <summary>
        /// Returns the color contents of the area defined by the sensor field. This output will feed into the neural controller inputs.
        /// The neural controller has 3 input layers, which are arranged on the same horizontal level. The current version of this function
        /// assumes the sensor field is a square so that the sensor data can meaningfully be stored in a 2D array. A better version of this 
        /// function will be made later so that sensor fields with different shapes are supported.
        /// </summary>
        /// <param name="creaturePosition">The absolute position of the creature in the world, measured from the upper left corner.</param>
        public void Update(Creature creature, RotationPacket creaturePacket)
        {
            // (These two variables don't factor into the actual sensing, but they are necessary if we want to visualize the sensor contents.)
            Color[] sensorContentsPixels = new Color[Simulator.FieldTexture.Width * Simulator.FieldTexture.Height];
            for (int row = 0; row < Simulator.FieldTexture.Height; row++)
            {
                for (int col = 0; col < Simulator.FieldTexture.Width; col++)
                    sensorContentsPixels[col + (row * Simulator.FieldTexture.Width)] = Color.Transparent;
            }

            // Reset the sensors
            resetArrays();

            // Find the new bounding box coordinates using the new creatureCenter
            List<Image> intersectingImages;
            if (Simulator.blindCreatures)
            {
                intersectingImages = new List<Image>();
                intersectingImages.Add(Simulator.initialBackground);
            }
            else if (Simulator.everyoneCanPlant)
            {
                intersectingImages = new List<Image>();
                intersectingImages.Add(Simulator.backgroundImage);
            }
            else
                intersectingImages = updateIntersectingImages(creature, creaturePacket);

            // Grab the rotation packets for each of the images in intersectingImages
            List<RotationPacket> rotationPackets = new List<RotationPacket>();
            for (int i = 0; i < intersectingImages.Count; i++)
                rotationPackets.Add(intersectingImages[i].getRotationPacketWithoutSensors(true));
            

            // Find the global coordinates of the creature's position and the sensor field's NW coordinate,
            // along with the local coordinates of the creature's center
            Point creaturePosition = VectorHelpers.asXNAPoint(creature.Position);
            Point sensorNWCoord = new Point(creaturePosition.X + creature.SensorFieldAnchorPoint.X - (SegmentLength * ResolutionX) / 2, creaturePosition.Y + creature.SensorFieldAnchorPoint.Y - (SegmentLength * ResolutionY) / 2);
            Vector2 creatureCenter = new Vector2(creature.Texture.Width / 2, creature.Texture.Height / 2);

            // Iterate through the sensor field texel positions and query for non-transparent pixels underneath the field
            Color worldTexel;
            int indexIntoArray;
            for (int x = 0; x < SegmentLength; x++)
            {
                for (int y = 0; y < SegmentLength; y++)
                {
                    // Get the global position that corresponds to the internal (x,y) coordinates
                    Point globalSensorCoord = MathHelpers.rotateAroundPoint(sensorNWCoord.X + SensorArray[x, y].X, sensorNWCoord.Y + SensorArray[x, y].Y, Convert.ToInt32(creature.Position.X + creatureCenter.X), Convert.ToInt32(creature.Position.Y + creatureCenter.Y), creature.XNAHeading);

                    // Loop through each image, starting with the one that's closest to (yet still underneath) the sensor field
                    for (int i = 0; i < intersectingImages.Count; i++)
                    {
                        // Check to make sure the image actually has a pixel located at the (global) x,y we're checking
                        Point imageNW = rotationPackets[i].NWCoord;
                        Point imageSE = rotationPackets[i].SECoord;
                        if(globalSensorCoord.X >= imageNW.X && globalSensorCoord.X < imageSE.X && globalSensorCoord.Y >= imageNW.Y && globalSensorCoord.Y < imageSE.Y)
                        {
                            indexIntoArray = (globalSensorCoord.X - imageNW.X + ((globalSensorCoord.Y - imageNW.Y) * rotationPackets[i].NewWidth));
                            worldTexel = rotationPackets[i].RotatedPixels[indexIntoArray];
                            if (worldTexel.A != 0)
                            {
                                // If we get here, the sensor field is working as it should!
                                sensedSomething = true;

                                // Store the sensed RGB values in an ANN-friendly format
                                R[x, y] = MathHelpers.Scale(worldTexel.R, 0, 255, -1, 1);
                                G[x, y] = MathHelpers.Scale(worldTexel.G, 0, 255, -1, 1);
                                B[x, y] = MathHelpers.Scale(worldTexel.B, 0, 255, -1, 1);

                                // If we're visualizing the sensor field contents, we need to update the texture-to-be-drawn also
                                if (Simulator.drawSensorField && (!Simulator.depthTest || creature.ID==Simulator.manuallyControlledCreatureID))
                                    sensorContentsPixels[x+ ((y * ResolutionX) * SegmentLength)] = worldTexel;
                                
                                // Then stop looping through the images and move on to the next texel location
                                break;
                            }
                        }
                    }
                }
            }

            // If we want to visualize the sensor field, we have to do it before we dispose of the temporary texture
            if (Simulator.drawSensorField)
                Simulator.sensorContentsTexture.SetData(sensorContentsPixels);

            // Raise an exception if the sensors are broken
            if (!sensedSomething)
                throw new Exception("Problem: Sensor.Sense() looped through the pixels in rotatedSensorField without finding any pixels with a nonzero alpha value.");
        
            // Reset the exception flag
            sensedSomething = false;
        }

        /// <summary>
        /// Resets the state of the sensor arrays back to the default state of perceiving no color.
        /// </summary>
        private void resetArrays()
        {
            for (int x = 0; x < SegmentLength; x++)
            {
                for (int y = 0; y < SegmentLength; y++)
                {
                    R[x, y] = 1;
                    G[x, y] = 1;
                    B[x, y] = 1;
                }
            }
        }

        /// <summary>
        /// During each call to Sense(), this function is called to update the list of images that intersect the sensor field.
        /// p1 is the NW sensor field corner, and p2 is the SE sensor field corner. 
        /// </summary>
        private List<Image> updateIntersectingImages(Image creature, RotationPacket creaturePacket)
        {
            // Figure out which regions the sensor field could intersect
            List<int> indicesOfPossibleNeighbors = new List<int>();
            indicesOfPossibleNeighbors.AddRange(Simulator.regions[creaturePacket.NWCoord.Y / Simulator.regionHeight, creaturePacket.NWCoord.X / Simulator.regionWidth]);
            indicesOfPossibleNeighbors.AddRange(Simulator.regions[creaturePacket.NWCoord.Y / Simulator.regionHeight, creaturePacket.SECoord.X / Simulator.regionWidth]);
            indicesOfPossibleNeighbors.AddRange(Simulator.regions[creaturePacket.SECoord.Y / Simulator.regionHeight, creaturePacket.NWCoord.X / Simulator.regionWidth]);
            indicesOfPossibleNeighbors.AddRange(Simulator.regions[creaturePacket.SECoord.Y / Simulator.regionHeight, creaturePacket.SECoord.X / Simulator.regionWidth]);

            // Grab the images corresponding to the indices for the possible neighbors
            float imageDepth;
            Image candidateImage;
            SortedDictionary<float, Image> neighboringRegions = new SortedDictionary<float, Image>();
            foreach (int index in indicesOfPossibleNeighbors)
            {
                candidateImage = (Image)creature.Game.Components[index];
                imageDepth = candidateImage.Depth;
                if (!neighboringRegions.ContainsKey(imageDepth) && candidateImage != creature &&  imageDepth > creature.Depth)
                    neighboringRegions.Add(imageDepth, candidateImage);
            }
                

            // Go through those regions and find the images that intersect the sensor field
            // (Also, we only want images that are underneath the sensor field)
            int x1, x2, y1, y2;
            SortedDictionary<float, Image> intersectingImages = new SortedDictionary<float, Image>();
            foreach (RotateableImage image in neighboringRegions.Values.OfType<RotateableImage>())
            {
                RotationPacket imagePacket = image.getRotationPacket();
                int halfChangeInWidth = (imagePacket.NewWidth - image.Texture.Width) / 2;
                int halfChangeInHeight = (imagePacket.NewHeight - image.Texture.Height) / 2;

                // (x1, y1) is the NE corner of the intersection space
                x1 = Math.Max(creaturePacket.NWCoord.X, imagePacket.NWCoord.X);
                y1 = Math.Max(creaturePacket.NWCoord.Y, imagePacket.NWCoord.Y);

                // (x2, y2) is the SW corner of the intersection space
                x2 = Math.Min(creaturePacket.SECoord.X, imagePacket.SECoord.X);
                y2 = Math.Min(creaturePacket.SECoord.Y, imagePacket.SECoord.X);

                // If there is a nonzero intersection space and the image under consideration isn't already in the image list, 
                // add it to the image list.
                if (x1 < x2 && y1 < y2)
                    intersectingImages.Add(image.Depth, image);
            }

            // Take care of the non-mobile images (basically just region backgroudnds)
            foreach (Image image in neighboringRegions.Values.OfType<StaticImage>())
            {
                // (x1, y1) is the NE corner of the intersection space
                x1 = Math.Max(creaturePacket.NWCoord.X, Convert.ToInt32(Math.Floor(image.Position.X)));
                y1 = Math.Max(creaturePacket.NWCoord.Y, Convert.ToInt32(Math.Floor(image.Position.Y)));

                // (x2, y2) is the SW corner of the intersection space 
                x2 = Math.Min(creaturePacket.SECoord.X, Convert.ToInt32(Math.Floor(image.Position.X)) + image.Texture.Width);
                y2 = Math.Min(creaturePacket.SECoord.Y, Convert.ToInt32(Math.Floor(image.Position.Y)) + image.Texture.Height);

                // If there is a nonzero intersection space and the image under consideration isn't already in the image list, 
                // add it to the image list.
                if (x1 < x2 && y1 < y2)
                {
                    intersectingImages.Add(image.Depth, image);
                }
            }

            // Convert the dictionary to a list and return it
            return intersectingImages.Values.ToList();
        }
    }
}
