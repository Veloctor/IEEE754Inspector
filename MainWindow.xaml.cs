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
        }

        void RefreshDisplay(int bits)
        {
            (int sign, int expo, int mantissa) = MathTool.SplitBinary32Bits(bits);
            RealValueBox.Text = MathTool.AsFloat(bits).ToString("G10");
            SignBitBox.Text = MathTool.ToBinString(sign, 1);
            ExponentBitBox.Text = MathTool.ToBinString(expo, 8);
            MantissaBitBox.Text = MathTool.ToBinString(mantissa, 23);
            //refresh details
            int mantValBits = MathTool.SetupBinary32Bits(0, 127, mantissa);
            float mantVal = MathTool.AsFloat(mantValBits);
            if (expo == 0) mantVal--;
            MantissaValBox.Text = mantVal.ToString();
            ExponentValBox.Text = (expo - 127).ToString();
            SignValBox.Text = sign == 0 ? "+" : "-";
            bool isDenormal = expo == 0 && mantissa != 0;
            IsNormalLabel.Content = isDenormal ? "是" : "否";
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
                int bits = MathTool.SetupBinary32Bits(sign, expo, mantissa);
                RefreshDisplay(bits);
            }
        }

        void RealValueBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (float.TryParse(RealValueBox.Text, out float result))
                    RefreshDisplay(MathTool.AsInt(result));
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
            int bits = MathTool.SetupBinary32Bits(sign, expo, mantissa);
            float before = MathTool.AsFloat(bits);
            bits = MathTool.FP32BitIncrement(bits);
            float after = MathTool.AsFloat(bits);
            RefreshDisplay(bits);
            ShowMsg("变化量:" + (after - before));
        }

        private void DecrementButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseBits(out int sign, out int expo, out int mantissa))
            {
                ShowMsg($"\"{RealValueBox.Text}\"\n不能转换为单精度浮点.");
                return;
            }
            int bits = MathTool.SetupBinary32Bits(sign, expo, mantissa);
            float before = MathTool.AsFloat(bits);
            bits = MathTool.FP32BitDecrement(bits);
            float after = MathTool.AsFloat(bits);
            RefreshDisplay(bits);
            ShowMsg("变化量:" + (after - before));
        }

        bool TryParseBits(out int sign, out int expo, out int mantissa)
        {
            expo = mantissa = 0;
            string txt = SignBitBox.Text.TrimStart('0');
            ParseResult result = MathTool.TryParseBin(txt, 1, out sign);
            if (result != ParseResult.成功)
            {
                ShowMsg($"{txt}:{result}.\n符号位应该是1bit二进制.");
                return false;
            }
            txt = ExponentBitBox.Text.TrimStart('0');
            result = MathTool.TryParseBin(txt, 8, out expo);
            if (result != ParseResult.成功)
            {
                ShowMsg($"{txt}:{result}.\n指数由8位二进制组成.");
                return false;
            }
            txt = MantissaBitBox.Text.TrimStart('0');
            result = MathTool.TryParseBin(txt, 23, out mantissa);
            if (result != ParseResult.成功)
            {
                ShowMsg($"{txt}:{result}.\n尾数由23位二进制组成.");
                return false;
            }
            return true;
        }
    }
}