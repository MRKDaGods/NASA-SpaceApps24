﻿using UnityEngine;
using UnityEngine.UI;

namespace MRK {
    public class EGRLocationManager : MRKBehaviour {
        const float LOCATION_REQUEST_DELAY = 0f; //0.5f

        GameObject m_CurrentLocationSprite;
        bool m_IsActive;
        bool m_RequestingLocation;
        float m_LastLocationRequestTime;
        Vector2d? m_LastFetchedCoords;
        float? m_LastFetchedBearing;

        public bool AllowMapRotation { get; set; }

        void Start() {
            m_CurrentLocationSprite = ScreenManager.MapInterface.MapInterfaceResources.CurrentLocationSprite;
            m_CurrentLocationSprite.SetActive(false);

            Client.RegisterMapModeDelegate(OnMapModeChanged);
            Client.FlatMap.OnMapUpdated += OnMapUpdated;
        }

        void OnDestroy() {
            Client.UnregisterMapModeDelegate(OnMapModeChanged);
            Client.FlatMap.OnMapUpdated -= OnMapUpdated;
        }

        void OnMapUpdated() {
            if (m_IsActive && m_LastFetchedCoords.HasValue) {
                Vector3 pos = Client.FlatMap.GeoToWorldPosition(m_LastFetchedCoords.Value);
                Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos);
                m_CurrentLocationSprite.transform.position = EGRPlaceMarker.ScreenToMarkerSpace(spos);
                m_CurrentLocationSprite.transform.localScale = Vector3.one *
                    ScreenManager.MapInterface.MapInterfaceResources.CurrentLocationScaleCurve.Evaluate(Client.FlatMap.Zoom / 21f);

                m_CurrentLocationSprite.transform.rotation = Quaternion.Euler(Quaternion.Euler(0f, 0f, m_LastFetchedBearing.Value).eulerAngles 
                    - Quaternion.Euler(-90f - Client.FlatCamera.MapRotation.x, 0f, -Client.FlatCamera.MapRotation.y).eulerAngles);

                //float show = 0f;
            }
        }

        void OnMapModeChanged(EGRMapMode mode) {
            if (mode != EGRMapMode.Flat) {
                DeActivate();
            }
            else {
                RequestCurrentLocation(true);
            }
        }

        void DeActivate() {
            if (!m_IsActive)
                return;

            m_IsActive = false;
            m_CurrentLocationSprite.SetActive(false);
        }

        void ActivateIfNeeded() {
            if (m_IsActive)
                return;

            m_IsActive = true;
            m_CurrentLocationSprite.SetActive(true);
        }

        void OnReceiveLocation(bool success, Vector2d? coords, float? bearing) {
            m_RequestingLocation = false;

            if (!success) {
                DeActivate();
                return;
            }

            ActivateIfNeeded();

            m_LastFetchedCoords = coords.Value;
            m_LastFetchedBearing = bearing.Value;

            OnMapUpdated(); //position marker

            if (AllowMapRotation) {
                Client.FlatCamera.SetRotation(new Vector2(0f, bearing.Value));
            }

            /*if (Client.NavigationManager.CurrentDirections.HasValue) {
                TestLineSegs();
            } */
        }

        void Update() {
            if (!m_IsActive)
                return;

            if (Client.NavigationManager.IsNavigating) {
                DeActivate();
                return;
            }

            RequestCurrentLocation();

            //assuming MRK
            if (Client.InputModel.NeedsUpdate) {
                OnMapUpdated();
            }
        }

        public void RequestCurrentLocation(bool silent = false, bool force = false, bool teleport = false) {
            if (!force && (Time.time - m_LastLocationRequestTime < LOCATION_REQUEST_DELAY || m_RequestingLocation)) {
                goto __teleport;
            }

            m_RequestingLocation = true;
            m_LastLocationRequestTime = Time.time;
            Client.LocationService.GetCurrentLocation(OnReceiveLocation, silent);

        __teleport:
            if (teleport && m_LastFetchedCoords.HasValue) {
                Client.FlatCamera.TeleportToLocationTweened(m_LastFetchedCoords.Value);
            }
        }
    }
}
