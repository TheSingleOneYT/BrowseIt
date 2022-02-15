using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using Newtonsoft.Json.Linq;
using BrowseIt.BenBot;
using System.Net;
using System.IO.Compression;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

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
                        var pathToSUA = jo["pathToSUA"].ToString();

                        Item o = new Item(Path.GetFileNameWithoutExtension(item), jo["gallery"].ToString(), img: new BitmapImage(new Uri(Benbot.GetImgURL(Path.GetFileNameWithoutExtension(item), jo["gallery"].ToString(), pathToSUA), UriKind.Absolute)));

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
                    var jo = JObject.Parse(File.ReadAllText(JSONDatabase + "\\" + itemClicked.Name + ".json"));
                    var pathToSUA = jo["pathToSUA"].ToString();

                    var uri = new Uri(Benbot.GetImgURL(itemClicked.Name, itemClicked.Gallery, pathToSUA));

                    await Windows.System.Launcher.LaunchUriAsync(uri);
                }
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await UpdateDB();
        }

        private async Task UpdateDB()
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

            ToastNotificationManagerCompat.History.Clear();
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
            tb3.Text = "BrowseIt is a Fortnite Creative prop browser, made because Epic haven't done it themselves. " +
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
            hl2.NavigateUri = new Uri("https://youtu.be/rGGZGyH-ncM");
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
                TextBlock dashes = new TextBlock();

                var jo = JObject.Parse(File.ReadAllText(JSONDatabase + "\\" + itemClicked.Name + ".json"));
                var pathToSUA = jo["pathToSUA"].ToString();

                pp.ProfilePicture = new BitmapImage(new Uri(Benbot.GetImgURL(itemClicked.Name, itemClicked.Gallery, pathToSUA)));
                tb.Text = $"NAME: {itemClicked.Name}\nGALLERY: {itemClicked.Gallery}";
                dashes.Text = "---------";
                dashes.HorizontalAlignment = HorizontalAlignment.Center;

                sp.Children.Add(pp);
                sp.Children.Add(dashes);
                sp.Children.Add(tb);

                ContentDialog dialog = new ContentDialog();
                dialog.Content = sp;
                dialog.Title = "OBJECT";
                dialog.CloseButtonText = "OK";
                if (!itemClicked.Name.Contains("rendered"))
                    await dialog.ShowAsync();
            }
        }

        private async void AddToDatabase_Click(object sender, RoutedEventArgs e)
        {
            StackPanel sp = new StackPanel();
            AutoSuggestBox tb = new AutoSuggestBox();
            AutoSuggestBox tb2 = new AutoSuggestBox();
            TextBlock tbl = new TextBlock();

            tbl.Text = "Enter details below:";
            tb.PlaceholderText = "Name";

            tb.TextChanged += Tb_TextChanged;
            tb2.TextChanged += Tb2_TextChanged;

            tb2.PlaceholderText = "gallery";

            sp.Children.Add(tbl);
            sp.Children.Add(tb);
            sp.Children.Add(tb2);

            ContentDialog dialog = new ContentDialog();
            dialog.Content = sp;
            dialog.Title = "Add To Database";
            dialog.PrimaryButtonText = "Add";
            dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick;
            dialog.CloseButtonText = "Cancel";
            await dialog.ShowAsync();
        }

        private async void DelFromDatabase_Click(object sender, RoutedEventArgs e)
        {
            StackPanel sp = new StackPanel();
            TextBlock tbl = new TextBlock();
            ModernWpf.Controls.ListView lst = new ModernWpf.Controls.ListView();
            lst.Height = 200;
            lst.SelectionChanged += Lst_SelectionChanged;

            foreach (var file in Directory.GetFiles(JSONDatabase))
            {
                lst.Items.Add(Path.GetFileNameWithoutExtension(file));
            }

            lst.IsMultiSelectCheckBoxEnabled = true;
            lst.SelectionMode = SelectionMode.Multiple;

            tbl.Text = "Select Files Below:";

            sp.Children.Add(tbl);
            sp.Children.Add(lst);

            ContentDialog dialog = new ContentDialog();
            dialog.Content = sp;
            dialog.Title = "Add To Database";
            dialog.PrimaryButtonText = "Delete";
            dialog.PrimaryButtonClick += JSONToDel_PrimaryButtonClick;
            dialog.CloseButtonText = "Cancel";
            await dialog.ShowAsync();
        }

        private void Lst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var si in e.AddedItems)
            {
                JSONtoDel.Add(si.ToString());
            }

            foreach (var ri in e.RemovedItems)
            {
                JSONtoDel.Remove(ri.ToString());
            }
        }

        private void JSONToDel_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            foreach (var file in JSONtoDel)
                if (file != "" && File.Exists(JSONDatabase + "\\" + file + ".json"))
                    File.Delete(JSONDatabase + "\\" + file + ".json");
        }

        private string newJSONname;
        private string newJSONgallery;
        private List<string> JSONtoDel = new List<string>();

        private void Tb2_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            newJSONgallery = sender.Text;
        }

        private void Tb_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            newJSONname = sender.Text;
        }

        private void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (newJSONgallery != "" && newJSONname != "")
                File.WriteAllText(JSONDatabase + "\\" + newJSONname + ".json", "{\"gallery\":\"" + newJSONgallery + "\"}");
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            if (hasInternet())
            {
                try
                {
                    string ver;
                    HttpWebRequest req;

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
                    }
                    else
                    {
                        new ToastContentBuilder()
                            .AddText("An update was discovered for the JSONDatabase, we are updating now. Start-up time for this run maybe longer than usual.")
                            .AddProgressBar("", status: "", valueStringOverride: "", isIndeterminate: true)
                            .Show();

                        this.Hide();

                        await UpdateDB();
                        this.Show();
                    }
                }
                catch (WebException)
                {

                }
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
