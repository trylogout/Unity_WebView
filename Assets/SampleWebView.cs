/*
 * Copyright (C) 2012 GREE, Inc.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

#region Header

using AppsFlyerSDK;
using System.Collections;
using UnityEngine;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Threading;
using UnityEngine.Android;

#endregion Header

public class SampleWebView : MonoBehaviour
{

#region Fields

	public string Url;
	public Text status;
	WebViewObject webViewObject;

	bool inRequestingCameraPermission;

	void OnApplicationFocus(bool hasFocus){
		if (inRequestingCameraPermission && hasFocus) {
			inRequestingCameraPermission = false;
		}
	}

	// For Debug Panel
	public bool dev = true;
	int StripStartTagsCount = 0;
	int UpdateCount = 0;
	int timerCount = 0;
	int cameraCount = 0;
	public Text tag1;
	public Text tag2;
	public Text tag3;
	public Text tag4;
	public Text tag5;

	bool errStatus = true;
	string mUrl;
	string openUrl = "";
	string localUrl;
	string devStatus;
	string connStatus = "NoErrors";
	string[] appsFlyerData = {"id"};

	public float waitTime = 1f;
    float timer;

#endregion Fields

	IEnumerator Start()
	{
		//  webView Init
		webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();

		if (!Permission.HasUserAuthorizedPermission(Permission.Camera)){
			inRequestingCameraPermission = true;
			Permission.RequestUserPermission(Permission.Camera);
			cameraCount++;
		}        
		while (inRequestingCameraPermission) {
			yield return new WaitForSeconds(0.5f);
		}

		webViewObject.Init(
			// Callback
			cb: (msg) =>
			{
				Debug.Log(string.Format("CallFromJS[{0}]", msg));
				mUrl = msg;
			},
			// On error
			err: (msg) =>
			{
				Debug.Log(string.Format("CallOnError[{0}]", msg));
				connStatus = msg;
			},
			// When started
			started: (msg) =>
			{
				// AppsFlyer Init START
				AppsFlyer.initSDK("DEV_ID", "APP_NAME");
				AppsFlyer.startSDK();

				string tempSettingsPath = Application.persistentDataPath + "/AFUID.dat";

				// cf. https://github.com/trylogin START
				if (!System.IO.File.Exists(tempSettingsPath)){
					appsFlyerData[0] = AppsFlyer.getAppsFlyerId();
					System.IO.File.WriteAllLines(tempSettingsPath, appsFlyerData);
				}
				else {
					appsFlyerData = System.IO.File.ReadAllLines(tempSettingsPath);
				}
				// cf. https://github.com/trylogin END
				// AppsFlyer Init END

				Debug.Log(string.Format("CallOnStarted[{0}]", msg));
			},
			hooked: (msg) =>
			{
				Debug.Log(string.Format("CallOnHooked[{0}]", msg));
			},
			// When loaded
			ld: (msg) =>
			{
				Debug.Log(string.Format("CallOnLoaded[{0}]", msg));

#if UNITY_EDITOR_OSX || (!UNITY_ANDROID && !UNITY_WEBPLAYER && !UNITY_WEBGL)
#if true
				webViewObject.EvaluateJS(@"
				  if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
					window.Unity = {
					  call: function(msg) {
						window.webkit.messageHandlers.unityControl.postMessage(msg);
					  }
					}
				  } else {
					window.Unity = {
					  call: function(msg) {
						window.location = 'unity:' + msg;
					  }
					}
				  }
				");
#else
				webViewObject.EvaluateJS(@"
				  if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
					window.Unity = {
					  call: function(msg) {
						window.webkit.messageHandlers.unityControl.postMessage(msg);
					  }
					}
				  } else {
					window.Unity = {
					  call: function(msg) {
						var iframe = document.createElement('IFRAME');
						iframe.setAttribute('src', 'unity:' + msg);
						document.documentElement.appendChild(iframe);
						iframe.parentNode.removeChild(iframe);
						iframe = null;
					  }
					}
				  }
				");
#endif
#elif UNITY_WEBPLAYER || UNITY_WEBGL
                webViewObject.EvaluateJS(
                    "window.Unity = {" +
                    "   call:function(msg) {" +
                    "       parent.unityWebView.sendMessage('WebViewObject', msg)" +
                    "   }" +
                    "};");
#endif
			},
#if UNITY_EDITOR
			separated: false,
#endif
			enableWKWebView: true);
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		webViewObject.bitmapRefreshCycle = 1;
#endif

		if (dev){
			webViewObject.SetMargins(0, 120, 0, 0); //WebView size
		} else {
			webViewObject.SetMargins(0, 0, 0, 0); //WebView size
		}
		webViewObject.SetVisibility(true);

#if !UNITY_WEBPLAYER && !UNITY_WEBGL
	   if (Url.StartsWith("http")) {
			webViewObject.LoadURL(Url.Replace(" ", "%20"));
		} else {
			var exts = new string[]{
				".jpg",
				".js",
				".html"
			};
			foreach (var ext in exts) {
				var url = Url.Replace(".html", ext);
				var src = System.IO.Path.Combine(Application.streamingAssetsPath, url);
				var dst = System.IO.Path.Combine(Application.persistentDataPath, url);
				byte[] result = null;
				if (src.Contains("://")) {
#if UNITY_2018_4_OR_NEWER
					var unityWebRequest = UnityWebRequest.Get(src);
					yield return unityWebRequest.SendWebRequest();
					result = unityWebRequest.downloadHandler.data;
#else
					var www = new WWW(src);
					yield return www;
					result = www.bytes;
#endif
				} else {
					result = System.IO.File.ReadAllBytes(src);
				}
				System.IO.File.WriteAllBytes(dst, result);
				if (ext == ".html") {
					webViewObject.LoadURL("file://" + dst.Replace(" ", "%20"));
					break;
				}
			}
		}

		webViewObject.EvaluateJS(
		"window.Unity = {" +
		"   call:function(msg) {" +
		"       parent.unityWebView.sendMessage('WebViewObject', msg)" +
		"   }" +
		"};");
#else
		if (Url.StartsWith("http")) {
			webViewObject.LoadURL(Url.Replace(" ", "%20"));
		} else {
			webViewObject.LoadURL("StreamingAssets/" + Url.Replace(" ", "%20"));
		}
		webViewObject.EvaluateJS(@"
			if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
			window.Unity = {
				call: function(msg) {
				window.webkit.messageHandlers.unityControl.postMessage(msg);
				}
			}
			} else {
			window.Unity = {
				call: function(msg) {
				var iframe = document.createElement('IFRAME');
				iframe.setAttribute('src', 'unity:' + msg);
				document.documentElement.appendChild(iframe);
				iframe.parentNode.removeChild(iframe);
				iframe = null;
				}
			}
			}
		");
#endif
		yield break;
	}
	
	void OnGUI()
	{
		if (dev){
			tag1.text = string.Format("Call Open Site[{0}]", StripStartTagsCount);
			tag2.text = string.Format("Call Update[{0}]", UpdateCount);
			tag3.text = string.Format("Status[{0}]", devStatus);
			tag4.text = string.Format("Timer Status[{0}]", timerCount);
			tag5.text = string.Format("Camera Alert[{0}]", cameraCount);
		}
		GUI.enabled = true;
	}

	// Try to open local page from StreamingAssets.
	// That function copy index.html page from StreamingAssets to persistance path
	// And trying to open it in webView
	void LoadLocalPage(){
		errStatus = false;

		string localPage = "index.html";
		string assetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, localPage);
		string storagePath = Application.persistentDataPath + "/" + localPage;

		if (assetsPath.Contains("://")){
			WWW www = new WWW(assetsPath);
			while (!www.isDone) {}

			if (string.IsNullOrEmpty(www.error)){
				System.IO.File.WriteAllBytes(storagePath, www.bytes);
		  	} 
			webViewObject.LoadURL("file://" + storagePath);
		}
		else{
			webViewObject.LoadURL(assetsPath);
		}
		devStatus = "LOADED LOCAL URL";
	}

	// Check domain name. If it's inner domain - returns true
	bool notInnerUrl(string openUrl){
		if (!(openUrl.Contains("benaughty")) & !(openUrl.Contains("about:blank")) & !(openUrl.Contains(Application.persistentDataPath))){
			return true;
		} else {
			return false;
		}
	}

	// Try to open url in external browser
	private void LoadExternalPage(string item){
		StripStartTagsCount++;
		if ((item.Trim().StartsWith("https")) | (item.Trim().StartsWith("http"))){
			Application.OpenURL(item);
		}
		else {
			Application.OpenURL("http://" + item);
		}
 
		openUrl = "";
		devStatus = "LOADED EXTERNAL URL";
	}

	// Check internet connection every 1 second and try to open mUrl
	// If loading domain != current domain and it not local then open it in standart browser
	// And waiting for key "back" to load previous page
	void Update() {

		UpdateCount++;

		webViewObject.EvaluateJS("if (location) { window.Unity.call('url:' + location.href); }");

		timer += Time.deltaTime;

        if (timer > waitTime) { 
			timerCount++;
            if (Application.internetReachability == NetworkReachability.NotReachable) {
				connStatus = "-2";
				LoadLocalPage();
            } else {
				connStatus = "NoErrors";
				devStatus = "CONNECTION SUCCESSFUL";

				if (notInnerUrl(mUrl)) {
					openUrl = mUrl;
					devStatus = "LOADING EXTERNAL URL";
					webViewObject.GoBack();
				}

				if ((webViewObject.Progress() == 100) & (openUrl != "")){
					LoadExternalPage(openUrl.Substring(4));
					connStatus = "LoadingEXT";
				}
            }
            timer = 0f;
        }

		// cf. https://github.com/trylogin START
			if ((Input.GetKeyDown(KeyCode.Escape)) & (webViewObject.CanGoBack())) {
				webViewObject.GoBack();
		}
		// cf. https://github.com/trylogin START
	}
}