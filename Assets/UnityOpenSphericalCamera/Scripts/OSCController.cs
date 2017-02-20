using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using LitJson;

namespace UnityOpenSphericalCamera
{
    public class OSCController : MonoBehaviour
    {
        // Http adress
        private const string c_HttpHead = "http://";
        private const string c_HttpPort = "80";
        // Select OpenSphericalCamera IP
        // Gear360:192.168.107.1 / Bublcam:192.168.0.100 / RICOH THETA:192.168.1.1
        [SerializeField]
        private string m_IPAddress = "192.168.107.1";

        // Set picture size
        // TODO: Auto set params
        [SerializeField] private int m_PictureWidth = 3840;
        [SerializeField] private int m_PictureHeight = 1920;

        private readonly float c_TimeOutSec = 5f;
        private readonly float c_LoadCheckLoopSec = 0.2f;

        public class ExecCommandResponse
        {
            public byte[] bytes;
            public string name;
            public string state;
            public string id;
            public Dictionary<string, double> progress;
            public Dictionary<string, object> results;
        }

        public class StatusResponse
        {
            public string name;
            public string state;
            public Dictionary<string, double> progress;
            public Dictionary<string, string> results;
        }

        public void GetInfo()
        {
            StartCoroutine(ExecGetInfoCoroutine((infoText)=> { Debug.Log(infoText); }, (errorText)=> { Debug.LogError(errorText); }));
        }

        public void ExecTakePictureAndLoadImage(Action<Texture2D> success, Action error)
        {
            StartCoroutine(ExecTakePictureAndLoadImageCoroutine(success, error));
        }

        private IEnumerator StartSessionCoroutine(Action<ExecCommandResponse> success, Action<string> error)
        {
            JsonData parameters = new JsonData();
            parameters["timeout"] = 180;
            JsonData s_data = new JsonData();
            s_data["name"] = "camera.startSession";
            s_data["parameters"] = parameters;
            string s_postJsonStr = s_data.ToJson();
            Debug.Log(s_postJsonStr);
            yield return StartCoroutine(
                ExecCommandCoroutine(
                    s_postJsonStr,
                    (response) =>
                    {
                        success(response);
                    },
                    (errorText)=>
                    {
                        error(errorText);
                    }
                ));
        }

        private IEnumerator ExecTakePictureAndLoadImageCoroutine(Action<Texture2D> success, Action error)
        {
            string sessionID = "";
            string commandID = "";
            string fileURI = "";
            bool hasError = false;

            // Session start
            yield return StartCoroutine(StartSessionCoroutine((response)=> {
                // Set session id
                sessionID = response.results["sessionId"].ToString();
            },
            (errorText) => {
                Debug.LogError(errorText);
                hasError = true;
            }
            ));

            // Error check
            if (hasError)
            {
                yield break;
            }

            // Take picture
            yield return StartCoroutine(
                ExecTakePictureCommandCoroutine(
                    sessionID,
                    (response) => {
                        commandID = response.id;
                    },
                    (errorText)=> {
                        Debug.LogError(errorText);
                        hasError = true;
                    }
            ));

            // Error check
            if (hasError)
            {
                yield break;
            }

            // Wait capture
            yield return StartCoroutine(
                ExecGetStatusCoroutine(
                    commandID,
                    (response) => {
                        fileURI = response.results["fileUri"].ToString();
                    },
                    (errorText) => {
                        Debug.LogError(errorText);
                        hasError = true;
                    }
                    ));

            // Error check
            if (hasError)
            {
                yield break;
            }

            // Load image texture
            yield return StartCoroutine(
                GetImageCoroutine(
                    fileURI,
                    (texture) => {
                        // Success load texture!
                        success(texture);
                    },
                    (errorText) => {
                        Debug.LogError(errorText);
                        hasError = true;
                    }
                    ));

            // Error check
            if (hasError)
            {
                yield break;
            }
        }

