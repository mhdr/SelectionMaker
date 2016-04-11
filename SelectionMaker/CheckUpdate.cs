using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

namespace SelectionMaker
{
    class CheckUpdate
    {
        public static XPathDocument doc = new XPathDocument("http://mahmoodramzani.persiangig.com/selectionmaker/SelectionMaker.xml");
        UpdateAvailbaleCallback _update_available;

        public CheckUpdate(UpdateAvailbaleCallback uv)
        {
            _update_available = uv;
        }

        public void Check()
        {
            WaitCallback wc=new WaitCallback(Check2);
            ThreadPool.QueueUserWorkItem(wc);

            //ThreadStart ts=new ThreadStart(Check2);
            //Thread t1 = new Thread(ts);
            //t1.IsBackground = true;
            //t1.Start();
        }

        private void Check2(object state)
        {
            try
            {
                Thread.Sleep(10000);

                // read xml file and get version from xml file
                XPathNavigator nav = doc.CreateNavigator();
                XPathNodeIterator nodes = nav.Select("/SelectionMaker/Version");
                nodes.MoveNext();
                string _version = nodes.Current.InnerXml;
                //

                // Check version in current executing assembly
                Assembly asm = Assembly.GetExecutingAssembly();
                AssemblyName asm_name = asm.GetName();
                Version _version2 = asm_name.Version;
                //

                // Compare versions
                int remote_version = Convert.ToInt32(_version);
                int local_version = _version2.Major * 10 + _version2.Minor;

                if (remote_version>local_version)
                {
                    _update_available();
                }
                //

                // Save Last Update Date
                Properties.Settings.Default.LastUpdateCheck = DateTime.Now;
                Properties.Settings.Default.Save();
                //
            }
            catch (System.Net.WebException ex)
            {

            }
        }

        private void RaiseWindowUpdate()
        {
            
        }
        private void RaiseWindowUpdate2()
        {
            WindowUpdate wu = new WindowUpdate();
            wu.Show();
        }
    }
}
