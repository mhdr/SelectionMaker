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
using System.Windows.Shapes;
using System.Threading;
using System.IO;

namespace SelectionMaker
{

    public delegate void CopyFileFinishedCallback(string oldFile, string newFile);
    public delegate void copyFileProgress(float progress);
    public delegate void TrasferedSizeDelegate(long transfered);
    public delegate void CopyAllFinishedCallback();
    public delegate void SingleFileCopyStartedDelegate(CopyCurrentFileInfo currentData);
    public enum SelectOperation : uint
    {
        COPY_FILES = 0,
        CUT_FILES = 1,
    }
    public enum FileNameExist : uint
    {
        RENAME_FILE = 0,
        REPLACE_FILE = 1,
        IGNORE_FILE = 2,
    }

    /// <summary>
    /// Interaction logic for WindowCopy.xaml
    /// </summary>
    public partial class WindowCopy : Window
    {
        public List<string> SelectedFiles;
        public string DestinationFolder;
        private long totalSize;
        static long transferedLast;
        static long totalTrasfered;
        public static bool CopyAsync;
        public static SelectOperation CopyOrCut;
        // Select between Rename,Replace,Ignore
        public static FileNameExist IfFileNameAlreadyExists;
        private bool ignoreCurrentFile;
        //

        EnableWatcherAfterCutting _enable_watcher;

        public WindowCopy()
        {
            InitializeComponent();
        }

        #region Start Copy
        private void StartCopy()
        {
            ThreadStart ts = new ThreadStart(StartCopy2);
            Thread t1 = new Thread(ts);
            t1.IsBackground = true;
            t1.Start();
        }

        private void StartCopy2()
        {
            // if last time stopped cancel set initials it again
            CopyFile.CancelStop();

            copyFileProgress cpProgress = new copyFileProgress(ProgressShowForSingleFile);
            CopyFileFinishedCallback cpFinished = new CopyFileFinishedCallback(CopySingleFileCompleted);
            TrasferedSizeDelegate tsTransfer = new TrasferedSizeDelegate(CalculateTotalTransfered);
            CopyAllFinishedCallback cpFinishedAll = new CopyAllFinishedCallback(FinishedCopyingAllFiles);
            SingleFileCopyStartedDelegate sfCopyd = new SingleFileCopyStartedDelegate(CopySingleFileStarted);
            foreach (string fileName in SelectedFiles)
            {

                CopyFile cp = new CopyFile(fileName, GetDestinationFileName(fileName), cpFinished, cpProgress, tsTransfer);

                if (ignoreCurrentFile == true)
                {
                    // Reset for next use
                    ignoreCurrentFile = false;
                    //
                    continue;
                }

                sfCopyd(new CopyCurrentFileInfo
                {
                    oldFile = fileName,
                    newFile = GetDestinationFileName(fileName),
                    CurrentFileNumber = SelectedFiles.IndexOf(fileName) + 1,
                    TotalFileNumber = SelectedFiles.Count
                });

                cp.StartCopyAsync2();
            }
            cpFinishedAll();
        }
        #endregion

        #region Start Cut
        private void StartCut()
        {
            ThreadStart ts = new ThreadStart(StartCut2);
            Thread t1 = new Thread(ts);
            t1.IsBackground = true;
            t1.Start();
        }

        private void StartCut2()
        {
            // if last time stopped cancel set initials it again
            CopyFile.CancelStop();

            copyFileProgress cpProgress = new copyFileProgress(ProgressShowForSingleFile);
            CopyFileFinishedCallback cpFinished = new CopyFileFinishedCallback(CopySingleFileCompleted);
            TrasferedSizeDelegate tsTransfer = new TrasferedSizeDelegate(CalculateTotalTransfered);
            CopyAllFinishedCallback cpFinishedAll = new CopyAllFinishedCallback(FinishedCopyingAllFiles);
            SingleFileCopyStartedDelegate sfCopyd = new SingleFileCopyStartedDelegate(CopySingleFileStarted);
            foreach (string fileName in SelectedFiles)
            {
                CutFile cu = new CutFile(fileName, GetDestinationFileName(fileName), cpFinished, cpProgress, tsTransfer);

                if (ignoreCurrentFile == true)
                {
                    // Reset for next use
                    ignoreCurrentFile = false;
                    //
                    continue;
                }

                sfCopyd(new CopyCurrentFileInfo
                {
                    newFile = fileName,
                    oldFile = GetDestinationFileName(fileName),
                    CurrentFileNumber = SelectedFiles.IndexOf(fileName) + 1,
                    TotalFileNumber = SelectedFiles.Count
                });

                cu.StartCutAsync2();
            }
            cpFinishedAll();

            // file watcher is disabled durring cutting operation
            // when cutting is done,it should be enabled again
            _enable_watcher();
            //
        }
        #endregion

