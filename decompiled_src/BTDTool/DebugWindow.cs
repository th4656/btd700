using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace BTDTool;

public class DebugWindow : Window, IComponentConnector
{
	internal ListBox Status;

	internal Button BtnClearStatus;

	internal Button BtnSaveStatus;

	internal ListBox Log;

	internal Button BtnClear;

	internal Button BtnPause;

	internal Button BtnPlay;

	internal TextBlock LoggingState;

	internal TextBox tbPollDuration;

	internal Button BtnSaveLog;

	private bool _contentLoaded;

	public DebugWindow()
	{
		InitializeComponent();
	}

	private void BtnSaveStatus_Click(object sender, RoutedEventArgs e)
	{
	}

	private void BtnSaveLog_Click(object sender, RoutedEventArgs e)
	{
	}

	private void BtnPause_Click(object sender, RoutedEventArgs e)
	{
	}

	private void BtnPlay_Click(object sender, RoutedEventArgs e)
	{
	}

	private void Log_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
	}

	private void Status_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
	}

	private void BtnClear_Click(object sender, RoutedEventArgs e)
	{
	}

	private void BtnClearStatus_Click(object sender, RoutedEventArgs e)
	{
	}

	private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Sennheiser Dongle Control;V1.0.5;component/debugwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Status = (ListBox)target;
			Status.PreviewMouseRightButtonUp += Status_PreviewMouseRightButtonUp;
			break;
		case 2:
			BtnClearStatus = (Button)target;
			BtnClearStatus.Click += BtnClearStatus_Click;
			break;
		case 3:
			BtnSaveStatus = (Button)target;
			BtnSaveStatus.Click += BtnSaveStatus_Click;
			break;
		case 4:
			Log = (ListBox)target;
			Log.PreviewMouseRightButtonUp += Log_PreviewMouseRightButtonUp;
			break;
		case 5:
			BtnClear = (Button)target;
			BtnClear.Click += BtnClear_Click;
			break;
		case 6:
			BtnPause = (Button)target;
			BtnPause.Click += BtnPause_Click;
			break;
		case 7:
			BtnPlay = (Button)target;
			BtnPlay.Click += BtnPlay_Click;
			break;
		case 8:
			LoggingState = (TextBlock)target;
			break;
		case 9:
			tbPollDuration = (TextBox)target;
			tbPollDuration.PreviewKeyDown += TextBox_PreviewKeyDown;
			break;
		case 10:
			BtnSaveLog = (Button)target;
			BtnSaveLog.Click += BtnSaveLog_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
