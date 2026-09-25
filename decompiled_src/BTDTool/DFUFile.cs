using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BTDTool;

public class DFUFile
{
	public class PartitionHeader
	{
		public uint u32Length;

		public ushort u16Type;

		public ushort u16Number;

		public PartitionHeader(uint len, ushort type, ushort number)
		{
			u32Length = len;
			u16Type = type;
			u16Number = number;
		}
	}

	private const string TEMP_PREFIX = "__tmp";

	public bool isValid;

	public string strError;

	public string strInfo;

	public string strShortInfo;

	public string strVersionInfo;

	public string strMerryVersionInfo;

	public string strChipModel;

	public string filepath;

	private BinaryReader reader;

	private BinaryWriter writer;

	private byte[] strFixedID;

	private uint u32Length;

	private byte[] strDevVariant;

	private uint u32VerMajorMinor;

	private ushort u16NumOfCompVersion;

	private uint[] arr_u32CompVersionList;

	private ushort u16VerConfig;

	private ushort u16NumOfCompConfig;

	private ushort[] arr_u16CompConfigList;

	private uint u32GeneralLength;

	private byte[] strSectionHeaderID;

	private byte[] OEMSignature;

	public byte[] FileSignature;

	private LinkedList<PartitionHeader> PartitionList;

	public static ushort Swap16(ushort u16)
	{
		return (ushort)(256 * (u16 % 256) + u16 / 256);
	}

	public static uint Swap32(uint u32)
	{
		return ((u32 & 0xFF) << 24) + ((u32 & 0xFF00) << 8) + ((u32 & 0xFF0000) >> 8) + ((u32 & 0xFF000000u) >> 24);
	}

	private void Cleanup()
	{
		strFixedID = null;
		strDevVariant = null;
		strSectionHeaderID = null;
		OEMSignature = null;
		arr_u32CompVersionList = null;
		arr_u16CompConfigList = null;
		if (PartitionList != null)
		{
			PartitionList.Clear();
		}
		PartitionList = null;
		if (reader != null)
		{
			reader.Close();
		}
		reader = null;
		FileSignature = null;
	}

	public void unsetFile()
	{
		try
		{
			Cleanup();
			isValid = false;
			strError = "";
			strInfo = "";
			strShortInfo = "";
			strChipModel = "";
			if (filepath != null && filepath.EndsWith("__tmp"))
			{
				File.Delete(filepath);
			}
			filepath = "";
		}
		catch (Exception)
		{
		}
	}

	public DFUFile()
	{
		unsetFile();
	}

	public DFUFile(string strFilePath)
	{
		setFile(strFilePath);
	}

	public static string convertToMerryVersion(uint u32Ver, out short usMajor, out short usMinor, out short usTest)
	{
		usMajor = (short)(u32Ver / 65536);
		usMinor = (short)(u32Ver % 65536 / 100);
		usTest = (short)(u32Ver % 65536 % 100);
		return $"{usMajor}.{usMinor}.{usTest}";
	}

	public bool checkIfVersionIsCompatible(uint u32Ver, ushort u16CfgVer)
	{
		bool result = false;
		if (isValid && arr_u32CompVersionList != null)
		{
			uint[] array = arr_u32CompVersionList;
			foreach (uint num in array)
			{
				if ((num / 65536 == u32Ver / 65536 && num % 65536 == 65535) || num % 65536 == u32Ver % 65536)
				{
					result = true;
					break;
				}
			}
		}
		return result;
	}

