using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using BTDTool.ViewModels;

namespace BTDTool.Views;

public class AppLicense : UserControl, IComponentConnector
{
	internal ToggleButton TogglePopupButton;

	internal Popup lbSelectLanguage;

	internal ListBox lbLanguageList;

	internal Button btDecline;

	internal Button btAccept;

	private bool _contentLoaded;

	public AppLicense()
	{
		InitializeComponent();
	}

	private void btDecline_Click(object sender, RoutedEventArgs e)
	{
		(base.DataContext as MainAppWindowViewModel)?.UI_UserResp(_EVENT.BT_EXIT);
	}

	private void btAccept_Click(object sender, RoutedEventArgs e)
	{
		(base.DataContext as MainAppWindowViewModel)?.UI_UserResp(_EVENT.BT_NEXT);
	}

	private void lbLanguageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender != null && (sender as ListBox)?.Parent != null)
		{
			((sender as ListBox).Parent as Popup).IsOpen = false;
		}
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.SetAppSelectionMenuPill();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Sennheiser Dongle Control;V1.0.5;component/views/applicense.xaml", UriKind.Relative);
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
			((AppLicense)target).Loaded += UserControl_Loaded;
			break;
		case 2:
			TogglePopupButton = (ToggleButton)target;
			break;
		case 3:
			lbSelectLanguage = (Popup)target;
			break;
		case 4:
			lbLanguageList = (ListBox)target;
			lbLanguageList.SelectionChanged += lbLanguageList_SelectionChanged;
			break;
		case 5:
			btDecline = (Button)target;
			btDecline.Click += btDecline_Click;
			break;
		case 6:
			btAccept = (Button)target;
			btAccept.Click += btAccept_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
