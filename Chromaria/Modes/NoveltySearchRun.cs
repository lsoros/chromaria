using Chromaria.SharpNeatLib;
using Chromaria.SharpNeatLib.CPPNs;
using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.Evolution.Xml;
using Chromaria.SharpNeatLib.Experiments;
using Chromaria.SharpNeatLib.Maths;
using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.SharpNeatLib.NeatGenome.Xml;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.Utils;
using Chromaria.VisibleComponents;
using Chromaria.VisibleComponents.Creatures;
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
using System.Windows;
using System.Xml;

namespace Chromaria.Modes
{
    class NoveltySearchRun : Simulator
    {
        // Instance variables: general
        int numBidirectionalPlanters;
        int numFirstTrialPlanters;
        int numSecondTrialPlanters;
        int numBidirectionalMisplanters;
        int numFirstTrialMisplanters;
        int numSecondTrialMisplanters;
        int GenomeIndexOfCurrentCreature;
        int numUpdates;
        bool firstTrial;
        bool plantedInColoredSpace1;
        bool plantedInColoredSpace2;
        bool plantedInWhiteSpace1;
        bool plantedInWhiteSpace2;
        Creature currentCreature;

        // Instance variables: evolution-specific
        NeatParameters neatParams;
        GenomeFactory cppnGenerator;
        GenomeList cppnGenomeList;
        Population popn;
        EvolutionAlgorithm ea;
        IdGenerator idGen;
        Texture2D morphology;
        int generation;

        // Instance variables: logging
        public static string noveltyLogsFolder;
        public static string plantersFolder;

        /// <summary>
        /// Default constructor. All logic is in the Simulator constructor.
        /// </summary>
        public NoveltySearchRun() : base() { }

        /// <summary>
        /// This function contains all of the pre-run logic that doesn't involve graphics.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Create the world / region system
            // Note: The morphology must be generated in advance of the Load
            INetwork morphologyCPPN = loadCPPNFromXml(initialMorphologyFilename).Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            morphology = generateMorphology(morphologyCPPN);
            redTexture = generateSolidMorphology(morphology);
            InitializeRegions();

            // Initialize a log to track some instance-specific data
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("RunInfo.txt", true))
            {
                file.WriteLine("Novelty search run");
                file.WriteLine("Start time: " + DateTime.Now.ToString("HH:mm:ss tt"));
                if (freezeAfterPlanting)
                    file.WriteLine("Individuals are immobilized once they attempt to plant.");
                else
                    file.WriteLine("Individuals are allowed to keep moving even if/after they attempt to plant.");
                file.WriteLine("Morphology genome XML filename: " + initialControllerFilename);
                file.WriteLine("Behavior update interval: " + behaviorUpdateInterval);
                file.WriteLine("Planting weight: " + plantingWeight);
                file.WriteLine("Position weight: " + positionWeight);
                file.WriteLine("Population size: " + populationSize);
                file.WriteLine("Archive threshold: " + archiveThreshold);
            }

            // Initialize some static variables for the simulation
            numBidirectionalPlanters = 0;
            numFirstTrialPlanters = 0;
            numSecondTrialPlanters = 0;
            numBidirectionalMisplanters = 0;
            numFirstTrialMisplanters = 0;
            numSecondTrialMisplanters = 0;
            firstTrial = true;

            // Set the NEAT parameters
            neatParams = new NeatParameters();
            neatParams.archiveThreshold = archiveThreshold;
            neatParams.noveltyFixed = true;
            neatParams.noveltySearch = true;

            // Configure the HyperNEAT substrate
            controllerSubstrate = new ControllerSubstrate(308, 4, 108, new BipolarSigmoid());
            controllerSubstrate.weightRange = 5.0;
            controllerSubstrate.threshold = 0.2;

