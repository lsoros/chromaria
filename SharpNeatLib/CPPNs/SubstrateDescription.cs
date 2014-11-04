using System; 
using System.Collections.Generic;
using System.Text;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.Xml;
using System.Xml;
using System.Drawing;
using Chromaria.SharpNeatLib.Experiments;
using Chromaria.SharpNeatLib.NeatGenome;

namespace Chromaria.SharpNeatLib.CPPNs
{
    public class SubstrateDescription
    {
        public class vis_node
        {
            public vis_node(uint _id, float xp, float yp, int _xpos, int _ypos, float _zfilter, int _groupID, int _number)
            {
                id = _id;
                groupID = _groupID;
                number = _number;
                pos = new PointF(xp, yp);
                ypos = _ypos;
                xpos = _xpos;
                zfilter = _zfilter;//-99 for no filtering
            }
            public uint id;
            public int groupID;
            public int number;//index of neuron in group
            public PointF pos; //the position of the node that should be displayed
            public int ypos; //where to display it
            public int xpos; //where to display it
            public float zfilter;//-99 for no filtering

        }

        public List<vis_node> visualizeNodes; 

        private List<NeuronGroup> neuronGroups;
        private List<PointF> hiddenNeurons;
        private List<PointF> inputNeurons;
        private List<PointF> outputNeurons;

        public uint InputCount { get; set; }
        public uint OutputCount { get; set; }
        public uint HiddenCount { get; set; }
        public bool useLeo;
        public SubstrateDescription(String filename)
        {
            useLeo = false;
            HiddenCount = 0;
            InputCount = 0;
            OutputCount = 0;

            hiddenNeurons = new List<PointF>();
            inputNeurons = new List<PointF>();
            outputNeurons = new List<PointF>();

            neuronGroups = new List<NeuronGroup>();

            XmlDocument document = new XmlDocument();
            document.Load(filename);
            XmlElement xmlSubstrate = (XmlElement)document.SelectSingleNode("substrate");

            if (xmlSubstrate == null)
                throw new Exception("The genome XML is missing the root 'substrate' element.");
            this.useLeo = bool.Parse(XmlUtilities.GetAttribute(xmlSubstrate, "leo").Value);
            //--- Read neuron genes into a list.
            //NeuronGeneList neuronGeneList = new NeuronGeneList();

            XmlElement xmlGroups = (XmlElement)xmlSubstrate.SelectSingleNode("neuronGroups");
            XmlNodeList listNeuronGroups = xmlGroups.SelectNodes("group");
            foreach (XmlElement xmlNeuronGroup in listNeuronGroups)
            {
                int id = int.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "id").Value);
                String tmp = XmlUtilities.GetAttribute(xmlNeuronGroup, "type").Value;

                int neuronGroupType = -1;

