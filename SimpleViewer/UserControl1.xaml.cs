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
using System.Xml;

namespace FractalLandscape
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : Window
    {
        private MyApp fractalTerrainApp;

        public UserControl1()
        {
            InitializeComponent();
        }

        public void init(MyApp myApp)
        {
            fractalTerrainApp = myApp;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            fractalTerrainApp.shutdown();
        }


    }
}
