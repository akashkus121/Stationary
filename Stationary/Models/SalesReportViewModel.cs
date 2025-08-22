using System;
using System.Collections.Generic;

namespace Stationary.Models
{
	public class SalesReportViewModel
	{
		public DateTime SelectedDate { get; set; }
		public decimal TotalSalesAmount { get; set; }
		public int TotalOrders { get; set; }
		public int TotalItemsSold { get; set; }

		public List<SalesReportRow> Rows { get; set; } = new List<SalesReportRow>();
	}

	public class SalesReportRow
	{
		public int OrderId { get; set; }
		public DateTime OrderDate { get; set; }
		public string Username { get; set; }
		public int Items { get; set; }
		public decimal Amount { get; set; }
	}
}