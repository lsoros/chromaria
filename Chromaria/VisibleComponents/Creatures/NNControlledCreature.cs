﻿using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Chromaria.Modes;
using Chromaria.SharpNeatLib.CPPNs;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chromaria.VisibleComponents.Creatures
{
    /// <summary>
    /// This class contains the logic for evolved creatures.
    /// </summary>
    class NNControlledCreature : Creature
    {
        float L, S, R, P;

        public INetwork Controller { get; set; }
        public CombinedGenome Genome { get; set; }
        protected bool freezeAfterPlanting;

        /// <summary>
        /// Constructor that uses a controller generated by the HyperNEAT substrate in the main simulator class (default). It also takes two separate genomes for the morphology and controller and combines then.
        /// </summary>
        /// <param name="x">X coordinate of initial position.</param>
        /// <param name="y">Y coordinate of intial position.</param>
        /// <param name="numSensors">Number of sensor components. Each agent has one sensor composed of at least 3 components.</param>
        public NNControlledCreature(Texture2D bodyTexture, float x, float y, float headingInRadians, INetwork generatedController, Simulator sim, bool drawSensorField_in, bool trackPlanting_in, int numSensors, bool freezeAfterPlanting_in, NeatGenome ControllerCPPNGenome_in, NeatGenome MorphologyCPPNGenome_in, GameTime gameTime = null)
            : base(bodyTexture, x, y, headingInRadians, sim, drawSensorField_in, trackPlanting_in, numSensors, gameTime)
        {
            Controller = generatedController;
            freezeAfterPlanting = freezeAfterPlanting_in;
            Genome = new CombinedGenome(MorphologyCPPNGenome_in, ControllerCPPNGenome_in);
            frozen = false;
        }

        /// <summary>
        /// Constructor that uses a controller generated by the HyperNEAT substrate in the main simulator class (default).
        /// </summary>
        /// <param name="x">X coordinate of initial position.</param>
        /// <param name="y">Y coordinate of intial position.</param>
        /// <param name="numSensors">Number of sensor components. Each agent has one sensor composed of at least 3 components.</param>
        public NNControlledCreature(Texture2D bodyTexture, float x, float y, float headingInRadians, INetwork generatedController, Simulator sim, bool drawSensorField_in, bool trackPlanting_in, int numSensors, bool freezeAfterPlanting_in)
            : base(bodyTexture, x, y, headingInRadians, sim, drawSensorField_in, trackPlanting_in, numSensors)
        {
            Controller = generatedController;
            freezeAfterPlanting = freezeAfterPlanting_in;
            frozen = false;
        }

        /// <summary>
        /// Constructor that manually generates a NN controller (used mostly for debugging).
        /// </summary>
        /// <param name="x">X coordinate of initial position.</param>
        /// <param name="y">Y coordinate of intial position.</param>
        /// <param name="numSensors">Number of sensor components. Each agent has one sensor composed of at least 3 components.</param>
        public NNControlledCreature(Texture2D bodyTexture, float x, float y, float headingInRadians, Simulator sim, bool drawSensorField_in, bool trackPlanting_in, int numSensors, bool freezeAfterPlanting_in)
            : base(bodyTexture, x, y, headingInRadians, sim, drawSensorField_in, trackPlanting_in, numSensors)
        {
            PlanterSubstrate substrate = new PlanterSubstrate(308, 4, 108, new BipolarSigmoid());
            NeatGenome genome = substrate.generateGenome();
            Controller = genome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            freezeAfterPlanting = freezeAfterPlanting_in;
            frozen = false;
        }

        /// <summary>
        /// Performs updates to the game engine based on the agent's intended actions.
        /// </summary>
        /// <param name="time">GameTime component from the main game engine.</param>
        public override void Update(GameTime gameTime)
        {
            if (!frozen && !Simulator.paused)
            {
                ActivateNetwork();
                UpdateMovement();
                packetAfterMoving = base.UpdatePosition(gameTime);
                Sensor.Update(this, packetAfterMoving);
            }
        }

        /// <summary>
        /// Sets the inputs to the creature's neural network, activates it, and reads the outputs.
        /// </summary>
        public void ActivateNetwork()
        {
            Controller.ClearSignals();

            // Set the controller inputs based on the sensors
            // [0-100] -> R
            // [100-199] -> G
            // [200-299] -> B
            for (int i = 0; i < Sensor.SegmentLength; i++)
            {
                for (int j = 0; j < Sensor.SegmentLength; j++)
                {
                    Controller.SetInputSignal(i * Sensor.SegmentLength + j, (float)Sensor.R[i, j]);
                    Controller.SetInputSignal(100 + (i * Sensor.SegmentLength) + j, (float)Sensor.G[i, j]);
                    Controller.SetInputSignal(200 + (i * Sensor.SegmentLength) + j, (float)Sensor.B[i, j]);
                }
            }

            // Set the controller's heading inputs
            int headingIndex;
            double headingSegmentRange = (2.0 * Math.PI) / 8.0;
            if (XNAHeading < (-Math.PI + headingSegmentRange))
                headingIndex = 1;
            else if (XNAHeading < (-Math.PI + (2 * headingSegmentRange)))
                headingIndex = 0;
            else if (XNAHeading < (-Math.PI + (3 * headingSegmentRange)))
                headingIndex = 3;
            else if (XNAHeading < (-Math.PI + (4 * headingSegmentRange)))
                headingIndex = 5;
            else if (XNAHeading < (-Math.PI + (5 * headingSegmentRange)))
                headingIndex = 6;
            else if (XNAHeading < (-Math.PI + (6 * headingSegmentRange)))
                headingIndex = 7;
            else if (XNAHeading < (-Math.PI + (7 * headingSegmentRange)))
                headingIndex = 4;
            else if (XNAHeading < (-Math.PI + (8 * headingSegmentRange)))
                headingIndex = 2;
            else
                headingIndex = 1;

            for (int i = 0; i < 8; i++)
            {
                if(i==headingIndex)
                    Controller.SetInputSignal(300 + i, 1.0f);
                else
                    Controller.SetInputSignal(300 + i, 0.0f);
            }

            // Activate the network
            Controller.MultipleSteps(2);

            // Get the outputs from the controller
            L = Controller.GetOutputSignal(0);
            S = MathHelpers.Scale(Controller.GetOutputSignal(1), -1.0, 1.0, 0.0, 1.0);
            R = Controller.GetOutputSignal(2);
            P = Controller.GetOutputSignal(3);
        }

        /// <summary>
        /// This is a subfunction of the Update()function that handles the movement logic.
        /// </summary>
        public override void UpdateMovement()
        {
            // If the creature has planted and we want to freeze after planting, don't bother doing anything else
            if (P > 0.0 && freezeAfterPlanting)
            {
                currentState = Simulator.State.Planting;
                speed = Vector2.Zero;
                direction = Vector2.Zero;
                return;
            }

            // Otherwise, first update the current state
            if(P > 0.0)
                currentState = Simulator.State.Planting;
            else
                currentState = Simulator.State.Moving;

            // Then move based on the outputs from the controller
            // (Outputs should range between -1 and 1.) 

            // Determine the angle of rotation (if any) based on the L and R outputs
            // (If the angle of rotation is positive, the rotation will be clockwise.)
            // Set the angle in degrees appropriately w.r.t. the direction of rotation
            int angleInDegrees = Convert.ToInt32(Simulator.ROTATION_SPEED * (R - L));
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

            // Calculate the new direction given the updated heading
            float xVal, yVal;

            // Southwest quadrant of the unit circle
            if (XNAHeading < (float)(-Math.PI / 2.0))
            {
                xVal = -1 - (((float)XNAHeading % (float)(Math.PI / 2.0)) / (float)(Math.PI / 2.0));
                yVal = -1 * ((float)XNAHeading % (float)(Math.PI / 2.0) / (float)(Math.PI / 2.0)); 
            }

            // Northwest quadrant of the unit circle
            else if (XNAHeading < 0.0)
            {
                xVal = (((float)XNAHeading % (float)(Math.PI / 2.0)) / (float)(Math.PI / 2.0));
                yVal = -1 + ((((float)XNAHeading % (float)(Math.PI / 2.0)) / (float)(Math.PI / 2.0)) * -1);
                
            }

            // Northeast quadrant of the unit circle
            else if (XNAHeading < (float)(Math.PI / 2.0))
            {
                xVal = (float)XNAHeading / (float)(Math.PI / 2.0);
                yVal = -1 - (-1 *  ((float)XNAHeading / (float)(Math.PI / 2.0)));
            }

            // Southeast quadrant of the unit circle
            else
            {
                xVal = 1 - (((float)XNAHeading % (float)(Math.PI / 2.0)) / (float)(Math.PI / 2.0));
                yVal = ((float)XNAHeading % (float)(Math.PI / 2.0 + 0.00001)) / (float)(Math.PI / 2.0);
            }

            // Set the new direction
            direction = new Vector2(xVal, yVal);

            // Set the new speed based on the S output
            speed = new Vector2(S * Simulator.MOVEMENT_SPEED, S * Simulator.MOVEMENT_SPEED);
        }

        /// <summary>
        /// Reset the neural controller and flip the creature to some default rotation. This function is usually only called during bidirectional novelty search.
        /// </summary>
        /// <param name="newXNAHeading"></param>
        public override void reset(float newXNAHeading)
        {
            Controller.ClearSignals();
            base.reset(newXNAHeading);
        }

        /// <summary>
        /// Stop the creature from moving (i.e. if it has planted).
        /// </summary>
        public void freeze()
        {
            frozen = true;
            Controller.ClearSignals();
            speed = Vector2.Zero;
        }
    }
}
