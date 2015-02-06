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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            fractalTerrainApp.generateNewScene();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(fractalTerrainApp == null)
            {
                return;
            }
            fractalTerrainApp.terrainHasWater = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            fractalTerrainApp.terrainHasWater = false;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }

            Slider slider = (Slider)sender;

            double value = slider.Value;

            value = value/4.9d;

            fractalTerrainApp.terrainRoughness = (float)value;
        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }

            Slider slider = (Slider)sender;

            double value = slider.Value;

            value = value * 1.0d;

            fractalTerrainApp.terrainFlatness = (float)value;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }

            TextBox box = (TextBox)sender;
            
            double value;

            bool success = Double.TryParse(box.Text,out value);

            if(!success)
            {
                return;
            }

            int result = (int)value;

            fractalTerrainApp.lodLevel = result;
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }

            TextBox box = (TextBox)sender;

            double value;

            bool success = Double.TryParse(box.Text, out value);

            if (!success)
            {
                return;
            }

            float result = (float)value;

            fractalTerrainApp.terrainScale = result;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }

            var comboBox = sender as ComboBox;

            int value = comboBox.SelectedIndex;

            fractalTerrainApp.colorIndex = value;
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> data = new List<string>();
            data.Add("Landscape");
            data.Add("Greyscale");
            data.Add("Rainbow");

            var comboBox = sender as ComboBox;

            comboBox.ItemsSource = data;

            comboBox.SelectedIndex = 0;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }
            fractalTerrainApp.centerScene();
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }
            fractalTerrainApp.rasterizeWireframe(true);
        }

        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }
            fractalTerrainApp.rasterizeWireframe(false);
        }

        private void CheckBox_Checked_2(object sender, RoutedEventArgs e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }
            fractalTerrainApp.enableShading(true);
        }

        private void CheckBox_Unchecked_2(object sender, RoutedEventArgs e)
        {
            if (fractalTerrainApp == null)
            {
                return;
            }
            fractalTerrainApp.enableShading(false);
        }


    }
}
