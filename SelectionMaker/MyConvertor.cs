using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Xml.XPath;
using System.Xml;
using System.IO;

namespace SelectionMaker
{
    class VolumeConvertor:IValueConverter
    {
        #region IValueConverter Members

        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _volume = Convert.ToDouble(value);
            return _volume * 100;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _volume = Convert.ToDouble(value);
            return _volume / 100;
        }

        #endregion
    }

    class PositionConverter:IValueConverter
    {

        #region IValueConverter Members

        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double positionSecond = (double)value;
            TimeSpan position = TimeSpan.FromSeconds(positionSecond);
            return position;
        }

        #endregion
    }

    class ConvertVersion : IValueConverter
    {
        #region IValueConverter Members

        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int _version_xml = Convert.ToInt32(value);
            int major = _version_xml / 10;
            int minor = _version_xml % 10;

            return string.Format("{0}.{1}", major, minor);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    class ConvertPathToImage:IValueConverter
    {

        #region IValueConverter Members

        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string FilePath = (string)value;
            return GetFileIcon.GetIcon(FilePath);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    class ConvertToFileName:IValueConverter
    {

        #region IValueConverter Members

        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string filePath = (string)value;
            FileInfo fi = new FileInfo(filePath);
            return fi.Name;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
