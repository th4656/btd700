using System;
using System.Collections.Concurrent;
using System.Linq;
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;

namespace BTDTool;

public class HidDeviceExt
{
	public HIDDEVICETYPES type = HIDDEVICETYPES.INVALID;

	public HidDevice? device;

	public Report? report;

	public HidDeviceInputReceiver? inputrec;

	public HidStream? stream;

	public BlockingCollection<byte[]>? resultCollection;

	private LogCallback? addLogEntry;

	private int iVerboseLevel;

	private bool isRunning;

	private bool bWriteTimeout;

	public ushort u16UPErrorCode;

	private bool bPendingResponse;

	public byte[]? cmd;

	public byte[]? ops;

	public byte[]? res;

	public bool IsRunning => isRunning;

	public string DeviceFingerprint
	{
		get
		{
			if (device != null)
			{
				return device.ToString() + device.DevicePath;
			}
			return "";
		}
	}

	public string DeviceName
	{
		get
		{
			if (device != null)
			{
				return device.GetFriendlyName() + " [" + device.GetSerialNumber() + "]";
			}
			return "";
		}
	}

	public string FriendlyName
	{
		get
		{
			if (device != null)
			{
				return device.GetFriendlyName();
			}
			return "";
		}
	}

	public string SerialNumber
	{
		get
		{
			if (device != null)
			{
				return device.GetSerialNumber();
			}
			return "";
		}
	}

	public string DevicePath
	{
		get
		{
			if (device != null)
			{
				return device.DevicePath;
			}
			return "";
		}
	}

	public byte ReportId => (byte)((report != null) ? report.ReportID : 0);

	public HidDeviceExt()
	{
	}

	public HidDeviceExt(HidDevice dev, Report rep, int verbose = 0, LogCallback? logger = null, HIDDEVICETYPES t = HIDDEVICETYPES.MAIN)
	{
		type = t;
		device = dev;
		report = rep;
		addLogEntry = logger;
		iVerboseLevel = verbose;
		if (!device.TryOpen(out stream))
		{
			return;
		}
		stream.WriteTimeout = 3000;
		stream.ReadTimeout = 6000;
		inputrec = device.GetReportDescriptor().CreateHidDeviceInputReceiver();
		cmd = new byte[device.GetMaxFeatureReportLength()];
		ops = new byte[device.GetMaxOutputReportLength()];
		res = new byte[device.GetMaxInputReportLength()];
		resultCollection = new BlockingCollection<byte[]>();
		inputrec.Received += delegate
		{
			byte[] array = new byte[device.GetReportDescriptor().MaxInputReportLength];
			Report report;
			while (inputrec.TryRead(array, 0, out report))
			{
				try
				{
					resultCollection.Add((byte[])array.Clone());
				}
				catch (Exception)
				{
				}
			}
		};
		inputrec.Start(stream);
		isRunning = true;
	}

	public void Destroy()
	{
		Dispose();
		isRunning = false;
		resultCollection = null;
		addLogEntry = null;
		inputrec = null;
		report = null;
		device = null;
	}

	public void Dispose()
	{
		res = null;
		ops = null;
		cmd = null;
		if (stream != null)
		{
			stream.Dispose();
		}
		stream = null;
	}

	public string printBytes(byte[] data, bool spaces = false)
	{
		string text = "";
		for (int i = 0; i < data.Length; i++)
		{
			text += string.Format(spaces ? "{0,2:X2} " : "{0,2:X2}", data[i]);
		}
		return text;
	}

	public bool GetResponse()
	{
		if (resultCollection != null && resultCollection.TryTake(out byte[] item))
		{
			for (int i = 0; i < res.Length; i++)
			{
				res[i] = item[i];
			}
			return true;
		}
		return false;
	}

	public bool HasData()
	{
		if (resultCollection != null)
		{
			return resultCollection.Count > 0;
		}
		return false;
	}

