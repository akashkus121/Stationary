namespace Stationary.Models
{
    public class StockAlertSummary
    {
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int CriticalStockProducts { get; set; } // Products with stock = 1

        public double InStockPercentage => TotalProducts > 0 ? (double)InStockProducts / TotalProducts * 100 : 0;
        public double LowStockPercentage => TotalProducts > 0 ? (double)LowStockProducts / TotalProducts * 100 : 0;
        public double OutOfStockPercentage => TotalProducts > 0 ? (double)OutOfStockProducts / TotalProducts * 100 : 0;

        public bool HasAlerts => LowStockProducts > 0 || OutOfStockProducts > 0;
        public bool HasCriticalAlerts => CriticalStockProducts > 0 || OutOfStockProducts > 0;
    }
}