            // Create a genome factory to generate a list of CPPN genomes
            cppnGenerator = new GenomeFactory();
            idGen = new IdGenerator();
            cppnGenomeList = cppnGenerator.CreateGenomeList(neatParams, idGen, 4, 8, 1.0f, populationSize);
            GenomeIndexOfCurrentCreature = 0;

            // Initialize the folders for storing the archive and planters
            noveltyLogsFolder = Directory.GetCurrentDirectory() + "\\archive\\" + GenomeIndexOfCurrentCreature + "\\";
            if (!Directory.Exists(noveltyLogsFolder))
                Directory.CreateDirectory(noveltyLogsFolder);
            plantersFolder = Directory.GetCurrentDirectory() + "\\planters\\" + GenomeIndexOfCurrentCreature + "\\";
            if (!Directory.Exists(plantersFolder))
                Directory.CreateDirectory(plantersFolder);

            // Create an initial population based on the genome list
            popn = new Population(idGen, cppnGenomeList);

            // Set the generation counter
            // Note: This must be kept seperately from the EA generation counter because novelty search here does't follow the traditional loop. 
            generation = 1;

            // Create the EA
            // (Don't run the EA until the first generation has had a chance to go through the simulation. 
            // The EA call happens in Simulator.NewGeneration().)
            ea = new EvolutionAlgorithm(popn, new ChromariaPopulationEvaluator(new ChromariaNetworkEvaluator()), neatParams);

            // Initialize the behavior trackers for this individual
            ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior = new BehaviorType();
            ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior.behaviorList = new List<double>();

            // Generate the initial creature
            int x = initialBoardWidth / 2;
            int y = initialBoardHeight / 2;
            INetwork newController = controllerSubstrate.generateGenome(ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"))).Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            if (bidirectionalTrials)
                currentCreature = new NNControlledCreature(morphology, x, y, initialHeading + (float)(Math.PI / 2.0), newController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting);
            else
                currentCreature = new NNControlledCreature(morphology, x, y, initialHeading, newController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting);
            currentCreature.DrawOrder = 1;
            indexOfCurrentCreature = Components.Count - 1;

            // Add the creature to the simulator's region lists
            int currentPointer = Components.Count - 1;
            regions[y / regionHeight, x / regionWidth].Add(currentPointer);
            if ((x % regionWidth > (x + morphology.Width) % regionWidth) && (y % regionHeight > (y + morphology.Height) % regionHeight) && !regions[(y + morphology.Height) / regionHeight, (x + morphology.Width) / regionWidth].Contains(currentPointer))
                regions[(y + morphology.Height) / regionHeight, (x + morphology.Width) / regionWidth].Add(currentPointer);
            if (x % regionWidth > (x + morphology.Width) % regionWidth && !regions[(y / regionHeight), (x + morphology.Width) / regionWidth].Contains(currentPointer))
                regions[(y / regionHeight), (x + morphology.Width) / regionWidth].Add(currentPointer);
            if (y % regionHeight > (y + morphology.Height) % regionHeight && !(regions[(y + morphology.Height) / regionHeight, x / regionWidth].Contains(currentPointer)))
                regions[(y + morphology.Height) / regionHeight, x / regionWidth].Add(currentPointer);

            // Preliminarily update the creature's sensors so its first movements are actually based on what's underneath its starting position
            currentCreature.InitializeSensor();

            plantedInColoredSpace1 = false;
            plantedInColoredSpace2 = false;
            plantedInWhiteSpace1 = false;
            plantedInWhiteSpace2 = false;
            numUpdates = 0;
        }

