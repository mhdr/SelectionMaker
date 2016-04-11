using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace SelectionMaker
{
    class CutFile
    {
        #region Fields
        private string _oldeFile;
        private string _newFile;
        private CopyFileFinishedCallback _Copy_Completed_Callback;
        private copyFileProgress _Copy_Progress;
        private TrasferedSizeDelegate _trasferedSize;
        #endregion

        #region Constructor Deconstructor
        public CutFile(string oldFile, string newFile,CopyFileFinishedCallback cpfcallback,copyFileProgress cpProgress,TrasferedSizeDelegate transfered)
        {
            this._oldeFile = oldFile;
            this._newFile = newFile;
            this._Copy_Completed_Callback = cpfcallback;
            this._Copy_Progress = cpProgress;
            this._trasferedSize = transfered;
        }
 
        ~CutFile()
        {

        }
        #endregion

        #region MoveFileWithProgress Implementation
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool MoveFileWithProgress(string lpExistingFileName, string lpNewFileName,
           CopyProgressRoutine lpProgressRoutine, IntPtr lpData, MoveFileFlags dwCopyFlags);

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

        enum CopyProgressResult : uint
        {
            PROGRESS_CONTINUE = 0,
            PROGRESS_CANCEL = 1,
            PROGRESS_STOP = 2,
            PROGRESS_QUIET = 3
        }

        enum CopyProgressCallbackReason : uint
        {
            CALLBACK_CHUNK_FINISHED = 0x00000000,
            CALLBACK_STREAM_SWITCH = 0x00000001
        }

        [Flags]
        enum MoveFileFlags : uint
        {
            MOVE_FILE_REPLACE_EXISTSING = 0x00000001,
            MOVE_FILE_COPY_ALLOWED = 0x00000002,
            MOVE_FILE_DELAY_UNTIL_REBOOT = 0x00000004,
            MOVE_FILE_WRITE_THROUGH = 0x00000008,
            MOVE_FILE_CREATE_HARDLINK = 0x00000010,
            MOVE_FILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
        }

        private CopyProgressResult CopyProgressHandler(long total, long transferred, long streamSize, long StreamByteTrans, uint dwStreamNumber, CopyProgressCallbackReason reason, IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData)
        {
            float progress = 100 * transferred / total;
            _Copy_Progress(progress);
            _trasferedSize(transferred);
            return CopyProgressResult.PROGRESS_CONTINUE; 
        } 
        #endregion

        public void StartCutAsync()
        {
            ThreadStart ts = new ThreadStart(StartCutAsync2);
            Thread t1 = new Thread(ts);
            t1.IsBackground = true;
            t1.Start();
        }
        public void StartCutAsync2()
        {
            CopyProgressRoutine cpr = new CopyProgressRoutine(CopyProgressHandler);
            MoveFileWithProgress(_oldeFile, _newFile, cpr, IntPtr.Zero, MoveFileFlags.MOVE_FILE_COPY_ALLOWED | MoveFileFlags.MOVE_FILE_REPLACE_EXISTSING);
            _Copy_Completed_Callback(_oldeFile, _newFile);
        }
    }
}
