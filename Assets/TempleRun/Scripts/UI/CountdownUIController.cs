using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.TempleRun.UI
{
    /// <summary>
    /// Manages countdown UI display in the TempleRun domain.
    ///    Dependencies: UIDocument (countdown panel)
    ///    Subscribes: TempleRunEvents.CountdownStarting
    ///    Subscribes: TempleRunEvents.CountdownTick
    ///    Subscribes: TempleRunEvents.CountdownEnded
    /// </summary>
    internal class CountdownUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument _countdownUI;

        private Label _countdownLabel;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownStarting, OnCountdownStarting);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownTick, OnCountdownTick);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownEnded, OnCountdownEnded);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.CountdownStarting, OnCountdownStarting);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.CountdownTick, OnCountdownTick);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.CountdownEnded, OnCountdownEnded);
        }

        private void OnCountdownStarting(string eventName, object sender, object data)
        {
            if (_countdownUI == null) return;

            SetActive(true);
            _countdownLabel = _countdownUI.rootVisualElement.Q<Label>("Countdown");
        }

        private void OnCountdownTick(string eventName, object sender, object data)
        {
            if (_countdownLabel != null)
            {
                int seconds = (int)data;
                _countdownLabel.text = (seconds + 1).ToString();
            }
        }

        private void OnCountdownEnded(string eventName, object sender, object data)
        {
            SetActive(false);
        }

        private void SetActive(bool on)
        {
            if (_countdownUI == null) return;
            _countdownUI.gameObject.SetActive(on);
            if (_countdownUI.rootVisualElement != null)
                _countdownUI.rootVisualElement.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
