using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace MRK
{
    [Serializable]
    public class HeatmapNode
    {
        public Vector2d LatLng;
        public float Intensity = -100f;
        public float Radius = -100f;
    }

    [Serializable]
    public class HeatmapData
    {
        public Button Button;
        public List<HeatmapNode> Nodes;
    }
}
