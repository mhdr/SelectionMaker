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

namespace SelectionMaker
{
    public enum AfterModifiactionDetected:uint
    {
        SEARCH=0,
        TURNOFF=1,
        CANCEL=2,
    }
    /// <summary>
    /// Interaction logic for DialogeBoxModificationDetected.xaml
    /// </summary>
    public partial class DialogeBoxModificationDetected : Window
    {
        private string SourcePath;
        public AfterModifiactionDetected doWhat = AfterModifiactionDetected.CANCEL;
        public static bool IsOpen = false;

        public DialogeBoxModificationDetected()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IsOpen = true;
            TextBlockSourcePath.Text = string.Format("Source path : {0}", SourcePath);
            TextBlockShowMSG.Text = "Modification detected in this folder,do you want to search this folder again?";
        }

        public void GetSourcePath(string path)
        {
            SourcePath = path;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            doWhat = AfterModifiactionDetected.CANCEL;
            this.Close();
        }

        private void ButtonSearchAgain_Click(object sender, RoutedEventArgs e)
        {
            doWhat = AfterModifiactionDetected.SEARCH;
            this.Close();
        }

        private void ButtonTurnOffWatcher_Click(object sender, RoutedEventArgs e)
        {
            doWhat = AfterModifiactionDetected.TURNOFF;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsOpen = false;
        }
    }
}
