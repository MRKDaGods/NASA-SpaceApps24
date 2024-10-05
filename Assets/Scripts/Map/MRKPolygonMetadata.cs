using System;
using System.Collections.Generic;
using UnityEngine.Video;

namespace MRK
{
    [Serializable]
    public class MRKPolygonMetadata
    {
        public int Id;
        public string Name;
        public List<int> CustomFixupData;

        // city stuff
        public List<VideoClip> Videos;

        public string Population;
        public string Area;
        public float Urban, Rural;

        public string Overcrowding;
        public int NumCities;

        public string YrPrec;
        public string Humidity;
        public string SolarRadiation;
    }
}
