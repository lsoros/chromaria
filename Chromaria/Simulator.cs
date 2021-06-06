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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;


namespace Chromaria
{
    /// <summary>
    /// This is the main class for the environment simulator.
    /// It instantiates and manages the game engine, in addition 
    /// to arranging things in the world. It also instantiates
    /// the creatures that inhabit the world, though their functionality
    /// is defined in separate classes. 
    /// </summary>
    public class Simulator : Microsoft.Xna.Framework.Game
    {
        // This enumeration contains all of the possible agent states.
        public enum State
        {
            Moving, Planting
        }

        #region config (constants)
        // Experimental controls
        public static bool everyoneCanPlant = false;
        public static bool blindCreatures = false;

        // File reading and writing
        public static string initialMorphologyFilename;
        public static string initialControllerFilename;
        public static string currentGenomeFolder;
        public static int numCreaturesPerFolder;
        
        // World options
        public static bool startFromCenter = false;
        public static bool autoGenerateBackground = true;
        public static bool colorAllFourBorders = true;
        public static bool randomMorphology = false;
        public static bool stripedBackground = false;
        public static bool fixedSizeWorld = true;
        public static int initialBoardWidth;
        public static int initialBoardHeight;
        public static double maxDistance;
        public static int regionWidth;
        public static int regionHeight;
        public static int deltaXThreshold = regionWidth / 2;
        public static int deltaYThreshold = regionHeight / 2;
        public static float colorRatio;
        public static int borderThickness;
        public static float initialHeading; // 0.0f -> the creature will start facing north
        public static string initialBackgroundFilename;

        // Novelty search options
        public bool bidirectionalTrials = false;
        public static bool freezeAfterPlanting = false;
        public static int behaviorUpdateInterval;
        public static int populationSize;
        public static double plantingWeight; // Coefficients for novelty search weighting
        public static double positionWeight; // To make them equal, set both weights to be equal. (Default value is 1 for each.)
        public static double headingWeight;
        public static double archiveThreshold;
        public static bool fixedIndividuals = false;
        public static int numIndividuals;

        // Manual & debug options
        public static string debugString = "";
        public static string debugOutputFile;
        public static bool debugSensors = false; // Prints out the sensor inputs and the ANN outputs at each timestep
        public static bool debugSensorFieldRotation = false;
        public static bool depthTest = false;
        public static bool useRandomMorphology = false;
        public static int replayIndividualNumber;

        // Visualization options
        public static int numDimsPerRGBAxis;

        // Graphics options
        public static bool trackPlanting = false; // Colors the creature red whenever it attemps to plant
        public static Texture2D redTexture;
        public static bool outputPlantingSuccess = false; // when a plant attempt is made, output whether it was successful or not
        public static bool drawSensorField = false; // Visually draws the sensor field contents on the screen
        public static bool graphicsEnabled = true; // turn graphics off by pressing 'u' and back on with 'i'
        public static bool paused = true; // press 'p' to pause and 'o' to unpause

        // EA options
        public static bool useHarderPlantingFunction = true;
        public static float harderPlantingFunctionMultiplier;
        public static bool replayRun = false;
        public static int nextGenomeID = 2;
        public static string controllerXMLprefix;
        public static string morphologyXMLprefix;
        public static string logsFolder;
        public static string snapshotFileName;
        public static int snapshotFolderNumber;
        public static bool loadBackgroundOnly = false;
        public static bool analyzePlantingRatesOnly = false;
		public static double pMutConnectionWeight;
		public static double pAddNode;
		public static double pDeleteSimpleNeuron;
		public static double pAddModule;
		public static double pAddConnection;
		public static double pDeleteConnection;

        public static int maxPopulationListSize;
		public static int numOffspringAttempts;
        public static int maxTimeSteps; //was 150
        public static int maxNumGenomesGenerated;
        public static bool updateBackground = false;
        public static RotationPacket planterRotationPacket;

        // CPPN options
		public static Dictionary<String, double> controllerCPPNactFuns;
		public static Dictionary<String, double> morphologyCPPNactFuns;


        // Creature options
        public static int ROTATION_SPEED; // Basically, the number of degrees the sprite will rotate if one output is maximized and the other is minimized.
        public static int MOVEMENT_SPEED; // Movement speed for sprites when F is maximized. Manipulating this affects the width of the creatures' movement arcs, so if they want to move in a tight circle they have to slow down.
        public static int defaultNumSegments = 4; // the number of sensor segments, defining the shape of the sensor field (ex. if defaultNumSegments is 4, the sensor field will be square)
        public static int defaultNumSensors = 10; // the number of sensors per dimension. (ex. if defaultNumSensors is 10, the sensor field will contain 100 active sensors)
        public static double toleratedDifference; // max. matching score is 8.0; see Creature.isAtValidPlantingLocation()
        public static int pixelHeight;
        public static int minCreatureSize;
        #endregion

