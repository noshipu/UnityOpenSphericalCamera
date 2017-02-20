using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityOpenSphericalCamera
{
    public class TakePictureManagerSample : MonoBehaviour
    {
        [SerializeField] private RawImage m_TargetCaptureImage;

        private OSCController m_OSCController;

        void Start()
        {
            m_OSCController = FindObjectOfType<OSCController>();
        }

        // Take capture
        public void OnClickTakePicture()
        {
            m_OSCController.ExecTakePictureAndLoadImage(
                (texture) => {
                    m_TargetCaptureImage.texture = texture;
                    Debug.Log("Finish take picture");
                },
                () => {
                    Debug.LogError("Faild take picture.");
                }
                );
        }
    }
}