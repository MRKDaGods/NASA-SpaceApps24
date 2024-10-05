using Codice.Client.BaseCommands;
using MRK;
using MRK.UI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EGRLanguageUpdater : MonoBehaviour {
    [MenuItem("EGR/Update Language")]
    static void Main() {
        TextAsset txt = Resources.Load<TextAsset>($"Lang/{EGRLanguage.English}");
        Dictionary<int, string> strings = new Dictionary<int, string>();
        EGRLanguageManager.ParseWithOccurence(txt, strings, true);

        using (FileStream fstream = new FileStream($@"{Application.dataPath}\Scripts\Localization\EGRLanguageData.cs", FileMode.Create))
        using (StreamWriter writer = new StreamWriter(fstream)) {
            writer.WriteLine("namespace MRK {\n\tpublic enum EGRLanguageData {");

            static string fixStr(string s) {
                string chars = "!@#$%^&*()-+=~`'\":;/.,><[]{}|\\ ?";
                foreach (char c in chars)
                    s = s.Replace(c, '_');

                return s;
            }

            foreach (KeyValuePair<int, string> pair in strings) {
                writer.WriteLine($"\t\t//{pair.Value}");
                writer.WriteLine($"\t\t{fixStr(pair.Value)} = {pair.Key},\n");
            }

            writer.WriteLine("\t\t__LANG_DATA_MAX");
            writer.WriteLine("\t}\n}");

            writer.Close();
        }
    }

    [MenuItem("EGR/SetCityData")]
    static void SetCityData()
    {
        var data = "n:1\r\np:5.7M\r\na:212,112KM<sup>2</sup>\r\nu:70\r\nr:30\r\no:1.23\r\nnc:11\r\ny:14.02mm/mon\r\n\r\nn:3\r\np:7M\r\na:9,826KM<sup>2</sup>\r\nu:20\r\nr:80\r\no:1.15\r\nnc:18\r\ny:15.5mm/mon\r\n\r\nn:4\r\np:3.7M\r\na:3,437KM<sup>2</sup>\r\nu:21\r\nr:79\r\no:1.14\r\nnc:14\r\ny:12mm/mon\r\n\r\nn:5\r\np:7.1M\r\na:3,500KM<sup>2</sup>\r\nu:25\r\nr:75\r\no:1.08\r\nnc:20\r\ny:2.21mm/mon\r\n\r\nn:6\r\np:1.6M\r\na:1,029KM<sup>2</sup>\r\nu:38\r\nr:62\r\no:1.12\r\nnc:11\r\ny:8.66mm/mon\r\n\r\nn:7\r\np:0.8M\r\na:1,345KM<sup>2</sup>\r\nu:50\r\nr:50\r\no:1.23\r\nnc:4\r\ny:8.83mm/mon\r\n\r\nn:8\r\np:0.5M\r\na:27,574KM<sup>2</sup>\r\nu:50\r\nr:50\r\no:1.25\r\nnc:8\r\ny:5.08mm/mon\r\n\r\nn:9\r\np:5.5M\r\na:1,942KM<sup>2</sup>\r\nu:29\r\nr:71\r\no:1.09\r\nnc:8\r\ny:11.2mm/mon\r\n\r\nn:10\r\np:4.8M\r\na:2,543KM<sup>2</sup>\r\nu:19\r\nr:81\r\no:1.16\r\nnc:9\r\ny:0.24mm/mon\r\n\r\nn:11\r\np:6.2M\r\na:1,124KM<sup>2</sup>\r\nu:45\r\nr:55\r\no:1.13\r\nnc:12\r\ny:6mm/mon\r\n\r\nn:12\r\np:8M\r\na:4,180KM<sup>2</sup>\r\nu:21\r\nr:79\r\no:1.13\r\nnc:19\r\ny:10.1mm/mon\r\n\r\nn:13\r\np:1.4M\r\na:5,066KM<sup>2</sup>\r\nu:45\r\nr:55\r\no:1.18\r\nnc:8\r\ny:1.27mm/mon\r\n\r\nn:15\r\np:4.1M\r\na:6,068KM<sup>2</sup>\r\nu:22\r\nr:78\r\no:1.26\r\nnc:7\r\ny:2.7mm/mon\r\n\r\nn:17\r\np:0.8M\r\na:17,840KM<sup>2</sup>\r\nu:100\r\nr:0\r\no:1.17\r\nnc:3\r\ny:1.7mm/mon\r\n\r\nn:18\r\np:0.1M\r\na:33,140KM<sup>2</sup>\r\nu:73\r\nr:27\r\no:1.39\r\nnc:10\r\ny:1.18\r\n\r\nn:19\r\np:3.6M\r\na:1,322KM<sup>2</sup>\r\nu:25\r\nr:75\r\no:1.25\r\nnc:9\r\ny:1.52mm/mon\r\n\r\nn:20\r\np:6.4M\r\na:32,279KM<sup>2</sup>\r\nu:22\r\nr:78\r\no:1.25\r\nnc:12\r\ny:0.15mm/mon\r\n\r\nn:21\r\np:0.3M\r\na:440,098KM<sup>2</sup>\r\nu:55\r\nr:45\r\no:1.11\r\nnc:6\r\ny:0mm/mon\r\n\r\nn:22\r\np:5.1M\r\na:26,600KM<sup>2</sup>\r\nu:27\r\nr:73\r\no:1.37\r\nnc:13\r\ny:0.07mm/mon\r\n\r\nn:23\r\np:0.4M\r\na:203,685KM<sup>2</sup>\r\nu:96\r\nr:4\r\no:1.24\r\nnc:7\r\ny:1.63mm/mon\r\n\r\nn:24\r\np:5.8M\r\na:1,547KM<sup>2</sup>\r\nu:21\r\nr:79\r\no:1.4\r\nnc:14\r\ny:0.002\r\n\r\nn:25\r\np:3.7M\r\na:9,565KM<sup>2</sup>\r\nu:19\r\nr:81\r\no:1.33\r\nnc:12\r\ny:0.5mm/mon\r\n\r\nn:26\r\np:1.4M\r\na:460KM<sup>2</sup>\r\nu:37\r\nr:63\r\no:1.26\r\nnc:10\r\ny:0.08mm/mon\r\n\r\nn:27\r\np:1.7M\r\na:62,726KM<sup>2</sup>\r\nu:44\r\nr:56\r\no:1.28\r\nnc:12\r\ny:0mm/mon";

        var cities = data.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None);


        foreach (var city in cities)
        {
            var cityData = city.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            
            var cityId = cityData[0].Split(':')[1];
            var cityPopulation = cityData[1].Split(':')[1];
            var cityArea = cityData[2].Split(':')[1];
            var cityUrban = cityData[3].Split(':')[1];
            var cityRural = cityData[4].Split(':')[1];
            var cityOvercrowd = cityData[5].Split(':')[1];
            var cityNumCities = cityData[6].Split(':')[1];
            var cityPrec = cityData[7].Split(':')[1];

            if (Selection.activeGameObject != null)
            {
                var map = Selection.activeGameObject.GetComponent<MRKMap>();
                if (map != null)
                {
                    var md = map.PolygonsMetadata.Find(x => x.Id.ToString() == cityId);
                    if (md == null)
                    {
                        Debug.LogError("City not found" + cityId);
                        continue;
                    }

                    md.Population = cityPopulation;
                    md.Area = cityArea;
                    md.Urban = float.Parse(cityUrban);
                    md.Rural = float.Parse(cityRural);
                    md.Overcrowding = cityOvercrowd;
                    md.NumCities = int.Parse(cityNumCities);
                    md.YrPrec = cityPrec;
                }
            }
        }
    }

    [MenuItem("EGR/Import Heatmaps")]
    static void ImportHeatmaps()
    {
        var path = EditorUtility.OpenFilePanel("Select heatmap file", "", "txt");
        if (path.Length != 0)
        {
            var data = File.ReadAllText(path).Trim();
            var heatmaps = data.Split(new string[] { "\n" }, StringSplitOptions.None);

            int idx = 4;

            List<HeatmapNode> nodes = new List<HeatmapNode>();
            for (int i = 0; i < heatmaps.Length; i += 4)
            {
                double lat = double.Parse(heatmaps[i]);
                double lon = double.Parse(heatmaps[i + 1]);
                float radius = float.Parse(heatmaps[i + 2]);
                float intensity  = float.Parse(heatmaps[i + 3]);
                nodes.Add(new HeatmapNode { LatLng = new Vector2d(lat, lon), Radius = radius, Intensity = intensity });
            }

            var mapInt = Selection.activeGameObject.GetComponent<EGRScreenMapInterface>();
            if (mapInt != null)
            {
                mapInt.HeatmapsData[idx].Nodes = nodes;
            }
        }
    }
}
