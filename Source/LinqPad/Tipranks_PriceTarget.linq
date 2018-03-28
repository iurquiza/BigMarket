<Query Kind="Program">
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
  <Namespace>System.Globalization</Namespace>
</Query>

DumpContainer dc = new DumpContainer().Dump();
void Main()
{
	//GetFreshData("FGEN").Dump();
	//return;
	
	var cwd = new FileInfo(Util.CurrentQueryPath).Directory.FullName;
	var symbols = GetSymbolsFromCsv(cwd + @"\data\watchlist.csv");
	
	var results = new List<MyModel>();
	results.AddRange(GetCleanData(symbols));

	results
		.Select(r => new {
			r.Symbol,
			r.Price,
			r.EstLow,
			r.EstHigh,
			r.EstAvg,
			r.EstCount,
			r.PctLow,
			r.PctHigh,
			r.PctAvg,
			r.FollowerCount,
			LastUpdated = r.Experts.Where(e => e!=null).Select(e => e.LastUpdated).FirstOrDefault(), 
			MostRecentTarget = r.Experts.Where(e => e!=null && e.Ratings!=null).Select(e => e.Ratings.Select(ra => ra.Target).FirstOrDefault()).FirstOrDefault(), 
		})
		
		.Dump("all", true);


	results.Where(r => r.Experts.Any(e => e.LastUpdated >= DateTime.Today.AddDays(-30)))
		.Select(r => new
		{
			
			Prices = r.Experts.Where(e => e.LastUpdated!=null && e.LastUpdated>=DateTime.Today.AddDays(-30)).Select(e => new {
				Ratings = e.Ratings.Select(x => new {
					Symbol=r.Symbol.Dump(),
					r.Price,
					e.TargetPrice,
					e.Stars,
					e.SuccessRate, 
					e.AvgReturn,
					e.TotalRecomendations,
					Date = x.Date.Date.ToShortDateString(),
					PctDelta = !e.TargetPrice.HasValue ? 0 : Math.Round((e.TargetPrice.Value - r.Price) / r.Price * 100, 2),
					r.PctLow,
					r.PctHigh,
					r.PctAvg,
					Title = x.Quote==null?null: new Hyperlinq(x.Quote.Link, x.Quote.Title),
				  	Quote = x.Quote==null?null:x.Quote.QuoteQuote,
				})
			})
		})
		.SelectMany(x => x.Prices)
		.SelectMany(x => x.Ratings)
		.Dump("Recently rated", true)
		.Where(x => x.SuccessRate>0.6)
		.Dump("Recently rated by analysts with good track record", true);
		
		
		
	results
			.Where(r => r.PctLow>0 && r.PctLow*1.5<r.PctHigh)
			.Dump("Low estimate is greater than zero and high estimate is twice the low estimate");
	
}

[Serializable]
public class MyModel { 
	
	public string Symbol { get; set; }
	public double Price { get; set; }
	public double EstLow { get; set; }
	public double EstHigh { get; set; }
	public double EstAvg { get; set; }
	public int EstCount { get; set; }
	public double PctLow { get {return Math.Round((EstLow-Price)/Price*100, 2);} }
	public double PctHigh { get {return Math.Round((EstHigh-Price)/Price*100, 2);} }
	public double PctAvg { get {return Math.Round((EstAvg-Price)/Price*100, 2);} }
	public long FollowerCount {get;set;}
	public List<MyExpert> Experts {get;set;}
	
}

[Serializable]
public class MyExpert
{
	public double? TargetPrice { get; set; }
	public double Stars { get; set; }
	public DateTimeOffset LastUpdated { get; set; }
	public string Name {get;set;}
	public string Firm {get;set;}
	public double SuccessRate { get; set; }
	public double AvgReturn { get; set; }
	public long TotalRecomendations { get; set; }
	public long GoodRecomendations { get; set; }
	public List<MyRating> Ratings {get;set;}
	
}

	[Serializable]
public class MyRating
{
	public DateTimeOffset Date { get; set; }
	public double? Target {get;set;}
	public Quote Quote { get; set; }
	
}
	List<MyModel> GetCleanData(List<string> symbols)
{
	var results = new List<MyModel>();
	foreach (var symbol in symbols)
	{
		results.Add(Util.Cache(() => { return GetFreshData(symbol); }, symbol));
	}
	return results;
}

