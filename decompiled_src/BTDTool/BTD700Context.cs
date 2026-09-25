using System;
using System.Collections.Generic;
using System.Text;

namespace BTDTool;

public class BTD700Context
{
	public _BTD700_STATES state;

	public _BTD700_AUDIO_MODE audioMode;

	public _BTD700_AUDIO_RESOLUTION audioResolution;

	public _BTD700_AUDIO_FREQUENCY audioFrequency;

	public _BTD700_TRANSPORT_MODE transportMode;

	public _BTD700_TRANSPORT_MODE connectedTransportMode;

	public _BTD700_LEAUDIO_STATE LEAstate;

	public ushort u16SupportedCodecs;

	public ushort u16UsedCodecs;

	public int iSelectedApplication;

	public int iGamingAvailable;

	public _BTD700_BROADCAST_STATE broadcastState;

	public _BTD700_BROADCAST_QUALITY broadcastQuality;

	public _BTD700_BROADCAST_ENCRYPTION broadcastEncrypt;

	public byte[] broadcastEncKey;

	public byte[] broadcastName;

	public string privacyCode = "";

	public _BTD700_SINKMODE sinkTransportMode;

	public byte[] broadcastInfoAsByteArray => new byte[3]
	{
		(byte)broadcastState,
		(byte)broadcastQuality,
		(byte)broadcastEncrypt
	};

	public BTD700Context()
	{
		state = _BTD700_STATES.S_NONE;
		broadcastEncKey = new byte[0];
		broadcastName = new byte[0];
		u16SupportedCodecs = 0;
		u16UsedCodecs = 0;
		iSelectedApplication = -1;
		iGamingAvailable = -1;
	}

	public void ImportBroadcastContext(BTD700BroadcastContext bcastctx)
	{
		broadcastState = bcastctx.broadcastState;
		broadcastQuality = bcastctx.broadcastQuality;
		broadcastEncrypt = bcastctx.broadcastEncrypt;
		broadcastName = new byte[0];
		if (bcastctx.broadcastName != null && bcastctx.broadcastName.Length != 0)
		{
			Array.Copy(bcastctx.broadcastName, broadcastName = new byte[bcastctx.broadcastName.Length], bcastctx.broadcastName.Length);
		}
		broadcastEncKey = new byte[0];
		if (bcastctx.broadcastEncKey != null && bcastctx.broadcastEncKey.Length != 0)
		{
			Array.Copy(bcastctx.broadcastEncKey, broadcastEncKey = new byte[bcastctx.broadcastEncKey.Length], bcastctx.broadcastEncKey.Length);
		}
	}

	public bool CompareNullableByteArray(byte[] a1, byte[] a2)
	{
		bool result = true;
		if (a1 == null)
		{
			if (a2 != null && a2.Length != 0)
			{
				result = false;
			}
		}
		else if (a1.Length == 0)
		{
			if (a2 != null && a2.Length != 0)
			{
				result = false;
			}
		}
		else if (a2 == null)
		{
			result = false;
		}
		else if (a2.Length == 0)
		{
			result = false;
		}
		else
		{
			try
			{
				string text = Encoding.ASCII.GetString(a1).TrimStart('\0').TrimEnd('\0');
				string text2 = Encoding.ASCII.GetString(a2).TrimStart('\0').TrimEnd('\0');
				result = text == text2;
			}
			catch (Exception)
			{
				result = false;
			}
		}
		return result;
	}

	public bool BcastContextIsDifferentFrom(BTD700BroadcastContext bcastctx, out bool quality, out bool state, out bool name, out bool enckey)
	{
		state = broadcastState != bcastctx.broadcastState;
		quality = broadcastQuality != bcastctx.broadcastQuality;
		bool flag = broadcastEncrypt != bcastctx.broadcastEncrypt;
		name = !CompareNullableByteArray(broadcastName, bcastctx.broadcastName);
		enckey = !CompareNullableByteArray(broadcastEncKey, bcastctx.broadcastEncKey);
		return ((state | quality) || flag) | name | enckey;
	}

	public List<CodecItem> GetSupportedCodecList()
	{
		List<CodecItem> list = new List<CodecItem>(0);
		if (((uint)u16SupportedCodecs & (true ? 1u : 0u)) != 0)
		{
			list.Add(new CodecItem("SBC", 0));
		}
		if ((u16SupportedCodecs & 2u) != 0)
		{
			list.Add(new CodecItem("aptX Classic", 1));
		}
		if ((u16SupportedCodecs & 4u) != 0)
		{
			list.Add(new CodecItem("aptX Adaptive", 2));
		}
		if ((u16SupportedCodecs & 8u) != 0)
		{
			list.Add(new CodecItem("aptX Lossless", 3));
		}
		if ((u16SupportedCodecs & 0x10u) != 0)
		{
			list.Add(new CodecItem("aptX Lite (QMAP)", 4));
		}
		if ((u16SupportedCodecs & 0x20u) != 0)
		{
			list.Add(new CodecItem("LC3", 5));
		}
		return list;
	}

	public List<TransportItem> GetSupportedTransportList()
	{
		List<TransportItem> list = new List<TransportItem>(0);
		if (((uint)(byte)sinkTransportMode & (true ? 1u : 0u)) != 0)
		{
			list.Add(new TransportItem("BT Classic", 1));
		}
		if (((byte)sinkTransportMode & 2u) != 0)
		{
			list.Add(new TransportItem("LE Audio", 2));
		}
		return list;
	}

	public int GetCodecInUseIdx()
	{
		int result = -1;
		for (int i = 0; i < Enum.GetNames(typeof(_BTD700_CODEC_BITS)).Length; i++)
		{
			if ((u16UsedCodecs & (1 << i)) > 0)
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public string GetCodecInUseName()
	{
		string result = "";
		for (int i = 0; i < Enum.GetNames(typeof(_BTD700_CODEC_BITS)).Length; i++)
		{
			if ((u16UsedCodecs & (1 << i)) > 0)
			{
				switch (i)
				{
				case 0:
					result = "SBC";
					break;
				case 1:
					result = "aptX Classic";
					break;
				case 2:
					result = "aptX Adaptive";
					break;
				case 3:
					result = "aptX Lossless";
					break;
				case 4:
					result = "aptX Lite (QMAP)";
					break;
				case 5:
					result = "LC3";
					break;
				}
				break;
			}
		}
		return result;
	}
}
