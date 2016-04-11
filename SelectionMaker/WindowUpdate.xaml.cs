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
using System.Xml;
using System.Xml.XPath;

namespace SelectionMaker
{
    /// <summary>
    /// Interaction logic for WindowUpdate.xaml
    /// </summary>
    public partial class WindowUpdate : Window
    {
        public WindowUpdate()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            #region Open Download Page
            XmlDataProvider xmlData = (XmlDataProvider)this.TryFindResource("NewFeaturesDataSource");
            XmlDocument doc = xmlData.Document;
            XPathNavigator nav = doc.CreateNavigator();
            XPathNodeIterator nodes = nav.Select("/SelectionMaker/DownloadLink");
            nodes.MoveNext();
            string _link = nodes.Current.InnerXml;

            System.Diagnostics.Process.Start(_link);
            this.Close();
            #endregion
        }
    }
}
