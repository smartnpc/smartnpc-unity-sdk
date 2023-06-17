using UnityEngine;

namespace SmartNPC
{
    public class BaseMonoBehaviour : MonoBehaviour
    {
        public T GetOrAddComponent<T>() where T : Component
        {
            T result = gameObject.GetComponent<T>();

            if (result == null) result = gameObject.AddComponent<T>();

            return result;
        }

        public T FindOrAddObjectOfType<T>() where T : Component
        {
            T result = FindObjectOfType<T>();

            if (result == null) result = gameObject.AddComponent<T>();

            return result;
        }
    }
}