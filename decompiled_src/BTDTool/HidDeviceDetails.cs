using System;
using System.Collections.Generic;
using HidSharp;
using HidSharp.Reports;

namespace BTDTool;

public class HidDeviceDetails
{
	public HidDevice hiddev;

	public ReportDescriptor repdesc;

	public List<ReportDetails> featurerepdetailslist;

	public List<ReportDetails> inputrepdetailslist;

	public List<ReportDetails> outputrepdetailslist;

	public HidDeviceDetails(HidDevice h)
	{
		hiddev = h;
		repdesc = hiddev.GetReportDescriptor();
		featurerepdetailslist = new List<ReportDetails>();
		inputrepdetailslist = new List<ReportDetails>();
		outputrepdetailslist = new List<ReportDetails>();
		try
		{
			foreach (Report featureReport in h.GetReportDescriptor().FeatureReports)
			{
				featurerepdetailslist.Add(new ReportDetails(featureReport));
			}
			foreach (Report inputReport in h.GetReportDescriptor().InputReports)
			{
				inputrepdetailslist.Add(new ReportDetails(inputReport));
			}
			foreach (Report outputReport in h.GetReportDescriptor().OutputReports)
			{
				outputrepdetailslist.Add(new ReportDetails(outputReport));
			}
		}
		catch (Exception)
		{
		}
	}
}