        private void InitializeRegions()
        {
            if (stripedBackground)
                new StaticImage("Background", 0, 0, generateStripedBackground(true, true, true, true), this);
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

        /// <summary>
        /// Performs one tick of the simulation. This function is called automatically on loop by the game engine. 
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            // base.Update() will call the Creature.Update() function
            base.Update(gameTime);

            if (!paused)
            {
                bool stop = false;

                // Stopping conditions:
                // 1) we are at the beginning of a first trial and there are already entries in the behavior characterization vector
                if (numUpdates == 0 && ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior.behaviorList.Count != 0)
                    stop = true;

                // 2) we have exceeded the number of max time steps
                else if ((!bidirectionalTrials && (numUpdates > maxTimeSteps)) || (bidirectionalTrials && (numUpdates > (2 * maxTimeSteps))))
                    stop = true;

                // 3) we are freezing after planted and someone has planted
                else if (freezeAfterPlanting && ((firstTrial && (plantedInColoredSpace1 || plantedInWhiteSpace1)) || (!firstTrial && (plantedInColoredSpace2 || plantedInWhiteSpace2))))
                    stop = true;

                if (stop)
                {
                    // If we're stopping, first check to see if we need to perform another generation of the EA
                    if (GenomeIndexOfCurrentCreature == ea.Population.GenomeList.Count - 1)
                    {
                        // Write the just-completed generation's data to XML before it is lost
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter("RunInfo.txt", true))
                            file.WriteLine("Generation " + generation + " completed at " + DateTime.Now.ToString("HH:mm:ss tt"));

                        // Perform one run of the EA now that we have behavioral data
                        ea.noveltyFixed.measure_against = ea.Population.GenomeList;
                        ea.PerformOneGeneration();

                        // Increase the generation counter, now that we've written the last one
                        generation++;
                        if (fixedIndividuals && generation * populationSize >= numIndividuals)
                            Exit();
                        else
                        {
                            // Reset the index counter and start the next generation from the beginning
                            GenomeIndexOfCurrentCreature = -1;
                            ResetToFirstTrial();
                            return;
                        }
                    }

                    // Otherwise we can just rest to the first trial
                    else
                    {
                        ResetToFirstTrial();
                        return;
                    }
                }

                // If we're in a bidirectional search, we may need to being the second trial
                if (bidirectionalTrials && (numUpdates == (maxTimeSteps - 1)))
                {
                    beginSecondTrial();
                    return;
                }

                // Update the behavior vector for the creature that is currently be being evaluated
                numUpdates++;
                ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior.behaviorList.Add(currentCreature.Position.X);
                ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior.behaviorList.Add(currentCreature.Position.Y);
                ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior.behaviorList.Add(currentCreature.Heading);

                // If the creature has planted itself, decide whether or not it planted itself in a valid position
                // and tidy up some other business. 
                if (currentCreature.currentState.Equals(State.Planting))
                {
                    // Append 1 to the behavior vector if the creature is planting
                    ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior.behaviorList.Add(1.0);
                    if (currentCreature.isAtValidPlantingLocation())
                    {
                        if (firstTrial)
                            plantedInColoredSpace1 = true;
                        else
                            plantedInColoredSpace2 = true;
                    }
                    else
                    {
                        if (firstTrial)
                            plantedInWhiteSpace1 = true;
                        else
                            plantedInWhiteSpace2 = true;
                    }
                }
                else
                {
                    // Otherwise append 0 to the behavior vector if the creature is not moving
                    ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior.behaviorList.Add(0.0);
                }
            }
        }

        /// <summary>
        /// Draws all drawable game components. This function is called automatically on loop by the game engine.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        /// <summary>
        /// If we are doing a bidirectional novelty search, flip the creature around and put it in the center of the world.
        /// </summary>
        private void beginSecondTrial()
        {
            numUpdates++;

            // Remove the creature from the old region lists
            // Update the region lists
            foreach (List<int> region in regions)
                region.RemoveAt(indexOfCurrentCreature);

            // If the creature has only completed one trial, we need to put it back in the creatureCenter of the world and flip it around.
            currentCreature.reset(initialHeading - (float)(Math.PI / 2.0));
            firstTrial = false;

            // Add the creature back into the appropriate region lists
            RotationPacket rp = currentCreature.getRotationPacket();
            regions[rp.NWCoord.Y / regionHeight, rp.NWCoord.X / regionWidth].Add(indexOfCurrentCreature);
            if ((rp.NWCoord.X % regionWidth > rp.SECoord.X % regionWidth) && (rp.NWCoord.Y % regionHeight > rp.SECoord.Y % regionHeight))
                regions[rp.SECoord.Y / regionHeight, rp.SECoord.X / regionWidth].Add(indexOfCurrentCreature);
            if (rp.NWCoord.X % regionWidth > rp.SECoord.X % regionWidth)
                regions[(rp.NWCoord.Y / regionHeight), rp.SECoord.X / regionWidth].Add(indexOfCurrentCreature);
            if (rp.NWCoord.Y % regionHeight > rp.SECoord.Y % regionHeight)
                regions[rp.SECoord.Y / regionHeight, rp.SECoord.X / regionWidth].Add(indexOfCurrentCreature);
        }

        /// <summary>
        /// This method is called when one individual finishes its run in the world (either because it planted itself
        /// or because it timed out). It should ONLY be called when the creature is being controlled by a neural network (novelty search or evolution).
        /// </summary>
        /// 
        private void ResetToFirstTrial()
        {
            // Reset numUpdates
            numUpdates = 0;

            // Remove the creature from the old region lists
            // Update the region lists
            foreach (List<int> region in regions)
                region.RemoveAt(indexOfCurrentCreature);

            // If the creature has already completed both trials, we need to _actually_ reset the simulator.
            #region Planter XML writing
            if (GenomeIndexOfCurrentCreature != -1)
            {
                // First, check to see if the individual was a valid planter
                if (freezeAfterPlanting)
                {
                    // Success on both trials
                    if (plantedInColoredSpace1 && plantedInColoredSpace2)
                    {
                        numBidirectionalPlanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_BidirectionalPlanterGenome" + numBidirectionalPlanters.ToString() + ".xml");
                    }

                    // Success on the first trial
                    else if (plantedInColoredSpace1)
                    {
                        numFirstTrialPlanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_FirstTrialPlanterGenome" + numFirstTrialPlanters.ToString() + ".xml");
                    }

                    // Sucess on the second trial
                    else if (plantedInColoredSpace2)
                    {
                        numSecondTrialPlanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_SecondTrialPlanterGenome" + numSecondTrialPlanters.ToString() + ".xml");
                    }
                }

                else
                {
                    // Success on both trials, no misplanting
                    if (plantedInColoredSpace1 && plantedInColoredSpace2 && !plantedInWhiteSpace1 && !plantedInWhiteSpace2)
                    {
                        numBidirectionalPlanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_BidirectionalPlanterGenome" + numBidirectionalPlanters.ToString() + ".xml");
                    }

                    // Planted only in white
                    // Success on the first trial, no misplanting
                    else if (!plantedInColoredSpace1 && plantedInWhiteSpace1)
                    {
                        numFirstTrialPlanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_FirstTrialPlanterGenome_White" + numFirstTrialPlanters.ToString() + ".xml");
                    }

                    // Success on the first trial, no misplanting
                    else if (plantedInColoredSpace1 && !plantedInWhiteSpace1)
                    {
                        numFirstTrialPlanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_FirstTrialPlanterGenome" + numFirstTrialPlanters.ToString() + ".xml");
                    }

                    // Sucess on the second trial, no misplanting
                    else if (plantedInColoredSpace2 && !plantedInWhiteSpace2)
                    {
                        numSecondTrialPlanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_SecondTrialPlanterGenome" + numSecondTrialPlanters.ToString() + ".xml");
                    }

                    // Success on both trials, but with misplanting
                    if ((plantedInColoredSpace1 && plantedInWhiteSpace1) || (plantedInColoredSpace2 && plantedInWhiteSpace2))
                    {
                        numBidirectionalMisplanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_BidirectionalPlanterGenome_Misplanted" + numBidirectionalMisplanters.ToString() + ".xml");
                    }

                    // Success on the first trial, but with misplanting
                    else if (plantedInColoredSpace1 && plantedInWhiteSpace1)
                    {
                        numFirstTrialMisplanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_FirstTrialPlanterGenome_Misplanted" + numFirstTrialMisplanters.ToString() + ".xml");
                    }

                    // Sucess on the second trial, but with misplanting
                    else if (plantedInColoredSpace2 && plantedInWhiteSpace2)
                    {
                        numSecondTrialMisplanters++;
                        XmlDocument results = new XmlDocument();
                        XmlDeclaration declaration = results.CreateXmlDeclaration("1.0", null, null);
                        results.AppendChild(declaration);
                        XmlGenomeWriterStatic.Write(results, (NeatGenome)ea.Population.GenomeList[GenomeIndexOfCurrentCreature]);
                        results.Save(plantersFolder + "NoveltySearch_SecondTrialPlanterGenome_Misplanted" + numSecondTrialMisplanters.ToString() + ".xml");
                    }
                }
            }
            #endregion

            // Remove the individual from the global image list
            Components.Remove(currentCreature);

            // Reset the success flags
            plantedInColoredSpace1 = false;
            plantedInWhiteSpace1 = false;
            plantedInColoredSpace2 = false;
            plantedInWhiteSpace2 = false;

            // Also reset the trial flag
            firstTrial = true;

            // Get the next creature for this generation, decode its CPPN, and replace the creature's controller
            GenomeIndexOfCurrentCreature++;

            // Update the folders for storing the archive and planters if necessary
            if (GenomeIndexOfCurrentCreature % numCreaturesPerFolder == 0)
            {
                int newFolderNumber = GenomeIndexOfCurrentCreature / numCreaturesPerFolder;
                noveltyLogsFolder = Directory.GetCurrentDirectory() + "\\archive\\" + newFolderNumber + "\\";
                if (!Directory.Exists(noveltyLogsFolder))
                    Directory.CreateDirectory(noveltyLogsFolder);
                plantersFolder = Directory.GetCurrentDirectory() + "\\planters\\" + newFolderNumber + "\\";
                if (!Directory.Exists(plantersFolder))
                    Directory.CreateDirectory(plantersFolder);
            }

            // Initialize the genome's behavior characterization structure if this genome has not previously been evaluated
            if (ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior == null)
            {
                ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior = new BehaviorType();
                ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Behavior.behaviorList = new List<double>();
            }
            INetwork newController = controllerSubstrate.generateGenome(ea.Population.GenomeList[GenomeIndexOfCurrentCreature].Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"))).Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            if (bidirectionalTrials)
                currentCreature = new NNControlledCreature(morphology, initialBoardWidth / 2, initialBoardHeight / 2, initialHeading + (float)(Math.PI / 2.0), newController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting);
            else
                currentCreature = new NNControlledCreature(morphology, initialBoardWidth / 2, initialBoardHeight / 2, initialHeading, newController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting);

            // Add the creature back into the appropriate region lists
            RotationPacket rp = currentCreature.getRotationPacket();
            regions[rp.NWCoord.Y / regionHeight, rp.NWCoord.X / regionWidth].Add(indexOfCurrentCreature);
            if ((rp.NWCoord.X % regionWidth > rp.SECoord.X % regionWidth) && (rp.NWCoord.Y % regionHeight > rp.SECoord.Y % regionHeight))
                regions[rp.SECoord.Y / regionHeight, rp.SECoord.X / regionWidth].Add(indexOfCurrentCreature);
            if (rp.NWCoord.X % regionWidth > rp.SECoord.X % regionWidth)
                regions[(rp.NWCoord.Y / regionHeight), rp.SECoord.X / regionWidth].Add(indexOfCurrentCreature);
            if (rp.NWCoord.Y % regionHeight > rp.SECoord.Y % regionHeight)
                regions[rp.SECoord.Y / regionHeight, rp.SECoord.X / regionWidth].Add(indexOfCurrentCreature);
        }
    }
}
