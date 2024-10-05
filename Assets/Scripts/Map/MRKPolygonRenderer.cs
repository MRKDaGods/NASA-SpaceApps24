using NetTopologySuite.Geometries;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MRK
{
    public class MRKPolygonRenderer : MRKBehaviour
    {
        private Material m_Material;

        [SerializeField]
        private bool m_Fill = true;

        [SerializeField]
        private Color m_FillColor = Color.white;

        [SerializeField]
        private Color m_FillHoverColor = Color.red;

        [SerializeField]
        private Color m_OutlineColor = Color.black;

        [SerializeField]
        private float m_OutlineWidth = 1f;

        [SerializeField]
        private List<Vector2> m_ScreenPoints;

        [SerializeField]
        private List<Vector2d> m_Coordinates;

        [SerializeField]
        private MRKPolygonMetadata m_Metadata;

        [SerializeField]
        private bool m_ApplyFixup = false;

        [SerializeField]
        private bool m_RenderVertexNumbers = false;

        private bool m_IsHovered;

        private Mesh m_Mesh;
        private MeshFilter m_MeshFilter;
        private TextMeshPro m_Text;
        private Vector2 m_MouseDownPos;
        private bool m_IsMouseDown;

        private Color FillColor => m_IsHovered ? m_FillHoverColor : m_FillColor;

        /// <summary>
        /// Polygon coordinates in LatLon
        /// </summary>
        public List<Vector2d> Coordinates
        {
            get => m_Coordinates;

            set
            {
                m_Coordinates = value;
                RecalculateScreenSpacePoints(null);
            }
        }

        public MRKPolygonMetadata Metadata
        {
            get => m_Metadata;
            set => m_Metadata = value;
        }

        public Polygon Polygon { get; set; }

        private void Start()
        {
            m_Text = gameObject.GetComponentInChildren<TextMeshPro>();
            if (m_Text != null)
            {
                m_Text.text = m_Metadata.Name;
                m_Text.fontSize = m_Metadata.Id == -1 ? 48 : 40;

                // fit width to content
                var rt = m_Text.rectTransform;
                rt.sizeDelta = new Vector2(m_Text.preferredWidth, rt.sizeDelta.y);
            }
        }

        public void RecalculateScreenSpacePoints(List<Rect> rects)
        {
            if (m_ScreenPoints == null)
            {
                m_ScreenPoints = new List<Vector2>();
            }
            else
            {
                m_ScreenPoints.Clear();
            }

            if (m_MeshFilter == null)
            {
                m_MeshFilter = GetComponent<MeshFilter>();

                // copy mat
                var renderer = GetComponent<MeshRenderer>();
                m_Material = Instantiate(renderer.material);
                renderer.material = m_Material;
            }

            // reconstruct mesh
            if (m_Mesh == null)
            {
                m_Mesh = new Mesh();
            }
            else
            {
                m_Mesh.Clear();
            }

            // calc center
            var wCenter = Client.FlatMap.GeoToWorldPosition(new Vector2d(Polygon.Centroid.Y, Polygon.Centroid.X));

            var vertices = new List<Vector3>();

            foreach (var coord in m_Coordinates)
            {
                var worldPoint = Client.FlatMap.GeoToWorldPosition(coord);
                var spos = Client.ActiveCamera.WorldToScreenPoint(worldPoint);
                spos.y = Screen.height - spos.y;

                m_ScreenPoints.Add(spos);

                vertices.Add(new Vector3(worldPoint.x - wCenter.x, 0f, worldPoint.z - wCenter.z));
            }

            var center = Client.ActiveCamera.WorldToScreenPoint(wCenter);
            center.z = 0f;

            // triangles
            var indices = Triangulator.Triangulate(vertices.ConvertAll(v => v.ToVector2xz()));
            if (m_ApplyFixup)
            {
                // seif fucked up the vertices
                if (m_Metadata.CustomFixupData != null && m_Metadata.CustomFixupData.Count > 2 && m_Metadata.CustomFixupData.Count % 3 == 0)
                {
                    indices.AddRange(m_Metadata.CustomFixupData);
                }
                else
                {
                    indices.Add(vertices.Count - 2);
                    indices.Add(vertices.Count - 1);
                    indices.Add(1); // dont ask why, ask seif
                }
            }

            m_Mesh.vertices = vertices.ToArray(); // m_ScreenPoints.ConvertAll(v => new Vector3(v.x, Screen.height - v.y) - center).ToArray();
            m_Mesh.triangles = indices.ToArray();

            m_Mesh.RecalculateBounds();
            //var bounds = m_Mesh.bounds;
            //bounds.center = new Vector3(bounds.center.x, bounds.center.y, 0);
            //m_Mesh.bounds = bounds;

            m_Mesh.RecalculateNormals();
            m_MeshFilter.mesh = m_Mesh;

            // update our position
            wCenter.y += 0.01f;
            transform.position = wCenter;

            //if (rects != null && m_Text != null)
            //{
            //    // check if text overlaps any of the rects
            //    bool hide = false;

            //    var worldRect = new Rect(m_Text.rectTransform.position, m_Text.rectTransform.sizeDelta);

            //    // conver to screen space
            //    var sTopLeft = Client.ActiveCamera.WorldToScreenPoint(new Vector3(worldRect.xMin, 0f, worldRect.yMin));
            //    var sBottomRight = Client.ActiveCamera.WorldToScreenPoint(new Vector3(worldRect.xMax, 0f, worldRect.yMax));
            //    //var sTopRight = Client.ActiveCamera.WorldToScreenPoint(new Vector3(worldRect.xMax, 0f, worldRect.yMin));
            //    //var sBottomLeft = Client.ActiveCamera.WorldToScreenPoint(new Vector3(worldRect.xMin, 0f, worldRect.yMax));
            //    var sRect = new Rect(sTopLeft, sBottomRight - sTopLeft);

            //    foreach (var rect in rects)
            //    {
            //        if (rect.Overlaps(sRect))
            //        {
            //            hide = true;
            //            break;
            //        }
            //    }

            //    if (hide)
            //    {
            //        m_Text.gameObject.SetActive(false);
            //    }
            //    else
            //    {
            //        m_Text.gameObject.SetActive(true);
            //        rects.Add(sRect);
            //    }
            //}
        }

        void OnGUI()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (m_ScreenPoints == null || m_ScreenPoints.Count < 2)
            {
                return;
            }

            MRKGL.DrawPolygon(m_ScreenPoints, m_OutlineColor, m_OutlineWidth);

            if (m_RenderVertexNumbers)
            {
                // Render vertex numbers
                for (int i = 0; i < m_ScreenPoints.Count; i++)
                {
                    Vector2 point = m_ScreenPoints[i];
                    GUI.Label(new Rect(point.x, point.y, 100, 100), $"<size=48><color=white><b>{i}</b></color></size>");
                }
            }
        }

        private void Update()
        {
            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            m_IsHovered = IsPointInPolygon(mousePos, m_ScreenPoints);

            // Interpolate color
            Color targetColor = m_IsHovered ? m_FillHoverColor : m_FillColor;
            m_Material.SetColor("_Color", Color.Lerp(m_Material.GetColor("_Color"), targetColor, Time.deltaTime * 10));

            if (m_IsHovered && Input.GetMouseButtonDown(0))
            {
                m_MouseDownPos = mousePos;
                m_IsMouseDown = true;
            }

            // check for click
            if (m_IsMouseDown && Input.GetMouseButtonUp(0))
            {
                m_IsMouseDown = false;
                float dragDistance = Vector2.Distance(m_MouseDownPos, mousePos);

                if (dragDistance < 5f && m_IsHovered)
                {
                    MRKPolygonsController.Instance.HandlePolygonClick(this);
                }
            }
        }

        private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            bool isInside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                    (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }
    }
}
