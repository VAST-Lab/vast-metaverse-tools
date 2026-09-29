using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using VastMetaverseTools.Managers;

namespace VastMetaverseTools.External
{
    public class ExternalTools : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void OpenURLInNewTab(string url);
#endif

        public static void OpenURL(string url)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenURLInNewTab(url);
#else
            Application.OpenURL(url);
#endif
        }

        public static void TeleportToSpace(string spaceId, bool showConfirmPopup = true)
        {
            // TODO: Move between spaces/metaverse sites
        }

        public static string BuildURLWithHashParams(Dictionary<string, string> hashParams)
        {
            string baseUrl = MetaverseManager.WebsiteUrl;

            if (string.IsNullOrEmpty(baseUrl)) return string.Empty;
            if (hashParams == null || hashParams.Count == 0) return baseUrl;

            var sb = new StringBuilder(baseUrl);
            sb.Append('#');

            bool first = true;
            foreach (KeyValuePair<string, string> kvp in hashParams)
            {
                if (!first) sb.Append('&');
                sb.Append(UnityWebRequest.EscapeURL(kvp.Key));
                sb.Append('=');
                sb.Append(UnityWebRequest.EscapeURL(kvp.Value));
                first = false;
            }

            return sb.ToString();
        }

        // Use this by running "ParseURLHash().TryGetValue("key", out string value)"
        private Dictionary<string, string> ParseURLHash()
        {
            string url = Application.absoluteURL;

            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(url)) return result;

            int hashIndex = url.IndexOf('#');
            if (hashIndex == -1 || hashIndex == url.Length - 1) return result;

            string hashString = url.Substring(hashIndex + 1);

            string[] pairs = hashString.Split('&');

            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = UnityWebRequest.UnEscapeURL(keyValue[0]);
                    string value = UnityWebRequest.UnEscapeURL(keyValue[1]);
                    result[key] = value;
                }
            }

            return result;
        }
    }
}
