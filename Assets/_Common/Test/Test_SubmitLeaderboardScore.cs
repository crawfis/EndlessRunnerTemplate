using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Events;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.UGS.Leaderboard.Test
{
    public class Test_SubmitLeaderboardScore : MonoBehaviour
    {
        [SerializeField] private float _minValue = 10;
        [SerializeField] private float _maxValue = 300;
        [SerializeField] private int _numberOfTimesToSubmit = 2;
        [SerializeField] private int _initialDelayInSeconds = 1;
        [SerializeField] private int _delayBetweenSubmissionsInSeconds = 2;
        [SerializeField] private bool _endGameAfterSubmissions = true;
        void Start()
        {
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameStarted, OnGameStarted);
        }

        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameStarted, OnGameStarted);
        }
        private void OnGameStarted(string eventName, object sender, object data)
        {
            StartCoroutine(SubmitScoresCoroutine());
        }

        private IEnumerator SubmitScoresCoroutine()
        {
            yield return new WaitForSeconds(_initialDelayInSeconds);
            for (int i = 0; i < _numberOfTimesToSubmit; i++)
            {
                float randomScore = UnityEngine.Random.Range((int)_minValue, (int)_maxValue + 1);
                EventsPublisherUGS.Instance.PublishEvent(Events.UGS_EventsEnum.ScoreUpdating, this, randomScore);
                yield return new WaitForSeconds(_delayBetweenSubmissionsInSeconds);
            }
            if(_endGameAfterSubmissions)
            {
                Debug.Log("All scores submitted. Ending game.");
                EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.GameEnded, this, null);
            }
        }
    }
}