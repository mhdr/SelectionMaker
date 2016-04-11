using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Data;
using System.Threading;
using System.Timers;


namespace SelectionMaker
{

    #region Delegates
    public delegate void CallBackSearchFolder(DataTable datatable);
    public delegate void DispatcherDelegate(object state);
    public delegate void ProgressCurrentFile(string currentFile);
    public delegate void ShowMSGDelegate(string msg);
    public delegate void UpdateAvailbaleCallback();
    public delegate void EnableWatcherAfterCutting();
    #endregion

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        #region Global Variables
        string SourcePath;
        string DestinationPath;
        DataTable dtFiles = new DataTable("Files");
        DataTable dtSelectedFiles = new DataTable("SelectedFiles");
        CollectionView viewFileNames;
        System.Timers.Timer timerClearStatus = new System.Timers.Timer();
        System.Timers.Timer timerUpdateSliderPosition = new System.Timers.Timer();
        DataView dvFiles;
        // This variable controls Play and Pause of Media and ButtonPlayPause.IsChecked
        bool PlayPause;

        public delegate void ClearMSGDelegate(object state);
        public delegate void ShowMSGDispatcherDelegate(object state);
        private delegate void InitialsWatcherDelegate(object state);
        private delegate void RaiseModificationDetectedDialogBoxDelegate(object state);
        FileSystemWatcher watcher = new FileSystemWatcher();
        #endregion

        #region Settings
        double SettingVolume;
        string SettingSourcePath;
        string SettingDestinationPath;
        bool SettingSearchSubFolder;
        #endregion

        public Window1()
        {
            InitializeComponent();
        }

