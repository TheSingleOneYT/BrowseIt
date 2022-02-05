using Newtonsoft.Json.Linq;
using System.Net;

namespace BrowseIt.BenBot
{
    public class Benbot
    {
        public static string GetImgURL(string asset)
        {
            string url;
            if (!asset.StartsWith("PPID_ST"))
                url = $"https://benbot.app/api/v1/exportAsset?path=FortniteGame/Content/Playsets/PlaysetProps/GeneratedThumbnails/PPID_ST_{asset.Replace("PPID_", "")}";
            else
                url = $"https://benbot.app/api/v1/exportAsset?path=FortniteGame/Content/Playsets/PlaysetProps/GeneratedThumbnails/{asset}";

            return url;
        }

        public static string GetPropName(string asset)
        {
            var wc = new WebClient();
            string url = $"https://benbot.app/api/v1/assetProperties?path=FortniteGame/Content/Playsets/PlaysetProps/{asset}";
            var jo = JObject.Parse(wc.DownloadString(url));
            var json = jo["export_properties"][0].ToString();//[5].ToString(); //["DisplayName"]
            JObject o = JObject.Parse(json);
            var name = o["DisplayName"]["finalText"].ToString();
            return name;
        }

        public static string GetPropNameFromPath(string path)
        {
            var wc = new WebClient();
            string url = $"https://benbot.app/api/v1/assetProperties?path={path}";
            var jo = JObject.Parse(wc.DownloadString(url));
            var json = jo["export_properties"][0].ToString();//[5].ToString(); //["DisplayName"]
            JObject o = JObject.Parse(json);
            var name = o["DisplayName"]["finalText"].ToString();
            return name;
        }

        public static bool CheckGallery(string path)
        {
            var wc = new WebClient();
            string url = $"https://benbot.app/api/v1/assetProperties?path={path}";

            try
            {
                wc.DownloadString(url);
                return true;
            }
            catch (WebException wex)
            {
                return false;
            }
        }

        public static string GetGalleryJArrayString(string path)
        {
            var wc = new WebClient();
            string url = $"https://benbot.app/api/v1/assetProperties?path={path}";

            var jo = JObject.Parse(wc.DownloadString(url));
            var jsonpt1 = jo.ToString().Remove(0, jo.ToString().IndexOf("\"AssociatedPlaysetProps\"")).Insert(0, "{");//[5].ToString(); //["DisplayName"]
            var jo2 = JObject.Parse(jsonpt1.Remove(jsonpt1.IndexOf("\"PreviewActorData\""), jsonpt1.Length - jsonpt1.IndexOf("\"PreviewActorData\"") - 1).Replace("}],\"PreviewActorData\"", "}]}"));
            var jsonpt2 = jo2["AssociatedPlaysetProps"];
            return jsonpt2.ToString();
        }

        public static string GetGalleryName(string path)
        {
            var wc = new WebClient();
            string url = $"https://benbot.app/api/v1/assetProperties?path={path}";
            var jo = JObject.Parse(wc.DownloadString(url));
            var json = jo["export_properties"][0].ToString();//[5].ToString(); //["DisplayName"]
            JObject o = JObject.Parse(json);
            var name = o["DisplayName"]["finalText"].ToString();
            return name;
        }
    }
}