        private IEnumerator ExecTakePictureCommandCoroutine(string sessionID, Action<ExecCommandResponse> success, Action<string> error)
        {
            // Set POST params
            JsonData parameters = new JsonData();
            parameters["sessionId"] = sessionID;
            JsonData data = new JsonData();
            data["name"] = "camera.takePicture";
            data["parameters"] = parameters;
            string postJsonStr = data.ToJson();
            byte[] postBytes = Encoding.Default.GetBytes(postJsonStr);
            yield return StartCoroutine(
                ExecCommandCoroutine(
                    postJsonStr,
                    (response) =>
                    {
                        success(response);
                    },
                    (errorText) =>
                    {
                        error(errorText);
                    }
                ));
        }

        private IEnumerator ExecCommandCoroutine(string postJsonStr, Action<ExecCommandResponse> success, Action<string> error)
        {
            // Set URL
            string url = MakeAPIURL("/osc/commands/execute");

            // Set header
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Content-Type", "application/json; charset=utf-8");

            // Set POST params
            byte[] postBytes = Encoding.Default.GetBytes(postJsonStr);

            var timeOutSec = Time.realtimeSinceStartup + c_TimeOutSec;

            // Start download
            WWW www = new WWW(url, postBytes, header);
            while (!www.isDone && Time.realtimeSinceStartup < timeOutSec)
            {
                yield return 0;
            }

            // Timeout check
            if (!www.isDone)
            {
                www.Dispose();
                www = null;
                error("connect time out");
                yield break;
            }

            // Output data
            if (www.error == null)
            {
                // success
                ExecCommandResponse response = JsonMapper.ToObject<ExecCommandResponse>(www.text);
                success(response);
            }
            else
            {
                // error
                error(www.error);
            }

            www.Dispose();
            www = null;
        }

        private IEnumerator ExecGetStatusCoroutine(string commandID, Action<StatusResponse> success, Action<string> error)
        {
            // Set URL
            string url = MakeAPIURL("/osc/commands/status");

            // Set header
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Content-Type", "application/json; charset=utf-8");

            // Set POST params
            JsonData data = new JsonData();
            data["id"] = commandID;
            string postJsonStr = data.ToJson();
            byte[] postBytes = Encoding.Default.GetBytes(postJsonStr);

            // Start download
            WWW www = new WWW(url, postBytes, header);
            yield return www;

            // Output data
            if (www.error == null)
            {
                StatusResponse response = JsonMapper.ToObject<StatusResponse>(www.text);
                if (response.progress != null)
                {
                    // Restart load
                    yield return new WaitForSeconds(c_LoadCheckLoopSec);
                    yield return StartCoroutine(ExecGetStatusCoroutine(commandID, success, error));
                }
                else
                {
                    // Finish command 
                    success(response);
                }
            }
            else
            {
                error(www.error);
            }

            www.Dispose();
            www = null;
        }

        private IEnumerator ExecGetInfoCoroutine(Action<string> success, Action<string> error)
        {
            // Make URL
            string url = MakeAPIURL("/osc/info");

            // Post data
            WWW www = new WWW(url);
            yield return www;

            // Start download
            if (www.error == null)
            {
                success(www.text);
            }
            else
            {
                error(www.error);
            }

            www.Dispose();
            www = null;
        }

        private IEnumerator GetImageCoroutine(string fileUri, Action<Texture2D> success, Action<string> error)
        {
            // Make URL
            string url = MakeAPIURL("/osc/commands/execute");

            // Set header
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Content-Type", "application/json; charset=utf-8");

            // Set POST params
            JsonData parameters = new JsonData();
            parameters["fileUri"] = fileUri;
            Debug.Log(fileUri);
            JsonData data = new JsonData();
            data["name"] = "camera.getImage";
            data["parameters"] = parameters;
            string postJsonStr = data.ToJson();
            byte[] postBytes = Encoding.Default.GetBytes(postJsonStr);

            // Post data
            WWW www = new WWW(url, postBytes, header);
            yield return www;

            // 結果出力
            if (www.error == null)
            {
                Texture2D texture = new Texture2D(m_PictureWidth, m_PictureHeight);
                texture.LoadImage(www.bytes);
                success(texture);
            }
            else
            {
                error(www.error);
            }
        }

        // Make URI
        private string MakeAPIURL(string command)
        {
            return string.Format("{0}{1}:{2}{3}", c_HttpHead, m_IPAddress, c_HttpPort, command);
        }
    }
}