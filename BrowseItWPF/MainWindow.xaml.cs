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
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;

namespace BrowseItWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string AppData = Environment.ExpandEnvironmentVariables("%LocalAppData%").ToString() + "\\BrowseIt";
        string JSONDatabase = Environment.ExpandEnvironmentVariables("%LocalAppData%").ToString() + "\\BrowseIt\\JSONDataBase";
        string tempLocation = Environment.ExpandEnvironmentVariables("%temp%").ToString();

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
                var wUnderscores = sender.Text.ToLower().Replace(" ", "_");
                var splitText = wUnderscores.Split(" ");
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
                    suitableItems.Add("No results for '" + wUnderscores + "' found");
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

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
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
            else if (noOfItems == 0)
            {
                Item o = new Item("No results found :(", "Results = 0", "INFO");
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
            displayUpdateNote = "";

            updatePR.IsActive = true;
            updatePR.Visibility = Visibility.Visible;
            updatePR.Width = 50;
            updatePR.Height = 50;

            updateTB.Text = displayUpdateNote;

            updateStackPanel.Children.Clear();
            updateStackPanel.Children.Add(updatePR);
            updateStackPanel.Children.Add(updateTB);

            dialog.Content = updateStackPanel;
            dialog.Title = "Updating Database";
            dialog.CloseButtonText = "";
            dialog.ShowAsync();

            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
            worker.Dispose();
        }

        private string displayUpdateNote;

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            updatePR.Visibility = Visibility.Hidden;
            dialog.CloseButtonText = "OK";
            updateTB.Text = displayUpdateNote;

            if (displayUpdateNote == "You have the latest JSONDatabase!")
                dialog.CloseButtonClick += Dialog_CloseButtonClick;
        }

        private BackgroundWorker worker = new BackgroundWorker();

        private ContentDialog dialog = new ContentDialog();
        private TextBlock updateTB = new TextBlock();
        private ProgressRing updatePR = new ProgressRing();
        private StackPanel updateStackPanel = new StackPanel();

        private async void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            await UpdateDB();
        }
        private async Task UpdateDB()
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
                        displayUpdateNote = "You have the latest JSONDatabase!";
                    }
                    else
                    {
                        Directory.CreateDirectory(AppData + "\\tmp");

                        var wc = new WebClient();

                        try
                        {
                            var zipLoc = tempLocation + "\\" + Path.GetRandomFileName() + ".zip";
                            var brtemp = tempLocation + "\\" + Path.GetRandomFileName() + "-BrowseItTempDIR";

                            wc.DownloadFile("https://github.com/TheSingleOneYT/BrowseIt/raw/main/JSONDatabase/files.zip", zipLoc);

                            ZipFile.ExtractToDirectory(zipLoc, brtemp);

                            foreach (var file in Directory.GetFiles(brtemp))
                            {
                                File.Copy(file, JSONDatabase + $"\\{Path.GetFileName(file)}", true);
                            }

                            Directory.Delete(AppData + "\\tmp");
                            File.Delete(AppData + "\\.zip");

                            displayUpdateNote = "Successfully updated the JSONDatabase!";
                            File.WriteAllText(AppData + "\\DatabaseVer.txt", ver);
                        }
                        catch (WebException)
                        {
                            displayUpdateNote = "An error happened whilst downloading the new JSONDatabase";
                        }
                    }

                }
                catch (WebException)
                {
                    displayUpdateNote = "An error happened whilst downloading the new JSONDatabase";
                }
            }
            else
            {
                displayUpdateNote = "No internet connection.";
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

            ContentDialog d = new ContentDialog();
            d.Content = sp;
            d.CloseButtonText = "OK";
            await d.ShowAsync();
        }

        private async void name_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ShowCD.IsChecked == true)
            {
                ModernWpf.Controls.ListView listView = (ModernWpf.Controls.ListView)sender;
                int itemNumber = listView.Items.IndexOf(listView.SelectedItem);

                Item itemClicked = (Item)listView.Items[itemNumber];

                if (!itemClicked.Name.Contains("loaded") && !itemClicked.Name.Contains(":("))
                {
                    var jo = JObject.Parse(File.ReadAllText(JSONDatabase + "\\" + itemClicked.Name + ".json"));
                    var pathToSUA = jo["pathToSUA"].ToString();

                    StackPanel sp = new StackPanel();
                    PersonPicture pp = new PersonPicture();
                    TextBlock tb = new TextBlock();
                    TextBlock dashes = new TextBlock();

                    pp.ProfilePicture = new BitmapImage(new Uri(Benbot.GetImgURL(itemClicked.Name, itemClicked.Gallery, pathToSUA)));
                    tb.Text = $"NAME: {itemClicked.Name}\nGALLERY: {itemClicked.Gallery}";
                    dashes.Text = "---------";
                    dashes.HorizontalAlignment = HorizontalAlignment.Center;

                    sp.Children.Add(pp);
                    sp.Children.Add(dashes);
                    sp.Children.Add(tb);

                    ContentDialog objectDR = new ContentDialog();
                    objectDR.Content = sp;
                    objectDR.Title = "OBJECT";
                    objectDR.CloseButtonText = "OK";

                    await objectDR.ShowAsync();
                }
            }
        }

        private async void AddToDatabase_Click(object sender, RoutedEventArgs e)
        {
            StackPanel sp = new StackPanel();
            AutoSuggestBox tb = new AutoSuggestBox();
            AutoSuggestBox tb2 = new AutoSuggestBox();
            AutoSuggestBox tb3 = new AutoSuggestBox();
            TextBlock tbl = new TextBlock();

            tbl.Text = "Enter details below:";
            tb.PlaceholderText = "Name";

            tb.TextChanged += Tb_TextChanged;
            tb2.TextChanged += Tb2_TextChanged;
            tb3.TextChanged += Tb3_TextChanged;

            tb2.PlaceholderText = "gallery";

            tb3.Header = "Path to SetupAssets, note - if path does not contain /CRG/ put notCRG";
            tb3.PlaceholderText = "SetupAssets Path";

            sp.Children.Add(tbl);
            sp.Children.Add(tb);
            sp.Children.Add(tb2);
            sp.Children.Add(tb3);

            ContentDialog addDR = new ContentDialog();
            addDR.Content = sp;
            addDR.Title = "Add To Database";
            addDR.PrimaryButtonText = "Add";
            addDR.PrimaryButtonClick += Dialog_PrimaryButtonClick;
            addDR.CloseButtonText = "Cancel";
            await addDR.ShowAsync();
        }

        private void Tb3_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            newPath = sender.Text;
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

            ContentDialog delDR = new ContentDialog();
            delDR.Content = sp;
            delDR.Title = "Delete from Databse";
            delDR.PrimaryButtonText = "Delete";
            delDR.PrimaryButtonClick += JSONToDel_PrimaryButtonClick;
            delDR.CloseButtonText = "Cancel";
            await delDR.ShowAsync();
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
        private string newPath;
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
                File.WriteAllText(JSONDatabase + "\\" + newJSONname + ".json", "{\"gallery\":\"" + newJSONgallery + "\",\"pathToSUA\":\""+ newPath + "\"}");
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            WebClient webClient = new WebClient();

            if (hasInternet())
            {
                try
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                    string version = fileVersionInfo.ProductVersion;

                    string newVer = webClient.DownloadString("https://raw.githubusercontent.com/TheSingleOneYT/BrowseIt/main/Installer/Installer.iss")
                        .Remove(0, 77 + 28);
                    newVer = newVer.Remove(newVer.IndexOf('\"'), newVer.Length - newVer.IndexOf('\"'));

                    if (newVer != version)
                    {
                        new ToastContentBuilder()
                            .AddText($"A new version of BrowseIt ({newVer}) has been detected. The patch will now be installed.")
                            .AddProgressBar("", status: "", valueStringOverride: "", isIndeterminate: true)
                            .SetToastDuration(ToastDuration.Long)
                            .Show();

                        string setupTempName = $"{tempLocation}\\BrowseIt-{newVer}-{Path.GetRandomFileName()}.exe";

                        webClient.DownloadFile(new Uri($"https://github.com/TheSingleOneYT/BrowseIt/releases/download/{newVer}/BrowseIt-setup.exe"), setupTempName);

                        Process.Start(setupTempName, "/NORESTART /NOCANCEL /SILENT /ALLUSERS");
                        ToastNotificationManagerCompat.History.Clear();
                        Application.Current.Shutdown();
                    }

                }
                catch (WebException)
                {

                }

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
                            .SetToastDuration(ToastDuration.Long)
                            .Show();

                        this.Hide();

                        await UpdateDB();
                        this.Show();

                        new ToastContentBuilder().AddText("JSONDatabase updated!").Show();
                    }
                }
                catch (WebException)
                {

                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (worker.IsBusy)
            {
                e.Cancel = true;
                MessageBox.Show(this, "The JSONDatabase is currently being updated, you cannot close the app at this moment.", "BrowseIt", MessageBoxButton.OK, MessageBoxImage.Error);
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
