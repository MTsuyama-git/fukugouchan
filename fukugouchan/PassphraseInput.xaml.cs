using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace fukugouchan
{
    /// <summary>
    /// Interaction logic for PassphraseInput.xaml
    /// </summary>
    public partial class PassphraseInput : Window
    {
        public string Text
        {
            get
            {
                return _Text;
            }
        }

        private string _Text
        {
            get; set;
        }
        public PassphraseInput()
        {
            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        public void OnCancel(object sender, RoutedEventArgs e)
        {
            _Text = "";
            DialogResult = false;
        }

        public void OnOK(object sender, RoutedEventArgs e)
        {
            _Text = PasswordBox.Password;
            DialogResult = true;
        }
    }
}
