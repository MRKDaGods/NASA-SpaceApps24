using UnityEngine.UI;

namespace MRK.UI
{
    public class EGRScreenRefs : EGRScreenAnimatedAlpha
    {
        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            GetElement<Button>("imgBg/bBack").onClick.AddListener(() => HideScreen());
        }
    }
}
