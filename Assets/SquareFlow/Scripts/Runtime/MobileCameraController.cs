using UnityEngine;

namespace SquareFlow.Runtime
{
    [RequireComponent(typeof(Camera))]
    public sealed class MobileCameraController : MonoBehaviour
    {
        public const float PortraitReferenceHeightWorldUnits = 19.2f;

        private Camera targetCamera;

        public Camera Camera
        {
            get
            {
                if (targetCamera == null)
                    targetCamera = GetComponent<Camera>();
                return targetCamera;
            }
        }

        public void Configure(Color background)
        {
            Camera.orthographic = true;
            Camera.orthographicSize = PortraitReferenceHeightWorldUnits * 0.5f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = background;
            Transform cameraTransform = Camera.transform;
            cameraTransform.position = new Vector3(0f, 0f, -10f);
            cameraTransform.rotation = Quaternion.identity;
        }
    }
}
