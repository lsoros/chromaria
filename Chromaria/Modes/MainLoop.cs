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

namespace Chromaria.Modes
{
    class MainLoop : Simulator
    {
        #region Instance Variables
        List<int> parentList;
        List<int> removalQueue;
        int currentParentIndex;
        int dequeuedCreatureID;
        NNControlledCreature currentCreature;
        Texture2D initialMorphology;
        public static int numUpdates;
        int indexOfDequeuedCreature;
        EvolutionAlgorithm controllerEA;
        EvolutionAlgorithm morphologyEA;
		NeatParameters controllerEAparams;
		NeatParameters morphologyEAparams;
        string logsFolderRoot;
		int numOffspringThisParent;
        #endregion

        /// <summary>
        /// Default constructor. All logic is in the Simulator constructor.
        /// </summary>
        public MainLoop() : base() { }

        /// <summary>
        /// This function contains all of the pre-run logic that doesn't involve graphics.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Configure the HyperNEAT substrate
            controllerSubstrate = new ControllerSubstrate(308, 4, 108, new BipolarSigmoid());
            controllerSubstrate.weightRange = 5.0;
            controllerSubstrate.threshold = 0.2;

            // Configure the NEAT parameters
            controllerEAparams = new NeatParameters();
            controllerEAparams.notGenerational = true;
            controllerEAparams.recurrenceDisabled = false;
            controllerEAparams.actFunDictionary = Simulator.controllerCPPNactFuns;
            controllerEAparams.pMutateConnectionWeights = Simulator.pMutConnectionWeight;
            controllerEAparams.pMutateAddNode = Simulator.pAddNode;
            controllerEAparams.pMutateDeleteSimpleNeuron = Simulator.pDeleteSimpleNeuron;
            controllerEAparams.pMutateAddModule = Simulator.pAddModule;
            controllerEAparams.pMutateAddConnection = Simulator.pAddConnection;
            controllerEAparams.pMutateDeleteConnection = Simulator.pDeleteConnection;

            morphologyEAparams = new NeatParameters();
            morphologyEAparams.notGenerational = true;
            morphologyEAparams.recurrenceDisabled = true;
            morphologyEAparams.actFunDictionary = Simulator.morphologyCPPNactFuns;
            morphologyEAparams.pMutateConnectionWeights = Simulator.pMutConnectionWeight;
            morphologyEAparams.pMutateAddNode = Simulator.pAddNode;
            morphologyEAparams.pMutateDeleteSimpleNeuron = Simulator.pDeleteSimpleNeuron;
            morphologyEAparams.pMutateAddModule = Simulator.pAddModule;
            morphologyEAparams.pMutateAddConnection = Simulator.pAddConnection;
            morphologyEAparams.pMutateDeleteConnection = Simulator.pDeleteConnection;

            // Create the logs folder if it doesn't already exist
            if (!replayRun)
            {
                snapshotFolderNumber = 0;
                logsFolderRoot = Directory.GetCurrentDirectory() + "\\" + "logs-" + startTime.ToString() + "\\";
                logsFolder = logsFolderRoot + "0\\";
                if (!Directory.Exists(logsFolder))
                    Directory.CreateDirectory(logsFolder);

                // Create a log with some basic info about the initial state of the simulation
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                {
                    file.WriteLine("Open-ended evolution run");
                    file.WriteLine("Initial morphology genome XML filename: " + initialMorphologyFilename);
                    file.WriteLine("Initial controller genome XML filename: " + initialControllerFilename + "\n");
                }
            }
            else
            {
                logsFolderRoot = logsFolder;
                logsFolder = logsFolderRoot + snapshotFolderNumber.ToString() + "\\";
            }

            // Initialize the population lists
            parentList = new List<int>();
            removalQueue = new List<int>();

            // Generate the initial creature, who is born at the center of the world, facing north
            // Load the initial morphology CPPN
            if (replayRun)
            {
                if (File.Exists(logsFolder + "Components.txt"))
                {
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(logsFolder + "Components.txt"))
                        // The first line is the current creature's ID
                        Simulator.nextGenomeID = Convert.ToInt32(reader.ReadLine());
                }
                else
                    throw new Exception(logsFolder + "Components.txt not found");

                // Then go find the genome corresponding to that ID
                int folderNumber = (Simulator.nextGenomeID - 1) / numCreaturesPerFolder;
                initialMorphologyFilename = logsFolderRoot + folderNumber.ToString() + "\\" + morphologyXMLprefix + Simulator.nextGenomeID.ToString() + ".xml";
            }

