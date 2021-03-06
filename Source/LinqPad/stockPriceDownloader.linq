<Query Kind="Program">
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

void Main()
{
	
	//StockData();
	TestBollinger();
}

void TestBollinger()
{
	var data = GetTestData().ToList();
	var dataWithAvgs = CreateMovingAverage(data);
	CreateStdv(dataWithAvgs);
	//data.Dump();
}

#region Moving Averages

List<HistoricalStockWithR> CreateMovingAverage(List<HistoricalStockWithR> data)
{
	var data2 = data.Select (p => //new {Data = 
		new HistoricalStockWithR
			{
				RowNo=p.RowNo,
				AdjClose=p.AdjClose,
				Close=p.Close,
				Date=p.Date,
				High=p.High,
				Low=p.Low,
				Open=p.Open,
				R=Math.Round(p.High-p.Low,2),
				Volume=p.Volume,
				Avg5 =GetMovingAverage(p.RowNo, 5, data),
				Avg10=GetMovingAverage(p.RowNo, 10, data),
				Avg20=GetMovingAverage(p.RowNo, 20, data),
				Avg30=GetMovingAverage(p.RowNo, 30, data),
				Avg60=GetMovingAverage(p.RowNo, 60, data),
				Avg90=GetMovingAverage(p.RowNo, 90, data)
				//Std5 = GetStdev(p.RowNo, 5, data)
//			}
//			, 
//			p.RowNo, 
//			RowsUsedForAveraging=(p.RowNo>5)?data.OrderBy (d => d.RowNo).Skip(p.RowNo-6).Take(5):null					
		} ).ToList();
	return data2;
	
}

double? GetMovingAverage(int currentRowNo, int days, List<HistoricalStockWithR> data)
{
	double? result = null;
	if(currentRowNo>days)
	{
		result = (double?)data.Skip(currentRowNo-(days+1)).Take(days).Average (da => da.AdjClose);
	}
	return result;
}

#endregion


#region StandardDeviations

