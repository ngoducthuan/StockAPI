namespace StockAPI.DTOs
{
	public class StocksOverTime
	{
		/// <summary>
		/// stock code
		/// </summary>
		public string symbol { get; set; }

		/// <summary>
		/// status
		/// </summary>
		public string s { get; set; }

		/// <summary>
		/// time
		/// </summary>
		public long[] t { get; set; }

		/// <summary>
		/// close
		/// </summary>
		public double[] c { get; set; } 

		/// <summary>
		/// open
		/// </summary>
		public double[] o { get; set; } 

		/// <summary>
		/// highest price
		/// </summary>
		public double[] h { get; set; } 

		/// <summary>
		/// Lowest price
		/// </summary>
		public double[] l { get; set; } 

		/// <summary>
		/// Volum
		/// </summary>
		public long[] v { get; set; } 
	}

}
