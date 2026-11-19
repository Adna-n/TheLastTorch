// ButtonSounds: plays the UI hover blip when the mouse enters a button and the click blip when
// it is pressed. Added automatically to every button made by UIFactory.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastTorch
{
    [RequireComponent(typeof(Button))]
    public class ButtonSounds : MonoBehaviour, IPointerEnterHandler
    {
        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => { if (AudioManager.Instance != null) AudioManager.Instance.Play(SoundId.UiClick); });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.interactable && AudioManager.Instance != null) AudioManager.Instance.Play(SoundId.UiHover);
        }
    }
}