void CreateStdv(List<HistoricalStockWithR> data)
{
	var dataWithDevSquared = data.Select (p => //new {Data = 
		new 
			{
				RowNo=p.RowNo,
				AdjClose=p.AdjClose,
				Close=p.Close,
				Date=p.Date,
				High=p.High,
				Low=p.Low,
				Open=p.Open,
				R=p.R,
				Volume=p.Volume,
				Avg5 =p.Avg5,
				Avg10=p.Avg10,
				Avg20=p.Avg20,
				Avg30=p.Avg30,
				Avg60=p.Avg60,
				Avg90=p.Avg90,
				dev5p2=(p.Avg5==null)?null:(double?)(Math.Round(Math.Pow((double)p.Avg5-p.AdjClose, 2), 3)),
				dev10p2=(p.Avg10==null)?null:(double?)(Math.Round(Math.Pow((double)p.Avg10-p.AdjClose, 2),3)),
				dev20p2=(p.Avg20==null)?null:(double?)(Math.Round(Math.Pow((double)p.Avg20-p.AdjClose, 2),3)),
				dev30p2=(p.Avg30==null)?null:(double?)(Math.Round(Math.Pow((double)p.Avg30-p.AdjClose, 2),3)),
				dev60p2=(p.Avg60==null)?null:(double?)(Math.Round(Math.Pow((double)p.Avg60-p.AdjClose, 2),3)),
				dev90p2=(p.Avg90==null)?null:(double?)(Math.Round(Math.Pow((double)p.Avg90-p.AdjClose, 2),3)),
				High14=(p.RowNo>14)?(double?)(data.Skip(p.RowNo-(14+1)).Take(14).Max(x=> x.AdjClose)):null,
				Low14=(p.RowNo>14)?(double?)(data.Skip(p.RowNo-(14+1)).Take(14).Min(x=> x.AdjClose)):null,
				High3=(p.RowNo>3)?(double?)(data.Skip(p.RowNo-(3+1)).Take(3).Max(x=> x.AdjClose)):null,
				Low3=(p.RowNo>3)?(double?)(data.Skip(p.RowNo-(3+1)).Take(3).Min(x=> x.AdjClose)):null,
				//Std5 = GetStdev(p.RowNo, 5, data)
//			}
//			, 
//			p.RowNo, 
//			RowsUsedForAveraging=(p.RowNo>5)?data.OrderBy (d => d.RowNo).Skip(p.RowNo-6).Take(5):null					
		} );
		
	
	var dataWithStd = dataWithDevSquared.Select (p => //new {Data = 
		new 
			{
				RowNo=p.RowNo,
				AdjClose=p.AdjClose,
				Close=p.Close,
				Date=p.Date,
				High=p.High,
				Low=p.Low,
				Open=p.Open,
				R=p.R,
				Volume=p.Volume,
				Avg5 =p.Avg5,
				Avg10=p.Avg10,
				Avg20=p.Avg20,
				Avg30=p.Avg30,
				Avg60=p.Avg60,
				Avg90=p.Avg90,
				Std5=(p.RowNo>5*2)? (double?) Math.Sqrt(dataWithDevSquared.Skip(p.RowNo-(5-1)).Take(5).Average (wds => (double)wds.dev5p2)) :null ,
				Std10=(p.RowNo>10*2)? (double?) Math.Sqrt(dataWithDevSquared.Skip(p.RowNo-(10-1)).Take(10).Average (wds => (double)wds.dev10p2)) :null ,
				Std20=(p.RowNo>20*2)? (double?) Math.Sqrt(dataWithDevSquared.Skip(p.RowNo-(20-1)).Take(20).Average (wds => (double)wds.dev20p2)) :null ,
				Std30=(p.RowNo>30*2)? (double?) Math.Sqrt(dataWithDevSquared.Skip(p.RowNo-(30-1)).Take(30).Average (wds => (double)wds.dev30p2)) :null ,
				Std60=(p.RowNo>60*2)? (double?) Math.Sqrt(dataWithDevSquared.Skip(p.RowNo-(60-1)).Take(60).Average (wds => (double)wds.dev60p2)) :null ,
				Std90=(p.RowNo>90*2)? (double?) Math.Sqrt(dataWithDevSquared.Skip(p.RowNo-(90-1)).Take(90).Average (wds => (double)wds.dev90p2)) :null ,
				Stochastic14=(p.RowNo>14)? (double?)(100*(p.AdjClose-p.Low14)/(p.High14-p.Low14)):null, 
//				Stochastic14Avg3=(p.RowNo>3)? (double?)(100*(p.AdjClose-p.Low3)/(p.High3-p.Low3)):null,
//				StochDiff = (p.RowNo>14)? (double?)(100*((p.AdjClose-p.Low3)/(p.High3-p.Low3) - (p.AdjClose-p.Low14)/(p.High14-p.Low14))):null,
				p.High14,
				p.Low14
		} );
		
	//Calculating Stochastic oscillator using this: http://www.investopedia.com/terms/s/stochasticoscillator.asp
	var dataWithStochasticAvg =  dataWithStd.Select (p => //new {Data = 
		new 
			{
				RowNo=p.RowNo,
				AdjClose=p.AdjClose,
				Close=p.Close,
				Date=p.Date,
				High=p.High,
				Low=p.Low,
				Open=p.Open,
				R=p.R,
				Volume=p.Volume,
				Avg5 =p.Avg5,
				Avg10=p.Avg10,
				Avg20=p.Avg20,
				Avg30=p.Avg30,
				Avg60=p.Avg60,
				Avg90=p.Avg90,
				Std5=p.Std5,
				Std10=p.Std10,
				Std20=p.Std20,
				Std30=p.Std30 ,
				Std60=p.Std60,
				Std90=p.Std90,
				Stochastic14=p.Stochastic14, 
				Stochastic14Avg3=(p.RowNo>14+3)? (double?)(dataWithStd.Skip(p.RowNo-(3+1)).Take(3).Average (z => z.Stochastic14)):null,
				Stochastic14DiffFromAvg3=(p.RowNo>14+3)? (double?)( p.Stochastic14 - dataWithStd.Skip(p.RowNo-(3+1)).Take(3).Average (z => z.Stochastic14)):null,
				High14= p.High14,
				Low14=p.Low14
		} );
	dataWithStochasticAvg.Dump();
}

double? GetStd(int currentRowNo, int days, List<HistoricalStockWithR> data)
{
	double? result = null;
	if(currentRowNo>days)
	{
		result = (double?)data.Skip(currentRowNo-(days+1)).Take(days).Average (da => da.AdjClose);
	}
	return result;
}

