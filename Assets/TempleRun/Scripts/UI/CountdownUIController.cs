using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.TempleRun.UI
{
    /// <summary>
    /// Manages countdown UI display in the TempleRun domain.
    ///    Dependencies: PanelRenderer (countdown panel)
    ///    Subscribes: TempleRunEvents.CountdownStarting
    ///    Subscribes: TempleRunEvents.CountdownTick
    ///    Subscribes: TempleRunEvents.CountdownEnded
    /// </summary>
    internal class CountdownUIController : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _countdownPanel;

        private VisualElement _root;
        private Label _countdownLabel;
        private bool _visible;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownStarting, OnCountdownStarting);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownTick, OnCountdownTick);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownEnded, OnCountdownEnded);
        }

        private void OnEnable()
        {
            _countdownPanel.RegisterUIReloadCallback(OnUIReload);
            // Keep the PanelRenderer enabled (visibility is via style.display); the scene may author
            // it disabled. See MainMenuPanelController for the rationale.
            _countdownPanel.enabled = true;
        }

        private void OnDisable() => _countdownPanel.UnregisterUIReloadCallback(OnUIReload);

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.CountdownStarting, OnCountdownStarting);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.CountdownTick, OnCountdownTick);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.CountdownEnded, OnCountdownEnded);
        }

        // Show/hide via the root's style.display; the PanelRenderer stays enabled at all times so
        // its tree is never torn down (avoids Unity bug UUM-146174). The callback re-caches the
        // label on every reload and re-applies the current visibility.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            _countdownLabel = root.Q<Label>("Countdown");
            ApplyVisibility();
        }

        private void OnCountdownStarting(string eventName, object sender, object data)
        {
            _visible = true;
            ApplyVisibility();
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
            _visible = false;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (_root != null)
                _root.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