	public void createCompatibleDFUFile(uint u32TargetVer, ushort u16TargetCfgVer)
	{
		if (!isValid)
		{
			return;
		}
		uint num = u32TargetVer;
		string text = filepath + "__tmp";
		try
		{
			reader.BaseStream.Seek(0L, SeekOrigin.Begin);
			writer = new BinaryWriter(File.Open(text, FileMode.Create));
			writer.Write(reader.ReadBytes(8));
			uint num2 = 4 + Swap32(reader.ReadUInt32());
			writer.Write((byte)((num2 >> 24) & 0xFFu));
			writer.Write((byte)((num2 >> 16) & 0xFFu));
			writer.Write((byte)((num2 >> 8) & 0xFFu));
			writer.Write((byte)(num2 & 0xFFu));
			writer.Write(reader.ReadBytes(12));
			ushort num3 = (ushort)(1 + Swap16(reader.ReadUInt16()));
			reader.BaseStream.Seek(4 * (num3 - 1), SeekOrigin.Current);
			writer.Write((byte)((uint)(num3 >> 8) & 0xFFu));
			writer.Write((byte)(num3 & 0xFFu));
			uint[] array = arr_u32CompVersionList;
			foreach (uint num4 in array)
			{
				if (num4 > num)
				{
					writer.Write((byte)((num >> 24) & 0xFFu));
					writer.Write((byte)((num >> 16) & 0xFFu));
					writer.Write((byte)((num >> 8) & 0xFFu));
					writer.Write((byte)(num & 0xFFu));
					num = uint.MaxValue;
				}
				writer.Write((byte)((num4 >> 24) & 0xFFu));
				writer.Write((byte)((num4 >> 16) & 0xFFu));
				writer.Write((byte)((num4 >> 8) & 0xFFu));
				writer.Write((byte)(num4 & 0xFFu));
			}
			if (num != uint.MaxValue)
			{
				writer.Write((byte)((num >> 24) & 0xFFu));
				writer.Write((byte)((num >> 16) & 0xFFu));
				writer.Write((byte)((num >> 8) & 0xFFu));
				writer.Write((byte)(num & 0xFFu));
				num = uint.MaxValue;
			}
			writer.Write(reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position)));
			writer.Flush();
			writer.Close();
			writer = null;
			unsetFile();
			setFile(text);
		}
		catch (Exception)
		{
		}
	}

	public long getSize()
	{
		if (!isValid)
		{
			return -1L;
		}
		return reader.BaseStream.Length;
	}

	public long getPosition()
	{
		if (!isValid)
		{
			return -1L;
		}
		return reader.BaseStream.Position;
	}

	public uint getFileData(long offset, uint length, out byte[] output)
	{
		output = null;
		if (isValid)
		{
			uint num = length;
			try
			{
				reader.BaseStream.Seek(offset, SeekOrigin.Current);
				if (num != 0)
				{
					byte[] array = reader.ReadBytes((int)num);
					num = (uint)array.Length;
					output = new byte[1 + num];
					output[0] = ((reader.BaseStream.Position >= reader.BaseStream.Length) ? ((byte)1) : ((byte)0));
					if (num != 0)
					{
						Array.Copy(array, 0L, output, 1L, num);
					}
					return num;
				}
				strError = "getFileData error : offset is beyond filelength";
			}
			catch (Exception ex)
			{
				strError = "getFileData exception : " + ex.ToString();
			}
		}
		else
		{
			strError = "No valid firmware file opened";
		}
		return 0u;
	}

	public void resetFile()
	{
		if (isValid)
		{
			reader.BaseStream.Seek(0L, SeekOrigin.Begin);
		}
	}

	public void setFile(string strFilePath)
	{
		unsetFile();
		if (!File.Exists(strFilePath))
		{
			return;
		}
		try
		{
			FileStream fileStream = File.Open(strFilePath, FileMode.Open);
			FileSignature = Crc32.GetCrc32(MD5.HashData(new BinaryReader(fileStream).ReadBytes((int)fileStream.Length)));
			fileStream.Close();
			filepath = strFilePath;
			strInfo = strFilePath + "\n\n";
			fileStream = File.Open(strFilePath, FileMode.Open);
			reader = new BinaryReader(fileStream);
			PartitionList = new LinkedList<PartitionHeader>();
			strFixedID = reader.ReadBytes(8);
			if (!Encoding.UTF8.GetString(strFixedID).Equals("APPUHDR5"))
			{
				isValid = false;
				strError = "APPUHDR5 string not found in DFU header";
				Cleanup();
				return;
			}
			u32Length = Swap32(reader.ReadUInt32());
			strDevVariant = reader.ReadBytes(8);
			if (!Encoding.UTF8.GetString(strDevVariant).Substring(0, 3).Equals("QCC"))
			{
				isValid = false;
				strError = "Device Variant does not specify QCC";
				Cleanup();
				return;
			}
			strChipModel = Encoding.Default.GetString(strDevVariant);
			strInfo = strInfo + "Device Variant : " + Encoding.UTF8.GetString(strDevVariant) + "\n";
			u32VerMajorMinor = Swap32(reader.ReadUInt32());
			strInfo = strInfo + "Firmware Version : " + u32VerMajorMinor / 65536 + "." + u32VerMajorMinor % 65536 + "\n";
			strInfo += "Compatible Firmware Version : ";
			u16NumOfCompVersion = Swap16(reader.ReadUInt16());
			if (u16NumOfCompVersion > 0)
			{
				arr_u32CompVersionList = new uint[u16NumOfCompVersion];
				for (int i = 0; i < u16NumOfCompVersion; i++)
				{
					arr_u32CompVersionList[i] = Swap32(reader.ReadUInt32());
					strInfo = strInfo + arr_u32CompVersionList[i] / 65536 + "." + arr_u32CompVersionList[i] % 65536 + "; ";
				}
			}
			strInfo += "\n";
			u16VerConfig = Swap16(reader.ReadUInt16());
			strInfo = strInfo + "Config Version : " + u16VerConfig + "\n";
			strInfo += "Compatible Config Version : ";
			strShortInfo = "DFU File Version: " + u32VerMajorMinor / 65536 + "." + u32VerMajorMinor % 65536 + "  Config Version: " + u16VerConfig + "  Signature: " + $"{FileSignature[0]:X2}{FileSignature[1]:X2}{FileSignature[2]:X2}{FileSignature[3]:X2}" + "  partitions: ";
			strVersionInfo = "DFU File Version: " + u32VerMajorMinor / 65536 + "." + u32VerMajorMinor % 65536 + "   Cfg Version: " + u16VerConfig;
			strMerryVersionInfo = "DFUFile v" + convertToMerryVersion(u32VerMajorMinor, out var _, out var _, out var _) + "   Cfg v" + u16VerConfig;
			u16NumOfCompConfig = Swap16(reader.ReadUInt16());
			if (u16NumOfCompConfig > 0)
			{
				arr_u16CompConfigList = new ushort[u16NumOfCompConfig];
				for (int j = 0; j < u16NumOfCompConfig; j++)
				{
					arr_u16CompConfigList[j] = Swap16(reader.ReadUInt16());
					strInfo = strInfo + arr_u16CompConfigList[j] + "; ";
				}
			}
			strInfo += "\n\n";
			while (reader.BaseStream.Position < u32Length + 12)
			{
				reader.ReadByte();
			}
			while (true)
			{
				strSectionHeaderID = reader.ReadBytes(8);
				if (!Encoding.UTF8.GetString(strSectionHeaderID).Equals("PARTDATA"))
				{
					break;
				}
				u32GeneralLength = Swap32(reader.ReadUInt32());
				PartitionHeader partitionHeader;
				PartitionList.AddLast(partitionHeader = new PartitionHeader(u32GeneralLength, 0, 0));
				partitionHeader.u16Type = Swap16(reader.ReadUInt16());
				partitionHeader.u16Number = Swap16(reader.ReadUInt16());
				reader.BaseStream.Seek(u32GeneralLength - 4, SeekOrigin.Current);
				strInfo = strInfo + " partition " + partitionHeader.u16Number + " type " + partitionHeader.u16Type + " length " + partitionHeader.u32Length + "\n";
				strShortInfo = strShortInfo + "[" + partitionHeader.u16Number + ":" + partitionHeader.u16Type + "] ";
			}
			if (Encoding.UTF8.GetString(strSectionHeaderID).Equals("APPUPFTR"))
			{
				u32GeneralLength = Swap32(reader.ReadUInt32());
				OEMSignature = reader.ReadBytes((int)u32GeneralLength);
				strInfo = strInfo + "\nOEM Signature (" + u32GeneralLength + " bytes) :\n";
				int num = 0;
				int num2 = 0;
				while (num < u32GeneralLength)
				{
					if (num2 > 15)
					{
						strInfo += "\n";
						num2 = 0;
					}
					strInfo += $"{OEMSignature[num],2:X2} ";
					num++;
					num2++;
				}
				isValid = true;
				reader.BaseStream.Seek(0L, SeekOrigin.Begin);
				reader.BaseStream.Seek(0L, SeekOrigin.Begin);
				strError = "";
			}
			else
			{
				isValid = false;
				strError = "Invalid content after header, PARTDATA or APPUPFTR not found";
				Cleanup();
			}
		}
		catch (Exception ex)
		{
			isValid = false;
			strError = ex.ToString();
			Cleanup();
		}
	}
}
