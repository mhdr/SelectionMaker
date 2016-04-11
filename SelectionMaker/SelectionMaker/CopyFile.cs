using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace SelectionMaker
{
    class CopyFile
    {
        #region Fields
        private static int _pbCancel = 0;
        private string _oldeFile;
        private string _newFile;
        private CopyFileFinishedCallback _Copy_Completed_Callback;
        private copyFileProgress _Copy_Progress;
        private TrasferedSizeDelegate _trasferedSize;
        #endregion

        #region Constructor Deconstructor
        public CopyFile(string oldFile, string newFile,CopyFileFinishedCallback cpfcallback,copyFileProgress cpProgress,TrasferedSizeDelegate transfered)
        {
            this._oldeFile = oldFile;
            this._newFile = newFile;
            this._Copy_Completed_Callback = cpfcallback;
            this._Copy_Progress = cpProgress;
            this._trasferedSize = transfered;
        } 
        ~CopyFile()
        {

        }
        #endregion

        #region CopyFileEx Implementation
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CopyFileEx(string lpExistingFileName, string lpNewFileName,
           CopyProgressRoutine lpProgressRoutine, IntPtr lpData, ref Int32 pbCancel,
           CopyFileFlags dwCopyFlags);

        delegate CopyProgressResult CopyProgressRoutine(
            long TotalFileSize,
            long TotalBytesTransferred,
            long StreamSize,
            long StreamBytesTransferred,
            uint dwStreamNumber,
            CopyProgressCallbackReason dwCallbackReason,
            IntPtr hSourceFile,
            IntPtr hDestinationFile,
            IntPtr lpData);

        enum CopyProgressCallbackReason : uint
        {
            CALLBACK_CHUNK_FINISHED = 0x00000000,
            CALLBACK_STREAM_SWITCH = 0x00000001
        }

        [Flags]
        enum CopyFileFlags : uint
        {
            COPY_FILE_FAIL_IF_EXISTS = 0x00000001,
            COPY_FILE_RESTARTABLE = 0x00000002,
            COPY_FILE_OPEN_SOURCE_FOR_WRITE = 0x00000004,
            COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x00000008
        }

        enum CopyProgressResult : uint
        {
            PROGRESS_CONTINUE = 0,
            PROGRESS_CANCEL = 1,
            PROGRESS_STOP = 2,
            PROGRESS_QUIET = 3
        }

        private CopyProgressResult CopyProgressHandler(long total, long transferred, long streamSize, long StreamByteTrans, uint dwStreamNumber, CopyProgressCallbackReason reason, IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData)
        {
            if (_pbCancel==0)
            {
                float progress = 100 * transferred / total;
                _Copy_Progress(progress);
                _trasferedSize(transferred);
                return CopyProgressResult.PROGRESS_CONTINUE; 
            }
            else
            {
                return CopyProgressResult.PROGRESS_CANCEL;
            }
        } 
        #endregion

        #region Start Copy
        public void StartCopyAsync()
        {
            ThreadStart ts = new ThreadStart(StartCopyAsync2);
            Thread t1 = new Thread(ts);
            t1.IsBackground = true;
            t1.Start();
        }
        public void StartCopyAsync2()
        {
            CopyProgressRoutine cpr = new CopyProgressRoutine(CopyProgressHandler);
            CopyFileEx(_oldeFile, _newFile, cpr, IntPtr.Zero, ref _pbCancel, CopyFileFlags.COPY_FILE_RESTARTABLE);
            _Copy_Completed_Callback(_oldeFile, _newFile);
        } 
        #endregion

        #region Cancel Copy
        public static void StopCopy()
        {
            _pbCancel = 1;
        }

        public static void CancelStop()
        {
            _pbCancel = 0;
        } 
        #endregion
    }
}
