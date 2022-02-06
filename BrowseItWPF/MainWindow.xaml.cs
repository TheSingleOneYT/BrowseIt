using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Newtonsoft.Json.Linq;
using BrowseIt.BenBot;
using System.Net;
using System.IO.Compression;

namespace BrowseItWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string AppData = Environment.ExpandEnvironmentVariables("%LocalAppData%").ToString() + "\\BrowseIt";
        string JSONDatabase = Environment.ExpandEnvironmentVariables("%LocalAppData%").ToString() + "\\BrowseIt\\JSONDataBase";

        public MainWindow()
        {
            if (!Directory.Exists(AppData))
                Directory.CreateDirectory(AppData);

            if (!Directory.Exists(JSONDatabase))
                Directory.CreateDirectory(JSONDatabase);

            InitializeComponent();

            foreach (var item in Directory.GetFiles(JSONDatabase))
            {
                objects.Add(Path.GetFileNameWithoutExtension(item));
            }
        }

        private List<string> objects = new List<string>()
        {

        };

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var suitableItems = new List<string>();
                var splitText = sender.Text.ToLower().Split(" ");
                foreach (var o in objects)
                {
                    var found = splitText.All((key) =>
                    {
                        return o.ToLower().Contains(key);
                    });
                    if (found)
                    {
                        suitableItems.Add(o);
                    }
                }
                if (suitableItems.Count == 0)
                {
                    suitableItems.Add("No results found");
                }
                sender.ItemsSource = suitableItems;
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            SearchBox.Text = args.SelectedItem.ToString();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double h = e.NewSize.Height;

            name.MaxHeight = h - 150;
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            name.Items.Clear();

            int noOfItems = 0;
            foreach (var item in Directory.GetFiles(JSONDatabase))
            {
                if (item.ToLower().Contains(SearchBox.Text.ToLower()))
                {
                    noOfItems++;
                }
            }

            if (noOfItems > 100)
            {
                Item o = new Item("Large numbers of results are not loaded.", "Results for search > 100", "INFO");
                name.Items.Add(o);
            }
            else
            {
                foreach (var item in Directory.GetFiles(JSONDatabase))
                {
                    if (item.ToLower().Contains(SearchBox.Text.ToLower()))
                    {
                        var jo = JObject.Parse(File.ReadAllText(item));
                        var json = jo["gallery"].ToString();

                        Item o = new Item(Path.GetFileNameWithoutExtension(item), json, img: new BitmapImage(new Uri(Benbot.GetImgURL(Path.GetFileNameWithoutExtension(item)), UriKind.Absolute)));

                        name.Items.Add(o);
                    }
                }
            }
        }

        private async void OpenImgInBrowser_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in name.SelectedItems)
            {
                ModernWpf.Controls.ListView listView = (ModernWpf.Controls.ListView)name;
                int itemNumber = listView.Items.IndexOf(item);
                Item itemClicked = (Item)listView.Items[itemNumber];
                if (itemClicked != null)
                {
                    var uri = new Uri($"https://benbot.app/api/v1/exportAsset?path=FortniteGame/Content/Playsets/PlaysetProps/GeneratedThumbnails/PPID_ST_{itemClicked.Name.Replace("PPID_", "")}");

                    await Windows.System.Launcher.LaunchUriAsync(uri);
                }
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            StackPanel sp = new StackPanel();
            ModernWpf.Controls.ProgressRing pb = new ModernWpf.Controls.ProgressRing();
            TextBlock tb = new TextBlock();
            pb.IsActive = true;
            pb.Width = 50;
            pb.Height = 50;

            sp.Children.Add(pb);
            sp.Children.Add(tb);

            ContentDialog dialog = new ContentDialog();
            dialog.Content = sp;
            dialog.Title = "Updating Database";
            dialog.ShowAsync();

            await Task.Delay(2000);

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
                        //pb.Value = 100;
                        pb.Visibility = Visibility.Hidden;
                        dialog.CloseButtonText = "OK";
                        tb.Text = "You have the latest JSONDatabase!";
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

                            //pb.Value = 100;
                            pb.Visibility = Visibility.Hidden;
                            dialog.CloseButtonText = "OK";
                            tb.Text = "Successfully updated the JSONDatabase!";
                            File.WriteAllText(AppData + "\\DatabaseVer.txt", ver);

                            dialog.CloseButtonClick += Dialog_CloseButtonClick;
                        }
                        catch (WebException wex)
                        {
                            tb.Text = "An error happened whilst downloading the new JSONDatabase";
                            //pb.Value = 100;
                            pb.Visibility = Visibility.Hidden;
                            dialog.CloseButtonText = "OK";
                        }
                    }

                }
                catch (WebException wex)
                {
                    tb.Text = "An error happened whilst downloading the new JSONDatabase";
                    //pb.Value = 100;
                    pb.Visibility = Visibility.Hidden;
                    dialog.CloseButtonText = "OK";
                }
            }
            else
            {
                //pb.Value = 100;
                pb.Visibility = Visibility.Hidden;
                dialog.CloseButtonText = "OK";
            }
        }

        private void Dialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            objects.Clear();
            foreach (var item in Directory.GetFiles(JSONDatabase))
            {
                objects.Add(Path.GetFileNameWithoutExtension(item));
            }
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

        private async void AboutProj_Click(object sender, RoutedEventArgs e)
        {
            StackPanel sp = new StackPanel();

            TextBlock tb1 = new TextBlock();
            tb1.FontSize =  26;
            tb1.Text = "About";
            sp.Children.Add(tb1);

            TextBlock tb2 = new TextBlock();
            tb2.FontSize =  22;
            tb2.Text = "The Aim:";
            sp.Children.Add(tb2);

            TextBlock tb3 = new TextBlock();
            tb3.TextWrapping = TextWrapping.Wrap;
            tb3.Text = "BrowseIt is a Fortnite Creative prop browser, made because Epic wouldn't do it themselves. " +
                "It can be used as an alternative to FCHQ's Prop browser which doesn't include props but does include things like tags " +
                "and memory usage.";
            sp.Children.Add(tb3);

            TextBlock tb4 = new TextBlock();
            tb4.FontSize =  22;
            tb4.Text = "Project Links";
            sp.Children.Add(tb4);

            TextBlock tb5 = new TextBlock();
            tb5.FontSize = 18;
            tb5.Text = "Github";
            sp.Children.Add(tb5);

            HyperlinkButton hl1 = new HyperlinkButton();
            hl1.Content = "BrowseIt - Github";
            hl1.NavigateUri = new Uri("https://github.com/TheSingleOneYT/BrowseIt");
            sp.Children.Add(hl1);

            TextBlock tb6 = new TextBlock();
            tb6.FontSize = 18;
            tb6.Text = "Video documentation";
            sp.Children.Add(tb6);

            HyperlinkButton hl2 = new HyperlinkButton();
            hl2.Content = "YouTube Video Link";
            hl2.NavigateUri = new Uri("https://youtu.be/I-8WRayvc3k");
            sp.Children.Add(hl2);

            ContentDialog dialog = new ContentDialog();
            dialog.Content = sp;
            dialog.CloseButtonText = "OK";
            await dialog.ShowAsync();
        }

        private async void name_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ShowCD.IsChecked == true)
            {
                ModernWpf.Controls.ListView listView = (ModernWpf.Controls.ListView)sender;
                int itemNumber = listView.Items.IndexOf(listView.SelectedItem);

                Item itemClicked = (Item)listView.Items[itemNumber];

                StackPanel sp = new StackPanel();
                PersonPicture pp = new PersonPicture();
                TextBlock tb = new TextBlock();

                pp.ProfilePicture = new BitmapImage(new Uri(Benbot.GetImgURL(itemClicked.Name)));
                tb.Text = $"NAME: {itemClicked.Name}\nGALLERY: {itemClicked.Gallery}";

                sp.Children.Add(pp);
                sp.Children.Add(tb);

                ContentDialog dialog = new ContentDialog();
                dialog.Content = sp;
                dialog.Title = "OBJECT";
                dialog.CloseButtonText = "OK";
                if (!itemClicked.Name.Contains("rendered"))
                    await dialog.ShowAsync();
            }
        }
    }

    public class Item
    {
        public string Name { get; private set; }
        public BitmapImage ImgSrc { get; private set; }
        public string Gallery { get; private set; }
        public string Initials { get; private set; }
        public Item(string name, string gallery, string initials = null, BitmapImage img = null)
        {
            Name = name;
            ImgSrc = img;
            Gallery = gallery;
            Initials = initials;
        }
    }
}