	public bool sendHIDCommand(U_HIDCMD hidcmd)
	{
		if (stream != null && cmd != null && res != null)
		{
			for (int i = 0; i < cmd.Length; i++)
			{
				cmd[i] = 0;
			}
			cmd[0] = 3;
			cmd[1] = 1;
			cmd[2] = (byte)hidcmd;
			try
			{
				if (iVerboseLevel > 0 && addLogEntry != null)
				{
					addLogEntry("->HID_SetFeature_" + $"{(byte)hidcmd,2:X2}" + ((iVerboseLevel > 1) ? (" " + printBytes(cmd)) : ""));
				}
				stream.SetFeature(cmd);
				return true;
			}
			catch (Exception ex)
			{
				if (addLogEntry != null)
				{
					addLogEntry("sendHIDCommand exception: " + ex.ToString());
				}
				return false;
			}
		}
		return false;
	}

	public bool processHIDResponse(out U_STATUS status)
	{
		status = U_STATUS.UPGRADE_STATUS_UNEXPECTED_ERROR;
		if (res != null && res[0] == 6)
		{
			status = (U_STATUS)res[2];
		}
		if (iVerboseLevel > 0 && addLogEntry != null)
		{
			addLogEntry("<-HID_Response_" + $"{(byte)status,2:X2}" + ((iVerboseLevel > 1) ? (" " + ((res != null) ? printBytes(res) : "")) : ""));
		}
		return status == U_STATUS.UPGRADE_STATUS_SUCCESS;
	}

	public void sendUPCommand(U_OP upcmd, byte[] args = null)
	{
		if (stream == null || ops == null || res == null || bWriteTimeout)
		{
			return;
		}
		int num = 5;
		for (int i = 0; i < ops.Length; i++)
		{
			ops[i] = 0;
		}
		ops[0] = 5;
		ops[1] = (byte)(3 + ((args != null) ? args.Length : 0));
		ops[2] = (byte)upcmd;
		if (args != null)
		{
			ops[3] = (byte)(args.Length / 256);
			ops[4] = (byte)(args.Length % 256);
			int num2 = 0;
			while (num2 < args.Length)
			{
				ops[5 + num2] = args[num2];
				num2++;
				num++;
			}
		}
		try
		{
			if (iVerboseLevel > 0 && addLogEntry != null)
			{
				addLogEntry("->" + (UPM.OpDict.TryGetValue(upcmd, out string value) ? value : "unknown") + ((iVerboseLevel > 1) ? (" " + printBytes(ops.Take(num).ToArray())) : ""));
			}
			bWriteTimeout = false;
			stream.Write(ops);
		}
		catch (Exception ex)
		{
			if (iVerboseLevel > 1 && addLogEntry != null)
			{
				addLogEntry(ex.ToString());
			}
			if (ex is TimeoutException)
			{
				bWriteTimeout = true;
			}
		}
	}

	public bool processUPResponse(U_OP expectedOp, out byte[] respdata)
	{
		int num = 5;
		respdata = null;
		u16UPErrorCode = 0;
		if (res == null)
		{
			return false;
		}
		if (res[0] == 6 && res[2] == (byte)expectedOp)
		{
			int num2 = 256 * res[3] + res[4];
			if (num2 > 0)
			{
				respdata = new byte[num2];
				Array.Copy(res, 5, respdata, 0, num2);
				num += num2;
			}
			if (iVerboseLevel > 0 && addLogEntry != null)
			{
				addLogEntry("<-" + ((!UPM.OpDict.TryGetValue(expectedOp, out string value)) ? "unknown_op" : value) + ((iVerboseLevel > 1) ? (" " + printBytes(res.Take(num).ToArray())) : ""));
			}
			return true;
		}
		num += 2;
		respdata = new byte[res.Length];
		Array.Copy(res, respdata, res.Length);
		if (res[0] == 6 && res[2] == 17)
		{
			string value2 = "";
			u16UPErrorCode = (ushort)(256 * res[5] + res[6]);
			if (addLogEntry != null)
			{
				addLogEntry("<-" + ((!UPM.ErrorDict.TryGetValue((U_ERR)u16UPErrorCode, out value2)) ? "unknown_error" : value2) + " " + printBytes(res.Take(num).ToArray()));
			}
			sendUPCommand(U_OP.UPGRADE_ERROR_RES, new ArraySegment<byte>(res, 5, 2).ToArray());
		}
		else if (res[0] == 6 && res[2] == 8)
		{
			if (addLogEntry != null)
			{
				addLogEntry("<- Late ABORT_CFM received " + printBytes(res.Take(num).ToArray()));
			}
		}
		else
		{
			u16UPErrorCode = ushort.MaxValue;
			if (addLogEntry != null)
			{
				addLogEntry("<-unexpected_res " + printBytes(res.Take(num).ToArray()));
			}
		}
		return false;
	}

