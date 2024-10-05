//#define MRK_RENDER_TILE_BOUNDS

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


namespace MRK
{
    public class MRKMap : MRKBehaviour
    {
        const int EX_N = 4, EX_S = 2, EX_W = 2, EX_E = 2;

        [SerializeField]
        float m_Zoom;
        [SerializeField]
        Vector2d m_CenterLatLng;
        float m_WorldRelativeScale;
        Vector2d m_CenterMercator;
        readonly List<MRKTileID> m_ActiveTileIDs;
        readonly List<MRKTileID> m_SortedTileIDs;
        readonly Dictionary<MRKTileID, MRKTileID> m_SortedToActiveIDs;
        readonly List<MRKTile> m_Tiles;
        readonly Dictionary<MRKTileID, MRKTile> m_IDsToTiles;
        int m_InitialZoom;
        int m_AbsoluteZoom;
        [SerializeField]
        Material m_TileMaterial;
        [SerializeField]
        Texture2D[] m_LoadingTextures;
        [SerializeField]
        Texture2D m_StationaryTexture;
        MRKMonitoredTexture m_MonitoredStationaryTexture;
        [SerializeField]
        bool m_AutoInit;
        [SerializeField]
        string m_Tileset;
        [SerializeField]
        float m_TileSize = 100f;
        [SerializeField]
        float[] m_DesiredTilesetEmission;
        IMRKMapController m_MapController;
        bool m_AwaitingMapFullUpdateEvent;
        readonly List<MRKTileID> m_PreviousTiles;
        bool m_TileDestroyZoomUpdatedDirty;
        [SerializeField]
        Material m_TilePlaneMaterial;
        readonly List<MRKTilePlane> m_ActivePlanes;
        readonly HashSet<int> m_VisibleTiles;
        int m_ExN;
        int m_ExS;
        int m_ExW;
        int m_ExE;
        string m_BackupTileset;
        MRKTileID m_CenterTile;
        readonly Dictionary<string, MRKMonitoredTexture> m_TopMostTiles;
        bool m_FetchingTopMost;

        [SerializeField]
        private MRKPolygonRenderer m_PolygonRendererPrefab;
        readonly List<MRKPolygonRenderer> m_PolygonRenderers;

        [SerializeField]
        private List<MRKPolygonMetadata> m_PolygonsMetadata;

        [SerializeField]
        private Heatmap m_Heatmap;

        private HeatmapData m_HeatmapData;

        public event Action OnMapUpdated;
        public event Action<int, int> OnMapZoomUpdated;
        public event Action OnMapFullyUpdated;
        public event Action OnPolygonsImported;

        public Vector2d CenterLatLng => m_CenterLatLng;
        public float WorldRelativeScale => m_WorldRelativeScale;
        public int InitialZoom => m_InitialZoom;
        public int AbsoluteZoom => m_AbsoluteZoom;
        public float Zoom => m_Zoom;
        public Material TileMaterial => m_TileMaterial;
        public Texture2D LoadingTexture => m_LoadingTextures[(int)EGRSettings.MapStyle];
        public MRKMonitoredTexture StationaryTexture => m_MonitoredStationaryTexture;
        public string Tileset => m_Tileset;
        public float TileSize => m_TileSize;
        public Vector2d CenterMercator => m_CenterMercator;
        public List<MRKTile> Tiles => m_Tiles;
        public Material TilePlaneMaterial => m_TilePlaneMaterial;
        public bool TileDestroyZoomUpdatedDirty => m_TileDestroyZoomUpdatedDirty;
        public List<MRKTilePlane> ActivePlanes => m_ActivePlanes;
        public int PreviousTilesCount => m_PreviousTiles.Count;
        public int MaxValidTile { get; private set; }
        public List<MRKPolygonRenderer> Polygons => m_PolygonRenderers;
        public HeatmapData HeatmapData
        {
            get => m_HeatmapData;
            set
            {
                m_HeatmapData = value;
                UpdateHeatmap();
            }
        }
        public List<MRKPolygonMetadata> PolygonsMetadata => m_PolygonsMetadata;

