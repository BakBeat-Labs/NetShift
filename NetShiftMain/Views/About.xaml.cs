using System.Diagnostics;
using System.Windows;
using System.Windows.Input;


namespace NetShift.Views
{
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

        }

        private void GitHubLink_Click(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Mainframe79/NetShift",
                UseShellExecute = true
            });
        }
    }
}