                float startX = float.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "startx").Value);
                float startY = float.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "starty").Value);
                float endX = float.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "endx").Value);
                float endY = float.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "endy").Value);
                uint dx = uint.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "dx").Value);
                uint dy = uint.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "dy").Value);
                NeuronGroup ng = null;

                if (tmp.Equals("Hidden"))
                {
                    neuronGroupType = 2;
                    ng = new NeuronGroup(startX, startY, endX, endY, dx, dy, id, neuronGroupType, HiddenCount);
                    HiddenCount += dx * dy;
                }
                else if (tmp.Equals("Output"))
                {
                    neuronGroupType = 1;
                    ng = new NeuronGroup(startX, startY, endX, endY, dx, dy, id, neuronGroupType, OutputCount);
                    OutputCount += dx * dy;
                }
                else if (tmp.Equals("Input"))
                {
                    neuronGroupType = 0;
                    ng = new NeuronGroup(startX, startY, endX, endY, dx, dy, id, neuronGroupType, InputCount);
                    InputCount += dx * dy;
                }

                //  Console.WriteLine(id + " " + neuronGroupType + " " + startX + " " + startY + " " + endX + " " + endY + " " + dx + " " + dy);

                neuronGroups.Add(ng);
            }
            //Load Connections
            XmlElement xmlConnections = (XmlElement)xmlSubstrate.SelectSingleNode("connections");
            XmlNodeList listConnections = xmlConnections.SelectNodes("connection");
            foreach (XmlElement xmlConnection in listConnections)
            {
                int srcID = int.Parse(XmlUtilities.GetAttribute(xmlConnection, "src-id").Value);
                int tgID = int.Parse(XmlUtilities.GetAttribute(xmlConnection, "tg-id").Value);
                bool hive = string.Equals("hivebrain", XmlUtilities.GetAttribute(xmlConnection, "type").Value);
                foreach (NeuronGroup n in neuronGroups)
                {
                    if (n.GroupID == srcID)
                    {
                        if (hive)
                            n.HiveConnectedTo.Add(tgID);
                        else
                            n.ConnectedTo.Add(tgID);
                    }
                }
            }

            xmlConnections = (XmlElement)xmlSubstrate.SelectSingleNode("visualize");
            if (xmlConnections == null) return;

            listConnections = xmlConnections.SelectNodes("node");
            uint vis_id = 0;
            visualizeNodes = new List<vis_node>();
            foreach (XmlElement xmlConnection in listConnections)
            {
                int groupID = int.Parse(XmlUtilities.GetAttribute(xmlConnection, "group-id").Value);

                int number = int.Parse(XmlUtilities.GetAttribute(xmlConnection, "number").Value);
                int ypos = int.Parse(XmlUtilities.GetAttribute(xmlConnection, "ypos").Value);
                int xpos = int.Parse(XmlUtilities.GetAttribute(xmlConnection, "xpos").Value);

                float zfilter = float.Parse(XmlUtilities.GetAttribute(xmlConnection, "z").Value);

                //  int c = 0; //index in group
                foreach (NeuronGroup n in neuronGroups)
                {
                    if (n.GroupID == groupID)
                    {
                        //n.gr
                        visualizeNodes.Add(new vis_node(vis_id, n.NeuronPositions[number].X, n.NeuronPositions[number].Y, xpos, ypos, zfilter, groupID, number));
                        vis_id++;
                        //n.NeuronPositions[number].x;
                        //n.NeuronPositions[number].y;

                        // foreach (PointF source in ng.NeuronPositions)

                    }
                    //   c++;
                }

            }
        }

        //Get the start ID for a specific group
        public uint getStartID(uint groupID)
        {
            return getNeuronGroup(groupID).GlobalID;
        }

        public void getNeuronDensity(uint groupID, out uint dx, out uint dy)
        {
            dx = getNeuronGroup(groupID).DX;
            dy = getNeuronGroup(groupID).DY;
        }
        public List<PointF> getNeuronGroupByType(uint groupType)
        {
            List<PointF> ngl = new List<PointF>();

            foreach (NeuronGroup ng in neuronGroups)
            {
                if (ng.GroupType == groupType)
                {
                    ngl.AddRange(ng.NeuronPositions);
                }
            }
            return ngl;
        }

        //Changes the neuron density for a given group. Call generateGenome afterwards to get the new network
        public void setNeuronDensity(uint groupID, uint dx, uint dy)
        {
            //Console.WriteLine("Changing neuron density. id:" + groupID + " dx:" + dx + " dy:" + dy);
            getNeuronGroup(groupID).DX = dx;
            getNeuronGroup(groupID).DY = dy;
            updateNeuronCounts();
            foreach (NeuronGroup ng in neuronGroups)
            {
                ng.generateNodePositions();
            }
        }

        //Call updateNeuronCount after changing the density of a node group
        private void updateNeuronCounts()
        {
            HiddenCount = 0;
            InputCount = 0;
            OutputCount = 0;

            hiddenNeurons = new List<PointF>();
            inputNeurons = new List<PointF>();
            outputNeurons = new List<PointF>();

            foreach (NeuronGroup ng in neuronGroups)
            {
                switch (ng.GroupType)
                {
                    case 2: HiddenCount += ng.DX * ng.DY; break;
                    case 1: OutputCount += ng.DX * ng.DY; break;
                    case 0: InputCount += ng.DX * ng.DY; break;
                }
            }
        }

        public void normalizeWeightConnections(ref ConnectionGeneList connections, int neuronCount)
        {
            double[] weightSumPos = new double[neuronCount];
            double[] weightSumNeg = new double[neuronCount];
            double[] weightCounts = new double[neuronCount];
            ////Normalize Connection Weights
            ////ONLY NORMALIZE WEIGHTS BETWEEN HIDDEN NEURONS
            for (int i = 0; i < connections.Count; i++)
            {

                if (connections[i].Weight >= 0.0f)
                {
                    weightSumPos[connections[i].TargetNeuronId] += Math.Abs(connections[i].Weight); //connections[i].weight; //Abs value?
                }
                else
                {
                    weightSumNeg[connections[i].TargetNeuronId] += Math.Abs(connections[i].Weight); //connections[i].weight; //Abs value?

                }
                weightCounts[connections[i].TargetNeuronId]++;
            }
            for (int i = 0; i < connections.Count; i++)
            {
                if (weightCounts[connections[i].TargetNeuronId] <= 1)
                    continue;
                if (connections[i].Weight >= 0.0f)
                {
                    if (weightSumPos[connections[i].TargetNeuronId] != 0.0f)
                        connections[i].Weight /= weightSumPos[connections[i].TargetNeuronId];
                }
                else
                {
                    if (weightSumNeg[connections[i].TargetNeuronId] != 0.0f)
                        connections[i].Weight /= weightSumNeg[connections[i].TargetNeuronId];
                }
                connections[i].Weight *= 3.0;
            }
        }

        public NeuronGroup getNeuronGroup(uint groupID)
        {
            foreach (NeuronGroup ng in neuronGroups)
            {
                if (ng.GroupID == groupID)
                {
                    return ng;
                }
            }
            return null;
        }

        public NeatGenome.NeatGenome generateGenome(INetwork network)
        {
            return null;    //Not supported right now
        }

        public NeatGenome.NeatGenome generateMultiGenomeModulus(INetwork network, uint numberOfAgents)
        {
            return null; //Not supported right now
        }

        #region Generate homogenous genome
        public NeatGenome.NeatGenome generateHomogeneousGenome(INetwork network, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet, bool evolveSubstrate)
        {
            if (evolveSubstrate)
            {
            return generateHomogeneousGenomeES(network, normalizeWeights, adaptiveNetwork, modulatoryNet);
            }
            else
                return generateHomogeneousGenome(network, normalizeWeights, adaptiveNetwork, modulatoryNet);
        }

        private NeatGenome.NeatGenome generateHomogeneousGenomeES(INetwork network, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet)
        {
            //CHECK TO SEE IF HIDDEN NEURONS ARE OUTPUT NEURONS
            List<PointF> hiddenNeuronPositions = new List<PointF>();

            IActivationFunction activationFunction = HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new ConnectionGeneList();//(int)((InputCount * HiddenCount) + (HiddenCount * OutputCount)));

            List<PointF> outputNeuronPositions = getNeuronGroupByType(1);
            List<PointF> inputNeuronPositions = getNeuronGroupByType(0);


            SubstrateEvolution se = new SubstrateEvolution();

            se.generateConnections(inputNeuronPositions, outputNeuronPositions, network,
                SubstrateEvolution.SAMPLE_WIDTH,
                SubstrateEvolution.SAMPLE_TRESHOLD,
                SubstrateEvolution.NEIGHBOR_LEVEL,
                SubstrateEvolution.INCREASE_RESSOLUTION_THRESHOLD,
                SubstrateEvolution.MIN_DISTANCE,
                SubstrateEvolution.CONNECTION_TRESHOLD, //0.4. ConnectionThreshold
                InputCount, OutputCount, -1.0f, -1.0f, 1.0f, 1.0f, ref connections, ref hiddenNeuronPositions);

            HiddenCount = (uint)hiddenNeuronPositions.Count;

            float[] coordinates = new float[5];
            uint connectionCounter = (uint)connections.Count;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount + OutputCount + HiddenCount));

            // set up the input nodes
            for (uint a = 0; a < InputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }

            // set up the output nodes
            for (uint a = 0; a < OutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount, NeuronType.Output, activationFunction));
                
            }
            // set up the hidden nodes
            for (uint a = 0; a < HiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + InputCount + OutputCount, NeuronType.Hidden, activationFunction));
            }


            uint sourceID = uint.MaxValue, targetID = uint.MaxValue;
            NeuronGroup connectedNG;
            uint c1, c2;

            float delta = 0.15f;//2.0f / InputCount;
            float minDistance, dist, sourceX = -1, sourceY = -1, targetX = -1, targetY = -1;
            uint closestNodeIndex;
            //   int index, hiddenCount;

            //Connections to input nodes    
            // hiddenCount = 0;

            //TEST??????????????????????????????

            double tolerance = 0.1;
            //bool[] taken = new bool[hiddenNeuronGroup.NeuronPositions.Count];
            closestNodeIndex = 0;
            int ccc;

            //CONNECT FROM INPUT NODES
            // ConnectionGeneList addConnections = new ConnectionGeneList();
            targetID = 0;
            bool[] visited = new bool[neurons.Count];
            List<uint> nodeList = new List<uint>();
            bool[] connectedToInput = new bool[neurons.Count];

            //From hidden to output
            //taken = new bool[hiddenNeuronGroup.NeuronPositions.Count];
            // float targetX=-1.0f, targetY=-1.0f;
            targetID = 0;
            //  bool outputConnectedToInput;

            bool[] isOutput = new bool[neurons.Count];
            //float output, weight;
            //bool[] connectedToInput = new bool[neurons.Count];

            //bool connectToHidden;

            float totalConnectionDist = 0.0f;
            //Add connections between Hidden Neurons
            // addConnections.AddRange(connections);

            bool danglingConnection = true;

            while (danglingConnection)
            {
                bool[] hasIncomming = new bool[neurons.Count];

                foreach (ConnectionGene co in connections)
                {
                    //  if (co.SourceNeuronId != co.TargetNeuronId)
                    // {
                    hasIncomming[co.TargetNeuronId] = true;
                    // }
                }
                for (int i = 0; i < InputCount; i++)
                    hasIncomming[i] = true;

                bool[] hasOutgoing = new bool[neurons.Count];
                foreach (ConnectionGene co in connections)
                {
                    //  if (co.TargetNeuronId != co.SourceNeuronId)
                    //  {
                    if (co.TargetNeuronId != co.SourceNeuronId)  //neurons that only connect to themselfs don't count
                    {
                        hasOutgoing[co.SourceNeuronId] = true;
                    }
                    //  }
                }

                //Keep  output neurons
                for (int i = 0; i < OutputCount; i++)
                    hasOutgoing[i + InputCount] = true;


                danglingConnection = false;
                //Check if there are still dangling connections
                foreach (ConnectionGene co in connections)
                {
                    if (!hasOutgoing[co.TargetNeuronId] || !hasIncomming[co.SourceNeuronId])
                    {
                        danglingConnection = true;
                        break;
                    }
                }

                connections.RemoveAll(delegate(ConnectionGene m) { return (!hasIncomming[m.SourceNeuronId]); });
                connections.RemoveAll(delegate(ConnectionGene m) { return (!hasOutgoing[m.TargetNeuronId]); });
            }

            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }

            SharpNeatLib.NeatGenome.NeatGenome gn = new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)(InputCount), (int)(OutputCount));
        //     SharpNeatLib.NeatGenome.NeatGenome sng = new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)(totalInputCount), (int)(totalOutputCount));

            gn.networkAdaptable = adaptiveNetwork;
            gn.networkModulatory = modulatoryNet;
       
            return gn;
        }

        private NeatGenome.NeatGenome generateHomogeneousGenome(INetwork network, bool normalizeWeights, bool  adaptiveNetwork,bool  modulatoryNet)
        {
            IActivationFunction activationFunction = HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new ConnectionGeneList((int)((InputCount * HiddenCount) + (HiddenCount * OutputCount)));
            float[] coordinates = new float[4];
            float output;
            uint connectionCounter = 0;
            int iterations = 2 * (network.TotalNeuronCount - (network.InputNeuronCount + network.OutputNeuronCount)) + 1;

            uint totalOutputCount = OutputCount;
            uint totalInputCount = InputCount;
            uint totalHiddenCount = HiddenCount;

            uint sourceCount, targetCout;
            double weightRange = HyperNEATParameters.weightRange;
            double threshold = HyperNEATParameters.threshold;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount + OutputCount + HiddenCount));

            // set up the input nodes
            for (uint a = 0; a < totalInputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }
            // set up the output nodes
            for (uint a = 0; a < totalOutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount, NeuronType.Output, activationFunction));
            }
            // set up the hidden nodes
            for (uint a = 0; a < totalHiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + InputCount + OutputCount, NeuronType.Hidden, activationFunction));
            }

            uint sourceID = uint.MaxValue, targetID = uint.MaxValue;
            NeuronGroup connectedNG;

            foreach (NeuronGroup ng in neuronGroups)
            {
                foreach (uint connectedTo in ng.ConnectedTo)
                {
                    connectedNG = getNeuronGroup(connectedTo);

                    sourceCount = 0;
                    foreach (PointF source in ng.NeuronPositions)
                    {

                        //-----------------Get the bias of the source node
                        switch (ng.GroupType)
                        {
                            case 0: sourceID = ng.GlobalID + sourceCount; break;                             //Input
                            case 1: sourceID = totalInputCount + ng.GlobalID + sourceCount; break;                //Output
                            case 2: sourceID = totalInputCount + totalOutputCount + ng.GlobalID + sourceCount; break;  //Hidden
                        }
                        coordinates[0] = source.X; coordinates[1] = source.Y; coordinates[2] = 0.0f; coordinates[3] = 0.0f;

                        network.ClearSignals();
                        network.SetInputSignals(coordinates);
                        network.MultipleSteps(iterations);

                        neurons[(int)sourceID].Bias = (float)(network.GetOutputSignal(1) * weightRange);
                        //----------------------------

                        targetCout = 0;
                        foreach (PointF target in connectedNG.NeuronPositions)
                        {
                            switch (ng.GroupType)
                            {
                                case 0: sourceID = ng.GlobalID + sourceCount; break;                             //Input
                                case 1: sourceID = totalInputCount + ng.GlobalID + sourceCount; break;                //Output
                                case 2: sourceID = totalInputCount + totalOutputCount + ng.GlobalID + sourceCount; break;  //Hidden
                            }

                            switch (connectedNG.GroupType)
                            {
                                case 0: targetID = connectedNG.GlobalID + targetCout; break;
                                case 1: targetID = totalInputCount + connectedNG.GlobalID + targetCout; break;
                                case 2: targetID = totalInputCount + totalOutputCount + connectedNG.GlobalID + targetCout; break;
                            }

                            coordinates[0] = source.X;
                            coordinates[1] = source.Y;
                            coordinates[2] = target.X;
                            coordinates[3] = target.Y;

                            network.ClearSignals();
                            network.SetInputSignals(coordinates);
                            network.MultipleSteps(iterations);
                            output = network.GetOutputSignal(0);

                            if (Math.Abs(output) > threshold)
                            {
                                float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                                connections.Add(new ConnectionGene(connectionCounter++, sourceID, targetID, weight, ref coordinates, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f));
                            }
                            //else
                            //{
                            //    Console.WriteLine("Not connected");
                            //}
                            targetCout++;
                        }
                        sourceCount++;
                    }
                }
            }
            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }
            NeatGenome.NeatGenome gn = new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)(totalInputCount), (int)(totalOutputCount));

            gn.networkAdaptable = adaptiveNetwork;
            gn.networkModulatory = modulatoryNet;
            return gn;
        }
        #endregion

        #region Generate heterogenous genomes with z-stack
        private NeatGenome.NeatGenome generateMultiGenomeStack(INetwork network, List<float> stackCoordinates, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet)
        {
            uint numberOfAgents = (uint)stackCoordinates.Count;
            IActivationFunction activationFunction = HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new ConnectionGeneList((int)(numberOfAgents * (InputCount * HiddenCount) + numberOfAgents * (HiddenCount * OutputCount)));
            float[] coordinates = new float[5];
            float output;
            uint connectionCounter = 0;
            float agentDelta = 2.0f / (numberOfAgents - 1);
            int iterations = 2 * (network.TotalNeuronCount - (network.InputNeuronCount + network.OutputNeuronCount)) + 1;

            uint totalOutputCount = OutputCount * numberOfAgents;
            uint totalInputCount = InputCount * numberOfAgents;
            uint totalHiddenCount = HiddenCount * numberOfAgents;

            uint sourceCount, targetCout;
            double weightRange = HyperNEATParameters.weightRange;
            double threshold = HyperNEATParameters.threshold;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount * numberOfAgents + OutputCount * numberOfAgents + HiddenCount * numberOfAgents));

            // set up the input nodes
            for (uint a = 0; a < totalInputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }
            // set up the output nodes
            for (uint a = 0; a < totalOutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount * numberOfAgents, NeuronType.Output, activationFunction));
            }
            // set up the hidden nodes
            for (uint a = 0; a < totalHiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + InputCount * numberOfAgents + OutputCount * numberOfAgents, NeuronType.Hidden, activationFunction));
            }

            uint agent = 0;
            float A = 0.0f, B = 0.0f, C = 0.0f, D = 0.0f, learningRate = 0.0f, modConnection;

            foreach (float stackCoordinate in stackCoordinates)
            {
                coordinates[4] = stackCoordinate;
                uint sourceID = uint.MaxValue, targetID = uint.MaxValue;
                NeuronGroup connectedNG;

                foreach (NeuronGroup ng in neuronGroups)
                {
                    foreach (uint connectedTo in ng.ConnectedTo)
                    {
                        connectedNG = getNeuronGroup(connectedTo);

                        sourceCount = 0;
                        foreach (PointF source in ng.NeuronPositions)
                        {

                            //-----------------Get the bias of the source node
                            switch (ng.GroupType)
                            {
                                case 0: sourceID = (agent * InputCount) + ng.GlobalID + sourceCount; break;                             //Input
                                case 1: sourceID = totalInputCount + (agent * OutputCount) + ng.GlobalID + sourceCount; break;                //Output
                                case 2: sourceID = totalInputCount + totalOutputCount + (agent * HiddenCount) + ng.GlobalID + sourceCount; break;  //Hidden
                            }
                            coordinates[0] = source.X; coordinates[1] = source.Y; coordinates[2] = 0.0f; coordinates[3] = 0.0f;

                            network.ClearSignals();
                            network.SetInputSignals(coordinates);
                            network.MultipleSteps(iterations);

                            neurons[(int)sourceID].Bias = (float)(network.GetOutputSignal(1) * weightRange);
                            //----------------------------

                            targetCout = 0;
                            foreach (PointF target in connectedNG.NeuronPositions)
                            {
                                switch (ng.GroupType)
                                {
                                    case 0: sourceID = (agent * InputCount) + ng.GlobalID + sourceCount; break;                             //Input
                                    case 1: sourceID = totalInputCount + (agent * OutputCount) + ng.GlobalID + sourceCount; break;                //Output
                                    case 2: sourceID = totalInputCount + totalOutputCount + (agent * HiddenCount) + ng.GlobalID + sourceCount; break;  //Hidden
                                }

                                switch (connectedNG.GroupType)
                                {
                                    case 0: targetID = (agent * InputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 1: targetID = totalInputCount + (agent * OutputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 2: targetID = totalInputCount + totalOutputCount + (agent * HiddenCount) + connectedNG.GlobalID + targetCout; break;
                                }

                                coordinates[0] = source.X;
                                coordinates[1] = source.Y;
                                coordinates[2] = target.X;
                                coordinates[3] = target.Y;

                                network.ClearSignals();
                                network.SetInputSignals(coordinates);
                                network.MultipleSteps(iterations);
                                output = network.GetOutputSignal(0);

                                double leo = 0.0;

                                if (adaptiveNetwork)
                                {
                                    A = network.GetOutputSignal(2);
                                    B = network.GetOutputSignal(3);
                                    C = network.GetOutputSignal(4);
                                    D = network.GetOutputSignal(5);
                                    learningRate = network.GetOutputSignal(6);
                                }

                                if (modulatoryNet)
                                {
                                    modConnection = network.GetOutputSignal(7);
                                }
                                else
                                {
                                    modConnection = 0.0f;
                                }

                                if (useLeo)
                                {
                                    threshold = 0.0;
                                    leo = network.GetOutputSignal(2);
                                }

                                if (!useLeo || leo > 0.0)
                                    if (Math.Abs(output) > threshold)
                                    {
                                        float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                                        //if (adaptiveNetwork)
                                        //{
                                        //    //If adaptive network set weight to small value
                                        //    weight = 0.1f;
                                        //}
                                        connections.Add(new ConnectionGene(connectionCounter++, sourceID, targetID, weight, ref coordinates, A, B, C, D, modConnection, learningRate));
                                    }
                                //else
                                //{
                                //    Console.WriteLine("Not connected");
                                //}
                                targetCout++;
                            }
                            sourceCount++;
                        }
                    }
                }
                agent++;
            }
            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }
            SharpNeatLib.NeatGenome.NeatGenome sng = new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)(totalInputCount), (int)(totalOutputCount));
            sng.networkAdaptable = adaptiveNetwork;
            sng.networkModulatory = modulatoryNet;
            return sng;
        }

        public NeatGenome.NeatGenome generateMultiGenomeStack(INetwork network, uint numberOfAgents, bool normalizeWeights, bool adaptiveNetwork, bool modNetwork, out List<float> coords, bool evolvedSubstrate)
        {
            //List<float>
            coords = new List<float>();

            float coord = -1.0f;
            float delta = 2.0f / (numberOfAgents - 1);
            for (uint x = 0; x < numberOfAgents; x++)
            {
                coords.Add(coord);
                coord += delta;
            }

            if (evolvedSubstrate)
                return this.generateMultiGenomeStackES(network, coords, normalizeWeights, adaptiveNetwork, modNetwork);
            else
                return this.generateMultiGenomeStack(network, coords, normalizeWeights, adaptiveNetwork, modNetwork);
        }

        public NeatGenome.NeatGenome generateHiveBrainGenomeStack(INetwork network, uint numberOfAgents, bool normalizeWeights, bool adaptiveNetwork, bool modNetwork, out List<float> coords, bool evolvedSubstrate, bool ct)
        {
            //List<float>
            coords = new List<float>();

            float coord = -1.0f;
            float delta = 2.0f / (numberOfAgents - 1);
            for (uint x = 0; x < numberOfAgents; x++)
            {
                coords.Add(coord);
                coord += delta;
            }

            if (evolvedSubstrate)
                return this.generateMultiGenomeStackES(network, coords, normalizeWeights, adaptiveNetwork, modNetwork);
            else
                return this.generateHiveBrainGenomeStack(network, coords, normalizeWeights, adaptiveNetwork, modNetwork, ct);
        }

        private String arrayToString(float[] array)
        {
            StringBuilder sb = new StringBuilder();
            foreach (float f in array)
                sb.Append(f.ToString()+" ");
            return sb.ToString();
        }

        private NeatGenome.NeatGenome generateHiveBrainGenomeStack(INetwork network, List<float> stackCoordinates, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet,bool ct)
        {
            bool relativeCoordinate = false;
            bool oneWay = false;
            bool homogeneous = false ;
            Dictionary<String, float> weights = new Dictionary<String, float>();
            float timeConstantMin = 0.1f;
            float timeConstantMax = 2.0f;

            uint numberOfAgents = (uint)stackCoordinates.Count;
            IActivationFunction activationFunction = HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new ConnectionGeneList((int)(numberOfAgents * (InputCount * HiddenCount) + numberOfAgents * (HiddenCount * OutputCount)));
            float[] coordinates = new float[6];
            float output;
            uint connectionCounter = 0;
            float agentDelta = 2.0f / (numberOfAgents - 1);
            int iterations = 2 * (network.TotalNeuronCount - (network.InputNeuronCount + network.OutputNeuronCount)) + 1;

            uint totalOutputCount = OutputCount * numberOfAgents;
            uint totalInputCount = InputCount * numberOfAgents;
            uint totalHiddenCount = HiddenCount * numberOfAgents;

            uint sourceCount, targetCout;
            double weightRange = HyperNEATParameters.weightRange;
            double threshold = HyperNEATParameters.threshold;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount * numberOfAgents + OutputCount * numberOfAgents + HiddenCount * numberOfAgents));

            // set up the input nodes
            for (uint a = 0; a < totalInputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }
            // set up the output nodes
            for (uint a = 0; a < totalOutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount * numberOfAgents, NeuronType.Output, activationFunction));
            }
            // set up the hidden nodes
            for (uint a = 0; a < totalHiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + InputCount * numberOfAgents + OutputCount * numberOfAgents, NeuronType.Hidden, activationFunction));
            }

            uint agent = 0;
            float A = 0.0f, B = 0.0f, C = 0.0f, D = 0.0f, learningRate = 0.0f, modConnection;

            foreach (float stackCoordinate in stackCoordinates)
            {
                coordinates[4] = homogeneous ? 0 : stackCoordinate;//-1 ? -1 : 0;//0;//stackCoordinate;
                coordinates[5] = stackCoordinate;
                uint sourceID = uint.MaxValue, targetID = uint.MaxValue;
                NeuronGroup connectedNG;

                foreach (NeuronGroup ng in neuronGroups)
                {
                    foreach (uint connectedTo in ng.ConnectedTo)
                    {
                        if (!relativeCoordinate)
                            coordinates[5] = stackCoordinate;
                        else
                            //USE RELATIVE
                            coordinates[5] = 0;
                        connectedNG = getNeuronGroup(connectedTo);

                        sourceCount = 0;
                        foreach (PointF source in ng.NeuronPositions)
                        {

                            //-----------------Get the bias of the source node
                           /* switch (ng.GroupType)
                            {
                                case 0: sourceID = (agent * InputCount) + ng.GlobalID + sourceCount; break;                             //Input
                                case 1: sourceID = totalInputCount + (agent * OutputCount) + ng.GlobalID + sourceCount; break;                //Output
                                case 2: sourceID = totalInputCount + totalOutputCount + (agent * HiddenCount) + ng.GlobalID + sourceCount; break;  //Hidden
                            }
                            coordinates[0] = source.X; coordinates[1] = source.Y; coordinates[2] = 0.0f; coordinates[3] = 0.0f;

                            network.ClearSignals();
                            network.SetInputSignals(coordinates);
                            network.MultipleSteps(iterations);

                            neurons[(int)sourceID].Bias = (float)(network.GetOutputSignal(1) * weightRange);
                            if (ct)
                            {
                                neurons[(int)sourceID].TimeConstant = 0.01f + ((((float)network.GetOutputSignal(2) + 1.0f) / 2.0f) * .05f);
                                System.Diagnostics.Debug.Assert(neurons[(int)sourceID].TimeConstant > 0);
                            }*/
                            //----------------------------

                            targetCout = 0;
                            foreach (PointF target in connectedNG.NeuronPositions)
                            {
                                switch (ng.GroupType)
                                {
                                    case 0: sourceID = (agent * InputCount) + ng.GlobalID + sourceCount; break;                             //Input
                                    case 1: sourceID = totalInputCount + (agent * OutputCount) + ng.GlobalID + sourceCount; break;                //Output
                                    case 2: sourceID = totalInputCount + totalOutputCount + (agent * HiddenCount) + ng.GlobalID + sourceCount; break;  //Hidden
                                }

                                switch (connectedNG.GroupType)
                                {
                                    case 0: targetID = (agent * InputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 1: targetID = totalInputCount + (agent * OutputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 2: targetID = totalInputCount + totalOutputCount + (agent * HiddenCount) + connectedNG.GlobalID + targetCout; break;
                                }

                                //-----------------Get the bias of the target node
                                   coordinates[0] = target.X; coordinates[1] = target.Y; coordinates[2] = 0.0f; coordinates[3] = 0.0f;
                                   //String s = arrayToString(coordinates);
                                   //if (weights.ContainsKey(s))
                                   //    neurons[(int)targetID].Bias = weights[s];
                                   //else
                                   {
                                       network.ClearSignals();
                                       network.SetInputSignals(coordinates);
                                       network.MultipleSteps(iterations);
                                       neurons[(int)targetID].Bias = (float)(network.GetOutputSignal(1) * weightRange);
                                       //weights.Add(s,neurons[(int)targetID].Bias);
                                   }
                                   if (ct)
                                   {
                                       neurons[(int)targetID].TimeConstant = timeConstantMin + ((((float)network.GetOutputSignal(2) + 1.0f) / 2.0f) * (timeConstantMax - timeConstantMin));
                                       System.Diagnostics.Debug.Assert(neurons[(int)targetID].TimeConstant > 0);
                                   }
                                //----------------------------

                                coordinates[0] = source.X;
                                coordinates[1] = source.Y;
                                coordinates[2] = target.X;
                                coordinates[3] = target.Y;
                                //Console.WriteLine(arrayToString(coordinates));
                                
                                //if(weights.ContainsKey(s))
                                //    output = weights[s];
                                //else
                                {
                                    network.ClearSignals();
                                    network.SetInputSignals(coordinates);
                                    network.MultipleSteps(iterations);
                                    output = network.GetOutputSignal(0);
                                    //weights.Add(s, output);
                                }
                                double leo = 0.0;

                                if (adaptiveNetwork)
                                {
                                    A = network.GetOutputSignal(2);
                                    B = network.GetOutputSignal(3);
                                    C = network.GetOutputSignal(4);
                                    D = network.GetOutputSignal(5);
                                    learningRate = network.GetOutputSignal(6);
                                }

                                if (modulatoryNet)
                                {
                                    modConnection = network.GetOutputSignal(7);
                                }
                                else
                                {
                                    modConnection = 0.0f;
                                }

                                if (useLeo)
                                {
                                    threshold = 0.0;
                                    leo = network.GetOutputSignal(2);
                                }

                                if (!useLeo || leo > 0.0)
                                    if (Math.Abs(output) > threshold)
                                    {
                                        float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                                        //if (adaptiveNetwork)
                                        //{
                                        //    //If adaptive network set weight to small value
                                        //    weight = 0.1f;
                                        //}
                                        connections.Add(new ConnectionGene(connectionCounter++, sourceID, targetID, weight, ref coordinates, A, B, C, D, modConnection, learningRate));
                                    }
                                //else
                                //{
                                //    Console.WriteLine("Not connected");
                                //}
                                targetCout++;
                            }
                            sourceCount++;
                        }
                    }

                    foreach (uint connectedTo in ng.HiveConnectedTo)
                    {
                        bool wrapAround = true;

                        for (uint agentConnect = 0; agentConnect < stackCoordinates.Count; agentConnect++)
                        {
                            //Make sure we're not making a recurrent connection on the same agent
                            //if (agentConnect == agent)
                            //    continue;
                           // else if ((agent == stackCoordinates.Count - 1 && agentConnect == 0) || (agent == 0 && agentConnect == stackCoordinates.Count - 1))
                           //     ;//agentConnect = 0;
                            if (agent != 0 && agent != stackCoordinates.Count - 1)
                                continue;

                            //if (agent == 1)
                            //    continue;

                            //if (agentConnect != 0 )
                            //    continue;

                            //Limits connections to only neighbors.  Good?
                            //if (!((agent == 0 || agentConnect >= agent - 1) && agentConnect <= agent + 1))
                            //    continue;
                            //if (agentConnect > agent + 1 || agentConnect < agent - 1)
                            //    continue;

                            if (oneWay)
                            {
                                //ONE-WAY
                                if (agentConnect > agent + 1 || agentConnect < agent)
                                    continue;
                            }

                            if (!relativeCoordinate)
                                //USE THE Z COORDINATE
                                coordinates[5] = stackCoordinates[(int)agentConnect];
                            else
                                //USE THE RELATIVE COORDINATE
                                coordinates[5] = agentConnect > agent ? 1 : -1;
                            //WRAP AROUND
                            /*if (agent == stackCoordinates.Count - 1 && agentConnect == 0)
                                coordinates[5] = 1;
                            else if (agent == 0 && agentConnect == stackCoordinates.Count - 1)
                                coordinates[5] = -1;
                             */

                            connectedNG = getNeuronGroup(connectedTo);

                            sourceCount = 0;
                            foreach (PointF source in ng.NeuronPositions)
                            {

                                //-----------------Get the bias of the source node
                               /* switch (ng.GroupType)
                                {
                                    case 0: sourceID = (agent * InputCount) + ng.GlobalID + sourceCount; break;                             //Input
                                    case 1: sourceID = totalInputCount + (agent * OutputCount) + ng.GlobalID + sourceCount; break;                //Output
                                    case 2: sourceID = totalInputCount + totalOutputCount + (agent * HiddenCount) + ng.GlobalID + sourceCount; break;  //Hidden
                                }
                                coordinates[0] = source.X; coordinates[1] = source.Y; coordinates[2] = 0.0f; coordinates[3] = 0.0f;

                                network.ClearSignals();
                                network.SetInputSignals(coordinates);
                                network.MultipleSteps(iterations);

                                neurons[(int)sourceID].Bias = (float)(network.GetOutputSignal(1) * weightRange);
                                if (ct)
                                {
                                    neurons[(int)sourceID].TimeConstant = 0.01f + ((((float)network.GetOutputSignal(2) + 1.0f) / 2.0f) * .05f);
                                    System.Diagnostics.Debug.Assert(neurons[(int)sourceID].TimeConstant > 0);
                                }*/
                                //----------------------------

                                targetCout = 0;
                                foreach (PointF target in connectedNG.NeuronPositions)
                                {
                                    /*if ((source.X != target.X))
                                    {
                                        targetCout++;
                                        continue;
                                    }*/
                                    if (/*source.X!= target.X ||*/ target.X != coordinates[4])// || source.X!= coordinates[4])
                                    {
                                        targetCout++;
                                        continue;
                                    }
                                   /* if (agent != 0 && agent != stackCoordinates.Count - 1)
                                    { 
                                        if(agentConnect != 0 && agentConnect != stackCoordinates.Count - 1)
                                        {
                                            targetCout++;
                                            continue;
                                        }
                                    }*/
                                    switch (ng.GroupType)
                                    {
                                        case 0: sourceID = (agent * InputCount) + ng.GlobalID + sourceCount; break;                             //Input
                                        case 1: sourceID = totalInputCount + (agent * OutputCount) + ng.GlobalID + sourceCount; break;                //Output
                                        case 2: sourceID = totalInputCount + totalOutputCount + (agent * HiddenCount) + ng.GlobalID + sourceCount; break;  //Hidden
                                    }

                                    switch (connectedNG.GroupType)
                                    {
                                        case 0: targetID = (agentConnect * InputCount) + connectedNG.GlobalID + targetCout; break;
                                        case 1: targetID = totalInputCount + (agentConnect * OutputCount) + connectedNG.GlobalID + targetCout; break;
                                        case 2: targetID = totalInputCount + totalOutputCount + (agentConnect * HiddenCount) + connectedNG.GlobalID + targetCout; break;
                                    }

                                    //-----------------Get the bias of the target node
                                    coordinates[0] = target.X; coordinates[1] = target.Y; coordinates[2] = 0.0f; coordinates[3] = 0.0f;
                                    //String s = arrayToString(coordinates);
                                    //if (weights.ContainsKey(s))
                                    //    neurons[(int)targetID].Bias = weights[s];
                                    //else
                                    {
                                        network.ClearSignals();
                                        network.SetInputSignals(coordinates);
                                        network.MultipleSteps(iterations);
                                        neurons[(int)targetID].Bias = (float)(network.GetOutputSignal(1) * weightRange);
                                       // weights.Add(s, neurons[(int)targetID].Bias);
                                    }
                                    if (ct)
                                    {
                                        neurons[(int)targetID].TimeConstant = timeConstantMin + ((((float)network.GetOutputSignal(2) + 1.0f) / 2.0f) * (timeConstantMax - timeConstantMin));
                                        System.Diagnostics.Debug.Assert(neurons[(int)targetID].TimeConstant > 0);
                                    }
                                    //----------------------------

                                    coordinates[0] = source.X;
                                    coordinates[1] = source.Y;
                                    coordinates[2] = target.X;
                                    coordinates[3] = target.Y;
                                    //s = arrayToString(coordinates);
                                    //if (weights.ContainsKey(s))
                                    //    output = weights[s];
                                    //else
                                    {
                                        network.ClearSignals();
                                        network.SetInputSignals(coordinates);
                                        network.MultipleSteps(iterations);
                                        output = network.GetOutputSignal(0);
                                      //  weights.Add(s, output);
                                    }

                                    double leo = 0.0;

                                    if (adaptiveNetwork)
                                    {
                                        A = network.GetOutputSignal(2);
                                        B = network.GetOutputSignal(3);
                                        C = network.GetOutputSignal(4);
                                        D = network.GetOutputSignal(5);
                                        learningRate = network.GetOutputSignal(6);
                                    }

                                    if (modulatoryNet)
                                    {
                                        modConnection = network.GetOutputSignal(7);
                                    }
                                    else
                                    {
                                        modConnection = 0.0f;
                                    }

                                    if (useLeo)
                                    {
                                        threshold = 0.0;
                                        leo = network.GetOutputSignal(2);
                                    }

                                    if (!useLeo || leo > 0.0)
                                        if (Math.Abs(output) > threshold)
                                        {
                                            float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                                            //if (adaptiveNetwork)
                                            //{
                                            //    //If adaptive network set weight to small value
                                            //    weight = 0.1f;
                                            //}
                                            connections.Add(new ConnectionGene(connectionCounter++, sourceID, targetID, weight, ref coordinates, A, B, C, D, modConnection, learningRate,true));
                                        }
                                    //else
                                    //{
                                    //    Console.WriteLine("Not connected");
                                    //}
                                    targetCout++;
                                }
                                sourceCount++;
                            }
                        }
                    }
                }
                agent++;
            }
            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }
            SharpNeatLib.NeatGenome.NeatGenome sng = new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)(totalInputCount), (int)(totalOutputCount));
            sng.networkAdaptable = adaptiveNetwork;
            sng.networkModulatory = modulatoryNet;
            return sng;
        }
      
        private NeatGenome.NeatGenome generateMultiGenomeStackES(INetwork network, List<float> stackCoordinates, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet)
        {
            uint numberOfAgents = (uint)stackCoordinates.Count;
            IActivationFunction activationFunction = HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new ConnectionGeneList((int)(numberOfAgents * (InputCount * HiddenCount) + numberOfAgents * (HiddenCount * OutputCount)));
            float[] coordinates = new float[5];
            float output;
            uint connectionCounter = 0;
            float agentDelta = 2.0f / (numberOfAgents - 1);
            int iterations = 2 * (network.TotalNeuronCount - (network.InputNeuronCount + network.OutputNeuronCount)) + 1;

            uint totalOutputCount = OutputCount * numberOfAgents;
            uint totalInputCount = InputCount * numberOfAgents;
            uint totalHiddenCount = HiddenCount * numberOfAgents;

            uint sourceCount, targetCout;
            double weightRange = HyperNEATParameters.weightRange;
            double threshold = HyperNEATParameters.threshold;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount * numberOfAgents + OutputCount * numberOfAgents + HiddenCount * numberOfAgents));

            // set up the input nodes
            for (uint a = 0; a < totalInputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }
            // set up the output nodes
            for (uint a = 0; a < totalOutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount * numberOfAgents, NeuronType.Output, activationFunction));
            }


            uint agent = 0;
            float A = 0.0f, B = 0.0f, C = 0.0f, D = 0.0f, learningRate = 0.0f, modConnection;


            List<PointF> outputNeuronPositions = getNeuronGroupByType(1);
            List<PointF> inputNeuronPositions = getNeuronGroupByType(0);

            uint hiddenCount = 0;

            foreach (float stackCoordinate in stackCoordinates)
            {
                List<PointF> hiddenNeuronPositions = new List<PointF>();
                ConnectionGeneList con = new ConnectionGeneList();
                SubstrateEvolution se = new SubstrateEvolution();
                se.generateConnections(inputNeuronPositions, outputNeuronPositions, network,
                    SubstrateEvolution.SAMPLE_WIDTH,
                    SubstrateEvolution.SAMPLE_TRESHOLD,
                    SubstrateEvolution.NEIGHBOR_LEVEL,
                    SubstrateEvolution.INCREASE_RESSOLUTION_THRESHOLD,
                    SubstrateEvolution.MIN_DISTANCE,
                    SubstrateEvolution.CONNECTION_TRESHOLD, //0.4. ConnectionThreshold
                    InputCount, OutputCount, -1.0f, -1.0f, 1.0f, 1.0f, ref con, ref hiddenNeuronPositions, stackCoordinate);

                // set up the hidden nodes
                for (uint a = 0; a < hiddenNeuronPositions.Count; a++)
                {
                    neurons.Add(new NeuronGene(hiddenCount + a + totalInputCount + totalOutputCount, NeuronType.Hidden, activationFunction));
                }



                foreach (ConnectionGene c in con)
                {
                    if (c.SourceNeuronId < InputCount)
                    {
                        c.SourceNeuronId += agent * InputCount;
                    }
                    else if (c.SourceNeuronId < InputCount + OutputCount)
                    {
                        c.SourceNeuronId = (c.SourceNeuronId - InputCount) + totalInputCount + agent * OutputCount;
                    }
                    else
                    {
                        c.SourceNeuronId = (uint)((c.SourceNeuronId - InputCount - OutputCount) + totalInputCount + totalOutputCount + hiddenCount);
                    }

                    if (c.TargetNeuronId < InputCount)
                    {
                        c.TargetNeuronId += agent * InputCount;
                    }
                    else if (c.TargetNeuronId < InputCount + OutputCount)
                    {
                        c.TargetNeuronId = (c.TargetNeuronId - InputCount) + totalInputCount + agent * OutputCount;
                    }
                    else
                    {
                        c.TargetNeuronId = (uint)((c.TargetNeuronId - InputCount - OutputCount) + totalInputCount + totalOutputCount + hiddenCount);
                    }

                    connections.Add(new ConnectionGene(connectionCounter++, c.SourceNeuronId, c.TargetNeuronId, c.Weight, ref c.coordinates, c.A, c.B, c.C, c.D, c.modConnection, c.learningRate));

                }
                hiddenCount += (uint)hiddenNeuronPositions.Count;
                agent++;

            }
            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }
            SharpNeatLib.NeatGenome.NeatGenome sng = new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)(totalInputCount), (int)(totalOutputCount));
            sng.networkAdaptable = adaptiveNetwork;
            sng.networkModulatory = modulatoryNet;
            return sng;
        }

        #endregion

        #region Generate heterogenous genomes with situational policy
        public List<NeatGenome.NeatGenome> generateGenomeStackSituationalPolicy(INetwork network, uint numberOfAgents, bool normalizeWeights, bool adaptiveNetwork, bool modNetwork, int numSig, out List<float> coords)
        {
            float signal = 0;
            List<NeatGenome.NeatGenome> genomes = new
 List<NeatGenome.NeatGenome>(numSig);
            coords = new List<float>((int)numberOfAgents);
            for (int j = 0; j < numSig; j++)
            {
                if (numSig <= 1)
                    signal = 0;
                else
                    signal = ((2.0f / (numSig - 1)) * j) + -1.0f;
                genomes.Add(generateGenomeStackSituationalPolicy(network,
 numberOfAgents, normalizeWeights, adaptiveNetwork, modNetwork, out
coords, signal));
            }

            return genomes;
        }
     
        public NeatGenome.NeatGenome generateGenomeStackSituationalPolicy(INetwork network, uint numberOfAgents, bool normalizeWeights, bool adaptiveNetwork, bool modNetwork, out List<float> coords, float signal)
        {
            coords = new List<float>();

            float coord = -1.0f;
            float delta = 2.0f / (numberOfAgents - 1);
            for (uint x = 0; x < numberOfAgents; x++)
            {
                coords.Add(coord);
                coord += delta;
            }

            return this.generateGenomeStackSituationalPolicy(network, coords,
 normalizeWeights, adaptiveNetwork, modNetwork, signal);
        }

        public NeatGenome.NeatGenome generateGenomeStackSituationalPolicy(INetwork network, List<float> stackCoordinates, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet, float signal)
        {
            uint numberOfAgents = (uint)stackCoordinates.Count;
            IActivationFunction activationFunction =
 HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new
 ConnectionGeneList((int)(numberOfAgents * (InputCount * HiddenCount) +
 numberOfAgents * (HiddenCount * OutputCount)));
            float[] coordinates = new float[5 + 1];
            float output;
            uint connectionCounter = 0;
            float agentDelta = 2.0f / (numberOfAgents - 1);
            int iterations = 2 * (network.TotalNeuronCount -
 (network.InputNeuronCount + network.OutputNeuronCount)) + 1;

            uint totalOutputCount = OutputCount * numberOfAgents;
            uint totalInputCount = InputCount * numberOfAgents;
            uint totalHiddenCount = HiddenCount * numberOfAgents;

            uint sourceCount, targetCout;
            double weightRange = HyperNEATParameters.weightRange;
            double threshold = HyperNEATParameters.threshold;

            coordinates[5] = signal;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in thisorder: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount *
 numberOfAgents + OutputCount * numberOfAgents + HiddenCount *
 numberOfAgents));

            // set up the input nodes
            for (uint a = 0; a < totalInputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input,
 ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }
            // set up the output nodes
            for (uint a = 0; a < totalOutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount *
 numberOfAgents, NeuronType.Output, activationFunction));
            }
            // set up the hidden nodes
            for (uint a = 0; a < totalHiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + InputCount *
 numberOfAgents + OutputCount * numberOfAgents, NeuronType.Hidden,
 activationFunction));
            }

            uint agent = 0;
            float A = 0.0f, B = 0.0f, C = 0.0f, D = 0.0f, learningRate
 = 0.0f, modConnection;

            foreach (float stackCoordinate in stackCoordinates)
            {
                coordinates[4] = stackCoordinate;
                uint sourceID = uint.MaxValue, targetID = uint.MaxValue;
                NeuronGroup connectedNG;

                foreach (NeuronGroup ng in neuronGroups)
                {
                    foreach (uint connectedTo in ng.ConnectedTo)
                    {
                        connectedNG = getNeuronGroup(connectedTo);

                        sourceCount = 0;
                        foreach (PointF source in ng.NeuronPositions)
                        {

                            //-----------------Get the bias of the source node
                            switch (ng.GroupType)
                            {
                                case 0: sourceID = (agent *
 InputCount) + ng.GlobalID + sourceCount; break;
                                //Input
                                case 1: sourceID = totalInputCount +
 (agent * OutputCount) + ng.GlobalID + sourceCount; break;
                                //Output
                                case 2: sourceID = totalInputCount +
 totalOutputCount + (agent * HiddenCount) + ng.GlobalID + sourceCount;
                                    break;  //Hidden
                            }
                            coordinates[0] = source.X; coordinates[1]
 = source.Y; coordinates[2] = 0.0f; coordinates[3] = 0.0f;

                            network.ClearSignals();
                            network.SetInputSignals(coordinates);
                            network.MultipleSteps(iterations);

                            neurons[(int)sourceID].Bias =
 (float)(network.GetOutputSignal(1) * weightRange);
                            //----------------------------

                            targetCout = 0;
                            foreach (PointF target in
 connectedNG.NeuronPositions)
                            {
                                switch (ng.GroupType)
                                {
                                    case 0: sourceID = (agent *
 InputCount) + ng.GlobalID + sourceCount; break;
                                    //Input
                                    case 1: sourceID = totalInputCount
 + (agent * OutputCount) + ng.GlobalID + sourceCount; break;
                                    //Output
                                    case 2: sourceID = totalInputCount
 + totalOutputCount + (agent * HiddenCount) + ng.GlobalID +
 sourceCount; break;  //Hidden
                                }

                                switch (connectedNG.GroupType)
                                {
                                    case 0: targetID = (agent *
 InputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 1: targetID = totalInputCount
 + (agent * OutputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 2: targetID = totalInputCount
 + totalOutputCount + (agent * HiddenCount) + connectedNG.GlobalID +
 targetCout; break;
                                }

                                coordinates[0] = source.X;
                                coordinates[1] = source.Y;
                                coordinates[2] = target.X;
                                coordinates[3] = target.Y;

                                network.ClearSignals();
                                network.SetInputSignals(coordinates);
                                network.MultipleSteps(iterations);
                                output = network.GetOutputSignal(0);

                                double leo = 0.0;

                                if (adaptiveNetwork)
                                {
                                    A = network.GetOutputSignal(2);
                                    B = network.GetOutputSignal(3);
                                    C = network.GetOutputSignal(4);
                                    D = network.GetOutputSignal(5);
                                    learningRate = network.GetOutputSignal(6);
                                }

                                if (modulatoryNet)
                                {
                                    modConnection = network.GetOutputSignal(7);
                                }
                                else
                                {
                                    modConnection = 0.0f;
                                }

                                if (useLeo)
                                {
                                    threshold = 0.0;
                                    leo = network.GetOutputSignal(2);
                                }

                                if (!useLeo || leo > 0.0)
                                    if (Math.Abs(output) > threshold)
                                    {
                                        float weight =
 (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) *
 weightRange * Math.Sign(output));
                                        //if (adaptiveNetwork)
                                        //{
                                        //    //If adaptive networkset weight to small value
                                        //    weight = 0.1f;
                                        //}
                                        connections.Add(new
 ConnectionGene(connectionCounter++, sourceID, targetID, weight, ref
coordinates, A, B, C, D, modConnection, learningRate));
                                    }
                                //else
                                //{
                                //    Console.WriteLine("Not connected");
                                //}
                                targetCout++;
                            }
                            sourceCount++;
                        }
                    }
                }
                agent++;
            }
            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }
            SharpNeatLib.NeatGenome.NeatGenome sng = new
 SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections,
 (int)(totalInputCount), (int)(totalOutputCount));
            sng.networkAdaptable = adaptiveNetwork;
            sng.networkModulatory = modulatoryNet;
            return sng;
        }
     
        #endregion
    }
}