List<string> GetSymbolsFromCsv(string filePath)
{
	var result = new List<string>();
	var lines = File.ReadAllLines(filePath);
	var columnNames = lines.First().Split(',').ToList();
	var indexOfSymbol = columnNames.IndexOf("Symbol");
	var i = 0;
	foreach (var line in lines.Skip(1))
	{
		var fields = line.Split(',').ToList();
		result.Add(fields[indexOfSymbol]);
	}
	
	return result;
}

	MyModel  GetFreshData(string symbol)
{
	var data = GetTipRanksData(symbol).Result;
	
	var lastPrice = data.Prices.Last().P;
	dc.Content =  "fetched fresh data for " + symbol + " - last price: " + lastPrice.ToString();
	double highTarget = 0.0, lowTarget = 0.0;
	int numberOfEstimates=data.Experts.Select(x=> x.Ratings.Where(r => r.PriceTarget != null)).Count();
	var target = data.PtConsensus.Where(pc => pc.High != null).FirstOrDefault();
	if (target != null)
	{
		highTarget = target.High.Value;
		lowTarget = data.PtConsensus.Where(pc => pc.Low != null).Average(pc => pc.Low.Value);
	}

	var experts = new List<MyExpert> { };
	if (data.Experts != null && data.Experts.Any())
	{
		foreach (Expert expert in data.Experts.Where(e => e.Ratings.Any(r => r.PriceTarget!=null)))
		{
			
			var myExpert = new MyExpert {
				AvgReturn=Math.Round(expert.StockAverageReturn, 2),
				SuccessRate = Math.Round(expert.StockSuccessRate, 2),
				Firm=expert.Firm,
				Name = expert.Name,
				GoodRecomendations=expert.StockGoodRecommendations,
				Stars = expert.Rankings!=null?expert.Rankings.Select(r => r.Stars).FirstOrDefault():0,
				TotalRecomendations = expert.StockTotalRecommendations,
				Ratings = new List<MyRating>()
			
			};
			
			foreach (var rating in expert.Ratings.OrderByDescending(x=> x.Date))
			{
				myExpert.Ratings.Add(new MyRating 
				{
					Date = rating.Date,
					Quote = rating.Quote,
					Target = rating.PriceTarget
				});
			}
			myExpert.TargetPrice = myExpert.Ratings.Select(r => r.Target).FirstOrDefault();
			myExpert.LastUpdated = myExpert.Ratings.Select(r => r.Date).FirstOrDefault();
			experts.Add(myExpert);
		}
		
	}
	
	return new MyModel { 
		Symbol= symbol, 
		Price= lastPrice, 
		EstHigh= highTarget, 
		EstLow = lowTarget,
		FollowerCount= data.FollowerCount, 
		EstCount= numberOfEstimates,
		EstAvg = (highTarget+lowTarget)/2,
		Experts = experts.OrderByDescending(x=> x.LastUpdated).ToList()
		};
}

async Task<Welcome> GetTipRanksData(string symbol)
{
	//sample api call result: https://www.tipranks.com/api/stocks/getData/?name=AAXN&benchmark=1&period=3&break=1521390728236
	var url = "https://www.tipranks.com/api/stocks/getData/?name=" + symbol + "&benchmark=1&period=3&break=1521390728236";


	using (var client = new System.Net.Http.HttpClient())
	{
//		var values = new Dictionary<string, string>
//			{
//				{ "text1", str1 },
//				{ "text2", str2 }
//			};
//		var content = new System.Net.Http.FormUrlEncodedContent(values);
		var response = client.GetAsync(url);
		var responseString = await response.Result.Content.ReadAsStringAsync();

		var data = Welcome.FromJson(responseString);
		return data;
	}

}

// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using QuickType;
//
//    var welcome = Welcome.FromJson(jsonString);

	[Serializable]
	public partial class Welcome
	{
		[JsonProperty("ticker")]
		public string Ticker { get; set; }

		[JsonProperty("companyName")]
		public string CompanyName { get; set; }

		[JsonProperty("stockUid")]
		public string StockUid { get; set; }

		[JsonProperty("sectorID")]
		public long SectorId { get; set; }

		[JsonProperty("market")]
		public string Market { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("hasEarnings")]
		public bool HasEarnings { get; set; }

		[JsonProperty("hasDividends")]
		public bool HasDividends { get; set; }

		[JsonProperty("prices")]
		public Price[] Prices { get; set; }

		[JsonProperty("consensuses")]
		public Consensus[] Consensuses { get; set; }

		[JsonProperty("experts")]
		public Expert[] Experts { get; set; }

		[JsonProperty("ptConsensus")]
		public PtConsensus[] PtConsensus { get; set; }

//		[JsonProperty("similarStocks")]
//		public SimilarStock[] SimilarStocks { get; set; }

		//[JsonProperty("insiderTrading")]
		public object InsiderTrading { get; set; }

		[JsonProperty("numOfAnalysts")]
		public long NumOfAnalysts { get; set; }

		[JsonProperty("numOfBloggers")]
		public long NumOfBloggers { get; set; }

		[JsonProperty("numOfExperts")]
		public long NumOfExperts { get; set; }

		[JsonProperty("marketCap")]
		public long MarketCap { get; set; }

		[JsonProperty("consensusOverTime")]
		public OnsensusOverTime[] ConsensusOverTime { get; set; }

		[JsonProperty("bestConsensusOverTime")]
		public OnsensusOverTime[] BestConsensusOverTime { get; set; }

		[JsonProperty("bpBloggers")]
		public BpBloggers BpBloggers { get; set; }

		[JsonProperty("bloggerArticleDistribution")]
		public BloggerArticleDistribution[] BloggerArticleDistribution { get; set; }

		[JsonProperty("bloggerSentiment")]
		public Entiment BloggerSentiment { get; set; }

		[JsonProperty("corporateInsiderTransactions")]
		public CorporateInsiderTransaction[] CorporateInsiderTransactions { get; set; }

		[JsonProperty("corporateInsiderActivity")]
		public CorporateInsiderActivity CorporateInsiderActivity { get; set; }

		[JsonProperty("insiders")]
		public WelcomeInsider[] Insiders { get; set; }

		[JsonProperty("insidrConfidenceSignal")]
		public OnfidenceSignal InsidrConfidenceSignal { get; set; }

		//[JsonProperty("notRankedExperts")]
		public object[] NotRankedExperts { get; set; }

		//[JsonProperty("notRankedConsensuses")]
		public object NotRankedConsensuses { get; set; }

		[JsonProperty("topStocksBySector")]
		public TopStocksBySector TopStocksBySector { get; set; }

		[JsonProperty("indexStockId")]
		public long IndexStockId { get; set; }

		[JsonProperty("numOfInsiders")]
		public long NumOfInsiders { get; set; }

		[JsonProperty("insiderslast3MonthsSum")]
		public double Insiderslast3MonthsSum { get; set; }

		[JsonProperty("hedgeFundData")]
		public HedgeFundData HedgeFundData { get; set; }

		[JsonProperty("stockId")]
		public long StockId { get; set; }

		[JsonProperty("followerCount")]
		public long FollowerCount { get; set; }
	}

	[Serializable]
	public partial class OnsensusOverTime
	{
		[JsonProperty("buy")]
		public long Buy { get; set; }

		[JsonProperty("hold")]
		public long Hold { get; set; }

		[JsonProperty("sell")]
		public long Sell { get; set; }

		[JsonProperty("date")]
		public System.DateTimeOffset Date { get; set; }

		[JsonProperty("consensus")]
		public long Consensus { get; set; }

		[JsonProperty("priceTarget")]
		public double? PriceTarget { get; set; }
	}

	[Serializable]
	public partial class BloggerArticleDistribution
	{
		[JsonProperty("site")]
		public string Site { get; set; }

		[JsonProperty("siteName")]
		public string SiteName { get; set; }

		[JsonProperty("percentage")]
		public string Percentage { get; set; }
	}

	[Serializable]
	public partial class Entiment
	{
		[JsonProperty("bearish")]
		public string Bearish { get; set; }

		[JsonProperty("bullish")]
		public string Bullish { get; set; }

		[JsonProperty("bullishCount")]
		public long BullishCount { get; set; }

		[JsonProperty("bearishCount")]
		public long BearishCount { get; set; }

		[JsonProperty("score")]
		public long Score { get; set; }

		[JsonProperty("avg")]
		public double Avg { get; set; }

		[JsonProperty("neutral")]
		public string Neutral { get; set; }

		[JsonProperty("neutralCount")]
		public long NeutralCount { get; set; }
	}

	[Serializable]
	public partial class BpBloggers
	{
		[JsonProperty("bullish")]
		public string[] Bullish { get; set; }

		[JsonProperty("bearish")]
		public string[] Bearish { get; set; }
	}

	[Serializable]
	public partial class Consensus
	{
		[JsonProperty("rating")]
		public long Rating { get; set; }

		[JsonProperty("nB")]
		public long NB { get; set; }

		[JsonProperty("nH")]
		public long NH { get; set; }

		[JsonProperty("nS")]
		public long NS { get; set; }

		[JsonProperty("period")]
		public long Period { get; set; }

		[JsonProperty("bench")]
		public long Bench { get; set; }

		[JsonProperty("mStars")]
		public long MStars { get; set; }

		[JsonProperty("d")]
		public string D { get; set; }

		[JsonProperty("isLatest")]
		public long IsLatest { get; set; }

		//[JsonProperty("priceTarget")]
		public object PriceTarget { get; set; }
	}

	[Serializable]
	public partial class CorporateInsiderActivity
	{
		[JsonProperty("informativeSum")]
		public long InformativeSum { get; set; }

		[JsonProperty("nonInformativeSum")]
		public long NonInformativeSum { get; set; }

		[JsonProperty("totalSum")]
		public long TotalSum { get; set; }

		[JsonProperty("informative")]
		public Nformative[] Informative { get; set; }

		[JsonProperty("nonInformative")]
		public Nformative[] NonInformative { get; set; }
	}

	[Serializable]
	public partial class Nformative
	{
		[JsonProperty("transactionTypeID")]
		public long TransactionTypeId { get; set; }

		[JsonProperty("count")]
		public long Count { get; set; }

		[JsonProperty("amount")]
		public double Amount { get; set; }
	}

	[Serializable]
	public partial class CorporateInsiderTransaction
	{
		//[JsonProperty("sharesBought")]
		public object SharesBought { get; set; }

		[JsonProperty("insidersBuyCount")]
		public long InsidersBuyCount { get; set; }

		//[JsonProperty("sharesSold")]
		public object SharesSold { get; set; }

		[JsonProperty("insidersSellCount")]
		public long InsidersSellCount { get; set; }

		[JsonProperty("month")]
		public long Month { get; set; }

		[JsonProperty("year")]
		public long Year { get; set; }

		[JsonProperty("transBuyCount")]
		public long TransBuyCount { get; set; }

		[JsonProperty("transSellCount")]
		public long TransSellCount { get; set; }

		[JsonProperty("transBuyAmount")]
		public double? TransBuyAmount { get; set; }

		[JsonProperty("transSellAmount")]
		public double TransSellAmount { get; set; }

		[JsonProperty("informativeBuyCount")]
		public long InformativeBuyCount { get; set; }

		[JsonProperty("informativeSellCount")]
		public long InformativeSellCount { get; set; }

		[JsonProperty("informativeBuyAmount")]
		public double InformativeBuyAmount { get; set; }

		[JsonProperty("informativeSellAmount")]
		public double InformativeSellAmount { get; set; }
	}

	[Serializable]
	public partial class Expert
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("firm")]
		public string Firm { get; set; }

		[JsonProperty("eUid")]
		public string EUid { get; set; }

		[JsonProperty("eTypeId")]
		public long ETypeId { get; set; }

		[JsonProperty("expertImg")]
		public string ExpertImg { get; set; }

		[JsonProperty("ratings")]
		public Rating[] Ratings { get; set; }

		[JsonProperty("stockSuccessRate")]
		public double StockSuccessRate { get; set; }

		[JsonProperty("stockAverageReturn")]
		public double StockAverageReturn { get; set; }

		[JsonProperty("stockTotalRecommendations")]
		public long StockTotalRecommendations { get; set; }

		[JsonProperty("stockGoodRecommendations")]
		public long StockGoodRecommendations { get; set; }

		[JsonProperty("rankings")]
		public Ranking[] Rankings { get; set; }

		[JsonProperty("stockid")]
		public long Stockid { get; set; }

		[JsonProperty("newPictureUrl")]
		public string NewPictureUrl { get; set; }

		[JsonProperty("quote")]
		public Quote Quote { get; set; }
}

	[Serializable]
	public partial class Ranking
	{
		[JsonProperty("period")]
		public long Period { get; set; }

		[JsonProperty("bench")]
		public long Bench { get; set; }

		[JsonProperty("lRank")]
		public long LRank { get; set; }

		[JsonProperty("gRank")]
		public long GRank { get; set; }

		[JsonProperty("gRecs")]
		public long GRecs { get; set; }

		[JsonProperty("tRecs")]
		public long TRecs { get; set; }

		[JsonProperty("avgReturn")]
		public double AvgReturn { get; set; }

		[JsonProperty("stars")]
		public double Stars { get; set; }

		[JsonProperty("tPos")]
		public double TPos { get; set; }
	}

	[Serializable]
	public partial class Rating
	{
		[JsonProperty("ratingId")]
		public long RatingId { get; set; }

		[JsonProperty("actionId")]
		public long ActionId { get; set; }

		[JsonProperty("date")]
		public System.DateTimeOffset Date { get; set; }

		[JsonProperty("d")]
		public string D { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("pos")]
		public long Pos { get; set; }

		[JsonProperty("priceTarget")]
		public double? PriceTarget { get; set; }

		[JsonProperty("time")]
		public System.DateTimeOffset Time { get; set; }

		[JsonProperty("quote")]
		public Quote Quote { get; set; }

		[JsonProperty("siteName")]
		public string SiteName { get; set; }

		[JsonProperty("site")]
		public string Site { get; set; }

		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("rD")]
		public System.DateTimeOffset RD { get; set; }

		[JsonProperty("timestamp")]
		public System.DateTimeOffset Timestamp { get; set; }
	}

	[Serializable]
	public partial class Quote
	{
		[JsonProperty("title")]
		public string Title { get; set; }

		[JsonProperty("date")]
		public System.DateTimeOffset Date { get; set; }

		[JsonProperty("quote")]
		public string QuoteQuote { get; set; }

		[JsonProperty("site")]
		public string Site { get; set; }

		[JsonProperty("link")]
		public string Link { get; set; }

		[JsonProperty("siteName")]
		public string SiteName { get; set; }
	}

	[Serializable]
	public partial class HedgeFundData
	{
		[JsonProperty("stockID")]
		public long StockId { get; set; }

		[JsonProperty("holdingsByTime")]
		public HoldingsByTime[] HoldingsByTime { get; set; }

		[JsonProperty("sentiment")]
		public double Sentiment { get; set; }

		[JsonProperty("trendAction")]
		public long TrendAction { get; set; }

		[JsonProperty("trendValue")]
		public double TrendValue { get; set; }

		[JsonProperty("institutionalHoldings")]
		public InstitutionalHolding[] InstitutionalHoldings { get; set; }
	}

	[Serializable]
	public partial class HoldingsByTime
	{
		[JsonProperty("date")]
		public System.DateTimeOffset Date { get; set; }

		[JsonProperty("holdingAmount")]
		public long HoldingAmount { get; set; }

		[JsonProperty("institutionHoldingPercentage")]
		public double InstitutionHoldingPercentage { get; set; }

		[JsonProperty("isComplete")]
		public bool IsComplete { get; set; }
	}

	[Serializable]
	public partial class InstitutionalHolding
	{
		[JsonProperty("institutionID")]
		public long InstitutionId { get; set; }

		[JsonProperty("managerName")]
		public string ManagerName { get; set; }

		[JsonProperty("institutionName")]
		public string InstitutionName { get; set; }

		[JsonProperty("action")]
		public long Action { get; set; }

		[JsonProperty("value")]
		public long Value { get; set; }

		[JsonProperty("expertUID")]
		public string ExpertUid { get; set; }

		[JsonProperty("change")]
		public double Change { get; set; }

		[JsonProperty("percentageOfPortfolio")]
		public double PercentageOfPortfolio { get; set; }

		[JsonProperty("rank")]
		public long Rank { get; set; }

		[JsonProperty("totalRankedInstitutions")]
		public long TotalRankedInstitutions { get; set; }

		[JsonProperty("imageURL")]
		public string ImageUrl { get; set; }

		[JsonProperty("isActive")]
		public bool IsActive { get; set; }
	}

	[Serializable]
	public partial class WelcomeInsider
	{
		[JsonProperty("uId")]
		public string UId { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("company")]
		public Company Company { get; set; }

		[JsonProperty("isOfficer")]
		public bool IsOfficer { get; set; }

		[JsonProperty("isDirector")]
		public bool IsDirector { get; set; }

		[JsonProperty("isTenPercentOwner")]
		public bool IsTenPercentOwner { get; set; }

		[JsonProperty("isOther")]
		public bool IsOther { get; set; }

		[JsonProperty("officerTitle")]
		public OfficerTitle OfficerTitle { get; set; }

		[JsonProperty("otherText")]
		public OtherText OtherText { get; set; }

		[JsonProperty("transTypeId")]
		public long TransTypeId { get; set; }

		[JsonProperty("action")]
		public long Action { get; set; }

		[JsonProperty("date")]
		public string Date { get; set; }

		[JsonProperty("amount")]
		public double? Amount { get; set; }

		[JsonProperty("rank")]
		public long Rank { get; set; }

		[JsonProperty("stars")]
		public double Stars { get; set; }

		[JsonProperty("expertImg")]
		public string ExpertImg { get; set; }

		[JsonProperty("rDate")]
		public System.DateTimeOffset RDate { get; set; }

		[JsonProperty("newPictureUrl")]
		public string NewPictureUrl { get; set; }

		[JsonProperty("link")]
		public string Link { get; set; }
	}

	[Serializable]
	public partial class OnfidenceSignal
	{
		[JsonProperty("stockScore")]
		public double StockScore { get; set; }

		[JsonProperty("sectorScore")]
		public double SectorScore { get; set; }

		[JsonProperty("score")]
		public long Score { get; set; }
	}

	[Serializable]
	public partial class Price
	{
		[JsonProperty("date")]
		public System.DateTimeOffset Date { get; set; }

		[JsonProperty("d")]
		public string D { get; set; }

		[JsonProperty("p")]
		public double P { get; set; }
	}

	[Serializable]
	public partial class PtConsensus
	{
		[JsonProperty("period")]
		public long Period { get; set; }

		[JsonProperty("bench")]
		public long Bench { get; set; }

		[JsonProperty("priceTarget")]
		public double? PriceTarget { get; set; }

		[JsonProperty("high")]
		public double? High { get; set; }

		[JsonProperty("low")]
		public double? Low { get; set; }
	}

	[Serializable]
	public partial class SimilarStock
	{
		[JsonProperty("uid")]
		public string Uid { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("ticker")]
		public string Ticker { get; set; }

		[JsonProperty("mktCap")]
		public string MktCap { get; set; }

		[JsonProperty("sectorId")]
		public long SectorId { get; set; }

		[JsonProperty("consensusData")]
		public ConsensusDatum[] ConsensusData { get; set; }
	}
	[Serializable]
	public partial class ConsensusDatum
	{
		[JsonProperty("nTotal")]
		public long NTotal { get; set; }

		[JsonProperty("nB")]
		public long NB { get; set; }

		[JsonProperty("nH")]
		public long NH { get; set; }

		[JsonProperty("nS")]
		public long NS { get; set; }

		[JsonProperty("period")]
		public long Period { get; set; }

		[JsonProperty("benchmark")]
		public long Benchmark { get; set; }

		[JsonProperty("wCon")]
		public long WCon { get; set; }

		[JsonProperty("priceTarget")]
		public double? PriceTarget { get; set; }
	}

	[Serializable]
	public partial class TopStocksBySector
	{
		[JsonProperty("analysts")]
		public Analyst[] Analysts { get; set; }

		[JsonProperty("bloggers")]
		public Blogger[] Bloggers { get; set; }

		[JsonProperty("insiders")]
		public TopStocksBySectorInsider[] Insiders { get; set; }
	}

	[Serializable]
	public partial class Analyst
	{
		[JsonProperty("ticker")]
		public string Ticker { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("consensusData")]
		public Consensus ConsensusData { get; set; }
	}

	[Serializable]
	public partial class Blogger
	{
		[JsonProperty("ticker")]
		public string Ticker { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("sentiment")]
		public Entiment Sentiment { get; set; }
	}

	[Serializable]
	public partial class TopStocksBySectorInsider
	{
		[JsonProperty("ticker")]
		public string Ticker { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("confidenceSignal")]
		public OnfidenceSignal ConfidenceSignal { get; set; }
	}

	public enum Company { MicrosoftCorp };

	public enum OfficerTitle { Cao, Ceo, Empty, Evp, EvpBd, EvpCfo, EvpCmo, EvpHr, President };

	public enum OtherText { Empty, SeeRemarks };

	
	public partial class Welcome
	{
		public static Welcome FromJson(string json) => JsonConvert.DeserializeObject<Welcome>(json, Converter.Settings);
	}

	
	[Serializable]
	static class OfficerTitleExtensions
	{
		public static OfficerTitle? ValueForString(string str)
		{
			switch (str)
			{
				case "CAO": return OfficerTitle.Cao;
				case "CEO": return OfficerTitle.Ceo;
				case "": return OfficerTitle.Empty;
				case "EVP": return OfficerTitle.Evp;
				case "EVP BD": return OfficerTitle.EvpBd;
				case "EVP & CFO": return OfficerTitle.EvpCfo;
				case "EVP, CMO": return OfficerTitle.EvpCmo;
				case "EVP HR": return OfficerTitle.EvpHr;
				case "President": return OfficerTitle.President;
				default: return null;
			}
		}

		public static OfficerTitle ReadJson(JsonReader reader, JsonSerializer serializer)
		{
			var str = serializer.Deserialize<string>(reader);
			var maybeValue = ValueForString(str);
			if (maybeValue.HasValue) return maybeValue.Value;
			throw new Exception("Unknown enum case " + str);
		}

		public static void WriteJson(this OfficerTitle value, JsonWriter writer, JsonSerializer serializer)
		{
			switch (value)
			{
				case OfficerTitle.Cao: serializer.Serialize(writer, "CAO"); break;
				case OfficerTitle.Ceo: serializer.Serialize(writer, "CEO"); break;
				case OfficerTitle.Empty: serializer.Serialize(writer, ""); break;
				case OfficerTitle.Evp: serializer.Serialize(writer, "EVP"); break;
				case OfficerTitle.EvpBd: serializer.Serialize(writer, "EVP BD"); break;
				case OfficerTitle.EvpCfo: serializer.Serialize(writer, "EVP & CFO"); break;
				case OfficerTitle.EvpCmo: serializer.Serialize(writer, "EVP, CMO"); break;
				case OfficerTitle.EvpHr: serializer.Serialize(writer, "EVP HR"); break;
				case OfficerTitle.President: serializer.Serialize(writer, "President"); break;
			}
		}
	}

	[Serializable]
	static class OtherTextExtensions
	{
		public static OtherText? ValueForString(string str)
		{
			switch (str)
			{
				case "": return OtherText.Empty;
				case "See Remarks": return OtherText.SeeRemarks;
				default: return null;
			}
		}

		public static OtherText ReadJson(JsonReader reader, JsonSerializer serializer)
		{
			var str = serializer.Deserialize<string>(reader);
			var maybeValue = ValueForString(str);
			if (maybeValue.HasValue) return maybeValue.Value;
			throw new Exception("Unknown enum case " + str);
		}

		public static void WriteJson(this OtherText value, JsonWriter writer, JsonSerializer serializer)
		{
			switch (value)
			{
				case OtherText.Empty: serializer.Serialize(writer, ""); break;
				case OtherText.SeeRemarks: serializer.Serialize(writer, "See Remarks"); break;
			}
		}
	}

	public static class Serialize
	{
		public static string ToJson(this Welcome self) => JsonConvert.SerializeObject(self, Converter.Settings);
	}

	internal class Converter : JsonConverter
	{
		public override bool CanConvert(Type t) => t == typeof(Company) || t == typeof(OfficerTitle) || t == typeof(OtherText) || t == typeof(Company?) || t == typeof(OfficerTitle?) || t == typeof(OtherText?);

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			//throw new NotImplementedException();
		}
		
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			//throw new NotImplementedException();
			return existingValue;
		}

	public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
			DateParseHandling = DateParseHandling.None,
			Converters = {
				new Converter(),
				new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
			},
		};
	}