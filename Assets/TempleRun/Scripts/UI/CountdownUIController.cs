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
        private bool _initialized;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownStarting, OnCountdownStarting);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownTick, OnCountdownTick);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownEnded, OnCountdownEnded);
        }

        private void OnEnable() => _countdownPanel.RegisterUIReloadCallback(OnUIReload);

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

        // The PanelRenderer surfaces its visual tree only through this callback (it has no
        // root-tree property). The callback fires while the panel loads enabled, so we cache the
        // label here and hide the panel once — content persists, so re-enabling shows it again.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            _countdownLabel = root.Q<Label>("Countdown");

            if (!_initialized)
            {
                _initialized = true;
                _countdownPanel.enabled = false; // hidden until a countdown starts
            }
        }

        private void OnCountdownStarting(string eventName, object sender, object data)
        {
            if (_countdownPanel == null) return;

            _countdownPanel.enabled = true;

            // Defensive: if the reload callback has not run yet, resolve the label lazily.
            if (_countdownLabel == null && _root != null)
                _countdownLabel = _root.Q<Label>("Countdown");
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
            if (_countdownPanel != null)
                _countdownPanel.enabled = false;
        }
    }
}
