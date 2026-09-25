namespace BTDTool;

public class TransportItem
{
	public string name;

	public byte value;

	public TransportItem(string name, byte value)
	{
		this.name = name;
		this.value = value;
	}

	public override string ToString()
	{
		return name;
	}
}
