using UnityEngine;

namespace CrawfisSoftware.Scriptables
{
    [CreateAssetMenu(fileName = "randomSeed", menuName = "delete me")]
    public class ScriptableInt : ScriptableObject
    {
        [SerializeField] public int m_Value = 0;
    }
}