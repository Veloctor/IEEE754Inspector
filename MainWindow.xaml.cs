using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kerwis.DDouble;
using static IEEE754Inspector.MathTool;

namespace IEEE754Inspector;

public partial class MainWindow
{
	bool IsSingleMode => FloatModeTabControl.SelectedIndex == 0;

	/// <summary> 如果十进制字符串表示可以Parse为当前浮点模式的对象则返回对应类型的box对象，否则返回原字符串 </summary>
	object CurrentValue => IsSingleMode
			? float.TryParse(RealValueBox.Text, out float valuef) ? valuef : RealValueBox.Text
			: double.TryParse(RealValueBox.Text, out double valued) ? valued : RealValueBox.Text;

	public MainWindow()
	{
		InitializeComponent();
		FloatModeTabControl.SelectionChanged += FloatModeTabControl_SelectionChanged;

		Title = $"IEE754检视器 v{typeof(MainWindow).Assembly.GetName().Version?.ToString(3)} by 矢速";
		ShowMsg("输入框内输入实数/位后按Enter.\n预计未来加入基本初等函数计算等功能");
	}

	private void FloatModeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		MantissaBitBox.MaxLength = IsSingleMode ? FP32MantBits : FP64MantBits;
		ExponentBitBox.MaxLength = IsSingleMode ? FP32ExpoBits : FP64ExpoBits;
		RefreshDisplayDec();
	}

	void BitBoxesKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter) return;

		if (IsSingleMode && TryParseBits(out int sign32, out int expo32, out int mantissa32))
			RefreshDisplay32(SetupFPBinary<int, float>(sign32, expo32, mantissa32));
		else if (TryParseBits(out long sign64, out long expo64, out long mantissa64))
			RefreshDisplay64(SetupFPBinary<long, double>(sign64, expo64, mantissa64));
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
		try {
			BitIncrementBox.Text = ((ddouble)Math.BitIncrement(val) - val).ToString();
			BitDecrementBox.Text = ((ddouble)Math.BitDecrement(val) - val).ToString();
		}
		catch (Exception) {
			// ignored
		}
		finally {
			BitIncrementBox.Text = (Math.BitIncrement(val) - val).ToString(CultureInfo.InvariantCulture);
			BitDecrementBox.Text = (Math.BitDecrement(val) - val).ToString(CultureInfo.InvariantCulture);
		}
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
		try {
			BitIncrementBox.Text = ((ddouble)MathF.BitIncrement(val) - val).ToString();
			BitDecrementBox.Text = ((ddouble)MathF.BitDecrement(val) - val).ToString();
		}
		catch (Exception) {
			// ignored
		}
		finally {
			BitIncrementBox.Text = ((double)MathF.BitIncrement(val) - val).ToString(CultureInfo.InvariantCulture);
			BitDecrementBox.Text = ((double)MathF.BitDecrement(val) - val).ToString(CultureInfo.InvariantCulture);
		}
	}

	void RefreshDisplayDec(Func<float, float> floatChangeFunc = null, Func<double, double> doubleChangeFunc = null)
	{
		switch (CurrentValue) {
			case float f:
				RefreshDisplay32(floatChangeFunc?.Invoke(f) ?? f); break;
			case double d:
				RefreshDisplay64(doubleChangeFunc?.Invoke(d) ?? d); break;
			case string originalStr:
				ShowMsg($"“{originalStr}”不能解析为当前所选类型浮点数!"); break;
			default:
				ShowMsg($"异常, 意外的类型: {CurrentValue.GetType().Name}, 值: {CurrentValue}."); break;
		}
	}

	private void IncrementButton_Click(object sender, RoutedEventArgs e) => RefreshDisplayDec(MathF.BitIncrement, Math.BitIncrement);
	private void DecrementButton_Click(object sender, RoutedEventArgs e) => RefreshDisplayDec(MathF.BitDecrement, Math.BitDecrement);

	private void RealValueBox_KeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter) return;
		RefreshDisplayDec();
	}

	/// <summary>
	/// 将符号位、指数位、尾数位的字符串表示的二进制解析填充为目标值类型。不足的位数补0
	/// </summary>
	/// <typeparam name="Tbin">输出的值类型</typeparam>
	bool TryParseBits<Tbin>(out Tbin sign, out Tbin expo, out Tbin mantissa) where Tbin : unmanaged
	{
		expo = mantissa = default;
		char c;
		if ((c = TryParseBin(SignBitBox.Text, out sign)) != '\0') {
			ShowMsg($"符号位输入\"{SignBitBox.Text}\"为非法字符'{c}'.\n应该输入0或1.");
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

	private void ShowMsg(object msg)
	{
		DebugTool.LogMsg(msg, 2);
		MsgBox.Text = msg?.ToString() ?? string.Empty;
	}
}
