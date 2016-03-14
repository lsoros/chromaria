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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        Texture2D seedMorphology;

        /// <summary>
        /// Default constructor. All logic is in the Simulator constructor.
        /// </summary>
        public DemoFromXML() : base() { }

        protected override void Initialize()
        {
            base.Initialize();

            string backgroundFileName = (logsFolder + "0\\background.dat");
            backgroundImage = new StaticImage("Background", 0, 0, ReadBackgroundFromFile(backgroundFileName), this);

            // Create the world / region system
            // Note: The morphology must be generated in advance of the Load

            int folderNumber = (Simulator.replayIndividualNumber-1) / numCreaturesPerFolder;
            initialMorphologyFilename = logsFolder + folderNumber.ToString() + "\\" + morphologyXMLprefix + Simulator.replayIndividualNumber.ToString() + ".xml";
            INetwork morphologyCPPN = loadCPPNFromXml(initialMorphologyFilename).Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            morphology = generateMorphology(morphologyCPPN);
            StaticImage demoCreature = new StaticImage("Creature", initialBoardWidth / 2, initialBoardHeight / 2, morphology, this);
        }


        /// <summary>
        /// This function loads any external graphics into the simulator via the XNA content pipeline. 
        /// Note: This project uses monogame insteadof XNA, but external graphics must be pre-compiled 
        /// using XNA (not monogame) in order to be loaded this way.
        /// </summary>
        protected override void LoadContent() { base.LoadContent(); }

        protected override void Update(GameTime gameTime){}

        protected Texture2D ReadBackgroundFromFile(string fileName)
        {
            Color[] loadedBackground = new Color[initialBoardHeight * initialBoardWidth];
            if (File.Exists(fileName))
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
                {
                    for (int x = 0; x < initialBoardWidth; x++)
                    {
                        for (int y = 0; y < initialBoardHeight; y++)
                        {
                            loadedBackground[x + (y * initialBoardWidth)].R = reader.ReadByte();
                            loadedBackground[x + (y * initialBoardWidth)].G = reader.ReadByte();
                            loadedBackground[x + (y * initialBoardWidth)].B = reader.ReadByte();
                            loadedBackground[x + (y * initialBoardWidth)].A = reader.ReadByte();
                        }
                    }
                }
                Texture2D newBackgroundTexture = new Texture2D(this.GraphicsDevice, initialBoardWidth, initialBoardHeight);
                newBackgroundTexture.SetData(loadedBackground);
                return newBackgroundTexture;
            }
            else
                throw new Exception(fileName + " does not exist.");
        }
    }
}
