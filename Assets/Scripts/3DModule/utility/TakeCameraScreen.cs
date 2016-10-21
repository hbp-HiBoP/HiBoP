//using UnityEngine;
//using System.Collections;

//public class TakeCameraScreen : MonoBehaviour
//{
//    int resWidth;
//    int resHeight;
//    public static int name;

//    void Start()
//    {
//        name = 0;
//    }

//    public static string ScreenShotName(int width, int height)
//    {
//        name++;
//        return Application.dataPath + "/screenshots/" + name.ToString() + ".png";

//    }

//    void Update()
//    {
//        //if (Input.GetKeyUp("k"))
//        //{
//        //    Debug.Log("screen");
//        //    //gameObject.GetComponent<Camera>().enabled = false;
//        //    takePics();
//        //}
//    }

//    public void takePics()
//    {
//        foreach (GameObject go in GameObject.FindGameObjectsWithTag("SingleCamera"))
//        {
//            Camera cam = go.GetComponent<Camera>();
//            resWidth = (int)cam.pixelWidth;
//            resHeight = (int)cam.pixelHeight;
//            Debug.Log(resWidth + " " + resHeight + " " + Application.dataPath + "/screenshots/" + name.ToString() + ".png");
//            //cam.Render();
//            RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
//            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
//            RenderTexture.active = rt;
//            screenShot.ReadPixels(new Rect(cam.pixelRect), 0, 0);
//            byte[] bytes = screenShot.EncodeToPNG();
//            string filename = ScreenShotName(resWidth, resHeight);
//            //Debug.Log(filename);
//            //System.IO.File.WriteAllBytes(filename, bytes);
//        }
//    }
//}