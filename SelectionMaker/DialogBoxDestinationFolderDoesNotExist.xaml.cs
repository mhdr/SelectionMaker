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
    public enum ConfirmCreate : uint
    {
        CREATE=0,
        SELECT=1,
        CANCEL=2,
    }

    /// <summary>
    /// Interaction logic for DialogBoxDestinationFolderDoesNotExist.xaml
    /// </summary>
    public partial class DialogBoxDestinationFolderDoesNotExist : Window
    {
        public ConfirmCreate UserSelection = ConfirmCreate.CANCEL;
        private string DestinationFolder;

        public void SetDestinationFolder(string path)
        {
            DestinationFolder = path;
        }

        public DialogBoxDestinationFolderDoesNotExist()
        {
            InitializeComponent();
        }

        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = ConfirmCreate.CREATE;
            this.Close();
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = ConfirmCreate.SELECT;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = ConfirmCreate.CANCEL;
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlockDestinationPath.Text = string.Format("Destination Folder : {0}", DestinationFolder);
            TextBlockShowMSG.Text = "This folder does not exist,do you want to create it or select another folder?";
        }
    }
}
