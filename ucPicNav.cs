using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace VTools
{
    public partial class ucPicNav : UserControl
    {
        protected string FOLDERPATH = string.Empty;
        protected string IMAGE_EXTENSION = string.Empty;
        protected string nameForItemToSelect = string.Empty;
        protected string PUNTO = ".";

        private FileSystemWatcher watcher;

        /// <summary>
        /// Main List Generator for my program in question
        /// </summary>
        /// <param name="baseFilename"></param>
        /// <param name="enumerator">  </param>
        /// <param name="files">       </param>
        /// <param name="ls">          </param>
        public void CreateListItems(string baseFilename, string enumerator, ref string[] files, ref List<ListViewItem> ls)
        {
            foreach (var item in files)
            {
                string file = FOLDERPATH;

                string itemText = CreateListItem(baseFilename, enumerator, ref file, item);

                if (String.IsNullOrEmpty(itemText)) continue;

                makeItem(ref ls, itemText, file);
            }
        }

        /// <summary>
        /// Function to override for list generation
        /// </summary>
        /// <param name="baseFilename"></param>
        /// <param name="enumerator">  </param>
        /// <param name="file">        </param>
        /// <param name="item">        </param>
        /// <returns></returns>
        public virtual string CreateListItem(string baseFilename, string enumerator, ref string file, string item)
        {
            return item;
        }

        protected void makeItem(ref List<ListViewItem> ls, string itemText, string file)
        {
            ListViewItem i = new ListViewItem(itemText.ToUpper());
            i.Tag = file; //attach to tag to open file later
            ls.Add(i);
        }

        /*
        /// <summary>
        /// cleans the list and hides
        /// </summary>
        /// <param name="hide"></param>
        public void HideList(bool hide)
        {
            listView1.Visible = !hide;
            if (hide)
            {
                showInBrowser(string.Empty);
                cleanList();
            }
        }
        */

        public void NavigateTo(Uri helpFile)
        {
            webBrowser1.Navigate(helpFile);
        }

        /// <summary>
        /// Makes a list from basefileName.Filter and removes the enumerator from their names
        /// </summary>
        /// <param name="baseFilename">i.e. 84.</param>
        /// <param name="filter">      84.*</param>
        /// <param name="enumerator">  84.N##.filename.extension</param>
        public void RefreshList(string baseFilename, string filter, string enumerator)
        {
            //get files
            watcher.EnableRaisingEvents = false;

            string[] files = Directory.GetFiles(FOLDERPATH, baseFilename + filter);
            files = SelectAndOrder(baseFilename, files);

            listView1.Visible = true;

            showInBrowser(string.Empty);
            cleanList();

            List<ListViewItem> ls = new List<ListViewItem>();

            CreateListItems(baseFilename, enumerator, ref files, ref ls);
            ls = ls.OrderBy(o => o.Text).ToList();
            addToGroups(ref ls);

            selectDefaultItem(nameForItemToSelect);

            watcher.EnableRaisingEvents = true;
        }

        public virtual string[] SelectAndOrder(string baseFilename, string[] files)
        {
            return files;
        }

        public void Set(string path, string filter, string imageExt, string nameDefaultSelectedItem)
        {
            nameForItemToSelect = nameDefaultSelectedItem;
            FOLDERPATH = path;
            IMAGE_EXTENSION = imageExt;

            watcher.EnableRaisingEvents = false;
            watcher.Path = FOLDERPATH;
            watcher.Filter = filter + imageExt;
            watcher.EnableRaisingEvents = true;
        }

        protected string comma = "comma";
        protected string spreadsheet = "spreadsheet";
        protected string last = "last";
        protected string full = "full";
        protected string all = "all";
        protected string html = "html";
        protected string separated = "separated";
        protected string csv = "csv";
        protected string xls = "xls";
        protected string xml = "xml";
        protected string xaml = "xaml";

        private void addToGroups(ref List<ListViewItem> ls)
        {
            foreach (var item in ls)
            {
                ListViewGroup g = null;
                string count = string.Empty;
                if (item.Text.ToLower().Contains(comma))
                {
                    count = comma;
                }
                else if (item.Text.ToLower().Contains(spreadsheet))
                {
                    count = spreadsheet;
                }
                else if (item.Text.ToLower().Contains(last))
                {
                    count = last;
                }
                else if (item.Text.ToLower().Contains(full))
                {
                    count = full;
                }
                else if (item.Text.ToLower().Contains(all))
                {
                    count = all;
                }
                else if (item.Text.ToLower().Contains(html))
                {
                    count = html;
                }
                else if (item.Text.ToLower().Contains(xml))
                {
                    count = xml;
                }
                else if (item.Text.ToLower().Contains(xaml))
                {
                    count = xaml;
                }
                else
                {
                    count = item.Text.Count().ToString();
                }
                g = listView1.Groups[count];
                if (g == null)
                {
                    g = new ListViewGroup(count, string.Empty);

                    g.HeaderAlignment = HorizontalAlignment.Left;

                    listView1.Groups.Add(g);
                }

                listView1.Items.Add(item);

                item.EnsureVisible();
                item.Group = g;
            }
        }

        private void cleanList()
        {
            if (listView1.Items.Count != 0) listView1.Items.Clear();
            if (listView1.Groups.Count != 0) listView1.Groups.Clear();
        }

        private void listViewItemChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            string file = e.Item.Tag.ToString();
            bool asHtml = !file.Contains(IMAGE_EXTENSION);
            // bool asHtml = !file.Contains(IMAGE_EXTENSION);
            showInBrowser(file, asHtml);
        }

        private void selectDefaultItem(string nameForItemToSelect)
        {
            ListViewItem selectedItem = listView1.Items.Cast<ListViewItem>().FirstOrDefault(o => o.Text.CompareTo(nameForItemToSelect) == 0);
            if (selectedItem != null) selectedItem.Selected = true;
        }

        private int guidLenght = 6;

        public int GuidLenght
        {
            get
            {
                return guidLenght;
            }

            set
            {
                guidLenght = value;
            }
        }

        private void showInBrowser(string file, bool asHtml = true)
        {
            try
            {
                // asHtml = asHtml && !file.Contains(IMAGE_EXTENSION);
                Uri uri = Rsx.Dumb.IO.GenerateURI(file, guidLenght, FOLDERPATH, asHtml);
                NavigateTo(uri);
            }
            catch (Exception ex)
            {
            }
        }

        private void watcherFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                string file = e.FullPath;
                bool asHtml = !file.Contains(IMAGE_EXTENSION);
                showInBrowser(file, asHtml);
            }
        }

        public ucPicNav()
        {
            InitializeComponent();

            webBrowser1.ScriptErrorsSuppressed = true;
            webBrowser1.AllowNavigation = true;

            listView1.ItemSelectionChanged += listViewItemChanged;
            listView1.ShowGroups = true;
            listView1.View = View.SmallIcon;

            watcher = new FileSystemWatcher();
            watcher.Changed += watcherFileChanged;

            this.listView1.MouseDoubleClick += ListView1_MouseDoubleClick;
            Application.EnableVisualStyles();
        }

        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // listView1.FocusedItem;
            string file = listView1.FocusedItem.Tag.ToString();
            bool image = file.Contains(IMAGE_EXTENSION);
            if (image)
            {
                Rsx.Dumb.IO.Process(new Process(), FOLDERPATH, "explorer", file, false);
            }
            else showInBrowser(file, false);
        }
    }
}