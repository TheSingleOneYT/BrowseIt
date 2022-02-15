using System.Net;

namespace BrowseIt.BenBot
{
    public class Benbot
    {
        public static string GetImgURL(string asset, string gallery, string pathToSUA)
        {
            string url = "";
            asset = asset + ".uasset";
            var wc = new WebClient();

            if (pathToSUA == "notCRG")
            {
                if (!asset.StartsWith("PPID_ST"))
                    url = $"https://benbot.app/api/v1/exportAsset?path=FortniteGame/Content/Playsets/PlaysetProps/GeneratedThumbnails/PPID_ST_{asset.Replace("PPID_", "")}";
                else
                    url = $"https://benbot.app/api/v1/exportAsset?path=FortniteGame/Content/Playsets/PlaysetProps/GeneratedThumbnails/{asset}";
            }
            else
            {
                if (!asset.StartsWith("PPID_ST"))
                    url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}GeneratedThumbnails/PPID_ST_{asset.Replace("PPID_", "")}";
                else
                    url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}GeneratedThumbnails/{asset}";

                try
                {
                    wc.DownloadString(url);
                }
                catch (WebException)
                {
                    if (!asset.StartsWith("PPID_ST"))
                        url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}PPIDS/Icons/PPID_ST_{asset.Replace("PPID_", "")}";
                    else
                        url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}PPIDS/Icons/{asset}";
                }
            }

            return url;
        }
    }
}
