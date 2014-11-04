using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.NeatGenome;

namespace Chromaria.SharpNeatLib.CPPNs
{
    public class SubstrateEvolution
    {
        public static int MAX_ITER_STEPS = 1;           //TODO change for other experiments
        public static int NETWORK_ITERATIONS = 3;
        public static float WEIGHT_RANGE = 3.0f;
        public static float SAMPLE_WIDTH = 0.5f;   //0.4 is better
        public static float SAMPLE_TRESHOLD = 0.03f;     // 0.03
        public static float CONNECTION_TRESHOLD = 0.0f;//was 0.6
        public static float NEIGHBOR_LEVEL = 0.3f; //was 0.1
        public static float INCREASE_RESSOLUTION_THRESHOLD = 0.03f; //0.03 //TODO add if higher than 0.9 than up to 0.1... and so on...
        public static float MIN_DISTANCE = 0.4f;        //was 0.4

        public class ExpressPoint
        {
            public float x1, y1, x2, y2;
            public float fixedx, fixedy;
            public float variance, activationLevel;
            //TODO use refernces?
            public ExpressPoint(float startx, float starty, float x1, float x2, float y1, float y2,
                                float _variance, float _activationLevel)
            {
                activationLevel = _activationLevel;
                this.x1 = x1;
                this.fixedx = startx;
                this.fixedy = starty;
                this.y1 = y1;
                this.x2 = x2;
                this.y2 = y2;
                variance = _variance;
            }
        }

        class Rect : IComparable<Rect>
        {
            public float x1, y1, x2, y2;

            public Rect parent;
            public float activationLevel;
            public float thr;
            public bool visited;
            public List<Rect> childs;
            public byte recPos;
            public float fixedx, fixedy;
            public bool from; //are the fixed locations target or source
            public INetwork genome; //reference to the CPPN

            float stacknumber; //for multiagent purposes only

            float[] coordinates;

            public Rect(float fixedx, float fixedy, bool from, Rect _parent, byte recPos, float _x1, float _y1, float _x2, float _y2, INetwork genome, float stackNumber)
            {
                if (stacknumber == float.NaN) //Single-agent
                {
                    coordinates = new float[4];
                }
                else   //multi-agent
                {
                    //additional z CPPN input
                    coordinates = new float[5];
                }
                this.stacknumber = stackNumber;
                this.genome = genome;
                this.fixedx = fixedx;
                this.fixedy = fixedy;
                this.recPos = recPos;
                this.from = from;

                activationLevel = float.NaN;
                visited = false;
                childs = new List<Rect>();
                parent = _parent;

                if (parent != null)
                {
                    parent.childs.Add(this);
                }

                if (_x1 < _x2)
                {
                    x1 = _x1;
                    x2 = _x2;
                }
                else
                {
                    x2 = _x1;
                    x1 = _x2;
                }

                if (_y1 < _y2)
                {
                    y1 = _y1;
                    y2 = _y2;
                }
                else
                {
                    y2 = _y1;
                    y1 = _y2;
                }

            }

            private void nextPos(out float new1, out float new2, ref float v1, ref float v2, bool direction)
            {
           
                if (!direction)
                {
                    new1 = v1 - Math.Abs(v1 - v2);
                    new2 = v1;
                }
                else
                {
                    new2 = v2 + Math.Abs(v1 - v2);
                    new1 = v2;
                }
            }

