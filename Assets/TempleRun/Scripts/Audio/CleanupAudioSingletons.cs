using GTMY.Audio;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace CrawfisSoftware.TempleRun.Audio
{
    internal class CleanupAudioSingletons : MonoBehaviour
    {
        private void OnDestroy()
        {
            AudioManagerSingleton.Instance.ClearAll();
        }
    }
}
