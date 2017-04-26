using UnityEngine;

namespace HBP.Module3D
{
    public class View : MonoBehaviour
    {
        public Cam.TrackBallCamera Camera { get; set; }
        public GameObject CameraPrefab;
        
        public void Awake()
        {
            Camera = transform.GetComponentInChildren<Cam.TrackBallCamera>();
        }
    }
}