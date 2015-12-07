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
    /// Base class for all Chromarians.
    /// </summary>
    public class Creature : RotateableImage
    {
        #region instance variables

        protected Vector2 direction { get; set; }
        protected Vector2 speed { get; set; }
        public SensorField Sensor { get; set; }
        public Point SensorFieldAnchorPoint { get; set; }
        public Chromaria.Simulator.State currentState { get; set; }
        protected bool drawSensorField { get; set; }
        protected bool trackPlanting { get; set; }
        protected int numSensorsPerDim { get; set; }
        protected RotationPacket packetBeforeMoving;
        protected RotationPacket packetAfterMoving;

        protected float distCenterToBorder { get; set; }
        protected int maxHeightAndWidth { get; set; }

        protected Vector2 bodyCenter { get; set; }

        double threshold = 0.5; // for planting

        Point rotatedCenter, rotatedCreatureNW, rotatedCreatureNE, rotatedCreatureSW, rotatedCreatureSE, rotatedSensorNW, rotatedSensorNE, rotatedSensorSW, rotatedSensorSE;
        Vector2 rotatedCenterAsVector, creatureCenter, oldPosition, newPosition, boundsNW, boundsSE, creatureNW, creatureNE, creatureSE, creatureSW, center, sensorNW, sensorNE, sensorSW, sensorSE,
            rotationOrigin, fieldNWGlobalCoords;
        RotationPacket rp;
        int distTopToNontransparent, distLeftToNontransparent, distBottomToNontransparent, distRightToNontransparent, maxWidthStartCol, maxWidthEndCol, maxHeightStartRow, maxHeightEndRow,
            maxWidth, rowWidth, startCol, endCol, maxHeight, colHeight, colWithMaxHeight, startRow, endRow, spriteResX, spriteResY, deltaX, deltaY, minCreatureX,
            maxCreatureX, minCreatureY, maxCreatureY, minSensorX, minSensorY, maxSensorX, maxSensorY, minX, minY, maxX, maxY;

        // Color counts and ratios for the planting eligibility function
        int blackCount, whiteCount, redCount, greenCount, blueCount, yellowCount, magentaCount, cyanCount;
        double blackRatioSensor, whiteRatioSensor, redRatioSensor, greenRatioSensor, blueRatioSensor, yellowRatioSensor, magentaRatioSensor, cyanRatioSensor, numSensors, summedDifferences;
        protected float blackRatio, whiteRatio, redRatio, greenRatio, blueRatio, yellowRatio, magentaRatio, cyanRatio;
        
        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="bodyTexture"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="headingInRadians"></param>
        /// <param name="sim"></param>
        /// <param name="drawSensorField_in"></param>
        /// <param name="trackPlanting_in"></param>
        /// <param name="numSensors"></param>
        public Creature(Texture2D bodyTexture, float x, float y, float headingInRadians, Simulator sim, bool drawSensorField_in, bool trackPlanting_in, int numSensors)
            : base(bodyTexture, x, y, headingInRadians, sim)
        {
            // Set the instance variables based on parameters
            currentState = Chromaria.Simulator.State.Moving;
            speed = Vector2.Zero;
            direction = Vector2.Zero;
            drawSensorField = drawSensorField_in;
            trackPlanting = trackPlanting_in;
            numSensorsPerDim = numSensors;

            // Generate some additional components based on the parameters
            generateSensorField(sim);
            getMaxBoundingBoxSize();
            getBodyColorCounts();
        }

        /// <summary>
        /// Calculate the max possible texture size (bounding box size will change based on angle of rotation).
        /// </summary>
        protected void getMaxBoundingBoxSize()
        {
            float minX = Position.X;
            float minY = Position.Y;

            // Rotate the texture at every possible angle to find the max necessary dimensions
            for (float theta = (float)-Math.PI; theta < Math.PI; theta += (1.0f / 360.0f))
            {
                rp = getRotationPacketAtAngle(theta);
                if (rp.NWCoord.X < minX)
                    minX = rp.NWCoord.X;
                if (rp.NWCoord.Y < minY)
                    minY = rp.NWCoord.Y;
            }

            // Calculate the distance from the creature's center to its border (used for calculating sensor placement and dimensions)
            creatureCenter = new Vector2(Position.X + (Texture.Width / 2), Position.Y + (Texture.Height / 2));
            distCenterToBorder = (float)Math.Max(Math.Ceiling(creatureCenter.X - minX), Math.Ceiling(creatureCenter.Y - minY)) + 1.0f;
        }

        /// <summary>
        /// Generates a sensor field for the creaure based on its morphology texture's dimensions.
        /// </summary>
        /// <param name="sim"></param>
        protected void generateSensorField(Simulator sim)
        {
            // In the implausible case that we would have no real nontransparent values along the x and y axes,
            // the anchor point for the sensor would be at the creatureCenter of the creature.
            distTopToNontransparent = 0;
            distLeftToNontransparent = 0;
            distBottomToNontransparent = 0;
            distRightToNontransparent = 0;

            maxWidthStartCol = 0;
            maxWidthEndCol = 0;
            maxHeightStartRow = 0;
            maxHeightEndRow = 0;

            // Find the appropriate sensor position.
            // Currently, a creature can only have one sensor.
            // The x-coordinate of the sensor is roughly centered.
            // The y-coordinate makes it such that the sensor field is half underneath the front of the
            // creature and half in front of it.

            // Calculate the maxWidth of the creature's texture
            maxWidth = 0;
            rowWidth = 0;
            startCol = 0;
            endCol = 0;
            for (int row = 0; row < Texture.Height; row++)
            {
                // Calculate the distance from the left edge to the leftmost transparent pixel
                for (int col = 0; col < Texture.Width; col++)
                {
                    if (TextureAsColorArray[col + (row * Texture.Width)].A != 0)
                    {

                        distLeftToNontransparent = col;
                        startCol = col;
                        break;
                    }
                }

                // Calculate the distance from the right edge to the rightmost transparent pixel
                for (int col = Texture.Width - 1; col > 0; col--)
                {
                    if (TextureAsColorArray[col + (row * Texture.Width)].A != 0)
                    {
                        distRightToNontransparent = col;
                        endCol = col;
                        break;
                    }
                }

                rowWidth = distRightToNontransparent - distLeftToNontransparent;
                if (rowWidth > maxWidth)
                {
                    maxWidth = rowWidth;
                    maxWidthStartCol = startCol;
                    maxWidthEndCol = endCol;
                }
            }

            // Calculate the maxWHeight of the creature's texture
            maxHeight = 0;
            colHeight = 0;
            colWithMaxHeight = 0;
            startRow = 0;
            endRow = 0;
            for (int col = 0; col < Texture.Width; col++)
            {
                // Calculate the distance from the leftmost 
                for (int row = 0; row < Texture.Height; row++)
                {
                    if (TextureAsColorArray[col + (row * Texture.Width)].A != 0)
                    {
                        distTopToNontransparent = row;
                        startRow = row;
                        break;
                    }
                }

                for (int row = Texture.Height - 1; row > 0; row--)
                {
                    if (TextureAsColorArray[col + (row * Texture.Width)].A != 0)
                    {
                        distBottomToNontransparent = row;
                        endRow = row;
                        break;
                    }
                }

                colHeight = distBottomToNontransparent - distTopToNontransparent;
                if (colHeight > maxHeight)
                {
                    maxHeight = colHeight;
                    colWithMaxHeight = col;
                    maxHeightStartRow = startRow;
                    maxHeightEndRow = endRow;
                }
            }

            maxHeightAndWidth = Math.Max(maxHeight, maxWidth);

            // Translate maxWidth and maxHeight into sensor field resolution
            spriteResX = maxWidth / numSensorsPerDim;
            spriteResY = maxHeight / numSensorsPerDim;

            // Create the sensor given the x and y components we just found
            Sensor = new SensorField(spriteResX, spriteResY, 4, numSensorsPerDim, sim);

            // Look for the first nontransparent pixel
            int rowOfFirstPixel = 0;
            for (int row = 0; row < Texture.Height; row++)
            {
                    if (TextureAsColorArray[colWithMaxHeight + (row * Texture.Width)].A != 0)
                    {
                        rowOfFirstPixel = row;
                        goto SetAnchorPoint;
                    }
            }

            // Calculate the anchor point for the sensor field
            SetAnchorPoint:
            SensorFieldAnchorPoint = new Point(colWithMaxHeight, rowOfFirstPixel);

            bodyCenter = new Vector2((maxWidthEndCol - maxWidthStartCol)/2 + maxWidthStartCol, (maxHeightEndRow - maxHeightStartRow)/2 + maxHeightStartRow);
        }

        /// <summary>
        /// Calculates the RGB counts of pixels within the creature's body.
        /// </summary>
        private void getBodyColorCounts()
        {
            // Loop through each nontransparent pixel in the body texture
            float numNonTransparentPixels = 0.0f;
            double threshold = Math.Floor(255.0 / 2.0);
            float blackCount = 0.0f, whiteCount = 0.0f, redCount = 0.0f, greenCount = 0.0f, blueCount = 0.0f, yellowCount = 0.0f, magentaCount = 0.0f, cyanCount = 0.0f;
            foreach (Color pixel in TextureAsColorArray)
            {
                if (pixel.A != 0)
                {
                    numNonTransparentPixels++;

                    // Black bin
                    if (pixel.R < threshold && pixel.G < threshold && pixel.B < threshold)
                        blackCount++;
                    // White bin
                    else if (pixel.R > threshold && pixel.G > threshold && pixel.B > threshold)
                        whiteCount++;
                    // Red bin
                    else if (pixel.R > threshold && pixel.G < threshold && pixel.B < threshold)
                        redCount++;
                    // Green bin
                    else if (pixel.R < threshold && pixel.G > threshold && pixel.B < threshold)
                        greenCount++;
                    // Blue bin
                    else if (pixel.R < threshold && pixel.G < threshold && pixel.B > threshold)
                        blueCount++;
                    // Yellow bin
                    else if (pixel.R > threshold && pixel.G > threshold && pixel.B < threshold)
                        yellowCount++;
                    // Magenta bin
                    else if (pixel.R > threshold && pixel.G < threshold && pixel.B > threshold)
                        magentaCount++;
                    // Cyan count
                    else
                        cyanCount++;
                }
            }

            // Get the ratios for all of the bins
            blackRatio = blackCount / numNonTransparentPixels;
            whiteRatio = whiteCount / numNonTransparentPixels;
            redRatio = redCount / numNonTransparentPixels;
            greenRatio = greenCount / numNonTransparentPixels;
            blueRatio = blueCount / numNonTransparentPixels;
            yellowRatio = yellowCount / numNonTransparentPixels;
            magentaRatio = magentaCount / numNonTransparentPixels;
            cyanRatio = cyanCount / numNonTransparentPixels;
        }

        /// <summary>
        /// Draws the sprite's texture to the screen at its given position.
        /// </summary>
        /// <param name="theSpriteBatch">The game engine's SpriteBatch, which handles the actual drawing.</param>
        public override void Draw(GameTime gameTime)
        {
            // Draw the creature
            base.Draw(gameTime);

            // Draw sensor fields (used only for debugging)
            if (drawSensorField && (!Chromaria.Simulator.depthTest || ID==Chromaria.Simulator.manuallyControlledCreatureID))
            {
                // Draw the sensor field
                fieldNWGlobalCoords = new Vector2(Position.X + SensorFieldAnchorPoint.X - (Chromaria.Simulator.FieldTexture.Width / 2), Position.Y + SensorFieldAnchorPoint.Y - (Chromaria.Simulator.FieldTexture.Height / 2));
                rotationOrigin = new Vector2(Chromaria.Simulator.FieldTexture.Width / 2, Chromaria.Simulator.FieldTexture.Height * 1.5f);
                Chromaria.Simulator.spriteBatch.Draw(Chromaria.Simulator.FieldTexture, fieldNWGlobalCoords + rotationOrigin, null, Color.White, XNAHeading, rotationOrigin, 1.0f, SpriteEffects.None, Depth);
                
                // Draw the sensor field's contents
                Chromaria.Simulator.spriteBatch.Draw(Chromaria.Simulator.sensorContentsTexture, new Vector2(100, 300), null, Color.White, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.0f);
            }

            // If desired, draw the red texture overtop of the creature's texture if it is planting
            if (trackPlanting && currentState == Chromaria.Simulator.State.Planting)
            {
                center = new Vector2(Texture.Width / 2, Texture.Height / 2);
                Chromaria.Simulator.spriteBatch.Draw(Chromaria.Simulator.redTexture, Position + center, null, Color.White, (float)Heading, center, 1.0f, SpriteEffects.None, 0.0f);
            }
        }

        /// <summary>
        /// Performs updates to the game engine based on the agent's intended actions.
        /// </summary>
        /// <param name="time">GameTime component from the main game engine.</param>
        public override void Update(GameTime gameTime)
        {
            if (!Simulator.paused)
            {
                packetBeforeMoving = getRotationPacket();
                UpdateMovement();
                packetAfterMoving = UpdatePosition(gameTime);
                checkregions();
                Sensor.Update(this, packetAfterMoving);
            }
        }

        /// <summary>
        /// Gives the sensor some initial value so its first move is made meaningfully.
        /// </summary>
        public void InitializeSensor()
        {
            Sensor.Update(this, getRotationPacket());
        }

        /// <summary>
        /// Make sure there is a region for the this to move into. This is calculated based on sensors because the sensor field will extend beyond the creature's texture.
        /// </summary>
        public void checkregions()
        {
            // Check if the left edge of the texture has crossed a region border
            deltaX = (packetBeforeMoving.NWCoord.X % Chromaria.Simulator.regionWidth) - (packetAfterMoving.NWCoord.X % Chromaria.Simulator.regionWidth);
            if (Math.Abs(deltaX) > Chromaria.Simulator.deltaXThreshold)
            {
                // Moving from left to right
                if (deltaX > 0)
                {
                    Chromaria.Simulator.regions[packetBeforeMoving.NWCoord.Y / Chromaria.Simulator.regionHeight, packetBeforeMoving.NWCoord.X / Chromaria.Simulator.regionWidth].Remove(Chromaria.Simulator.indexOfCurrentCreature);
                    if (packetAfterMoving.NWCoord.Y % Chromaria.Simulator.regionHeight > packetAfterMoving.SECoord.Y % Chromaria.Simulator.regionHeight)
                        Chromaria.Simulator.regions[packetBeforeMoving.SECoord.Y / Chromaria.Simulator.regionHeight, packetBeforeMoving.NWCoord.X / Chromaria.Simulator.regionWidth].Remove(Chromaria.Simulator.indexOfCurrentCreature);
                }
                // Moving from right to left
                else
                {
                    Chromaria.Simulator.regions[packetAfterMoving.NWCoord.Y / Chromaria.Simulator.regionHeight, packetAfterMoving.NWCoord.X / Chromaria.Simulator.regionWidth].Add(Chromaria.Simulator.indexOfCurrentCreature);
                    if (packetAfterMoving.NWCoord.Y % Chromaria.Simulator.regionHeight > packetAfterMoving.SECoord.Y % Chromaria.Simulator.regionHeight)
                        Chromaria.Simulator.regions[packetAfterMoving.SECoord.Y / Chromaria.Simulator.regionHeight, packetAfterMoving.NWCoord.X / Chromaria.Simulator.regionWidth].Add(Chromaria.Simulator.indexOfCurrentCreature);
                }
            }

            // Check if the right edge of the texture has crossed a region border
            deltaX = (packetBeforeMoving.SECoord.X % Chromaria.Simulator.regionWidth) - (packetAfterMoving.SECoord.X % Chromaria.Simulator.regionWidth);
            if (Math.Abs(deltaX) > Chromaria.Simulator.deltaXThreshold)
            {
                // Moving from left to right
                if (deltaX > 0)
                {
                    Chromaria.Simulator.regions[packetAfterMoving.NWCoord.Y / Chromaria.Simulator.regionHeight, packetAfterMoving.SECoord.X / Chromaria.Simulator.regionWidth].Add(Chromaria.Simulator.indexOfCurrentCreature);
                    if (packetAfterMoving.NWCoord.Y % Chromaria.Simulator.regionHeight > packetAfterMoving.SECoord.Y % Chromaria.Simulator.regionHeight)
                        Chromaria.Simulator.regions[packetAfterMoving.SECoord.Y / Chromaria.Simulator.regionHeight, packetAfterMoving.SECoord.X / Chromaria.Simulator.regionWidth].Add(Chromaria.Simulator.indexOfCurrentCreature);
                }
                // Moving from right to left
                else
                {
                    Chromaria.Simulator.regions[packetBeforeMoving.NWCoord.Y / Chromaria.Simulator.regionHeight, packetBeforeMoving.SECoord.X / Chromaria.Simulator.regionWidth].Remove(Chromaria.Simulator.indexOfCurrentCreature);
                    if (packetAfterMoving.NWCoord.Y % Chromaria.Simulator.regionHeight > packetAfterMoving.SECoord.Y % Chromaria.Simulator.regionHeight)
                        Chromaria.Simulator.regions[packetBeforeMoving.SECoord.Y / Chromaria.Simulator.regionHeight, packetBeforeMoving.SECoord.X / Chromaria.Simulator.regionWidth].Remove(Chromaria.Simulator.indexOfCurrentCreature);
                }
            }

            // Check if the top edge of the texture has crossed a region border
            deltaY = (packetBeforeMoving.NWCoord.Y % Chromaria.Simulator.regionHeight) - (packetAfterMoving.NWCoord.Y % Chromaria.Simulator.regionHeight);
            if (Math.Abs(deltaY) > Chromaria.Simulator.deltaYThreshold)
            {
                // Moving from top to bottom (y is increasing overall, though, as it is measured from top=0)
                if (deltaY > 0)
                {
                    Chromaria.Simulator.regions[packetBeforeMoving.NWCoord.Y / Chromaria.Simulator.regionHeight, packetBeforeMoving.NWCoord.X / Chromaria.Simulator.regionWidth].Remove(Chromaria.Simulator.indexOfCurrentCreature);
                    if (packetAfterMoving.NWCoord.X % Chromaria.Simulator.regionWidth > packetAfterMoving.SECoord.X % Chromaria.Simulator.regionWidth)
                        Chromaria.Simulator.regions[packetBeforeMoving.NWCoord.Y / Chromaria.Simulator.regionHeight, packetBeforeMoving.SECoord.X / Chromaria.Simulator.regionWidth].Remove(Chromaria.Simulator.indexOfCurrentCreature);
                }
                // Moving from bottom to top
                else
                {
                    Chromaria.Simulator.regions[packetAfterMoving.NWCoord.Y / Chromaria.Simulator.regionHeight, packetAfterMoving.NWCoord.X / Chromaria.Simulator.regionWidth].Add(Chromaria.Simulator.indexOfCurrentCreature);
                    if (packetAfterMoving.NWCoord.X % Chromaria.Simulator.regionWidth > packetAfterMoving.SECoord.X % Chromaria.Simulator.regionWidth)
                        Chromaria.Simulator.regions[packetAfterMoving.NWCoord.Y / Chromaria.Simulator.regionHeight, packetAfterMoving.SECoord.X / Chromaria.Simulator.regionWidth].Add(Chromaria.Simulator.indexOfCurrentCreature);
                }
            }

            // Check if the bottom edge of the texture has crossed a region border
            deltaY = (packetBeforeMoving.SECoord.Y % Chromaria.Simulator.regionHeight) - (packetAfterMoving.SECoord.Y % Chromaria.Simulator.regionHeight);
            if (Math.Abs(deltaY) > Chromaria.Simulator.deltaYThreshold)
            {
                // Moving from top to bottom (y is increasing overall, though, as it is measured from top=0)
                if (deltaY > 0)
                {
                    Chromaria.Simulator.regions[packetAfterMoving.SECoord.Y / Chromaria.Simulator.regionHeight, packetAfterMoving.NWCoord.X / Chromaria.Simulator.regionWidth].Add(Chromaria.Simulator.indexOfCurrentCreature);
                    if (packetAfterMoving.NWCoord.X % Chromaria.Simulator.regionWidth > packetAfterMoving.SECoord.X % Chromaria.Simulator.regionWidth)
                        Chromaria.Simulator.regions[packetAfterMoving.SECoord.Y / Chromaria.Simulator.regionHeight, packetAfterMoving.SECoord.X / Chromaria.Simulator.regionWidth].Add(Chromaria.Simulator.indexOfCurrentCreature);
                }
                // Moving from bottom to top
                else
                {
                    Chromaria.Simulator.regions[packetBeforeMoving.SECoord.Y / Chromaria.Simulator.regionHeight, packetBeforeMoving.NWCoord.X / Chromaria.Simulator.regionWidth].Remove(Chromaria.Simulator.indexOfCurrentCreature);
                    if (packetAfterMoving.NWCoord.X % Chromaria.Simulator.regionWidth > packetAfterMoving.SECoord.X % Chromaria.Simulator.regionWidth)
                        Chromaria.Simulator.regions[packetBeforeMoving.SECoord.Y / Chromaria.Simulator.regionHeight, packetBeforeMoving.SECoord.X / Chromaria.Simulator.regionWidth].Remove(Chromaria.Simulator.indexOfCurrentCreature);
                }
            }
        }

        public virtual void UpdateMovement() { }

        /// <summary>
        /// Updates the sprite's position given its current speed and direction.
        /// </summary>
        /// <param name="time">GameTime manager from the main game engine.</param>
        /// <param name="speed">Sprite's speed, vectorized so it can be multiplied with the direction vector.</param>
        /// <param name="direction">Sprite's direction as described by x and y coordinates.</param>
        public RotationPacket UpdatePosition(GameTime time)
        {
            oldPosition = new Vector2(Position.X, Position.Y);
            newPosition = Position + direction * speed * (float)time.ElapsedGameTime.TotalSeconds;
            boundsNW = new Vector2(newPosition.X + Texture.Width / 2 - distCenterToBorder, newPosition.Y + Texture.Height / 2 - distCenterToBorder);
            boundsSE = new Vector2(newPosition.X + Texture.Width / 2 + distCenterToBorder, newPosition.Y + Texture.Height / 2 + distCenterToBorder);

            // If the sensor field would go off the screen, push the creature back a little bit
            // Check the western border
            if (boundsNW.X <= 0)
            {
                newPosition = new Vector2(oldPosition.X + 10, newPosition.Y);
                speed = Vector2.Zero;
            }

            // Check the northern border
            if (boundsNW.Y <= 0)
            {
                newPosition = new Vector2(newPosition.X, oldPosition.Y + 10);
                speed = Vector2.Zero;
            }

            if (Chromaria.Simulator.fixedSizeWorld)
            {
                // Check the eastern border (if applicable)
                if (boundsSE.X >= Chromaria.Simulator.initialBoardWidth - 1)
                {
                    newPosition = new Vector2(oldPosition.X - 10, newPosition.Y);
                    speed = Vector2.Zero;
                }

                // Check the southern border (if applicable)
                if (boundsSE.Y >= Chromaria.Simulator.initialBoardHeight - 1)
                {
                    newPosition = new Vector2(newPosition.X, oldPosition.Y -10);
                    speed = Vector2.Zero;
                }
            }

            Position = newPosition;

            return getRotationPacket();
        }

        /// <summary>
        /// Returns the new width, height, NW coord, and SW coord of the bounding box after the creature (including its sensor field) has been rotated.
        /// </summary>
        /// <returns></returns>
        public override RotationPacket getRotationPacket()
        {
            // Find the borders (unmaximized) of the creature's texture
            creatureNW = new Vector2(this.Position.X, this.Position.Y);
            creatureNE = new Vector2(this.Position.X + Texture.Width, this.Position.Y);
            creatureSW = new Vector2(this.Position.X, this.Position.Y + Texture.Height);
            creatureSE = new Vector2(this.Position.X + Texture.Width, this.Position.Y + Texture.Height);

            // Rotate the points to find the actual global coordinates of the creature's corners
            center = new Vector2(Texture.Width / 2, Texture.Height / 2);
            rotatedCreatureNW = MathHelpers.rotateAroundPoint(Convert.ToInt32(creatureNW.X), Convert.ToInt32(creatureNW.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), Heading);
            rotatedCreatureNE = MathHelpers.rotateAroundPoint(Convert.ToInt32(creatureNE.X), Convert.ToInt32(creatureNE.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), Heading);
            rotatedCreatureSW = MathHelpers.rotateAroundPoint(Convert.ToInt32(creatureSW.X), Convert.ToInt32(creatureSW.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), Heading);
            rotatedCreatureSE = MathHelpers.rotateAroundPoint(Convert.ToInt32(creatureSE.X), Convert.ToInt32(creatureSE.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), Heading);

            // Use the rotated corners to find the min and max values along the x and y dimensions
            minCreatureX = Math.Min(Math.Min(rotatedCreatureNW.X, rotatedCreatureNE.X), Math.Min(rotatedCreatureSW.X, rotatedCreatureSE.X));
            minCreatureY = Math.Min(Math.Min(rotatedCreatureNW.Y, rotatedCreatureNE.Y), Math.Min(rotatedCreatureSW.Y, rotatedCreatureSE.Y));
            maxCreatureX = Math.Max(Math.Max(rotatedCreatureNW.X, rotatedCreatureNE.X), Math.Max(rotatedCreatureSW.X, rotatedCreatureSE.X));
            maxCreatureY = Math.Max(Math.Max(rotatedCreatureNW.Y, rotatedCreatureNE.Y), Math.Max(rotatedCreatureSW.Y, rotatedCreatureSE.Y));

            // Now, apply the same process to find the sensor field's bounding box
            // Get the unrotated coordinates for the (unmaximized) sensor field boundaries
            sensorNW = new Vector2(Position.X + SensorFieldAnchorPoint.X - ((Sensor.SegmentLength * Sensor.ResolutionX) / 2), Position.Y + SensorFieldAnchorPoint.Y - ((Sensor.SegmentLength * Sensor.ResolutionY) / 2));
            sensorNE = new Vector2(sensorNW.X + (Sensor.SegmentLength * Sensor.ResolutionX), sensorNW.Y);
            sensorSW = new Vector2(sensorNW.X, sensorNW.Y + (Sensor.SegmentLength * Sensor.ResolutionY));
            sensorSE = new Vector2(sensorNE.X, sensorSW.Y);

            // Rotate the coordinates to find the actual positions of the sensors in the world
            rotatedSensorNW = MathHelpers.rotateAroundPoint(Convert.ToInt32(sensorNW.X), Convert.ToInt32(sensorNW.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), Heading);
            rotatedSensorNE = MathHelpers.rotateAroundPoint(Convert.ToInt32(sensorNE.X), Convert.ToInt32(sensorNE.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), Heading);
            rotatedSensorSW = MathHelpers.rotateAroundPoint(Convert.ToInt32(sensorSW.X), Convert.ToInt32(sensorSW.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), Heading);
            rotatedSensorSE = MathHelpers.rotateAroundPoint(Convert.ToInt32(sensorSE.X), Convert.ToInt32(sensorSE.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), Heading);

            // Find the max and min values along the x and y dimensions
            minSensorX = Math.Min(Math.Min(rotatedSensorNW.X, rotatedSensorNE.X), Math.Min(rotatedSensorSW.X, rotatedSensorSE.X));
            minSensorY = Math.Min(Math.Min(rotatedSensorNW.Y, rotatedSensorNE.Y), Math.Min(rotatedSensorSW.Y, rotatedSensorSE.Y));
            maxSensorX = Math.Max(Math.Max(rotatedSensorNW.X, rotatedSensorNE.X), Math.Max(rotatedSensorSW.X, rotatedSensorSE.X));
            maxSensorY = Math.Max(Math.Max(rotatedSensorNW.Y, rotatedSensorNE.Y), Math.Max(rotatedSensorSW.Y, rotatedSensorSE.Y));

            // Find the min and max values along the x and y dimensions for the whole creature + sensor system
            minX = Math.Min(minSensorX, minCreatureX);
            minY = Math.Min(minSensorY, minCreatureY);
            maxX = Math.Max(maxSensorX, maxCreatureX);
            maxY = Math.Max(maxSensorY, maxCreatureY);

            // Return the new width, height, NW coord, and SW coord of the bounding box now that the creature (including its sensor field) has been rotated
            return new RotationPacket(maxX - minX, maxY - minY, new Point(minX, minY), new Point(maxX, maxY));
        }

        /// <summary>
        /// Returns the new width, height, NW coord, and SW coord of the bounding box after the creature (including its sensor field) at some arbitrary angle.
        /// </summary>
        /// <param name="angleInRadians"></param>
        /// <returns></returns>
        public RotationPacket getRotationPacketAtAngle(double angleInRadians)
        {
            // Find the borders (unmaximized) of the creature's texture
            creatureNW = new Vector2(this.Position.X, this.Position.Y);
            creatureNE = new Vector2(this.Position.X + Texture.Width, this.Position.Y);
            creatureSW = new Vector2(this.Position.X, this.Position.Y + Texture.Height);
            creatureSE = new Vector2(this.Position.X + Texture.Width, this.Position.Y + Texture.Height);

            // Rotate the points to find the actual global coordinates of the creature's corners
            center = new Vector2(Texture.Width / 2, Texture.Height / 2);
            rotatedCreatureNW = MathHelpers.rotateAroundPoint(Convert.ToInt32(creatureNW.X), Convert.ToInt32(creatureNW.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), angleInRadians);
            rotatedCreatureNE = MathHelpers.rotateAroundPoint(Convert.ToInt32(creatureNE.X), Convert.ToInt32(creatureNE.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), angleInRadians);
            rotatedCreatureSW = MathHelpers.rotateAroundPoint(Convert.ToInt32(creatureSW.X), Convert.ToInt32(creatureSW.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), angleInRadians);
            rotatedCreatureSE = MathHelpers.rotateAroundPoint(Convert.ToInt32(creatureSE.X), Convert.ToInt32(creatureSE.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), angleInRadians);

            // Use the rotated corners to find the min and max values along the x and y dimensions
            minCreatureX = Math.Min(Math.Min(rotatedCreatureNW.X, rotatedCreatureNE.X), Math.Min(rotatedCreatureSW.X, rotatedCreatureSE.X));
            minCreatureY = Math.Min(Math.Min(rotatedCreatureNW.Y, rotatedCreatureNE.Y), Math.Min(rotatedCreatureSW.Y, rotatedCreatureSE.Y));
            maxCreatureX = Math.Max(Math.Max(rotatedCreatureNW.X, rotatedCreatureNE.X), Math.Max(rotatedCreatureSW.X, rotatedCreatureSE.X));
            maxCreatureY = Math.Max(Math.Max(rotatedCreatureNW.Y, rotatedCreatureNE.Y), Math.Max(rotatedCreatureSW.Y, rotatedCreatureSE.Y));

            // Now, apply the same process to find the sensor field's bounding box
            // Get the unrotated coordinates for the (unmaximized) sensor field boundaries
            sensorNW = new Vector2(Position.X + SensorFieldAnchorPoint.X - ((Sensor.SegmentLength * Sensor.ResolutionX) / 2), Position.Y + SensorFieldAnchorPoint.Y - ((Sensor.SegmentLength * Sensor.ResolutionY) / 2));
            sensorNE = new Vector2(sensorNW.X + (Sensor.SegmentLength * Sensor.ResolutionX), sensorNW.Y);
            sensorSW = new Vector2(sensorNW.X, sensorNW.Y + (Sensor.SegmentLength * Sensor.ResolutionY));
            sensorSE = new Vector2(sensorNE.X, sensorSW.Y);

            // Rotate the coordinates to find the actual positions of the sensors in the world
            rotatedSensorNW = MathHelpers.rotateAroundPoint(Convert.ToInt32(sensorNW.X), Convert.ToInt32(sensorNW.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), angleInRadians);
            rotatedSensorNE = MathHelpers.rotateAroundPoint(Convert.ToInt32(sensorNE.X), Convert.ToInt32(sensorNE.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), angleInRadians);
            rotatedSensorSW = MathHelpers.rotateAroundPoint(Convert.ToInt32(sensorSW.X), Convert.ToInt32(sensorSW.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), angleInRadians);
            rotatedSensorSE = MathHelpers.rotateAroundPoint(Convert.ToInt32(sensorSE.X), Convert.ToInt32(sensorSE.Y), Convert.ToInt32(creatureNW.X + center.X), Convert.ToInt32(creatureNW.Y + center.Y), angleInRadians);

            // Find the max and min values along the x and y dimensions
            minSensorX = Math.Min(Math.Min(rotatedSensorNW.X, rotatedSensorNE.X), Math.Min(rotatedSensorSW.X, rotatedSensorSE.X));
            minSensorY = Math.Min(Math.Min(rotatedSensorNW.Y, rotatedSensorNE.Y), Math.Min(rotatedSensorSW.Y, rotatedSensorSE.Y));
            maxSensorX = Math.Max(Math.Max(rotatedSensorNW.X, rotatedSensorNE.X), Math.Max(rotatedSensorSW.X, rotatedSensorSE.X));
            maxSensorY = Math.Max(Math.Max(rotatedSensorNW.Y, rotatedSensorNE.Y), Math.Max(rotatedSensorSW.Y, rotatedSensorSE.Y));

            // Find the min and max values along the x and y dimensions for the whole creature + sensor system
            minX = Math.Min(minSensorX, minCreatureX);
            minY = Math.Min(minSensorY, minCreatureY);
            maxX = Math.Max(maxSensorX, maxCreatureX);
            maxY = Math.Max(maxSensorY, maxCreatureY);

            // Return the new width, height, NW coord, and SW coord of the bounding box now that the creature (including its sensor field) has been rotated
            return new RotationPacket(maxX - minX, maxY - minY, new Point(minX, minY), new Point(maxX, maxY));
        }

        /// <summary>
        /// Determines whether or not the creature is at a valid planting location. A creature is at a valid planting location 
        /// if the contents of its sensor field satisfy a specified function of the creature's own color contents. 
        /// </summary>
        /// <returns>Returns true if the creature is at a valid planting location, otherwise returns false.</returns>
        public bool isAtValidPlantingLocation()
        {
            // If we are using the harder planting function, the creature is not allowed to plant within a certain distance from the center (2x its max dimension)
            if (Chromaria.Simulator.useHarderPlantingFunction)
            {
                rotatedCenter = MathHelpers.rotateAroundPoint(Convert.ToInt32(bodyCenter.X), Convert.ToInt32(bodyCenter.Y), Convert.ToInt32(center.X), Convert.ToInt32(center.Y), Convert.ToDouble(XNAHeading));
                rotatedCenterAsVector = new Vector2(rotatedCenter.X, rotatedCenter.Y);

                float distanceFromCenter = Vector2.Distance(Position + rotatedCenterAsVector, new Vector2(Chromaria.Simulator.initialBoardWidth / 2, Chromaria.Simulator.initialBoardHeight / 2));
                if (distanceFromCenter < (Chromaria.Simulator.harderPlantingFunctionMultiplier * Chromaria.Simulator.pixelHeight))
                    return false;
            }

            blackCount = 0;
            whiteCount = 0;
            redCount = 0;
            greenCount = 0;
            blueCount = 0;
            yellowCount = 0;
            magentaCount = 0;
            cyanCount = 0;
            blackRatioSensor = 0.0;
            whiteRatioSensor = 0.0;
            redRatioSensor = 0.0;
            greenRatioSensor = 0.0;
            blueRatioSensor = 0.0;
            yellowRatioSensor = 0.0;
            magentaRatioSensor = 0.0;
            cyanRatioSensor = 0.0;
                
            // Loop through all valid positions in the sensor fields.
            // (Even though the sensors may take data from noncontiguous pixels, they themselves are 
            // contiguous, so this loop doesn't have to account for resolution.)
            for (int x = 0; x < Sensor.SegmentLength; x++)
            {
                for (int y = 0; y < Sensor.SegmentLength; y++)
                {
                    // Black bin
                    if (Sensor.R[x, y] < threshold && Sensor.G[x, y] < threshold && Sensor.B[x, y] < threshold)
                        blackCount++;
                    // White bin
                    else if (Sensor.R[x, y] > threshold && Sensor.G[x, y] > threshold && Sensor.B[x, y] > threshold)
                        whiteCount++;
                    // Red bin
                    else if (Sensor.R[x, y] > threshold && Sensor.G[x, y] < threshold && Sensor.B[x, y] < threshold)
                        redCount++;
                    // Green bin
                    else if (Sensor.R[x, y] < threshold && Sensor.G[x, y] > threshold && Sensor.B[x, y] < threshold)
                        greenCount++;
                    // Blue bin
                    else if (Sensor.R[x, y] < threshold && Sensor.G[x, y] < threshold && Sensor.B[x, y] > threshold)
                        blueCount++;
                    // Yellow bin
                    else if (Sensor.R[x, y] > threshold && Sensor.G[x, y] > threshold && Sensor.B[x, y] < threshold)
                        yellowCount++;
                    // Magenta bin
                    else if (Sensor.R[x, y] > threshold && Sensor.G[x, y] < threshold && Sensor.B[x, y] > threshold)
                        magentaCount++;
                    // Cyan count
                    else
                        cyanCount++;


                    // If the pixel is close (defined by a user-specified threshold) to R, G, or B, 
                    // add to the relevant colorcounts
                    // Get the ratios for all of the bins
                    numSensors = Math.Pow(Sensor.SegmentLength, 2);
                    blackRatioSensor = blackCount / numSensors;
                    whiteRatioSensor = whiteCount / numSensors;
                    redRatioSensor = redCount / numSensors;
                    greenRatioSensor = greenCount / numSensors;
                    blueRatioSensor = blueCount / numSensors;
                    yellowRatioSensor = yellowCount / numSensors;
                    magentaRatioSensor = magentaCount / numSensors;
                    cyanRatioSensor = cyanCount / numSensors;
                }
            }

            // Check to see if the planting function is satisfied:
            // To calculate the summed difference, take the difference between bin ratios for body and sensor field. 
            // The maximum summed difference will be 8 (max. of 1 for each bin; 8 bins).
            summedDifferences = 0.0;
            summedDifferences += Math.Abs(blackRatioSensor - blackRatio);
            summedDifferences += Math.Abs(whiteRatioSensor - whiteRatio);
            summedDifferences += Math.Abs(redRatioSensor - redRatio);
            summedDifferences += Math.Abs(greenRatioSensor - greenRatio);
            summedDifferences += Math.Abs(blueRatioSensor - blueRatio);
            summedDifferences += Math.Abs(yellowRatioSensor - yellowRatio);
            summedDifferences += Math.Abs(magentaRatioSensor - magentaRatio);
            summedDifferences += Math.Abs(cyanRatioSensor - cyanRatio);

            // Then, if the summed difference does not exceed our tolerated difference, planting succeeds.
			return (summedDifferences <= Chromaria.Simulator.plantingThresholdUpper && summedDifferences >= Chromaria.Simulator.plantingThresholdLower);

        }

        /// <summary>
        /// Places the creature at the center of the world and resets its heading, movement, and state. This is mostly used for bidirectional novelty search.
        /// </summary>
        /// <param name="newXNAHeading"></param>
        public virtual void reset(float newXNAHeading)
        {
            // Put the creature back at the creatureCenter of the world
            Position = new Vector2(Chromaria.Simulator.initialBoardWidth / 2, Chromaria.Simulator.initialBoardHeight / 2);

            // Reset heading and current state
            speed = Vector2.Zero;
            direction = Vector2.Zero;
            currentState = Chromaria.Simulator.State.Moving;
            XNAHeading = newXNAHeading;
            Heading = XNAHeading + Math.PI / 2.0; 
        }
    }
}
