using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kerwis.DDouble;
using static IEEE754Inspector.MathTool;

namespace IEEE754Inspector
{
	public enum FloatMode { Single, Double } //为了保证FloatModeTab Index与枚举值同步, 不要手动指定值

	public partial class MainWindow : Window
	{
		FloatMode CurrentMode => (FloatMode)FloatModeTabControl.SelectedIndex;

		public MainWindow()
		{
			InitializeComponent();
			foreach (var m in typeof(FloatMode).GetEnumNames())
				FloatModeTabControl.Items.Add(new TabItem() { Header = m });
			Title = $"IEE754检视器 v{typeof(MainWindow).Assembly.GetName().Version.ToString(3)} by 矢速";
			ShowMsg("输入框内输入实数/位后按Enter.\n预计未来加入基本初等函数计算等功能");
		}

		private void FloatModeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			bool isSingle = CurrentMode == FloatMode.Single;
			MantissaBitBox.MaxLength = isSingle ? FP32MantBits : FP64MantBits;
			ExponentBitBox.MaxLength = isSingle ? FP32ExpoBits : FP64ExpoBits;

			if (CurrentMode == FloatMode.Single) {
				var x = float.Parse(RealValueBox.Text);
				RefreshDisplay(x);
			}
			else if (CurrentMode == FloatMode.Double) {
				var x = double.Parse(RealValueBox.Text);
				RefreshDisplay(x);
			}
		}

		unsafe void RefreshDisplay<T>(T binary) where T : unmanaged
		{
			if (sizeof(T) == sizeof(float))
				RefreshDisplay32(AsFP32(binary));
			else if (sizeof(T) == sizeof(double))
				RefreshDisplay64(AsFP64(binary));
		}

		void RefreshDisplay64(double val)
		{
			(long sign, long expo, long mantissa) = SplitFPBinary<double, long>(val);
			RealValueBox.Text = val.ToString("G17");
			SignBitBox.Text = ToBinString(sign, 1);
			ExponentBitBox.Text = ToBinString(expo, 11);
			MantissaBitBox.Text = ToBinString(mantissa, 52);
			//refresh details
			bool isDenormal = expo == 0 && mantissa != 0;
			double mantVal = SetupFPBinary<long, double>(0, 1023, mantissa);
			if (expo == 0) {
				expo++;
				mantVal--;
			}
			MantissaValBox.Text = mantVal.ToString("G17");
			ExponentValBox.Text = (expo - 1023).ToString();
			SignValBox.Text = sign == 0 ? "+" : "-";
			IsNormalLabel.Content = isDenormal ? "是" : "否";
			long bits = AsInt64(val);
			ShowMsg($"十六进制: {bits:X}\n二进制: {ToBinString(bits, 64)}");
			var inc = (ddouble)Math.BitIncrement(val) - val;
			var dec = (ddouble)Math.BitDecrement(val) - val;
			BitIncrementBox.Text = inc.ToString(30, fillZero: false);
			BitDecrementBox.Text = dec.ToString(30, fillZero: false);
		}

		void RefreshDisplay32(float val)
		{
			(int sign, int expo, int mantissa) = SplitFPBinary<float, int>(val);
			RealValueBox.Text = val.ToString("G8");
			SignBitBox.Text = ToBinString(sign, 1);
			ExponentBitBox.Text = ToBinString(expo, 8);
			MantissaBitBox.Text = ToBinString(mantissa, 23);
			//refresh details
			bool isDenormal = expo == 0 && mantissa != 0;
			float mantVal = SetupFPBinary<int, float>(0, 127, mantissa);
			if (expo == 0) {
				expo++;
				mantVal--;
			}
			MantissaValBox.Text = mantVal.ToString("G8");
			ExponentValBox.Text = (expo - 127).ToString();
			SignValBox.Text = sign == 0 ? "+" : "-";
			IsNormalLabel.Content = isDenormal ? "是" : "否";
			int bits = AsInt32(val);
			ShowMsg($"十六进制: {bits:X}\n二进制: {ToBinString(bits, 32)}");
			var inc = (ddouble)MathF.BitIncrement(val) - val;
			var dec = (ddouble)MathF.BitDecrement(val) - val;
			BitIncrementBox.Text = inc.ToString(30, fillZero: false);
			BitDecrementBox.Text = dec.ToString(30, fillZero: false);
		}

		void BitBoxesKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;

			if (CurrentMode == FloatMode.Single && TryParseBits(out int sign32, out int expo32, out int mantissa32))
				RefreshDisplay(SetupFPBinary<int, float>(sign32, expo32, mantissa32));
			else if (CurrentMode == FloatMode.Double && TryParseBits(out long sign64, out long expo64, out long mantissa64))
				RefreshDisplay(SetupFPBinary<long, double>(sign64, expo64, mantissa64));
		}

		private void IncrementButton_Click(object sender, RoutedEventArgs e) => DoValueChange(MathF.BitIncrement, Math.BitIncrement);

		private void DecrementButton_Click(object sender, RoutedEventArgs e) => DoValueChange(MathF.BitDecrement, Math.BitDecrement);

		void DoValueChange(Func<float, float> floatChangeFunc, Func<double, double> doubleChangeFunc)
		{
			if (CurrentMode == FloatMode.Single && TryParseBits(out int signf, out int expof, out int mantissaf)) {
				float before = SetupFPBinary<int, float>(signf, expof, mantissaf);
				float after = floatChangeFunc(before);
				RefreshDisplay(after);
			}
			else if (CurrentMode == FloatMode.Double && TryParseBits(out long signd, out long expod, out long mantissad)) {
				double before = SetupFPBinary<long, double>(signd, expod, mantissad);
				double after = doubleChangeFunc(before);
				RefreshDisplay(after);
			}
			else ShowMsg($"\"{RealValueBox.Text}\"\n不能转换为单精度浮点.");
		}

		bool TryParseBits<Tbin>(out Tbin sign, out Tbin expo, out Tbin mantissa) where Tbin : unmanaged
		{
			expo = mantissa = default;
			char c;
			if ((c = TryParseBin(SignBitBox.Text, out sign)) != '\0') {
				ShowMsg($"符号位输入\"{SignBitBox.Text}\"含非法字符'{c}'.\n应该输入0或1.");
			}
			else if ((c = TryParseBin(ExponentBitBox.Text, out expo)) != '\0') {
				ShowMsg($"指数输入\"{ExponentBitBox.Text}\"含非法字符'{c}'.\n应该输入0或1.");
			}
			else if ((c = TryParseBin(MantissaBitBox.Text, out mantissa)) != '\0') {
				ShowMsg($"尾数输入\"{MantissaBitBox.Text}\"含非法字符'{c}'.\n应该输入0或1.");
			}
			else return true;
			return false;
		}

		private void ShowMsg<T>(in T msg)
		{
			DebugTool.LogMsg(msg, 2);
			if (msg is string str)
				MsgBox.Text = str;
			else
				MsgBox.Text = msg.ToString();
		}

		private void RealValueBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;

			if (CurrentMode == FloatMode.Single && float.TryParse(RealValueBox.Text, out float resultf))
				RefreshDisplay(resultf);
			else if (CurrentMode == FloatMode.Double && double.TryParse(RealValueBox.Text, out double resultd))
				RefreshDisplay(resultd);
			else
				ShowMsg($"\"{RealValueBox.Text}\"不能转换为{CurrentMode}浮点数.");
		}
	}
}
