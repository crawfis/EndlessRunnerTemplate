using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public class DebugLog : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("DebugLog Awake");
        }
        private void Start()
        {
            Debug.Log("DebugLog Start");
        }
    }
}