        public MRKMap()
        {
            m_ActiveTileIDs = new List<MRKTileID>();
            m_SortedTileIDs = new List<MRKTileID>();
            m_SortedToActiveIDs = new Dictionary<MRKTileID, MRKTileID>();
            m_Tiles = new List<MRKTile>();
            m_IDsToTiles = new Dictionary<MRKTileID, MRKTile>();
            m_PreviousTiles = new List<MRKTileID>();
            m_ActivePlanes = new List<MRKTilePlane>();
            m_VisibleTiles = new HashSet<int>();
            m_TopMostTiles = new Dictionary<string, MRKMonitoredTexture>();
            m_PolygonRenderers = new List<MRKPolygonRenderer>();

            //default extents
            m_ExN = EX_N;
            m_ExS = EX_S;
            m_ExW = EX_W;
            m_ExE = EX_E;
        }

        void Awake()
        {
            // disable polygon prefab
            m_PolygonRendererPrefab.gameObject.SetActive(false);
        }

        void Start()
        {
            //m_TileMaterial.SetTexture("_SecTex", m_LoadingTexture);

            if (m_AutoInit)
            {
                Initialize(m_CenterLatLng, Mathf.FloorToInt(m_Zoom));
            }

            ImportPolygons();
        }

        public void AdjustTileSizeForScreen()
        {
            //auto calc tile size
            // 1080 -> 100
            // x -> ?

            bool useHeight = Screen.height < Screen.width;
            float cur = useHeight ? Screen.height : Screen.width;
            float dimBase = useHeight ? 2316f : 1080f;

            m_TileSize = 120f + (120f - cur * 120f / dimBase);
        }

        void Update()
        {
            //wait for velocity to go down
            if (m_AwaitingMapFullUpdateEvent)
            {
                float sqrMagnitude = m_MapController.GetMapVelocity().sqrMagnitude;
                if (sqrMagnitude <= 5f || m_Zoom < m_AbsoluteZoom || m_Zoom > m_AbsoluteZoom + 1)
                {
                    m_AwaitingMapFullUpdateEvent = false;
                    int newAbsZoom = Mathf.Clamp(Mathf.FloorToInt(m_Zoom), 0, 21);
                    m_TileDestroyZoomUpdatedDirty = m_AbsoluteZoom != newAbsZoom;
                    m_AbsoluteZoom = newAbsZoom;

                    //raise map full update
                    if (OnMapFullyUpdated != null)
                    {
                        OnMapFullyUpdated();
                    }

                    m_PreviousTiles.Clear();
                    foreach (MRKTileID tile in m_SortedTileIDs)
                    {
                        m_PreviousTiles.Add(m_SortedToActiveIDs[tile]);
                    }

                    UpdateScale();
                    UpdatePosition();
                }
            }

#if MRK_DEBUG_FETCHER_LOCK
			Debug.Log(MRKTile.FetcherLock.Recursion);
#endif

#if HEATMAP_EDITOR
            ExUpdate();
#endif
        }

        public void Initialize(Vector2d center, int zoom)
        {
            m_AbsoluteZoom = zoom;
            m_Zoom = m_AbsoluteZoom;
            m_InitialZoom = m_AbsoluteZoom;
            m_CenterLatLng = center;

            m_MonitoredStationaryTexture = new MRKMonitoredTexture(m_StationaryTexture, true);

            UpdateTileset();
            UpdateMap(m_CenterLatLng, m_Zoom, true);
        }

        public void UpdateTileset()
        {
            string newTileset = EGRSettings.GetCurrentTileset();
            if (m_Tileset != newTileset)
            {
                m_Tileset = newTileset;

                if (!m_TopMostTiles.ContainsKey(newTileset))
                {
                    Client.Runnable.Run(FetchAndStoreTopMostTile(newTileset));
                }
            }
        }