        #region instance variables
        public static int manuallyControlledCreatureID;    // This is used so only one creature's sensor field contents are drawn on the screen
        public static int indexOfCurrentCreature;
        public static Texture2D FieldTexture { get; set; } // These two structures are used
        public static Texture2D sensorContentsTexture;     // solely for visual debugging.
        public static List<int>[,] regions;
        public static SpriteBatch spriteBatch;
        GraphicsDeviceManager graphics;
        public static ControllerSubstrate controllerSubstrate; // HyperNEAT substrate
        public static float nextDepth;
        public static int nextImageID;
		public static long startTime;
        public static StaticImage initialBackground;
        public static StaticImage backgroundImage;
        #endregion 

        /// <summary>
        /// Simulator constructor. Sets graphics and content loading options, in addition 
        /// to reading parameters and initializing some of the other internal components.
        /// </summary>
        public Simulator()
        {
			// Initialize the cppn activation function probabilities lists
			controllerCPPNactFuns = new Dictionary<string, double>(15);
			morphologyCPPNactFuns = new Dictionary<string, double>(15);

            // Read the parameters from chromaria-params.txt
            loadParameters();

            // Initialize instance variables
            nextImageID = 0;
            nextDepth = 1.0f;
            maxDistance = Math.Sqrt(Math.Pow(0 - initialBoardWidth, 2) + Math.Pow(0 - initialBoardHeight, 2));

            // Set graphics options.
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferHeight = initialBoardHeight;
            graphics.PreferredBackBufferWidth = initialBoardWidth;

            // Set content options.
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Reads the parameters from an external file called chromaria-params.txt.
        /// </summary>
        private void loadParameters()
        {
            try
            {
                string line, subtoken;
                String[] splitString;
				bool readingControllerCPPNprobs = false;

                using (StreamReader sr = new StreamReader("chromaria-params.txt"))
                {
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine().Trim();
                        {
                            if (!(line.StartsWith("-") || line.StartsWith("(")))
                            {
                                // If the line starts with [, it is a boolean parameter
                                if (line.StartsWith("[x]"))
                                {
                                    if (line.Contains("Control 1"))
                                        everyoneCanPlant = true;
                                    else if (line.Contains("Control 2"))
                                        blindCreatures = true;
                                    else if (line.Contains("load static background only"))
                                        loadBackgroundOnly = true;
                                    else if (line.Contains("Analyze planting rates only"))
                                        analyzePlantingRatesOnly = true;
                                    else if (line.Contains("Fixed spawn point at center of world"))
                                        startFromCenter = true;
                                    else if (line.Contains("Load initial background from file"))
                                        autoGenerateBackground = false;
                                    else if (line.Contains("Grey striped background"))
                                        stripedBackground = true;
                                    else if (line.Contains("Fixed size world"))
                                        fixedSizeWorld = true;
                                    else if (line.Contains("Color all 4 borders of the world"))
                                        colorAllFourBorders = true;
                                    else if (line.Contains("Bidirectional trials"))
                                        bidirectionalTrials = true;
                                    else if (line.Contains("Freeze individuals after they attempt to plant"))
                                        freezeAfterPlanting = true;
                                    else if (line.Contains("Stop novelty search after"))
                                    {
                                        fixedIndividuals = true;
                                        foreach (String token in line.Split())
                                        {
                                            if (token.StartsWith("<"))
                                            {
                                                subtoken = token.Remove(0, 1);
                                                subtoken = subtoken.Remove(subtoken.Length - 1);
                                                numIndividuals = Convert.ToInt32(subtoken);
                                            }
                                        }
                                    }
                                    else if (line.Contains("Use stricter planting function"))
                                        useHarderPlantingFunction = true;
                                    else if (line.Contains("Overlay a red circle on a creature when it is planting"))
                                        trackPlanting = true;
                                    else if (line.Contains("Dummy morphology with a uniformly distributed color pattern"))
                                        useRandomMorphology = true;
                                    else if (line.Contains("Draw the sensor field and its contents onscreen"))
                                        drawSensorField = true;
                                    else if (line.Contains("Include overlapping dummy creatures in background"))
                                        depthTest = true;
                                    else if (line.Contains("Print out the sensor inputs and ANN outputs at each timestep to"))
                                    {
                                        trackPlanting = true;
                                        foreach (String token in line.Split())
                                        {
                                            if (token.StartsWith("<"))
                                            {
                                                subtoken = token.Remove(0, 1);
                                                subtoken = subtoken.Remove(subtoken.Length - 1);
                                                debugOutputFile = subtoken;
                                            }
                                        }
                                    }
                                    else if (line.Contains("Output planting attempt results to the console"))
                                        outputPlantingSuccess = true;
                                }

                                // Otherwise, it is a valued parameter following from a : separator
                                else
                                {
                                    if (line.StartsWith("Controller:"))
                                    {
                                        foreach (String token in line.Split())
                                        {
                                            if (token.StartsWith("<"))
                                            {
                                                subtoken = token.Remove(0, 1);
                                                subtoken = subtoken.Remove(subtoken.Length - 1);
                                                initialControllerFilename = Path.Combine("Seeds", subtoken);
                                            }
                                        }
                                    }
                                    else if (line.StartsWith("Morphology:"))
                                    {
                                        foreach (String token in line.Split())
                                        {
                                            if (token.StartsWith("<"))
                                            {
                                                subtoken = token.Remove(0, 1);
                                                subtoken = subtoken.Remove(subtoken.Length - 1);
                                                initialMorphologyFilename = Path.Combine("Seeds", subtoken);
                                            }
                                        }
                                    }
                                    else if (line.StartsWith("Initial background filename:"))
                                    {
                                        splitString = line.Split();
                                        subtoken = splitString[splitString.Length - 1];
                                        subtoken = subtoken.Remove(0, 1);
                                        initialBackgroundFilename = Path.Combine("Backgrounds", subtoken.Remove(subtoken.Length - 1));
                                    }
                                    else if (line.StartsWith("Initial world height:"))
                                    {
                                        splitString = line.Split();
                                        initialBoardHeight = Convert.ToInt32(splitString[splitString.Length - 2]);
                                        regionHeight = initialBoardHeight;
                                    }
                                    else if (line.StartsWith("Initial world width:"))
                                    {
                                        splitString = line.Split();
                                        initialBoardWidth = Convert.ToInt32(splitString[splitString.Length - 2]);
                                        regionWidth = initialBoardWidth;
                                    }
                                    else if (line.StartsWith("Region height:"))
                                    {
                                        splitString = line.Split();
                                        regionHeight = Convert.ToInt32(splitString[splitString.Length - 2]);
                                    }
                                    else if (line.StartsWith("Region width:"))
                                    {
                                        splitString = line.Split();
                                        regionWidth = Convert.ToInt32(splitString[splitString.Length - 2]);
                                    }
                                    else if (line.StartsWith("Color ratio:"))
                                    {
                                        splitString = line.Split();
                                        colorRatio = (float)Convert.ToDouble("0." + splitString[splitString.Length - 2]);
                                    }
                                    else if (line.StartsWith("Border thickness:"))
                                    {
                                        splitString = line.Split();
                                        borderThickness = Convert.ToInt32(splitString[splitString.Length - 2]);
                                    }
                                    else if (line.StartsWith("Max time steps:"))
                                    {
                                        splitString = line.Split();
                                        maxTimeSteps = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Behavior vector update interval:"))
                                    {
                                        splitString = line.Split();
                                        behaviorUpdateInterval = Convert.ToInt32(splitString[splitString.Length - 2]);
                                    }
                                    else if (line.StartsWith("Population size:"))
                                    {
                                        splitString = line.Split();
                                        populationSize = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Archive threshold:"))
                                    {
                                        splitString = line.Split();
                                        archiveThreshold = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Name of folder containing run logs and morphologies:"))
                                    {
                                        splitString = line.Split();
                                        subtoken = splitString[splitString.Length - 1];
                                        subtoken = subtoken.Remove(0, 1);
                                        subtoken = subtoken.Remove(subtoken.Length - 1);
                                        logsFolder = Directory.GetCurrentDirectory() + "\\" + subtoken + "\\";
                                    }
                                    else if (line.StartsWith("Max parent list size:"))
                                    {
                                        splitString = line.Split();
                                        maxPopulationListSize = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Number of allowed attempts to generate a viable offspring:"))
                                    {
                                        splitString = line.Split();
                                        numOffspringAttempts = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Max number of genomes generated:"))
                                    {
                                        splitString = line.Split();
                                        maxNumGenomesGenerated = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Minimum creature size:"))
                                    {
                                        splitString = line.Split();
                                        minCreatureSize = Convert.ToInt32(splitString[splitString.Length - 2]);
                                    }
                                    else if (line.StartsWith("Controller genome prefix:"))
                                    {
                                        splitString = line.Split();
                                        controllerXMLprefix = (splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Morphology genome prefix:"))
                                    {
                                        splitString = line.Split();
                                        morphologyXMLprefix = (splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Controller CPPN:"))
                                    {
                                        readingControllerCPPNprobs = true;
                                    }
                                    else if (line.StartsWith("Morphology CPPN:"))
                                    {
                                        readingControllerCPPNprobs = false;
                                    }
                                    else if (line.StartsWith("Bipolar sigmoid"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("BipolarSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("BipolarSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Error sign"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("ErrorSign", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("ErrorSign", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Gaussian"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("Gaussian", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("Gaussian", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Inverse absolute sigmoid"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("InverseAbsoluteSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("InverseAbsoluteSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Modulus"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("Modulus", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("Modulus", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Null function"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("NullFn", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("NullFn", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Plain sigmoid"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("PlainSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("PlainSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Reduced sigmoid"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("ReducedSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("ReducedSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Sigmoid approximation"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("SigmoidApproximation", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("SigmoidApproximation", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Sign function"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("Sign", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("Sign", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Sine"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("Sine", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("Sine", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Steepened sigmoid"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("SteepenedSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("SteepenedSigmoid", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Steepened sigmoid approximation"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("SteepenedSigmoidApproximation", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("SteepenedSigmoidApproximation", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Step function"))
                                    {
                                        splitString = line.Split();
                                        if (readingControllerCPPNprobs)
                                            controllerCPPNactFuns.Add("StepFunction", Convert.ToDouble(splitString[splitString.Length - 2]));
                                        else
                                            morphologyCPPNactFuns.Add("StepFunction", Convert.ToDouble(splitString[splitString.Length - 2]));
                                    }
                                    else if (line.StartsWith("Required distance from center"))
                                    {
                                        splitString = line.Split();
                                        harderPlantingFunctionMultiplier = (float)Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Initial heading"))
                                    {
                                        splitString = line.Split();
                                        initialHeading = (float)Convert.ToDouble(splitString[splitString.Length - 2]);
                                    }
                                    else if (line.StartsWith("Rotation speed:"))
                                    {
                                        splitString = line.Split();
                                        ROTATION_SPEED = Convert.ToInt32(splitString[splitString.Length - 2]);
                                    }
                                    else if (line.StartsWith("Movement speed:"))
                                    {
                                        splitString = line.Split();
                                        MOVEMENT_SPEED = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Number of sides on the sensor field"))
                                    {
                                        splitString = line.Split();
                                        defaultNumSegments = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Sensors per side:"))
                                    {
                                        splitString = line.Split();
                                        defaultNumSensors = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Tolerated difference between sensor field contents"))
                                    {
                                        splitString = line.Split();
                                        toleratedDifference = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Texture width"))
                                    {
                                        splitString = line.Split();
                                        pixelHeight = Convert.ToInt32(splitString[splitString.Length - 2]);
                                    }
                                    else if (line.StartsWith("Planting:"))
                                    {
                                        splitString = line.Split();
                                        plantingWeight = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Position:"))
                                    {
                                        splitString = line.Split();
                                        positionWeight = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Heading:"))
                                    {
                                        splitString = line.Split();
                                        headingWeight = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Number of creatures per folder:"))
                                    {
                                        splitString = line.Split();
                                        numCreaturesPerFolder = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Background byte file name:"))
                                    {
                                        splitString = line.Split();
                                        subtoken = splitString[splitString.Length - 1];
                                        subtoken = subtoken.Remove(0, 1);
                                        snapshotFileName = subtoken.Remove(subtoken.Length - 1);
                                    }
                                    else if (line.StartsWith("Snapshot folder number:"))
                                    {
                                        splitString = line.Split();
                                        snapshotFolderNumber = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Replay individual number:"))
                                    {
                                        splitString = line.Split();
                                        replayIndividualNumber = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Dimensions per color bin:"))
                                    {
                                        splitString = line.Split();
                                        numDimsPerRGBAxis = Convert.ToInt32(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Prob. mutate connection weights:"))
                                    {
                                        splitString = line.Split();
                                        pMutConnectionWeight = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Prob. mutate add node:"))
                                    {
                                        splitString = line.Split();
                                        pAddNode = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Prob. mutate delete node:"))
                                    {
                                        splitString = line.Split();
                                        pDeleteSimpleNeuron = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Prob. mutate add module:"))
                                    {
                                        splitString = line.Split();
                                        pAddModule = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Prob. mutate add connection:"))
                                    {
                                        splitString = line.Split();
                                        pAddConnection = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                    else if (line.StartsWith("Prob. mutate delete connection:"))
                                    {
                                        splitString = line.Split();
                                        pDeleteConnection = Convert.ToDouble(splitString[splitString.Length - 1]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize() will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
			// Get the (unique) start time
			startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            // Initialize simulator
            base.Initialize();

            // Set interface options
            IsMouseVisible = true;

            // Set the title of the window
            Window.Title = "Chromaria";

            // Create a new SpriteBatch, which can be used to render textures onscreen
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Initialize the region lists
            regions = new List<int>[(int)Math.Ceiling((double)initialBoardHeight / regionHeight), (int)Math.Ceiling((double)initialBoardWidth / regionWidth)];
            for (int row = 0; row < (int)Math.Ceiling((double)initialBoardHeight / regionHeight); row++)
            {
                for (int col = 0; col < (int)Math.Ceiling((double)initialBoardWidth / regionWidth); col++)
                    regions[row, col] = new List<int>();
            }
        }

        #region region initialization
        /// <summary>
        /// Generates a completely blank background for the region.
        /// </summary>
        protected Texture2D generateBlankBackground(bool northBorder, bool eastBorder, bool southBorder, bool westBorder)
        {
            Color[] pixels = new Color[regionWidth * regionHeight];
            for (int y = 0; y < regionHeight; y++)
            {
                for (int x = 0; x < regionWidth; x++)
                    pixels[(regionWidth * y) + x] = Color.White;
            }

            // Top edge
            if (northBorder)
            {
                for (int y = 0; y < borderThickness; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Right edge
            if (eastBorder)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = regionWidth - borderThickness - 1; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Bottom edge
            if (southBorder)
            {
                for (int y = regionHeight - borderThickness - 1; y < regionHeight; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Left edge
            if (westBorder)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = 0; x < borderThickness; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Convert the collected pixels into a 2D texture
            Texture2D background = new Texture2D(GraphicsDevice, regionWidth, regionHeight);
            background.SetData(pixels);
            return background;
        }

        /// <summary>
        /// (x1, y1) and (x2, y2) specify the corners of the rectangular colored area. For no colored area (a blank texture),
        /// set all of these fields to 0. The coordinates are defined as offsets from the top and left of the region. Thus,
        /// the x and y coordinates can't exceed the region's width and height. 
        /// Note that the pixels at (x1,y1) and (x2,y2) will be included in the colored area.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        protected Texture2D generateRandomBackground(bool northBorder, bool eastBorder, bool southBorder, bool westBorder)
        {
            Color[] pixels = new Color[regionWidth * regionHeight];
            Color pixelColor = new Color();
            Random random = new Random();

            // Loop through all pixels in the background
            for (int y = 0; y < regionHeight; y++)
            {
                for (int x = 0; x < regionWidth; x++)
                {
                    // If (x,y) is in the area defined by (x1,y1) and (x2,y2), fill it in with a random color
                    if (x >= 0 && x <= regionWidth && y >= 0 && y <= (int)(regionHeight * colorRatio))
                    {
                        pixelColor.R = (byte)random.Next(256);
                        pixelColor.G = (byte)random.Next(256);
                        pixelColor.B = (byte)random.Next(256);
                        pixelColor.A = 255;
                    }
                    // Otherwise, make it white
                    else
                        pixelColor = Color.White;

                    pixels[(regionWidth * y) + x] = pixelColor;
                }
            }
            
            
            // Top edge
            if (northBorder)
            {
                for (int y = 0; y < borderThickness; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Right edge
            if (eastBorder)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = regionWidth - borderThickness - 1; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Bottom edge
            if (southBorder)
            {
                for (int y = regionHeight - borderThickness - 1; y < regionHeight; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Left edge
            if (westBorder)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = 0; x < borderThickness; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Convert the collected pixels into a 2D texture
            Texture2D background = new Texture2D(GraphicsDevice, regionWidth, regionHeight);
            background.SetData(pixels);
            return background;
        }

        /// <summary>
        /// Generates a grey striped background. This is mostly useful for debugging purposes.
        /// </summary>
        /// <param name="northBorder"></param>
        /// <param name="eastBorder"></param>
        /// <param name="southBorder"></param>
        /// <param name="westBorder"></param>
        /// <returns></returns>
        protected Texture2D generateStripedBackground(bool northBorder, bool eastBorder, bool southBorder, bool westBorder)
        {
            Color[] pixels = new Color[regionWidth * regionHeight];
            Color pixelColor = new Color();
            Random random = new Random();

            // Loop through all the pixels in the background image
            for (int y = 0; y < regionHeight; y++)
            {
                for (int x = 0; x < regionWidth; x++)
                {
                    // If (x,y) is in the area defined by (x1,y1) and (x2,y2), fill it in with alternating grey stripes
                    if (x >= 0 && x <= regionWidth && y >= 0 && y <= (int)(regionHeight * colorRatio))
                    {
                        if (y % 40 > 19)
                            pixelColor = Color.SlateGray;
                        else
                            pixelColor = Color.Silver;
                    }
                    // Otherwise, make it white.
                    else
                        pixelColor = Color.White;

                    pixels[(regionWidth * y) + x] = pixelColor;
                }
            }

            // Top edge
            if (northBorder)
            {
                for (int y = 0; y < borderThickness; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Right edge
            if (eastBorder)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = regionWidth - borderThickness - 1; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Bottom edge
            if (southBorder)
            {
                for (int y = regionHeight - borderThickness - 1; y < regionHeight; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Left edge
            if (westBorder)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = 0; x < borderThickness; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Convert the collected pixels into a 2D texture
            Texture2D background = new Texture2D(GraphicsDevice, regionWidth, regionHeight);
            background.SetData(pixels);
            return background;
        }

        /// <summary>
        /// (x1, y1) and (x2, y2) specify the corners of the rectangular colored area. For no colored area (a blank texture),
        /// set all of these fields to 0. The coordinates are defined as offsets from the top and left of the region. Thus,
        /// the x and y coordinates can't exceed the region's width and height. 
        /// Note that the pixels at (x1,y1) and (x2,y2) will be included in the colored area.
        /// This overloaded version of the function creates a ground colored based on the color ratios of the specified seed texture.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        protected Texture2D generateSeededBackground(Texture2D seedTexture, bool northBorder, bool eastBorder, bool southBorder, bool westBorder)
        {
            #region get color counts
            // Loop through each nontransparent pixel in the body texture
            float numNonTransparentPixels = 0.0f;
            double threshold = Math.Floor(255.0 / 2.0);

            float blackCount = 0.0f, whiteCount = 0.0f, redCount = 0.0f, greenCount = 0.0f, blueCount = 0.0f, yellowCount = 0.0f, magentaCount = 0.0f, cyanCount = 0.0f;
            float blackRatio, whiteRatio, redRatio, greenRatio, blueRatio, yellowRatio, magentaRatio, cyanRatio;

            Color[] textureAsColorArray = new Color[seedTexture.Width * seedTexture.Height];
            seedTexture.GetData(textureAsColorArray);
            foreach (Color pixel in textureAsColorArray)
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
            #endregion

            // Initialize temporary data structures
            Color[] colors = new Color[8] { Color.Black, Color.White, Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Magenta, Color.Cyan };
            float[] colorWeights = new float[8]{blackRatio*100, whiteRatio*100, redRatio*100, blueRatio*100, greenRatio*100, 
                yellowRatio*100, magentaRatio*100, cyanRatio*100};

            float sum = 0;
            for (int i = 0; i < colorWeights.Length; i++)
                sum += colorWeights[i];

            Color[] pixels = new Color[regionWidth * regionHeight];
            Random randomGen = new Random();
            int randomNum;
            float count;

            if(colorAllFourBorders)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                    {
                        // If (x,y) is in the area defined by (x1,y1) and (x2,y2), fill it in with a color according to the calculated ratios
                        if (x >= 0 && x <= regionWidth && y >= 0 && y <= (int)(regionHeight * colorRatio))
                        {
                            count = 0;
                            randomNum = randomGen.Next(100);
                            for (int i = 0; i < colorWeights.Length; i++)
                            {
                                count += colorWeights[i];
                                if (randomNum < count || i == colorWeights.Length - 1)
                                {
                                    pixels[(regionWidth * y) + x] = colors[i];
                                    break;
                                }
                            }
                        }
                        else if (x >= 0 && x <= regionWidth && y >= (regionHeight - (int)(regionHeight * colorRatio)))
                        {
                            count = 0;
                            randomNum = randomGen.Next(100);
                            for (int i = 0; i < colorWeights.Length; i++)
                            {
                                count += colorWeights[i];
                                if (randomNum < count || i == colorWeights.Length - 1)
                                {
                                    pixels[(regionWidth * y) + x] = colors[i];
                                    break;
                                }
                            }
                        }
                        else if(x < (int)(regionHeight * colorRatio))
                        {
                            count = 0;
                            randomNum = randomGen.Next(100);
                            for (int i = 0; i < colorWeights.Length; i++)
                            {
                                count += colorWeights[i];
                                if (randomNum < count || i == colorWeights.Length - 1)
                                {
                                    pixels[(regionWidth * y) + x] = colors[i];
                                    break;
                                }
                            }
                        }
                        else if(x > (regionWidth  - (int)(regionHeight * colorRatio)))
                        {
                            count = 0;
                            randomNum = randomGen.Next(100);
                            for (int i = 0; i < colorWeights.Length; i++)
                            {
                                count += colorWeights[i];
                                if (randomNum < count || i == colorWeights.Length - 1)
                                {
                                    pixels[(regionWidth * y) + x] = colors[i];
                                    break;
                                }
                            }
                        }
                        // Otherwise, make it white.
                        else
                            pixels[(regionWidth * y) + x] = Color.White;
                    }
                }
            }

            // (This option will only color the north border)
            else
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                    {
                        // If (x,y) is in the area defined by (x1,y1) and (x2,y2), 
                        if (x >= 0 && x <= regionWidth && y >= 0 && y <= (int)(regionHeight * colorRatio))
                        {
                            count = 0;
                            randomNum = randomGen.Next(100);
                            for (int i = 0; i < colorWeights.Length; i++)
                            {
                                count += colorWeights[i];
                                if (randomNum < count || i == colorWeights.Length - 1)
                                {
                                    pixels[(regionWidth * y) + x] = colors[i];
                                    break;
                                }
                            }
                        }
                        // Otherwise, make it white.
                        else
                            pixels[(regionWidth * y) + x] = Color.White;
                    }
                }
            }

            // Top edge
            if (northBorder)
            {
                for (int y = 0; y < borderThickness; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Right edge
            if (eastBorder)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = regionWidth - borderThickness - 1; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Bottom edge
            if (southBorder)
            {
                for (int y = regionHeight - borderThickness - 1; y < regionHeight; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Left edge
            if (westBorder)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = 0; x < borderThickness; x++)
                        pixels[(regionWidth * y) + x] = Color.Black;
                }
            }

            // Convert the collected pixels to a 2D texture
            Texture2D background = new Texture2D(GraphicsDevice, regionWidth, regionHeight);
            background.SetData(pixels);
            return background;
        }
        #endregion

        /// <summary>
        /// Loads a genome from an XML file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public NeatGenome loadCPPNFromXml(String filename)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);
            return XmlNeatGenomeReaderStatic.Read(doc);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Check to see if we've requested to turn the graphics on / off or pause the simulation
            KeyboardState keyboard = Keyboard.GetState();

            // pause / unpause
            if (keyboard.IsKeyDown(Keys.P) && !paused)
                paused = true;
            else if (keyboard.IsKeyDown(Keys.O) && paused)
                paused = false;

            // graphics enabled / disabled
            if (keyboard.IsKeyDown(Keys.U))
            {
                graphicsEnabled = false;
                GraphicsDevice.Clear(Color.Black);
                TargetElapsedTime = TimeSpan.FromSeconds(1.0f/2000.0f); //10x speedup
            }
            else if (keyboard.IsKeyDown(Keys.I))
            {
                graphicsEnabled = true;
                TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 60.0f); // default value
            }
            if (!graphicsEnabled)
                SuppressDraw();

            // Calling this function will loop through all of the update functions for the individual game components. 
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Reset the graphics device
            GraphicsDevice.Clear(Color.White);
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            // This function will loop through all of the GameComponents and call their individual Draw() functions
            base.Draw(gameTime);

            // Wrap it up
            spriteBatch.End();
        }

        #region morphology generation
        /// <summary>
        /// Generates the creature's morphology using a manually-specified CPPN
        /// </summary>
        /// <returns></returns>
        public Texture2D generateMorphology(INetwork morphologyCPPN)
        {
            // Create a blank Color array
            // This is where we'll store the pixels in the CPPN-generated texture
            // Make sure all the pixels are initially transparent. Only valid pixels will be non-transparent. 
            Color[] pixels = new Color[pixelHeight * pixelHeight];
            for (int x = 0; x < pixelHeight; x++)
            {
                for (int y = 0; y < pixelHeight; y++)
                {
                    pixels[x + y * pixelHeight] = Color.Transparent;
                }
            }
            
            // Temporary variable declarations
            int red, green, blue, rmax;
            float inputSignal;
            Microsoft.Xna.Framework.Point xyPoint;

            // Draw the image one line segment at a time, starting at theta=0 and working around until you hit 360 degrees
            for (float theta = (float)-Math.PI; theta < Math.PI; theta+= (1.0f/360.0f))
            {
                // Activate the CPPN with r=0 to find rmax for this value of theta.
                morphologyCPPN.SetInputSignal(0, 0);
                inputSignal = MathHelpers.Scale(theta, -Math.PI, Math.PI, -1.0, 1.0);
                morphologyCPPN.SetInputSignal(1, inputSignal);
                morphologyCPPN.SingleStep();

                // Query the CPPN for rmax
                rmax = (int)Math.Floor(MathHelpers.Scale(morphologyCPPN.GetOutputSignal(3), -1.0, 1.0, 0.0, 1.0) * 49.0);

                xyPoint = MathHelpers.ConvertToCartesian(rmax, theta + (float)(Math.PI / 2));
                pixels[xyPoint.X + (xyPoint.Y * pixelHeight)] = Color.TransparentBlack;

                // Color one line segment extending from the origin to rmax
                for (int r = 0; r < rmax; r++)
                {
                    morphologyCPPN.SetInputSignal(0, MathHelpers.Scale((float)r, 0.0, 49.0, -1.0, 1.0));
                    morphologyCPPN.SingleStep();

                    xyPoint = MathHelpers.ConvertToCartesian(r, theta + (float)(Math.PI / 2));

                    // Convert the CPPN output signals to byte format
                    red = Convert.ToInt32(MathHelpers.Scale(morphologyCPPN.GetOutputSignal(0), -1.0, 1.0, 0.0, 1.0) * 255.0);
                    green = Convert.ToInt32(MathHelpers.Scale(morphologyCPPN.GetOutputSignal(1), -1.0, 1.0, 0.0, 1.0) * 255.0);
                    blue = Convert.ToInt32(MathHelpers.Scale(morphologyCPPN.GetOutputSignal(2), -1.0, 1.0, 0.0, 1.0) * 255.0);

                    pixels[xyPoint.X + (xyPoint.Y * pixelHeight)] = new Color(red, green, blue, 255);
                }
            }

            // Convert the collected pixels to a 2D texture
            Texture2D morphologyTexture = new Texture2D(GraphicsDevice, pixelHeight, pixelHeight);
            morphologyTexture.SetData(pixels);
            return morphologyTexture;
        }

        /// <summary>
        /// Generates a perfectly circular body filled with pixels that are the same color.
        /// </summary>
        /// <returns></returns>
        public Texture2D generateSolidMorphology(Texture2D referenceTexture)
        {
            // Create a blank Color array
            // This is where we'll store the pixels in the CPPN-generated texture
            // Make sure all the pixels are initially transparent. Only valid pixels will be non-transparent. 
            Color[] pixels = new Color[referenceTexture.Height * referenceTexture.Width];
            for (int x = 0; x < referenceTexture.Width; x++)
            {
                for (int y = 0; y < referenceTexture.Height; y++)
                {
                    pixels[x + y * referenceTexture.Height] = new Color(0, 0, 0, 0);
                }
            }

            // Generate the creature's morphology
            int rmax;
            Microsoft.Xna.Framework.Point xyPoint;
            int red, green, blue;
            for (int theta = 0; theta < 360; theta++)
            {
                // Draw the outermost point at (rmax, theta) here
                rmax = 40;
                xyPoint = MathHelpers.ConvertToCartesian(rmax, theta);
                pixels[xyPoint.X + (xyPoint.Y * referenceTexture.Height)] = Color.Black;

                // Color all of the points inside of rmax
                for (int r = 0; r < rmax; r++)
                {
                    xyPoint = MathHelpers.ConvertToCartesian(r, theta);
                    red = 255;
                    green = 0;
                    blue = 0;

                    pixels[xyPoint.X + (xyPoint.Y * referenceTexture.Height)] = new Color(red, green, blue, 255);
                }
            }

            // Convert the collected pixels to a 2D texture
            Texture2D morphologyTexture = new Texture2D(GraphicsDevice, referenceTexture.Width, referenceTexture.Height);
            morphologyTexture.SetData(pixels);
            return morphologyTexture;
        }

        /// <summary>
        /// Generates a perfectly circular body filled with randomly-generated colors at each pixel.
        /// This function is used with novelty search so that the creature's body has roughly the same 
        /// colors as the fertile ground at the top of the world (and thus it should be theoretically able to 
        /// satisfy the planting function.)
        /// </summary>
        /// <returns></returns>
        public Texture2D generateUniformlyColoredMorphology()
        {
            // Create a blank Color array
            // This is where we'll store the pixels in the CPPN-generated texture
            // Make sure all the pixels are initially transparent. Only valid pixels will be non-transparent. 
            Color[] pixels = new Color[pixelHeight * pixelHeight];
            for (int x = 0; x < pixelHeight; x++)
            {
                for (int y = 0; y < pixelHeight; y++)
                {
                    pixels[x + y * pixelHeight] = new Color(0, 0, 0, 0);
                }
            }

            // This will be used for generating random colors for the pixels
            Random random = new Random();

            // Generate the creature's morphology
            int rmax;
            Microsoft.Xna.Framework.Point xyPoint;
            int red, green, blue;
            for (int theta = 0; theta < 360; theta++)
            {
                // Draw the outermost point at (rmax, theta) here
                rmax = 40;
                xyPoint = MathHelpers.ConvertToCartesian(rmax, theta);
                pixels[xyPoint.X + (xyPoint.Y * pixelHeight)] = Color.Black;

                // Color all of the points inside of rmax
                for (int r = 0; r < rmax; r++)
                {
                    xyPoint = MathHelpers.ConvertToCartesian(r, theta);
                    red = random.Next(256);
                    green = random.Next(256);
                    blue = random.Next(256);

                    pixels[xyPoint.X + (xyPoint.Y * pixelHeight)] = new Color(red, green, blue, 255);
                }
            }

            // Convert the collected pixels into a 2D texture
            Texture2D morphologyTexture = new Texture2D(GraphicsDevice, pixelHeight, pixelHeight);
            morphologyTexture.SetData(pixels);
            return morphologyTexture;
        }
        #endregion
    }
}