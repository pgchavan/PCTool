using System;
using System.Diagnostics;
using System.Windows;

namespace HPDTool.UI
{
    public partial class FeedbackWindow : Window
    {
        private const string FeedbackEmail = "prasad.chavan@hpdglobal.com";

        public FeedbackWindow()
        {
            InitializeComponent();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string feedback = FeedbackBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(feedback))
            {
                MessageBox.Show("Please enter some feedback before sending.",
                    "Empty Feedback", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string subject = Uri.EscapeDataString("HPDTool Feedback");
            string body = Uri.EscapeDataString(feedback);

            string mailto = $"mailto:{FeedbackEmail}?subject={subject}&body={body}";

            try
            {
                Process.Start(mailto);
                MessageBox.Show("Thank you! Your email client will open now.",
                    "Feedback", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open email client.\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}