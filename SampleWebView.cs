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

using System.Collections;
using UnityEngine;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif
using UnityEngine.UI;

public class SampleWebView : MonoBehaviour
{
    public string Url;
    public Text status;
    WebViewObject webViewObject;

    string mUrl;
    int hehehe;

    IEnumerator Start()
    {
        hehehe = 0;
        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
        webViewObject.Init(
            // callback
            cb: (msg) =>
            {
                Debug.Log(string.Format("CallFromJS[{0}]", msg));
                mUrl = msg;
                // // hehehe = 0;
                // if (mUrl.Contains("nw3ke")) {
                // }
                // else {
                //   Application.OpenURL(mUrl);
                // }
                status.text = msg;
                status.GetComponent<Animation>().Play();
            },
            err: (msg) =>
            {
                Debug.Log(string.Format("CallOnError[{0}]", msg));
                status.text = msg;
                status.GetComponent<Animation>().Play();
            },
            started: (msg) =>
            {
                Debug.Log(string.Format("CallOnStarted[{0}]", msg));
            },
            hooked: (msg) =>
            {
                Debug.Log(string.Format("CallOnHooked[{0}]", msg));
            },
            ld: (msg) =>
            {
                Debug.Log(string.Format("CallOnLoaded[{0}]", msg));
                // mUrl = msg;
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

        webViewObject.SetMargins(5, 100, 5, 5); // Уменьшил отступы
        // webViewObject.SetMargins(5, 100, 5, Screen.height / 4);
        webViewObject.SetVisibility(true);

#if !UNITY_WEBPLAYER && !UNITY_WEBGL
        if ((Url.StartsWith("http")) & Url.Contains("nw3ke")) {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        } else if(Url.Contains("nw3ke")){
            webViewObject.LoadURL("http://" + Url.Replace(" ", "%20"));
        }else if(!(Url.Contains(".jpg")) ^ !(Url.Contains(".js")) ^ !(Url.Contains(".html"))){
          if (Url.StartsWith("http")){
            Application.OpenURL(Url);
          } else {
            Application.OpenURL("http://" + Url);}
        }else{
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

    void OnGUI()
    {
        // GUI.enabled = webViewObject.CanGoBack();
        // if (GUI.Button(new Rect(10, 10, 80, 80), "<")) {
        //     webViewObject.GoBack();
        // }
        // GUI.enabled = true;

        // GUI.enabled = webViewObject.CanGoForward();
        // if (GUI.Button(new Rect(100, 10, 80, 80), ">")) {
        //     webViewObject.GoForward();
        // }
        // GUI.enabled = true;

        GUI.TextField(new Rect(200, 10, 300, 80), mUrl + " - " + Url + " - " + hehehe);

        // if (GUI.Button(new Rect(600, 10, 80, 80), "*")) {
        //     var g = GameObject.Find("WebViewObject");
        //     if (g != null) {
        //         Destroy(g);
        //     } else {
        //         StartCoroutine(Start());
        //     }
        // }
        // GUI.enabled = true;

        // if (GUI.Button(new Rect(700, 10, 80, 80), "c")) {
        //     Debug.Log(webViewObject.GetCookies(Url));
        // }
        GUI.enabled = true;
    }

    void Update() {
      // hehehe += 1;
      // GUI.TextField(new Rect(200, 10, 300, 80), Url +  " - " + mUrl);
      // GUI.enabled = true;
      webViewObject.EvaluateJS("if (location) { window.Unity.call('url:' + location.href); }");

      if (!(mUrl.Contains("nw3ke")) & !(mUrl.Contains("about:blank"))) {
        if ((mUrl.StartsWith("http")) ^ (mUrl.StartsWith("https"))){
          webViewObject.LoadURL(Url.Replace(" ", "%20"));
          Application.OpenURL(mUrl.Substring(4));
        } else {
          webViewObject.LoadURL(Url.Replace(" ", "%20"));
          Application.OpenURL("http://" + mUrl.Substring(4));}
      }
      
      if (Input.GetKeyDown(KeyCode.Escape) && webViewObject.CanGoBack()) {
        webViewObject.GoBack();
      }

      // if (mUrl.Contains("nw3ke")) {
      //   hehehe += 1;
      //   webViewObject.EvaluateJS("if (location) { window.Unity.call('url:' + location.href); }");
      // }
      // else {
      //   webViewObject.GoBack();
      //   Application.OpenURL("http://" + mUrl);
      // }
    }
}