        IEnumerator FetchAndStoreTopMostTile(string tileset)
        {
            m_FetchingTopMost = true;

            MRKFileTileFetcher fileFetcher = MRKSelfContainedPtr<MRKFileTileFetcher>.Global;
            MRKTileID tileID = MRKTileID.TopMost;
            MRKTileFetcher fetcher = fileFetcher.Exists(tileset, tileID, false) ? (MRKTileFetcher)fileFetcher : MRKSelfContainedPtr<MRKRemoteTileFetcher>.Global;

            Reference<UnityWebRequest> webReq = ReferencePool<UnityWebRequest>.Default.Rent();
            MRKTileFetcherContext context = new MRKTileFetcherContext();
            yield return fetcher.Fetch(context, tileset, tileID, webReq, false);

            if (!context.Error && context.Texture != null)
            {
                m_TopMostTiles[tileset] = new MRKMonitoredTexture(context.Texture, true);
            }

            ReferencePool<UnityWebRequest>.Default.Free(webReq);

            m_FetchingTopMost = false;
        }

        public void SetNavigationTileset()
        {
            m_BackupTileset = m_Tileset;
            m_Tileset = "nav";

            UpdatePosition();
        }

        void UpdateScale()
        {
            Rectd referenceTileRect = MRKMapUtils.TileBounds(MRKMapUtils.CoordinateToTileId(m_CenterLatLng, m_AbsoluteZoom));
            m_WorldRelativeScale = m_TileSize / (float)referenceTileRect.Size.x;
        }

        public void FitToBounds(Vector2d min, Vector2d max, float padding = 0.2f, bool teleport = true, Reference<float> zoomRef = null)
        {
            float z = Client.ActiveCamera.transform.position.y - transform.position.y;
            Vector3 botLeft = Client.ActiveCamera.ViewportToWorldPoint(new Vector3(padding, padding, z));
            Vector2d botLeftCoords = WorldToGeoPosition(botLeft);
            Vector3 topRight = Client.ActiveCamera.ViewportToWorldPoint(new Vector3(1f - padding, 1f - padding, z));
            Vector2d topRightCoords = WorldToGeoPosition(topRight);

            var targetLonDelta = max.y - min.y;
            var targetLatDelta = max.x - min.x;

            var screenLonDelta = topRightCoords.y - botLeftCoords.y;
            var screenLatDelta = topRightCoords.x - botLeftCoords.x;

            var zoomLatMultiplier = screenLatDelta / targetLatDelta;
            var zoomLonMultiplier = screenLonDelta / targetLonDelta;

            var latZoom = Mathf.Log((float)zoomLatMultiplier, 2);
            var lonZoom = Mathf.Log((float)zoomLonMultiplier, 2);

            float zoom = m_Zoom + Mathf.Min(latZoom, lonZoom);

            if (zoomRef != null)
            {
                zoomRef.Value = zoom;
            }

            if (teleport)
            {
                m_MapController.SetCenterAndZoom((min + max) * 0.5f, zoom);
            }
        }

        void UpdateTileExtents()
        {
            m_ActiveTileIDs.Clear();
            m_SortedTileIDs.Clear();
            m_SortedToActiveIDs.Clear();
            m_VisibleTiles.Clear();

            //set max valid tile
            MaxValidTile = (1 << m_AbsoluteZoom) - 1;
            for (int x = (m_CenterTile.X - m_ExW); x <= (m_CenterTile.X + m_ExE); x++)
            {
                for (int y = (m_CenterTile.Y - m_ExN); y <= (m_CenterTile.Y + m_ExS); y++)
                {
                    m_ActiveTileIDs.Add(new MRKTileID(m_AbsoluteZoom, x, y, x < 0 || x > MaxValidTile || y < 0 || y > MaxValidTile));
                }
            }

            List<MRKTile> buf = new List<MRKTile>();
            foreach (MRKTile tile in m_Tiles)
            {
                if (!m_ActiveTileIDs.Contains(tile.ID))
                {
                    //m_TilePool.Free(tile);
                    buf.Add(tile);
                }
            }

            if (m_TileDestroyZoomUpdatedDirty)
            {
                m_ActivePlanes.Clear();
            }

            foreach (MRKTile tile in buf)
            {
                tile.OnDestroy();

                //tell the map interface to dispose markers owned by such tiles
                EventManager.BroadcastEvent(new EGREventTileDestroyed(tile, m_TileDestroyZoomUpdatedDirty));
                m_Tiles.Remove(tile);
            }

            m_IDsToTiles.Clear();
            m_TileDestroyZoomUpdatedDirty = false;

            foreach (MRKTileID id in m_ActiveTileIDs)
            {
                MRKTileID sortedID = new MRKTileID(0, id.X - m_CenterTile.X, id.Y - m_CenterTile.Y);
                m_SortedTileIDs.Add(sortedID);
                m_SortedToActiveIDs[sortedID] = id;
            }

            m_SortedTileIDs.Sort((x, y) =>
            {
                int sqrMagX = x.Magnitude;
                int sqrMagY = y.Magnitude;

                return sqrMagX.CompareTo(sqrMagY);
            });
        }

