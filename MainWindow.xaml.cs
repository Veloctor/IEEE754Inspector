using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
            ShowMsg("预计未来加入基本初等函数计算\\双精度等");
        }

        void RefreshDisplay(int bits)
        {
            (int sign, int expo, int mantissa)  = MathTool.SplitBinary32Bits(bits);
            RealValueBox.Text = MathTool.AsFloat(bits).ToString("G9");
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

        void ShowMsg(string msg) => MessagesBox.Text = msg;

        void BitBoxesKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string txt = SignBitBox.Text.TrimStart('0');
                ParseResult result = MathTool.TryParseBin(txt, out int signBit, 1);
                if (result != ParseResult.成功)
                {
                    ShowMsg($"{result}.符号位应该是二进制:\n{txt}");
                    return;
                }
                txt = ExponentBitBox.Text.TrimStart('0');
                result = MathTool.TryParseBin(txt, out int exponentBits, 8);
                if (result != ParseResult.成功)
                {
                    ShowMsg($"{result}.指数由8位二进制组成:\n{txt}");
                    return;
                }
                txt = MantissaBitBox.Text.TrimStart('0');
                result = MathTool.TryParseBin(txt, out int mantissaBits, 23);
                if (result != ParseResult.成功)
                {
                    ShowMsg($"{result}.尾数由23位二进制组成:\n{txt}");
                    return;
                }
                int bits = MathTool.SetupBinary32Bits(signBit, exponentBits, mantissaBits);
                RefreshDisplay(bits);
            }
        }

        void RealValueBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string txt = RealValueBox.Text.TrimStart('0');
                if (float.TryParse(txt, out float result))
                    RefreshDisplay(MathTool.AsInt(result));
                else
                    ShowMsg($"\"{txt}\"\n不能转换为单精度浮点.");
            }
        }
    }
}
