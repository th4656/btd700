using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using BTDTool.ViewModels;

namespace BTDTool.Views;

public class AppToastWindow : Window, IComponentConnector
{
	internal Button Button3;

	internal Button Button2;

	internal Button Button1;

	private bool _contentLoaded;

	public AppToastWindow()
	{
		InitializeComponent();
	}

	private void Window_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			DragMove();
		}
	}

	private void Button3_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is AppToastWindowViewModel appToastWindowViewModel)
		{
			appToastWindowViewModel.iResult = 3;
		}
		Close();
	}

	private void Button2_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is AppToastWindowViewModel appToastWindowViewModel)
		{
			appToastWindowViewModel.iResult = 2;
		}
		Close();
	}

	private void Button1_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is AppToastWindowViewModel appToastWindowViewModel)
		{
			appToastWindowViewModel.iResult = 1;
		}
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Sennheiser Dongle Control;V1.0.5;component/views/apptoastwindow.xaml", UriKind.Relative);
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
			((AppToastWindow)target).MouseDown += Window_MouseDown;
			break;
		case 2:
			Button3 = (Button)target;
			Button3.Click += Button3_Click;
			break;
		case 3:
			Button2 = (Button)target;
			Button2.Click += Button2_Click;
			break;
		case 4:
			Button1 = (Button)target;
			Button1.Click += Button1_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
