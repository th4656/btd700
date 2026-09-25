using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using BTDTool.ViewModels;

namespace BTDTool.Views;

public class AppBtd700Features : UserControl, IComponentConnector
{
	internal Button btOneToOne;

	internal CheckBox cbOneToOne;

	internal Button btGaming;

	internal CheckBox cbGaming;

	internal Button btBroadcast;

	internal CheckBox cbBroadcast;

	internal Border rowAudioQualityInfo;

	internal Border rowBroadcastQualitySelect;

	internal ToggleButton BroadcastQualityButton;

	internal TextBlock tbBroadcastQuality;

	internal Popup lbBroadcastQuality;

	internal ListBox lbBroadcastQualityList;

	internal Border rowTransportTypeSelect;

	internal ToggleButton TransportSelectButton;

	internal TextBlock tbSelectedTransport;

	internal Popup lbTransportSelect;

	internal ListBox lbTransportList;

	internal Border rowBroadcastPublicProfile;

	internal CheckBox PBPCheckbox;

	internal Border rowCodecSelect;

	internal ToggleButton CodecSelectButton;

	internal TextBlock tbSelectedCodec;

	internal Popup lbCodecSelect;

	internal ListBox lbCodecList;

	internal Border rowBroadcastName;

	internal TextBox BroadcastNameString;

	internal Border rowBroadcastPassword;

	internal Grid gridBroadcastPassword;

	internal PasswordBox BcastKeyPasswordBox;

	internal Button ButtonShowBcastKey;

	internal TextBox BroadcastEncryptionKey;

	internal Button ButtonHideBcastKey;

	internal Border rowActionButtons;

	internal Button btDiscardBcastSettings;

	internal Button btSaveBcastSettings;

	private bool _contentLoaded;

	public AppBtd700Features()
	{
		InitializeComponent();
		DataObject.AddPastingHandler(BroadcastNameString, OnPasteAllowSpace);
		DataObject.AddPastingHandler(BcastKeyPasswordBox, OnPasteDisallowSpace);
		DataObject.AddPastingHandler(BroadcastEncryptionKey, OnPasteDisallowSpace);
	}

