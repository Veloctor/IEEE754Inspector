using System.Windows.Controls;

namespace IEEE754Calculator
{
    public static class WindowHelpers
    {
        public static void RemoveInputChange(TextBox textBox, TextChangedEventArgs e)
        {
            foreach (TextChange change in e.Changes)
            {
                if (change.AddedLength <= 0)
                {
                    continue;
                }
                int offset = change.Offset;
                textBox.Select(offset, 0);
                textBox.Text = textBox.Text.Remove(offset, change.AddedLength);
            }
        }
    }
}