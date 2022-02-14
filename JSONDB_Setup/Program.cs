using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows.Forms;

namespace JSONDB_Setup // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        string AppData;
        string JSONDatabase;

        static void Main(string[] args)
        {
            bool retry = true;

            string AppData = Environment.ExpandEnvironmentVariables("%LocalAppData%").ToString() + "\\BrowseIt";
            string JSONDatabase = Environment.ExpandEnvironmentVariables("%LocalAppData%").ToString() + "\\BrowseIt\\JSONDataBase";

            if (!Directory.Exists(AppData))
                Directory.CreateDirectory(AppData);

            if (!Directory.Exists(JSONDatabase))
                Directory.CreateDirectory(JSONDatabase);


            while (retry)
            {
                if (hasInternet())
                {
                    string ver;
                    HttpWebRequest req;

                    try
                    {
                        req = (HttpWebRequest)WebRequest.Create("https://raw.githubusercontent.com/TheSingleOneYT/BrowseIt/main/JSONDatabase/Version.txt");
                        using (WebResponse response = req.GetResponse())
                        {
                            using (Stream stream = response.GetResponseStream())
                            {
                                StreamReader reader = new StreamReader(stream);
                                ver = reader.ReadToEnd();
                            }
                        }

                        string installed;

                        if (!File.Exists(AppData + "\\DatabaseVer.txt"))
                            installed = "1";
                        else
                            installed = File.ReadAllText(AppData + "\\DatabaseVer.txt");

                        if (ver == installed)
                        {
                            retry = false;
                        }
                        else
                        {
                            Directory.CreateDirectory(AppData + "\\tmp");

                            var wc = new WebClient();

                            try
                            {
                                wc.DownloadFile("https://github.com/TheSingleOneYT/BrowseIt/raw/main/JSONDatabase/files.zip", AppData + "\\.zip");

                                ZipFile.ExtractToDirectory(AppData + "\\.zip", AppData + "\\tmp");

                                foreach (var file in Directory.GetFiles(AppData + "\\tmp"))
                                {
                                    File.Copy(file, JSONDatabase + $"\\{Path.GetFileName(file)}", true);
                                    File.Delete(file);
                                }

                                Directory.Delete(AppData + "\\tmp");
                                File.Delete(AppData + "\\.zip");

                                File.WriteAllText(AppData + "\\DatabaseVer.txt", ver);
                            }
                            catch (WebException wex)
                            {
                                DialogResult dr = showError();

                                if (dr == DialogResult.Retry)
                                {
                                    retry = true;
                                }
                                else
                                {
                                    retry = false;
                                }
                            }
                        }

                    }
                    catch (WebException wex)
                    {
                        DialogResult dr = showError();

                        if (dr == DialogResult.Retry)
                        {
                            retry = true;
                        }
                        else
                        {
                            retry = false;
                        }
                    }
                }
                else
                {
                    DialogResult dr = showError();

                    if (dr == DialogResult.Retry)
                    {
                        retry = true;
                    }
                    else
                    {
                        retry = false;
                    }
                }
            }
            Application.Exit();
        }

        public static DialogResult showError()
        {
            DialogResult dr = MessageBox.Show("An error occured downloading the latest JSONDatabase. Please click 'Refresh' when you open the app if you press cancel. Or press retry to retry.", "JSONDB_Setup.exe", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            return dr;
        }

        public static bool hasInternet()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://www.google.com");
                request.KeepAlive = false;
                request.Timeout = 1000;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return true;
            }
            catch
            {
                return false;
            }
        }
    }
}