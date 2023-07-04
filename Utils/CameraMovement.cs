using UnityEngine;

namespace SmartNPC
{
    public class CameraMovement : MonoBehaviour
    {
        [SerializeField] private float mouseSensitivity = 100f;

        private SmartNPCPlayer player;
        private new CapsuleCollider collider;
        private float xRotation = 0f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            player = FindObjectOfType<SmartNPCPlayer>();

            if (player) collider = player.GetComponent<CapsuleCollider>();
        }
        
        private void Update()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            if (player)
            {
                player.transform.Rotate(Vector3.up * mouseX);

                float yPosition = collider ? collider.height / 2 : 0;

                transform.position = player.transform.position + new Vector3(0, yPosition, 0);
                
                Vector3 newRotation = transform.rotation.eulerAngles;

                newRotation.x = xRotation;
                newRotation.y = player.transform.rotation.eulerAngles.y;

                transform.rotation = Quaternion.Euler(newRotation);
            }
        }
    }
}