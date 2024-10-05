﻿using Coffee.UIEffects;
using DG.Tweening;
using MRK.UI.MapInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_MapInterface;

namespace MRK.UI
{
    [Serializable]
    public class EGRMapInterfaceResources
    {
        public GameObject CurrentLocationSprite;
        public AnimationCurve CurrentLocationScaleCurve;
        public Image LocationPinSprite;
    }

    public class EGRScreenMapInterface : EGRScreenAnimatedAlpha
    {
        [Serializable]
        struct MarkerSprite
        {
            public EGRPlaceType Type;
            public Sprite Sprite;
        }

        //deprecated
        public static class MapButtonIDs
        {
            public static int CURRENT_LOCATION = 0;
            public static int HOTTEST_TRENDS = 1;
            public static int SETTINGS = 2;
            public static int NAVIGATION = 3;
            public static int BACK_TO_EARTH = 4;
            public static int SPACE_FOV = 5;
            public static int MAX = 6;

            public static int ALL_MASK = EGRUtils.FillBits(MAX);
        }

        MRKMap m_Map;
        TextMeshProUGUI m_CamDistLabel;
        [SerializeField]
        GameObject m_SpaceLabelsRoot;
        [SerializeField]
        TextMeshPro m_ContextLabel;
        [SerializeField]
        TextMeshPro m_TimeLabel;
        [SerializeField]
        TextMeshPro m_DistLabel;
        [SerializeField]
        GameObject m_MapButtonPrefab;
        float m_LastTimeUpdate;
        [SerializeField]
        AnimationCurve m_MarkerScaleCurve;
        [SerializeField]
        AnimationCurve m_MarkerOpacityCurve;
        RawImage m_TransitionImg;
        [SerializeField]
        MarkerSprite[] m_MarkerSprites;
        bool m_MouseDown;
        Vector3 m_MouseDownPos;
        bool m_ZoomHasChanged;
        Dictionary<Transform, TextMeshPro> m_PlanetNames;
        [SerializeField]
        EGRMapInterfacePlaceMarkersResources m_PlaceMarkersResources;
        [SerializeField]
        EGRMapInterfaceResources m_MapInterfaceResources;
        [SerializeField]
        EGRUIMapButtonInfo[] m_MapButtonsInfo;
        bool m_MapButtonsEnabled;
        Button m_BackButton;

        [SerializeField]
        GameObject m_HeatmapContainer;

        [SerializeField]
        List<HeatmapData> m_HeatmapData;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0x00000000;
        EGRCamera m_EGRCamera => EGRMain.Instance.ActiveEGRCamera;
        public string ContextText => m_ContextLabel.text;
        public bool IsInTransition => m_TransitionImg.gameObject.activeInHierarchy;
        public Transform ObservedTransform { get; set; }
        public bool ObservedTransformDirty { get; set; }
        public Transform ScalebarParent { get; private set; }
        public EGRMapInterfacePlaceMarkersResources PlaceMarkersResources => m_PlaceMarkersResources;
        public EGRMapInterfaceResources MapInterfaceResources => m_MapInterfaceResources;
        public bool MapButtonsEnabled
        {
            get { return m_MapButtonsEnabled; }
            set
            {
                if (m_MapButtonsEnabled != value)
                {
                    m_MapButtonsEnabled = value;
                    RegenerateMapButtons();
                }
            }
        }
        public int MapButtonsMask { get; set; }
        public int MapButtonsInteractivityMask { get; set; }
        public EGRMapInterfaceComponentCollection Components { get; private set; }
        public List<HeatmapData> HeatmapsData => m_HeatmapData;

        public EGRScreenMapInterface()
        {
            MapButtonsMask = MapButtonIDs.ALL_MASK;
            MapButtonsInteractivityMask = MapButtonIDs.ALL_MASK;

            Components = new EGRMapInterfaceComponentCollection();
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            m_Map = Client.FlatMap;
            m_Map.gameObject.SetActive(false);

            m_BackButton = GetElement<Button>(Buttons.Back);
            m_BackButton.onClick.AddListener(OnBackClick);
            m_MapButtonPrefab.SetActive(false); //disable our template button

            m_CamDistLabel = GetElement<TextMeshProUGUI>(Labels.CamDist);
            m_TransitionImg = GetElement<RawImage>(Images.Transition);
            m_TransitionImg.gameObject.SetActive(false);

            ScalebarParent = GetTransform(Others.DistProg);

            ObservedTransform = Client.GlobalMap.transform;

            foreach (var data in m_HeatmapData)
            {
                data.Button.onClick.AddListener(() =>
                {
                    m_Map.HeatmapData = data;
                });
            }

            RegisterInterfaceComponent(EGRMapInterfaceComponentType.PlaceMarkers, new EGRMapInterfaceComponentPlaceMarkers());
            RegisterInterfaceComponent(EGRMapInterfaceComponentType.ScaleBar, new EGRMapInterfaceComponentScaleBar());
            RegisterInterfaceComponent(EGRMapInterfaceComponentType.Navigation, new EGRMapInterfaceComponentNavigation());
            RegisterInterfaceComponent(EGRMapInterfaceComponentType.LocationOverlay, new EGRMapInterfaceComponentLocationOverlay());
            RegisterInterfaceComponent(EGRMapInterfaceComponentType.MapButtons, new EGRMapInterfaceComponentMapButtons(m_MapButtonsInfo));
        }