        #region After Copying of all files finieshed
        delegate void DispatchFinisheAllCompleted(object state);
        private void FinishedCopyingAllFiles()
        {
            DispatchFinisheAllCompleted dd = new DispatchFinisheAllCompleted(FinishedCopyingAllFiles2);
            this.Dispatcher.BeginInvoke(dd, "Nothing");
        }
        private void FinishedCopyingAllFiles2(object state)
        {
            // Call Garbage Collector
            GC.Collect();

            this.Close();
        }
        #endregion

        /// <summary>
        /// Check if it should be Rename,Replace,Ignore
        /// then if needed return new file name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetDestinationFileName(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            string ReturnValue = DestinationFolder + "\\" + fi.Name;

            if (IfFileNameAlreadyExists == FileNameExist.RENAME_FILE)
            {
                while (File.Exists(ReturnValue))
                {
                    FileInfo fi2 = new FileInfo(ReturnValue);
                    string OldName = fi2.Name;
                    ReturnValue = DestinationFolder + "\\" + "Copy Of " + fi2.Name;
                }
            }
            else if (IfFileNameAlreadyExists == FileNameExist.IGNORE_FILE)
            {
                if (File.Exists(ReturnValue))
                {
                    ignoreCurrentFile = true;
                }
            }

            return ReturnValue;
        }

        #region Show Progress for Single File
        delegate void DelegateProgressShow(object state);
        private void ProgressShowForSingleFile(float progress)
        {
            DelegateProgressShow dd = new DelegateProgressShow(ProgressShowForSingleFile2);
            this.Dispatcher.BeginInvoke(dd, progress);
        }

        private void ProgressShowForSingleFile2(object state)
        {
            float progress = (float)state;
            ProgressBarSingleFile.Value = Convert.ToDouble(progress);
            TextBlockProgressCurrentFile.Text = string.Format("{0}%", progress);
        }
        #endregion

        #region Show Progress for total
        delegate void DelegateProgressTotalShow(object state);
        private void ProgressShowForTotal()
        {
            DelegateProgressTotalShow dd = new DelegateProgressTotalShow(ProgressShowForTotal2);
            this.Dispatcher.BeginInvoke(dd, "Nothing");
        }

        private void ProgressShowForTotal2(object state)
        {
            ProgressBarTotal.Value = Convert.ToDouble(100 * totalTrasfered / totalSize);
            TextBlockTotoalProgress.Text = string.Format("{0}%", 100 * totalTrasfered / totalSize);
        }

        private void GetSizeofAllFiles()
        {
            ThreadStart ts = new ThreadStart(GetSizeofAllFiles2);
            Thread t1 = new Thread(ts);
            t1.IsBackground = true;
            t1.Start();
        }

        private void GetSizeofAllFiles2()
        {
            try
            {
                long totalFileSize = 0;
                foreach (string file in SelectedFiles)
                {
                    FileInfo fi = new FileInfo(file);
                    totalFileSize += fi.Length;
                }

                this.totalSize = totalFileSize;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void TrasferedBeforeLastFile(long lastFile)
        {
            transferedLast += lastFile;
        }

        private void CalculateTotalTransfered(long trasfered)
        {
            totalTrasfered = transferedLast + trasfered;
            ProgressShowForTotal();
        }
        #endregion

        #region After Copying of Single File Finished
        private void CopySingleFileCompleted(string oldFile, string newFile)
        {
            try
            {
                FileInfo fi = new FileInfo(newFile);
                TrasferedBeforeLastFile(fi.Length);
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        #region Copying of a Single File Started
        delegate void CopySingleFileStartedDelegate(object data);
        private void CopySingleFileStarted(CopyCurrentFileInfo data)
        {
            CopySingleFileStartedDelegate dd = new CopySingleFileStartedDelegate(CopySingleFileStarted2);
            this.Dispatcher.BeginInvoke(dd, data);
        }

        private void CopySingleFileStarted2(object data)
        {
            CopyCurrentFileInfo passeddata = (CopyCurrentFileInfo)data;
            TextBlockSourceFile.Text = passeddata.oldFile;
            TextBlockDestinationFile.Text = passeddata.newFile;
            TextBlockCurrentFileNumber.Text = passeddata.CurrentFileNumber.ToString();
            TextBlockTotalNumberofFiles.Text = passeddata.TotalFileNumber.ToString();
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Clear From Last Operation
            totalSize = 0;
            totalTrasfered = 0;
            transferedLast = 0;
            //

            GetSizeofAllFiles();

            if (CopyOrCut == SelectOperation.COPY_FILES)
            {
                StartCopy();
            }
            else if (CopyOrCut == SelectOperation.CUT_FILES)
            {
                StartCut();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (CopyOrCut == SelectOperation.COPY_FILES)
            {
                CopyFile.StopCopy();
            }
        }

        #region Initials _enable_watcher
        public void EnbaleWatcher(EnableWatcherAfterCutting watcher)
        {
            _enable_watcher = watcher;
        } 
        #endregion
    }

    public class CopyCurrentFileInfo
    {
        public string oldFile;
        public string newFile;
        public int CurrentFileNumber;
        public int TotalFileNumber;
    }
}
