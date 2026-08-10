using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace HPDTool.UI
{
    public partial class TxtWin : Window
    {
        private Document _doc;
        private List<TextNote> _textNotes;

        public TxtWin(Document doc, List<TextNote> notes)
        {
            InitializeComponent();
            _doc = doc;
            _textNotes = notes;
        }

        private void ModifyTextNotes(System.Func<string, string> transformFunc)
        {
            using (Transaction t = new Transaction(_doc, "Change Text Case"))
            {
                t.Start();
                foreach (var note in _textNotes)
                {
                    string oldText = note.Text;
                    string newText = transformFunc(oldText);
                    note.Text = newText;
                }
                t.Commit();
            }
            MessageBox.Show("Text updated.");

        }

        private void UpperCase_Click(object sender, RoutedEventArgs e)
        {
            ModifyTextNotes(text => text.ToUpper());
        }

        private void LowerCase_Click(object sender, RoutedEventArgs e)
        {
            ModifyTextNotes(text => text.ToLower());
        }

        private void TitleCase_Click(object sender, RoutedEventArgs e)
        {
            ModifyTextNotes(ToTitleCase);
        }

        private void SentenceCase_Click(object sender, RoutedEventArgs e)
        {
            ModifyTextNotes(ToSentenceCase);
        }

        private string ToTitleCase(string input)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
        }

        private string ToSentenceCase(string input)
        {
            input = input.ToLower();

            // Capitalize the first letter of the string
            if (input.Length == 0) return input;

            // Capitalize first non-whitespace letter
            return Regex.Replace(input, @"(^\s*\w)", m => m.Value.ToUpper());
        }

    }
}
