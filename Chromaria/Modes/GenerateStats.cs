using Chromaria.SharpNeatLib;
using Chromaria.SharpNeatLib.CPPNs;
using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.Evolution.Xml;
using Chromaria.SharpNeatLib.Experiments;
using Chromaria.SharpNeatLib.Maths;
using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.SharpNeatLib.NeatGenome.Xml;
using Chromaria.SharpNeatLib.NeuralNetwork;
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
    class GenerateStats : Simulator
    {
        string nextLine;
        int numPlanters;
        int numNonTransparentPixels;
        int summedR, summedG, summedB;
        GraphNode[, ,] parentQueueGraph;
        List<GraphNode> activatedNodes;
        List<List<GraphNode>> disjointSets;

        protected override void Initialize()
        {
            int divider = (int)Math.Ceiling((double)(256.0 / numDimsPerRGBAxis));

            // Iterate through each snapshot (each subdirectory of logsfolder)
            List<String> dirEnum = new List<String>(Directory.EnumerateDirectories(logsFolder));
            foreach (string snapshotNum in dirEnum)
            {
                if (!analyzePlantingRatesOnly)
                {
                    // First, get bins for single snapshot of the world
                    activatedNodes = new List<GraphNode>();
                    disjointSets = new List<List<GraphNode>>();
                    numNonTransparentPixels = 0;

                    // Populate the components list
                    if (File.Exists(snapshotNum + "\\Components.txt"))
                    {
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(snapshotNum + "\\Components.txt"))
                        {
                            // The first line is the current creature's ID; skip it
                            reader.ReadLine();

                            int genomeID, folderNumber;
                            String nextLine;
                            NeatGenome morphologyCPPNGenome;
                            INetwork morphologyCPPN;
                            Texture2D newMorphology;
                            NNControlledCreature newCreature;
                            while ((nextLine = reader.ReadLine()) != null)
                            {
                                // First get the ID of the morphology
                                genomeID = Convert.ToInt32(nextLine);

                                // Then go find the genome corresponding to that ID
                                folderNumber = (genomeID - 1) / numCreaturesPerFolder;

                                // Load the creature's morphology CPPN
                                morphologyCPPNGenome = loadCPPNFromXml(logsFolder + folderNumber.ToString() + "\\" + morphologyXMLprefix + genomeID.ToString() + ".xml");
                                morphologyCPPN = morphologyCPPNGenome.Decode(ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"));
                                newMorphology = generateMorphology(morphologyCPPN);

                                // Create a new creature, which will automatically add it to the components list
                                newCreature = new NNControlledCreature(newMorphology, initialBoardWidth / 2, initialBoardWidth / 2, 0.0f, null, this, drawSensorField, trackPlanting, defaultNumSensors, freezeAfterPlanting, null, morphologyCPPNGenome);
                                newCreature.Genome.fileNumber = genomeID;
                            }
                        }
                    }


                    // Loop through the parent list and find the number of pixels that belong in each bin
                    numNonTransparentPixels = 0;
                    parentQueueGraph = new GraphNode[numDimsPerRGBAxis, numDimsPerRGBAxis, numDimsPerRGBAxis];

                    if (File.Exists(snapshotNum + "\\ParentList.txt"))
                    {
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(snapshotNum + "\\ParentList.txt")) //parent list contains index into components list
                        {
                            // Skip first line, then read the rest
                            reader.ReadLine();
                            while ((nextLine = reader.ReadLine()) != null)
                            {
                                // Find the decoded individual (index into components list)
                                NNControlledCreature currentCreature = (NNControlledCreature)(Components[Convert.ToInt32(nextLine) - 1]);

                                summedR = 0;
                                summedG = 0;
                                summedB = 0;
                                numNonTransparentPixels = 0;

                                // calculate the RGB ratios
                                foreach (Color pixel in currentCreature.TextureAsColorArray)
                                {
                                    if (pixel.A != 0)
                                    {
                                        numNonTransparentPixels++;

                                        summedR += pixel.R;
                                        summedG += pixel.G;
                                        summedB += pixel.B;
                                    }
                                }

                                // Add individual to one bin based on R, G, B ratios
                                // To get ratios, just sum the R, G, B values divided by number of nontransparent pixels 
                                if (parentQueueGraph[summedR / (divider * numNonTransparentPixels), summedG / (divider * numNonTransparentPixels), summedB / (divider * numNonTransparentPixels)] == null)
                                {
                                    parentQueueGraph[summedR / (divider * numNonTransparentPixels), summedG / (divider * numNonTransparentPixels), summedB / (divider * numNonTransparentPixels)] = new GraphNode();
                                    parentQueueGraph[summedR / (divider * numNonTransparentPixels), summedG / (divider * numNonTransparentPixels), summedB / (divider * numNonTransparentPixels)].representativeID = currentCreature.Genome.fileNumber;
                                }
                                parentQueueGraph[summedR / (divider * numNonTransparentPixels), summedG / (divider * numNonTransparentPixels), summedB / (divider * numNonTransparentPixels)].r = summedR / (divider * numNonTransparentPixels);
                                parentQueueGraph[summedR / (divider * numNonTransparentPixels), summedG / (divider * numNonTransparentPixels), summedB / (divider * numNonTransparentPixels)].g = summedG / (divider * numNonTransparentPixels);
                                parentQueueGraph[summedR / (divider * numNonTransparentPixels), summedG / (divider * numNonTransparentPixels), summedB / (divider * numNonTransparentPixels)].b = summedB / (divider * numNonTransparentPixels);
                            }
                        }

                        // After all individuals have been read, append the counts to an external text file
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "parent-queue-visualization.txt", true))
                        {
                            // Then, loop through each bin and record the count for each
                            for (int r = 0; r < numDimsPerRGBAxis; r++)
                            {
                                for (int g = 0; g < numDimsPerRGBAxis; g++)
                                {
                                    for (int b = 0; b < numDimsPerRGBAxis; b++)
                                    {
                                        if (parentQueueGraph[r, g, b] == null)
                                            file.Write("0,");
                                        else
                                            file.Write("1,");
                                    }
                                }
                            }
                            file.WriteLine();
                        }

                        // Connect all adjacent neighbors in the parent queue graph
                        for (int r = 0; r < numDimsPerRGBAxis; r++)
                        {
                            for (int g = 0; g < numDimsPerRGBAxis; g++)
                            {
                                for (int b = 0; b < numDimsPerRGBAxis; b++)
                                {
                                    if (parentQueueGraph[r, g, b] != null)
                                    {
                                        activatedNodes.Add(parentQueueGraph[r, g, b]);
                                        for (int rmod = -1; rmod < 2; rmod++)
                                        {
                                            for (int gmod = -1; gmod < 2; gmod++)
                                            {
                                                for (int bmod = -1; bmod < 2; bmod++)
                                                {
                                                    if (!(rmod == 0 && bmod == 0 && gmod == 0) && (r + rmod > -1) && (r + rmod < numDimsPerRGBAxis) && (g + gmod > -1) && (g + gmod < numDimsPerRGBAxis) && (b + bmod > -1) && (b + bmod < numDimsPerRGBAxis))
                                                    {
                                                        if (parentQueueGraph[r + rmod, g + gmod, b + bmod] != null)
                                                            parentQueueGraph[r, g, b].connections.Add(parentQueueGraph[r + rmod, g + gmod, b + bmod]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }


                        // Now that all nodes have been connected, identify the connected components.
                        // To find all the connected components of a graph, loop through its vertices, 
                        // starting a new breadth first or depth first search whenever the loop reaches 
                        // a vertex that has not already been included in a previously found connected component.
                        while (activatedNodes.Count > 0)
                        {
                            if (activatedNodes[0].parent == null)
                            {
                                // Create a new disjoint set and add the current parent (it will be the root)
                                disjointSets.Add(new List<GraphNode>());
                                disjointSets[disjointSets.Count - 1].Add(activatedNodes[0]);
                            }
                            else
                            {
                                // Add the node to the correct disjoint set (containing the parent)
                                foreach (List<GraphNode> set in disjointSets)
                                {
                                    if (set.Contains(activatedNodes[0].parent))
                                    {
                                        set.Add(activatedNodes[0]);
                                        break;
                                    }
                                }
                            }

                            // Find all of the current node's children and set the current node as the parent
                            foreach (GraphNode connectedNode in activatedNodes[0].connections)
                                connectedNode.parent = activatedNodes[0];

                            activatedNodes.RemoveAt(0);
                        }

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "disjointsets.txt", true))
                        {
                            file.WriteLine(disjointSets.Count);
                        }

                        // Print out connected component information
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "parent-queue-connected-components.txt", true))
                        {
                            file.WriteLine(disjointSets.Count);
                            foreach (List<GraphNode> set in disjointSets)
                            {
                                for (int i = 0; i < set.Count; i++)
                                {
                                    file.Write(set[i].r + " " + set[i].g + " " + set[i].b + " " + set[i].representativeID + ", ");
                                }
                                file.WriteLine();

                                set.Clear();
                            }
                        }

                        activatedNodes.Clear();
                        disjointSets.Clear();
                    }
                    else
                        throw new Exception(logsFolder + "ParentList.txt not found.");


                    // Finally, reset the components list and free the textures for each individual
                    for (int i = 0; i < Components.Count; i++)
                        ((Creature)(Components[i])).Texture.Dispose();
                    Components.Clear();

                }

                numPlanters = 0;
                if (File.Exists(snapshotNum + "\\RunInfo.txt"))
                {
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(snapshotNum + "\\RunInfo.txt")) 
                    {
                        // Skip first line, then read the rest
                        while ((nextLine = reader.ReadLine()) != null)
                        {
                            if(nextLine.Contains("planted successfully"))
                                numPlanters++;
                        }
                    }
                }

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logsFolder + "planters.txt", true))
                {
                    file.WriteLine(numPlanters);
                }
            }
        }
        

        private Boolean creaturesAreCompatible(Creature creature1, Creature creature2)
        {
            // Check to see if the planting function is satisfied:
            // To calculate the summed difference, take the difference between bin ratios for body and sensor field. 
            // The maximum summed difference will be 8 (max. of 1 for each bin; 8 bins).
            float summedDifferences = 0.0f;
            summedDifferences += Math.Abs(creature1.BlackRatio - creature2.BlackRatio);
            summedDifferences += Math.Abs(creature1.WhiteRatio - creature2.WhiteRatio);
            summedDifferences += Math.Abs(creature1.RedRatio - creature2.RedRatio);
            summedDifferences += Math.Abs(creature1.GreenRatio - creature2.GreenRatio);
            summedDifferences += Math.Abs(creature1.BlueRatio - creature2.BlueRatio);
            summedDifferences += Math.Abs(creature1.YellowRatio - creature2.YellowRatio);
            summedDifferences += Math.Abs(creature1.MagentaRatio - creature2.MagentaRatio);
            summedDifferences += Math.Abs(creature1.CyanRatio - creature2.CyanRatio);

            // Then, if the summed difference does not exceed our tolerated difference, planting succeeds.
            return (summedDifferences <= Chromaria.Simulator.toleratedDifference);
        }

        protected override void Draw(GameTime gameTime)
        {
            Exit();
        }

        protected override void Update(GameTime gameTime)
        {
            Exit();
        }
    }

    class GraphNode
    {
        public int representativeID;
        public GraphNode parent;
        public List<GraphNode> connections;
        public int r, g, b;

        public GraphNode()
        {
            parent = null;
            connections = new List<GraphNode>();
        }
    }
}


