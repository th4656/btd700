namespace BTDTool;

public class CodecItem
{
	public string name;

	public int bitidx;

	public CodecItem(string name, int bitidx)
	{
		this.name = name;
		this.bitidx = bitidx;
	}

	public override string ToString()
	{
		return name;
	}
}
