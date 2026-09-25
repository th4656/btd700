using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using HidSharp.Reports;

namespace BTDTool;

public class DongleDeviceExt : DependencyObject
{
	public UsbDeviceDetailConfig? usbDeviceDetailConfig;

	private string strFingerprint = string.Empty;

	private List<HidDeviceExt>? hidDeviceExts;

	public int count;

	public U_OP uopLastCommandMain = U_OP.INVALID_VALUE;

	public List<bool> WaitingFor = new List<bool>();

	private bool _UIUpdated;

	private string _DongleVersion = "";

	private ushort _u16UHVMajor;

	private ushort _u16UHVMinor;

	private ushort _u16UHVConfig;

	private ushort _u16AppMajor;

	private ushort _u16AppMinor;

	private ushort _u16AppTest;

	private byte _b0;

	private byte _b1;

	private byte _b2;

	private byte _b3;

	private ushort _u16AppRunningNumber;

	private ushort _u16AppSupplInd;

	public int VID
	{
		get
		{
			if (usbDeviceDetailConfig == null)
			{
				return 0;
			}
			return usbDeviceDetailConfig.u16VID;
		}
	}

	public int PID
	{
		get
		{
			if (usbDeviceDetailConfig == null)
			{
				return 0;
			}
			return usbDeviceDetailConfig.u16PID;
		}
	}

	public int NumOfValidDevices
	{
		get
		{
			int num = 0;
			foreach (HidDeviceExt hidDeviceExt in hidDeviceExts)
			{
				if (hidDeviceExt.device != null && hidDeviceExt.type != HIDDEVICETYPES.INVALID)
				{
					num++;
				}
			}
			return num;
		}
	}

	public bool IsAllDevicesObtained => usbDeviceDetailConfig.byReportID.Count + 1 == NumOfValidDevices;

	public bool IsDetected { get; set; }

	public bool IsConnected
	{
		get
		{
			if (hidDeviceExts != null)
			{
				return hidDeviceExts.Count() > 0;
			}
			return false;
		}
	}

