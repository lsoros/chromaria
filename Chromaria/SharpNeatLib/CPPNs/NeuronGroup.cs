using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Chromaria.SharpNeatLib.CPPNs
{
    public class NeuronGroup
    {
        public int GroupID { get; set; }
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float EndY { get; set; }
        public float EndX { get; set; }
        public uint DX { get; set; }
        public uint DY { get; set; }
        public List<int> ConnectedTo { get; set; }
        public List<int> HiveConnectedTo { get; set; }
        public int GroupType { get; set; }          //Input, Output, Hidden
        public List<PointF> NeuronPositions { get; set; }
        public uint GlobalID { get; set; }

        public NeuronGroup(float startX, float startY, float endX, float endY, uint dx, uint dy, int groupID, int groupType, uint globalID)
        {
            List<int> connectedTo = new List<int>();
            HiveConnectedTo = new List<int>();
            GroupType = groupType;
            ConnectedTo = connectedTo;
            GroupID = groupID;
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
            DX = dx;
            DY = dy;
            GlobalID = globalID;
            //Generate neuron positions
            generateNodePositions();
        }

        public void generateNodePositions()
        {
            NeuronPositions = new List<PointF>();
            float deltaX = (EndX - StartX) / (float)DX;
            float deltaY = (EndY - StartY) / (float)DY;

            float ypos, xpos;

            if (DY == 1)
            {
                ypos = StartY + (deltaY / 2.0f); ;
            }
            else
            {
                ypos = StartY;
                deltaY = (EndY - StartY) / (DY - 1.0f);
            }

            for (int e = 0; e < DY; e++)
            {
                if (DX == 1)
                {
                    xpos = StartX + (deltaX / 2.0f);
                }
                else
                {
                    xpos = StartX;
                    deltaX = (EndX - StartX) / (DX - 1.0f);
                }

                for (int i = 0; i < DX; i++)
                {
                    NeuronPositions.Add(new PointF(xpos, ypos));
                    xpos += deltaX;
                }
                ypos += deltaY;
            }
        }
    }
}
