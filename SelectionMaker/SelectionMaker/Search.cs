using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Threading;

namespace SelectionMaker
{
    class Search
    {
        #region Fields
        private string _sourcepPath;
        private CallBackSearchFolder _callbackSearch;
        public static DataTable dtFiles = new DataTable("Files");
        private ProgressCurrentFile _currentFile_Callback;
        public static bool CancelSearch;
        private ShowMSGDelegate _show_msg_callback;
        #endregion

        #region Constructors Deconstructor
        public Search()
        {
            MakeDataTable();
        }

        public Search(string SourcePath)
        {
            this._sourcepPath = SourcePath;
            MakeDataTable();
        }

        public Search(string SourcePath, CallBackSearchFolder callback,ShowMSGDelegate showmsg)
        {
            this._sourcepPath = SourcePath;
            this._callbackSearch = callback;
            this._show_msg_callback = showmsg;
            MakeDataTable();
        } 

        public Search(string SourcePath, CallBackSearchFolder callback,ProgressCurrentFile currentFileCallback,ShowMSGDelegate showmsg)
        {
            this._sourcepPath = SourcePath;
            this._callbackSearch = callback;
            this._currentFile_Callback = currentFileCallback;
            this._show_msg_callback = showmsg;
            MakeDataTable();
        }
        ~ Search()
        {

        }
        #endregion

        #region Search Files in Main Thread
        public DataTable SearchFolder()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(this._sourcepPath);
                FileInfo[] fi = di.GetFiles();

                foreach (FileInfo file in fi)
                {
                    DataRow dr = dtFiles.NewRow();
                    dr["FilePath"] = file.FullName;
                    dtFiles.Rows.Add(dr);
                }

                return dtFiles;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion

        #region Search Files in New Thread
        public void SearchFolderNewThread()
        {
            
            try
            {
                DirectoryInfo di = new DirectoryInfo(this._sourcepPath);
                FileInfo[] fi = di.GetFiles();

                foreach (FileInfo file in fi)
                {
                    DataRow dr = dtFiles.NewRow();
                    dr["FilePath"] = file.FullName;
                    dtFiles.Rows.Add(dr);
                }
                _callbackSearch(dtFiles);
                _show_msg_callback(string.Format("Total Files Found: {0},Search Completed", dtFiles.Rows.Count));
            }
            catch (Exception ex)
            {
                _show_msg_callback(ex.Message);
            }
        }
        #endregion

        #region Make Columns of DataTable
        private static void MakeDataTable()
        {
            dtFiles.Clear();
            try
            {
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
            }
            catch (System.Data.DuplicateNameException ex)
            {

            }
        }
        #endregion

        #region Search SubFolder in Main Thread
        public DataTable SearchSubFolder()
        {
            DoSearchSubFolder(this._sourcepPath);
            return dtFiles;
        }

        private void DoSearchSubFolder(string searchDirectory)
        {
            try
            {
                foreach (string st in Directory.GetDirectories(searchDirectory))
                {
                    foreach (string f in Directory.GetFiles(st))
                    {
                        DataRow dr = dtFiles.NewRow();
                        dr["FilePath"] = f;
                        dtFiles.Rows.Add(dr);
                    }
                    DoSearchSubFolder(st);
                }
            }
            catch (System.Exception ex)
            {

            }
        }
        #endregion

        #region Search SubFolder in New Thread
        public void SearchSubFolderNewThread()
        {
            SearchFolder();
            DoSearchSubFolderNewThread(this._sourcepPath);
        }

        private void DoSearchSubFolderNewThread(string searchDirectory)
        {
            try
            {
                foreach (string st in Directory.GetDirectories(searchDirectory))
                {
                    _currentFile_Callback(string.Format("Files Found: {0}, Searching: {1}", dtFiles.Rows.Count, st));
                    foreach (string f in Directory.GetFiles(st))
                    {
                        // When users press Cancel Button the result that is collected so far
                        // will be displayed and then current thread will shutdown
                        if (CancelSearch==true)
                        {
                            Thread currentThread = Thread.CurrentThread;
                            _callbackSearch(dtFiles);
                            currentThread.Abort();
                        }
                        DataRow dr = dtFiles.NewRow();
                        dr["FilePath"] = f;
                        dtFiles.Rows.Add(dr);
                    }
                    DoSearchSubFolder(st);
                }
                _callbackSearch(dtFiles);
                _show_msg_callback(string.Format("Total Files Found: {0},Search Completed", dtFiles.Rows.Count));
            }
            catch (System.Threading.ThreadAbortException ex)
            {
                _show_msg_callback("Search Canceled");
            }
            catch (System.Exception ex)
            {
                _show_msg_callback(ex.Message);
            }
        }
        #endregion
    }
}
