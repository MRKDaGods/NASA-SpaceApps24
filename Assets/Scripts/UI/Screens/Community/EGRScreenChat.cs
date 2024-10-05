using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class EGRScreenChat : EGRScreenAnimatedAlpha
    {
        TMP_InputField m_Textbox;

        [SerializeField]
        Transform m_ChatMessagePrefab;

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            GetElement<Button>("imgBg/bBack").onClick.AddListener(() => HideScreen());
            GetElement<Button>("imgBg/bSend").onClick.AddListener(SendChatMessage);

            m_Textbox = GetElement<TMP_InputField>("imgBg/Textbox");

            m_ChatMessagePrefab.gameObject.SetActive(false);
        }

        protected override void OnScreenShow()
        {
            m_Textbox.text = "";
        }

        void SendChatMessage()
        {
            var msg = Instantiate(m_ChatMessagePrefab, m_ChatMessagePrefab.parent);
            msg.Find("Username").GetComponent<TextMeshProUGUI>().text = EGRLocalUser.Instance.FullName;
            msg.Find("Text").GetComponent<TextMeshProUGUI>().text = m_Textbox.text;
            msg.gameObject.SetActive(true);

            m_Textbox.text = "";
        }
    }
}
