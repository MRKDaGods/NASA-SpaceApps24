using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityEngine.Video;

namespace MRK.UI
{
    public class EGRScreenCommunityDetails : EGRScreenAnimatedAlpha, IEGRScreenSupportsBackKey
    {
        [SerializeField]
        VideoPlayer m_VideoPlayer;

        [SerializeField]
        VideoClip m_FallbackVideo;

        [SerializeField]
        TextMeshProUGUI m_Title;

        [SerializeField]
        TextMeshProUGUI m_Population;

        [SerializeField]
        TextMeshProUGUI m_Area;

        [SerializeField]
        TextMeshProUGUI m_Urban;

        [SerializeField]
        TextMeshProUGUI m_Rural;

        [SerializeField]
        Gradient2 m_SizeGradient;

        [SerializeField]
        TextMeshProUGUI m_Overcrowding;

        [SerializeField]
        TextMeshProUGUI m_NumCities;

        [SerializeField]
        TextMeshProUGUI m_YrPrec;

        [SerializeField]
        TextMeshProUGUI m_Humidity;

        [SerializeField]
        TextMeshProUGUI m_SolarRadiation;

        RectTransform m_VideoPlayerTarget;
        Vector2 m_DefaultAnchoredPos;

        private MRKPolygonsController PolygonsController => MRKPolygonsController.Instance;

        public MRKPolygonMetadata Metadata { get; set; }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            GetElement<Button>("imgBg/bBack").onClick.AddListener(OnBackKeyDown);

            m_VideoPlayerTarget = (RectTransform)m_VideoPlayer.transform.GetChild(0);
            m_DefaultAnchoredPos = m_VideoPlayerTarget.anchoredPosition;
        }

        protected override void OnScreenShow()
        {
            PolygonsController.PolygonBlockerSemaphore++;

            if (Metadata == null) return;

            m_Title.text = Metadata.Name.ToUpper();
            m_Population.text = Metadata.Population;
            m_Area.text = Metadata.Area;
            m_Urban.text = "URBAN " + Metadata.Urban.ToString("0.0") + "%";
            m_Rural.text = "RURAL " + Metadata.Rural.ToString("0.0") + "%";
            m_SizeGradient.Offset = Mathf.Lerp(-1f, 1f, Metadata.Urban / 100f);
            m_Overcrowding.text = Metadata.Overcrowding;
            m_NumCities.text = Metadata.NumCities.ToString();
            m_YrPrec.text = Metadata.YrPrec;
            m_Humidity.text = Metadata.Humidity;
            m_SolarRadiation.text = Metadata.SolarRadiation;

            m_VideoPlayer.clip = Metadata.Videos.Count > 0 ? Metadata.Videos[0] : m_FallbackVideo;

            // if giza set anchors to 0
            if (Metadata != null && Metadata.Name == "Giza")
            {
                m_VideoPlayerTarget.anchoredPosition = Vector2.zero;
            }
            else
            {
                m_VideoPlayerTarget.anchoredPosition = m_DefaultAnchoredPos;
            }

            StartCoroutine(WaitForVideoPreparation());
        }

        IEnumerator WaitForVideoPreparation()
        {
            m_VideoPlayerTarget.GetComponent<RawImage>().color = Color.clear;

            while (!m_VideoPlayer.isPrepared)
            {
                yield return null;
            }

            m_VideoPlayerTarget.GetComponent<RawImage>().DOColor(Color.white, 1f)
                .ChangeStartValue(Color.white.AlterAlpha(0f));
        }

        protected override void OnScreenHide()
        {
            PolygonsController.PolygonBlockerSemaphore--;
        }

        public void OnBackKeyDown()
        {
            HideScreen();
        }
    }
}