        #region Get SourcePath and DestinationPath
        private void ButtonSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.ShowNewFolderButton = false;
            fbd.SelectedPath = SourcePath;
            
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxSourceFolder.Text = fbd.SelectedPath;
            }
        }

        private void ButtonDestinationFolder_Click(object sender, RoutedEventArgs e)
        {
            SelectDestinationFolder();
        }

        private bool SelectDestinationFolder()
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.SelectedPath = DestinationPath;
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxDestinationFolder.Text = fbd.SelectedPath;
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool GetSourceFolder()
        {
            if (TextBoxSourceFolder.Text == "")
            {
                ShowMSG("Source path can not be empty");
                return false;
            }
            else if (!Directory.Exists(TextBoxSourceFolder.Text))
            {
                ShowMSG(string.Format("{0} : Does not exist",TextBoxSourceFolder.Text));
                return false;
            }
            else
            {
                SourcePath = TextBoxSourceFolder.Text;
                return true;
            }
        }

        private bool GetDestinationFolder()
        {
            if (TextBoxDestinationFolder.Text == "")
            {
                ShowMSG("Destination path can not be empty");
                return false;
            }
            else if (!Directory.Exists(TextBoxDestinationFolder.Text))
            {
                //ShowMSG(string.Format("{0} : Does not exist",TextBoxDestinationFolder.Text));
                return false;
            }
            else
            {
                DestinationPath = TextBoxDestinationFolder.Text;
                return true;
            }
        }
        #endregion

        private void ButtonListFiles_Click(object sender, RoutedEventArgs e)
        {
            StartSearch();
        }

        private void StartSearch()
        {
            #region Start Search
            // Get SourcePath
            if (GetSourceFolder() == false)
            {
                return;
            }
            //

            // Clear ListView
            dtFiles.Clear();
            //

            // If Search was canceled last time reset flag
            Search.CancelSearch = false;
            //

            if (CheckBoxSearchSubFolder.IsChecked == false)
            {
                // Search in New Thread
                CallBackSearchFolder cbSearch = new CallBackSearchFolder(BindAsync);
                ShowMSGDelegate shd = new ShowMSGDelegate(ShowMSG);
                Search sh = new Search(SourcePath, cbSearch, shd);
                ThreadStart ts = new ThreadStart(sh.SearchFolderNewThread);
                Thread t1 = new Thread(ts);
                t1.IsBackground = true;
                t1.Start();
                //
            }
            else if (CheckBoxSearchSubFolder.IsChecked == true)
            {
                CallBackSearchFolder cbSearch = new CallBackSearchFolder(BindAsync);
                ProgressCurrentFile pcFile = new ProgressCurrentFile(ShowFoundCurrentFile);
                ShowMSGDelegate shd = new ShowMSGDelegate(ShowMSG);
                Search sh = new Search(SourcePath, cbSearch, pcFile, shd);
                ThreadStart ts = new ThreadStart(sh.SearchSubFolderNewThread);
                Thread t1 = new Thread(ts);
                t1.IsBackground = true;
                t1.Start();
            }
            #endregion
        }


        #region Bind Async - Search Files
        /// <summary>
        /// This Method will be run when Async Search Finished
        /// </summary>
        /// <param name="dt"></param>
        private void BindAsync(DataTable dt)
        {
            dtFiles = new DataTable("Files");
            // this is a callback
            dtFiles.Merge(dt, false);
            DispatcherDelegate dd = new DispatcherDelegate(BindAsync2);
            this.Dispatcher.BeginInvoke(dd, "async");

            // Initials FileSystemWatcher
            InitialsWatcherDelegate dd2 = new InitialsWatcherDelegate(InitialsWatcher);
            this.Dispatcher.BeginInvoke(dd2, "Nothing");
            //
        }

        private void BindAsync2(object state)
        {
            this.DataContext = dtFiles;
            viewFileNames = (CollectionView)CollectionViewSource.GetDefaultView(this.DataContext);
            // Clear TextBlockCurrentFile if it's not empty
            TextBlockCurrentFile.Text = "";
            //

            // Call GC
            GC.Collect();
        }
        #endregion

        #region FileWatcher
        // this will watch source folder and if it detects any modification,it will show 
        // a dialogbox,
        //but the dialogebox will be shown only when the current will is activated

        bool ShouldShowDialogeBoxWatcher = false;
        private void Window_Activated(object sender, EventArgs e)
        {
            if (ShouldShowDialogeBoxWatcher == true)
            {
                ShouldShowDialogeBoxWatcher = false;
                RaiseDialogeBoxForReSearch();
            }
        }

        private void InitialsWatcher(object state)
        {
            watcher.Path = SourcePath;
            watcher.IncludeSubdirectories = (bool)CheckBoxSearchSubFolder.IsChecked;
            watcher.EnableRaisingEvents = true;
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Created += new FileSystemEventHandler(watcher_Modified);
            watcher.Deleted += new FileSystemEventHandler(watcher_Modified);
            watcher.Renamed += new System.IO.RenamedEventHandler(watcher_Renamed);
        }

        private void watcher_Modified(object sender, FileSystemEventArgs e)
        {
            ShouldShowDialogeBoxWatcher = true;
            watcher.EnableRaisingEvents = false;
        }

        private void watcher_Renamed(object sender, RenamedEventArgs e)
        {
            ShouldShowDialogeBoxWatcher = true;
            watcher.EnableRaisingEvents = false;
        }

        private void RaiseDialogeBoxForReSearch()
        {
            RaiseModificationDetectedDialogBoxDelegate dd = new RaiseModificationDetectedDialogBoxDelegate(RaiseDialogeBoxForReSearch2);
            this.Dispatcher.BeginInvoke(dd, "Nothing"); 
        }

        private void RaiseDialogeBoxForReSearch2(object state)
        {
            if (DialogeBoxModificationDetected.IsOpen == true)
            {
                return;
            }

            lock (this)
            {
                DialogeBoxModificationDetected dd = new DialogeBoxModificationDetected();
                dd.GetSourcePath(SourcePath);
                dd.ShowDialog();
                if (dd.doWhat == AfterModifiactionDetected.SEARCH)
                {
                    StartSearch();
                    watcher.EnableRaisingEvents = false;
                }
                else if (dd.doWhat == AfterModifiactionDetected.TURNOFF)
                {
                    watcher.EnableRaisingEvents = false;
                }
                else
                {
                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        private delegate void EnableWatcherDelegate(object state);
        private void EnableWatcher()
        {
            EnableWatcherDelegate dd = new EnableWatcherDelegate(EnableWatcher2);
            this.Dispatcher.BeginInvoke(dd, "Nothing");
        }
        private void EnableWatcher2(object state)
        {
            watcher.EnableRaisingEvents = true;
        }
        #endregion

        #region Bind Data -
        private void BindData()
        {
            // Bind Form to Data Table and Set View
            this.DataContext = dtFiles;
            viewFileNames = (CollectionView)CollectionViewSource.GetDefaultView(this.DataContext);
            //

            // Call Garbage Collector
            GC.Collect();
        }
        #endregion

        #region Show Current Folder in Statusbar
        private void ShowFoundCurrentFile(string file)
        {
            DispatcherDelegate dd = new DispatcherDelegate(ShowFoundCurrentFile2);
            this.Dispatcher.BeginInvoke(dd, file);
        }

        private void ShowFoundCurrentFile2(object state)
        {
            string msg = (string)state;
            TextBlockCurrentFile.Text = msg;
        } 
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Make Columns of DataTable
            // dtFiles
            DataColumn dc1 = new DataColumn("FileID");
            dc1.AutoIncrement = true;
            dc1.AutoIncrementSeed = 0;
            dc1.AutoIncrementStep = 1;

            DataColumn dc2 = new DataColumn("FilePath");

            DataColumn dc3 = new DataColumn("Selected");
            dc3.DefaultValue = false;

            dtFiles.Columns.Add(dc1);
            dtFiles.Columns.Add(dc2);
            dtFiles.Columns.Add(dc3);
            //

            // dtSelectedFiles
            DataColumn dc11 = new DataColumn("FileID");
            dc11.AutoIncrement = true;
            dc11.AutoIncrementSeed = 0;
            dc11.AutoIncrementStep = 1;

            DataColumn dc22 = new DataColumn("FilePath");

            DataColumn dc33 = new DataColumn("Selected");
            dc33.DefaultValue = false;

            dtSelectedFiles.Columns.Add(dc11);
            dtSelectedFiles.Columns.Add(dc22);
            dtSelectedFiles.Columns.Add(dc33);
            //
            #endregion

            #region Initials Timers
            timerClearStatus.AutoReset = false;
            timerClearStatus.Enabled = false;
            timerClearStatus.Interval = 10000;
            timerClearStatus.Elapsed += new System.Timers.ElapsedEventHandler(ClearStatusbar);

            timerUpdateSliderPosition.AutoReset = true;
            timerUpdateSliderPosition.Enabled = false;
            timerUpdateSliderPosition.Interval = 1000;
            timerUpdateSliderPosition.Elapsed += new System.Timers.ElapsedEventHandler(UpdatePosition);
            #endregion

            #region Check for Update
            // Check for update if last check was more than 7 days before
            DateTime currentDate = DateTime.Now;
            DateTime lastUpdate = Properties.Settings.Default.LastUpdateCheck;
            TimeSpan ts = currentDate - lastUpdate;
            //
            if (ts.Days > 7)
            {
                UpdateAvailbaleCallback uaCallback = new UpdateAvailbaleCallback(RaiseUpdateWindow);
                CheckUpdate chkUp = new CheckUpdate(uaCallback);
                chkUp.Check();
            }

            //UpdateAvailbaleCallback uaCallback = new UpdateAvailbaleCallback(RaiseUpdateWindow);
            //CheckUpdate chkUp = new CheckUpdate(uaCallback);
            //chkUp.Check();
            #endregion

            LoadSettings();
        }

        #region Raise Update Window
        delegate void RaiseUpdateDelegate(object state);
        private void RaiseUpdateWindow()
        {
            RaiseUpdateDelegate dd = new RaiseUpdateDelegate(RaiseUpdateWindow2);
            this.Dispatcher.BeginInvoke(dd, "Nothing");
        }

        private void RaiseUpdateWindow2(object state)
        {
            WindowUpdate wu = new WindowUpdate();
            wu.Show();
        } 
        #endregion

        #region Save and Load Setting
        private void LoadSettings()
        {
            if (Properties.Settings.Default.SourcePath != null)
            {
                SettingSourcePath = Properties.Settings.Default.SourcePath;
                TextBoxSourceFolder.Text = SettingSourcePath;
                SourcePath = SettingSourcePath;
            }
            if (Properties.Settings.Default.DestinationPath != null)
            {
                SettingDestinationPath = Properties.Settings.Default.DestinationPath;
                TextBoxDestinationFolder.Text = SettingDestinationPath;
                DestinationPath = SettingDestinationPath;
            }
            if (Properties.Settings.Default.SearchSubFolder != null)
            {
                SettingSearchSubFolder = Properties.Settings.Default.SearchSubFolder;
                CheckBoxSearchSubFolder.IsChecked = SettingSearchSubFolder;
            }
            if (Properties.Settings.Default.Volume != null)
            {
                SettingVolume = Properties.Settings.Default.Volume;
                MediaElementSelectedMedia.Volume = SettingVolume;
            }

            if (Properties.Settings.Default.Replace==true)
            {
                RadioButtonReplace.IsChecked = true;
            }
            else if (Properties.Settings.Default.Ignore==true)
            {
                RadioButtonIgnore.IsChecked = true;
            }
            else
            {
                RadioButtonRename.IsChecked = true;
            }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Volume = MediaElementSelectedMedia.Volume;
            Properties.Settings.Default.SourcePath = TextBoxSourceFolder.Text;
            Properties.Settings.Default.DestinationPath = TextBoxDestinationFolder.Text;
            Properties.Settings.Default.SearchSubFolder = (bool)CheckBoxSearchSubFolder.IsChecked;
            Properties.Settings.Default.Rename = (bool)RadioButtonRename.IsChecked;
            Properties.Settings.Default.Replace = (bool)RadioButtonReplace.IsChecked;
            Properties.Settings.Default.Ignore = (bool)RadioButtonIgnore.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }
        #endregion

        #region Clear
        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            dtFiles.Clear();
            TextBoxSearch.Text = "";
            ButtonPlayPause.IsChecked = false;
            MediaElementSelectedMedia.Close();
            SliderMediaPosition.Value = 0;
            PlayPause = false;

            // Call GC
            GC.Collect();
        } 
        #endregion

        #region Cancel Search
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Search.CancelSearch = true;
        } 
        #endregion

        #region Show Statusbar and then Clear it
        private void ShowMSG(string msg)
        {
            ShowMSGDispatcherDelegate smdispatch = new ShowMSGDispatcherDelegate(ShowMSG2);
            this.Dispatcher.BeginInvoke(smdispatch, msg);
            timerClearStatus.Enabled = true;
            timerClearStatus.Start();
        }

        private void ShowMSG2(object state)
        {
            string msg = (string)state;
            TextBlockStatus.Text = msg;
        }

        private void ClearStatusbar(object sender, ElapsedEventArgs e)
        {
            CallClearStatusbar();
        }

        private void CallClearStatusbar()
        {
            ClearMSGDelegate cmd = new ClearMSGDelegate(ClearStatusbar2);
            this.Dispatcher.BeginInvoke(cmd, "Nothing");
        }

        private void ClearStatusbar2(object state)
        {
            TextBlockStatus.Text = "";
        }
        #endregion

        #region Filter Based on TextBox
        private void TextBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ThreadStart ts = new ThreadStart(BeginSearch);
            Thread t1 = new Thread(ts);
            t1.IsBackground = true;
            t1.Start();
        }

        delegate void SearchFileNamesDelegate(object state);

        public void BeginSearch()
        {
            SearchFileNamesDelegate sfn = new SearchFileNamesDelegate(BindDataViewAsync);
            Dispatcher.BeginInvoke(sfn, "Bind");
        }

        private void BindDataViewAsync(object state)
        {
            dvFiles = dtFiles.DefaultView;
            dvFiles.RowFilter = string.Format("FilePath LIKE '%{0}%'", TextBoxSearch.Text);
        }
        #endregion

        #region Control Media Playback
        private void PlayCurrentSelectedItem(object sender, RoutedEventArgs e)
        {
            PlayPause = false;
            DoPlay();
        }

        /// <summary>
        /// This method play current item in default CollectionView
        /// </summary>
        private void DoPlay()
        {
            try
            {
                if (PlayPause == false)
                {
                    DataRowView currentRow = (DataRowView)viewFileNames.CurrentItem;
                    string FileTobePlayed = currentRow["FilePath"].ToString();
                    Uri _uri = new Uri(FileTobePlayed);
                    MediaElementSelectedMedia.Source = _uri;
                    MediaElementSelectedMedia.Play();
                    ButtonPlayPause.IsChecked = true;
                }
                else
                {
                    MediaElementSelectedMedia.Play();
                }
            }
            catch (NullReferenceException ex)
            {
                ButtonPlayPause.IsChecked = false;
            }
            catch(Exception ex)
            {

            }
        }

        private void PlayFigure()
        {
            System.Windows.Shapes.Path playPath = new System.Windows.Shapes.Path();
            playPath.Data = Geometry.Parse("F1 M 352.17,191.918L 384.667,207.876L 352.001,224.085L 352.17,191.918 Z ");
            playPath.Width=12;
            playPath.Height=15;
            playPath.Stretch=Stretch.Fill;
            BrushConverter brushCv = new BrushConverter();
            playPath.Fill =(Brush) brushCv.ConvertFromString("#FF000000");

            ButtonPlayPause.Content = playPath;
        }

        #region Play Pause Figure
        private void PauseFigure()
        {
            System.Windows.Shapes.Path playPath = new System.Windows.Shapes.Path();
            playPath.Data = Geometry.Parse("F1 M 352.001,191.751L 352.001,207.585");
            playPath.Width = 8;
            playPath.Height = 10;
            playPath.Stretch = Stretch.Fill;
            playPath.StrokeLineJoin = PenLineJoin.Round;
            BrushConverter brushCv = new BrushConverter();
            playPath.Fill = (Brush)brushCv.ConvertFromString("#FF000000");

            System.Windows.Shapes.Path playPath2 = new System.Windows.Shapes.Path();
            playPath2.Data = Geometry.Parse("F1 M 352.001,191.751L 352.001,207.585");
            playPath2.Width = 8;
            playPath2.Height = 10;
            playPath2.Stretch = Stretch.Fill;
            playPath2.StrokeLineJoin = PenLineJoin.Round;
            BrushConverter brushCv2 = new BrushConverter();
            playPath2.Fill = (Brush)brushCv2.ConvertFromString("#FF000000");

            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Children.Add(playPath);
            sp.Children.Add(playPath2);

            ButtonPlayPause.Content = sp;
        } 
        #endregion

        //private void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
        //{
        //    DoPlay();
        //}

        /// <summary>
        /// 10 Seconds forward
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonFastForward_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double currentPosition = MediaElementSelectedMedia.Position.TotalSeconds;
                double newPosition = currentPosition + 10;
                MediaElementSelectedMedia.Position = TimeSpan.FromSeconds(newPosition);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// 10 Seconds backward
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonBackward_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double currentPosition = MediaElementSelectedMedia.Position.TotalSeconds;
                double newPosition = currentPosition - 10;
                MediaElementSelectedMedia.Position = TimeSpan.FromSeconds(newPosition);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Play Next Media
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (viewFileNames.CurrentPosition < viewFileNames.Count - 1)
                {
                    viewFileNames.MoveCurrentToNext();
                    PlayPause = false;
                    DoPlay();
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Play Previous Media
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPrev_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (viewFileNames.CurrentPosition > 0)
                {
                    viewFileNames.MoveCurrentToPrevious();
                    PlayPause = false;
                    DoPlay();
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Stop Media
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ButtonPlayPause.IsChecked = false;
                MediaElementSelectedMedia.Stop();
                PlayPause = false;
            }
            catch (Exception ex)
            {
            }
        }


        private void MediaElementSelectedMedia_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Set Slider timer to false,so at the next media playback
            // it will be Enabled
            timerUpdateSliderPosition.Enabled = false;
            //

            // Play Next media
            PlayPause = false;
            if (viewFileNames.CurrentPosition < viewFileNames.Count - 1)
            {
                viewFileNames.MoveCurrentToNext();
                DoPlay();
            }
            else if (viewFileNames.CurrentPosition==viewFileNames.Count-1)
            {
                viewFileNames.MoveCurrentToFirst();
                DoPlay();
            }
            //
        }

        private void MediaElementSelectedMedia_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Just before playing media these lines of codes get duration of media
            // and set the Maximum property of Slider to Duration of Media
            WMPLib.WindowsMediaPlayerClass mp = new WMPLib.WindowsMediaPlayerClass();
            WMPLib.IWMPMedia mediainfo = mp.newMedia(MediaElementSelectedMedia.Source.ToString());
            SliderMediaPosition.Maximum = mediainfo.duration;
            //

            // Start the timer that moves Slider every one minute
            timerUpdateSliderPosition.Enabled = true;
            timerUpdateSliderPosition.Start();
            //
        }

        // Move Slider every one minute
        delegate void UpdatePositionDelegate(object state);
        private void UpdatePosition(object sender, ElapsedEventArgs e)
        {
            UpdatePositionDelegate dd = new UpdatePositionDelegate(UpdatePosition2);
            this.Dispatcher.BeginInvoke(dd, "Nothing");
        }
        private void UpdatePosition2(object state)
        {
            SliderMediaPosition.Value = MediaElementSelectedMedia.Position.TotalSeconds;
        }
        //
        #endregion

        #region Copy
        private void ButtonCopyFiles_Click(object sender, RoutedEventArgs e)
        {
            dvFiles = dtFiles.DefaultView;
            dvFiles.RowFilter = "Selected=true";
            // if no item is selected do nothing
            if (dvFiles.Count == 0)
            {
                dvFiles.RowFilter = "";
                ShowMSG("No item is selected");
                return;
            }
            dtSelectedFiles.Clear();
            dtSelectedFiles = dvFiles.ToTable();

            List<string> SelectedFiles = new List<string>();
            for (int i = 0; i < dtSelectedFiles.Rows.Count; i++)
            {
                DataRow dr = dtSelectedFiles.Rows[i];
                SelectedFiles.Add(dr["FilePath"].ToString());
            }

            WindowCopy winCP = new WindowCopy();
            winCP.SelectedFiles = SelectedFiles;
            if (GetDestinationFolder() == true)
            {
                winCP.DestinationFolder = TextBoxDestinationFolder.Text;
            }
            else
            {
                // Alert user that the destination folder does not exist
                // and what he want to do? Create?Select another folder?Cancel?
                DialogBoxDestinationFolderDoesNotExist dd = new DialogBoxDestinationFolderDoesNotExist();
                dd.SetDestinationFolder(TextBoxDestinationFolder.Text);
                dd.ShowDialog();
                try
                {
                    do
                    {
                        if (dd.UserSelection == ConfirmCreate.CREATE)
                        {
                            Directory.CreateDirectory(TextBoxDestinationFolder.Text);
                            winCP.DestinationFolder = TextBoxDestinationFolder.Text;
                        }
                        else if (dd.UserSelection == ConfirmCreate.SELECT)
                        {
                            if (SelectDestinationFolder()==false)
                            {
                                return;
                            }
                            winCP.DestinationFolder = TextBoxDestinationFolder.Text;
                        }
                        else
                        {
                            return;
                        }
                    } while (GetDestinationFolder()==false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                    return;
                }
            }
            // Set operation to Copy
            WindowCopy.CopyOrCut = SelectOperation.COPY_FILES;

            if (RadioButtonRename.IsChecked == true)
            {
                WindowCopy.IfFileNameAlreadyExists = FileNameExist.RENAME_FILE;
            }
            else if (RadioButtonReplace.IsChecked == true)
            {
                WindowCopy.IfFileNameAlreadyExists = FileNameExist.REPLACE_FILE;
            }
            else if (RadioButtonIgnore.IsChecked == true)
            {
                WindowCopy.IfFileNameAlreadyExists = FileNameExist.IGNORE_FILE;
            }

            winCP.Show();
        } 
        #endregion

        #region Cut
        private void ButtonCut_Click(object sender, RoutedEventArgs e)
        {
            watcher.EnableRaisingEvents = false;
            dvFiles = dtFiles.DefaultView;
            dvFiles.RowFilter = "Selected=true";
            dtSelectedFiles.Clear();
            dtSelectedFiles = dvFiles.ToTable();

            // if no item is selected do nothing
            if (dvFiles.Count == 0)
            {
                dvFiles.RowFilter = "";
                ShowMSG("No item is selected");
                return;
            }

            List<string> SelectedFiles = new List<string>();
            for (int i = 0; i < dtSelectedFiles.Rows.Count; i++)
            {
                DataRow dr = dtSelectedFiles.Rows[i];
                SelectedFiles.Add(dr["FilePath"].ToString());
            }

            WindowCopy winCP = new WindowCopy();
            winCP.EnbaleWatcher(this.EnableWatcher);
            winCP.SelectedFiles = SelectedFiles;
            if (GetDestinationFolder() == true)
            {
                winCP.DestinationFolder = TextBoxDestinationFolder.Text;
            }
            else
            {
                // Alert user that the destination folder does not exist
                // and what he want to do? Create?Select another folder?Cancel?
                DialogBoxDestinationFolderDoesNotExist dd = new DialogBoxDestinationFolderDoesNotExist();
                dd.SetDestinationFolder(TextBoxDestinationFolder.Text);
                dd.ShowDialog();
                try
                {
                    do
                    {
                        if (dd.UserSelection == ConfirmCreate.CREATE)
                        {
                            Directory.CreateDirectory(TextBoxDestinationFolder.Text);
                            winCP.DestinationFolder = TextBoxDestinationFolder.Text;
                        }
                        else if (dd.UserSelection == ConfirmCreate.SELECT)
                        {
                            if (SelectDestinationFolder() == false)
                            {
                                return;
                            }
                            winCP.DestinationFolder = TextBoxDestinationFolder.Text;
                        }
                        else
                        {
                            return;
                        }
                    } while (GetDestinationFolder() == false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                    return;
                }
                //
            }
            // Set operation to Copy
            WindowCopy.CopyOrCut = SelectOperation.CUT_FILES;

            if (RadioButtonRename.IsChecked == true)
            {
                WindowCopy.IfFileNameAlreadyExists = FileNameExist.RENAME_FILE;
            }
            else if (RadioButtonReplace.IsChecked == true)
            {
                WindowCopy.IfFileNameAlreadyExists = FileNameExist.REPLACE_FILE;
            }
            else if (RadioButtonIgnore.IsChecked == true)
            {
                WindowCopy.IfFileNameAlreadyExists = FileNameExist.IGNORE_FILE;
            }

            winCP.Show();
        } 
        #endregion

        #region PlayPauseButton
        private void ButtonPlayPause_Checked(object sender, RoutedEventArgs e)
        {
            DoPlay();
            PlayPause = false;
        }

        private void ButtonPlayPause_Unchecked(object sender, RoutedEventArgs e)
        {
            MediaElementSelectedMedia.Pause();
            PlayPause = true;
        }
        #endregion

        #region ListView ContextMenu
        private void ListViewMenuItemCheckAllSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (DataRowView row in ListViewFiles.SelectedItems)
                {
                    row["Selected"] = true;
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void ListViewMenuItemCheckAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ListViewFiles.SelectAll();
                foreach (DataRowView row in ListViewFiles.SelectedItems)
                {
                    row["Selected"] = true;
                }
            }
            catch (Exception ex)
            {

            }
        }
        private void ListViewMenuItemClearAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ListViewFiles.SelectAll();
                foreach (DataRowView row in ListViewFiles.SelectedItems)
                {
                    row["Selected"] = false;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void ListViewMenuItemClearAllSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (DataRowView row in ListViewFiles.SelectedItems)
                {
                    row["Selected"] = false;
                }
            }
            catch (Exception ex)
            {
            }
        }
        #endregion


        //private void ChechBoxCopyAsync_Checked(object sender, RoutedEventArgs e)
        //{
        //    WindowCopy.CopyAsync = (bool)ChechBoxCopyAsync.IsChecked;
        //}

    }
}
