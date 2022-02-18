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

                try
                {
                    wc.DownloadString(url);
                }
                catch (WebException)
                {
                    if (!asset.StartsWith("PPID_ST"))
                        url = $"https://benbot.app/api/v1/exportAsset?path=FortniteGame/Content/Playsets/PlaysetProps/Icons/PPID_ST_{asset.Replace("PPID_", "")}";
                    else
                        url = $"https://benbot.app/api/v1/exportAsset?path=FortniteGame/Content/Playsets/PlaysetProps/Icons/{asset}";
                }
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

                    try
                    {
                        wc.DownloadString(url);
                    }
                    catch (WebException)
                    {
                        if (!asset.StartsWith("PPID_ST"))
                            url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}PlaysetProps/GeneratedThumbnails/PPID_ST_{asset.Replace("PPID_", "")}";
                        else
                            url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}PlaysetProps/GeneratedThumbnails/{asset}";

                        try
                        {
                            wc.DownloadString(url);
                        }
                        catch (WebException)
                        {
                            if (!asset.StartsWith("PPID_ST"))
                                url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}Maps/PlaysetProps/GeneratedThumbnails/PPID_ST_{asset.Replace("PPID_", "")}";
                            else
                                url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}Maps/PlaysetProps/GeneratedThumbnails/{asset}";

                            try
                            {
                                wc.DownloadString(url);
                            }
                            catch (WebException)
                            {
                                if (!asset.StartsWith("PPID_ST"))
                                    url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}PlaysetProps/Icons/PPID_ST_{asset.Replace("PPID_", "")}";
                                else
                                    url = $"https://benbot.app/api/v1/exportAsset?path={pathToSUA}PlaysetProps/Icons/{asset}";
                            }
                        }
                    }
                }
            }

            return url;
        }
    }
}
