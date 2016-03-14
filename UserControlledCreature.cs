using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chromaria
{
    /// <summary>
    /// This class contains the logic for sprites that are controlled by the keyboard (mostly for debugging purposes).
    /// </summary>
    class UserControlledCreature : Creature
    {
        const int NORTH = -1;
        const int SOUTH = 1;
        const int WEST = -1;
        const int EAST = 1;

        Simulator s;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="x">X coordinate of initial position.</param>
        /// <param name="y">Y coordinate of intial position.</param>
        /// <param name="numSensors">Number of sensor components. Each agent has one sensor composed of at least 3 components.</param>
        // Note: depth is required because the creatures cannot be on the deepest layer.
        public UserControlledCreature(Texture2D bodyTexture, float x, float y, float headingInRadians, Simulator sim, bool drawSensorField_in, bool trackPlanting_in, int numSensors)
            : base(bodyTexture, x, y, headingInRadians, sim, drawSensorField_in, trackPlanting_in, numSensors) { s = sim; }

        /// <summary>
        /// This is a subfunction of the Update() function that handles the movement logic.
        /// </summary>
        /// <param name="aCurrentKeyboardState">Tells us which keyboard buttons were pressed at update time.</param>
        public override void UpdateMovement()
        {
            // Read any input from the keyboard
            KeyboardState keyboard = Keyboard.GetState();

            if (this.currentState == Chromaria.Simulator.State.Moving)
            {
                // Rotate the creature clockwise if R is pressed or counterclockwise if E is pressed.
                if (keyboard.IsKeyDown(Keys.R) || keyboard.IsKeyDown(Keys.E))
                {
                    // Set the angle in degrees appropriately w.r.t. the direction of rotation
                    int angleInDegrees;
                    if (keyboard.IsKeyDown(Keys.E) == true)
                        angleInDegrees = -5;
                    else
                        angleInDegrees = 5;

                    // Convert from degrees to radians
                    float angleInRadians = MathHelpers.convertToRadians(angleInDegrees);

                    // Update the heading based on the new angle of rotation
                    Heading += angleInRadians;
                    if (Heading > Math.PI)
                        Heading = -Math.PI;
                    else if (Heading < -Math.PI)
                        Heading = Math.PI;

                    XNAHeading += angleInRadians;
                    if (XNAHeading > Math.PI)
                        XNAHeading = (float)(-Math.PI);
                    else if (XNAHeading < -Math.PI)
                        XNAHeading = (float)Math.PI;
                }

                // Set the new speed
                float xVal = 0, yVal = 0;
                if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.Right))
                    xVal = Chromaria.Simulator.MOVEMENT_SPEED;
                if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.Down))
                    yVal = Chromaria.Simulator.MOVEMENT_SPEED;
                speed = new Vector2(xVal, yVal);

                // Set the new direction
                xVal = 0;
                yVal = 0;
                if (keyboard.IsKeyDown(Keys.Left))
                    xVal = WEST;
                else if (keyboard.IsKeyDown(Keys.Right))
                    xVal = EAST;
                if (keyboard.IsKeyDown(Keys.Up))
                    yVal = NORTH;
                else if (keyboard.IsKeyDown(Keys.Down))
                    yVal = SOUTH;
                direction = new Vector2(xVal, yVal);
            }

            // Pressing L will trigger planting. Useful for debugging.
            if (keyboard.IsKeyDown(Keys.L))
                this.currentState = Simulator.State.Planting;
            else
                this.currentState = Simulator.State.Moving;

            if (this.currentState.Equals(Simulator.State.Planting) && Simulator.outputPlantingSuccess)
            {
                if (this.isAtValidPlantingLocation())
                    Console.WriteLine("Planting attempt succeeded.");
                else
                    Console.WriteLine("Planting attempt failed.");
            }
        }
    }
}