        public void OnInterfaceEarlyShow()
        {
            m_EGRCamera.SetInterfaceState(true);

            m_SpaceLabelsRoot.SetActive(true);
            m_ContextLabel.gameObject.SetActive(true);
            m_TimeLabel.gameObject.SetActive(EGRSettings.ShowTime);
            m_DistLabel.gameObject.SetActive(EGRSettings.ShowDistance);

            UpdateTime();
        }

        protected override void OnScreenShow()
        {
            m_MapButtonsEnabled = true;

            //hide bg since it's only for designing
            GetElement<Image>(Images.BaseBg).gameObject.SetActive(false);

            m_Map.OnMapUpdated += OnMapUpdated;
            m_Map.OnMapFullyUpdated += OnMapFullyUpdated;
            m_Map.OnMapZoomUpdated += OnMapZoomUpdated;

            Client.RegisterMapModeDelegate(OnMapModeChanged);
            Client.RegisterControllerReceiver(OnControllerMessageReceived);

            //map mode might've changed when visible=false
            OnMapModeChanged(Client.MapMode);

            if (m_PlanetNames == null) {
                m_PlanetNames = new Dictionary<Transform, TextMeshPro>();

                foreach (var planet in Client.Planets) {
                    if (planet.transform == Client.GlobalMap.transform)
                        continue;

                    TextMeshPro txt = planet.transform.Find("Name").GetComponent<TextMeshPro>();
                    txt.gameObject.SetActive(false);
                    m_PlanetNames[planet.transform] = txt;
                }
            }

            Client.DisableAllScreensExcept<EGRScreenMapInterface>();

            Components.OnComponentsShow();
        }

        protected override void OnScreenHide()
        {
            m_Map.OnMapUpdated -= OnMapUpdated;
            m_Map.OnMapFullyUpdated -= OnMapFullyUpdated;
            m_Map.OnMapZoomUpdated -= OnMapZoomUpdated;

            Client.UnregisterMapModeDelegate(OnMapModeChanged);
            Client.UnregisterControllerReceiver(OnControllerMessageReceived);

            //copied to direct hidescreen
            //ScreenManager.MainScreen.ShowScreen();

            Client.SetPostProcessState(false);

            Components.OnComponentsHide();

            //reset map button mask
            MapButtonsMask = MapButtonIDs.ALL_MASK;
            MapButtonsInteractivityMask = MapButtonIDs.ALL_MASK;

            m_Map.HeatmapData = null;

            ShowBackButton(true);
        }

        protected override void OnScreenUpdate()
        {
            if (Time.time - m_LastTimeUpdate >= 60f)
            {
                UpdateTime();
            }

            Components.OnComponentsUpdate();
        }

        public void ShowBackButton(bool show)
        {
            m_BackButton.gameObject.SetActive(show);
        }

        public void ShowHeatmapData(bool externalShow)
        {
            m_HeatmapContainer.SetActive(externalShow && Client.MapMode == EGRMapMode.Flat);
        }

        void RegisterInterfaceComponent(EGRMapInterfaceComponentType type, EGRMapInterfaceComponent component)
        {
            Components[type] = component;
            component.OnComponentInit(this);
        }

        void OnMapModeChanged(EGRMapMode mode)
        {
            m_CamDistLabel.gameObject.SetActive(/*isGlobe*/false);
            Components.ScaleBar.SetActive(mode == EGRMapMode.Flat);
            ShowHeatmapData(true);
            Client.ActiveEGRCamera.ResetStates();

            //from globe to flat
            if (mode == EGRMapMode.Flat && Client.PreviousMapMode == EGRMapMode.Globe)
            {
                Client.FlatCamera.UpdateMapViewingAngles(null, 0f);
            }

            if (Visible)
            {
                m_SpaceLabelsRoot.SetActive(mode == EGRMapMode.Globe);
            }

            RegenerateMapButtons();
        }

