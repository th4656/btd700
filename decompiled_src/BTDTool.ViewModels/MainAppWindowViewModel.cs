using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using BTDTool.Views;
using HidSharp;
using HidSharp.Reports;

namespace BTDTool.ViewModels;

internal class MainAppWindowViewModel : ViewModelBase, IDisposable
{
	public enum _DFUNOTI
	{
		NONE,
		NONETWORK,
		UPTODATE,
		VARIABLE
	}

	private static TimeSpan _time;

	private static DispatcherTimer? _timer;

	private int _SelectedApplication = -1;

	private bool _bOneToOne;

	private bool _bGaming;

	private bool _bBroadcast;

	private bool _bShowBcastKey;

	public BTD700Context btd700Ctx = new BTD700Context();

	public BTD700BroadcastContext btd700BcastCtxOriginal = new BTD700BroadcastContext();

	public BTD700BroadcastContext btd700BcastCtxBackup = new BTD700BroadcastContext();

	private byte[] _broadcastName = Array.Empty<byte>();

	private bool _EnableBtSaveBcastName;

	private bool _EnableBtSaveBcastKey;

	private bool _EnableBtDiscardBcastSettings;

	private bool _EnableBtSaveBcastSettings;

	private bool _BroadcastQualityChanged;

	private bool _BroadcastPBPChanged;

	private bool _BroadcastEncryptionChanged;

	private bool _BroadcastNameChanged;

	private bool _BroadcastPasswordChanged;

	private string _BroadcastQuality = "";

	private string _SelectedBroadcastQuality = "";

	private int _SelectedBroadcastQualityIndex = -1;

	private bool _BroadcastParamUIEnabled = true;

	private bool _ApplicationSelectionEnabled = true;

	private string _audio_quality = "";

	private Visibility _StateAndAudioQualityVisibility;

	private Visibility _audioQualityVisibility = Visibility.Hidden;

	private Visibility _WarningPanelVisibility = Visibility.Hidden;

	private Visibility _TransportPanelVisibility = Visibility.Collapsed;

	private Visibility _CodecPanelVisibility = Visibility.Hidden;

	private Visibility _BroadcastPanelVisibility = Visibility.Hidden;

	private List<TransportItem> _transport_list = new List<TransportItem>(0);

	private TransportItem _selected_transport = new TransportItem("", 0);

	private int _selected_transport_index = -1;

	private CodecItem _selected_codec = new CodecItem(" --- ", -1);

	private int _selected_codec_index = -1;

	private List<CodecItem> _codec_list = new List<CodecItem>(0);

	private Visibility _disconnectVisibility = Visibility.Collapsed;

	private Visibility _connectVisibility;

	private int _SelectedApplicationIndex = -1;

	public bool bSendAudioAndTransportCmd = true;

	public bool bSendCodecSelectionCmd = true;

	private Queue<EventObj> _events = new Queue<EventObj>();

	public _STATES enFSMState;

	public _STATES enFSMPrevState;

	public _STATES enFSMPreAbortState;

	private ushort usSubstate;

	private ushort usNextSubstate;

	private int iLoopCounter;

	private int iTransferState;

	private DFUFile dfufile;

	private bool bAbortTransfer;

	public byte[] UniqueFileID = new byte[4] { 1, 2, 3, 4 };

	public const ushort DONGLE_RECONNECT_WAITING_TIME = 60;

	private string SelectedRepoVersion = "";

	private DateTime startDT = DateTime.Now;

	private Thread threadMain;

	public int PollDurationSeconds;

	public string strMainProc = "";

	public Stopwatch swElapsedTimeInSubstate = new Stopwatch();

	public int iVerboseLevel;

	public char cRepoURLBase = 'd';

	public bool bExitApplication;

	public MainAppWindow view;

	private ResDictExt _selectedLanguage = new ResDictExt();

	private ObservableCollection<ResDictExt> _LanguageList = new ObservableCollection<ResDictExt>();

	private Visibility _LogoVisibility;

	private Visibility _AppSelectionVisibility;

	private bool _MenuDashboardEnabled;

	private bool _MenuUpdateEnabled;

	private bool _MenuSettingsEnabled;

	private Visibility _DashboardMenuVisibility;

	private UserControl? _userInterface;

	private bool _SafeForEditing = true;

	private bool _MenuPillEnabled = true;

	private bool _bUseDefaultFileID;

	private bool _bForceTransfer;

	private List<string> _RepoVersionList = new List<string>();

	private string variableDFUNotification = "";

	private string variableDFUNotificationSub = "";

	private string _RepoVersionInfo = "";

	private string _dongleimage_source = "/BTD700.png";

	private Visibility _dongleimage_visibility;

	private DongleDeviceExt _SelectedDongle = new DongleDeviceExt();

	public AppDfu? DFUView;

	public AppSettings? SettingsView;

	public AppLicense? LicenseView;

	public AppDongleWait? DongleWaitView;

	public AppBtd700Features? Btd700View;

	private string _strCurrFWVersionLabel = "";

	private string _FWVersionLabel = "";

	private string _AppVersionLabel = "";

	private bool disposedValue;

	private int _SelectedMenu = -1;

	private string _ltxt2 = "";

	private string _ltxt3_text = "";

	private SolidColorBrush _ltxt3_background = Brushes.White;

	private Visibility _ltxt3_visibility = Visibility.Hidden;

	private string _ltxt4 = "";

	private string _ltxt6_text = "";

	private HorizontalAlignment _ltxt6_location = HorizontalAlignment.Center;

	private Visibility _ltxt6_Visible = Visibility.Collapsed;

	private string _ltxt7_text = "";

	private Visibility _ltxt7_Visible;

	public List<string> _delayedStatusEntries = new List<string>();

	public List<string> _delayedLogEntries = new List<string>();

	private const int MAX_DELAYED_ENTRIES = 100;

	private Visibility _DongleNotFoundVisibility;

	private string _pbTransfer_Value = "";

	private Visibility _pbTransfer_Visible;

	private _EVENT _bt3Act;

	private string _bt3_text = "";

	private Visibility _bt3_Visible;

	private _EVENT _bt2Act;

	private string _bt2_text = "";

	private Visibility _bt2_Visible;

	private _EVENT _bt1Act;

	private string _bt1_text = "";

	private Visibility _bt1_Visible;

	public bool bUsbHidInitialized;

	private ObservableCollection<DongleDeviceExt> _DongleDevices = new ObservableCollection<DongleDeviceExt>();

	public int SelectedApplication
	{
		get
		{
			return _SelectedApplication;
		}
		set
		{
			if (0 <= value && value <= 2)
			{
				_SelectedApplication = value;
				SetCenterPanel(_SelectedApplication);
			}
		}
	}

	public bool bOneToOne
	{
		get
		{
			return _bOneToOne;
		}
		set
		{
			_bOneToOne = value;
			OnPropertyChanged("bOneToOne");
		}
	}

	public bool bGaming
	{
		get
		{
			return _bGaming;
		}
		set
		{
			_bGaming = value;
			OnPropertyChanged("bGaming");
		}
	}

	public bool bBroadcast
	{
		get
		{
			return _bBroadcast;
		}
		set
		{
			_bBroadcast = value;
			OnPropertyChanged("bBroadcast");
		}
	}

	public bool bShowBcastKey
	{
		get
		{
			return _bShowBcastKey;
		}
		set
		{
			_bShowBcastKey = value;
			OnPropertyChanged("bShowBcastKey");
		}
	}

	public byte[] broadcastName
	{
		get
		{
			return _broadcastName;
		}
		set
		{
			_broadcastName = value;
			OnPropertyChanged("BroadcastName");
			Btd700View_UpdateBroadcastActionButtonState();
		}
	}

	public bool EnableBtSaveBcastName
	{
		get
		{
			return _EnableBtSaveBcastName;
		}
		set
		{
			_EnableBtSaveBcastName = value;
			OnPropertyChanged("EnableBtSaveBcastName");
		}
	}

	public bool EnableBtSaveBcastKey
	{
		get
		{
			return _EnableBtSaveBcastKey;
		}
		set
		{
			_EnableBtSaveBcastKey = value;
			OnPropertyChanged("EnableBtSaveBcastKey");
		}
	}

	public bool EnableBtDiscardBcastSettings
	{
		get
		{
			return _EnableBtDiscardBcastSettings;
		}
		set
		{
			_EnableBtDiscardBcastSettings = value;
			OnPropertyChanged("EnableBtDiscardBcastSettings");
		}
	}

	public bool EnableBtSaveBcastSettings
	{
		get
		{
			return _EnableBtSaveBcastSettings;
		}
		set
		{
			_EnableBtSaveBcastSettings = value;
			OnPropertyChanged("EnableBtSaveBcastSettings");
		}
	}

	public bool BroadcastQualityChanged
	{
		get
		{
			return _BroadcastQualityChanged;
		}
		set
		{
			_BroadcastQualityChanged = value;
			OnPropertyChanged("BroadcastQualityChanged");
		}
	}

	public bool BroadcastPBPChanged
	{
		get
		{
			return _BroadcastPBPChanged;
		}
		set
		{
			_BroadcastPBPChanged = value;
			OnPropertyChanged("BroadcastPBPChanged");
		}
	}

	public bool BroadcastEncryptionChanged
	{
		get
		{
			return _BroadcastEncryptionChanged;
		}
		set
		{
			_BroadcastEncryptionChanged = value;
			OnPropertyChanged("BroadcastEncryptionChanged");
		}
	}

	public bool BroadcastNameChanged
	{
		get
		{
			return _BroadcastNameChanged;
		}
		set
		{
			_BroadcastNameChanged = value;
			OnPropertyChanged("BroadcastNameChanged");
		}
	}

	public bool BroadcastPasswordChanged
	{
		get
		{
			return _BroadcastPasswordChanged;
		}
		set
		{
			_BroadcastPasswordChanged = value;
			OnPropertyChanged("BroadcastPasswordChanged");
		}
	}

	public string BroadcastName
	{
		get
		{
			if (_broadcastName == null || _broadcastName.Length == 0)
			{
				return "";
			}
			return Encoding.UTF8.GetString(_broadcastName);
		}
		set
		{
			byte[] array = ((value == null || value.Length == 0) ? null : Encoding.UTF8.GetBytes(value.Replace("\0", "")));
			EnableBtSaveBcastName = (btd700Ctx.broadcastName == null && array != null) || (btd700Ctx.broadcastName != null && array == null) || !btd700Ctx.broadcastName.SequenceEqual(array);
			if (array == null || array.Length < 17)
			{
				broadcastName = array;
			}
			btd700Ctx.broadcastName = broadcastName;
			OnPropertyChanged("IsBroadcastNameValid");
		}
	}

	public bool IsBroadcastNameValid
	{
		get
		{
			if (BroadcastName != null)
			{
				if (BroadcastName != null)
				{
					if (BroadcastName.Length != 0)
					{
						return BroadcastName.Length >= 4;
					}
					return true;
				}
				return false;
			}
			return true;
		}
	}

	public string BroadcastQuality
	{
		get
		{
			return _BroadcastQuality;
		}
		set
		{
			_BroadcastQuality = value;
			OnPropertyChanged("BroadcastQuality");
		}
	}

	public string SelectedBroadcastQuality
	{
		get
		{
			return _SelectedBroadcastQuality;
		}
		set
		{
			_SelectedBroadcastQuality = value;
			OnPropertyChanged("SelectedBroadcastQuality");
			BroadcastQuality = value;
		}
	}

	public int SelectedBroadcastQualityIndex
	{
		get
		{
			return _SelectedBroadcastQualityIndex;
		}
		set
		{
			_SelectedBroadcastQualityIndex = value;
			OnPropertyChanged("SelectedBroadcastQualityIndex");
		}
	}

	public byte[] broadcastEncKey
	{
		get
		{
			return btd700Ctx.broadcastEncKey;
		}
		set
		{
			if (value != null)
			{
				btd700Ctx.broadcastEncKey = new byte[value.Length];
				Array.Copy(value, 0, btd700Ctx.broadcastEncKey, 0, value.Length);
			}
			else
			{
				btd700Ctx.broadcastEncKey = null;
			}
			BroadcastEncryption = btd700Ctx.broadcastEncKey != null && btd700Ctx.broadcastEncKey.Length >= 4;
			OnPropertyChanged("BroadcastKey");
			OnPropertyChanged("BroadcastEncryption");
			Btd700View_UpdateBroadcastActionButtonState();
		}
	}

	public string BroadcastKey
	{
		get
		{
			if (btd700Ctx.broadcastEncKey == null || btd700Ctx.broadcastEncKey.Length == 0)
			{
				return "";
			}
			return Encoding.UTF8.GetString(btd700Ctx.broadcastEncKey);
		}
		set
		{
			byte[] array = ((value == null || value.Length == 0) ? null : Encoding.UTF8.GetBytes(value.Trim().Replace("\0", "")));
			if (array == null || array.Length < 17)
			{
				broadcastEncKey = array;
			}
			OnPropertyChanged("IsBroadcastKeyValid");
		}
	}

	public bool IsBroadcastKeyValid
	{
		get
		{
			if (BroadcastKey != null)
			{
				if (BroadcastKey != null)
				{
					if (BroadcastKey.Length != 0)
					{
						return BroadcastKey.Length >= 4;
					}
					return true;
				}
				return false;
			}
			return true;
		}
	}

	public bool BroadcastEncryption
	{
		get
		{
			return btd700Ctx.broadcastEncrypt == _BTD700_BROADCAST_ENCRYPTION.BCAST_ENCR_ON;
		}
		set
		{
			btd700Ctx.broadcastEncrypt = (value ? _BTD700_BROADCAST_ENCRYPTION.BCAST_ENCR_ON : _BTD700_BROADCAST_ENCRYPTION.BCAST_ENCR_OFF);
			OnPropertyChanged("BroadcastEncryption");
		}
	}

	public bool BroadcastState
	{
		get
		{
			return btd700Ctx.broadcastState == _BTD700_BROADCAST_STATE.BCAST_ON_PUBLIC;
		}
		set
		{
			btd700Ctx.broadcastState = (value ? _BTD700_BROADCAST_STATE.BCAST_ON_PUBLIC : _BTD700_BROADCAST_STATE.BCAST_OFF_PRIVATE);
			OnPropertyChanged("BroadcastState");
		}
	}

	public bool BroadcastParamUIEnabled
	{
		get
		{
			return _BroadcastParamUIEnabled;
		}
		set
		{
			_BroadcastParamUIEnabled = value;
			OnPropertyChanged("BroadcastParamUIEnabled");
		}
	}

	public bool ApplicationSelectionEnabled
	{
		get
		{
			return _ApplicationSelectionEnabled;
		}
		set
		{
			_ApplicationSelectionEnabled = value;
			OnPropertyChanged("ApplicationSelectionEnabled");
		}
	}

	public Visibility dstate_visibility_1 { get; set; } = Visibility.Hidden;


	public Visibility dstate_visibility_2 { get; set; } = Visibility.Hidden;


	public Visibility dstate_visibility_3 { get; set; } = Visibility.Hidden;


	public Visibility dstate_visibility_4 { get; set; } = Visibility.Hidden;


	public bool IsGamingModeAllowed
	{
		get
		{
			bool result = bGaming;
			if (btd700Ctx.iGamingAvailable == -1)
			{
				switch (btd700Ctx.connectedTransportMode)
				{
				case _BTD700_TRANSPORT_MODE.TMODE_BR_EDR:
					result = (btd700Ctx.u16SupportedCodecs & 4) > 0;
					break;
				case _BTD700_TRANSPORT_MODE.TMODE_LEAUDIO:
					result = true;
					break;
				case _BTD700_TRANSPORT_MODE.TMODE_DUAL:
					result = true;
					break;
				}
			}
			else
			{
				result = btd700Ctx.iGamingAvailable == 1;
			}
			return result;
		}
	}

	public Visibility propertylabel_visibility
	{
		get
		{
			if (AudioQualityVisibility != 0 && BroadcastPanelVisibility != 0 && TransportPanelVisibility != 0 && BroadcastPanelVisibility != 0 && CodecPanelVisibility != 0)
			{
				return Visibility.Hidden;
			}
			return Visibility.Visible;
		}
	}

	public string audio_quality
	{
		get
		{
			return _audio_quality;
		}
		set
		{
			_audio_quality = value;
			OnPropertyChanged("audio_quality");
		}
	}

	public Visibility StateAndAudioQualityVisibility
	{
		get
		{
			return _StateAndAudioQualityVisibility;
		}
		set
		{
			_StateAndAudioQualityVisibility = value;
			OnPropertyChanged("StateAndAudioQualityVisibility");
		}
	}

	public Visibility AudioQualityVisibility
	{
		get
		{
			return _audioQualityVisibility;
		}
		set
		{
			_audioQualityVisibility = value;
			OnPropertyChanged("AudioQualityVisibility");
		}
	}

