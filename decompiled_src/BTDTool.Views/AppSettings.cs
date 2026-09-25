using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;
using BTDTool.ViewModels;

namespace BTDTool.Views;

public class AppSettings : UserControl, IComponentConnector
{
	internal Button ButtonConnect;

	internal Button ButtonDisconnect;

	internal Button ButtonResetDongle;

	internal TextBlock AppVersionLabel;

	internal ToggleButton TogglePopupButton;

	internal Popup lbSelectLanguage;

	internal ListBox lbLanguageList;

	internal Button ButtonContactSupport;

	private bool _contentLoaded;

	public T? FindParent<T>(DependencyObject child) where T : DependencyObject
	{
		DependencyObject parent = VisualTreeHelper.GetParent(child);
		if (parent == null)
		{
			return null;
		}
		if (!(parent is T result))
		{
			return FindParent<T>(parent);
		}
		return result;
	}

	public AppSettings()
	{
		InitializeComponent();
	}

	private void lbLanguageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender != null && (sender as ListBox)?.Parent != null)
		{
			((sender as ListBox).Parent as Popup).IsOpen = false;
		}
	}

	private void ButtonResetDongle_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel && mainAppWindowViewModel.modalToastBox(mainAppWindowViewModel.SelectedLanguage.warntxt_factoryreset, mainAppWindowViewModel.SelectedLanguage.warntxt_factoryreset_sub, mainAppWindowViewModel.SelectedLanguage.btntxt_resetdongle, mainAppWindowViewModel.SelectedLanguage.btntxt_Cancel) == 1)
		{
			mainAppWindowViewModel.UI_UserResp(_EVENT.BT_ABS_BTD700FACTORYRESET);
		}
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		MainAppWindow mainAppWindow = FindParent<MainAppWindow>(this);
		if (mainAppWindow != null)
		{
			AppVersionLabel.Text = mainAppWindow.AppVersion;
		}
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.SelectedMenu = 2;
		}
	}

	private void ButtonConnect_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.UI_UserResp(_EVENT.BT_ABS_BTD700CONNECT);
		}
	}

	private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel && mainAppWindowViewModel.modalToastBox(mainAppWindowViewModel.SelectedLanguage.warntxt_disconnect, mainAppWindowViewModel.SelectedLanguage.warntxt_disconnect_sub, mainAppWindowViewModel.SelectedLanguage.menutxt_disconnect, mainAppWindowViewModel.SelectedLanguage.btntxt_Cancel) == 1)
		{
			mainAppWindowViewModel.UI_UserResp(_EVENT.BT_ABS_BTD700DISCONNECT);
		}
	}

	private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
	{
		Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
		e.Handled = true;
	}

	private void ButtonContactSupport_Click(object sender, RoutedEventArgs e)
	{
		string text = "https://www.sennheiser-hearing.com/contact/";
		Process.Start(new ProcessStartInfo("cmd", "/c start " + text)
		{
			CreateNoWindow = true
		});
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Sennheiser Dongle Control;V1.0.5;component/views/appsettings.xaml", UriKind.Relative);
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
			((AppSettings)target).Loaded += UserControl_Loaded;
			break;
		case 2:
			ButtonConnect = (Button)target;
			ButtonConnect.Click += ButtonConnect_Click;
			break;
		case 3:
			ButtonDisconnect = (Button)target;
			ButtonDisconnect.Click += ButtonDisconnect_Click;
			break;
		case 4:
			ButtonResetDongle = (Button)target;
			ButtonResetDongle.Click += ButtonResetDongle_Click;
			break;
		case 5:
			AppVersionLabel = (TextBlock)target;
			break;
		case 6:
			TogglePopupButton = (ToggleButton)target;
			break;
		case 7:
			lbSelectLanguage = (Popup)target;
			break;
		case 8:
			lbLanguageList = (ListBox)target;
			lbLanguageList.SelectionChanged += lbLanguageList_SelectionChanged;
			break;
		case 9:
			ButtonContactSupport = (Button)target;
			ButtonContactSupport.Click += ButtonContactSupport_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
