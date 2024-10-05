using MRK.UI;
using UnityEngine;

namespace MRK
{
    public enum SelectionMode
    {
        Country,
        City,
        Issues
    }

    public class MRKPolygonsController : MRKBehaviour
    {
        [SerializeField]
        private GameObject m_PolygonsContainer;
        private bool m_SelectionEnabled;

        // semaphore for blocking polygon selection
        private int m_PolygonBlockerSemaphore;

        private SelectionMode m_SelectionMode = SelectionMode.Country;

        public int PolygonBlockerSemaphore
        {
            get => m_PolygonBlockerSemaphore;
            set
            {
                m_PolygonBlockerSemaphore = value;
                UpdatePolygonsContainerState();
            }
        }

        public SelectionMode SelectionMode
        {
            get => m_SelectionMode;
            set
            {
                m_SelectionMode = value;
                OnPolygonsImported();
            }
        }

        public static MRKPolygonsController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            Client.FlatMap.OnPolygonsImported += OnPolygonsImported;
            Client.RegisterMapModeDelegate(OnMapModeChanged);

            // initially call handler
            OnMapModeChanged(Client.MapMode);

            // initially disable selection
            UpdatePolygonsContainerState();
        }

        private void OnPolygonsImported()
        {
            // enable egypt only
            foreach (var polygon in Client.FlatMap.Polygons)
            {
                if (polygon.Metadata.Id == -1) // egy
                {
                    polygon.gameObject.SetActive(m_SelectionMode == SelectionMode.Country);
                }
                else
                {
                    polygon.gameObject.SetActive(m_SelectionMode == SelectionMode.City);
                }
            }
        }

        private void OnMapModeChanged(EGRMapMode mode)
        {
            UpdatePolygonsContainerState();

            if (mode != EGRMapMode.Flat)
            {
                m_SelectionEnabled = false;
                UpdatePolygonsContainerState();
            }
        }

        public void ToggleSelection()
        {
            m_SelectionEnabled = !m_SelectionEnabled;
            UpdatePolygonsContainerState();

            if (m_SelectionEnabled)
            {
                Client.FlatMap.HeatmapData = null; // disable heatmap data
            }
            else
            {
                SelectionMode = SelectionMode.Country;
            }

            ScreenManager.MapInterface.ShowHeatmapData(!m_SelectionEnabled);
        }

        private void UpdatePolygonsContainerState()
        {
            m_PolygonsContainer.SetActive(m_SelectionEnabled && Client.MapMode == EGRMapMode.Flat && m_PolygonBlockerSemaphore == 0);
        }

        public void HandlePolygonClick(MRKPolygonRenderer polygon)
        {
            if (!m_SelectionEnabled) return;

            if (polygon.Metadata.Id == -1)
            {
                //egy
                ScreenManager.GetScreen<EGRScreenEgyptDetails>().ShowScreen();
            }
            else
            {
                var commDetails = ScreenManager.GetScreen<EGRScreenCommunityDetails>();
                commDetails.Metadata = polygon.Metadata;
                commDetails.ShowScreen();
            }
        }
    }
}