        public void RegenerateMapButtons()
        {
            //remove all buttons anyway
            Components.MapButtons.RemoveAllButtons();

            if (MapButtonsEnabled)
            {
                HashSet<EGRUIMapButtonID> ids = HashSetPool<EGRUIMapButtonID>.Default.Rent();

                // ids.Add(EGRUIMapButtonID.CurrentLocation);

                if (Client.MapMode == EGRMapMode.Flat)
                {
                    // ids.Add(EGRUIMapButtonID.Trending);
                    // ids.Add(EGRUIMapButtonID.Navigation);
                    ids.Add(EGRUIMapButtonID.Selection);
                    ids.Add(EGRUIMapButtonID.BackToEarth);
                }
                else
                {
                    ids.Add(EGRUIMapButtonID.FieldOfView);
                }

                ids.Add(EGRUIMapButtonID.Settings);

                Components.MapButtons.SetButtons(EGRUIMapButtonsGroupAlignment.BottomRight, ids);

                if (Client.MapMode == EGRMapMode.Globe && ObservedTransform != Client.GlobalMap.transform)
                {
                    ids.Clear();
                    ids.Add(EGRUIMapButtonID.FieldOfView);
                    ids.Add(EGRUIMapButtonID.BackToEarth);
                    Components.MapButtons.SetButtons(EGRUIMapButtonsGroupAlignment.BottomCenter, ids);
                    Components.MapButtons.SetGroupExpansionState(EGRUIMapButtonsGroupAlignment.BottomCenter, true);
                }

                HashSetPool<EGRUIMapButtonID>.Default.Free(ids);
            }
        }

        void OnControllerMessageReceived(MRKInputControllerMessage msg)
        {
            if (Client.MapMode != EGRMapMode.Globe)
                return;

            if (!m_EGRCamera.ShouldProcessControllerMessage(msg))
                return;

            if (msg.ContextualKind == MRKInputControllerMessageContextualKind.Mouse)
            {
                MRKInputControllerMouseEventKind kind = (MRKInputControllerMouseEventKind)msg.Payload[0];

                switch (kind)
                {
                    case MRKInputControllerMouseEventKind.Down:
                        m_MouseDown = true;
                        m_MouseDownPos = (Vector3)msg.Payload[3];
                        break;

                    case MRKInputControllerMouseEventKind.Up:
                        if (m_MouseDown)
                        {
                            m_MouseDown = false;

                            Vector3 pos = (Vector3)msg.Payload[1];
                            if ((pos - m_MouseDownPos).sqrMagnitude < 9f)
                                ChangeObservedTransform((Vector3)msg.Payload[1]);
                        }
                        break;
                }
            }
        }

        public void SetObservedTransformNameState(bool active)
        {
            if (ObservedTransform != Client.GlobalMap.transform)
            {
                TextMeshPro txt = m_PlanetNames[ObservedTransform];
                txt.gameObject.SetActive(active);

                if (active)
                {
                    StartCoroutine(EGRUtils.SetTextEnumerator(x => txt.text = x, txt.text, 0.3f, ""));
                }
            }
        }