	public Visibility WarningPanelVisibility
	{
		get
		{
			return _WarningPanelVisibility;
		}
		set
		{
			_WarningPanelVisibility = value;
			OnPropertyChanged("WarningPanelVisibility");
		}
	}

	public Visibility TransportPanelVisibility
	{
		get
		{
			return _TransportPanelVisibility;
		}
		set
		{
			_TransportPanelVisibility = value;
			OnPropertyChanged("TransportPanelVisibility");
		}
	}

	public Visibility CodecPanelVisibility
	{
		get
		{
			return _CodecPanelVisibility;
		}
		set
		{
			_CodecPanelVisibility = value;
			OnPropertyChanged("CodecPanelVisibility");
		}
	}

	public Visibility BroadcastPanelVisibility
	{
		get
		{
			return _BroadcastPanelVisibility;
		}
		set
		{
			_BroadcastPanelVisibility = value;
			OnPropertyChanged("BroadcastPanelVisibility");
		}
	}

	public List<TransportItem> TransportList
	{
		get
		{
			return _transport_list;
		}
		set
		{
			_transport_list = value;
			OnPropertyChanged("TransportList");
			OnPropertyChanged("IsGamingModeAllowed");
		}
	}

	public TransportItem SelectedTransport
	{
		get
		{
			return _selected_transport;
		}
		set
		{
			_selected_transport = value;
			OnPropertyChanged("SelectedTransport");
		}
	}

	public int SelectedTransportIndex
	{
		get
		{
			return _selected_transport_index;
		}
		set
		{
			_selected_transport_index = value;
			OnPropertyChanged("SelectedTransportIndex");
		}
	}

	public CodecItem SelectedCodec
	{
		get
		{
			return _selected_codec;
		}
		set
		{
			_selected_codec = value;
			OnPropertyChanged("SelectedCodec");
		}
	}

	public int SelectedCodecIndex
	{
		get
		{
			return _selected_codec_index;
		}
		set
		{
			_selected_codec_index = value;
			OnPropertyChanged("SelectedCodecIndex");
		}
	}

	public List<CodecItem> CodecList
	{
		get
		{
			return _codec_list;
		}
		set
		{
			_codec_list = value;
			OnPropertyChanged("CodecList");
			OnPropertyChanged("IsGamingModeAllowed");
		}
	}

	public Visibility DisconnectVisibility
	{
		get
		{
			return _disconnectVisibility;
		}
		set
		{
			if (_disconnectVisibility != value)
			{
				_disconnectVisibility = value;
				OnPropertyChanged("DisconnectVisibility");
			}
		}
	}

	public Visibility ConnectVisibility
	{
		get
		{
			return _connectVisibility;
		}
		set
		{
			if (_connectVisibility != value)
			{
				_connectVisibility = value;
				OnPropertyChanged("ConnectVisibility");
			}
		}
	}

	public int SelectedApplicationIndex
	{
		get
		{
			return _SelectedApplicationIndex;
		}
		set
		{
			_SelectedApplicationIndex = value;
			OnPropertyChanged("SelectedApplicationIndex");
			StateAndAudioQualityVisibility = ((value == 2) ? Visibility.Hidden : Visibility.Visible);
		}
	}

	public ResDictExt SelectedLanguage
	{
		get
		{
			return _selectedLanguage;
		}
		set
		{
			_selectedLanguage = value;
			Application.Current.Properties["SelectedLanguage"] = _selectedLanguage;
			OnPropertyChanged("SelectedLanguage");
		}
	}

	public ObservableCollection<ResDictExt> LanguageList => _LanguageList;

	public Visibility LogoVisibility
	{
		get
		{
			return _LogoVisibility;
		}
		set
		{
			_LogoVisibility = value;
			OnPropertyChanged("LogoVisibility");
		}
	}

	public Visibility AppSelectionVisibility
	{
		get
		{
			return _AppSelectionVisibility;
		}
		set
		{
			_AppSelectionVisibility = value;
			OnPropertyChanged("AppSelectionVisibility");
		}
	}

	public bool MenuDashboardEnabled
	{
		get
		{
			return _MenuDashboardEnabled;
		}
		set
		{
			_MenuDashboardEnabled = value;
			OnPropertyChanged("MenuDashboardEnabled");
		}
	}

	public bool MenuUpdateEnabled
	{
		get
		{
			return _MenuUpdateEnabled;
		}
		set
		{
			_MenuUpdateEnabled = value;
			OnPropertyChanged("MenuUpdateEnabled");
		}
	}

	public bool MenuSettingsEnabled
	{
		get
		{
			return _MenuSettingsEnabled;
		}
		set
		{
			_MenuSettingsEnabled = value;
			OnPropertyChanged("MenuSettingsEnabled");
		}
	}

	public Visibility DashboardMenuVisibility
	{
		get
		{
			return _DashboardMenuVisibility;
		}
		set
		{
			_DashboardMenuVisibility = value;
			OnPropertyChanged("DashboardMenuVisibility");
		}
	}

	public UserControl? UserInterface
	{
		get
		{
			return _userInterface;
		}
		set
		{
			SetEditable(editable: true);
			_userInterface = value;
			OnPropertyChanged("UserInterface");
		}
	}

	public bool SafeForEditing
	{
		get
		{
			return _SafeForEditing;
		}
		set
		{
			_SafeForEditing = value;
			OnPropertyChanged("SafeForEditing");
		}
	}

	public bool MenuPillEnabled
	{
		get
		{
			return _MenuPillEnabled;
		}
		set
		{
			_MenuPillEnabled = value;
			OnPropertyChanged("MenuPillEnabled");
		}
	}

	public bool bUseDefaultFileID
	{
		get
		{
			return _bUseDefaultFileID;
		}
		set
		{
			_bUseDefaultFileID = value;
			OnPropertyChanged("bUseDefaultFileID");
		}
	}

	public bool bForceTransfer
	{
		get
		{
			return _bForceTransfer;
		}
		set
		{
			_bForceTransfer = value;
			OnPropertyChanged("bForceTransfer");
		}
	}

	public List<string> RepoVersionList
	{
		get
		{
			return _RepoVersionList;
		}
		set
		{
			_RepoVersionList = value;
			OnPropertyChanged("RepoVersionList");
		}
	}

	public string SelectedRepoVersionString { get; set; }

	public string VariableDFUNotification
	{
		get
		{
			return variableDFUNotification;
		}
		set
		{
			variableDFUNotification = value;
			OnPropertyChanged("VariableDFUNotification");
		}
	}

	public string VariableDFUNotificationSub
	{
		get
		{
			return variableDFUNotificationSub;
		}
		set
		{
			variableDFUNotificationSub = value;
			OnPropertyChanged("VariableDFUNotificationSub");
		}
	}

	public string RepoVersionInfo
	{
		get
		{
			return _RepoVersionInfo;
		}
		set
		{
			_RepoVersionInfo = value;
			OnPropertyChanged("RepoVersionInfo");
		}
	}

	public string dongleimage_source
	{
		get
		{
			return _dongleimage_source;
		}
		set
		{
			_dongleimage_source = value;
			OnPropertyChanged("dongleimage_source");
		}
	}

	public Visibility dongleimage_visibility
	{
		get
		{
			return _dongleimage_visibility;
		}
		set
		{
			_dongleimage_visibility = value;
			OnPropertyChanged("dongleimage_visibility");
		}
	}

	public DongleDeviceExt SelectedDongle
	{
		get
		{
			return _SelectedDongle;
		}
		set
		{
			_SelectedDongle = value;
			if (_SelectedDongle == null || _SelectedDongle.usbDeviceDetailConfig == null)
			{
				SetAppSelectionMenuPill();
			}
			else
			{
				SetAppSelectionMenuPill(bVisible: true, _SelectedDongle.usbDeviceDetailConfig.dtype == _DEVICETYPE.DEV_BTD700, bEnableUpdate: true, bEnableSettings: true);
				DashboardMenuVisibility = ((_SelectedDongle.usbDeviceDetailConfig.dtype != _DEVICETYPE.DEV_BTD700) ? Visibility.Collapsed : Visibility.Visible);
			}
			OnPropertyChanged("SelectedDongle");
			OnPropertyChanged("DongleSpecificSettingsVisibility");
		}
	}

	public string strCurrFWVersionLabel
	{
		get
		{
			return _strCurrFWVersionLabel;
		}
		set
		{
			_strCurrFWVersionLabel = value;
			OnPropertyChanged("strCurrFWVersionLabel");
		}
	}

	public string FWVersionLabel
	{
		get
		{
			return _FWVersionLabel;
		}
		set
		{
			_FWVersionLabel = value;
			OnPropertyChanged("FWVersionLabel");
		}
	}

	public string AppVersionLabel
	{
		get
		{
			return _AppVersionLabel;
		}
		set
		{
			_AppVersionLabel = value;
			OnPropertyChanged("AppVersionLabel");
		}
	}

	public int SelectedMenu
	{
		get
		{
			return _SelectedMenu;
		}
		set
		{
			_SelectedMenu = value;
			OnPropertyChanged("SelectedMenu");
		}
	}

	public string ltxt2
	{
		get
		{
			return _ltxt2;
		}
		set
		{
			_ltxt2 = value;
			OnPropertyChanged("ltxt2");
		}
	}

	public string ltxt3_Text
	{
		get
		{
			return _ltxt3_text;
		}
		set
		{
			_ltxt3_text = value;
			OnPropertyChanged("ltxt3_Text");
		}
	}

	public SolidColorBrush ltxt3_background
	{
		get
		{
			return _ltxt3_background;
		}
		set
		{
			_ltxt3_background = value;
			OnPropertyChanged("ltxt3_background");
		}
	}

	public Visibility ltxt3_visibility
	{
		get
		{
			return _ltxt3_visibility;
		}
		set
		{
			_ltxt3_visibility = value;
			OnPropertyChanged("ltxt3_visibility");
		}
	}

	public string ltxt4
	{
		get
		{
			return _ltxt4;
		}
		set
		{
			_ltxt4 = value;
			OnPropertyChanged("ltxt4");
		}
	}

	public string ltxt6_Text
	{
		get
		{
			return _ltxt6_text;
		}
		set
		{
			_ltxt6_text = value;
			OnPropertyChanged("ltxt6_Text");
		}
	}

	public HorizontalAlignment ltxt6_Location
	{
		get
		{
			return _ltxt6_location;
		}
		set
		{
			_ltxt6_location = value;
			OnPropertyChanged("ltxt6_Location");
		}
	}

	public Visibility ltxt6_Visible
	{
		get
		{
			return _ltxt6_Visible;
		}
		set
		{
			_ltxt6_Visible = value;
			OnPropertyChanged("ltxt6_Visible");
			OnPropertyChanged("ltxt6B_Visible");
		}
	}

