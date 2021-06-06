using Chromaria.SharpNeatLib;
using Chromaria.SharpNeatLib.CPPNs;
using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.Evolution.Xml;
using Chromaria.SharpNeatLib.Experiments;
using Chromaria.SharpNeatLib.Maths;
using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.SharpNeatLib.NeatGenome.Xml;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.VisibleComponents;
using Chromaria.VisibleComponents.Creatures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml;

namespace Chromaria.Modes
{
    /// <summary>
    /// Creates a static environment and a creature that is controlled with the keyboard. Mostly used for debugging.
    /// </summary>
    class ManualRun : Simulator
    {
        #region instance variables
        int numUpdates;
        Texture2D morphology;
        #endregion

        /// <summary>
        /// Default constructor. All logic is in the Simulator constructor.
        /// </summary>
        public ManualRun() : base() { }

        /// <summary>
        /// This function contains all of the pre-run logic that doesn't involve graphics.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Create the world / region system
            // Note: The morphology must be generated in advance of the Load
            INetwork morphologyCPPN = loadCPPNFromXml(initialMorphologyFilename).Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            if (useRandomMorphology)
                morphology = generateUniformlyColoredMorphology();
            else
                morphology = generateMorphology(morphologyCPPN);
            redTexture = generateSolidMorphology(morphology);
            InitializeRegions();

            int x, y;

            // Load the user-controlled agent into the world
            x = 350;
            y = 350;

            manuallyControlledCreatureID = 21;

            Creature creature;
            creature = new UserControlledCreature(morphology, x, y, initialHeading, this, drawSensorField, trackPlanting, defaultNumSensors);

            indexOfCurrentCreature = Components.Count - 1;
            regions[y / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
            if ((x % regionWidth > (x + morphology.Width) % regionWidth) && (y % regionHeight > (y + morphology.Height) % regionHeight) && !regions[(y + morphology.Height) / regionHeight, (x + morphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                regions[(y + morphology.Height) / regionHeight, (x + morphology.Width) / regionWidth].Add(indexOfCurrentCreature);
            if (x % regionWidth > (x + morphology.Width) % regionWidth && !regions[(y / regionHeight), (x + morphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                regions[(y / regionHeight), (x + morphology.Width) / regionWidth].Add(indexOfCurrentCreature);
            if (y % regionHeight > (y + morphology.Height) % regionHeight && !(regions[(y + morphology.Height) / regionHeight, x / regionWidth].Contains(indexOfCurrentCreature)))
                regions[(y + morphology.Height) / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);


            manuallyControlledCreatureID = creature.ID;
        }

        /// <summary>
        /// Generates the background for the world.
        /// </summary>
        private void InitializeRegions()
        {
            if (stripedBackground)
                new StaticImage("Background", 0, 0, generateStripedBackground(true, true, true, true), this);
            else
                new StaticImage("Background", 0, 0, generateSeededBackground(morphology, true, true, true, true), this);
            regions[0, 0].Add(Components.Count - 1);
        }

        /// <summary>
        /// Loads any graphics resources into the content manager.
        /// </summary>
        protected override void LoadContent() { base.LoadContent(); }

        /// <summary>
        /// Handles any per-tick logic. Called automatically on loop by the game engine.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            numUpdates++;
            base.Update(gameTime);
        }

        /// <summary>
        ///  Handles any logic that should execute when the simulator quits, such as writing log files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
        }
    }
}