	private void btOneToOne_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel { bOneToOne: false } mainAppWindowViewModel && (!mainAppWindowViewModel.bBroadcast || (!mainAppWindowViewModel.BroadcastQualityChanged && !mainAppWindowViewModel.BroadcastPBPChanged && !mainAppWindowViewModel.BroadcastNameChanged && !mainAppWindowViewModel.BroadcastPasswordChanged) || mainAppWindowViewModel.modalToastBox("", mainAppWindowViewModel.SelectedLanguage.warntxt_discardbcastsettings, mainAppWindowViewModel.SelectedLanguage.btntxt_Confirm, mainAppWindowViewModel.SelectedLanguage.btntxt_Cancel) == 1))
		{
			mainAppWindowViewModel.bOneToOne = true;
			mainAppWindowViewModel.bGaming = false;
			mainAppWindowViewModel.bBroadcast = false;
			mainAppWindowViewModel.SelectedApplication = 0;
			mainAppWindowViewModel.PostEvent(_EVENT.BT_ABS_BTD700SETAUDIOANDTRANSPORT, new byte[2]
			{
				(byte)mainAppWindowViewModel.SelectedApplication,
				(byte)mainAppWindowViewModel.btd700Ctx.transportMode
			});
			mainAppWindowViewModel.ApplicationSelectionEnabled = false;
			TransportSelectButton.IsEnabled = true;
			CodecSelectButton.IsEnabled = true;
			mainAppWindowViewModel.ModeSetBackoffFor(4);
		}
	}

	private void btGaming_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel { bGaming: false } mainAppWindowViewModel && (!mainAppWindowViewModel.bBroadcast || (!mainAppWindowViewModel.BroadcastQualityChanged && !mainAppWindowViewModel.BroadcastPBPChanged && !mainAppWindowViewModel.BroadcastNameChanged && !mainAppWindowViewModel.BroadcastPasswordChanged) || mainAppWindowViewModel.modalToastBox("", mainAppWindowViewModel.SelectedLanguage.warntxt_discardbcastsettings, mainAppWindowViewModel.SelectedLanguage.btntxt_Confirm, mainAppWindowViewModel.SelectedLanguage.btntxt_Cancel) == 1))
		{
			mainAppWindowViewModel.bOneToOne = false;
			mainAppWindowViewModel.bGaming = true;
			mainAppWindowViewModel.bBroadcast = false;
			mainAppWindowViewModel.SelectedApplication = 1;
			mainAppWindowViewModel.PostEvent(_EVENT.BT_ABS_BTD700SETAUDIOANDTRANSPORT, new byte[2]
			{
				(byte)mainAppWindowViewModel.SelectedApplication,
				(byte)mainAppWindowViewModel.btd700Ctx.transportMode
			});
			mainAppWindowViewModel.ApplicationSelectionEnabled = false;
			TransportSelectButton.IsEnabled = false;
			CodecSelectButton.IsEnabled = false;
			mainAppWindowViewModel.ModeSetBackoffFor(4);
		}
	}

	private void btBroadcast_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel { bBroadcast: false } mainAppWindowViewModel)
		{
			mainAppWindowViewModel.bOneToOne = false;
			mainAppWindowViewModel.bGaming = false;
			mainAppWindowViewModel.bBroadcast = true;
			mainAppWindowViewModel.SelectedApplication = 2;
			mainAppWindowViewModel.PostEvent(_EVENT.BT_ABS_BTD700SETAUDIOANDTRANSPORT, new byte[2]
			{
				(byte)mainAppWindowViewModel.SelectedApplication,
				(byte)mainAppWindowViewModel.btd700Ctx.transportMode
			});
			mainAppWindowViewModel.ApplicationSelectionEnabled = false;
			mainAppWindowViewModel.BroadcastParamUIEnabled = false;
			TransportSelectButton.IsEnabled = true;
			CodecSelectButton.IsEnabled = true;
			mainAppWindowViewModel.ModeSetBackoffFor(4);
		}
	}

	private void BroadcastNameString_GotFocus(object sender, RoutedEventArgs e)
	{
	}

	private void BroadcastNameString_LostFocus(object sender, RoutedEventArgs e)
	{
	}

	private void BroadcastEncryptionKey_GotFocus(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			ButtonShowBcastKey.Visibility = (mainAppWindowViewModel.bShowBcastKey ? Visibility.Collapsed : Visibility.Visible);
			ButtonHideBcastKey.Visibility = ((!mainAppWindowViewModel.bShowBcastKey) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void BroadcastEncryptionKey_LostFocus(object sender, RoutedEventArgs e)
	{
		MainAppWindowViewModel mainAppWindowViewModel = base.DataContext as MainAppWindowViewModel;
		if (!ButtonShowBcastKey.IsMouseOver && !ButtonShowBcastKey.IsPressed)
		{
			ButtonShowBcastKey.Visibility = Visibility.Collapsed;
		}
		else if (mainAppWindowViewModel != null)
		{
			ButtonShowBcastKey.Visibility = (mainAppWindowViewModel.bShowBcastKey ? Visibility.Collapsed : Visibility.Visible);
		}
		if (!ButtonHideBcastKey.IsMouseOver && !ButtonHideBcastKey.IsPressed)
		{
			ButtonHideBcastKey.Visibility = Visibility.Collapsed;
		}
		else if (mainAppWindowViewModel != null)
		{
			ButtonHideBcastKey.Visibility = ((!mainAppWindowViewModel.bShowBcastKey) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.SetAppSelectionMenuPill(bVisible: true, bEnableDashboard: true, bEnableUpdate: true, bEnableSettings: true);
			mainAppWindowViewModel.SelectedMenu = 0;
			mainAppWindowViewModel.EnableBtSaveBcastName = false;
		}
	}

	private void lbTransportList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender is ListBox { SelectedItem: TransportItem selectedItem } listBox && base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			if (mainAppWindowViewModel.bSendAudioAndTransportCmd)
			{
				mainAppWindowViewModel.PostEvent(_EVENT.BT_ABS_BTD700SETAUDIOANDTRANSPORT, new byte[2]
				{
					(byte)mainAppWindowViewModel.btd700Ctx.iSelectedApplication,
					selectedItem.value
				});
				mainAppWindowViewModel.SelectedTransport = new TransportItem("", 0);
				listBox.SelectedIndex = -1;
			}
			else
			{
				mainAppWindowViewModel.bSendAudioAndTransportCmd = true;
			}
		}
		TransportSelectButton.IsChecked = false;
	}

	private void lbCodecList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender is ListBox { SelectedItem: CodecItem selectedItem } listBox && base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			if (mainAppWindowViewModel.bSendCodecSelectionCmd)
			{
				mainAppWindowViewModel.PostEvent(_EVENT.BT_ABS_BTD700SETCODECTOUSE, new byte[1] { (byte)(1 << selectedItem.bitidx) });
				mainAppWindowViewModel.SelectedCodec = new CodecItem(" --- ", -1);
				listBox.SelectedIndex = -1;
			}
			else
			{
				mainAppWindowViewModel.bSendCodecSelectionCmd = true;
			}
		}
		CodecSelectButton.IsChecked = false;
	}

	private void lbBroadcastQualityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender is ListBox { SelectedIndex: var selectedIndex } && selectedIndex != -1 && base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.btd700Ctx.broadcastQuality = (_BTD700_BROADCAST_QUALITY)selectedIndex;
			mainAppWindowViewModel.Btd700View_UpdateBroadcastActionButtonState();
		}
		BroadcastQualityButton.IsChecked = false;
	}

	private void ButtonShowHideBcastKey_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.bShowBcastKey = !mainAppWindowViewModel.bShowBcastKey;
			if (mainAppWindowViewModel.bShowBcastKey)
			{
				BroadcastEncryptionKey.Focus();
			}
			else
			{
				BcastKeyPasswordBox.Focus();
			}
		}
	}

	private void btWriteBcastName_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel && mainAppWindowViewModel.modalToastBox((mainAppWindowViewModel.BroadcastName == "") ? mainAppWindowViewModel.SelectedLanguage.warntxt_setdefaultbcastname_hdr : mainAppWindowViewModel.SelectedLanguage.warntxt_changebcastcfg_hdr, mainAppWindowViewModel.SelectedLanguage.warntxt_changebcastcfg_content, "Save", "Cancel") == 1)
		{
			mainAppWindowViewModel.WriteBroadcastName();
		}
	}

	private void btWriteBcastKey_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel && mainAppWindowViewModel.modalToastBox((mainAppWindowViewModel.BroadcastKey == "") ? mainAppWindowViewModel.SelectedLanguage.warntxt_clearbcastkey_hdr : mainAppWindowViewModel.SelectedLanguage.warntxt_changebcastcfg_hdr, mainAppWindowViewModel.SelectedLanguage.warntxt_changebcastcfg_content, "Save", "Cancel") == 1)
		{
			mainAppWindowViewModel.UI_UserResp(_EVENT.BT_ABS_BTD700WRITEBCASTKEY);
		}
	}

	private void PBPCheckbox_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			if (mainAppWindowViewModel.modalToastBox(mainAppWindowViewModel.SelectedLanguage.warntxt_changebcastcfg, mainAppWindowViewModel.SelectedLanguage.warntxt_changebcastcfg_sub, mainAppWindowViewModel.SelectedLanguage.btntxt_Confirm, mainAppWindowViewModel.SelectedLanguage.btntxt_Cancel) == 1)
			{
				mainAppWindowViewModel.BroadcastPBPChanged = true;
				mainAppWindowViewModel.btd700BcastCtxOriginal.broadcastState = (PBPCheckbox.IsChecked.Value ? _BTD700_BROADCAST_STATE.BCAST_ON_PUBLIC : _BTD700_BROADCAST_STATE.BCAST_OFF_PRIVATE);
				mainAppWindowViewModel.btd700BcastCtxBackup.ImportBroadcastContext(mainAppWindowViewModel.btd700Ctx, isBackup: true);
				mainAppWindowViewModel.btd700BcastCtxBackup.broadcastState = mainAppWindowViewModel.btd700BcastCtxOriginal.broadcastState;
				mainAppWindowViewModel.SetEditable(editable: false);
				mainAppWindowViewModel.UI_UserResp(_EVENT.BT_ABS_BTD700CHANGEBCASTSTATE);
			}
			else
			{
				mainAppWindowViewModel.BroadcastPBPChanged = false;
				mainAppWindowViewModel.btd700BcastCtxBackup.Reset();
				mainAppWindowViewModel.btd700Ctx.broadcastState = mainAppWindowViewModel.btd700BcastCtxOriginal.broadcastState;
				mainAppWindowViewModel.BroadcastState = mainAppWindowViewModel.btd700Ctx.broadcastState == _BTD700_BROADCAST_STATE.BCAST_ON_PUBLIC;
				e.Handled = true;
			}
		}
	}

	private void BroadcastEncryptionCheckbox_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			if (mainAppWindowViewModel.btd700Ctx.broadcastEncKey != null && mainAppWindowViewModel.btd700Ctx.broadcastEncKey.Length != 0)
			{
				mainAppWindowViewModel.UI_UserResp(_EVENT.BT_ABS_BTD700CHANGEBCASTENCRYPTION);
			}
			else
			{
				e.Handled = true;
			}
		}
	}

	private void BroadcastNameString_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		BroadcastNameString_PreviewKey(sender, e, isKeyDown: true);
	}

	private void BroadcastNameString_PreviewKeyUp(object sender, KeyEventArgs e)
	{
		BroadcastNameString_PreviewKey(sender, e, isKeyDown: false);
	}

	private void BroadcastNameString_PreviewKey(object sender, KeyEventArgs e, bool isKeyDown)
	{
		Key key = e.Key;
		if (key == Key.ImeProcessed)
		{
			key = e.ImeProcessedKey;
		}
		if (!IsCharValidWithSpace(key))
		{
			e.Handled = true;
			return;
		}
		switch (key)
		{
		case Key.Space:
			if (BroadcastNameString.Text.Length == 0)
			{
				e.Handled = true;
				return;
			}
			break;
		case Key.Back:
		case Key.Left:
		case Key.Right:
		case Key.Delete:
			return;
		}
		MainAppWindowViewModel mainAppWindowViewModel = base.DataContext as MainAppWindowViewModel;
		try
		{
			if (mainAppWindowViewModel != null && mainAppWindowViewModel.broadcastName != null && Encoding.ASCII.GetString(mainAppWindowViewModel.broadcastName) != null && Encoding.ASCII.GetString(mainAppWindowViewModel.broadcastName).TrimEnd('\0').Length >= 16 && !BroadcastNameString.IsSelectionActive)
			{
				e.Handled = true;
				return;
			}
			if (isKeyDown)
			{
				if (e.Key == Key.ImeProcessed)
				{
					e.Handled = true;
				}
				return;
			}
			byte b = KeyToByte(key, bAllowSpace: true);
			if (e.Key != Key.ImeProcessed && b != byte.MaxValue && InputMethod.Current.ImeState == InputMethodState.On)
			{
				int start = BroadcastNameString.SelectionStart + 1;
				TextBox broadcastNameString = BroadcastNameString;
				char c = (char)b;
				broadcastNameString.SelectedText = c.ToString() ?? "";
				BroadcastNameString.Select(start, 0);
			}
		}
		catch (Exception)
		{
		}
	}

	private void BroadcastEncryptionKey_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		BroadcastPasswordString_PreviewKey(sender, e, isKeyDown: true);
	}

	private void BroadcastEncryptionKey_PreviewKeyUp(object sender, KeyEventArgs e)
	{
		BroadcastPasswordString_PreviewKey(sender, e, isKeyDown: false);
	}

	public byte KeyToByte(Key k, bool bAllowSpace = false)
	{
		byte result = byte.MaxValue;
		byte b = (byte)((Keyboard.IsKeyToggled(Key.Capital) || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ? 65u : 97u);
		if (k >= Key.D0 && k <= Key.D9)
		{
			result = (byte)((byte)k - 34 + 48);
		}
		else if (k >= Key.NumPad0 && k <= Key.NumPad9)
		{
			if (Keyboard.IsKeyToggled(Key.NumLock))
			{
				result = (byte)((byte)k - 74 + 48);
			}
		}
		else if (k >= Key.A && k <= Key.Z)
		{
			result = (byte)((byte)k - 44 + b);
		}
		else if (k == Key.Space && bAllowSpace)
		{
			result = 32;
		}
		return result;
	}

	private void BroadcastPasswordString_PreviewKey(object sender, KeyEventArgs e, bool isKeyDown)
	{
		Key key = e.Key;
		if (key == Key.ImeProcessed)
		{
			key = e.ImeProcessedKey;
		}
		if (!IsCharValidNoSpace(key))
		{
			e.Handled = true;
		}
		else
		{
			if (key == Key.Left || key == Key.Right || key == Key.Back || key == Key.Delete)
			{
				return;
			}
			MainAppWindowViewModel mainAppWindowViewModel = base.DataContext as MainAppWindowViewModel;
			try
			{
				bool flag;
				if (!mainAppWindowViewModel.bShowBcastKey)
				{
					try
					{
						TextSelection textSelection = (TextSelection)typeof(PasswordBox).GetProperty("Selection", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod.Invoke(BcastKeyPasswordBox, null);
						flag = textSelection != null && textSelection.Text != null && textSelection.Text.Length > 0;
					}
					catch (Exception)
					{
						flag = false;
					}
				}
				else
				{
					flag = BroadcastEncryptionKey.IsSelectionActive;
				}
				if (mainAppWindowViewModel != null && mainAppWindowViewModel.broadcastEncKey != null && Encoding.ASCII.GetString(mainAppWindowViewModel.broadcastEncKey) != null && Encoding.ASCII.GetString(mainAppWindowViewModel.broadcastEncKey).TrimEnd('\0').Length >= 16 && !flag)
				{
					e.Handled = true;
				}
				else
				{
					if (!mainAppWindowViewModel.bShowBcastKey)
					{
						return;
					}
					if (isKeyDown)
					{
						e.Handled = true;
						return;
					}
					byte b = KeyToByte(key);
					if (e.Key != Key.ImeProcessed && b != byte.MaxValue)
					{
						int start = BroadcastEncryptionKey.SelectionStart + 1;
						TextBox broadcastEncryptionKey = BroadcastEncryptionKey;
						char c = (char)b;
						broadcastEncryptionKey.SelectedText = c.ToString() ?? "";
						BroadcastEncryptionKey.Select(start, 0);
					}
				}
			}
			catch (Exception)
			{
			}
		}
	}

	public bool IsCharValidWithSpace(Key k)
	{
		switch (k)
		{
		default:
			if ((k < Key.D0 || k > Key.D9) && (k < Key.NumPad0 || k > Key.NumPad9))
			{
				return k == Key.Space;
			}
			break;
		case Key.Back:
		case Key.Capital:
		case Key.Left:
		case Key.Right:
		case Key.Delete:
		case Key.A:
		case Key.B:
		case Key.C:
		case Key.D:
		case Key.E:
		case Key.F:
		case Key.G:
		case Key.H:
		case Key.I:
		case Key.J:
		case Key.K:
		case Key.L:
		case Key.M:
		case Key.N:
		case Key.O:
		case Key.P:
		case Key.Q:
		case Key.R:
		case Key.S:
		case Key.T:
		case Key.U:
		case Key.V:
		case Key.W:
		case Key.X:
		case Key.Y:
		case Key.Z:
		case Key.NumLock:
		case Key.LeftShift:
		case Key.RightShift:
			break;
		}
		return true;
	}

	public bool IsCharValidNoSpace(Key k)
	{
		switch (k)
		{
		default:
			if (k < Key.D0 || k > Key.D9)
			{
				if (k >= Key.NumPad0)
				{
					return k <= Key.NumPad9;
				}
				return false;
			}
			break;
		case Key.Back:
		case Key.Capital:
		case Key.Left:
		case Key.Right:
		case Key.Delete:
		case Key.A:
		case Key.B:
		case Key.C:
		case Key.D:
		case Key.E:
		case Key.F:
		case Key.G:
		case Key.H:
		case Key.I:
		case Key.J:
		case Key.K:
		case Key.L:
		case Key.M:
		case Key.N:
		case Key.O:
		case Key.P:
		case Key.Q:
		case Key.R:
		case Key.S:
		case Key.T:
		case Key.U:
		case Key.V:
		case Key.W:
		case Key.X:
		case Key.Y:
		case Key.Z:
		case Key.NumLock:
		case Key.LeftShift:
		case Key.RightShift:
			break;
		}
		return true;
	}

	private void btDiscardBcastSettings_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel && mainAppWindowViewModel.modalToastBox(mainAppWindowViewModel.SelectedLanguage.warntxt_changebcastcfg_hdr, mainAppWindowViewModel.SelectedLanguage.warntxt_discardbcastsettings, mainAppWindowViewModel.SelectedLanguage.btntxt_Confirm, mainAppWindowViewModel.SelectedLanguage.btntxt_Cancel) == 1)
		{
			mainAppWindowViewModel.btd700Ctx.ImportBroadcastContext(mainAppWindowViewModel.btd700BcastCtxOriginal);
			mainAppWindowViewModel.SetBroadcastQuality((byte)mainAppWindowViewModel.btd700Ctx.broadcastQuality);
			mainAppWindowViewModel.BroadcastState = mainAppWindowViewModel.btd700Ctx.broadcastState == _BTD700_BROADCAST_STATE.BCAST_ON_PUBLIC;
			mainAppWindowViewModel.BroadcastEncryption = mainAppWindowViewModel.btd700Ctx.broadcastEncrypt == _BTD700_BROADCAST_ENCRYPTION.BCAST_ENCR_ON;
			mainAppWindowViewModel.broadcastName = mainAppWindowViewModel.btd700Ctx.broadcastName;
			mainAppWindowViewModel.broadcastEncKey = mainAppWindowViewModel.btd700Ctx.broadcastEncKey;
			mainAppWindowViewModel.ValidateBroadcastName();
			mainAppWindowViewModel.ValidateBroadcastKey();
			mainAppWindowViewModel.btd700BcastCtxBackup.Reset();
			mainAppWindowViewModel.Btd700View_UpdateBroadcastActionButtonState();
		}
	}

	private void btSaveBcastSettings_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			if (mainAppWindowViewModel.BroadcastNameChanged && !IsTextAllowed(BroadcastNameString.Text, "[^a-zA-Z0-9 ]"))
			{
				mainAppWindowViewModel.modalToastBox(mainAppWindowViewModel.SelectedLanguage.ltxt_Error + " : " + mainAppWindowViewModel.SelectedLanguage.ltxt_bcastname, mainAppWindowViewModel.SelectedLanguage.ltxt_bcasttooltip, mainAppWindowViewModel.SelectedLanguage.btntxt_Close);
			}
			else if (mainAppWindowViewModel.BroadcastNameChanged && BroadcastNameString.Text != BroadcastNameString.Text.Trim())
			{
				mainAppWindowViewModel.modalToastBox(mainAppWindowViewModel.SelectedLanguage.ltxt_Error + " : " + mainAppWindowViewModel.SelectedLanguage.ltxt_bcastname, mainAppWindowViewModel.SelectedLanguage.ltxt_nowhitespace, mainAppWindowViewModel.SelectedLanguage.btntxt_Close);
			}
			else if (mainAppWindowViewModel.modalToastBox(mainAppWindowViewModel.SelectedLanguage.warntxt_changebcastcfg, mainAppWindowViewModel.SelectedLanguage.warntxt_changebcastcfg_sub, mainAppWindowViewModel.SelectedLanguage.btntxt_Confirm, mainAppWindowViewModel.SelectedLanguage.btntxt_Cancel) == 1)
			{
				mainAppWindowViewModel.BroadcastEncryptionChanged = mainAppWindowViewModel.btd700Ctx.broadcastEncKey != mainAppWindowViewModel.btd700BcastCtxOriginal.broadcastEncKey || !mainAppWindowViewModel.btd700Ctx.broadcastEncKey.SequenceEqual(mainAppWindowViewModel.btd700BcastCtxOriginal.broadcastEncKey);
				mainAppWindowViewModel.btd700BcastCtxBackup.Reset();
				mainAppWindowViewModel.SetEditable(editable: false);
				mainAppWindowViewModel.UI_UserResp(_EVENT.BT_ABS_BTD700CHANGEBCASTSTATE);
			}
		}
	}

	private void BroadcastNameString_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (base.DataContext is MainAppWindowViewModel mainAppWindowViewModel)
		{
			mainAppWindowViewModel.Btd700View_UpdateBroadcastActionButtonState();
		}
	}

	private void BcastKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
	{
	}

	private void BcastKeyPasswordBox_PasswordChanged(object sender, TextChangedEventArgs e)
	{
	}

	private void BroadcastNameString_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z0-9 ]");
	}

	private void BcastKeyPasswordBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z0-9]");
	}

	private void BroadcastEncryptionKey_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z0-9]");
	}

	private void OnPasteDisallowSpace(object sender, DataObjectPastingEventArgs e)
	{
		if (e.SourceDataObject.GetDataPresent(DataFormats.Text, autoConvert: true) && e.SourceDataObject.GetData(DataFormats.Text) is string text && !IsTextAllowed(text, "[^a-zA-Z0-9]"))
		{
			e.CancelCommand();
		}
	}

	private void OnPasteAllowSpace(object sender, DataObjectPastingEventArgs e)
	{
		if (e.SourceDataObject.GetDataPresent(DataFormats.Text, autoConvert: true) && e.SourceDataObject.GetData(DataFormats.Text) is string text)
		{
			_ = base.DataContext;
			if (!IsTextAllowed(text, "[^a-zA-Z0-9 ]"))
			{
				e.CancelCommand();
			}
			else if (text != text.Trim())
			{
				e.CancelCommand();
			}
		}
	}

	private static bool IsTextAllowed(string Text, string AllowedRegex)
	{
		try
		{
			return !new Regex(AllowedRegex).IsMatch(Text);
		}
		catch
		{
			return true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Sennheiser Dongle Control;V1.0.5;component/views/appbtd700features.xaml", UriKind.Relative);
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
			((AppBtd700Features)target).Loaded += UserControl_Loaded;
			break;
		case 2:
			btOneToOne = (Button)target;
			btOneToOne.Click += btOneToOne_Click;
			break;
		case 3:
			cbOneToOne = (CheckBox)target;
			cbOneToOne.Click += btOneToOne_Click;
			break;
		case 4:
			btGaming = (Button)target;
			btGaming.Click += btGaming_Click;
			break;
		case 5:
			cbGaming = (CheckBox)target;
			cbGaming.Click += btGaming_Click;
			break;
		case 6:
			btBroadcast = (Button)target;
			btBroadcast.Click += btBroadcast_Click;
			break;
		case 7:
			cbBroadcast = (CheckBox)target;
			cbBroadcast.Click += btBroadcast_Click;
			break;
		case 8:
			rowAudioQualityInfo = (Border)target;
			break;
		case 9:
			rowBroadcastQualitySelect = (Border)target;
			break;
		case 10:
			BroadcastQualityButton = (ToggleButton)target;
			break;
		case 11:
			tbBroadcastQuality = (TextBlock)target;
			break;
		case 12:
			lbBroadcastQuality = (Popup)target;
			break;
		case 13:
			lbBroadcastQualityList = (ListBox)target;
			lbBroadcastQualityList.SelectionChanged += lbBroadcastQualityList_SelectionChanged;
			break;
		case 14:
			rowTransportTypeSelect = (Border)target;
			break;
		case 15:
			TransportSelectButton = (ToggleButton)target;
			break;
		case 16:
			tbSelectedTransport = (TextBlock)target;
			break;
		case 17:
			lbTransportSelect = (Popup)target;
			break;
		case 18:
			lbTransportList = (ListBox)target;
			lbTransportList.SelectionChanged += lbTransportList_SelectionChanged;
			break;
		case 19:
			rowBroadcastPublicProfile = (Border)target;
			break;
		case 20:
			PBPCheckbox = (CheckBox)target;
			PBPCheckbox.Click += PBPCheckbox_Click;
			break;
		case 21:
			rowCodecSelect = (Border)target;
			break;
		case 22:
			CodecSelectButton = (ToggleButton)target;
			break;
		case 23:
			tbSelectedCodec = (TextBlock)target;
			break;
		case 24:
			lbCodecSelect = (Popup)target;
			break;
		case 25:
			lbCodecList = (ListBox)target;
			lbCodecList.SelectionChanged += lbCodecList_SelectionChanged;
			break;
		case 26:
			rowBroadcastName = (Border)target;
			break;
		case 27:
			BroadcastNameString = (TextBox)target;
			BroadcastNameString.PreviewKeyUp += BroadcastNameString_PreviewKeyUp;
			BroadcastNameString.PreviewKeyDown += BroadcastNameString_PreviewKeyDown;
			BroadcastNameString.TextChanged += BroadcastNameString_TextChanged;
			BroadcastNameString.GotFocus += BroadcastNameString_GotFocus;
			BroadcastNameString.LostFocus += BroadcastNameString_LostFocus;
			BroadcastNameString.PreviewTextInput += BroadcastNameString_PreviewTextInput;
			break;
		case 28:
			rowBroadcastPassword = (Border)target;
			break;
		case 29:
			gridBroadcastPassword = (Grid)target;
			break;
		case 30:
			BcastKeyPasswordBox = (PasswordBox)target;
			BcastKeyPasswordBox.GotFocus += BroadcastEncryptionKey_GotFocus;
			BcastKeyPasswordBox.LostFocus += BroadcastEncryptionKey_LostFocus;
			BcastKeyPasswordBox.PreviewKeyUp += BroadcastEncryptionKey_PreviewKeyUp;
			BcastKeyPasswordBox.PreviewKeyDown += BroadcastEncryptionKey_PreviewKeyDown;
			BcastKeyPasswordBox.PasswordChanged += BcastKeyPasswordBox_PasswordChanged;
			BcastKeyPasswordBox.PreviewTextInput += BcastKeyPasswordBox_PreviewTextInput;
			break;
		case 31:
			ButtonShowBcastKey = (Button)target;
			ButtonShowBcastKey.Click += ButtonShowHideBcastKey_Click;
			break;
		case 32:
			BroadcastEncryptionKey = (TextBox)target;
			BroadcastEncryptionKey.PreviewKeyUp += BroadcastEncryptionKey_PreviewKeyUp;
			BroadcastEncryptionKey.PreviewKeyDown += BroadcastEncryptionKey_PreviewKeyDown;
			BroadcastEncryptionKey.GotFocus += BroadcastEncryptionKey_GotFocus;
			BroadcastEncryptionKey.LostFocus += BroadcastEncryptionKey_LostFocus;
			BroadcastEncryptionKey.TextChanged += BcastKeyPasswordBox_PasswordChanged;
			BroadcastEncryptionKey.PreviewTextInput += BroadcastEncryptionKey_PreviewTextInput;
			break;
		case 33:
			ButtonHideBcastKey = (Button)target;
			ButtonHideBcastKey.Click += ButtonShowHideBcastKey_Click;
			break;
		case 34:
			rowActionButtons = (Border)target;
			break;
		case 35:
			btDiscardBcastSettings = (Button)target;
			btDiscardBcastSettings.Click += btDiscardBcastSettings_Click;
			break;
		case 36:
			btSaveBcastSettings = (Button)target;
			btSaveBcastSettings.Click += btSaveBcastSettings_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
