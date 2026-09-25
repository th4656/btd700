using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using BTDTool.ViewModels;

namespace BTDTool.Views;

public class MainAppWindow : Window, IComponentConnector
{
	private const int GWL_STYLE = -16;

	private const int WS_SYSMENU = 524288;

	private ulong ulVersion;

	public DateTime dtBuildTimestamp;

	internal Grid WelcomeGrid;

	internal Button SysBtnMinimizeWelcome;

	internal Button SysBtnCloseWelcome;

	internal GifImage AnimatedLogo;

	internal StackPanel TitlePanel;

	internal TextBlock ApplicationVersion;

	internal WrapPanel ButtonPanel;

	internal ToggleButton TogglePopupButton;

	internal Popup lbSelectLanguage;

	internal ListBox lbLanguageList;

	internal Button btStart;

	internal Grid ApplicationGrid;

	internal Grid MainGrid;

	internal Button SysBtnMinimize;

	internal Button SysBtnClose;

	internal Grid MainHeader;

	internal TextBlock MenuItemDashboard;

	internal TextBlock MenuItemUpdate;

	internal TextBlock MenuItemSettings;

	internal ContentPresenter contentPresenter;

	internal Border BlurSplash;

	private bool _contentLoaded;

	public string AppVersion => ((ulVersion >> 16) & 0xFFFF) + "." + ((ulVersion >> 8) & 0xFF) + "." + (ulVersion & 0xFF);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int GetWindowLong(nint hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

	public string WindowTitle(string t = null)
	{
		return ((t == null) ? "Sennheiser Dongle Control" : t) + " - v" + ((ulVersion >> 16) & 0xFFFF) + "." + ((ulVersion >> 8) & 0xFF) + "." + (ulVersion & 0xFF);
	}

	private static DateTime GetBuildDate(Assembly assembly)
	{
		return assembly.GetCustomAttribute<BuildDateAttribute>()?.DateTime ?? default(DateTime);
	}

	public MainAppWindow(ulong version)
	{
		InitializeComponent();
		ulVersion = version;
		dtBuildTimestamp = GetBuildDate(Assembly.GetExecutingAssembly()).ToLocalTime();
	}

	private void Window_MouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				DragMove();
			}
		}
		catch (Exception)
		{
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		nint handle = new WindowInteropHelper(this).Handle;
		SetWindowLong(handle, -16, GetWindowLong(handle, -16) & -524289);
		Uri uri = new Uri("pack://application:,,,/Assets/sennheiser_animation_whitebg_single.gif", UriKind.RelativeOrAbsolute);
		Application.GetResourceStream(uri);
		MediaPlayer mediaPlayer = new MediaPlayer();
		mediaPlayer.Open(uri);
		mediaPlayer.Play();
		base.Title = WindowTitle();
		ApplicationVersion.Text = AppVersion;
		ApplicationGrid.Visibility = Visibility.Collapsed;
		WelcomeGrid.Visibility = Visibility.Visible;
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
	}

	private void Image_MouseDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void SysBtnMinimize_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void SysBtnClose_Click(object sender, RoutedEventArgs e)
	{
		MainAppWindowViewModel mainAppWindowViewModel = base.DataContext as MainAppWindowViewModel;
		if (mainAppWindowViewModel != null && mainAppWindowViewModel.IsUpdateOngoing())
		{
			mainAppWindowViewModel.UI_UserResp(_EVENT.BT_CANCEL);
			return;
		}
		mainAppWindowViewModel.bExitApplication = true;
		mainAppWindowViewModel.UI_UserResp(_EVENT.BT_EXIT);
	}

	private void MenuItemDashboard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!(base.DataContext is MainAppWindowViewModel mainAppWindowViewModel) || mainAppWindowViewModel.UserInterface == mainAppWindowViewModel.Btd700View)
		{
			return;
		}
		if (mainAppWindowViewModel.SelectedDongle == null)
		{
			mainAppWindowViewModel.UserInterface = mainAppWindowViewModel.DongleWaitView;
		}
		else if (mainAppWindowViewModel.SelectedDongle.usbDeviceDetailConfig != null && mainAppWindowViewModel.SelectedDongle.usbDeviceDetailConfig.dtype.Equals(_DEVICETYPE.DEV_BTD700))
		{
			mainAppWindowViewModel.UI_UserResp(_EVENT.BT_BTD700CANCEL);
			mainAppWindowViewModel.UserInterface = mainAppWindowViewModel.Btd700View;
			if (mainAppWindowViewModel.bBroadcast)
			{
				mainAppWindowViewModel.BroadcastParamUIEnabled = false;
				mainAppWindowViewModel.ApplicationSelectionEnabled = false;
				mainAppWindowViewModel.ModeSetBackoffFor(4);
			}
		}
		else if (mainAppWindowViewModel.SelectedDongle.usbDeviceDetailConfig == null || !mainAppWindowViewModel.SelectedDongle.usbDeviceDetailConfig.dtype.Equals(_DEVICETYPE.DEV_BTD600))
		{
			mainAppWindowViewModel.UserInterface = mainAppWindowViewModel.DongleWaitView;
		}
	}

	private void MenuItemUpdate_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel && mainAppWindowViewModel.UserInterface != mainAppWindowViewModel.DFUView && (mainAppWindowViewModel.UserInterface != mainAppWindowViewModel.Btd700View || !mainAppWindowViewModel.bBroadcast || (!mainAppWindowViewModel.BroadcastQualityChanged && !mainAppWindowViewModel.BroadcastPBPChanged && !mainAppWindowViewModel.BroadcastNameChanged && !mainAppWindowViewModel.BroadcastPasswordChanged) || mainAppWindowViewModel.modalToastBox("", mainAppWindowViewModel.SelectedLanguage.warntxt_discardbcastsettings, mainAppWindowViewModel.SelectedLanguage.btntxt_Confirm, mainAppWindowViewModel.SelectedLanguage.btntxt_Cancel) == 1))
		{
			mainAppWindowViewModel.UserInterface = mainAppWindowViewModel.DFUView;
			mainAppWindowViewModel.DisplayDFUNotificationPanel();
			mainAppWindowViewModel.UI_UserResp(_EVENT.BT_MENUUPDATE);
		}
	}

	private void MenuItemSettings_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel && mainAppWindowViewModel.UserInterface != mainAppWindowViewModel.SettingsView && (mainAppWindowViewModel.UserInterface != mainAppWindowViewModel.Btd700View || !mainAppWindowViewModel.bBroadcast || (!mainAppWindowViewModel.BroadcastQualityChanged && !mainAppWindowViewModel.BroadcastPBPChanged && !mainAppWindowViewModel.BroadcastNameChanged && !mainAppWindowViewModel.BroadcastPasswordChanged) || mainAppWindowViewModel.modalToastBox("", mainAppWindowViewModel.SelectedLanguage.warntxt_discardbcastsettings, mainAppWindowViewModel.SelectedLanguage.btntxt_Confirm, mainAppWindowViewModel.SelectedLanguage.btntxt_Cancel) == 1))
		{
			mainAppWindowViewModel.UserInterface = mainAppWindowViewModel.SettingsView;
			mainAppWindowViewModel.UI_UserResp(_EVENT.BT_MENUSETTINGS);
		}
	}

	private void lbLanguageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender != null && (sender as ListBox)?.Parent != null)
		{
			((sender as ListBox).Parent as Popup).IsOpen = false;
		}
	}

	private void btStart_Click(object sender, RoutedEventArgs e)
	{
		WelcomeGrid.Visibility = Visibility.Collapsed;
		ApplicationGrid.Visibility = Visibility.Visible;
	}

	private void MediaTimeline_Completed(object sender, EventArgs e)
	{
		StartWelcomeScreen();
	}

	private void AnimatedLogo_MediaEnded(object sender, RoutedEventArgs e)
	{
		StartWelcomeScreen();
	}

	private void AnimatedLogo_PreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		StartWelcomeScreen();
	}

	private void StartWelcomeScreen()
	{
		AnimatedLogo.Visibility = Visibility.Collapsed;
		TitlePanel.Visibility = Visibility.Visible;
		ButtonPanel.Visibility = Visibility.Visible;
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.FSM_InitAndStart();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Sennheiser Dongle Control;V1.0.5;component/views/mainappwindow.xaml", UriKind.Relative);
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
			((MainAppWindow)target).MouseDown += Window_MouseDown;
			((MainAppWindow)target).Loaded += Window_Loaded;
			((MainAppWindow)target).Closing += Window_Closing;
			break;
		case 2:
			WelcomeGrid = (Grid)target;
			break;
		case 3:
			SysBtnMinimizeWelcome = (Button)target;
			SysBtnMinimizeWelcome.Click += SysBtnMinimize_Click;
			break;
		case 4:
			SysBtnCloseWelcome = (Button)target;
			SysBtnCloseWelcome.Click += SysBtnClose_Click;
			break;
		case 5:
			AnimatedLogo = (GifImage)target;
			break;
		case 6:
			TitlePanel = (StackPanel)target;
			break;
		case 7:
			ApplicationVersion = (TextBlock)target;
			break;
		case 8:
			ButtonPanel = (WrapPanel)target;
			break;
		case 9:
			TogglePopupButton = (ToggleButton)target;
			break;
		case 10:
			lbSelectLanguage = (Popup)target;
			break;
		case 11:
			lbLanguageList = (ListBox)target;
			lbLanguageList.SelectionChanged += lbLanguageList_SelectionChanged;
			break;
		case 12:
			btStart = (Button)target;
			btStart.Click += btStart_Click;
			break;
		case 13:
			ApplicationGrid = (Grid)target;
			break;
		case 14:
			MainGrid = (Grid)target;
			break;
		case 15:
			((Image)target).MouseDown += Image_MouseDown;
			break;
		case 16:
			SysBtnMinimize = (Button)target;
			SysBtnMinimize.Click += SysBtnMinimize_Click;
			break;
		case 17:
			SysBtnClose = (Button)target;
			SysBtnClose.Click += SysBtnClose_Click;
			break;
		case 18:
			MainHeader = (Grid)target;
			break;
		case 19:
			MenuItemDashboard = (TextBlock)target;
			MenuItemDashboard.PreviewMouseLeftButtonDown += MenuItemDashboard_PreviewMouseLeftButtonDown;
			break;
		case 20:
			MenuItemUpdate = (TextBlock)target;
			MenuItemUpdate.PreviewMouseLeftButtonDown += MenuItemUpdate_PreviewMouseLeftButtonDown;
			break;
		case 21:
			MenuItemSettings = (TextBlock)target;
			MenuItemSettings.PreviewMouseLeftButtonDown += MenuItemSettings_PreviewMouseLeftButtonDown;
			break;
		case 22:
			contentPresenter = (ContentPresenter)target;
			break;
		case 23:
			BlurSplash = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
