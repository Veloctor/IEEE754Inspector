using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kerwis.DDouble;
using static IEEE754Inspector.MathTool;

namespace IEEE754Inspector;

public partial class MainWindow : Window
{
	object CurrentValue => FloatModeTabControl.SelectedIndex switch {
		0 => float.Parse(RealValueBox.Text) as object,
		1 => double.Parse(RealValueBox.Text),
		_ => throw new InvalidOperationException("不正确的浮点类型选项卡编号")
	};

	public MainWindow()
	{
		InitializeComponent();
		FloatModeTabControl.SelectionChanged += FloatModeTabControl_SelectionChanged;
		
		Title = $"IEE754检视器 v{typeof(MainWindow).Assembly.GetName().Version.ToString(3)} by 矢速";
		ShowMsg("输入框内输入实数/位后按Enter.\n预计未来加入基本初等函数计算等功能");
	}

	private void FloatModeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		MantissaBitBox.MaxLength = CurrentValue is float ? FP32MantBits : FP64MantBits;
		ExponentBitBox.MaxLength = CurrentValue is float ? FP32ExpoBits : FP64ExpoBits;
		RefreshDisplayFromValBox();
	}

	unsafe void RefreshDisplayFromValBox()
	{
		try {
			switch (CurrentValue) {
				case float f:
					RefreshDisplay32(f); break;
				case double d:
					RefreshDisplay64(d); break;
			}
		}
		catch (Exception ex) {
			ShowMsg(ex.Message);
		}
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
		ddouble inc = (ddouble)Math.BitIncrement(val) - val;
		ddouble dec = (ddouble)Math.BitDecrement(val) - val;
		BitIncrementBox.Text = inc.ToString();
		BitDecrementBox.Text = dec.ToString();
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
		ddouble inc = (ddouble)MathF.BitIncrement(val) - val;
		ddouble dec = (ddouble)MathF.BitDecrement(val) - val;
		BitIncrementBox.Text = inc.ToString();
		BitDecrementBox.Text = dec.ToString();
	}

	void BitBoxesKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter) return;

		if (CurrentValue is float && TryParseBits(out int sign32, out int expo32, out int mantissa32))
			RefreshDisplay32(SetupFPBinary<int, float>(sign32, expo32, mantissa32));
		else if (CurrentValue is double && TryParseBits(out long sign64, out long expo64, out long mantissa64))
			RefreshDisplay64(SetupFPBinary<long, double>(sign64, expo64, mantissa64));
	}

	private void IncrementButton_Click(object sender, RoutedEventArgs e) => DoValueChange(MathF.BitIncrement, Math.BitIncrement);

	private void DecrementButton_Click(object sender, RoutedEventArgs e) => DoValueChange(MathF.BitDecrement, Math.BitDecrement);

	void DoValueChange(Func<float, float> floatChangeFunc, Func<double, double> doubleChangeFunc)
	{
		if (CurrentValue is float && TryParseBits(out int signf, out int expof, out int mantissaf)) {
			float before = SetupFPBinary<int, float>(signf, expof, mantissaf);
			float after = floatChangeFunc(before);
			RefreshDisplay32(after);
		}
		else if (CurrentValue is double && TryParseBits(out long signd, out long expod, out long mantissad)) {
			double before = SetupFPBinary<long, double>(signd, expod, mantissad);
			double after = doubleChangeFunc(before);
			RefreshDisplay64(after);
		}
		else ShowMsg($"\"{RealValueBox.Text}\"\n不能转换为单精度浮点.");
	}

	private void RealValueBox_KeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter) return;
		RefreshDisplayFromValBox();
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
}