	public bool processAnyUPResponse(out U_OP op, out byte[] respdata)
	{
		int num = 5;
		respdata = null;
		op = U_OP.INVALID_VALUE;
		u16UPErrorCode = 0;
		if (res != null && res[0] == 6)
		{
			if (res[2] != 17)
			{
				op = (U_OP)res[2];
				int num2 = 256 * res[3] + res[4];
				num += num2;
				if (num2 > 0)
				{
					respdata = new byte[num2];
					Array.Copy(res, 5, respdata, 0, num2);
				}
				if (iVerboseLevel > 0 && addLogEntry != null)
				{
					addLogEntry("<-" + ((!UPM.OpDict.TryGetValue(op, out string value)) ? "unknown_op" : value) + ((iVerboseLevel > 1) ? (" " + printBytes(res.Take(num).ToArray())) : ""));
				}
				return true;
			}
			num += 2;
			u16UPErrorCode = (ushort)(256 * res[5] + res[6]);
			string value2 = "";
			if (!UPM.ErrorDict.TryGetValue((U_ERR)u16UPErrorCode, out value2))
			{
				value2 = "unknown_error";
			}
			if (addLogEntry != null)
			{
				addLogEntry("<-" + value2 + " " + printBytes(res.Take(num).ToArray()));
			}
			sendUPCommand(U_OP.UPGRADE_ERROR_RES, new ArraySegment<byte>(res, 5, 2).ToArray());
		}
		else if (iVerboseLevel > 0 && addLogEntry != null)
		{
			addLogEntry("<-unexpected_res " + printBytes(res));
		}
		return false;
	}

	public void sendGenericCommand(byte reportID, byte[] cmd)
	{
		int num = 1;
		if (stream == null || ops == null || res == null)
		{
			return;
		}
		Array.Fill(ops, (byte)0, 0, ops.Length);
		ops[0] = reportID;
		Array.Copy(cmd, 0, ops, 1, cmd.Length);
		num += cmd.Length;
		try
		{
			if (iVerboseLevel > 0 && addLogEntry != null)
			{
				addLogEntry("-> " + printBytes(ops.Take(num).ToArray()));
			}
			stream.Write(ops);
		}
		catch (Exception ex)
		{
			if (iVerboseLevel > 1 && addLogEntry != null)
			{
				addLogEntry(ex.ToString());
			}
		}
	}

	public bool processGenericResponse(byte reportID, out byte[] respdata)
	{
		int num = 4;
		respdata = null;
		if (res != null && res.Length != 0 && res[0] == reportID)
		{
			respdata = (byte[])res.Clone();
			num += res[3];
			if (iVerboseLevel > 0 && addLogEntry != null)
			{
				addLogEntry("<- " + printBytes(res.Take(num).ToArray()));
			}
			for (int i = 0; i < res.Length; i++)
			{
				res[i] = 0;
			}
			return true;
		}
		return false;
	}
}