        void ChangeObservedTransform(Vector3 pos)
        {
            Ray ray = Client.ActiveCamera.ScreenPointToRay(pos);

            //simulate physics
            Physics.Simulate(0.1f);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Client.ActiveCamera.farClipPlane, 1 << 6, QueryTriggerInteraction.Collide))
            {
                if (hit.transform != ObservedTransform)
                {
                    SetObservedTransformNameState(false);

                    ObservedTransform = hit.transform;
                    ObservedTransformDirty = true;

                    SetObservedTransformNameState(true);
                    OnObservedTransformChanged();
                }
            }
        }

        public void ChangeObservedTransform(Transform t)
        {
            if (t != ObservedTransform)
            {
                SetObservedTransformNameState(false);

                ObservedTransform = t;
                ObservedTransformDirty = true;

                SetObservedTransformNameState(true);
                OnObservedTransformChanged();
            }
        }

        public bool IsObservedTransformEarth()
        {
            return ObservedTransform == Client.GlobalMap.transform;
        }

        IEnumerator WaitForCameraLockRelease(Action callback)
        {
            if (callback == null)
                yield break;

            //TODO: check if map mode changes while waiting?
            while (Client.GlobeCamera.IsLocked)
                yield return new WaitForEndOfFrame();

            callback();
        }

        public void SetObservedTransformToEarth(Action callback = null)
        {
            if (!IsObservedTransformEarth())
            {
                SetObservedTransformNameState(false);
                ObservedTransform = Client.GlobalMap.transform;
                ObservedTransformDirty = true;

                if (callback != null)
                {
                    Client.Runnable.Run(WaitForCameraLockRelease(callback));
                }
            }
        }

        public void OnObservedTransformChanged()
        {
            Components.MapButtons.ShrinkOtherGroups(null);

            if (IsObservedTransformEarth())
            {
                Components.MapButtons.RemoveButton(EGRUIMapButtonsGroupAlignment.BottomCenter, EGRUIMapButtonID.BackToEarth);

                //eyad: hide all map buttons when not in earth
                RegenerateMapButtons();
            }
            else
            {
                Components.MapButtons.AddButton(EGRUIMapButtonsGroupAlignment.BottomCenter, EGRUIMapButtonID.BackToEarth, expand: true);
                Components.MapButtons.SetButtons(EGRUIMapButtonsGroupAlignment.BottomRight, null);
            }
        }

        public void SetDistanceText(string txt, bool animated = false)
        {
            if (m_DistLabel.gameObject.activeInHierarchy)
            {
                if (animated)
                    StartCoroutine(EGRUtils.SetTextEnumerator(x => m_DistLabel.text = x, txt, 0.9f, "m"));
                else
                    m_DistLabel.text = txt;
            }
        }

        public void SetContextText(string txt)
        {
            Client.StartCoroutine(EGRUtils.SetTextEnumerator(x => m_ContextLabel.text = x, txt, 0.7f, "\n"));
        }

        void UpdateTime()
        {
            m_LastTimeUpdate = Time.time;
            Client.StartCoroutine(EGRUtils.SetTextEnumerator(x => m_TimeLabel.text = x, DateTime.Now.ToString("HH:mm"), 1f, ":"));
        }

        public void SetTransitionTex(RenderTexture rt, TweenCallback callback = null, float speed = 0.6f)
        {
            m_TransitionImg.texture = rt;
            m_TransitionImg.gameObject.SetActive(true);

            m_TransitionImg.DOColor(Color.white.AlterAlpha(0f), speed)
                .ChangeStartValue(Color.white.AlterAlpha(1f))
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    m_TransitionImg.gameObject.SetActive(false);
                });

            UIDissolve dis = m_TransitionImg.GetComponent<UIDissolve>();
            DOTween.To(() => dis.effectFactor, x => dis.effectFactor = x, 1f, speed)
                .SetEase(Ease.OutSine)
                .ChangeStartValue(0f)
                .OnComplete(callback);
        }

        public void OnBackClick()
        {
            // check for polygons controller
            var poly = MRKPolygonsController.Instance;
            if (poly.SelectionMode != SelectionMode.Country)
            {
                poly.SelectionMode = SelectionMode.Country;
                return;
            }

            if (Client.MapMode == EGRMapMode.Flat)
            {
                Client.GlobeCamera.SetDistance(Client.RuntimeConfiguration.GlobeSettings.FlatTransitionOffset);
            }

            m_EGRCamera.SetInterfaceState(false);
            SetObservedTransformToEarth();
            HideScreen();

            m_SpaceLabelsRoot.SetActive(false);
            ScreenManager.MainScreen.ShowScreen();
        }

        public void ExternalForceHide()
        {
            m_EGRCamera.SetInterfaceState(false);
            SetObservedTransformToEarth();
            ForceHideScreen(true);

            m_SpaceLabelsRoot.SetActive(false);
            ScreenManager.MainScreen.ShowScreen();
        }

        void OnMapUpdated()
        {
            if (Client.MapMode != EGRMapMode.Flat)
                return;

            Components.OnMapUpdated();
        }

        void OnMapZoomUpdated(int oldZoom, int newZoom)
        {
            if (m_TransitionImg.gameObject.activeInHierarchy)
                return;

            //Debug.Log($"Zoom updated {oldZoom} -> {newZoom}");
            m_ZoomHasChanged = true;
        }

        void OnMapFullyUpdated()
        {
            if (m_ZoomHasChanged)
            {
                m_ZoomHasChanged = false;
                //SetTransitionTex(Client.CaptureScreenBuffer());
            }

            Components.OnMapFullyUpdated();
        }

        public void Warmup()
        {
            Components.OnComponentsWarmUp();
            ScreenManager.GetScreen<EGRPopupPlaceGroup>().Warmup();
        }

        public float EvaluateMarkerScale(float time)
        {
            return m_MarkerScaleCurve.Evaluate(time);
        }

        public float EvaluateMarkerOpacity(float time)
        {
            return m_MarkerOpacityCurve.Evaluate(time);
        }

        public Sprite GetSpriteForPlaceType(EGRPlaceType type)
        {
            foreach (MarkerSprite ms in m_MarkerSprites)
            {
                if (ms.Type == type)
                    return ms.Sprite;
            }

            return m_MarkerSprites[0].Sprite; //NONE
        }
    }
}