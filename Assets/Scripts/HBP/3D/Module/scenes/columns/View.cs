using UnityEngine;

namespace HBP.Module3D
{
    public class View : MonoBehaviour
    {
        #region Properties
        public Cam.TrackBallCamera Camera { get; set; }
        #endregion

        #region Private Methods
        public void Awake()
        {
            Camera = transform.GetComponentInChildren<Cam.TrackBallCamera>();
        }
        #endregion

        #region Public Methods

        #endregion
    }
}