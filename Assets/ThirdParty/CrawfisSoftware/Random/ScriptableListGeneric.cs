using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.Scriptables
{
    public class ScriptableListGeneric<T> : ScriptableObject
    {
        [SerializeField] public List<T> List;
    }
}