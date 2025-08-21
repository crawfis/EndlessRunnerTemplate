using UnityEngine;

namespace Crawfis.CSE5559.Lab1
{
    public static class GameObjectExtensions
    {
        public static void CopyScripts(this GameObject instance, GameObject template)
        {
            instance.SetActive(false);
            var componentsToAdd = template.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in componentsToAdd)
            {
                if (component.GetType() == typeof(Transform)) continue;
                MonoBehaviour script = instance.AddComponent(component.GetType()) as MonoBehaviour;
                script.enabled = component.enabled;
            }
            instance.SetActive(true);
        }
    }
}