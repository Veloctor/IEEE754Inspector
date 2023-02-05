using System.Windows;
using System.Windows.Input;

namespace IEEE754Calculator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            RefreshDisplay(0);
            ShowMsg("输入框内输入实数/位后按Enter.\n预计未来加入基本初等函数计算\\双精度等");
        }

        void RefreshDisplay(float val)
        {
            (int sign, int expo, int mantissa) = MathTool.SplitBinary32(val);
            RealValueBox.Text = val.ToString("G10");
            SignBitBox.Text = MathTool.ToBinString(sign, 1);
            ExponentBitBox.Text = MathTool.ToBinString(expo, 8);
            MantissaBitBox.Text = MathTool.ToBinString(mantissa, 23);
            //refresh details
            float mantVal = MathTool.SetupBinary32(0, 127, mantissa);
            if (expo == 0) mantVal--;
            MantissaValBox.Text = mantVal.ToString("G7");
            ExponentValBox.Text = (expo - 127).ToString();
            SignValBox.Text = sign == 0 ? "+" : "-";
            bool isDenormal = expo == 0 && mantissa != 0;
            IsNormalLabel.Content = isDenormal ? "是" : "否";
            int bits = MathTool.AsInt(val);
            ShowMsg($"0x{bits:X}\n0b{MathTool.ToBinString(bits, 32)}");
        }

        void ShowMsg(string msg)
        {
            MsgBox.Text = msg;
        }

        void BitBoxesKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && TryParseBits(out int sign, out int expo, out int mantissa))
            {
                RefreshDisplay(MathTool.SetupBinary32(sign, expo, mantissa));
            }
        }

        void RealValueBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (float.TryParse(RealValueBox.Text, out float result))
                    RefreshDisplay(result);
                else
                    ShowMsg($"\"{RealValueBox.Text}\"\n不能转换为单精度浮点.");
            }
        }

        private void IncrementButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseBits(out int sign, out int expo, out int mantissa))
            {
                ShowMsg($"\"{RealValueBox.Text}\"\n不能转换为单精度浮点.");
                return;
            }
            float before = MathTool.SetupBinary32(sign, expo, mantissa);
            float after = MathTool.FP32BitIncrement(before);
            RefreshDisplay(after);
            ShowMsg($"变化量:{after - before:G10}");
        }

        private void DecrementButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseBits(out int sign, out int expo, out int mantissa))
            {
                ShowMsg($"\"{RealValueBox.Text}\"\n不能转换为单精度浮点.");
                return;
            }
            float before = MathTool.SetupBinary32(sign, expo, mantissa);
            float after = MathTool.FP32BitDecrement(before);
            RefreshDisplay(after);
            ShowMsg($"变化量:{after - before:G10}");
        }

        bool TryParseBits(out int sign, out int expo, out int mantissa)
        {
            expo = mantissa = 0;
            string txt = SignBitBox.Text.TrimStart('0');
            if (!MathTool.TryParseBin(txt, 1, out sign))
            {
                ShowMsg($"符号位输入\"{txt}\"含非法字符'{(char)sign}'.\n应该输入0或1.");
                return false;
            }
            txt = ExponentBitBox.Text.TrimStart('0');
            if (!MathTool.TryParseBin(txt, 8, out expo))
            {
                ShowMsg($"指数输入\"{txt}\"含非法字符'{(char)expo}'.\n应该输入0或1.");
                return false;
            }
            txt = MantissaBitBox.Text.TrimStart('0');
            if(!MathTool.TryParseBin(txt, 23, out mantissa))
            {
                ShowMsg($"尾数输入\"{txt}\"含非法字符'{(char)mantissa}'.\n应该输入0或1.");
                return false;
            }
            return true;
        }
    }
}