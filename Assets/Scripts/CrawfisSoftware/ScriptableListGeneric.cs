using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.Unity3D.Utility
{
    public class ScriptableListGeneric<T> : ScriptableObject
    {
        [SerializeField] public List<T> List;
    }
}