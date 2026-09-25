namespace BTDTool;

public class EventObj
{
	public _EVENT e = _EVENT.NONE;

	public object? o;

	public EventObj()
	{
	}

	public EventObj(_EVENT e, object? o)
	{
		this.e = e;
		this.o = o;
	}
}
