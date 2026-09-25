using System.Collections.Generic;

namespace BTDTool;

public class UsbDeviceDetailConfig
{
	public required _DEVICETYPE dtype { get; set; }

	public required ushort u16VID { get; set; }

	public required ushort u16PID { get; set; }

	public required ushort u16UsagePage { get; set; }

	public required ushort u16Usage { get; set; }

	public required List<byte> byReportID { get; set; }

	public required string strDFUChipModelPrefix { get; set; }

	public required string strDeviceSKUCode { get; set; }

	public required string strDeviceImageFile { get; set; }

	public required string strDeviceName { get; set; }
}
