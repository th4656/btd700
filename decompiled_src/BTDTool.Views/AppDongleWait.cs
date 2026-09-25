using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using BTDTool.ViewModels;

namespace BTDTool.Views;

public class AppDongleWait : UserControl, IComponentConnector
{
	internal GifImage WaitSpinner;

	private bool _contentLoaded;

	public AppDongleWait()
	{
		InitializeComponent();
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.SelectedMenu = 0;
			WaitSpinner.StartAnimation();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Sennheiser Dongle Control;V1.0.5;component/views/appdonglewait.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((AppDongleWait)target).Loaded += UserControl_Loaded;
			break;
		case 2:
			WaitSpinner = (GifImage)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