        void UpdatePosition()
        {
            m_CenterMercator = MRKMapUtils.LatLonToMeters(m_CenterLatLng);

            MRKTileID centerTile = MRKMapUtils.CoordinateToTileId(m_CenterLatLng, m_AbsoluteZoom);
            if (m_CenterTile != centerTile)
            {
                m_CenterTile = centerTile;
                UpdateTileExtents();
            }

            float scaleFactor = Mathf.Pow(2, m_InitialZoom - m_AbsoluteZoom);

            int siblingIdx = 0;
            foreach (MRKTileID tileID in m_SortedTileIDs)
            {
                MRKTileID realID = m_SortedToActiveIDs[tileID];
                MRKTile tile = m_Tiles.Find(x => x.ID == realID);
                if (tile == null)
                {
                    tile = new MRKTile();
                }

                tile.InitTile(this, realID, siblingIdx);

                Rectd rect = tile.Rect.Value;
                Vector3 position = new Vector3((float)(rect.Center.x - m_CenterMercator.x) * m_WorldRelativeScale * scaleFactor, 0f,
                    (float)(rect.Center.y - m_CenterMercator.y) * m_WorldRelativeScale * scaleFactor);

                tile.Obj.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                tile.Obj.transform.localPosition = position;
                tile.Obj.transform.SetSiblingIndex(siblingIdx++);

                if (!m_Tiles.Contains(tile))
                    m_Tiles.Add(tile);

                if (siblingIdx < 6)
                    m_VisibleTiles.Add(realID.GetHashCode());

                m_IDsToTiles[realID] = tile;
            }

            transform.position = Vector3.zero;

            // update poly positions
            UpdatePolygonsPosition();

            // update heatmap
            if (m_HeatmapData != null)
            {
                UpdateHeatmap();
            }
        }

#if MRK_RENDER_TILE_BOUNDS
		void OnGUI() {
			foreach (MRKTile tile in m_Tiles) {
				RectD r = tile.Rect;

				Vector3 pos = GeoToWorldPosition(MRKMapUtils.MetersToLatLon(r.Min));

				Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos); spos.y = Screen.height - spos.y;

				Vector3 pos2 = GeoToWorldPosition(MRKMapUtils.MetersToLatLon(r.Max));

				Vector3 spos2 = Client.ActiveCamera.WorldToScreenPoint(pos2); spos2.y = Screen.height - spos2.y;

				EGRGL.DrawCircle(spos, 10f, Color.blue);
				EGRGL.DrawCircle(spos2, 20f, Color.white);
				EGRGL.DrawLine(spos, spos2, Color.red, 1.5f);

				EGRGL.DrawBox(spos, spos2, Color.magenta, 1.5f);
			}
		}
#endif

