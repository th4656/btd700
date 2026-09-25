using System.Collections.Generic;

namespace BTDTool;

public static class BTD700Tool
{
	public static byte BTD700_REPORT_ID = 52;

	public static byte[] HostCmd(_BTD700_HOSTCMD cmd, byte[]? args = null)
	{
		List<byte> list = new List<byte>();
		list.Add(254);
		list.Add((byte)cmd);
		if (args != null)
		{
			list.Add((byte)args.Length);
			foreach (byte item in args)
			{
				list.Add(item);
			}
		}
		else
		{
			list.Add(0);
		}
		return list.ToArray();
	}

	public static byte[] HostResp(_BTD700_DONGLECMD cmd)
	{
		return new byte[3]
		{
			253,
			(byte)cmd,
			0
		};
	}
}
