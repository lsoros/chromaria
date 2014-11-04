using Chromaria.SharpNeatLib;
using Chromaria.SharpNeatLib.CPPNs;
using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.Evolution.Xml;
using Chromaria.SharpNeatLib.Experiments;
using Chromaria.SharpNeatLib.Maths;
using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.SharpNeatLib.NeatGenome.Xml;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml;

namespace Chromaria
{
    /// <summary>
    /// Replays one XML-encoded invidual (i.e. one generated via novelty search).
    /// </summary>
    class DemoFromXML : Simulator
    {
        // Instance variables
        NNControlledCreature creature;
        Texture2D morphology;

        bool fixedBackground = true;
        Texture2D seedMorphology;

        /// <summary>
        /// Default constructor. All logic is in the Simulator constructor.
        /// </summary>
        public DemoFromXML() : base() { }

        protected override void Initialize()
        {
            base.Initialize();

            string backgroundSeedFilename = "pink_blue.xml";
            NeatGenome seedCPPNGenome = loadCPPNFromXml(backgroundSeedFilename);
            INetwork seedCPPN = seedCPPNGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            seedMorphology = generateMorphology(seedCPPN);

            // Create the world / region system
            // Note: The morphology must be generated in advance of the Load
            INetwork morphologyCPPN = loadCPPNFromXml(initialMorphologyFilename).Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            morphology = generateMorphology(morphologyCPPN);
            redTexture = generateSolidMorphology(morphology);
            InitializeRegions();

            // Configure the HyperNEAT substrate
            controllerSubstrate = new ControllerSubstrate(308, 4, 108, new BipolarSigmoid());
            controllerSubstrate.weightRange = 5.0;
            controllerSubstrate.threshold = 0.2;

            // Load up the creature to be demo'd
            IGenome controllerCPPNGenome = loadCPPNFromXml(initialControllerFilename);
            INetwork controllerCPPN = controllerCPPNGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            IGenome controllerGenome = controllerSubstrate.generateGenome(controllerCPPN);
            INetwork generatedController = controllerGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));

            int x = initialBoardWidth / 2;
            int y = initialBoardHeight / 2;

            creature = new NNControlledCreature(morphology, x, y, initialHeading, generatedController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting);
            indexOfCurrentCreature = Components.Count - 1;
            regions[y / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
            if ((x % regionWidth > (x + morphology.Width) % regionWidth) && (y % regionHeight > (y + morphology.Height) % regionHeight) && !regions[(y + morphology.Height) / regionHeight, (x + morphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                regions[(y + morphology.Height) / regionHeight, (x + morphology.Width) / regionWidth].Add(indexOfCurrentCreature);
            if (x % regionWidth > (x + morphology.Width) % regionWidth && !regions[(y / regionHeight), (x + morphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                regions[(y / regionHeight), (x + morphology.Width) / regionWidth].Add(indexOfCurrentCreature);
            if (y % regionHeight > (y + morphology.Height) % regionHeight && !(regions[(y + morphology.Height) / regionHeight, x / regionWidth].Contains(indexOfCurrentCreature)))
                regions[(y + morphology.Height) / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
        }

        private void InitializeRegions()
        {
            if (stripedBackground)
                new StaticImage("Background", 0, 0, generateStripedBackground(true, true, true, true), this);
            else if (fixedBackground)
                Simulator.initialBackground = new StaticImage("Background", 0, 0, generateSeededBackground(seedMorphology, true, true, true, true), this);
            else
                new StaticImage("Background", 0, 0, generateSeededBackground(morphology, true, true, true, true), this);
            regions[0, 0].Add(Components.Count - 1);
        }

        /// <summary>
        /// This function loads any external graphics into the simulator via the XNA content pipeline. 
        /// Note: This project uses monogame insteadof XNA, but external graphics must be pre-compiled 
        /// using XNA (not monogame) in order to be loaded this way.
        /// </summary>
        protected override void LoadContent() { base.LoadContent(); }

        protected INetwork generateControllerManually()
        {
            FloatFastConnection[] connections = new FloatFastConnection[11];
            connections[0] = new FloatFastConnection();
            connections[0].sourceNeuronIdx = 0;
            connections[0].targetNeuronIdx = 11;
            connections[0].weight = 1.0f;
            connections[0].signal = 1.0f;
            connections[1] = new FloatFastConnection();
            connections[1].sourceNeuronIdx = 1;
            connections[1].targetNeuronIdx = 11;
            connections[1].weight = 1.0f;
            connections[1].signal = 1.0f;
            connections[2] = new FloatFastConnection();
            connections[2].sourceNeuronIdx = 2;
            connections[2].targetNeuronIdx = 11;
            connections[2].weight = 1.0f;
            connections[2].signal = 1.0f;
            connections[3] = new FloatFastConnection();
            connections[3].sourceNeuronIdx = 3;
            connections[3].targetNeuronIdx = 11;
            connections[3].weight = 1.0f;
            connections[3].signal = 1.0f;
            connections[4] = new FloatFastConnection();
            connections[4].sourceNeuronIdx = 4;
            connections[4].targetNeuronIdx = 11;
            connections[4].weight = 1.0f;
            connections[4].signal = 1.0f;
            connections[5] = new FloatFastConnection();
            connections[5].sourceNeuronIdx = 11;
            connections[5].targetNeuronIdx = 5;
            connections[5].weight = 1.0f;
            connections[5].signal = 1.0f;
            connections[6] = new FloatFastConnection();
            connections[6].sourceNeuronIdx = 11;
            connections[6].targetNeuronIdx = 6;
            connections[6].weight = 1.0f;
            connections[6].signal = 1.0f;
            connections[7] = new FloatFastConnection();
            connections[7].sourceNeuronIdx = 11;
            connections[7].targetNeuronIdx = 7;
            connections[7].weight = 1.0f;
            connections[7].signal = 1.0f;
            connections[8] = new FloatFastConnection();
            connections[8].sourceNeuronIdx = 11;
            connections[8].targetNeuronIdx = 8;
            connections[8].weight = 1.0f;
            connections[8].signal = 1.0f;
            connections[9] = new FloatFastConnection();
            connections[9].sourceNeuronIdx = 11;
            connections[9].targetNeuronIdx = 9;
            connections[9].weight = 1.0f;
            connections[9].signal = 1.0f;
            connections[10] = new FloatFastConnection();
            connections[10].sourceNeuronIdx = 11;
            connections[10].targetNeuronIdx = 10;
            connections[10].weight = 1.0f;
            connections[10].signal = 1.0f;

            IActivationFunction[] funs = new IActivationFunction[12];
            for (int i = 0; i < 12; i++)
                funs[i] = new BipolarSigmoid();

            INetwork ControllerCPPN = new FloatFastConcurrentNetwork(1, 4, 6, 12, connections, funs);
            return controllerSubstrate.generateNetwork(ControllerCPPN);
        }
    }
}