        public void UpdateMap(Vector2d latLon, float zoom, bool force = false)
        {
            if (zoom > 21 || zoom < 0)
                return;

            //zoom didnt change
            if (!force && Math.Abs(m_Zoom - zoom) <= Mathf.Epsilon && !latLon.IsNotEqual(m_CenterLatLng)) return;

            //we are raising the full update event sometime soon
            m_AwaitingMapFullUpdateEvent = true;

            int newZoom = Mathf.FloorToInt(zoom);
            int oldZoom = AbsoluteZoom;
            bool egrMapZoomUpdated = newZoom != oldZoom;
            if (egrMapZoomUpdated)
            {
                if (OnMapZoomUpdated != null)
                    OnMapZoomUpdated(oldZoom, newZoom);
            }

            bool egrMapUpdated = latLon.IsNotEqual(CenterLatLng) || egrMapZoomUpdated || Mathf.Abs(zoom - m_Zoom) > Mathf.Epsilon;

            // Update map zoom, if it has changed.
            if (Math.Abs(m_Zoom - zoom) > Mathf.Epsilon)
            {
                m_Zoom = zoom;
                //m_AbsoluteZoom = Mathf.FloorToInt(m_Zoom);
            }

            // Compute difference in zoom. Will be used to calculate correct scale of the map.
            float differenceInZoom = m_Zoom - m_InitialZoom;
            bool isAtInitialZoom = (differenceInZoom - 0.0 < Mathf.Epsilon);

            //Update center latitude longitude
            var centerLatitudeLongitude = latLon;
            double xDelta = centerLatitudeLongitude.x;
            double zDelta = centerLatitudeLongitude.y;

            //xDelta = xDelta > 0 ? Mathd.Min(xDelta, MRKMapUtils.LATITUDE_MAX) : Mathd.Max(xDelta, -MRKMapUtils.LATITUDE_MAX);
            //zDelta = zDelta > 0 ? Mathd.Min(zDelta, MRKMapUtils.LONGITUDE_MAX) : Mathd.Max(zDelta, -MRKMapUtils.LONGITUDE_MAX);

            //Set Center in Latitude Longitude and Mercator.
            m_CenterLatLng = new Vector2d(xDelta, zDelta);
            UpdateScale();
            UpdatePosition();

            //Scale the map accordingly.
            if (Math.Abs(differenceInZoom) > Mathf.Epsilon || isAtInitialZoom)
            {
                Vector3 sc = Vector3.one * Mathf.Pow(2, differenceInZoom);
                transform.localScale = sc;
            }

            //EGR
            if (egrMapUpdated)
            {
                if (OnMapUpdated != null)
                    OnMapUpdated();
            }

            foreach (MRKTilePlane plane in m_ActivePlanes)
            {
                plane.UpdatePlane();
            }
        }

