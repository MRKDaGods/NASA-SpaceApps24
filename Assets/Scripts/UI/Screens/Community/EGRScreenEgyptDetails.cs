using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class EGRScreenEgyptDetails : EGRScreen, IEGRScreenSupportsBackKey
    {
        private MRKPolygonsController PolygonsController => MRKPolygonsController.Instance;

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            GetElement<Button>("imgBg/bBack").onClick.AddListener(OnBackKeyDown);
            GetElement<Button>("imgBg/Scroll View/Viewport/Content/Layout/ExploreComms").onClick.AddListener(OnExploreClick);
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            m_LastGraphicsBuf = new Graphic[] { GetElement<Graphic>("imgBg") };

            PushGfxState(EGRGfxState.Position);

            // slide up
            // cur y
            var gfx = m_LastGraphicsBuf[0];

            var oldY = gfx.rectTransform.anchoredPosition.y;
            gfx.rectTransform.anchoredPosition -= new Vector2(0f, Screen.height);

            gfx.rectTransform.DOAnchorPosY(oldY, TweenMonitored(0.3f))
                .ChangeStartValue(-Screen.height)
                .SetEase(Ease.OutSine);
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            SetTweenCount(m_LastGraphicsBuf.Length);

            // slide down
            float y = m_LastGraphicsBuf[0].rectTransform.anchoredPosition.y;
            m_LastGraphicsBuf[0].rectTransform.DOAnchorPosY(-Screen.height, TweenMonitored(0.3f))
                .SetEase(Ease.OutSine)
                .OnComplete(OnTweenFinished);

            return true;
        }

        protected override void OnScreenShow()
        {
            PolygonsController.PolygonBlockerSemaphore++;
        }

        protected override void OnScreenHide()
        {
            PolygonsController.PolygonBlockerSemaphore--;
        }

        public void OnBackKeyDown()
        {
            HideScreen();
        }

        void OnExploreClick()
        {
            // hide us
            HideScreen();

            // show cities
            PolygonsController.SelectionMode = SelectionMode.City;
        }
    }
}
