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

public class AppDfu : UserControl, IComponentConnector
{
	internal TextBlock __ltxt2;

	internal TextBlock ltxt6;

	internal TextBlock __ltxt4;

	internal GifImage WaitSpinner;

	internal Border RepoVersionInfo;

	internal StackPanel RepoVersionPanel;

	internal ListBox RepoVersionList;

	internal StackPanel NotiNoNetwork;

	internal StackPanel NotiUpToDate;

	internal StackPanel NotiVariable;

	internal Button bt3;

	internal TextBlock bt3Text;

	internal Button bt2;

	internal TextBlock bt2Text;

	internal Button bt1;

	internal TextBlock bt1Text;

	private bool _contentLoaded;

	public AppDfu()
	{
		InitializeComponent();
	}

	private void OnDfuPageBtClick(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.UI_UserResp((_EVENT)((Button)sender).Tag);
		}
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.SetAppSelectionMenuPill(bVisible: true, mainAppWindowViewModel.SelectedDongle != null && mainAppWindowViewModel.SelectedDongle.usbDeviceDetailConfig != null && mainAppWindowViewModel.SelectedDongle.usbDeviceDetailConfig.dtype.Equals(_DEVICETYPE.DEV_BTD700), bEnableUpdate: true, bEnableSettings: true);
			mainAppWindowViewModel.SelectedMenu = 1;
			mainAppWindowViewModel.showRepoVersionList(bVisible: false);
			mainAppWindowViewModel.showSelectedRepoVersionInfo(bVisible: false);
			mainAppWindowViewModel.updateTransferProgress(bVisibility: false, 0);
			mainAppWindowViewModel.SetDFUButton3();
			mainAppWindowViewModel.SetDFUButton2();
			mainAppWindowViewModel.SetDFUButton1(_EVENT.BT_CHECKUPDATE, "btntxt_Check");
		}
	}

	private void RepoVersionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.UI_UserResp(_EVENT.BT_ABS_REPOVERSIONSELECT);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Sennheiser Dongle Control;V1.0.5;component/views/appdfu.xaml", UriKind.Relative);
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
			((AppDfu)target).Loaded += UserControl_Loaded;
			break;
		case 2:
			__ltxt2 = (TextBlock)target;
			break;
		case 3:
			ltxt6 = (TextBlock)target;
			break;
		case 4:
			__ltxt4 = (TextBlock)target;
			break;
		case 5:
			WaitSpinner = (GifImage)target;
			break;
		case 6:
			RepoVersionInfo = (Border)target;
			break;
		case 7:
			RepoVersionPanel = (StackPanel)target;
			break;
		case 8:
			RepoVersionList = (ListBox)target;
			RepoVersionList.MouseDoubleClick += RepoVersionList_MouseDoubleClick;
			break;
		case 9:
			NotiNoNetwork = (StackPanel)target;
			break;
		case 10:
			NotiUpToDate = (StackPanel)target;
			break;
		case 11:
			NotiVariable = (StackPanel)target;
			break;
		case 12:
			bt3 = (Button)target;
			bt3.Click += OnDfuPageBtClick;
			break;
		case 13:
			bt3Text = (TextBlock)target;
			break;
		case 14:
			bt2 = (Button)target;
			bt2.Click += OnDfuPageBtClick;
			break;
		case 15:
			bt2Text = (TextBlock)target;
			break;
		case 16:
			bt1 = (Button)target;
			bt1.Click += OnDfuPageBtClick;
			break;
		case 17:
			bt1Text = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