            NeatGenome morphologyCPPNGenome = loadCPPNFromXml(initialMorphologyFilename);
            INetwork morphologyCPPN = morphologyCPPNGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            initialMorphology = generateMorphology(morphologyCPPN);

            redTexture = generateSolidMorphology(initialMorphology);

            // Initialize the world (which depends on the initial morphology)
            InitializeRegions();

            if (!replayRun)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                {
                    file.WriteLine("Controller substrate weight range: " + controllerSubstrate.weightRange);
                    file.WriteLine("Controller substrate threshold: " + controllerSubstrate.threshold);
                }
            }

            if (!loadBackgroundOnly)
            {
                // Initialize pointer to current Chromarian
                currentParentIndex = 0;
                if (replayRun)
                {
                    Simulator.nextGenomeID = numCreaturesPerFolder * snapshotFolderNumber + 1;
                    ReadListsFromFiles();
                }

                // Load the initial controller CPPN
                if (replayRun)
                {
                    int folderNumber = (Simulator.nextGenomeID - 1) / numCreaturesPerFolder;
                    initialControllerFilename = logsFolderRoot + folderNumber.ToString() + "\\" + controllerXMLprefix + Simulator.nextGenomeID.ToString() + ".xml";
                }
                NeatGenome controllerCPPNGenome = loadCPPNFromXml(initialControllerFilename);
                INetwork controllerCPPN = controllerCPPNGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
                IGenome controllerGenome = controllerSubstrate.generateGenome(controllerCPPN);
                INetwork generatedController = controllerGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));

                // Generate the intial creature and place it in the world
                int x = initialBoardWidth / 2;
                int y = initialBoardHeight / 2;
                if (!replayRun)
                    currentCreature = new NNControlledCreature(initialMorphology, x, y, initialHeading, generatedController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting, controllerCPPNGenome, morphologyCPPNGenome);
                indexOfCurrentCreature = Components.Count - 1;
                regions[y / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
                if ((x % regionWidth > (x + initialMorphology.Width) % regionWidth) && (y % regionHeight > (y + initialMorphology.Height) % regionHeight) && !regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                    regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                if (x % regionWidth > (x + initialMorphology.Width) % regionWidth && !regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                    regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                if (y % regionHeight > (y + initialMorphology.Height) % regionHeight && !(regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Contains(indexOfCurrentCreature)))
                    regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                {
                    file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : first creature generated from initial seed");
                }

                // Add the initial creature to the parent list
                if (!replayRun)
                    parentList.Add(indexOfCurrentCreature);

                // Preliminarily update the creature's sensors so its first movements are actually based on what's underneath its starting position
                currentCreature.InitializeSensor();
                currentCreature.Genome.ControllerCPPNGenome.Behavior = new BehaviorType();
                currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList = new List<double>();

                // Initialize the offspring viability counter
                // (The initial value doesn't really matter because the seed is guaranteed to plant.)
                numOffspringThisParent = 0;

                // Initialize the update/timestep counter. maxTimeSteps is defined in the Simulator class
                numUpdates = 0;

                // Initialize the evolutionary algorithm (EA) for the controllers 
                // The EA here is only being used for lookup tables, etc.; we aren't maintaining a population or using generational evaluations
                uint nextGenomeID = controllerCPPNGenome.GenomeId + 1;
                uint nextInnovationID = 0;
                foreach (NeuronGene gene in controllerCPPNGenome.NeuronGeneList)
                {
                    if (gene.InnovationId > nextInnovationID)
                        nextInnovationID = gene.InnovationId;
                }
                foreach (ConnectionGene gene in controllerCPPNGenome.ConnectionGeneList)
                {
                    if (gene.InnovationId > nextInnovationID)
                        nextInnovationID = gene.InnovationId;
                }
                foreach (ModuleGene gene in controllerCPPNGenome.ModuleGeneList)
                {
                    if (gene.InnovationId > nextInnovationID)
                        nextInnovationID = gene.InnovationId;
                }
                nextInnovationID++;
                controllerEA = new EvolutionAlgorithm(null, null, controllerEAparams, nextGenomeID, nextInnovationID);

                // Initialize the evolutionary algorithm (EA) for the morphologies
                // The EA here is only being used for lookup tables, etc.; we aren't maintaining a population or using generational evaluations
                nextGenomeID = morphologyCPPNGenome.GenomeId + 1;
                nextInnovationID = 0;
                foreach (NeuronGene gene in morphologyCPPNGenome.NeuronGeneList)
                {
                    if (gene.InnovationId > nextInnovationID)
                        nextInnovationID = gene.InnovationId;
                }
                foreach (ConnectionGene gene in morphologyCPPNGenome.ConnectionGeneList)
                {
                    if (gene.InnovationId > nextInnovationID)
                        nextInnovationID = gene.InnovationId;
                }
                foreach (ModuleGene gene in morphologyCPPNGenome.ModuleGeneList)
                {
                    if (gene.InnovationId > nextInnovationID)
                        nextInnovationID = gene.InnovationId;
                }
                nextInnovationID++;
                morphologyEA = new EvolutionAlgorithm(null, null, morphologyEAparams, nextGenomeID, nextInnovationID);

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                {
                    file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : begin evolution");
                }

                if (!replayRun)
                {
                    // Write the initial morphology genome to the log folder
                    XmlDocument morphologyGenomeXML = new XmlDocument();
                    XmlDeclaration declaration = morphologyGenomeXML.CreateXmlDeclaration("1.0", null, null);
                    morphologyGenomeXML.AppendChild(declaration);
                    XmlGenomeWriterStatic.Write(morphologyGenomeXML, morphologyCPPNGenome);
                    morphologyGenomeXML.Save(logsFolder + "MorphologyGenome" + currentCreature.ID + ".xml");

                    // Write the initial controller genome to the log folder
                    XmlDocument controllerGenomeXML = new XmlDocument();
                    XmlDeclaration declaration2 = controllerGenomeXML.CreateXmlDeclaration("1.0", null, null);
                    controllerGenomeXML.AppendChild(declaration2);
                    XmlGenomeWriterStatic.Write(controllerGenomeXML, controllerCPPNGenome);
                    controllerGenomeXML.Save(logsFolder + "ControllerGenome" + currentCreature.ID + ".xml");

                    // Also write parent list, removal queue, and components list files. 
                    // These files will be mostly empty, but snapshot loading will fail if 
                    // they aren't present.
                    WriteListsToFiles(currentCreature.ID);
                }
            }
        }

        private void InitializeRegions()
        {
            // Regardless of whether or not we're replaying or starting from scratch, handle the blindcreatures scenario
            if (blindCreatures)
            {
                if (stripedBackground)
                    Simulator.initialBackground = new StaticImage("Background", 0, 0, generateStripedBackground(true, true, true, true), this);
                else
                    Simulator.initialBackground = new StaticImage("Background", 0, 0, generateSeededBackground(initialMorphology, true, true, true, true), this);
                regions[0, 0].Add(Components.Count - 1);
            }
			if (replayRun)
			{
				string backgroundFileName = (logsFolder + snapshotFileName);
				backgroundImage = new StaticImage("Background", 0, 0, ReadBackgroundFromFile(backgroundFileName), this);
			}
            else
            {
				if (stripedBackground)
					backgroundImage = new StaticImage ("Background", 0, 0, generateStripedBackground (true, true, true, true), this);
				else if (autoGenerateBackground)
					backgroundImage = new StaticImage ("Background", 0, 0, generateSeededBackground(initialMorphology, true, true, true, true), this);
                else
                    backgroundImage = new StaticImage("Background", 0, 0, ReadBackgroundFromFile(initialBackgroundFilename), this);

                WriteBackgroundToFile(logsFolder + snapshotFileName);
            }
            regions[0, 0].Add(Components.Count - 1);
        }

        /// <summary>
        /// This function loads any external graphics into the simulator via the XNA content pipeline. 
        /// Note: This project uses monogame insteadof XNA, but external graphics must be pre-compiled 
        /// using XNA (not monogame) in order to be loaded this way.
        /// </summary>
        protected override void LoadContent() { base.LoadContent(); }

        // Main recurring update loop
        protected override void Update (GameTime gameTime)
		{
			if (!loadBackgroundOnly) {
				// The following actions occur within base.Update()->NNControlledCreature.Update():
				// The Chromarian's sensors are updated
				// The Chromarian's neural network is activated, which may result in a planting attempt
				base.Update (gameTime);
                if (!paused)
                {
                    // First, if this is a new creature, check to make sure it meets the minimum size requirement. 
                    // (Creatures that don't meet the minimum size requirement should be removed immediately so as
                    // to not waste simulation time.)
                    if (numUpdates == 0 && (currentCreature.countNontransparentPixels() < minCreatureSize))
                    {
                        // Write to the external log
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                            file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : creature " + currentCreature.ID + " did not meet the minimum size requirement.");

                        // Also remove its body from the world
                        Components.RemoveAt(indexOfCurrentCreature);

                        // Reset the simulator state and load the next creature into the world
                        if (numOffspringThisParent == Simulator.numOffspringAttempts)
                        {
                            currentParentIndex++;
                            if (currentParentIndex > (parentList.Count - 1))
                                currentParentIndex = 0;
                            numOffspringThisParent = 0;
                        }
                        currentCreature = generateCreatureFrom((NNControlledCreature)Components[parentList[currentParentIndex]], gameTime);
                        currentCreature.Genome.ControllerCPPNGenome.Behavior = new BehaviorType();
                        currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList = new List<double>();
                        numOffspringThisParent++;

                        // Add the creature to the appropriate regions
                        int x = (int)currentCreature.Position.X;
                        int y = (int)currentCreature.Position.Y;

                        regions[y / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
                        if ((x % regionWidth > (x + initialMorphology.Width) % regionWidth) && (y % regionHeight > (y + initialMorphology.Height) % regionHeight) && !regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                            regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                        if (x % regionWidth > (x + initialMorphology.Width) % regionWidth && !regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                            regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                        if (y % regionHeight > (y + initialMorphology.Height) % regionHeight && !regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Contains(indexOfCurrentCreature))
                            regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
                        return;
                    }

                    // There are 3 stopping conditions:
                    // 1) successful planting attempt
                    // 2) unsuccessful planting attempt
                    // 3) timeout
                    // Either way, we need to generate a new offspring to evaluate.
                    else if (currentCreature.currentState.Equals(State.Planting))
                    {
                        if (currentCreature.isAtValidPlantingLocation() || everyoneCanPlant)
                        {
                            // If such a planting attempt occurred and was successful, the Chromarian generates an offspring.
                            // Stop the current creature (but keep its body in the world)
                            currentCreature.freeze();

                            // Get ready for the background to update
                            updateBackground = true;
                            planterRotationPacket = currentCreature.getRotationPacketWithoutSensors(true);

                            backgroundImage.Update(gameTime);

                            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                            {
                                file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : creature " + currentCreature.ID + " planted successfully");
                            }

                            // Add the current (successful) creature to the population list PRECEDING its parent
                            if (parentList[currentParentIndex] != indexOfCurrentCreature)
                                parentList.Insert(currentParentIndex, indexOfCurrentCreature); // TEST THIS
                            currentParentIndex += 2;
                            if (currentParentIndex > (parentList.Count - 1))
                                currentParentIndex = 0;
                            numOffspringThisParent = 0;

                            // Also add the new offspring to the end of the removal queue
                            removalQueue.Add(indexOfCurrentCreature);

                            // Reset the simulator state and load the next creature into the world
                            numUpdates = 0;
                            currentCreature = generateCreatureFrom((NNControlledCreature)Components[parentList[currentParentIndex]], gameTime);
                            currentCreature.Genome.ControllerCPPNGenome.Behavior = new BehaviorType();
                            currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList = new List<double>();

                            int x = (int)currentCreature.Position.X;
                            int y = (int)currentCreature.Position.Y;

                            regions[y / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
                            if ((x % regionWidth > (x + initialMorphology.Width) % regionWidth) && (y % regionHeight > (y + initialMorphology.Height) % regionHeight) && !regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                            if (x % regionWidth > (x + initialMorphology.Width) % regionWidth && !regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                            if (y % regionHeight > (y + initialMorphology.Height) % regionHeight && !regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);

                            // If the list is full, remove the oldest member
                            if (parentList.Count == maxPopulationListSize)
                            {
                                // Remove the creature from the population list
                                indexOfDequeuedCreature = removalQueue[0];
                                dequeuedCreatureID = ((Creature)(Components[indexOfDequeuedCreature])).ID;
                                removalQueue.RemoveAt(0);
                                parentList.Remove(indexOfDequeuedCreature);
                                if (currentParentIndex > (parentList.Count - 1))
                                    currentParentIndex = parentList.Count - 1;

                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                                {
                                    file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : creature " + dequeuedCreatureID + " was dequeued");
                                }

                                // Also remove its body from the world
                                Components.RemoveAt(indexOfDequeuedCreature);

                                // Adjust the pointers in the parent, removal, and region lists to account for the changed index into the components list
                                for (int i = 0; i < parentList.Count; i++)
                                {
                                    if (parentList[i] >= indexOfDequeuedCreature)
                                        parentList[i]--;
                                }

                                for (int i = 0; i < removalQueue.Count; i++)
                                {
                                    if (removalQueue[i] >= indexOfDequeuedCreature)
                                        removalQueue[i]--;
                                }

                                foreach (List<int> region in regions)
                                {
                                    for (int i = 0; i < region.Count; i++)
                                    {
                                        if (region[i] >= indexOfDequeuedCreature)
                                            region[i]--;
                                    }
                                }

                                if (indexOfCurrentCreature > indexOfDequeuedCreature)
                                    indexOfCurrentCreature--;
                            }
                        }

                        else 
                        {
                            // Otherwise (if an unsuccessful planting attempt occurred), it does not get added to the list
                            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                            {
                                file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : creature " + currentCreature.ID + " planted invalidly and was removed");
                            }

                            // Also remove its body from the world
                            Components.RemoveAt(indexOfCurrentCreature);


                            // Reset the simulator state and load the next creature into the world
                            numUpdates = 0;
                            if (numOffspringThisParent == numOffspringAttempts)
                            {
                                currentParentIndex++;
                                if (currentParentIndex > (parentList.Count - 1))
                                    currentParentIndex = 0;
                                numOffspringThisParent = 0;
                            }
                            currentCreature = generateCreatureFrom((NNControlledCreature)Components[parentList[currentParentIndex]], gameTime);
                            currentCreature.Genome.ControllerCPPNGenome.Behavior = new BehaviorType();
                            currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList = new List<double>();
                            numOffspringThisParent++;

                            int x = (int)currentCreature.Position.X;
                            int y = (int)currentCreature.Position.Y;

                            regions[y / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
                            if ((x % regionWidth > (x + initialMorphology.Width) % regionWidth) && (y % regionHeight > (y + initialMorphology.Height) % regionHeight) && !regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                            if (x % regionWidth > (x + initialMorphology.Width) % regionWidth && !regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                            if (y % regionHeight > (y + initialMorphology.Height) % regionHeight && !regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
                            return;
                        }
                    }

                    else if (numUpdates == maxTimeSteps)
                    {
                        if (everyoneCanPlant)
                        {
                            currentCreature.freeze();

                            // In this case, the creature should be added to the parent queue but not have its body kept in the world
                            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                            {
                                file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : creature " + currentCreature.ID + " timed out but generated an offspring");
                            }

                            // Add the current (successful) creature to the population list PRECEDING its parent
                            if (parentList[currentParentIndex] != indexOfCurrentCreature)
                                parentList.Insert(currentParentIndex, indexOfCurrentCreature); // TEST THIS
                            currentParentIndex += 2;
                            if (currentParentIndex > (parentList.Count - 1))
                                currentParentIndex = 0;
                            numOffspringThisParent = 0;

                            // Also add the new offspring to the end of the removal queue
                            removalQueue.Add(indexOfCurrentCreature);

                            // Reset the simulator state and load the next creature into the world
                            numUpdates = 0;
                            currentCreature = generateCreatureFrom((NNControlledCreature)Components[parentList[currentParentIndex]], gameTime);
                            currentCreature.Genome.ControllerCPPNGenome.Behavior = new BehaviorType();
                            currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList = new List<double>();

                            int x = (int)currentCreature.Position.X;
                            int y = (int)currentCreature.Position.Y;

                            regions[y / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
                            if ((x % regionWidth > (x + initialMorphology.Width) % regionWidth) && (y % regionHeight > (y + initialMorphology.Height) % regionHeight) && !regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                            if (x % regionWidth > (x + initialMorphology.Width) % regionWidth && !regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                            if (y % regionHeight > (y + initialMorphology.Height) % regionHeight && !regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);

                            // If the list is full, remove the oldest member
                            if (parentList.Count == maxPopulationListSize)
                            {
                                // Remove the creature from the population list
                                indexOfDequeuedCreature = removalQueue[0];
                                dequeuedCreatureID = ((Creature)(Components[indexOfDequeuedCreature])).ID;
                                removalQueue.RemoveAt(0);
                                parentList.Remove(indexOfDequeuedCreature);
                                if (currentParentIndex > (parentList.Count - 1))
                                    currentParentIndex = parentList.Count - 1;

                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                                {
                                    file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : creature " + dequeuedCreatureID + " was dequeued");
                                }

                                // Also remove its body from the world
                                Components.RemoveAt(indexOfDequeuedCreature);

                                // Adjust the pointers in the parent, removal, and region lists to account for the changed index into the components list
                                for (int i = 0; i < parentList.Count; i++)
                                {
                                    if (parentList[i] >= indexOfDequeuedCreature)
                                        parentList[i]--;
                                }

                                for (int i = 0; i < removalQueue.Count; i++)
                                {
                                    if (removalQueue[i] >= indexOfDequeuedCreature)
                                        removalQueue[i]--;
                                }

                                foreach (List<int> region in regions)
                                {
                                    for (int i = 0; i < region.Count; i++)
                                    {
                                        if (region[i] >= indexOfDequeuedCreature)
                                            region[i]--;
                                    }
                                }

                                if (indexOfCurrentCreature > indexOfDequeuedCreature)
                                    indexOfCurrentCreature--;
                            }
                        }

                        else
                        {
                            // If the creature has timed out, generate a new currentCreature
                            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
                            {
                                file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : creature " + currentCreature.ID + " timed out");
                            }

                            // Reset the simulator state and generate a new offspring from the next eligible parent
                            numUpdates = 0;
                            if (numOffspringThisParent == numOffspringAttempts)
                            {
                                currentParentIndex++;
                                if (currentParentIndex > (parentList.Count - 1))
                                    currentParentIndex = 0;
                                numOffspringThisParent = 0;
                            }
                            Components.RemoveAt(indexOfCurrentCreature);
                            currentCreature = generateCreatureFrom((NNControlledCreature)Components[parentList[currentParentIndex]], gameTime);
                            currentCreature.Genome.ControllerCPPNGenome.Behavior = new BehaviorType();
                            currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList = new List<double>();
                            numOffspringThisParent++;

                            int x = (int)currentCreature.Position.X;
                            int y = (int)currentCreature.Position.Y;

                            regions[y / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
                            if ((x % regionWidth > (x + initialMorphology.Width) % regionWidth) && (y % regionHeight > (y + initialMorphology.Height) % regionHeight) && !regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y + initialMorphology.Height) / regionHeight, (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                            if (x % regionWidth > (x + initialMorphology.Width) % regionWidth && !regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y / regionHeight), (x + initialMorphology.Width) / regionWidth].Add(indexOfCurrentCreature);
                            if (y % regionHeight > (y + initialMorphology.Height) % regionHeight && !regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Contains(indexOfCurrentCreature))
                                regions[(y + initialMorphology.Height) / regionHeight, x / regionWidth].Add(indexOfCurrentCreature);
                            return;
                        }
                    }
                    else
                    {
                        // Otherwise the creature continues to move in the world as long as it hasn't timed out
                        numUpdates++;

                        // Update the creature's behavior log
                        currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList.Add(currentCreature.Position.X);
                        currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList.Add(currentCreature.Position.Y);
                        currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList.Add(currentCreature.Heading);

                        // If the creature has planted itself, decide whether or not it planted itself in a valid position
                        // and tidy up some other business. 
                        if (currentCreature.currentState.Equals(State.Planting))
                            currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList.Add(1.0);
                        else
                            currentCreature.Genome.ControllerCPPNGenome.Behavior.behaviorList.Add(0.0);
                    }
                }
			}
		}

        private NNControlledCreature generateCreatureFrom(NNControlledCreature parent, GameTime gameTime = null)
        {
            // Remove the old creature from the regions so that it can't be sensed anymore
            foreach (List<int> region in regions)
            {
                region.Remove(indexOfCurrentCreature);
                if ((!Simulator.blindCreatures && region.Count > 1) || (Simulator.blindCreatures && region.Count > 2))
                    throw new Exception("More than one creature remaining in the region.");
            }
            currentCreature.Texture.Dispose();

            if (replayRun && (nextGenomeID == maxNumGenomesGenerated))
            {
                paused = true;
            }

            // Generate an offspring
            NeatGenome newMorphologyCPPNGenome;
            if(replayRun)
                newMorphologyCPPNGenome = loadCPPNFromXml(logsFolder + morphologyXMLprefix + nextGenomeID + ".xml");
            else
                newMorphologyCPPNGenome = (NeatGenome)parent.Genome.MorphologyCPPNGenome.CreateOffspring_Asexual(morphologyEA);
            INetwork newMorphologyCPPN = newMorphologyCPPNGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            Texture2D newMorphology = generateMorphology(newMorphologyCPPN);

            NeatGenome newControllerCPPNGenome;
            if(replayRun)
                newControllerCPPNGenome = loadCPPNFromXml(logsFolder + controllerXMLprefix + nextGenomeID + ".xml");
            else
                newControllerCPPNGenome = (NeatGenome)parent.Genome.ControllerCPPNGenome.CreateOffspring_Asexual(controllerEA);
            INetwork newControllerCPPN = newControllerCPPNGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            IGenome controllerGenome = controllerSubstrate.generateGenome(newControllerCPPN);
            INetwork generatedController = controllerGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
            
            NNControlledCreature offspring;
            if (startFromCenter)
                offspring = new NNControlledCreature(newMorphology, initialBoardWidth / 2, initialBoardHeight / 2, initialHeading, generatedController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting, newControllerCPPNGenome, newMorphologyCPPNGenome, gameTime);
            else
            {
				
                float newX = parent.Position.X;
                float newY = parent.Position.Y;
				/*
                if (parent.Position.X < 200)
                    newX = parent.Position.X + 20;
                else if (parent.Position.X + parent.Texture.Width > (initialBoardWidth - 200))
                    newX = parent.Position.X - 20;
                if (parent.Position.Y < 200)
                    newY = parent.Position.Y + 20;
                else if (parent.Position.Y + parent.Texture.Height > (initialBoardHeight - 200))
                    newY = parent.Position.Y - 20;
                    */
                offspring = new NNControlledCreature(newMorphology, newX, newY, (float)parent.XNAHeading, generatedController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting, newControllerCPPNGenome, newMorphologyCPPNGenome, gameTime);
            }
            if (!replayRun && (offspring.ID == maxNumGenomesGenerated))
                Exit();
            
            offspring.InitializeSensor();
            indexOfCurrentCreature = Components.Count - 1;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RunInfo.txt", true))
            {
                file.WriteLine(DateTime.Now.ToString("0:MM/dd/yy H:mm:ss tt") + " : creature " + parent.ID + " generated offspring " + offspring.ID);
            }

            if (!replayRun)
            {
                // Rewrite the ancestry info to a separate file
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "Ancestry.txt", true))
                {
                    file.WriteLine(offspring.ID + " " + parent.ID);
                }

                // Write the new morphology genome to the log folder
                XmlDocument morphologyGenomeXML = new XmlDocument();
                XmlDeclaration declaration = morphologyGenomeXML.CreateXmlDeclaration("1.0", null, null);
                morphologyGenomeXML.AppendChild(declaration);
                XmlGenomeWriterStatic.Write(morphologyGenomeXML, offspring.Genome.MorphologyCPPNGenome);
                morphologyGenomeXML.Save(logsFolder + "MorphologyGenome" + offspring.ID + ".xml");

                // Write the new controller genome to the log folder
                XmlDocument controllerGenomeXML = new XmlDocument();
                XmlDeclaration declaration2 = controllerGenomeXML.CreateXmlDeclaration("1.0", null, null);
                controllerGenomeXML.AppendChild(declaration2);
                XmlGenomeWriterStatic.Write(controllerGenomeXML, offspring.Genome.ControllerCPPNGenome);
                controllerGenomeXML.Save(logsFolder + "ControllerGenome" + offspring.ID + ".xml");
            }

            if(replayRun)
            {
                if (nextGenomeID % numCreaturesPerFolder == 0)
                {
                    int newFolderNumber = nextGenomeID / numCreaturesPerFolder;
                    logsFolder = logsFolderRoot + newFolderNumber + "\\";
                }
                nextGenomeID++;
            }

            // Update the log folders if necessary
            if (offspring.ID % numCreaturesPerFolder == 0 && !replayRun)
            {
                // Create a new folder to store the new set of genomes
                int newFolderNumber = offspring.ID / numCreaturesPerFolder;
                logsFolder = logsFolderRoot + newFolderNumber + "\\";
                if (!Directory.Exists(logsFolder))
                    Directory.CreateDirectory(logsFolder);

                // Take a snapshot of the current state of the world so the background is preserved
                WriteListsToFiles(offspring.ID);
                WriteBackgroundToFile(logsFolder + snapshotFileName);
            }

            return offspring;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        private void WriteListsToFiles(int offspringID)
        {
            // Write the parent list
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "ParentList.txt", true))
            {
                // Write the current parent
                file.WriteLine(currentParentIndex.ToString());

                // then the parent list
                foreach (int parentID in parentList)
                    file.WriteLine(parentID.ToString());
            }

            // Write the removal queue
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "RemovalQueue.txt", true))
            {
                for (int i = 0; i < removalQueue.Count; i++)
                    file.WriteLine(removalQueue[i].ToString());
            }

            // Write the components list
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "Components.txt", true))
            {
                // Write the ID of the next creature to evaluate
                file.WriteLine(offspringID.ToString());

                // Write the IDs of the rest of the components
                int startIndex = 1;
                if (blindCreatures)
                    startIndex = 2;

                for (int i = startIndex; i < Components.Count; i++)
                    file.WriteLine(((Creature)(Components[i])).ID.ToString());
            }
        }

        private void ReadListsFromFiles()
        {
            // Read the parent queue
            if (File.Exists(logsFolder + "ParentList.txt"))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(logsFolder + "ParentList.txt"))
                {
                    // Read the pointer to the current parent
                    currentParentIndex = Convert.ToInt32(reader.ReadLine());

                    // Then read the actual entries for the parent list
                    String nextLine;
                    while ((nextLine = reader.ReadLine()) != null)
                        parentList.Add(Convert.ToInt32(nextLine));
                }
            }
            else
                throw new Exception(logsFolder + "ParentList.txt not found.");

            // Read the removal queue
            if (File.Exists(logsFolder + "RemovalQueue.txt"))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(logsFolder + "RemovalQueue.txt"))
                {
                    String nextLine;
                    while ((nextLine = reader.ReadLine()) != null)
                        removalQueue.Add(Convert.ToInt32(nextLine));
                }
            }
            else
                throw new Exception(logsFolder + "RemovalQueue.txt not found.");

            // Read the components list and track down the genomes across the file hierarchy
            if (File.Exists(logsFolder + "Components.txt"))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(logsFolder + "Components.txt"))
                {
                    // The first line is the current creature's ID
                    int currentCreatureID = Convert.ToInt32(reader.ReadLine());

                    int genomeID, folderNumber;
                    String nextLine;
                    NeatGenome morphologyCPPNGenome, controllerCPPNGenome;
                    INetwork morphologyCPPN, controllerCPPN, generatedController;
                    IGenome controllerGenome;
                    Texture2D newMorphology;
                    NNControlledCreature newCreature;
                    while ((nextLine = reader.ReadLine()) != null)
                    {
                        // First get the ID of the morphology
                        genomeID = Convert.ToInt32(nextLine);

                        // Then go find the genome corresponding to that ID
                        folderNumber = (genomeID-1) / numCreaturesPerFolder;

                        // Load the creature's morphology CPPN
                        morphologyCPPNGenome = loadCPPNFromXml(logsFolderRoot + folderNumber.ToString() + "\\" + morphologyXMLprefix + genomeID.ToString() + ".xml");
                        morphologyCPPN = morphologyCPPNGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
                        newMorphology = generateMorphology(morphologyCPPN);

                        // Load the creature's controller CPPN
                        // TODO: copy behavior list from controller genome file to controller genome
                        // behaviorlist is null in controllerCPPNGenome
                        controllerCPPNGenome = loadCPPNFromXml(logsFolderRoot + folderNumber.ToString() + "\\" + controllerXMLprefix + genomeID.ToString() + ".xml");
                        controllerCPPN = controllerCPPNGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
                        controllerGenome = controllerSubstrate.generateGenome(controllerCPPN);
                        generatedController = controllerGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));

                        // Create a new creature, which will automatically add it to the components list
                        if(startFromCenter)
                            newCreature = new NNControlledCreature(newMorphology, initialBoardWidth / 2, initialBoardWidth / 2, 0.0f, generatedController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting, controllerCPPNGenome, morphologyCPPNGenome);
                        else
                            newCreature = new NNControlledCreature(newMorphology, (float)controllerCPPNGenome.Behavior.behaviorList[0], (float)controllerCPPNGenome.Behavior.behaviorList[1], (float)controllerCPPNGenome.Behavior.behaviorList[2], generatedController, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting, controllerCPPNGenome, morphologyCPPNGenome);
                        if (genomeID != currentCreatureID)
                            newCreature.freeze();
                        else
                            currentCreature = newCreature;
                    }
                }
            }
            else
                throw new Exception(logsFolder + "Components.txt not found.");

        }

        private void WriteBackgroundToFile(string fileName)
        {
            Color[] colorArray = backgroundImage.TextureAsColorArray;

            using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                for (int x = 0; x < initialBoardWidth; x++)
                {
                    for (int y = 0; y < initialBoardHeight; y++)
                    {
                        writer.Write(colorArray[x + (y * initialBoardWidth)].R);
                        writer.Write(colorArray[x + (y * initialBoardWidth)].G);
                        writer.Write(colorArray[x + (y * initialBoardWidth)].B);
                        writer.Write(colorArray[x + (y * initialBoardWidth)].A);
                    }
                }
            }
        }

        protected Texture2D ReadBackgroundFromFile(string fileName)
        {
            // TODO: account for dynamically-resized world
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