        public Vector2d WorldToGeoPosition(Vector3 realworldPoint)
        {
            float scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));
            return (transform.InverseTransformPoint(realworldPoint)).GetGeoPosition(m_CenterMercator, m_WorldRelativeScale * scaleFactor);
        }

        public Vector3 GeoToWorldPosition(Vector2d latitudeLongitude)
        {
            float scaleFactor = Mathf.Pow(2f, InitialZoom - AbsoluteZoom);
            var worldPos = MRKMapUtils.GeoToWorldPosition(latitudeLongitude.x, latitudeLongitude.y, m_CenterMercator, WorldRelativeScale * scaleFactor).ToVector3xz();
            return transform.TransformPoint(worldPos);
        }

        public float GetDesiredTilesetEmission()
        {
            int idx = (int)EGRSettings.MapStyle;
            if (m_DesiredTilesetEmission.Length > idx)
                return m_DesiredTilesetEmission[idx];

            return 1f;
        }

        public void SetMapController(IMRKMapController controller)
        {
            m_MapController = controller;
        }

        public MRKTileID GetPreviousTileID(int idx)
        {
            if (idx >= m_PreviousTiles.Count)
                return null;

            return m_PreviousTiles[idx];
        }

        public MRKTile GetTileFromSiblingIndex(int index)
        {
            if (m_SortedToActiveIDs.Count <= index)
                return null;

            MRKTileID id = m_SortedToActiveIDs[m_SortedTileIDs[index]];
            if (m_IDsToTiles.ContainsKey(id))
                return m_IDsToTiles[id];

            return null;
        }

        public void InvokeUpdateEvent()
        {
            if (OnMapUpdated != null)
                OnMapUpdated();
        }

        public bool IsTileVisible(int hash)
        {
            return m_VisibleTiles.Contains(hash);
        }

        public bool IsAnyTileFetching()
        {
            return m_Tiles.Any(x => x.IsFetching);
        }

        public void DestroyAllTiles()
        {
            lock (m_Tiles)
            {
                foreach (MRKTile tile in m_Tiles)
                {
                    tile.OnDestroy();
                    EventManager.BroadcastEvent<EGREventTileDestroyed>(new EGREventTileDestroyed(tile, false));
                }

                m_Tiles.Clear();
            }
        }

        public Vector2d ProjectVector(Vector2d v)
        {
            float delta = m_MapController.MapRotation.y * Mathf.Deg2Rad;
            return new Vector2d(v.x * Mathd.Cos(delta) - v.y * Mathd.Sin(delta), v.x * Mathd.Sin(delta) + v.y * Mathd.Cos(delta));
        }

        public MRKMonitoredTexture GetCurrentTopMostTile()
        {
            MRKMonitoredTexture tex;
            if (!m_TopMostTiles.TryGetValue(m_Tileset, out tex))
            {
                if (!m_FetchingTopMost)
                {
                    Client.Runnable.Run(FetchAndStoreTopMostTile(m_Tileset));
                }
            }

            return tex;
        }

        static FeatureCollection ReadGeoJsonFeatures(string path)
        {
            var geoJsonContent = File.ReadAllText(path);
            var reader = new GeoJsonReader();
            return reader.Read<FeatureCollection>(geoJsonContent);      
        }

        void ImportPolygons()
        {
            // country
            var egyPath = Path.Combine(Application.streamingAssetsPath, "Map", "EGY", "egybounds.geojson");
            var egyFeatures = ReadGeoJsonFeatures(egyPath);
            if (egyFeatures.Count == 1)
            {
                var polygon = egyFeatures[0].Geometry as Polygon;
                if (polygon != null)
                {
                    var polyRenderer = Instantiate(m_PolygonRendererPrefab, m_PolygonRendererPrefab.transform.parent);
                    polyRenderer.Metadata = new MRKPolygonMetadata { Id = -1, Name = "EGYPT" };
                    polyRenderer.Polygon = polygon;
                    polyRenderer.Coordinates = polygon.ExteriorRing.Coordinates.Select(c => new Vector2d(c.Y, c.X)).ToList();
                    polyRenderer.gameObject.SetActive(true);
                    m_PolygonRenderers.Add(polyRenderer);
                }
            }

            // cities
            var citiesPath = Path.Combine(Application.streamingAssetsPath, "Map", "EGY", "boundaries.geojson");
            var cityFeatures = ReadGeoJsonFeatures(citiesPath);

            foreach (var feature in cityFeatures)
            {
                if (feature.Geometry is Polygon polygon)
                {
                    // create new poly renderer
                    var polyRenderer = Instantiate(m_PolygonRendererPrefab, m_PolygonRendererPrefab.transform.parent);
                    polyRenderer.Metadata = m_PolygonsMetadata.FirstOrDefault(m => m.Id.ToString() == feature.Attributes["n"].ToString());
                    polyRenderer.Polygon = polygon;
                    polyRenderer.Coordinates = polygon.ExteriorRing.Coordinates.Select(c => new Vector2d(c.Y, c.X)).ToList();
                    polyRenderer.gameObject.SetActive(true);
                    m_PolygonRenderers.Add(polyRenderer);
                }
            }

            if (OnPolygonsImported != null)
            {
                OnPolygonsImported();
            }
        }

        void UpdatePolygonsPosition()
        {
            var rects = new List<Rect>();
            m_PolygonRenderers.ForEach(p => { p.RecalculateScreenSpacePoints(rects); });
        }

        void UpdateHeatmap()
        {
            if (m_HeatmapData == null || m_HeatmapData.Nodes.Count == 0)
            {
                m_Heatmap.count = 0;
                m_Heatmap.gameObject.SetActive(false);
                return;
            } 

            if (m_Heatmap.count != m_HeatmapData.Nodes.Count || !m_Heatmap.gameObject.activeInHierarchy)
            {
                m_Heatmap.count = m_HeatmapData.Nodes.Count;
                m_Heatmap.AllocateIfNeeded();

                m_Heatmap.gameObject.SetActive(true);
            }

            for (int i = 0; i < m_HeatmapData.Nodes.Count; i++)
            {
                var node = m_HeatmapData.Nodes[i];

                // convert to world position
                var worldPos = GeoToWorldPosition(node.LatLng);
                m_Heatmap.positions[i] = new Vector4(worldPos.x, worldPos.z, 0, 0);
                m_Heatmap.properties[i] = new Vector4(node.Radius * transform.localScale.x, node.Intensity, 0, 0);
            }
        }

