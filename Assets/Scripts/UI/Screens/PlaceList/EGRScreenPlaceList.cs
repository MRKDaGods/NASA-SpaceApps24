﻿using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public partial class EGRScreenPlaceList : EGRScreenAnimatedAlpha {
        SearchArea m_SearchArea;
        readonly ObjectPool<PlaceItem> m_PlaceItemPool;
        GameObject m_PlaceItemPrefab;
        TextMeshProUGUI m_ResultLabel;
        readonly List<PlaceItem> m_Items;

        static EGRScreenPlaceList Instance { get; set; }

        public EGRScreenPlaceList() {
            m_PlaceItemPool = new ObjectPool<PlaceItem>(() => {
                Transform trans = Instantiate(m_PlaceItemPrefab, m_PlaceItemPrefab.transform.parent).transform;
                PlaceItem item = new PlaceItem(trans);
                return item;
            }, true, OnItemHide);

            m_Items = new List<PlaceItem>();
        }

        protected override void OnScreenInit() {
            base.OnScreenInit();

            Instance = this;

            m_SearchArea = new SearchArea(Body.Find("Search"));
            m_PlaceItemPrefab = Body.Find("Scroll View/Viewport/Content/Item").gameObject;
            m_PlaceItemPrefab.SetActive(false);

            m_ResultLabel = Body.GetElement<TextMeshProUGUI>("Results");

            Body.GetElement<Button>("Top/Back").onClick.AddListener(OnBackClick);
        }

        protected override void OnScreenShow() {
            m_SearchArea.Clear();
            SetResultsText(0);
        }

        protected override void OnScreenHide() {
            SetPlaces(null);
        }

        void SetResultsText(int n) {
            m_ResultLabel.text = string.Format(Localize(EGRLanguageData._b__0___b__RESULTS), n);
        }

        public void SetPlaces(List<EGRWTEProxyPlace> places) {
            m_PlaceItemPool.FreeAll();
            m_Items.Clear();

            if (places != null) {
                foreach (EGRWTEProxyPlace place in places) {
                    PlaceItem item = m_PlaceItemPool.Rent();
                    item.SetInfo(place.Name, place.Tags.StringifyList(", "), place.CID);
                    item.SetActive(true);
                    m_Items.Add(item);

                    Debug.Log("Added " + place.Name);
                }

                SetResultsText(places.Count);
            }
        }

        void OnBackClick() {
            HideScreen();
        }

        void OnItemHide(PlaceItem item) {
            item.SetActive(false);
        }

        void ClearFocusedItems() {
            foreach (PlaceItem place in m_Items) {
                place.SetActive(true);
                place.Transform.SetSiblingIndex(m_PlaceItemPool.GetIndex(place));
            }
        }

        void SetFocusedItems(List<PlaceItem> items) {
            foreach (PlaceItem place in m_Items) {
                int idx = items.FindIndex(p => p == place);
                if (idx != -1) {
                    place.SetActive(true);
                    place.Transform.SetSiblingIndex(idx);
                }
                else {
                    place.SetActive(false);
                }
            }
        }
    }
}