            public float maxHighestDif(int dimension, bool direction, ref float maxValue)
            {
                // Rect d = this;
                //   float maxDif = 0.0f

                float v;

                if (float.IsNaN(activationLevel))
                {
                    //activationLevel = queryCPPN((x1 + x2) / 2.0f, (y1 + y2) / 2.0f, (z1 + z2) / 2.0f, (w1 + w2) / 2.0f);
                    Console.WriteLine("ERROR :" + x1 + " " + x2 + " " + y1 + " " + y2);

                }

                float newx1 = x1, newx2 = x2, newy1 = y1, newy2 = y2;
                switch (dimension)
                {
                    case 0:
                        nextPos(out newx1, out newx2, ref x1, ref x2, direction);
                        break;
                    case 1:
                        nextPos(out newy1, out newy2, ref y1, ref y2, direction);
                        break;
                }

                float tx, ty, tz, tw;
                tx = (newx2 + newx1) / 2.0f;
                ty = (newy2 + newy1) / 2.0f; //(newx2 + newx1) / 2.0f; //(newy2 + newy1) / 2.0f;  //BUG was  
                if ((tx < -1.0) || (tx > 1.0) || (ty < -1.0) || (ty > 1.0))
                {
                    //Outside of bounds
                    return 0.0f;
                }
                float tmp;
                //v = queryCPPN(fixedx, fixedy, tx, ty);
                if (from)
                {
                    v = queryCPPN(fixedx, fixedy, tx, ty, out tmp);
                }
                else
                {
                    v = queryCPPN(tx, ty, fixedx, fixedy, out tmp);
                }

                if (Math.Abs(v) > Math.Abs(maxValue)) maxValue = v;

                return Math.Abs(v - activationLevel);
            }

            private float neighborDifference(int dimension, ref float maxValue)
            {
                return Math.Min(maxHighestDif(dimension, true, ref maxValue), maxHighestDif(dimension, false, ref maxValue));
            }

