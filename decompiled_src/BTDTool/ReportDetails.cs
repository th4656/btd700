using System.Collections.Generic;
using System.Linq;
using HidSharp.Reports;

namespace BTDTool;

public class ReportDetails
{
	public Report rep;

	public List<uint> extusagelist;

	public ReportDetails(Report r)
	{
		rep = r;
		extusagelist = r.GetAllUsages().ToList();
	}
}