	public bool WaitingForMain
	{
		get
		{
			if (WaitingFor[0] && IsDeviceValid(HIDDEVICETYPES.MAIN))
			{
				HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.MAIN);
				WaitingFor[0] = hDE == null || !hDE.GetResponse();
			}
			return WaitingFor[0];
		}
		set
		{
			WaitingFor[0] = value;
		}
	}

	public bool WaitingForApp
	{
		get
		{
			if (WaitingFor[1] && IsDeviceValid(HIDDEVICETYPES.APP))
			{
				HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.APP);
				WaitingFor[1] = hDE == null || !hDE.GetResponse();
			}
			return WaitingFor[1];
		}
		set
		{
			WaitingFor[1] = value;
		}
	}

	public bool WaitingForApp2
	{
		get
		{
			if (WaitingFor[2] && IsDeviceValid(HIDDEVICETYPES.APP2))
			{
				HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.APP2);
				WaitingFor[2] = hDE == null || !hDE.GetResponse();
			}
			return WaitingFor[2];
		}
		set
		{
			WaitingFor[2] = value;
		}
	}

	public string strDevDongleFingerprint
	{
		get
		{
			if (!IsConnected || hidDeviceExts == null || !IsDeviceValid(HIDDEVICETYPES.MAIN))
			{
				return "";
			}
			return hidDeviceExts[0].DeviceFingerprint;
		}
	}

	public string DongleName
	{
		get
		{
			if (usbDeviceDetailConfig != null)
			{
				return usbDeviceDetailConfig.strDeviceName;
			}
			return "";
		}
	}

	public string DongleDeviceName
	{
		get
		{
			if (!IsConnected || hidDeviceExts == null || !IsDeviceValid(HIDDEVICETYPES.MAIN))
			{
				return "";
			}
			return hidDeviceExts[0].DeviceName;
		}
	}

	public string DongleFriendlyName
	{
		get
		{
			if (!IsConnected || hidDeviceExts == null || !IsDeviceValid(HIDDEVICETYPES.MAIN))
			{
				return "";
			}
			return hidDeviceExts[0].FriendlyName;
		}
	}

	public string DongleSerialNumber
	{
		get
		{
			if (!IsConnected || hidDeviceExts == null || !IsDeviceValid(HIDDEVICETYPES.MAIN))
			{
				return "";
			}
			return hidDeviceExts[0].SerialNumber;
		}
	}

	public string DongleFirmwareVersion
	{
		get
		{
			if (!IsConnected || hidDeviceExts == null || !IsDeviceValid(HIDDEVICETYPES.MAIN))
			{
				return "";
			}
			short usMajor;
			short usMinor;
			short usTest;
			return "V" + DFUFile.convertToMerryVersion((uint)(65536 * _u16UHVMajor + _u16UHVMinor), out usMajor, out usMinor, out usTest);
		}
	}

	public string DongleDevicePath
	{
		get
		{
			if (!IsConnected || hidDeviceExts == null || !IsDeviceValid(HIDDEVICETYPES.MAIN))
			{
				return "";
			}
			return hidDeviceExts[0].DevicePath;
		}
	}

	public string DongleImage
	{
		get
		{
			if (usbDeviceDetailConfig != null)
			{
				return usbDeviceDetailConfig.strDeviceImageFile;
			}
			return "";
		}
	}

	public bool UIUpdated
	{
		get
		{
			return _UIUpdated;
		}
		set
		{
			_UIUpdated = value;
		}
	}

	public string DongleVersion
	{
		get
		{
			return _DongleVersion;
		}
		set
		{
			_DongleVersion = value;
		}
	}

	public ushort u16UPErrorCode
	{
		get
		{
			return GetHDE(HIDDEVICETYPES.MAIN)?.u16UPErrorCode ?? 0;
		}
		set
		{
			HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.MAIN);
			if (hDE != null)
			{
				hDE.u16UPErrorCode = value;
			}
		}
	}

	public DongleDeviceExt()
	{
	}

	public DongleDeviceExt(UsbDeviceDetailConfig uddc)
	{
		usbDeviceDetailConfig = uddc;
		hidDeviceExts = new List<HidDeviceExt>();
		for (int i = 0; i < Enum.GetNames(typeof(HIDDEVICETYPES)).Length - 1; i++)
		{
			AddHidDeviceExt(new HidDeviceExt());
			WaitingFor.Add(item: false);
		}
	}

	public bool IncomingFromApp2()
	{
		HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.APP2);
		if (hDE != null && hDE.HasData())
		{
			return hDE.GetResponse();
		}
		return false;
	}

	public bool IsDeviceValid(HIDDEVICETYPES hdt)
	{
		HidDeviceExt hDE = GetHDE(hdt);
		if (hdt.Equals(HIDDEVICETYPES.INVALID) || hDE == null || hDE.type.Equals(HIDDEVICETYPES.INVALID))
		{
			return false;
		}
		return true;
	}

	public override string ToString()
	{
		if (GetHDE(HIDDEVICETYPES.MAIN) == null)
		{
			return "";
		}
		return GetHDE(HIDDEVICETYPES.MAIN).ToString();
	}

	public void SetUpgradeHostVersion(ushort major, ushort minor, ushort config)
	{
		_u16UHVMajor = major;
		_u16UHVMinor = minor;
		_u16UHVConfig = config;
	}

	public (ushort, ushort, ushort) GetUpgradeHostVersion()
	{
		return (_u16UHVMajor, _u16UHVMinor, _u16UHVConfig);
	}

	public string GetUpgradeHostVersionString()
	{
		short usMajor;
		short usMinor;
		short usTest;
		return DFUFile.convertToMerryVersion((uint)(65536 * _u16UHVMajor + _u16UHVMinor), out usMajor, out usMinor, out usTest) + " Cfg Ver: " + _u16UHVConfig;
	}

	public void SetAppVersion(ushort major, ushort minor, ushort test)
	{
		_u16AppMajor = major;
		_u16AppMinor = minor;
		_u16AppTest = test;
	}

	public void SetAppVersionExtra(ushort runningnumber, ushort suppl, byte b0 = 0, byte b1 = 0, byte b2 = 0, byte b3 = 0)
	{
		_u16AppRunningNumber = runningnumber;
		_u16AppSupplInd = suppl;
		_b0 = b0;
		_b1 = b1;
		_b2 = b2;
		_b3 = b3;
	}

	public (ushort, ushort, ushort) GetAppVersion()
	{
		return (_u16AppMajor, _u16AppMinor, _u16AppTest);
	}

	public (ushort, ushort, byte, byte, byte, byte) GetAppVersionExtra()
	{
		return (_u16AppRunningNumber, _u16AppSupplInd, _b0, _b1, _b2, _b3);
	}

	public uint GetAppVersionU32()
	{
		return (uint)(1000000 * _u16AppMajor + 1000 * _u16AppMinor + _u16AppTest);
	}

	public string GetAppVersionString()
	{
		string text = "";
		if (char.IsAsciiLetterOrDigit((char)_b0) || char.IsPunctuation((char)_b0))
		{
			ReadOnlySpan<char> readOnlySpan = text;
			char reference = (char)_b0;
			text = string.Concat(readOnlySpan, new ReadOnlySpan<char>(ref reference));
		}
		if (char.IsAsciiLetterOrDigit((char)_b1) || char.IsPunctuation((char)_b1))
		{
			ReadOnlySpan<char> readOnlySpan2 = text;
			char reference = (char)_b1;
			text = string.Concat(readOnlySpan2, new ReadOnlySpan<char>(ref reference));
		}
		return $"{_u16AppMajor}.{_u16AppMinor}.{_u16AppTest} {text}".Trim();
	}

	public string GetAppVersionExtraString()
	{
		return $"{_b2:X2}{_b3:X2} - {_u16AppRunningNumber:X4}/{_u16AppSupplInd:X4}";
	}

	public byte[] GetCmdBuffer(HIDDEVICETYPES t)
	{
		if (GetHDE(t) == null)
		{
			return null;
		}
		return GetHDE(t).cmd;
	}

	public byte[] GetOpsBuffer(HIDDEVICETYPES t)
	{
		if (GetHDE(t) == null)
		{
			return null;
		}
		return GetHDE(t).ops;
	}

	public byte[] GetResBuffer(HIDDEVICETYPES t)
	{
		if (GetHDE(t) == null)
		{
			return null;
		}
		return GetHDE(t).res;
	}

	public int CmdLength(HIDDEVICETYPES t)
	{
		if (GetCmdBuffer(t) != null)
		{
			return GetCmdBuffer(t).Length;
		}
		return 0;
	}

	public int OpsLength(HIDDEVICETYPES t)
	{
		if (GetOpsBuffer(t) != null)
		{
			return GetOpsBuffer(t).Length;
		}
		return 0;
	}

	public int ResLength(HIDDEVICETYPES t)
	{
		if (GetResBuffer(t) != null)
		{
			return GetResBuffer(t).Length;
		}
		return 0;
	}

	public void AddHidDeviceExt(HidDeviceExt hde)
	{
		if (hidDeviceExts != null)
		{
			hidDeviceExts.Add(hde);
		}
	}

	public bool AssignHidDeviceExt(HIDDEVICETYPES t, HidDeviceExt hde)
	{
		if (hidDeviceExts != null)
		{
			hidDeviceExts[(int)t] = hde;
			return true;
		}
		return false;
	}

	public bool HasReport(Report r)
	{
		if (hidDeviceExts != null)
		{
			foreach (HidDeviceExt hidDeviceExt in hidDeviceExts)
			{
				if (hidDeviceExt.report != null && hidDeviceExt.report.Equals(r))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool HasHDE(HidDeviceExt hde)
	{
		if (hidDeviceExts != null)
		{
			foreach (HidDeviceExt hidDeviceExt in hidDeviceExts)
			{
				if (hidDeviceExt.DeviceFingerprint.Equals(hde.DeviceFingerprint))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool HasHDE(string DeviceFingerprint)
	{
		if (hidDeviceExts != null)
		{
			foreach (HidDeviceExt hidDeviceExt in hidDeviceExts)
			{
				if (hidDeviceExt.DeviceFingerprint == DeviceFingerprint)
				{
					return true;
				}
			}
		}
		return false;
	}

	public HidDeviceExt? GetHDE(HIDDEVICETYPES type)
	{
		if (hidDeviceExts != null && type != HIDDEVICETYPES.INVALID && type >= HIDDEVICETYPES.MAIN && (int)type < hidDeviceExts.Count)
		{
			try
			{
				return hidDeviceExts[(int)type];
			}
			catch (Exception)
			{
				return null;
			}
		}
		return null;
	}

	public bool SendMainHIDCommand(U_HIDCMD hidcmd, bool bNeedResponse)
	{
		HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.MAIN);
		bool result = false;
		if (hDE != null && hDE.device != null)
		{
			result = hDE.sendHIDCommand(hidcmd);
			WaitingForMain = bNeedResponse;
		}
		return result;
	}

	public bool ProcessMainHIDResponse(out U_STATUS status)
	{
		HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.MAIN);
		if (hDE != null && hDE.device != null)
		{
			return hDE.processHIDResponse(out status);
		}
		status = U_STATUS.UPGRADE_STATUS_UNEXPECTED_ERROR;
		return false;
	}

	public void SendMainUPCommand(U_OP upcmd, bool bNeedResponse, byte[] args = null)
	{
		HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.MAIN);
		if (hDE != null && hDE.device != null)
		{
			hDE.sendUPCommand(upcmd, args);
			uopLastCommandMain = upcmd;
			WaitingForMain = bNeedResponse;
		}
	}

	public bool ProcessMainUPResponse(U_OP expectedOp, out byte[] respdata)
	{
		HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.MAIN);
		if (hDE != null && hDE.device != null)
		{
			return hDE.processUPResponse(expectedOp, out respdata);
		}
		respdata = Array.Empty<byte>();
		return false;
	}

	public bool ProcessMainAnyUPResponse(out U_OP op, out byte[] respdata)
	{
		HidDeviceExt hDE = GetHDE(HIDDEVICETYPES.MAIN);
		if (hDE != null && hDE.device != null)
		{
			return hDE.processAnyUPResponse(out op, out respdata);
		}
		op = U_OP.INVALID_VALUE;
		respdata = Array.Empty<byte>();
		return false;
	}

	public void SendAppGenericCommand(HIDDEVICETYPES hdt, byte[] cmd, bool bNeedResponse)
	{
		if (IsDeviceValid(hdt))
		{
			HidDeviceExt hDE = GetHDE(hdt);
			if (hDE != null)
			{
				hDE.sendGenericCommand(hDE.ReportId, cmd);
				WaitingFor[(int)hdt] = bNeedResponse;
			}
		}
	}

	public bool CheckAppGenericResponse(HIDDEVICETYPES hdt)
	{
		if (IsDeviceValid(hdt))
		{
			HidDeviceExt hDE = GetHDE(hdt);
			if (hDE != null)
			{
				return hDE.GetResponse();
			}
		}
		return false;
	}

	public bool ProcessAppGenericResponse(HIDDEVICETYPES hdt, out byte[] respdata)
	{
		if (IsDeviceValid(hdt))
		{
			HidDeviceExt hDE = GetHDE(hdt);
			if (hDE != null)
			{
				return hDE.processGenericResponse(hDE.ReportId, out respdata);
			}
		}
		respdata = Array.Empty<byte>();
		return false;
	}

	public bool ProcessAppIncoming(HIDDEVICETYPES hdt, out byte[] respdata)
	{
		respdata = Array.Empty<byte>();
		if (IsDeviceValid(hdt))
		{
			HidDeviceExt hDE = GetHDE(hdt);
			if (hDE != null && hDE.GetResponse())
			{
				WaitingFor[(int)hdt] = false;
				return hDE.processGenericResponse(hDE.ReportId, out respdata);
			}
		}
		return false;
	}

	public void RemoveDevices()
	{
		if (hidDeviceExts == null)
		{
			return;
		}
		foreach (HidDeviceExt hidDeviceExt in hidDeviceExts)
		{
			hidDeviceExt.Destroy();
		}
		hidDeviceExts = new List<HidDeviceExt>();
		for (int i = 0; i < Enum.GetNames(typeof(HIDDEVICETYPES)).Length - 1; i++)
		{
			AddHidDeviceExt(new HidDeviceExt());
			WaitingFor.Add(item: false);
		}
	}

	public void DisposeMain()
	{
		if (hidDeviceExts != null && hidDeviceExts.Count > 0)
		{
			hidDeviceExts[0].Dispose();
			uopLastCommandMain = U_OP.INVALID_VALUE;
			WaitingForMain = false;
			SetUpgradeHostVersion(0, 0, 0);
		}
	}

	public void CleanUp()
	{
		if (hidDeviceExts == null)
		{
			return;
		}
		foreach (HidDeviceExt hidDeviceExt in hidDeviceExts)
		{
			hidDeviceExt.Destroy();
		}
		hidDeviceExts = null;
	}
}
