using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace VastMetaverseTools.External
{
    public class ExternalTools : MonoBehaviour
    {
        public static void OpenURL(string url)
        {
            // TODO: Open URL
        }

        public static void TeleportToSpace(string spaceId, bool showConfirmPopup = true)
        {
            // TODO: Move between spaces/metaverse sites
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
