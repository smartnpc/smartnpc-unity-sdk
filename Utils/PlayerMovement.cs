using UnityEngine;

namespace SmartNPC
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float speed;

        void Update()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            gameObject.transform.Translate(speed / 10 * horizontal, 0.0f, speed / 10 * vertical);
        }
    }
}