#if HEATMAP_EDITOR

        // map stuff
        List<HeatmapNode> nodes = new List<HeatmapNode>();
        private Vector2 m_MouseDownPos;
        private bool m_IsMouseDown;
        bool m_EnableCapture;
        float m_Radius = 0.1f, m_Intensity = 0.1f;
        Vector2d m_CurLatLng;

        void ExUpdate()
        {
            if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.S))
            {
                m_MouseDownPos = Input.mousePosition;
                m_IsMouseDown = true;
            }

            // check for click
            if (m_IsMouseDown && Input.GetMouseButtonUp(0))
            {
                m_IsMouseDown = false;
                float dragDistance = Vector2.Distance(m_MouseDownPos, Input.mousePosition);

                if (dragDistance < 5f)
                {
                    var pos = Input.mousePosition;
                    pos.z = Client.ActiveCamera.transform.localPosition.y;
                    Vector3 wPos = Client.ActiveCamera.ScreenToWorldPoint(pos);
                    m_CurLatLng = WorldToGeoPosition(wPos);
                }
            }

            m_Heatmap.gameObject.SetActive(m_EnableCapture);
            
            if (m_EnableCapture)
            {
                m_Heatmap.count = nodes.Count + 1;
                m_Heatmap.AllocateIfNeeded();

                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];

                    // convert to world position
                    var worldPos = GeoToWorldPosition(node.LatLng);

                    m_Heatmap.positions[i] = new Vector4(worldPos.x, worldPos.z, 0, 0);
                    m_Heatmap.properties[i] = new Vector4(node.Radius * transform.localScale.x, node.Intensity, 0, 0);
                }

                var wpos = GeoToWorldPosition(m_CurLatLng);
                m_Heatmap.positions[nodes.Count] = new Vector4(wpos.x, wpos.z, 0, 0);
                m_Heatmap.properties[nodes.Count] = new Vector4(m_Radius * transform.localScale.x, m_Intensity, 0, 0);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 300, 300f, 500f));
            GUILayout.BeginVertical();

            if (GUILayout.Button("Export"))
            {
                // conver to json
                string s = "";
                foreach (var n in nodes)
                {
                    s += $"{n.LatLng.x}\n{n.LatLng.y}\n{n.Radius}\n{n.Intensity}\n";
                }

                Debug.Log(s);
                File.WriteAllText("heatmap.txt", s);
            }

            m_EnableCapture = GUILayout.Toggle(m_EnableCapture, "Enable Capture");

            GUILayout.Label($"Lat: {m_CurLatLng.x}, Lon: {m_CurLatLng.y}");

            GUILayout.Label("Radius: " + m_Radius);
            m_Radius = GUILayout.HorizontalSlider(m_Radius, 0f, 50f);

            GUILayout.Label("Intensity" + m_Intensity);
            m_Intensity = GUILayout.HorizontalSlider(m_Intensity, -0.5f, 3f);

            if (GUILayout.Button("Add"))
            {
                nodes.Add(new HeatmapNode { LatLng = m_CurLatLng, Radius = m_Radius, Intensity = m_Intensity });

                m_CurLatLng = Vector2d.zero;
                m_Radius = 0.1f;
                m_Intensity = 0.1f;
            }

            GUILayout.Label($"Node Count: {nodes.Count}");
            for (int i = nodes.Count - 1; i > -1; i--)
            {
                var node = nodes[i];
                GUILayout.Label($"Lat: {node.LatLng.x}, Lon: {node.LatLng.y}, Radius: {node.Radius}, Intensity: {node.Intensity}");

                if (GUILayout.Button("Delete"))
                {
                    nodes.RemoveAt(i);
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif
    }
}