	public Visibility ltxt6B_Visible
	{
		get
		{
			if (_ltxt6_Visible != 0)
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}

	public string ltxt7_Text
	{
		get
		{
			return _ltxt7_text;
		}
		set
		{
			_ltxt7_text = value;
			OnPropertyChanged("ltxt7_Text");
		}
	}

	public Visibility ltxt7_Visible
	{
		get
		{
			return _ltxt7_Visible;
		}
		set
		{
			_ltxt7_Visible = value;
			OnPropertyChanged("ltxt7_Visible");
		}
	}

	public Visibility DongleNotFoundVisibility
	{
		get
		{
			return _DongleNotFoundVisibility;
		}
		set
		{
			_DongleNotFoundVisibility = value;
			OnPropertyChanged("DongleNotFoundVisibility");
		}
	}

	public Visibility DongleSpecificSettingsVisibility
	{
		get
		{
			if (SelectedDongle != null && SelectedDongle.usbDeviceDetailConfig != null && SelectedDongle.usbDeviceDetailConfig.dtype == _DEVICETYPE.DEV_BTD700)
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}

	public string pbTransfer_Value
	{
		get
		{
			return _pbTransfer_Value;
		}
		set
		{
			_pbTransfer_Value = value;
			OnPropertyChanged("pbTransfer_Value");
		}
	}

	public Visibility pbTransfer_Visible
	{
		get
		{
			return _pbTransfer_Visible;
		}
		set
		{
			_pbTransfer_Visible = value;
			OnPropertyChanged("pbTransfer_Visible");
		}
	}

	public _EVENT bt3Act
	{
		get
		{
			return _bt3Act;
		}
		set
		{
			_bt3Act = value;
			OnPropertyChanged("bt3Act");
		}
	}

	public string bt3_Text
	{
		get
		{
			return _bt3_text;
		}
		set
		{
			_bt3_text = value;
			OnPropertyChanged("bt3_Text");
		}
	}

	public Visibility bt3_Visible
	{
		get
		{
			return _bt3_Visible;
		}
		set
		{
			_bt3_Visible = value;
			OnPropertyChanged("bt3_Visible");
		}
	}

	public _EVENT bt2Act
	{
		get
		{
			return _bt2Act;
		}
		set
		{
			_bt2Act = value;
			OnPropertyChanged("bt2Act");
		}
	}

	public string bt2_Text
	{
		get
		{
			return _bt2_text;
		}
		set
		{
			_bt2_text = value;
			OnPropertyChanged("bt2_Text");
		}
	}

	public Visibility bt2_Visible
	{
		get
		{
			return _bt2_Visible;
		}
		set
		{
			_bt2_Visible = value;
			OnPropertyChanged("bt2_Visible");
		}
	}

	public _EVENT bt1Act
	{
		get
		{
			return _bt1Act;
		}
		set
		{
			_bt1Act = value;
			OnPropertyChanged("bt1Act");
		}
	}

	public string bt1_Text
	{
		get
		{
			return _bt1_text;
		}
		set
		{
			_bt1_text = value;
			OnPropertyChanged("bt1_Text");
		}
	}

	public Visibility bt1_Visible
	{
		get
		{
			return _bt1_Visible;
		}
		set
		{
			_bt1_Visible = value;
			OnPropertyChanged("bt1_Visible");
		}
	}

	public ObservableCollection<DongleDeviceExt> DongleDevices
	{
		get
		{
			return _DongleDevices;
		}
		set
		{
			_DongleDevices = value;
			OnPropertyChanged("DongleDevices");
		}
	}

	public void ModeSetBackoffFor(int sec)
	{
		if (sec <= 1 || (_timer != null && _timer.IsEnabled))
		{
			return;
		}
		_time = TimeSpan.FromSeconds(sec);
		_timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
		{
			if (_time == TimeSpan.Zero)
			{
				ApplicationSelectionEnabled = true;
				_timer.Stop();
			}
			_time = _time.Add(TimeSpan.FromSeconds(-1.0));
		}, Application.Current.Dispatcher);
		_timer.Start();
	}

	public void CancelModeSetBackoff()
	{
		ApplicationSelectionEnabled = true;
		if (_timer != null)
		{
			_timer.Stop();
		}
	}

	public void RefreshBTD700Context()
	{
		try
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				OnPropertyChanged("btd700Ctx");
				OnPropertyChanged("broadcastName");
			});
		}
		catch (Exception)
		{
		}
	}

	public void ValidateBroadcastName()
	{
		OnPropertyChanged("IsBroadcastNameValid");
	}

	public void WriteBroadcastName()
	{
		btd700Ctx.broadcastName = broadcastName;
		UI_UserResp(_EVENT.BT_ABS_BTD700WRITEBCASTNAME);
	}

	public void SetBroadcastQuality(byte q)
	{
		switch (q)
		{
		case 0:
			BroadcastQuality = "Standard Quality, 16kHz";
			SelectedBroadcastQualityIndex = 0;
			break;
		case 1:
			BroadcastQuality = "Standard Quality, 24kHz";
			SelectedBroadcastQualityIndex = 1;
			break;
		case 2:
			BroadcastQuality = "High Quality, 48kHz";
			SelectedBroadcastQualityIndex = 2;
			break;
		default:
			BroadcastQuality = "";
			SelectedBroadcastQualityIndex = -1;
			break;
		}
	}

	public void ValidateBroadcastKey()
	{
		OnPropertyChanged("IsBroadcastKeyValid");
	}

	public void SetDongleStateUI(byte dstate)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			dstate_visibility_1 = Visibility.Hidden;
			dstate_visibility_2 = Visibility.Hidden;
			dstate_visibility_3 = Visibility.Hidden;
			dstate_visibility_4 = Visibility.Hidden;
			switch ((_BTD700_STATES)dstate)
			{
			case _BTD700_STATES.S_NONE:
				dstate_visibility_1 = Visibility.Visible;
				DisconnectVisibility = Visibility.Collapsed;
				ConnectVisibility = Visibility.Visible;
				AudioQualityVisibility = Visibility.Hidden;
				break;
			case _BTD700_STATES.S_DISCONNECTED:
				dstate_visibility_1 = Visibility.Visible;
				DisconnectVisibility = Visibility.Collapsed;
				ConnectVisibility = Visibility.Visible;
				AudioQualityVisibility = Visibility.Hidden;
				break;
			case _BTD700_STATES.S_CONNECTED:
				dstate_visibility_2 = Visibility.Visible;
				DisconnectVisibility = Visibility.Visible;
				ConnectVisibility = Visibility.Collapsed;
				if (!bBroadcast)
				{
					AudioQualityVisibility = Visibility.Visible;
				}
				break;
			case _BTD700_STATES.S_STREAMING_AUDIO:
				dstate_visibility_3 = Visibility.Visible;
				DisconnectVisibility = Visibility.Visible;
				ConnectVisibility = Visibility.Collapsed;
				if (!bBroadcast)
				{
					AudioQualityVisibility = Visibility.Visible;
				}
				break;
			case _BTD700_STATES.S_STREAMING_VOICE:
				dstate_visibility_4 = Visibility.Visible;
				DisconnectVisibility = Visibility.Visible;
				ConnectVisibility = Visibility.Collapsed;
				if (!bBroadcast)
				{
					AudioQualityVisibility = Visibility.Visible;
				}
				break;
			}
			SetCenterPanel(SelectedApplication);
			OnPropertyChanged("dstate_visibility_1");
			OnPropertyChanged("dstate_visibility_2");
			OnPropertyChanged("dstate_visibility_3");
			OnPropertyChanged("dstate_visibility_4");
		});
	}

	public void RefreshGamingModeItem()
	{
		OnPropertyChanged("IsGamingModeAllowed");
	}

	public void SetAudioQuality(byte res, byte freq)
	{
		string text = "";
		if (Enum.IsDefined(typeof(_BTD700_AUDIO_RESOLUTION), (int)res) && Enum.IsDefined(typeof(_BTD700_AUDIO_FREQUENCY), (int)freq))
		{
			btd700Ctx.audioResolution = (_BTD700_AUDIO_RESOLUTION)res;
			btd700Ctx.audioFrequency = (_BTD700_AUDIO_FREQUENCY)freq;
			text = res switch
			{
				2 => "24bit", 
				1 => "16bit", 
				_ => "", 
			};
			if (text != "")
			{
				text += ", ";
			}
			text += freq switch
			{
				3 => "96kHz", 
				2 => "48kHz", 
				1 => "44.1kHz", 
				_ => "", 
			};
		}
		audio_quality = text;
	}

	public void SetCenterPanel(int appidx)
	{
		WarningPanelVisibility = Visibility.Hidden;
		TransportPanelVisibility = Visibility.Collapsed;
		CodecPanelVisibility = Visibility.Hidden;
		BroadcastPanelVisibility = Visibility.Hidden;
		switch (appidx)
		{
		case 0:
			if (btd700Ctx.state.Equals(_BTD700_STATES.S_CONNECTED) || btd700Ctx.state.Equals(_BTD700_STATES.S_STREAMING_AUDIO) || btd700Ctx.state.Equals(_BTD700_STATES.S_STREAMING_VOICE))
			{
				WarningPanelVisibility = Visibility.Hidden;
				if (btd700Ctx.state.Equals(_BTD700_STATES.S_STREAMING_VOICE))
				{
					CodecPanelVisibility = Visibility.Hidden;
					break;
				}
				TransportPanelVisibility = Visibility.Visible;
				CodecPanelVisibility = Visibility.Visible;
			}
			else
			{
				WarningPanelVisibility = Visibility.Visible;
				CodecPanelVisibility = Visibility.Hidden;
			}
			break;
		case 1:
			if (btd700Ctx.state.Equals(_BTD700_STATES.S_CONNECTED) || btd700Ctx.state.Equals(_BTD700_STATES.S_STREAMING_AUDIO) || btd700Ctx.state.Equals(_BTD700_STATES.S_STREAMING_VOICE))
			{
				WarningPanelVisibility = Visibility.Hidden;
				if (btd700Ctx.state.Equals(_BTD700_STATES.S_STREAMING_VOICE))
				{
					CodecPanelVisibility = Visibility.Hidden;
					break;
				}
				TransportPanelVisibility = Visibility.Visible;
				CodecPanelVisibility = Visibility.Visible;
			}
			else
			{
				WarningPanelVisibility = Visibility.Visible;
				CodecPanelVisibility = Visibility.Hidden;
			}
			break;
		case 2:
			WarningPanelVisibility = Visibility.Hidden;
			BroadcastPanelVisibility = Visibility.Visible;
			break;
		}
		OnPropertyChanged("WarningPanelVisibility");
		OnPropertyChanged("propertylabel_visibility");
		OnPropertyChanged("TransportPanelVisibility");
		OnPropertyChanged("CodecPanelVisibility");
		OnPropertyChanged("BroadcastPanelVisibility");
	}

	public int GetTransportInUseListIdx(byte val)
	{
		int num = -1;
		foreach (TransportItem item in _transport_list)
		{
			num++;
			if (item.value == val)
			{
				break;
			}
		}
		return num;
	}

	public int GetCodecInUseListIdx(int bitidx)
	{
		int num = -1;
		foreach (CodecItem item in _codec_list)
		{
			num++;
			if (item.bitidx == bitidx)
			{
				break;
			}
		}
		return num;
	}

	public void Btd700View_InitUI()
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
		});
	}

	public void Btd700View_SelectApplication(int idx)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			btd700Ctx.iSelectedApplication = idx;
			SelectedApplicationIndex = idx;
			SelectedApplication = idx;
			switch (idx)
			{
			case 0:
				bOneToOne = true;
				bGaming = false;
				bBroadcast = false;
				break;
			case 1:
				bOneToOne = false;
				bGaming = true;
				bBroadcast = false;
				break;
			case 2:
				bOneToOne = false;
				bGaming = false;
				bBroadcast = true;
				break;
			}
			AudioQualityVisibility = ((bBroadcast || btd700Ctx.state == _BTD700_STATES.S_DISCONNECTED || btd700Ctx.state == _BTD700_STATES.S_NONE) ? Visibility.Hidden : Visibility.Visible);
			SetCenterPanel(idx);
			OnPropertyChanged("IsGamingModeAllowed");
		});
	}

	public void Btd700View_SelectTransport(byte transport)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			bSendAudioAndTransportCmd = false;
			SelectedTransportIndex = ((transport == 0) ? (-1) : GetTransportInUseListIdx(transport));
			if (Btd700View != null && SelectedTransportIndex != -1 && TransportList != null && TransportList.Count > SelectedTransportIndex)
			{
				SelectedTransport = TransportList[SelectedTransportIndex];
			}
		});
	}

	public byte Btd700View_GetSelectedTransportAsByte()
	{
		return (byte)((SelectedTransportIndex != -1) ? SelectedTransport.value : 0);
	}

	public byte Btd700View_GetSinkTransportListAsByte()
	{
		byte b = 0;
		foreach (TransportItem transport in TransportList)
		{
			b |= transport.value;
		}
		return b;
	}

	public void Btd700View_SelectCodec(int bitidx)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			bSendCodecSelectionCmd = false;
			SelectedCodecIndex = ((bitidx == -1) ? (-1) : GetCodecInUseListIdx(bitidx));
			if (Btd700View != null)
			{
				if (SelectedCodecIndex != -1 && CodecList != null && CodecList.Count > SelectedCodecIndex)
				{
					SelectedCodec = CodecList[SelectedCodecIndex];
				}
				else
				{
					SelectedCodec = new CodecItem(" --- ", -1);
				}
			}
			bSendCodecSelectionCmd = true;
		});
	}

	public byte Btd700View_GetSelectedCodecAsByte()
	{
		return (byte)((SelectedCodecIndex != -1) ? ((uint)(1 << SelectedCodec.bitidx)) : 0u);
	}

	public void Btd700View_SetCodecSelectButtonEnabled(bool bEnable)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			if (Btd700View != null)
			{
				Btd700View.CodecSelectButton.IsEnabled = bEnable;
			}
		});
	}

	public void Btd700View_UpdateBroadcastActionButtonState()
	{
		try
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				bool enableBtDiscardBcastSettings = (EnableBtSaveBcastSettings = btd700Ctx.BcastContextIsDifferentFrom(btd700BcastCtxOriginal, out _BroadcastQualityChanged, out _BroadcastPBPChanged, out _BroadcastNameChanged, out _BroadcastPasswordChanged));
				EnableBtDiscardBcastSettings = enableBtDiscardBcastSettings;
				if (EnableBtSaveBcastSettings && (!IsBroadcastNameValid || !IsBroadcastKeyValid))
				{
					EnableBtSaveBcastSettings = false;
				}
				OnPropertyChanged("BroadcastQualityChanged");
				OnPropertyChanged("BroadcastPBPChanged");
				OnPropertyChanged("BroadcastNameChanged");
				OnPropertyChanged("BroadcastPasswordChanged");
				OnPropertyChanged("EnableBtDiscardBcastSettings");
				OnPropertyChanged("EnableBtSaveBcastSettings");
			});
		}
		catch (Exception)
		{
		}
	}

	public void Btd700View_ResetChangeFlags()
	{
		try
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				BroadcastQualityChanged = false;
				BroadcastPBPChanged = false;
				BroadcastNameChanged = false;
				BroadcastPasswordChanged = false;
			});
		}
		catch (Exception)
		{
		}
	}

	[DllImport("herve.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern int uw(byte[] buf, int buflen);

	[DllImport("kernel32", SetLastError = true)]
	private static extern bool FreeLibrary(nint hModule);

	public MainAppWindowViewModel(MainAppWindow v, int i, char c, bool a, byte[] dfuid)
	{
		view = v;
		iVerboseLevel = i;
		cRepoURLBase = c;
		try
		{
			string uwstub = "herve.dll";
			string name = (from s in Assembly.GetExecutingAssembly().GetManifestResourceNames()
				where s.IndexOf(uwstub) > 0
				select s).ToArray().First();
			Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
			FileStream fileStream = new FileStream(uwstub, FileMode.Create);
			for (int j = 0; j < manifestResourceStream.Length; j++)
			{
				fileStream.WriteByte((byte)manifestResourceStream.ReadByte());
			}
			fileStream.Close();
			byte[] array = new byte[384];
			uw(array, array.Length);
			string[] array2 = Encoding.UTF8.GetString(array).Replace("\0", "").Split("#");
			for (int k = 0; k < array2.Length; k++)
			{
				string[] array3 = array2[k].Split("|");
				string[] array4 = array3[0].Split(",");
				if (array4[0][0] == 'P')
				{
					RepoHandler.SetBaseURL(array3[1]);
					if (array4[1].Trim().Length > 0 && array4[2].Trim().Length > 0)
					{
						RepoHandler.httpClient.DefaultRequestHeaders.Add(array4[1], array4[2]);
					}
				}
			}
			ProcessModule processModule = ((IEnumerable)Process.GetCurrentProcess().Modules).OfType<ProcessModule>().FirstOrDefault((ProcessModule x) => x.ModuleName == uwstub);
			if (processModule != null)
			{
				FreeLibrary(processModule.BaseAddress);
			}
			File.Delete(uwstub);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message);
		}
		if (bUseDefaultFileID = dfuid != null)
		{
			Array.Copy(dfuid, UniqueFileID, 4);
		}
		RepoHandler.SetCaller(this);
		UsbHid_Init();
		UI_Init();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			disposedValue = true;
		}
	}

	public void ResetEventQueue()
	{
		if (_events == null)
		{
			_events = new Queue<EventObj>();
		}
		else
		{
			_events.Clear();
		}
	}

	public void PostEvent(_EVENT e, object? o = null)
	{
		_events.Enqueue(new EventObj(e, o));
	}

	public void RepostEvent(_EVENT e, object? o = null)
	{
		PostEvent(e, o);
	}

	public bool GetEvent(out _EVENT e, out object o)
	{
		EventObj eventObj = new EventObj();
		bool result = false;
		if (_events.Count > 0)
		{
			try
			{
				eventObj = _events.Dequeue();
				result = true;
			}
			catch (Exception)
			{
				eventObj = null;
			}
		}
		if (eventObj == null)
		{
			eventObj = new EventObj();
			result = false;
		}
		e = eventObj.e;
		o = eventObj.o;
		return result;
	}

	public List<string> ShowAllEvents()
	{
		List<string> list = new List<string>();
		foreach (EventObj @event in _events)
		{
			list.Add(@event.e.ToString());
		}
		return list;
	}

	public void FSM_InitAndStart()
	{
		RefreshDongleDevices();
		dfufile = new DFUFile();
		enFSMState = _STATES.FSM_LICENSEAGREEMENT;
		usSubstate = 0;
		strMainProc = Process.GetCurrentProcess().ProcessName;
		threadMain = new Thread(FSM_ThreadProc);
		threadMain.Start();
	}

	private void FSM_Exit()
	{
		if (enFSMState == _STATES.FSM_TERMINATE)
		{
			Dispose(disposing: true);
		}
	}

	private void ResetButtonAndChangeState(_STATES newState)
	{
		enFSMPrevState = enFSMState;
		enFSMState = newState;
		usSubstate = 0;
		iLoopCounter = 0;
		swElapsedTimeInSubstate.Reset();
	}

	public bool IsUpdateOngoing()
	{
		bool result = false;
		_STATES sTATES = enFSMState;
		if ((uint)(sTATES - 12) <= 5u)
		{
			result = true;
		}
		return result;
	}

	private void abortTransfer()
	{
		dfufile.unsetFile();
		iTransferState = 0;
		usSubstate = 0;
		enFSMState = _STATES.FSM_ABORT;
		enFSMPreAbortState = _STATES.FSM_INIT;
	}

	private void abortTransfer(_STATES nextState)
	{
		dfufile.unsetFile();
		iTransferState = 0;
		usSubstate = 0;
		enFSMState = _STATES.FSM_ABORT;
		enFSMPreAbortState = nextState;
	}

	public void SwitchSubstate(ushort newSubstate, ushort returnToSubstate = 255)
	{
		usSubstate = newSubstate;
		usNextSubstate = returnToSubstate;
		swElapsedTimeInSubstate.Reset();
		swElapsedTimeInSubstate.Start();
	}

	public long MSecInSubstate()
	{
		return swElapsedTimeInSubstate.ElapsedMilliseconds;
	}

	public void FSM_ThreadProc()
	{
		_EVENT e = _EVENT.NONE;
		object o = null;
		byte b = byte.MaxValue;
		bool flag = false;
		bool flag2 = false;
		uint num = 0u;
		long num2 = 0L;
		long num3 = 0L;
		byte[] array = new byte[1];
		byte[] respdata = new byte[64];
		ushort num4 = 0;
		ushort num5 = 0;
		_REASON rEASON = _REASON.None;
		int num6 = 0;
		Process[] processesByName;
		do
		{
			byte[] respdata2;
			switch (enFSMState)
			{
			case _STATES.FSM_LICENSEAGREEMENT:
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_EXIT:
						ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
						break;
					case _EVENT.BT_NEXT:
						SetUserInterface(DongleWaitView);
						ShowDongleNotFoundLabel(bVisibility: true);
						ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLE);
						break;
					}
				}
				else
				{
					Thread.Sleep(200);
				}
				break;
			case _STATES.FSM_WAITFORDONGLE:
			{
				if (GetEvent(out e, out o))
				{
					if (e == _EVENT.BT_EXIT)
					{
						ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
					}
					break;
				}
				int num10 = 0;
				foreach (DongleDeviceExt item in DongleDevices.Where((DongleDeviceExt d) => d.IsDetected).ToList())
				{
					num10 += item.count;
				}
				switch (num10)
				{
				case 0:
					Thread.Sleep(200);
					break;
				case 1:
				{
					List<DongleDeviceExt> list = DongleDevices.Where((DongleDeviceExt d) => d.IsDetected).ToList();
					if (list != null && list.Count() > 0)
					{
						DongleDeviceExt dongleDeviceExt = list.First();
						if (dongleDeviceExt.IsConnected && dongleDeviceExt.IsDeviceValid(HIDDEVICETYPES.MAIN))
						{
							SelectedDongle = list.First();
							DisplayDFUNotificationPanel();
							setMainAndSubLabel(SelectedDongle.DongleName, SelectedDongle.DongleVersion, bUseBinding: false);
							ResetButtonAndChangeState(_STATES.FSM_GETDONGLEFWVERSION);
						}
						else
						{
							modalToastBox(SelectedLanguage.ltxt_Error, SelectedLanguage.ltxt2_NotFound, SelectedLanguage.btntxt_TryAgain);
							list.Remove(dongleDeviceExt);
						}
					}
					break;
				}
				default:
					ShowDongleNotFoundLabel(bVisibility: true);
					modalToastBox(SelectedLanguage.ltxt2_MultipleDongle, SelectedLanguage.ltxt_PleaseRemoveAllDongles, SelectedLanguage.btntxt_Close);
					RefreshDongleDevices();
					break;
				}
				break;
			}
			case _STATES.FSM_CHOOSEDONGLE:
				if (!GetEvent(out e, out o))
				{
					break;
				}
				switch (e)
				{
				case _EVENT.BT_EXIT:
					ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
					break;
				case _EVENT.DEV_ATTACH:
					if (o is DongleDeviceExt dongleDeviceExt2)
					{
						if (!dongleDeviceExt2.UIUpdated)
						{
							dongleDeviceExt2.DongleVersion = "---";
							dongleDeviceExt2.UIUpdated = true;
						}
						DongleDevices.IndexOf(dongleDeviceExt2);
					}
					break;
				case _EVENT.DEV_DETACH:
					switch (DongleDevices.Where((DongleDeviceExt d) => d.IsDetected).ToList().Count())
					{
					case 0:
						setMainAndSubLabel("ltxt2_NotFound", "ltxt4_Searching");
						SetUserInterface(DongleWaitView);
						ShowDongleNotFoundLabel(bVisibility: true);
						ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLE);
						break;
					case 1:
						SelectedDongle = DongleDevices.Where((DongleDeviceExt d) => d.IsDetected).ToList().First();
						PostEvent(_EVENT.BT_ABS_DONGLESELECT);
						break;
					}
					break;
				case _EVENT.BT_ABS_DONGLESELECT:
					DisplayDFUNotificationPanel();
					setMainAndSubLabel(SelectedDongle.DongleName, SelectedDongle.DongleVersion, bUseBinding: false);
					ResetButtonAndChangeState(_STATES.FSM_GETDONGLEFWVERSION);
					break;
				}
				break;
			case _STATES.FSM_DEVICESPECIFICFEATURES:
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_EXIT:
						ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
						break;
					case _EVENT.BT_MENUUPDATE:
						ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
						break;
					case _EVENT.BT_MENUSETTINGS:
						ResetButtonAndChangeState(_STATES.FSM_SETTINGSDISPLAY);
						break;
					case _EVENT.BT_ABS_BTD700WRITEBCASTNAME:
						if (usSubstate == 12)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastName, btd700Ctx.broadcastName), bNeedResponse: true);
							SwitchSubstate(254, 8);
						}
						else
						{
							RepostEvent(e, o);
						}
						break;
					case _EVENT.BT_ABS_BTD700WRITEBCASTKEY:
						if (usSubstate == 12)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastEncryptKey, btd700Ctx.broadcastEncKey), bNeedResponse: true);
							SwitchSubstate(254, 10);
						}
						else
						{
							RepostEvent(e, o);
						}
						break;
					case _EVENT.BT_ABS_BTD700CHANGEBCASTSTATE:
						if (usSubstate == 12)
						{
							if (BroadcastPBPChanged)
							{
								SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastInfo, btd700BcastCtxOriginal.broadcastInfoAsByteArray), bNeedResponse: true);
								SwitchSubstate(254, 14);
							}
							else if (BroadcastPasswordChanged)
							{
								SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastEncryptKey, btd700Ctx.broadcastEncKey), bNeedResponse: true);
								SwitchSubstate(254, 14);
							}
							else if (BroadcastNameChanged)
							{
								SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastName, btd700Ctx.broadcastName), bNeedResponse: true);
								SwitchSubstate(254, 14);
							}
							else if (BroadcastQualityChanged || BroadcastEncryptionChanged)
							{
								SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastInfo, btd700Ctx.broadcastInfoAsByteArray), bNeedResponse: true);
								SwitchSubstate(254, 14);
							}
						}
						else
						{
							RepostEvent(e, o);
						}
						break;
					case _EVENT.BT_ABS_BTD700CHANGEBCASTENCRYPTION:
						if (usSubstate == 12)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastInfo, btd700Ctx.broadcastInfoAsByteArray), bNeedResponse: true);
							SwitchSubstate(254, 6);
						}
						else
						{
							RepostEvent(e, o);
						}
						break;
					case _EVENT.BT_ABS_BTD700SETAUDIOANDTRANSPORT:
						if (usSubstate == 12)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetAudioModeAndTransport, o as byte[]), bNeedResponse: true);
							SwitchSubstate(254, 0);
						}
						else
						{
							RepostEvent(e, o);
						}
						break;
					case _EVENT.BT_ABS_BTD700SETCODECTOUSE:
						if (usSubstate == 12)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetCodecToUse, o as byte[]), bNeedResponse: true);
							btd700Ctx.u16UsedCodecs = 0;
							SwitchSubstate(254, 4);
						}
						else
						{
							RepostEvent(e, o);
						}
						break;
					case _EVENT.BT_ABS_BTD700FACTORYRESET:
						if (usSubstate == 12)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetFactoryReset), bNeedResponse: true);
							Thread.Sleep(5000);
							ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
							PostEvent(_EVENT.BT_NEXT);
						}
						else
						{
							RepostEvent(e, o);
						}
						break;
					case _EVENT.BT_ABS_BTD700DISCONNECT:
						if (usSubstate == 12)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBTConnect, new byte[1]), bNeedResponse: true);
							SwitchSubstate(254, 12);
						}
						else
						{
							RepostEvent(e, o);
						}
						break;
					case _EVENT.BT_ABS_BTD700CONNECT:
						if (usSubstate == 12)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBTConnect, new byte[1] { 1 }), bNeedResponse: true);
							SwitchSubstate(254, 12);
						}
						else
						{
							RepostEvent(e, o);
						}
						break;
					case _EVENT.DEV_DETACH:
						if ((o as DongleDeviceExt).Equals(SelectedDongle))
						{
							addStatusEntry("Transmitter unplugged - BTD700 state is " + _BTD700_STATES.S_NONE);
							SetDongleStateUI(1);
							btd700Ctx.state = _BTD700_STATES.S_DISCONNECTED;
							ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
							PostEvent(_EVENT.BT_NEXT);
							SelectedDongle = new DongleDeviceExt();
							FWVersionLabel = "";
						}
						break;
					}
				}
				if (enFSMState != _STATES.FSM_DEVICESPECIFICFEATURES)
				{
					break;
				}
				switch (usSubstate)
				{
				case 254:
				case 255:
					if (!SelectedDongle.ProcessAppIncoming(HIDDEVICETYPES.APP2, out respdata) || respdata[0] != SelectedDongle.GetHDE(HIDDEVICETYPES.APP2).ReportId)
					{
						break;
					}
					switch (respdata[1])
					{
					case byte.MaxValue:
						SwitchSubstate(usNextSubstate, 255);
						break;
					case 252:
						switch (respdata[2])
						{
						case 2:
							UpdateAudioModeAndTransport(respdata);
							break;
						case 3:
							UpdateSupportedCodecs(respdata);
							break;
						case 4:
							UpdateCodecInUse(respdata);
							break;
						case 15:
							UpdateDongleState(respdata);
							break;
						case 16:
							UpdateLEAudioState(respdata);
							break;
						case 17:
							SetAudioQuality(respdata[4], respdata[5]);
							break;
						case 22:
							UpdateSinkSupportTransport(respdata);
							break;
						case 23:
							UpdateGamingAvailableStatus(respdata[4]);
							break;
						}
						SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostResp((_BTD700_DONGLECMD)respdata[2]), bNeedResponse: true);
						break;
					}
					break;
				case 0:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetDongleState), bNeedResponse: true);
					num5 = 101;
					SwitchSubstate(255, 1);
					break;
				case 1:
					if (respdata[2] == 6)
					{
						SetDongleStateUI(respdata[4]);
						_BTD700_STATES bTD700_STATES = (_BTD700_STATES)respdata[4];
						addStatusEntry("BTD700 state is " + bTD700_STATES);
						btd700Ctx.state = (_BTD700_STATES)respdata[4];
						if (btd700Ctx.state == _BTD700_STATES.S_CONNECTED || btd700Ctx.state == _BTD700_STATES.S_STREAMING_AUDIO || btd700Ctx.state == _BTD700_STATES.S_STREAMING_VOICE)
						{
							SwitchSubstate(20, 255);
						}
						else
						{
							SwitchSubstate(2, 255);
						}
						num5 = 0;
					}
					else
					{
						SetDongleStateUI(0);
						btd700Ctx.state = _BTD700_STATES.S_NONE;
						SwitchSubstate(num5++, 255);
					}
					break;
				case 101:
				case 102:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetDongleState), bNeedResponse: true);
					SwitchSubstate(255, 1);
					break;
				case 103:
					num5 = 0;
					SwitchSubstate(12, 255);
					break;
				case 20:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetSinkSupportTransport), bNeedResponse: true);
					SwitchSubstate(255, 21);
					break;
				case 21:
					if (respdata[2] == 21)
					{
						UpdateSinkSupportTransport(respdata);
						SwitchSubstate(2, 255);
					}
					else
					{
						SwitchSubstate(20, 255);
					}
					break;
				case 2:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetAudioModeAndTransport), bNeedResponse: true);
					num5 = 301;
					SwitchSubstate(255, 3);
					break;
				case 3:
					if (respdata[2] == 1)
					{
						UpdateAudioModeAndTransport(respdata);
						num5 = 0;
						SwitchSubstate(30, 255);
					}
					else
					{
						SwitchSubstate(num5++, 255);
					}
					break;
				case 301:
				case 302:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetAudioModeAndTransport), bNeedResponse: true);
					SwitchSubstate(255, 3);
					break;
				case 303:
					num5 = 0;
					SwitchSubstate(30, 255);
					break;
				case 30:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetAudioQuality), bNeedResponse: true);
					SwitchSubstate(255, 31);
					break;
				case 31:
					if (respdata[2] == 8)
					{
						SetAudioQuality(respdata[4], respdata[5]);
					}
					SwitchSubstate(4, 255);
					break;
				case 4:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetSupportedCodec), bNeedResponse: true);
					SwitchSubstate(255, 5);
					break;
				case 5:
					if (respdata[2] == 3)
					{
						UpdateSupportedCodecs(respdata);
					}
					SwitchSubstate(50, 255);
					break;
				case 50:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetCodecInUse), bNeedResponse: true);
					SwitchSubstate(255, 51);
					break;
				case 51:
					if (respdata[2] == 5)
					{
						btd700Ctx.u16UsedCodecs = (ushort)(respdata[4] & 0xFFu);
						Btd700View_SelectCodec(btd700Ctx.GetCodecInUseIdx());
						addStatusEntry("BTD700 codec in-use " + btd700Ctx.GetCodecInUseName());
					}
					SwitchSubstate(6, 255);
					break;
				case 6:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetBroadcastInfo), bNeedResponse: true);
					SwitchSubstate(255, 7);
					break;
				case 7:
					if (respdata[2] == 9)
					{
						btd700BcastCtxOriginal.SetBroadcastInfo(respdata[4], respdata[5], respdata[6]);
						SetBroadcastQuality((byte)(btd700Ctx.broadcastQuality = (_BTD700_BROADCAST_QUALITY)respdata[5]));
						BroadcastState = respdata[4] == 1;
						BroadcastEncryption = respdata[6] == 1;
						string[] obj = new string[6] { "BTD700 broadcast ", null, null, null, null, null };
						_BTD700_BROADCAST_STATE bTD700_BROADCAST_STATE = (_BTD700_BROADCAST_STATE)respdata[4];
						obj[1] = bTD700_BROADCAST_STATE.ToString();
						obj[2] = " ";
						_BTD700_BROADCAST_QUALITY bTD700_BROADCAST_QUALITY = (_BTD700_BROADCAST_QUALITY)respdata[5];
						obj[3] = bTD700_BROADCAST_QUALITY.ToString();
						obj[4] = " ";
						_BTD700_BROADCAST_ENCRYPTION bTD700_BROADCAST_ENCRYPTION = (_BTD700_BROADCAST_ENCRYPTION)respdata[6];
						obj[5] = bTD700_BROADCAST_ENCRYPTION.ToString();
						addStatusEntry(string.Concat(obj));
						if (btd700BcastCtxBackup.bBackupValid)
						{
							btd700Ctx.ImportBroadcastContext(btd700BcastCtxBackup);
							SetBroadcastQuality((byte)btd700Ctx.broadcastQuality);
							BroadcastEncryption = btd700Ctx.broadcastEncrypt == _BTD700_BROADCAST_ENCRYPTION.BCAST_ENCR_ON;
						}
						Btd700View_UpdateBroadcastActionButtonState();
					}
					SwitchSubstate(8, 255);
					break;
				case 8:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetBroadcastName), bNeedResponse: true);
					SwitchSubstate(255, 9);
					break;
				case 9:
					if (respdata[2] == 13)
					{
						broadcastName = respdata.Skip(4).Take(respdata[3]).ToArray();
						btd700BcastCtxOriginal.SetBroadcastName(broadcastName);
						btd700Ctx.broadcastName = broadcastName;
						if (btd700BcastCtxBackup.bBackupValid)
						{
							btd700Ctx.ImportBroadcastContext(btd700BcastCtxBackup);
							broadcastName = btd700BcastCtxBackup.broadcastName;
						}
						Btd700View_UpdateBroadcastActionButtonState();
					}
					SwitchSubstate(10, 255);
					break;
				case 10:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetBroadcastEncryptKey), bNeedResponse: true);
					SwitchSubstate(255, 11);
					break;
				case 11:
					if (respdata[2] == 11)
					{
						broadcastEncKey = respdata.Skip(4).Take(respdata[3]).ToArray();
						btd700BcastCtxOriginal.SetBroadcastEncKey(broadcastEncKey);
						if (btd700BcastCtxBackup.bBackupValid)
						{
							btd700Ctx.ImportBroadcastContext(btd700BcastCtxBackup);
							broadcastEncKey = btd700BcastCtxBackup.broadcastEncKey;
						}
						Btd700View_UpdateBroadcastActionButtonState();
						RefreshBTD700Context();
						BroadcastParamUIEnabled = true;
					}
					SwitchSubstate(12, 255);
					break;
				case 12:
					if (PollDurationSeconds == 0)
					{
						if (SelectedDongle.ProcessAppIncoming(HIDDEVICETYPES.APP2, out respdata) && respdata[0] == SelectedDongle.GetHDE(HIDDEVICETYPES.APP2).ReportId && respdata[1] == 252)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostResp((_BTD700_DONGLECMD)respdata[2]), bNeedResponse: true);
							switch (respdata[2])
							{
							case 2:
								UpdateAudioModeAndTransport(respdata);
								SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetAudioQuality), bNeedResponse: true);
								SwitchSubstate(255, 13);
								break;
							case 3:
								UpdateSupportedCodecs(respdata);
								break;
							case 4:
								UpdateCodecInUse(respdata);
								break;
							case 15:
								UpdateDongleState(respdata);
								SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetAudioModeAndTransport), bNeedResponse: true);
								SwitchSubstate(255, 13);
								break;
							case 16:
								UpdateLEAudioState(respdata);
								SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetAudioModeAndTransport), bNeedResponse: true);
								SwitchSubstate(255, 13);
								break;
							case 17:
								SetAudioQuality(respdata[4], respdata[5]);
								break;
							case 22:
								UpdateSinkSupportTransport(respdata);
								SwitchSubstate(2, 255);
								break;
							}
						}
					}
					else if (MSecInSubstate() > PollDurationSeconds * 1000)
					{
						SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetDongleState), bNeedResponse: true);
						SwitchSubstate(255, 13);
					}
					break;
				case 13:
					switch ((_BTD700_HOSTCMD)respdata[2])
					{
					case _BTD700_HOSTCMD.GetDongleState:
						UpdateDongleState(respdata);
						SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetAudioModeAndTransport), bNeedResponse: true);
						SwitchSubstate(255, 13);
						break;
					case _BTD700_HOSTCMD.GetAudioModeAndTransport:
						UpdateAudioModeAndTransport(respdata);
						SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetAudioQuality), bNeedResponse: true);
						SwitchSubstate(255, 13);
						break;
					case _BTD700_HOSTCMD.GetAudioQuality:
						SetAudioQuality(respdata[4], respdata[5]);
						if (btd700Ctx.iSelectedApplication == 2)
						{
							SwitchSubstate(12, 255);
							break;
						}
						SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetSupportedCodec), bNeedResponse: true);
						SwitchSubstate(255, 13);
						break;
					case _BTD700_HOSTCMD.GetSupportedCodec:
						if ((btd700Ctx.u16SupportedCodecs & 0xFF) != (respdata[4] & 0xFF))
						{
							btd700Ctx.u16SupportedCodecs = (ushort)(respdata[4] & 0xFFu);
							CodecList = btd700Ctx.GetSupportedCodecList();
							string text2 = "";
							foreach (CodecItem codec in CodecList)
							{
								text2 = text2 + codec.ToString() + " ";
							}
							addStatusEntry("BTD700 supported codecs " + text2);
						}
						if (btd700Ctx.state == _BTD700_STATES.S_CONNECTED || btd700Ctx.state == _BTD700_STATES.S_STREAMING_AUDIO || btd700Ctx.state == _BTD700_STATES.S_STREAMING_VOICE)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetSinkSupportTransport), bNeedResponse: true);
						}
						else
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetCodecInUse), bNeedResponse: true);
						}
						SwitchSubstate(255, 13);
						break;
					case _BTD700_HOSTCMD.GetSinkSupportTransport:
						UpdateSinkSupportTransport(respdata);
						SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetCodecInUse), bNeedResponse: true);
						SwitchSubstate(255, 13);
						break;
					case _BTD700_HOSTCMD.GetCodecInUse:
						UpdateCodecInUse(respdata);
						SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.GetLEAudioState), bNeedResponse: true);
						SwitchSubstate(255, 13);
						break;
					case _BTD700_HOSTCMD.GetLEAudioState:
						UpdateLEAudioState(respdata);
						SwitchSubstate(12, 255);
						break;
					default:
						SwitchSubstate(12, 255);
						break;
					}
					break;
				case 14:
				{
					bool flag4 = false;
					switch ((_BTD700_HOSTCMD)respdata[2])
					{
					case _BTD700_HOSTCMD.SetBroadcastEncryptKey:
						if (BroadcastNameChanged)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastName, btd700Ctx.broadcastName), bNeedResponse: true);
							SwitchSubstate(254, 14);
						}
						else if (BroadcastQualityChanged || BroadcastPBPChanged || BroadcastEncryptionChanged)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastInfo, btd700Ctx.broadcastInfoAsByteArray), bNeedResponse: true);
							SwitchSubstate(254, 14);
						}
						else
						{
							flag4 = true;
						}
						break;
					case _BTD700_HOSTCMD.SetBroadcastName:
						if (BroadcastQualityChanged || BroadcastPBPChanged || BroadcastEncryptionChanged)
						{
							SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP2, BTD700Tool.HostCmd(_BTD700_HOSTCMD.SetBroadcastInfo, btd700Ctx.broadcastInfoAsByteArray), bNeedResponse: true);
							SwitchSubstate(254, 14);
						}
						else
						{
							flag4 = true;
						}
						break;
					case _BTD700_HOSTCMD.SetBroadcastInfo:
						flag4 = true;
						break;
					}
					if (flag4)
					{
						Btd700View_ResetChangeFlags();
						SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP, new byte[2] { 206, 15 }, bNeedResponse: false);
						SwitchSubstate(254, 255);
					}
					break;
				}
				case 160:
					if (respdata[2] == 19)
					{
						Thread.Sleep(5000);
						ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
						PostEvent(_EVENT.BT_NEXT);
					}
					break;
				}
				Thread.Sleep(30);
				break;
			case _STATES.FSM_SETTINGSDISPLAY:
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_EXIT:
						ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
						break;
					case _EVENT.BT_MENUUPDATE:
						ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
						break;
					case _EVENT.BT_BTD700CANCEL:
						SetUserInterface(Btd700View);
						SetButton1(Btd700View, _EVENT.BT_EXIT, "btntxt_Exit");
						SetButton3(Btd700View);
						ResetButtonAndChangeState(_STATES.FSM_DEVICESPECIFICFEATURES);
						break;
					case _EVENT.BT_ABS_BTD700FACTORYRESET:
					case _EVENT.BT_ABS_BTD700DISCONNECT:
					case _EVENT.BT_ABS_BTD700CONNECT:
						ResetButtonAndChangeState(_STATES.FSM_DEVICESPECIFICFEATURES);
						RepostEvent(e, o);
						break;
					case _EVENT.DEV_DETACH:
						if ((o as DongleDeviceExt).Equals(SelectedDongle))
						{
							addStatusEntry("Transmitter unplugged");
							SetDongleStateUI(1);
							btd700Ctx.state = _BTD700_STATES.S_DISCONNECTED;
							ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
							PostEvent(_EVENT.BT_NEXT);
							SelectedDongle = new DongleDeviceExt();
							FWVersionLabel = "";
						}
						break;
					}
				}
				else
				{
					Thread.Sleep(200);
				}
				break;
			case _STATES.FSM_GETDONGLEFWVERSION:
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_EXIT:
						ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
						break;
					}
				}
				if (enFSMState != _STATES.FSM_GETDONGLEFWVERSION)
				{
					break;
				}
				switch (usSubstate)
				{
				case 0:
					SetAppSelectionMenuPill(bVisible: true, SelectedDongle != null && SelectedDongle.usbDeviceDetailConfig != null && SelectedDongle.usbDeviceDetailConfig.dtype.Equals(_DEVICETYPE.DEV_BTD700), bEnableUpdate: true, bEnableSettings: true);
					if (SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_CONNECTION_REQ, bNeedResponse: true))
					{
						usSubstate++;
						break;
					}
					if (iLoopCounter == 0)
					{
						iLoopCounter = 5;
					}
					Thread.Sleep(1000);
					usSubstate = 4;
					break;
				case 4:
					if (iLoopCounter > 0)
					{
						iLoopCounter--;
						break;
					}
					addLogEntry("HID Connection failed - please remove dongle");
					ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
					break;
				case 1:
					if (!SelectedDongle.WaitingForMain)
					{
						if (SelectedDongle.ProcessMainHIDResponse(out var status2))
						{
							addLogEntry("HID Connected");
							usSubstate++;
						}
						else if (status2 == U_STATUS.UPGRADE_STATUS_ALREADY_CONNECTED_WARNING)
						{
							addLogEntry("HID Already connected - forcing disconnection");
							SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_DISCONNECT_REQ, bNeedResponse: false);
							usSubstate = 0;
							Thread.Sleep(200);
						}
						else
						{
							addLogEntry("HID Connection failed - please remove dongle");
							SetDFUButton1(_EVENT.BT_EXIT, "btntxt_Exit");
							ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
						}
					}
					break;
				case 2:
					SelectedDongle.GetHDE(HIDDEVICETYPES.MAIN).stream.WriteTimeout = 1500;
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_HOST_VERSION_REQ, bNeedResponse: true);
					usSubstate++;
					break;
				case 3:
					if (SelectedDongle.WaitingForMain)
					{
						break;
					}
					if (SelectedDongle.ProcessMainUPResponse(U_OP.UPGRADE_HOST_VERSION_CFM, out respdata))
					{
						SelectedDongle.SetUpgradeHostVersion((ushort)(256 * respdata[0] + respdata[1]), (ushort)(256 * respdata[2] + respdata[3]), (ushort)(256 * respdata[4] + respdata[5]));
						addLogEntry("Dongle Firmware Version (Merry): " + SelectedDongle.GetUpgradeHostVersionString());
						setCurrFWVerLabel(SelectedDongle.GetUpgradeHostVersionString());
					}
					SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_DISCONNECT_REQ, bNeedResponse: false);
					usSubstate = 0;
					if (SelectedDongle.GetHDE(HIDDEVICETYPES.APP) == null)
					{
						if (iTransferState == 2)
						{
							TimeSpan timeSpan2 = DateTime.Now - startDT;
							addLogEntry("Transfer time: " + $"{timeSpan2.Minutes:D2}:{timeSpan2.Seconds:D2}.{timeSpan2.Milliseconds:D3}");
						}
						ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
						iTransferState = 0;
					}
					else
					{
						ResetButtonAndChangeState(_STATES.FSM_APP_CHECKPOINT);
					}
					break;
				}
				break;
			case _STATES.FSM_APP_CHECKPOINT:
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_EXIT:
						ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
						break;
					}
				}
				if (bAbortTransfer)
				{
					abortTransfer();
					break;
				}
				switch (usSubstate)
				{
				case 0:
				{
					byte[] cmd = new byte[2] { 206, 4 };
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP, cmd, bNeedResponse: true);
					usSubstate++;
					break;
				}
				case 1:
					if (SelectedDongle.ProcessAppIncoming(HIDDEVICETYPES.APP, out respdata))
					{
						SelectedDongle.SetAppVersion(respdata[3], respdata[4], respdata[5]);
						SelectedDongle.SetAppVersionExtra((ushort)(256 * respdata[10] + respdata[11]), (ushort)(256 * respdata[12] + respdata[13]), respdata[6], respdata[7], respdata[8], respdata[9]);
						string text3 = "Firmware App: " + SelectedDongle.GetAppVersionString();
						FWVersionLabel = SelectedDongle.GetAppVersionString();
						addLogEntry(text3);
						setCurrFWVerLabel("Serial Number: " + SelectedDongle.DongleSerialNumber + Environment.NewLine + "Firmware Version: " + SelectedDongle.GetUpgradeHostVersionString() + Environment.NewLine + text3);
						switch (SelectedDongle.usbDeviceDetailConfig.dtype)
						{
						case _DEVICETYPE.DEV_BTD600:
							SetUserInterface(DFUView);
							ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
							break;
						case _DEVICETYPE.DEV_BTD700:
							if (iTransferState == 2)
							{
								SetUserInterface(DFUView);
								ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
							}
							else
							{
								SetUserInterface(Btd700View);
								ResetButtonAndChangeState(_STATES.FSM_DEVICESPECIFICFEATURES);
							}
							break;
						}
					}
					else
					{
						addLogEntry("FSM_APP_CHECKPOINT error, disconnecting from dongle");
						usSubstate = 255;
					}
					if (iTransferState == 2)
					{
						TimeSpan timeSpan = DateTime.Now - startDT;
						string strNewLogEntry = "Transfer time: " + $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds:D3}";
						addLogEntry(strNewLogEntry);
						setMiniStatusTextCentered("", bUseBinding: false);
						SetDFUButton1(_EVENT.BT_CHECKUPDATE, "btntxt_Check");
						modalToastBox(SelectedLanguage.ltxt_firmwareupdated, SelectedLanguage.ltxt_firmwareupdated_sub, SelectedLanguage.btntxt_Done, "", "", 1);
					}
					iTransferState = 0;
					break;
				default:
					ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
					break;
				}
				break;
			case _STATES.FSM_LOCALORREMOTE:
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_EXIT:
						ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
						break;
					case _EVENT.BT_CANCEL:
						ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
						PostEvent(_EVENT.BT_NEXT);
						break;
					case _EVENT.BT_BTD700CANCEL:
						ResetButtonAndChangeState(_STATES.FSM_DEVICESPECIFICFEATURES);
						break;
					case _EVENT.BT_MENUSETTINGS:
						ResetButtonAndChangeState(_STATES.FSM_SETTINGSDISPLAY);
						break;
					case _EVENT.BT_CHECKUPDATE:
						DisplayDFUNotificationPanel();
						updateTransferProgress(bVisibility: true, -1);
						RepoHandler.Reset();
						num6 = 100;
						SetDFUButton1Enable(bEnable: false);
						SetDFUButton3Enable(bEnable: false);
						RepoHandler.SetSKUCode(SelectedDongle.usbDeviceDetailConfig.strDeviceSKUCode);
						RepoHandler.GetAvailableVersions();
						ResetButtonAndChangeState(_STATES.FSM_WAITFORREMOTE);
						break;
					case _EVENT.DEV_DETACH:
						if ((o as DongleDeviceExt).Equals(SelectedDongle))
						{
							addStatusEntry("Transmitter unplugged");
							SetDongleStateUI(1);
							btd700Ctx.state = _BTD700_STATES.S_DISCONNECTED;
							ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
							PostEvent(_EVENT.BT_NEXT);
							SelectedDongle = new DongleDeviceExt();
							FWVersionLabel = "";
						}
						break;
					}
				}
				else
				{
					Thread.Sleep(200);
				}
				break;
			case _STATES.FSM_WAITFORREMOTE:
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_EXIT:
						ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
						break;
					case _EVENT.BT_BTD700CANCEL:
						showRepoVersionList(bVisible: false);
						showSelectedRepoVersionInfo(bVisible: false);
						SetUserInterface(Btd700View);
						SetButton1(Btd700View, _EVENT.BT_EXIT, "btntxt_Exit");
						SetButton3(Btd700View);
						ResetButtonAndChangeState(_STATES.FSM_DEVICESPECIFICFEATURES);
						break;
					case _EVENT.BT_MENUSETTINGS:
						ResetButtonAndChangeState(_STATES.FSM_SETTINGSDISPLAY);
						break;
					case _EVENT.BT_CHECKUPDATE:
						DisplayDFUNotificationPanel();
						updateTransferProgress(bVisibility: true, -1);
						num6 = 100;
						SetDFUButton1Enable(bEnable: false);
						SetDFUButton3Enable(bEnable: false);
						RepoHandler.SetSKUCode(SelectedDongle.usbDeviceDetailConfig.strDeviceSKUCode);
						RepoHandler.GetAvailableVersions();
						ResetButtonAndChangeState(_STATES.FSM_WAITFORREMOTE);
						break;
					case _EVENT.DEV_DETACH:
						if ((o as DongleDeviceExt).Equals(SelectedDongle))
						{
							addStatusEntry("Transmitter unplugged");
							SetDongleStateUI(1);
							btd700Ctx.state = _BTD700_STATES.S_DISCONNECTED;
							ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
							PostEvent(_EVENT.BT_NEXT);
							SelectedDongle = new DongleDeviceExt();
							FWVersionLabel = "";
						}
						break;
					case _EVENT.NETWORK_ERROR:
						RepoHandler.Reset();
						DisplayDFUNotificationPanel((VariableDFUNotification == "") ? _DFUNOTI.NONETWORK : _DFUNOTI.VARIABLE);
						SetDFUButton1Enable(bEnable: true);
						SetDFUButton3Enable(bEnable: true);
						ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
						break;
					}
				}
				if (dfufile.isValid)
				{
					break;
				}
				if (RepoHandler.LastErrorCode == HttpStatusCode.OK && RepoHandler.versionResp != null)
				{
					updateTransferProgress(bVisibility: false, -1);
					if (RepoHandler.versionResp.success)
					{
						uint latestVersionAsU = RepoHandler.versionResp.GetLatestVersionAsU32();
						if (latestVersionAsU > SelectedDongle.GetAppVersionU32())
						{
							SelectedRepoVersionString = RepoHandler.ConvertVersionFromU32ToString(latestVersionAsU);
							ResetButtonAndChangeState(_STATES.FSM_SELECTFROMREMOTE);
							PostEvent(_EVENT.BT_ABS_REPOVERSIONSELECT);
						}
						else
						{
							RepoHandler.Reset();
							DisplayDFUNotificationPanel(_DFUNOTI.UPTODATE);
							ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
						}
					}
					else
					{
						MessageBox.Show("Unexpected versionResp : " + RepoHandler.versionResp.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Hand);
						rEASON = _REASON.NoNetwork;
					}
					usSubstate = 0;
				}
				else if (RepoHandler.LastErrorCode > (HttpStatusCode)0 && RepoHandler.LastErrorCode != HttpStatusCode.OK)
				{
					rEASON = _REASON.NoNetwork;
				}
				else
				{
					Thread.Sleep(200);
					num6--;
					if (num6 <= 0)
					{
						rEASON = _REASON.NoNetwork;
					}
				}
				SetDFUButton1Enable(bEnable: true);
				SetDFUButton3Enable(bEnable: true);
				break;
			case _STATES.FSM_SELECTFROMREMOTE:
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_EXIT:
						ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
						break;
					case _EVENT.BT_BTD700CANCEL:
						showRepoVersionList(bVisible: false);
						showSelectedRepoVersionInfo(bVisible: false);
						SetUserInterface(Btd700View);
						SetButton1(Btd700View, _EVENT.BT_EXIT, "btntxt_Exit");
						SetButton3(Btd700View);
						ResetButtonAndChangeState(_STATES.FSM_DEVICESPECIFICFEATURES);
						break;
					case _EVENT.BT_MENUSETTINGS:
						ResetButtonAndChangeState(_STATES.FSM_SETTINGSDISPLAY);
						break;
					case _EVENT.BT_CANCEL:
						showSelectedRepoVersionInfo(bVisible: false);
						SelectedRepoVersion = "";
						SelectedRepoVersionString = "";
						showRepoVersionList(bVisible: true, RepoHandler.versionResp.data);
						SetDFUButton2();
						SetDFUButton2Enable(bEnable: false);
						SetDFUButton1(_EVENT.BT_CHECKUPDATE, "btntxt_Check");
						SetDFUButton1Enable(bEnable: true);
						usSubstate = 0;
						break;
					case _EVENT.BT_ABS_REPOVERSIONSELECT:
						SelectedRepoVersion = SelectedRepoVersionString;
						showRepoVersionList(bVisible: false);
						break;
					case _EVENT.BT_UPDATE:
						if (modalToastBox(SelectedLanguage.ltxt7_IfProceed_Header, SelectedLanguage.ltxt7_IfProceed, SelectedLanguage.btntxt_Update, SelectedLanguage.btntxt_Cancel) == 1)
						{
							SelectedRepoVersion = "";
							SelectedRepoVersionString = "";
							showSelectedRepoVersionInfo(bVisible: false);
							updateTransferProgress(bVisibility: true, 0);
							SetDFUButton1(_EVENT.BT_CANCEL, "btntxt_Cancel");
							SetDFUButton2();
							SetDFUButton3();
							SetMenuPillEnabled(selectable: false);
							setMiniStatusTextCentered("TextStepDownloading");
							RepoHandler.DownloadDFUFileFromRepo();
						}
						break;
					case _EVENT.NETWORK_ERROR:
						updateTransferProgress(bVisibility: false, 0);
						rEASON = _REASON.NoNetwork;
						RepoHandler.Reset();
						DisplayDFUNotificationPanel((VariableDFUNotification == "") ? _DFUNOTI.NONETWORK : _DFUNOTI.VARIABLE);
						SetDFUButton1Enable(bEnable: true);
						SetDFUButton1(_EVENT.BT_CHECKUPDATE, "btntxt_Check");
						SetDFUButton3();
						SetDFUButton3Enable(bEnable: false);
						ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
						break;
					case _EVENT.DEV_DETACH:
						if ((o as DongleDeviceExt).Equals(SelectedDongle))
						{
							addStatusEntry("Transmitter unplugged");
							SetDongleStateUI(1);
							btd700Ctx.state = _BTD700_STATES.S_DISCONNECTED;
							ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
							PostEvent(_EVENT.BT_NEXT);
							SelectedDongle = new DongleDeviceExt();
							FWVersionLabel = "";
						}
						break;
					}
				}
				if (enFSMState != _STATES.FSM_SELECTFROMREMOTE)
				{
					break;
				}
				switch (usSubstate)
				{
				case 0:
					if (SelectedRepoVersion != "")
					{
						RepoHandler.GetVersionManifest(SelectedRepoVersion);
						usSubstate++;
					}
					break;
				case 1:
					if (RepoHandler.LastErrorCode == HttpStatusCode.OK && RepoHandler.versionManifest != null && RepoHandler.extras != null && RepoHandler.versionManifest.success)
					{
						if (RepoHandler.extras.success)
						{
							showSelectedRepoVersionInfo(bVisible: true, RepoHandler.extras.data.release_notes.en.Replace("\n", Environment.NewLine));
						}
						else
						{
							showSelectedRepoVersionInfo(bVisible: true, RepoHandler.versionManifest.GetManifestDetails());
						}
						SetDFUButton1(_EVENT.BT_UPDATE, "btntxt_Update");
						usSubstate++;
					}
					break;
				case 2:
					if (RepoHandler.DFUFileLocalPath != "")
					{
						dfufile.setFile(RepoHandler.DFUFileLocalPath);
						string text = "";
						if (dfufile.isValid)
						{
							if (dfufile.strChipModel == SelectedDongle.usbDeviceDetailConfig.strDFUChipModelPrefix)
							{
								bAbortTransfer = false;
								addLogEntry(RepoHandler.DFUFileLocalPath);
								addLogEntry(dfufile.strShortInfo);
								SelectedRepoVersionString = dfufile.strVersionInfo;
								setWarning("ltxt7_DoNotRemove");
								ResetButtonAndChangeState(_STATES.FSM_SYNCANDSTART);
							}
							else
							{
								text = "Mismatched chip type (DFU: " + dfufile.strChipModel + ", Dongle: " + SelectedDongle.usbDeviceDetailConfig.strDFUChipModelPrefix + ")";
							}
						}
						else
						{
							text = dfufile.strError;
						}
						if (text != "")
						{
							modalToastBox("ERROR", text, "OK");
							dfufile.unsetFile();
							SetUserInterface(DFUView);
							ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
						}
					}
					else if (RepoHandler.LastErrorCode == HttpStatusCode.GatewayTimeout)
					{
						updateTransferProgress(bVisibility: false, 0);
						rEASON = _REASON.NoNetwork;
						RepoHandler.Reset();
						DisplayDFUNotificationPanel(_DFUNOTI.NONETWORK);
						SetDFUButton1Enable(bEnable: true);
						SetDFUButton1(_EVENT.BT_CHECKUPDATE, "btntxt_Check");
						SetDFUButton3();
						SetDFUButton3Enable(bEnable: false);
						SetMenuPillEnabled(selectable: true);
						ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
					}
					break;
				}
				Thread.Sleep(200);
				break;
			case _STATES.FSM_WAITFORDONGLEREMOVAL:
				if (!GetEvent(out e, out o))
				{
					break;
				}
				switch (e)
				{
				case _EVENT.BT_EXIT:
					ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
					break;
				case _EVENT.DEV_DETACH:
					if ((o as DongleDeviceExt).Equals(SelectedDongle))
					{
						ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
						PostEvent(_EVENT.BT_NEXT);
						SelectedDongle = new DongleDeviceExt();
						FWVersionLabel = "";
					}
					break;
				}
				if (!flag2)
				{
				}
				break;
			case _STATES.FSM_LOCALCONFIRM:
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_CANCEL:
						dfufile?.unsetFile();
						ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLE);
						PostEvent(_EVENT.BT_ABS_DONGLESELECT);
						break;
					case _EVENT.BT_BTD700CANCEL:
						showRepoVersionList(bVisible: false);
						showSelectedRepoVersionInfo(bVisible: false);
						SetUserInterface(Btd700View);
						SetButton1(Btd700View, _EVENT.BT_EXIT, "btntxt_Exit");
						SetButton3(Btd700View);
						ResetButtonAndChangeState(_STATES.FSM_DEVICESPECIFICFEATURES);
						break;
					case _EVENT.BT_UPDATE:
						if (modalToastBox(SelectedLanguage.ltxt7_IfProceed_Header, SelectedLanguage.ltxt7_IfProceed, SelectedLanguage.btntxt_Update, SelectedLanguage.btntxt_Cancel) == 1)
						{
							SelectedRepoVersion = "";
							SelectedRepoVersionString = "";
							showSelectedRepoVersionInfo(bVisible: false);
							updateTransferProgress(bVisibility: true, 0);
							SetDFUButton1(_EVENT.BT_CANCEL, "btntxt_Cancel");
							SetDFUButton2();
							SetDFUButton3();
							SetMenuPillEnabled(selectable: false);
							setWarning("ltxt7_DoNotRemove");
							ResetButtonAndChangeState(_STATES.FSM_SYNCANDSTART);
						}
						break;
					case _EVENT.DEV_DETACH:
						if ((o as DongleDeviceExt).Equals(SelectedDongle))
						{
							ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
							PostEvent(_EVENT.BT_NEXT);
							SelectedDongle = new DongleDeviceExt();
							FWVersionLabel = "";
						}
						break;
					}
					if (!flag2)
					{
					}
				}
				else
				{
					Thread.Sleep(200);
				}
				break;
			case _STATES.FSM_SYNCANDSTART:
				flag2 = false;
				if (GetEvent(out e, out o) && e == _EVENT.BT_CANCEL)
				{
					dfufile?.unsetFile();
					setWarning();
					setMiniStatusTextLeft();
					SetMenuPillEnabled(selectable: true);
					ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLE);
					PostEvent(_EVENT.BT_ABS_DONGLESELECT);
					flag2 = true;
				}
				if (flag2)
				{
					break;
				}
				switch (usSubstate)
				{
				case 0:
					var (num7, num8, num9) = SelectedDongle.GetUpgradeHostVersion();
					if (!dfufile.checkIfVersionIsCompatible((uint)(65536 * num7 + num8), num9) && forceTransfer())
					{
						addLogEntry("Dongle version not in DFU Version List - updating DFU file for forced transfer");
						dfufile.createCompatibleDFUFile((uint)(65536 * num7 + num8), num9);
					}
					if (iTransferState == 0)
					{
						startDT = new DateTime(DateTime.Now.Ticks);
					}
					iTransferState = 1;
					b = byte.MaxValue;
					SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_CONNECTION_REQ, bNeedResponse: true);
					usSubstate++;
					break;
				case 1:
					if (!SelectedDongle.WaitingForMain)
					{
						if (SelectedDongle.ProcessMainHIDResponse(out var _))
						{
							addLogEntry("HID Connected");
							usSubstate++;
							break;
						}
						updateTransferProgress(bVisibility: false, 0);
						SetDFUButton1(_EVENT.BT_EXIT, "btntxt_Exit");
						SetDFUButton2(_EVENT.BT_CANCEL, "btntxt_Cancel");
						setWarning();
						addLogEntry("HID Connection failed - please remove dongle");
						ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
					}
					break;
				case 2:
					SelectedDongle.GetHDE(HIDDEVICETYPES.MAIN).stream.WriteTimeout = 3000;
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_SYNC_REQ, bNeedResponse: true, useDefaultFileID() ? UniqueFileID : dfufile.FileSignature);
					usSubstate++;
					break;
				case 3:
					if (SelectedDongle.WaitingForMain)
					{
						break;
					}
					if (SelectedDongle.ProcessMainUPResponse(U_OP.UPGRADE_SYNC_CFM, out respdata))
					{
						b = respdata[0];
						addLogEntry("SYNC_CFM::RESUME_POINT " + b.ToString("X2"));
						bool flag3 = true;
						byte[] array2 = (useDefaultFileID() ? UniqueFileID : dfufile.FileSignature);
						for (int i = 0; i < 4; i++)
						{
							if (array2[i] != respdata[i + 1])
							{
								flag3 = false;
								break;
							}
						}
						if (flag3)
						{
							dfufile.resetFile();
							usSubstate++;
							break;
						}
						addLogEntry("SYNC_CFM: returned FileID is different, aborting ..");
						enFSMPreAbortState = _STATES.FSM_LICENSEAGREEMENT;
						PostEvent(_EVENT.BT_NEXT);
						ResetButtonAndChangeState(_STATES.FSM_ABORT);
					}
					else
					{
						iTransferState = 0;
						usSubstate = 0;
						enFSMState = _STATES.FSM_ABORT;
						enFSMPreAbortState = _STATES.FSM_SYNCANDSTART;
					}
					break;
				case 4:
					setMiniStatusTextCentered("TextStep4_1");
					SelectedDongle.GetHDE(HIDDEVICETYPES.MAIN).stream.WriteTimeout = 1500;
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_START_REQ, bNeedResponse: true);
					usSubstate++;
					break;
				case 5:
					if (SelectedDongle.WaitingForMain)
					{
						break;
					}
					if (SelectedDongle.ProcessMainUPResponse(U_OP.UPGRADE_START_CFM, out respdata))
					{
						addLogEntry("START_CFM " + respdata[0].ToString("X2") + ", batt: " + (ushort)(256 * respdata[1] + respdata[2]) + "mV");
						if (!flag && b != 0 && modalMsgBox("There is already a new firmware in this device waiting to be activated. Press YES to discard the existing firmware and proceed with new transfer, NO to try activating the existing new firmware instead.", "ALERT", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
						{
							b = 0;
						}
						switch ((U_RESUMEPOINT)b)
						{
						case U_RESUMEPOINT.UPGRADE_RESUME_POINT_START:
							ResetButtonAndChangeState(_STATES.FSM_RP_START);
							break;
						case U_RESUMEPOINT.UPGRADE_RESUME_POINT_PRE_VALIDATE:
							ResetButtonAndChangeState(_STATES.FSM_RP_PRE_VALIDATE);
							break;
						case U_RESUMEPOINT.UPGRADE_RESUME_POINT_PRE_REBOOT:
							ResetButtonAndChangeState(_STATES.FSM_RP_PRE_REBOOT);
							break;
						case U_RESUMEPOINT.UPGRADE_RESUME_POINT_POST_REBOOT:
							ResetButtonAndChangeState(_STATES.FSM_RP_POST_REBOOT);
							break;
						case U_RESUMEPOINT.UPGRADE_RESUME_POINT_POST_COMMIT:
							ResetButtonAndChangeState(_STATES.FSM_RP_POST_COMMIT);
							break;
						default:
							ResetButtonAndChangeState(_STATES.FSM_RP_START);
							break;
						}
					}
					else
					{
						addLogEntry("START_CFM error, disconnecting from dongle");
						SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_DISCONNECT_REQ, bNeedResponse: false);
						if (respdata[0] == 6 && respdata[2] == 8)
						{
							ResetButtonAndChangeState(_STATES.FSM_SYNCANDSTART);
						}
						else
						{
							ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
						}
					}
					break;
				}
				break;
			case _STATES.FSM_RP_START:
				flag2 = false;
				if (GetEvent(out e, out o))
				{
					switch (e)
					{
					case _EVENT.BT_CANCEL:
						if (modalToastBox(SelectedLanguage.warntxt_cancelupdate, SelectedLanguage.warntxt_cancelupdate_sub, SelectedLanguage.btntxt_Confirm, SelectedLanguage.btntxt_Close) == 1)
						{
							dfufile?.unsetFile();
							setWarning();
							setMiniStatusTextLeft();
							SetMenuPillEnabled(selectable: true);
							ResetButtonAndChangeState(_STATES.FSM_ABORT);
							enFSMPreAbortState = _STATES.FSM_LICENSEAGREEMENT;
							PostEvent(_EVENT.BT_NEXT);
							flag2 = true;
						}
						break;
					case _EVENT.DEV_DETACH:
						if ((o as DongleDeviceExt).Equals(SelectedDongle))
						{
							modalToastBox(SelectedLanguage.ltxt_firmwareupdatefailed, SelectedLanguage.ltxt_firmwareupdatefailed_sub, SelectedLanguage.btntxt_Close, "", "", 2);
							setMiniStatusTextCentered();
							setWarning();
							SelectedDongle = new DongleDeviceExt();
							RepoHandler.Reset();
							dfufile?.unsetFile();
							FWVersionLabel = "";
							SetMenuPillEnabled(selectable: true);
							ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
							PostEvent(_EVENT.BT_NEXT);
							flag2 = true;
						}
						break;
					}
				}
				if (flag2)
				{
					break;
				}
				switch (usSubstate)
				{
				case 0:
					if (dfufile.isValid)
					{
						setMiniStatusTextCentered("TextStep4_2");
						SelectedDongle.GetHDE(HIDDEVICETYPES.MAIN).stream.WriteTimeout = 1500;
						SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_START_DATA_REQ, bNeedResponse: true);
						num = 0u;
						num2 = 0L;
						num3 = 0L;
						usSubstate = 1;
						flag = true;
					}
					else
					{
						Thread.Sleep(500);
					}
					break;
				case 1:
					if (!SelectedDongle.WaitingForMain)
					{
						if (SelectedDongle.ProcessMainUPResponse(U_OP.UPGRADE_DATA_BYTES_REQ, out respdata))
						{
							num = (uint)(16777216 * respdata[0] + 65536 * respdata[1] + 256 * respdata[2] + respdata[3]);
							num2 = 16777216 * respdata[4] + 65536 * respdata[5] + 256 * respdata[6] + respdata[7];
							if (num2 > 0)
							{
								if (num3 == 0L)
								{
									num3 = num2;
								}
								else
								{
									num2 = 0L;
								}
							}
							addLogEntry("DATA_BYTES_REQ offs:" + num2 + " len:" + num + " (" + (dfufile.getPosition() + 1) + "/" + dfufile.getSize() + ")");
							usSubstate = 2;
						}
						else if (respdata[0] == 6 && respdata[2] == 8)
						{
							SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_DISCONNECT_REQ, bNeedResponse: false);
							ResetButtonAndChangeState(_STATES.FSM_SYNCANDSTART);
						}
						else
						{
							addLogEntry("DATA_BYTES_REQ error, disconnecting from dongle");
							if (SelectedDongle.u16UPErrorCode == 128)
							{
								modalMsgBox("Dongle version is incompatible with DFU version, aborting transfer.", "ALERT", MessageBoxButton.OK, MessageBoxImage.Exclamation);
							}
							else
							{
								modalToastBox(SelectedLanguage.ltxt_firmwareupdatefailed, SelectedLanguage.ltxt_firmwareupdatefailed_sub, SelectedLanguage.btntxt_Close, "", "", 2);
							}
							updateTransferProgress(bVisibility: false, 0);
							SetDFUButton1(_EVENT.BT_EXIT, "btntxt_Exit");
							setWarning();
							rEASON = _REASON.UpdateError;
							abortTransfer(_STATES.FSM_ERROR);
						}
					}
					else if (SelectedDongle.uopLastCommandMain == U_OP.UPGRADE_START_DATA_REQ)
					{
						Thread.Sleep(500);
					}
					break;
				case 2:
				{
					uint num11 = (uint)(SelectedDongle.OpsLength(HIDDEVICETYPES.MAIN) - 6);
					if (num11 >= num)
					{
						num11 = num;
					}
					byte[] output;
					uint fileData = dfufile.getFileData(num2, num11, out output);
					num -= fileData;
					num2 = 0L;
					if (fileData != 0)
					{
						bool bNeedResponse = num == 0 && output[0] == 0;
						SelectedDongle.GetHDE(HIDDEVICETYPES.MAIN).stream.WriteTimeout = 1500;
						SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_DATA, bNeedResponse, output);
						updateTransferProgress(bVisibility: true, (int)(100 * (dfufile.getPosition() + 1) / dfufile.getSize()));
						if (output[0] == 1)
						{
							usSubstate = 0;
							enFSMState = _STATES.FSM_RP_PRE_VALIDATE;
							break;
						}
						if (num != 0)
						{
							usSubstate = 2;
						}
						else
						{
							usSubstate = 1;
						}
						Thread.Sleep(2);
					}
					else
					{
						addLogEntry(dfufile.strError);
						addLogEntry("File error, disconnecting from dongle");
						modalToastBox(SelectedLanguage.ltxt_firmwareupdatefailed, SelectedLanguage.ltxt_firmwareupdatefailed_sub, SelectedLanguage.btntxt_Close, "", "", 2);
						updateTransferProgress(bVisibility: false, 0);
						rEASON = _REASON.UpdateError;
						abortTransfer(_STATES.FSM_ERROR);
					}
					break;
				}
				default:
					SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_DISCONNECT_REQ, bNeedResponse: false);
					ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
					break;
				}
				break;
			case _STATES.FSM_RP_PRE_VALIDATE:
				if (bAbortTransfer)
				{
					abortTransfer();
					break;
				}
				switch (usSubstate)
				{
				case 0:
					setMiniStatusTextCentered("TextStep4_3");
					SelectedDongle.GetHDE(HIDDEVICETYPES.MAIN).stream.WriteTimeout = 6000;
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_IS_VALIDATION_DONE_REQ, bNeedResponse: true);
					usSubstate++;
					break;
				case 1:
				{
					if (SelectedDongle.WaitingForMain)
					{
						break;
					}
					if (SelectedDongle.ProcessMainAnyUPResponse(out U_OP op, out respdata))
					{
						switch (op)
						{
						case U_OP.UPGRADE_IS_VALIDATION_DONE_CFM:
						{
							int millisecondsTimeout = 256 * respdata[0] + respdata[1];
							addLogEntry("IS_VALIDATION_DONE_CFM: backoff for " + millisecondsTimeout + "ms");
							usSubstate = 0;
							Thread.Sleep(millisecondsTimeout);
							break;
						}
						case U_OP.UPGRADE_TRANSFER_COMPLETE_IND:
							ResetButtonAndChangeState(_STATES.FSM_RP_PRE_REBOOT);
							break;
						default:
							addLogEntry("IS_VALIDATION_DONE_REQ unexpected ops " + $"{(byte)op,2:X2} ");
							usSubstate = 255;
							break;
						}
					}
					else
					{
						addLogEntry("IS_VALIDATION_DONE_REQ error, disconnecting from dongle");
						usSubstate = 255;
					}
					break;
				}
				default:
					SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_DISCONNECT_REQ, bNeedResponse: false);
					ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
					break;
				}
				break;
			case _STATES.FSM_RP_PRE_REBOOT:
				if (GetEvent(out e, out o) && e != _EVENT.BT_CANCEL && e != _EVENT.DEV_ATTACH && e == _EVENT.DEV_DETACH && usSubstate == 1)
				{
					usSubstate = 2;
				}
				switch (usSubstate)
				{
				case 0:
					array[0] = 0;
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_TRANSFER_COMPLETE_RES, bNeedResponse: false, array);
					num4 = 60;
					addLogEntry("TRANSFER_COMPLETE_IND - dongle reboot");
					updateTransferProgress(bVisibility: false, 0);
					setWarning();
					SelectedDongle.DisposeMain();
					usSubstate++;
					break;
				case 1:
					Thread.Sleep(200);
					break;
				case 2:
					if (num4 > 0 && !SelectedDongle.IsDetected)
					{
						setMiniStatusTextCentered("TextStep4_4");
						Thread.Sleep(1000);
						num4--;
						break;
					}
					if (!SelectedDongle.IsDetected)
					{
						Thread.Sleep(500);
						break;
					}
					setMiniStatusTextCentered("TextStep4_0");
					try
					{
						addLogEntry("FSM_RP_PRE_REBOOT : Found dongle with serial number " + SelectedDongle.DongleSerialNumber);
						addLogEntry(SelectedDongle.ToString());
						addLogEntry(SelectedDongle.DongleDevicePath);
						if (SelectedDongle.IsConnected)
						{
							ResetButtonAndChangeState(_STATES.FSM_SYNCANDSTART);
							break;
						}
						addLogEntry("FSM_RP_PRE_REBOOT error: cannot open dongle with serial number " + SelectedDongle.DongleSerialNumber);
						ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
					}
					catch (Exception ex)
					{
						addLogEntry(ex.ToString());
						addLogEntry("FSM_RP_PRE_REBOOT error on accessing dongle");
						ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
					}
					break;
				default:
					addLogEntry("FSM_RP_PRE_REBOOT error: invalid usSubstate");
					ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
					break;
				}
				break;
			case _STATES.FSM_RP_POST_REBOOT:
				array[0] = 0;
				switch (usSubstate)
				{
				case 0:
					setMiniStatusTextCentered("TextStep4_5");
					addLogEntry("FSM_RP_POST_REBOOT : PROCEED_TO_COMMIT");
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_PROCEED_TO_COMMIT, bNeedResponse: true, array);
					usSubstate++;
					break;
				case 1:
					if (!SelectedDongle.WaitingForMain)
					{
						if (SelectedDongle.ProcessMainUPResponse(U_OP.UPGRADE_COMMIT_REQ, out respdata2))
						{
							addLogEntry("FSM_RP_POST_REBOOT : COMMIT_CFM");
							SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_COMMIT_CFM, bNeedResponse: true, array);
							ResetButtonAndChangeState(_STATES.FSM_RP_POST_COMMIT);
						}
						else
						{
							usSubstate = 255;
						}
					}
					break;
				default:
					SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_DISCONNECT_REQ, bNeedResponse: false);
					ResetButtonAndChangeState(_STATES.FSM_WAITFORDONGLEREMOVAL);
					break;
				}
				break;
			case _STATES.FSM_RP_POST_COMMIT:
				if (!SelectedDongle.WaitingForMain && SelectedDongle.ProcessMainUPResponse(U_OP.UPGRADE_COMPLETE_IND, out respdata2))
				{
					addLogEntry("FSM_RP_POST_COMMIT : <-COMPLETE_IND");
					SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_DISCONNECT_REQ, bNeedResponse: false);
					addLogEntry("Done.");
					setMiniStatusTextCentered("TextStep4_6");
					flag = false;
					bAbortTransfer = false;
					dfufile.unsetFile();
					iTransferState = 2;
					SetMenuPillEnabled(selectable: true);
					ResetButtonAndChangeState(_STATES.FSM_GETDONGLEFWVERSION);
				}
				break;
			case _STATES.FSM_ABORT:
				switch (usSubstate)
				{
				case 0:
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_ABORT_REQ, bNeedResponse: true);
					bAbortTransfer = false;
					flag = false;
					usSubstate++;
					break;
				case 1:
					if (SelectedDongle.ProcessMainUPResponse(U_OP.UPGRADE_ABORT_CFM, out respdata2))
					{
						ResetButtonAndChangeState(enFSMPreAbortState);
						break;
					}
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_ABORT_REQ, bNeedResponse: true);
					bAbortTransfer = false;
					flag = false;
					usSubstate++;
					break;
				case 2:
					if (SelectedDongle.ProcessMainUPResponse(U_OP.UPGRADE_ABORT_CFM, out respdata2))
					{
						ResetButtonAndChangeState(enFSMPreAbortState);
						break;
					}
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_ABORT_REQ, bNeedResponse: true);
					bAbortTransfer = false;
					flag = false;
					usSubstate++;
					break;
				case 3:
					if (SelectedDongle.ProcessMainUPResponse(U_OP.UPGRADE_ABORT_CFM, out respdata2))
					{
						ResetButtonAndChangeState(enFSMPreAbortState);
						break;
					}
					SelectedDongle.SendMainUPCommand(U_OP.UPGRADE_ABORT_REQ, bNeedResponse: true);
					bAbortTransfer = false;
					flag = false;
					usSubstate++;
					break;
				case 4:
					SelectedDongle.SendAppGenericCommand(HIDDEVICETYPES.APP, new byte[2] { 206, 15 }, bNeedResponse: false);
					ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
					PostEvent(_EVENT.BT_NEXT);
					SetMenuPillEnabled(selectable: true);
					setMiniStatusTextLeft();
					break;
				case 5:
					SelectedDongle.SendMainHIDCommand(U_HIDCMD.HID_CMD_DISCONNECT_REQ, bNeedResponse: false);
					usSubstate = 0;
					enFSMState = enFSMPreAbortState;
					break;
				}
				Thread.Sleep(200);
				break;
			case _STATES.FSM_ERROR:
				if (!GetEvent(out e, out o))
				{
					break;
				}
				switch (e)
				{
				case _EVENT.BT_EXIT:
					ResetButtonAndChangeState(_STATES.FSM_TERMINATE);
					break;
				case _EVENT.BT_CANCEL:
					ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
					PostEvent(_EVENT.BT_NEXT);
					SetMenuPillEnabled(selectable: true);
					setMiniStatusTextLeft();
					break;
				case _EVENT.BT_TRYAGAIN:
					switch (rEASON)
					{
					case _REASON.NoNetwork:
						ResetButtonAndChangeState(_STATES.FSM_LOCALORREMOTE);
						PostEvent(_EVENT.BT_CHECKUPDATE);
						break;
					case _REASON.WriteTimeout:
						ResetButtonAndChangeState(_STATES.FSM_LICENSEAGREEMENT);
						PostEvent(_EVENT.BT_NEXT);
						break;
					}
					break;
				}
				break;
			}
			processesByName = Process.GetProcessesByName(strMainProc);
		}
		while (!(strMainProc == "") && processesByName.Length != 0 && processesByName[0].MainWindowHandle != 0 && enFSMState != _STATES.FSM_TERMINATE && !bExitApplication);
	}

	public void UpdateDongleState(byte[] r)
	{
		if (Enum.IsDefined(typeof(_BTD700_STATES), (int)r[4]))
		{
			if (btd700Ctx.state != (_BTD700_STATES)r[4])
			{
				_BTD700_STATES bTD700_STATES = (_BTD700_STATES)r[4];
				addStatusEntry("BTD700 state is " + bTD700_STATES);
				btd700Ctx.state = (_BTD700_STATES)r[4];
				SetDongleStateUI(r[4]);
			}
			if (btd700Ctx.state == _BTD700_STATES.S_NONE || btd700Ctx.state == _BTD700_STATES.S_DISCONNECTED)
			{
				UpdateGamingAvailableStatus(-1);
			}
		}
		else
		{
			SetDongleStateUI(0);
			btd700Ctx.state = _BTD700_STATES.S_NONE;
		}
	}

	public void UpdateAudioModeAndTransport(byte[] r)
	{
		if (Enum.IsDefined(typeof(_BTD700_AUDIO_MODE), (int)r[4]) && Enum.IsDefined(typeof(_BTD700_TRANSPORT_MODE), (int)r[5]))
		{
			btd700Ctx.audioMode = (_BTD700_AUDIO_MODE)r[4];
			_BTD700_AUDIO_MODE bTD700_AUDIO_MODE = (_BTD700_AUDIO_MODE)r[4];
			addStatusEntry("BTD700 audiomode " + bTD700_AUDIO_MODE);
			Btd700View_SelectApplication(r[4]);
			btd700Ctx.transportMode = (_BTD700_TRANSPORT_MODE)r[5];
			_BTD700_TRANSPORT_MODE bTD700_TRANSPORT_MODE = (_BTD700_TRANSPORT_MODE)r[5];
			addStatusEntry("BTD700 transport " + bTD700_TRANSPORT_MODE);
			if (r[3] > 2)
			{
				btd700Ctx.connectedTransportMode = (_BTD700_TRANSPORT_MODE)r[6];
				if (TransportList.Count > 0 && Btd700View_GetSelectedTransportAsByte() != r[6])
				{
					Btd700View_SelectTransport(r[6]);
					bTD700_TRANSPORT_MODE = (_BTD700_TRANSPORT_MODE)r[6];
					addStatusEntry("BTD700 connected transport " + bTD700_TRANSPORT_MODE);
				}
				RefreshGamingModeItem();
			}
		}
		CancelModeSetBackoff();
	}

	public void UpdateSinkSupportTransport(byte[] r)
	{
		btd700Ctx.sinkTransportMode = (_BTD700_SINKMODE)r[4];
		TransportList = btd700Ctx.GetSupportedTransportList();
		Btd700View_SelectTransport((byte)btd700Ctx.connectedTransportMode);
		RefreshGamingModeItem();
		_BTD700_SINKMODE bTD700_SINKMODE = (_BTD700_SINKMODE)r[4];
		addStatusEntry("BTD700 supported sinkTransportMode " + bTD700_SINKMODE);
	}

	public void UpdateGamingAvailableStatus(int ig)
	{
		btd700Ctx.iGamingAvailable = ig;
		RefreshGamingModeItem();
		addStatusEntry("BTD700 Gaming available status: " + ig);
	}

	public void UpdateSupportedCodecs(byte[] r)
	{
		btd700Ctx.u16SupportedCodecs = r[4];
		CodecList = btd700Ctx.GetSupportedCodecList();
		string text = "";
		foreach (CodecItem codec in CodecList)
		{
			text = text + codec.ToString() + " ";
		}
		addStatusEntry("BTD700 supported codecs " + text);
		RefreshGamingModeItem();
	}

	public void UpdateCodecInUse(byte[] r)
	{
		if ((btd700Ctx.u16UsedCodecs & 0xFF) != (r[4] & 0xFF) || Btd700View_GetSelectedCodecAsByte() != r[4])
		{
			btd700Ctx.u16UsedCodecs = (ushort)(r[4] & 0xFFu);
			Btd700View_SelectCodec(btd700Ctx.GetCodecInUseIdx());
			addStatusEntry("BTD700 codec in-use " + btd700Ctx.GetCodecInUseName());
			Btd700View_SetCodecSelectButtonEnabled(bEnable: true);
		}
		RefreshGamingModeItem();
	}

	public void UpdateLEAudioState(byte[] r)
	{
		btd700Ctx.LEAstate = (_BTD700_LEAUDIO_STATE)r[4];
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}

	public void SetEditable(bool editable)
	{
		try
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				SafeForEditing = editable;
			});
		}
		catch (Exception)
		{
		}
	}

	public void SetMenuPillEnabled(bool selectable)
	{
		try
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				MenuPillEnabled = selectable;
			});
		}
		catch (Exception)
		{
		}
	}

	private void UI_Init()
	{
		LicenseView = new AppLicense();
		LicenseView.DataContext = this;
		DongleWaitView = new AppDongleWait();
		DongleWaitView.DataContext = this;
		Btd700View = new AppBtd700Features();
		Btd700View.DataContext = this;
		DFUView = new AppDfu();
		DFUView.DataContext = this;
		SettingsView = new AppSettings();
		SettingsView.DataContext = this;
		AppSelectionVisibility = Visibility.Hidden;
		string value = "English";
		if (Application.Current.Properties["SelectedLanguage"] != null)
		{
			value = (string)Application.Current.Properties["SelectedLanguage"];
		}
		foreach (ResourceDictionary item in Application.Current.Resources.MergedDictionaries.ToList())
		{
			ResDictExt resDictExt = new ResDictExt(item);
			_LanguageList.Add(resDictExt);
			if (resDictExt.Language.Equals(value))
			{
				SelectedLanguage = resDictExt;
			}
		}
		LogoVisibility = Visibility.Visible;
		UserInterface = LicenseView;
	}

	public void SetAppSelectionMenuPill(bool bVisible = false, bool bEnableDashboard = false, bool bEnableUpdate = false, bool bEnableSettings = false)
	{
		AppSelectionVisibility = ((!bVisible) ? Visibility.Collapsed : Visibility.Visible);
		MenuDashboardEnabled = bEnableDashboard;
		MenuUpdateEnabled = bEnableUpdate;
		MenuSettingsEnabled = bEnableSettings;
	}

	public void UI_DiplayLicense()
	{
		LogoVisibility = Visibility.Visible;
		UserInterface = LicenseView;
	}

	public void UI_UserResp(_EVENT e)
	{
		PostEvent(e);
		if (e == _EVENT.BT_EXIT)
		{
			Thread.Sleep(1000);
			Application.Current.Shutdown();
		}
	}

	public void DisplayDFUNotificationPanel(_DFUNOTI which = _DFUNOTI.NONE)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			if (DFUView != null)
			{
				DFUView.NotiNoNetwork.Visibility = ((which != _DFUNOTI.NONETWORK) ? Visibility.Collapsed : Visibility.Visible);
				DFUView.NotiUpToDate.Visibility = ((which != _DFUNOTI.UPTODATE) ? Visibility.Collapsed : Visibility.Visible);
				DFUView.NotiVariable.Visibility = ((which != _DFUNOTI.VARIABLE) ? Visibility.Collapsed : Visibility.Visible);
				if (which != 0)
				{
					updateTransferProgress(bVisibility: false, -1);
				}
			}
		});
	}

	public bool forceTransfer()
	{
		bool res = false;
		Application.Current.Dispatcher.Invoke(delegate
		{
			res = bForceTransfer;
		});
		return res;
	}

	public bool useDefaultFileID()
	{
		bool res = false;
		Application.Current.Dispatcher.Invoke(delegate
		{
			res = bUseDefaultFileID;
		});
		return res;
	}

	public void addLogEntry(string strNewLogEntry)
	{
	}

	public void addStatusEntry(string strNewStatusEntry)
	{
	}

	public void setMainAndSubLabel(string main, string sub, bool bUseBinding = true)
	{
		string main2 = main;
		string sub2 = sub;
		Application.Current.Dispatcher.Invoke(delegate
		{
			ltxt2 = main2;
			ltxt4 = sub2;
		});
	}

	public void setCurrFWVerLabel(string text)
	{
		string text2 = text;
		Application.Current.Dispatcher.Invoke(delegate
		{
			strCurrFWVersionLabel = text2;
			ltxt4 = strCurrFWVersionLabel;
		});
	}

	public void setWarning(string txt = null, bool bUseBinding = true)
	{
		string txt2 = txt;
		Application.Current.Dispatcher.Invoke(delegate
		{
			ltxt7_Text = "";
			if (txt2 != null && txt2.Length > 0)
			{
				ltxt7_Visible = Visibility.Visible;
			}
			else
			{
				ltxt7_Visible = Visibility.Hidden;
			}
		});
	}

	public void SetUserInterface(UserControl newUC)
	{
		UserControl newUC2 = newUC;
		if (newUC2 != null)
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				UserInterface = newUC2;
				OnPropertyChanged("UserInterface");
			});
		}
	}

	public void showRepoVersionList(bool bVisible, List<string> data = null)
	{
		List<string> data2 = data;
		Application.Current.Dispatcher.Invoke(delegate
		{
			if (bVisible)
			{
				RepoVersionList = ((data2 == null) ? new List<string>() : data2);
				DFUView.RepoVersionPanel.Visibility = Visibility.Visible;
				updateTransferProgress(bVisibility: false, -1);
			}
			else
			{
				RepoVersionList = new List<string>();
				DFUView.RepoVersionPanel.Visibility = Visibility.Collapsed;
			}
			OnPropertyChanged("RepoVersionList");
		});
	}

	public void showSelectedRepoVersionInfo(bool bVisible, string text = null)
	{
		string text2 = text;
		Application.Current.Dispatcher.Invoke(delegate
		{
			if (bVisible)
			{
				string text3 = (bUseDefaultFileID ? ($"Using default ID {UniqueFileID[0]:X2}{UniqueFileID[1]:X2}{UniqueFileID[2]:X2}{UniqueFileID[3]:X2}" + Environment.NewLine) : "");
				RepoVersionInfo = ((text2 == null) ? "" : (text3 + text2));
				dongleimage_visibility = Visibility.Collapsed;
				DFUView.RepoVersionInfo.Visibility = Visibility.Visible;
			}
			else
			{
				RepoVersionInfo = "";
				dongleimage_visibility = Visibility.Visible;
				DFUView.RepoVersionInfo.Visibility = Visibility.Collapsed;
			}
		});
	}

	public MessageBoxResult modalMsgBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
	{
		string text2 = text;
		string caption2 = caption;
		MessageBoxResult res = MessageBoxResult.None;
		Application.Current.Dispatcher.Invoke(delegate
		{
			res = MessageBox.Show(text2, caption2, buttons, icon);
		});
		return res;
	}

	public int modalToastBox(string title, string content, string bt1, string bt2 = "", string bt3 = "", int image = 0)
	{
		string title2 = title;
		string content2 = content;
		string bt4 = bt1;
		string bt5 = bt2;
		string bt6 = bt3;
		int res = 0;
		Application.Current.Dispatcher.Invoke(delegate
		{
			if (view != null)
			{
				view.MainGrid.Effect = new BlurEffect();
				view.BlurSplash.Visibility = Visibility.Visible;
			}
			AppToastWindow appToastWindow = new AppToastWindow();
			AppToastWindowViewModel appToastWindowViewModel = new AppToastWindowViewModel();
			appToastWindowViewModel.MainTitle = title2;
			appToastWindowViewModel.MainContent = content2;
			appToastWindowViewModel.ImageIndex = image;
			appToastWindowViewModel.SetButtonCaptions(bt4, bt5, bt6);
			appToastWindow.DataContext = appToastWindowViewModel;
			if (view != null)
			{
				appToastWindow.Left = view.Left + (view.Width - appToastWindow.Width) / 2.0;
				appToastWindow.Top = view.Top + (view.Height - appToastWindow.Height) / 2.0;
			}
			appToastWindow.ShowDialog();
			if (view != null)
			{
				view.BlurSplash.Visibility = Visibility.Collapsed;
				view.MainGrid.Effect = null;
			}
			res = appToastWindowViewModel.iResult;
		});
		return res;
	}

	public void ShowDongleNotFoundLabel(bool bVisibility)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			DongleNotFoundVisibility = ((!bVisibility) ? Visibility.Hidden : Visibility.Visible);
		});
	}

	public void setMiniStatusTextCentered(string txt = null, bool bUseBinding = true)
	{
		string txt2 = txt;
		Application.Current.Dispatcher.Invoke(delegate
		{
			ltxt6_Text = "";
			if (txt2 != null && txt2.Length > 0)
			{
				ltxt6_Visible = Visibility.Visible;
				if (bUseBinding)
				{
					DFUView.ltxt6.SetBinding(TextBlock.TextProperty, new Binding("SelectedLanguage." + txt2));
				}
				else
				{
					ltxt6_Text = txt2;
				}
				ltxt6_Location = HorizontalAlignment.Center;
			}
			else
			{
				ltxt6_Visible = Visibility.Collapsed;
			}
		});
	}

	public void setMiniStatusTextLeft(string txt = null, bool bUseBinding = true)
	{
		string txt2 = txt;
		Application.Current.Dispatcher.Invoke(delegate
		{
			ltxt6_Text = "";
			if (txt2 != null && txt2.Length > 0)
			{
				ltxt6_Visible = Visibility.Visible;
				if (bUseBinding)
				{
					DFUView.ltxt6.SetBinding(TextBlock.TextProperty, new Binding("SelectedLanguage." + txt2));
				}
				else
				{
					ltxt6_Text = txt2;
				}
				ltxt6_Location = HorizontalAlignment.Left;
			}
			else
			{
				ltxt6_Visible = Visibility.Collapsed;
			}
		});
	}

	public void updateTransferProgress(bool bVisibility, int percentage)
	{
		try
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				pbTransfer_Visible = ((!bVisibility) ? Visibility.Hidden : Visibility.Visible);
				if (percentage >= 0)
				{
					pbTransfer_Value = $"{percentage}%";
				}
				else
				{
					pbTransfer_Value = "";
				}
			});
		}
		catch (Exception)
		{
		}
	}

	public void setBt3Text(string txt, bool bUseBinding = true)
	{
		string txt2 = txt;
		Application.Current.Dispatcher.Invoke(delegate
		{
			bt3_Text = "";
			if (txt2 != null && txt2.Length > 0)
			{
				if (bUseBinding)
				{
					DFUView.bt3Text.SetBinding(TextBlock.TextProperty, new Binding("SelectedLanguage." + txt2));
				}
				else
				{
					bt3_Text = txt2;
				}
				bt3_Visible = Visibility.Visible;
			}
			else
			{
				bt3_Visible = Visibility.Collapsed;
			}
		});
	}

	public void setBt3TextGeneral(UserControl view, string txt, bool bUseBinding = true)
	{
		UserControl view2 = view;
		string txt2 = txt;
		Application.Current.Dispatcher.Invoke(delegate
		{
			bt3_Text = "";
			if (view2 != null && txt2 != null && txt2.Length > 0 && view2.FindName("bt3Text") != null && view2.FindName("bt3Text") is TextBlock)
			{
				if (bUseBinding)
				{
					(view2.FindName("bt3Text") as TextBlock).SetBinding(TextBlock.TextProperty, new Binding("SelectedLanguage." + txt2));
				}
				else
				{
					bt3_Text = txt2;
				}
				bt3_Visible = Visibility.Visible;
			}
			else
			{
				bt3_Visible = Visibility.Collapsed;
			}
		});
	}

	public void SetDFUButton3(_EVENT act = _EVENT.BT_NONE, string text = "", bool binding = true)
	{
		bt3Act = act;
		setBt3Text((act == _EVENT.BT_NONE) ? "" : text, act != 0 && binding);
		bt3_Visible = ((act == _EVENT.BT_NONE) ? Visibility.Hidden : Visibility.Visible);
	}

	public void SetDFUButton3Enable(bool bEnable)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			DFUView.bt3.IsEnabled = bEnable;
		});
	}

	public void SetButton3(UserControl view, _EVENT act = _EVENT.BT_NONE, string text = "", bool binding = true)
	{
		bt3Act = act;
		setBt3TextGeneral(view, (act == _EVENT.BT_NONE) ? "" : text, act != 0 && binding);
		bt3_Visible = ((act == _EVENT.BT_NONE) ? Visibility.Hidden : Visibility.Visible);
	}

	public void setBt2Text(string txt, bool bUseBinding = true)
	{
		string txt2 = txt;
		Application.Current.Dispatcher.Invoke(delegate
		{
			bt2_Text = "";
			if (txt2 != null && txt2.Length > 0)
			{
				if (bUseBinding)
				{
					DFUView.bt2Text.SetBinding(TextBlock.TextProperty, new Binding("SelectedLanguage." + txt2));
				}
				else
				{
					bt2_Text = txt2;
				}
				bt2_Visible = Visibility.Visible;
			}
			else
			{
				bt2_Visible = Visibility.Hidden;
			}
		});
	}

	public void SetDFUButton2(_EVENT act = _EVENT.BT_NONE, string text = "", bool binding = true)
	{
		bt2Act = act;
		setBt2Text((act == _EVENT.BT_NONE) ? "" : text, act != 0 && binding);
		bt2_Visible = ((act == _EVENT.BT_NONE) ? Visibility.Hidden : Visibility.Visible);
	}

	public void SetDFUButton2Enable(bool bEnable)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			DFUView.bt2.IsEnabled = bEnable;
		});
	}

	public void setBt1Text(string txt, bool bUseBinding = true)
	{
		string txt2 = txt;
		Application.Current.Dispatcher.Invoke(delegate
		{
			bt1_Text = "";
			if (txt2 != null && txt2.Length > 0)
			{
				if (bUseBinding)
				{
					DFUView.bt1Text.SetBinding(TextBlock.TextProperty, new Binding("SelectedLanguage." + txt2));
				}
				else
				{
					bt1_Text = txt2;
				}
				bt1_Visible = Visibility.Visible;
			}
			else
			{
				bt1_Visible = Visibility.Hidden;
			}
		});
	}

	public void setBt1TextGeneral(UserControl view, string txt, bool bUseBinding = true)
	{
		UserControl view2 = view;
		string txt2 = txt;
		Application.Current.Dispatcher.Invoke(delegate
		{
			bt1_Text = "";
			if (view2 != null && txt2 != null && txt2.Length > 0 && view2.FindName("bt1Text") != null && view2.FindName("bt1Text") is TextBlock)
			{
				if (bUseBinding)
				{
					(view2.FindName("bt1Text") as TextBlock).SetBinding(TextBlock.TextProperty, new Binding("SelectedLanguage." + txt2));
				}
				else
				{
					bt1_Text = txt2;
				}
				bt1_Visible = Visibility.Visible;
			}
			else
			{
				bt1_Visible = Visibility.Hidden;
			}
		});
	}

	public void SetDFUButton1(_EVENT act = _EVENT.BT_NONE, string text = "", bool binding = true)
	{
		bt1Act = act;
		setBt1Text((act == _EVENT.BT_NONE) ? "" : text, act != 0 && binding);
		bt1_Visible = ((act == _EVENT.BT_NONE) ? Visibility.Hidden : Visibility.Visible);
	}

	public void SetDFUButton1Enable(bool bEnable)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			DFUView.bt1.IsEnabled = bEnable;
		});
	}

	public void SetButton1(UserControl view, _EVENT act = _EVENT.BT_NONE, string text = "", bool binding = true)
	{
		bt1Act = act;
		setBt1TextGeneral(view, (act == _EVENT.BT_NONE) ? "" : text, act != 0 && binding);
		bt1_Visible = ((act == _EVENT.BT_NONE) ? Visibility.Hidden : Visibility.Visible);
	}

	private void RefreshDongleDevices()
	{
		foreach (DongleDeviceExt dde in DongleDevices)
		{
			IEnumerable<HidDevice> enumerable = (from h in DeviceList.Local.GetHidDevices()
				where h.VendorID == dde.VID && h.ProductID == dde.PID
				select h).ToList();
			if (dde.DongleSerialNumber == "")
			{
				if (enumerable == null || enumerable.Count() <= 0)
				{
					continue;
				}
				addStatusEntry("Found " + enumerable.Count() + " HID Devices with " + $"VID:{dde.VID:X4} PID:{dde.PID:X4}");
				List<string> list = new List<string>();
				dde.count = 0;
				UsbDeviceDetailConfig usbDeviceDetailConfig = dde.usbDeviceDetailConfig;
				foreach (HidDevice hd2 in enumerable)
				{
					try
					{
						if (list.Where((string s) => s == hd2.GetSerialNumber()).ToList().Count() == 0)
						{
							list.Add(hd2.GetSerialNumber());
							addStatusEntry("Added HIDDev with s/n:" + hd2.GetSerialNumber());
							dde.count++;
						}
						foreach (Report report in hd2.GetReportDescriptor().Reports)
						{
							int num = usbDeviceDetailConfig.byReportID.IndexOf(report.ReportID);
							if (num == -1)
							{
								continue;
							}
							switch (report.ReportType)
							{
							case ReportType.Output:
							{
								HIDDEVICETYPES hIDDEVICETYPES = (HIDDEVICETYPES)(num + 1);
								if (dde.GetHDE(hIDDEVICETYPES).device == null)
								{
									dde.AssignHidDeviceExt(hIDDEVICETYPES, new HidDeviceExt(hd2, report, iVerboseLevel, addLogEntry, hIDDEVICETYPES));
									dde.UIUpdated = false;
									addStatusEntry(" > hde_type:" + hIDDEVICETYPES.ToString() + $", RepID:{report.ReportID:X2}" + ", Report:Output, len:" + report.Length);
								}
								break;
							}
							case ReportType.Feature:
							{
								HIDDEVICETYPES hIDDEVICETYPES = HIDDEVICETYPES.MAIN;
								if (dde.GetHDE(hIDDEVICETYPES).device == null && report.GetAllUsages().Contains((uint)((usbDeviceDetailConfig.u16UsagePage << 16) | usbDeviceDetailConfig.u16Usage)))
								{
									dde.AssignHidDeviceExt(hIDDEVICETYPES, new HidDeviceExt(hd2, report, iVerboseLevel, addLogEntry, hIDDEVICETYPES));
									dde.UIUpdated = false;
									addStatusEntry(" > hde_type:" + hIDDEVICETYPES.ToString() + $", RepID:{report.ReportID:X2}" + ", Report:Feature, len:" + report.Length + $", usage_page:{usbDeviceDetailConfig.u16UsagePage:X4}, usage:{usbDeviceDetailConfig.u16Usage:X4}");
								}
								break;
							}
							}
						}
					}
					catch (Exception)
					{
					}
				}
				if (dde.IsAllDevicesObtained)
				{
					addStatusEntry("==> all hde_types have been obtained.");
					dde.IsDetected = true;
					PostEvent(_EVENT.DEV_ATTACH, dde);
				}
				continue;
			}
			bool flag = false;
			if (enumerable != null && enumerable.Count() > 0)
			{
				List<string> list2 = new List<string>();
				foreach (HidDevice hd in enumerable)
				{
					if (list2.Where((string s) => s == hd.GetSerialNumber()).ToList().Count() == 0)
					{
						list2.Add(hd.GetSerialNumber());
					}
				}
				if (!list2.Contains(dde.DongleSerialNumber))
				{
					flag = true;
				}
				else if ((dde.count = list2.Count()) <= 1)
				{
				}
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				dde.RemoveDevices();
				dde.UIUpdated = false;
				dde.count = 0;
				dde.IsDetected = false;
				PostEvent(_EVENT.DEV_DETACH, dde);
			}
		}
	}

	private void DevListChanged(object? sender, EventArgs e)
	{
		RefreshDongleDevices();
	}

	private void UsbHid_Init()
	{
		foreach (UsbDeviceDetailConfig value in BTDConfig.KnownDevicesDict.Values)
		{
			DongleDevices.Add(new DongleDeviceExt(value));
		}
		DeviceList.Local.Changed += DevListChanged;
		bUsbHidInitialized = true;
	}

	private void UsbHid_Cleanup()
	{
		DeviceList.Local.Changed -= DevListChanged;
		if (DongleDevices == null)
		{
			return;
		}
		foreach (DongleDeviceExt dongleDevice in DongleDevices)
		{
			dongleDevice.CleanUp();
		}
		DongleDevices.Clear();
	}
}
