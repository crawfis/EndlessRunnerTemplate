using UnityEngine;

namespace CrawfisSoftware.Unity3D.Utility
{
    [CreateAssetMenu(fileName ="randomSeed", menuName ="delete me")]
    public class ScriptableInt : ScriptableObject
    {
        [SerializeField] public int m_Value = 0;
    }
}