#endregion


IEnumerable<HistoricalStockWithR> GetTestData(){
   	var fromDate = DateTime.Now.AddDays(-180);//must be at least 30 days ago
   	var toDate = DateTime.Now;
	int i = 1;
	return HistoricalStockDownloader.DownloadData("msft", fromDate, toDate)
	.OrderBy (p => p.Date )
	.Select(p=>new HistoricalStockWithR
	{
		RowNo=i++,
		AdjClose=p.AdjClose,
		Close=p.Close,
		Date=p.Date,
		High=p.High,
		Low=p.Low,
		Open=p.Open,
		R=Math.Round(p.High-p.Low,2),
		Volume=p.Volume	
	});
}

void StockData()
{
	  var dc  = new DumpContainer().Dump();
	  var creditsHyperlinq =new Hyperlinq("http://www.jarloo.com/get-historical-stock-data/", "courtesy of jarloo.com"); 
	   dc.Content = Util.HorizontalRun(true, "PLease enter a symbol.", creditsHyperlinq);
	   string symbol ="AAPL"; //Console.ReadLine();
	   
	   
	   
	   var fromDate = DateTime.Now.AddDays(-1000);//must be at least 30 days ago
	   var toDate = DateTime.Now;
	   List<HistoricalStock> data = HistoricalStockDownloader.DownloadData(symbol, fromDate, toDate);
		dc.Content = Util.HorizontalRun(true, 
			string.Format("Here's the stock quote for {0}, retrieved {1:###,###} records!", symbol, data.Count),
			creditsHyperlinq);
       foreach (HistoricalStock stock in data.OrderBy (d => d.Date).Take(10))
       {
           Console.WriteLine(string.Format("Date={0} High={1} Low={2} Open={3} Close{4}",stock.Date,stock.High,stock.Low,stock.Open,stock.Close));
       }

		data.Dump();

       Visualizer.printChart(symbol);

}
public class Visualizer
{
		public static void  printChart(string symbol)
		{
			Util.Image("http://chart.finance.yahoo.com/z?s=" + symbol).Dump();
		}

}
  public class HistoricalStockDownloader
    {
        public static List<HistoricalStock> DownloadData(string ticker, DateTime fromDate, DateTime toDate)
        {
            List<HistoricalStock> retval = new List<HistoricalStock>();
 
            using (WebClient web = new WebClient())
            {
				var url=string.Format("http://ichart.finance.yahoo.com/table.csv?s={0}&a={1}&b={2}&c={3}", 
						ticker, 
						fromDate.Month,
						fromDate.Day,
						fromDate.Year);
				//url.Dump();
                string data = web.DownloadString(url);
                 //data.Dump("Data dump");
                data =  data.Replace("r","");
 
                string[] rows = data.Split('\n');
 
                //First row is headers so Ignore it
                for (int i = 1; i < rows.Length; i++)
                {
                    if (rows[i].Replace("n","").Trim() == "") continue;
 					//rows[i].Dump(string.Format("processing row {0}", i));
                    string[] cols = rows[i].Split(',');
 
                    HistoricalStock hs = new HistoricalStock();
					hs.Date = Convert.ToDateTime(cols[0]);
                    hs.Open = Convert.ToDouble(cols[1]);
                    hs.High = Convert.ToDouble(cols[2]);
                    hs.Low = Convert.ToDouble(cols[3]);
                    hs.Close = Convert.ToDouble(cols[4]);
                    hs.Volume = Convert.ToDouble(cols[5]);
                    hs.AdjClose = Convert.ToDouble(cols[6]);
                     
                    retval.Add(hs);
                }
 
                return retval;
            }
        }
    }
	
	public interface IHistoricalPrice {
		int RowNo {get;set;}
		double AdjClose { get; set; }
	}
	
   public class HistoricalStock
    {
        public DateTime Date { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
        public double AdjClose { get; set; }
    }
	public class HistoricalStockWithR:HistoricalStock,IHistoricalPrice
    {
		public int RowNo {get;set;}
		public double R { get; set; }
		public double? Avg5 {get;set;}
		public double? Avg10 {get;set;}
		public double? Avg20 {get;set;}
		public double? Avg30 {get;set;}
		public double? Avg60 {get;set;}
		public double? Avg90 {get;set;}
    }