            public void createTree(double minDistance)
            {
                List<Rect> rectList = new List<Rect>();
                List<Rect> rectToAdd = new List<Rect>();
                float midx, midy;
                rectList.Add(this);
                //  minDistance = 0.25;

                float x1, y1, x2, y2;

                while (true)    //TODO replace. Do not calculate distance for every rectangle
                {
                    rectToAdd.Clear();
                    if ((Math.Abs(rectList[0].x1 - rectList[0].x2) > minDistance))// || (sampleTreshold != -1.0f && dif > sampleTreshold))
                    {
                        foreach (Rect rect in rectList)
                        {
                            midx = (rect.x1 + rect.x2) / 2.0f;
                            midy = (rect.y1 + rect.y2) / 2.0f;

                            rectToAdd.Add(new Rect(fixedx, fixedy, from, rect, 0x0, rect.x1, rect.y1, midx, midy, rect.genome, rect.stacknumber));
                            rectToAdd.Add(new Rect(fixedx, fixedy, from, rect, 0x1, rect.x1, rect.y2, midx, midy, rect.genome, rect.stacknumber));
                            rectToAdd.Add(new Rect(fixedx, fixedy, from, rect, 0x2, rect.x2, rect.y1, midx, midy, rect.genome, rect.stacknumber));
                            rectToAdd.Add(new Rect(fixedx, fixedy, from, rect, 0x3, rect.x2, rect.y2, midx, midy, rect.genome, rect.stacknumber));
                            //rect.visited = true;
                        }

                        rectList.Clear();
                        foreach (Rect addRect in rectToAdd)
                        {
                            x1 = (addRect.x2 + addRect.x1) / 2.0f;
                            y1 = (addRect.y2 + addRect.y1) / 2.0f;

                            if (from)
                            {
                                addRect.activationLevel = queryCPPN(fixedx, fixedy, x1, y1, out addRect.thr);
                            }
                            else
                            {
                                addRect.activationLevel = queryCPPN(x1, y1, fixedx, fixedy, out addRect.thr);
                            }
                            rectList.Add(addRect);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                //    Console.WriteLine("Number initial rectangles: " + rectList.Count);

                //foreach (Rect rect in rectList)
                //{
                //    x1 = (rect.x2 + rect.x1) / 2.0f; y1 = (rect.y2 + rect.y1) / 2.0f;
                //    x2 = (rect.z1 + rect.z2) / 2.0f; y2 = (rect.w1 + rect.w2) / 2.0f;

                //    float middle = queryCPPN(x1, y1, x2, y2);
                //    rect.activationLevel = middle;
                //}
            }


            public void addPoints(ref List<ExpressPoint> list, ref float treshold, ref float neighborLevel, ref float increaseResolutionThreshold,
                ref float minDistance)
            {
                float childVariance, tmp;


                float parentVariance = this.variance(out tmp);

                //TODO make resolution easier adjustable
                if ((parentVariance > increaseResolutionThreshold) && (Math.Abs(x1 - x2) > MIN_DISTANCE))//(1.0f - parentVariance)))
                {
                    foreach (Rect r in childs)
                    {
                        if (r.childs.Count == 0)
                        {
                            //   Console.WriteLine(Math.Abs(r.x1 - r.x2) / 2.0);

                            r.createTree(Math.Abs(r.x1 - r.x2) / 2.0);//TODO Calculate Abs only once
                        }
                    }
                }

                foreach (Rect r in childs)
                {
                    childVariance = r.variance(out tmp);


                    if (childVariance >= treshold)  //greater or greater and equal?
                    {
                        r.addPoints(ref list, ref treshold, ref neighborLevel, ref increaseResolutionThreshold, ref minDistance);
                    }
                    else
                    {
                        //TODO check if this is doing the right thing
                        bool add = false;
                        float maxValue = 0;
                        for (int b = 0; b < 2; b++)
                        {
                            // Console.WriteLine(b);
                            if (r.neighborDifference(b, ref maxValue) > neighborLevel)
                            {
                                add = true;
                                break;
                            }
                        }
                        if (add)
                        {
                            if (Math.Abs(r.activationLevel) > CONNECTION_TRESHOLD)//&& ((dist/2.5f) < Math.Abs(r.thr)))//&& (r.y2>=r.y1))//(dist / 2.0f + 0.5))
                            {
                                ExpressPoint p = new ExpressPoint(r.fixedx, r.fixedy, r.x1, r.x2, r.y1, r.y2, 1.0f, r.activationLevel);
                                list.Add(p);
                            }
                        }

                    }

                }
            }

            public Rect getChild(byte pos)
            {
                foreach (Rect r in childs)
                {
                    if (r.recPos == pos) return r;
                }
                return null;
            }

            public Rect neighbor(byte mask, bool up)
            {
                if (parent == null) return null;
                if (up)
                {
                    byte neighborBits = (byte)(recPos | mask);
                    if ((this.recPos & mask) != mask)//RectPosition.bl)
                    {
                        return parent.getChild(neighborBits);
                    }
                    Rect u = parent.neighbor(mask, up);// northNeighbor();
                    if (u == null || u.childs.Count == 0) return u;
                    return u.getChild((byte)(recPos ^ mask));
                }
                else
                {
                    byte neighborBits = (byte)(recPos ^ mask);

                    if ((this.recPos & mask) == mask)//RectPosition.bl)
                    {
                        return parent.getChild(neighborBits);
                    }
                    Rect u = parent.neighbor(mask, up);// northNeighbor();
                    if (u == null || u.childs.Count == 0) return u;
                    return u.getChild((byte)(recPos | mask));
                }
            }

            private void getPoints(ref List<float> l)
            {
                if (this.childs.Count > 0.0f)
                {
                    foreach (Rect rect in childs)
                    {
                        rect.getPoints(ref l);
                    }
                }
                else
                {
                    l.Add(activationLevel);
                }
            }

            public float variance(out float med)
            {
                if (childs.Count == 0)
                {
                    med = this.activationLevel;
                    return 0.0f;
                }

                List<float> l = new List<float>();
                getPoints(ref l);

                float m = 0.0f, v = 0.0f;
                foreach (float f in l)
                {
                    m += f;
                }
                m /= l.Count;
                med = m;
                foreach (float f in l)
                {
                    v += (float)Math.Pow(f - m, 2);
                }
                v /= l.Count;
                return v;
            }

            public int CompareTo(Rect obj)
            {
                return activationLevel.CompareTo(obj.activationLevel);
            }

            public float queryCPPN(float x1, float y1, float x2, float y2, out float thrs)
            {
                coordinates[0] = x1;
                coordinates[1] = y1;
                coordinates[2] = x2;
                coordinates[3] = y2;

                if (!stacknumber.Equals(float.NaN))
                {
                    coordinates[4] = stacknumber;   //z-coordinate. only necessary for multiagent experiments
                }

                genome.ClearSignals();
                genome.SetInputSignals(coordinates);
                genome.MultipleSteps(NETWORK_ITERATIONS);  //TODO query CPPN based on depth

                thrs = 0.0f;

                return genome.GetOutputSignal(0);  //use weight
            }
        }

        public void generateConnections(List<PointF> inputNeuronPositions, List<PointF> outputNeuronPositions,
            INetwork genome, float sampleWidth, float sampleThreshold, float neighborLevel,
                                                float increaseResolutionThreshold, float minDistance,
                                                float connectionThreshold,
                                                uint inputCount, uint outputCount,
                                                float minX, float minY, float maxX, float maxY,
                                                ref  ConnectionGeneList connections, ref List<PointF> hiddenNeurons) {
			generateConnections(inputNeuronPositions,outputNeuronPositions,genome,sampleWidth,
			                    sampleThreshold,neighborLevel,increaseResolutionThreshold,minDistance,
			                    connectionThreshold,inputCount,outputCount,minX,minY,maxX,maxY,ref connections,ref hiddenNeurons,float.NaN);
		}
        
		public void generateConnections(List<PointF> inputNeuronPositions, List<PointF> outputNeuronPositions,
            INetwork genome, float sampleWidth, float sampleThreshold, float neighborLevel,
                                                float increaseResolutionThreshold, float minDistance,
                                                float connectionThreshold,
                                                uint inputCount, uint outputCount,
                                                float minX, float minY, float maxX, float maxY,
                                                ref  ConnectionGeneList connections, ref List<PointF> hiddenNeurons, float stackNumber)
        {
           // SubstrateEvolution.stackNumber = stackNumber; //TODO hacky
          //  totalLength = 0.0f;
            List<Rect> rectList = new List<Rect>();

            //minX = -1.0f;
            //minY = -1.0f;
            //maxX = 1.0f;
            //maxY = 1.0f;

            List<ExpressPoint> _connections = new List<ExpressPoint>();

            List<PointF> hiddenPos = new List<PointF>();

            List<PointF> pointToAdd = new List<PointF>();
            float targetX, targetY;


            double output;
            int sourceIndex, targetIndex, neuronCount = 0;
            uint counter = 0;
            float weight;
            float[] connectionCoordinates = new float[5];

            connectionCoordinates[4] = stackNumber;

            //CONNECTION DIRECTLY FROM INPUT NODES
            List<PointF> tabuList = new List<PointF>();
            foreach (PointF input in inputNeuronPositions)
            {
                Rect startRec = new Rect(input.X, input.Y, true, null, 0, minX, minY, maxX, maxY, genome, stackNumber);
                startRec.createTree(sampleWidth);
                startRec.addPoints(ref _connections, ref sampleThreshold, ref neighborLevel, ref increaseResolutionThreshold, ref minDistance);

                foreach (ExpressPoint p in _connections)
                {
                    targetX = (p.x1 + p.x2) / 2.0f;
                    targetY = (p.y1 + p.y2) / 2.0f;

                    PointF newp = new PointF(targetX, targetY);
                    if (!hiddenPos.Contains(newp))
                    {
                        hiddenPos.Add(newp);
                        tabuList.Add(newp);
                    }

                }
            }
            foreach (ExpressPoint t in _connections)
            {
                connectionCoordinates[0] = t.fixedx;
                connectionCoordinates[1] = t.fixedy;
                connectionCoordinates[2] = (float)(t.x1 + t.x2) / 2.0f;
                connectionCoordinates[3] = (float)(t.y1 + t.y2) / 2.0f;

                //        double dist = Math.Sqrt(Math.Pow(connectionCoordinates[0] - connectionCoordinates[2], 2) +
                //Math.Pow(connectionCoordinates[1] - connectionCoordinates[3], 2));

                if (float.IsNaN(t.activationLevel))
                {
                    Console.WriteLine("Normally this shouldn't happen");
                    return;
                    //  
                }
                else
                {
                    output = t.activationLevel;
                }

                //!remove
                //recurrent = (connectionCoordinates[0] == connectionCoordinates[2]) && (connectionCoordinates[1] == connectionCoordinates[3]);
                //
                //   if ((Math.Abs(output) > connectionThreshold)) //&& (pcount % 10 ==0))
                //  {
                PointF source = new PointF(connectionCoordinates[0], connectionCoordinates[1]);
                PointF target = new PointF(connectionCoordinates[2], connectionCoordinates[3]);


                //connectionList.Add(new Connection(x1, y1, x2, y2));

                sourceIndex = inputNeuronPositions.IndexOf(source);            //TODO change. computationally expensive
                if (sourceIndex == -1) //!hiddenNeurons.Contains(source)) 
                {
                    Console.WriteLine("This shouldn't happen.");
                    sourceIndex = inputNeuronPositions.Count;
                    // hiddenNeurons.Add(source);
                    //  neuronCount++;
                }

                targetIndex = hiddenNeurons.IndexOf(target);
                if (targetIndex == -1) //!hiddenNeurons.Contains(target)) hiddenNeurons.Add(target);
                {
                    targetIndex = hiddenNeurons.Count;
                    hiddenNeurons.Add(target);
                    neuronCount++;
                }

                weight = (float)(((Math.Abs(output) - (connectionThreshold)) / (1 - connectionThreshold)) * WEIGHT_RANGE * Math.Sign(output));
                //if (weight > 0.0) weight = 1.0f;
                //else weight = -1.0f;

                connections.Add(new ConnectionGene(counter++, (uint)(sourceIndex), (uint)(targetIndex + inputCount + outputCount), weight, ref connectionCoordinates, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f));
                //   }

            }
            //return;//! remove
            _connections.Clear();
            //**************************************
            //Hidden to Hidden
            for (int step = 0; step < MAX_ITER_STEPS; step++)
            {
                pointToAdd.Clear();
                foreach (PointF hiddenP in hiddenPos)
                {
                    Rect startRec = new Rect(hiddenP.X, hiddenP.Y, true, null, 0, minX, minY, maxX, maxY, genome, stackNumber);
                    startRec.createTree(sampleWidth);
                    startRec.addPoints(ref _connections, ref sampleThreshold, ref neighborLevel, ref increaseResolutionThreshold, ref minDistance);

                    foreach (ExpressPoint p in _connections)
                    {
                        //     double dist = Math.Sqrt(Math.Pow(p.x1 - p.x2, 2) + Math.Pow(p.y1 - p.y2, 2));

                        // connectionThreshold*

                        //  if (Math.Abs(p.activationLevel) > connectionThreshold)//(dist / 2.5f + 0.5))
                        //  {
                        targetX = (p.x1 + p.x2) / 2.0f;
                        targetY = (p.y1 + p.y2) / 2.0f;
                        PointF newp = new PointF(targetX, targetY);
                        if (!tabuList.Contains(newp))
                        {
                            pointToAdd.Add(newp);
                            tabuList.Add(newp);
                        }
                        //  }
                        //  if (targetY>input.Y)
                        //      
                    }
                }
                hiddenPos.Clear();
                if (pointToAdd.Count == 0) break;
                hiddenPos.AddRange(pointToAdd);
            }


            foreach (ExpressPoint t in _connections)
            {
                connectionCoordinates[0] = t.fixedx;
                connectionCoordinates[1] = t.fixedy;
                connectionCoordinates[2] = (float)(t.x1 + t.x2) / 2.0f;
                connectionCoordinates[3] = (float)(t.y1 + t.y2) / 2.0f;

                //         double dist = Math.Sqrt(Math.Pow(connectionCoordinates[0] - connectionCoordinates[2], 2) +
                // Math.Pow(connectionCoordinates[1] - connectionCoordinates[3], 2));

                if (float.IsNaN(t.activationLevel))
                {
                    Console.WriteLine("Normally this shouldn't happen");
                    return;
                    //  
                }
                else
                {
                    output = t.activationLevel;
                }

                //  if ((Math.Abs(output) > connectionThreshold)) //&& (pcount % 10 ==0))
                // {
                PointF source = new PointF(connectionCoordinates[0], connectionCoordinates[1]);
                PointF target = new PointF(connectionCoordinates[2], connectionCoordinates[3]);
                //connectionList.Add(new Connection(x1, y1, x2, y2));

                sourceIndex = hiddenNeurons.IndexOf(source);            //TODO change. computationally expensive
                if (sourceIndex == -1) //!hiddenNeurons.Contains(source)) 
                {
                    sourceIndex = hiddenNeurons.Count;
                    hiddenNeurons.Add(source);
                    neuronCount++;
                }

                targetIndex = hiddenNeurons.IndexOf(target);
                if (targetIndex == -1) //!hiddenNeurons.Contains(target)) hiddenNeurons.Add(target);
                {
                    targetIndex = hiddenNeurons.Count;
                    hiddenNeurons.Add(target);
                    neuronCount++;
                }

                weight = (float)(((Math.Abs(output) - (connectionThreshold)) / (1 - connectionThreshold)) * WEIGHT_RANGE * Math.Sign(output));
                //if (weight > 0.0) weight = 1.0f;
                //else weight = -1.0f;

                connections.Add(new ConnectionGene(counter++, (uint)(sourceIndex + inputCount + outputCount), (uint)(targetIndex + inputCount + outputCount), weight, ref connectionCoordinates, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f));
                //   }
            }

            _connections.Clear();

            //CONNECT TO OUTPUT
            foreach (PointF outputPos in outputNeuronPositions)
            {
                Rect startRec = new Rect(outputPos.X, outputPos.Y, false, null, 0, minX, minY, maxX, maxY, genome, stackNumber);
                startRec.createTree(sampleWidth);
                startRec.addPoints(ref _connections, ref sampleThreshold, ref neighborLevel, ref increaseResolutionThreshold, ref minDistance);

                //foreach (ExpressPoint p in _connections)
                //{
                //    targetX = (p.x1 + p.x2) / 2.0f;
                //    targetY = (p.y1 + p.y2) / 2.0f;
                //    //  if (targetY>input.Y)
                //    inputPos.Add(new PointF(targetX, targetY));
                //}
            }
            //GO DEEPER
            foreach (ExpressPoint t in _connections)
            {
                connectionCoordinates[0] = (float)(t.x1 + t.x2) / 2.0f;
                connectionCoordinates[1] = (float)(t.y1 + t.y2) / 2.0f;
                connectionCoordinates[2] = t.fixedx;
                connectionCoordinates[3] = t.fixedy;

                //          double dist = Math.Sqrt(Math.Pow(connectionCoordinates[0] - connectionCoordinates[2], 2) +
                // Math.Pow(connectionCoordinates[1] - connectionCoordinates[3], 2));

                if (float.IsNaN(t.activationLevel))
                {
                    Console.WriteLine("Normally this shouldn't happen");
                    return;
                    //  
                }
                else
                {
                    output = t.activationLevel;
                }

                //!remove
                //recurrent = (connectionCoordinates[0] == connectionCoordinates[2]) && (connectionCoordinates[1] == connectionCoordinates[3]);
                //
                //    if ((Math.Abs(output) > connectionThreshold)) //&& (pcount % 10 ==0))
                //   {
                PointF source = new PointF(connectionCoordinates[0], connectionCoordinates[1]);
                PointF target = new PointF(connectionCoordinates[2], connectionCoordinates[3]);
                //connectionList.Add(new Connection(x1, y1, x2, y2));

                sourceIndex = hiddenNeurons.IndexOf(source);            //TODO change. computationally expensive

                //only connect if hidden neuron already exists
                if (sourceIndex != -1)
                {
                    if (sourceIndex == -1) //!hiddenNeurons.Contains(source)) 
                    {
                        //IF IT DOES NOT EXIST WE COULD POTENTIALLY STOP HERE
                        sourceIndex = hiddenNeurons.Count;
                        hiddenNeurons.Add(source);
                        neuronCount++;
                    }

                    targetIndex = outputNeuronPositions.IndexOf(target);
                    if (targetIndex == -1) //!hiddenNeurons.Contains(target)) hiddenNeurons.Add(target);
                    {
                        Console.WriteLine("SubstrateEvolution: This shouldn't happen");
                        //targetIndex = hiddenNeurons.Count;
                        //hiddenNeurons.Add(target);
                        //neuronCount++;
                    }

                    weight = (float)(((Math.Abs(output) - (connectionThreshold)) / (1 - connectionThreshold)) * WEIGHT_RANGE * Math.Sign(output));
                    //if (weight > 0.0) weight = 1.0f;
                    //else weight = -1.0f;

                    connections.Add(new ConnectionGene(counter++, (uint)(sourceIndex + inputCount + outputCount), (uint)(targetIndex + inputCount), weight, ref connectionCoordinates, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f));
                }
                // }

            }

        }
    }
}
