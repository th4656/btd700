using System.Collections.Generic;

namespace BTDTool;

public static class BTDConfig
{
	public static Dictionary<uint, UsbDeviceDetailConfig> KnownDevicesDict = new Dictionary<uint, UsbDeviceDetailConfig>
	{
		[893530112u] = new UsbDeviceDetailConfig
		{
			dtype = _DEVICETYPE.DEV_BTD600,
			u16VID = 13634,
			u16PID = 12288,
			u16UsagePage = 65280,
			u16Usage = 2,
			byReportID = new List<byte> { 3 },
			strDFUChipModelPrefix = "QCC515Xx",
			strDeviceSKUCode = "700248",
			strDeviceImageFile = "BTD600.png",
			strDeviceName = "BTD 600"
		},
		[893530113u] = new UsbDeviceDetailConfig
		{
			dtype = _DEVICETYPE.DEV_BTD700,
			u16VID = 13634,
			u16PID = 12289,
			u16UsagePage = 65280,
			u16Usage = 2,
			byReportID = new List<byte> { 3, 52 },
			strDFUChipModelPrefix = "QCC518Xx",
			strDeviceSKUCode = "700434",
			strDeviceImageFile = "BTD700.png",
			strDeviceName = "BTD 700"
		}
	};
}
