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

public class SampleWebView : MonoBehaviour
{
	public string Url;
	public Text status;
	WebViewObject webViewObject;

	bool dev = true; //FLAG IT
	bool tredInit = false;

	int StripStartTagsCount = 0;
	int UpdateCount = 0;
	int timerCount = 0;
	public Text tag1;
	public Text tag2;
	public Text tag3;
	public Text tag4;

	bool openingExtUrl = false;
	bool errStatus = true;
	string mUrl;
	string openUrl = "";
	string localUrl;
	string devStatus;
	string connStatus = "NoErrors";
	string[] appsFlyerData = {"id"};

	public float waitTime = 2f;
    float timer;

	IEnumerator Start()
	{
		//  webView Init
		webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
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
				AppsFlyer.initSDK("edd67iJkn2KvUu77AH4BQf", "WebViewMaster");
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

#if UNITY_EDITOR_OSX || !UNITY_ANDROID
				// NOTE: depending on the situation, you might prefer
				// the 'iframe' approach.
				// cf. https://github.com/gree/unity-webview/issues/189
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
#endif
				//webViewObject.EvaluateJS(@"Unity.call('ua=' + navigator.userAgent)");
			},
			//ua: "custom user agent string",
#if UNITY_EDITOR
			separated: false,
#endif
			enableWKWebView: true);
			//TimerCallback tmCallback = new TimerCallback(ConnectionTesting); 
			//Timer timer = new Timer(tmCallback,null,0,10000);
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		webViewObject.bitmapRefreshCycle = 1;
#endif
		// cf. https://github.com/gree/unity-webview/pull/512
		// Added alertDialogEnabled flag to enable/disable alert/confirm/prompt dialogs. by KojiNakamaru · Pull Request #512 · gree/unity-webview
		//webViewObject.SetAlertDialogEnabled(false);

		// cf. https://github.com/gree/unity-webview/pull/550
		// introduced SetURLPattern(..., hookPattern). by KojiNakamaru · Pull Request #550 · gree/unity-webview
		//webViewObject.SetURLPattern("", "^https://.*youtube.com", "^https://.*google.com");

		// cf. https://github.com/gree/unity-webview/pull/570
		// Add BASIC authentication feature (Android and iOS with WKWebView only) by takeh1k0 · Pull Request #570 · gree/unity-webview
		//webViewObject.SetBasicAuthInfo("id", "password");

		webViewObject.SetMargins(0, 120, 0, 0); //WebView size
		webViewObject.SetVisibility(true);

#if !UNITY_WEBPLAYER && !UNITY_WEBGL
	   if (Url.StartsWith("http")) {
			webViewObject.LoadURL(Url.Replace(" ", "%20"));
		} else {
			var exts = new string[]{
				".jpg",
				".js",
				".html"  // should be last
			};
			foreach (var ext in exts) {
				var url = Url.Replace(".html", ext);
				var src = System.IO.Path.Combine(Application.streamingAssetsPath, url);
				var dst = System.IO.Path.Combine(Application.persistentDataPath, url);
				byte[] result = null;
				if (src.Contains("://")) {  // for Android
#if UNITY_2018_4_OR_NEWER
					// NOTE: a more complete code that utilizes UnityWebRequest can be found in https://github.com/gree/unity-webview/commit/2a07e82f760a8495aa3a77a23453f384869caba7#diff-4379160fa4c2a287f414c07eb10ee36d
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
#else
		if (Url.StartsWith("http")) {
			webViewObject.LoadURL(Url.Replace(" ", "%20"));
		} else {
			webViewObject.LoadURL("StreamingAssets/" + Url.Replace(" ", "%20"));
		}
		webViewObject.EvaluateJS(
			"parent.$(function() {" +
			"   window.Unity = {" +
			"       call:function(msg) {" +
			"           parent.unityWebView.sendMessage('WebViewObject', msg)" +
			"       }" +
			"   };" +
			"});");
#endif
		yield break;
	}
	
	private void StripStartTags(string item){
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

	void OnGUI()
	{
		tag1.text = string.Format("Call Open Site[{0}]", StripStartTagsCount);
		tag2.text = string.Format("Call Update[{0}]", UpdateCount);
		tag3.text = string.Format("Status[{0}]", devStatus);
		tag4.text = string.Format("Timer Status[{0}]", timerCount);
		GUI.enabled = true;
	}

	void LoadStaticHtml(){
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

	// If loading domain != current domain then open it in standart browser
	// And waiting for key "back" to load previous page
	void Update() {

		UpdateCount++;

		webViewObject.EvaluateJS("if (location) { window.Unity.call('url:' + location.href); }");

		if (connStatus == "NoErrors") {

			devStatus = "CONNECTION SUCCESSFUL";

			// [BUG REPORT] За несколько апдейтов происходит несколько запросов
			if (!(mUrl.Contains("nw3ke")) & !(mUrl.Contains("about:blank"))) {
				openUrl = mUrl;
				devStatus = "LOADING EXTERNAL URL";
				webViewObject.GoBack();
			}

			if ((webViewObject.Progress() == 100) & (openUrl != "") & (!openingExtUrl)){
				StripStartTags(openUrl.Substring(4));
				connStatus = "LoadingEXT";
			}

		} else if (errStatus){
			devStatus = "LOADING LOCAL URL";
			LoadStaticHtml(); 
		}

		timer += Time.deltaTime;

        if (timer > waitTime) { 
			timerCount++;
            if (Application.internetReachability == NetworkReachability.NotReachable) {
                //Debug.Log ("No internet access");
				connStatus = "-2";
				LoadStaticHtml();
            } else {
                //Debug.Log ("Internet connection OK");
				connStatus = "NoErrors";
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