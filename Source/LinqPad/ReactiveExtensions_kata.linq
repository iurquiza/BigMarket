<Query Kind="Program">
  <NuGetReference>mongocsharpdriver</NuGetReference>
  <NuGetReference>DotNetEx.Reactive</NuGetReference>
  <NuGetReference>Rx.Contrib</NuGetReference>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
</Query>

IObservable<int> _source;
bool _done;
class DateAndPrice
{
	public DateTime Date { get; set; }
	public Decimal Price { get; set; }
}
void Main()
{
	//SampleRxObservable();
	//SampleRxPolling();
		
	//SampleStockPriceTicking();
	SampleStockPriceHistoricalScheduler();
	//TestObservableInterval(); //each tick in the interval is projected onto an item on the return stack
	//System.Threading.Thread.Sleep(2000);
}

 private static void WriteThis(double i)
    {
        Console.WriteLine("OnNext - {0}", i);
    }

void TestObservableInterval()
	{
//	var clock = from _ in Observable.Interval(TimeSpan.FromSeconds(1))
//            select DateTime.Now;

	var clock = Observable.Interval(TimeSpan.FromSeconds(1))
		.Select((x)=> {return new DateAndPrice(){Date= DateTime.Now, Price=x };})
		//.Select(x=> GetSampleStockPrice(3, 10, 50))
		;
clock.Subscribe(datePrice =>
{
    Console.WriteLine("It's now {0} o'clock. Price: {1}", datePrice.Date, datePrice.Price);
});
	}

void SampleStockPriceTicking(){
//	foreach (var element in GetSampleStockPrice(1, 5, 10))
//	{
//		string.Format("{0}: {1}", element.Date, element.Price).Dump();
//	}

	//var scheduler = Scheduler.AsPeriodic();
	//scheduler.SchedulePeriodic(()=>{}, TimeSpan.FromSeconds(1), (_, state)=> 
	var observableCollection = GetSampleStockPrice(20).ToObservable()
		.TimeInterval();
	List<DateAndPrice> previousPrices = new List<DateAndPrice>();
	var dc1 = new DumpContainer().Dump();
	var dc2 = new DumpContainer().Dump("Historic Prices");
	IObserver<TimeInterval<DateAndPrice>> handler=Observer.Create(	
			(TimeInterval<DateAndPrice> onNext) => 
			{
				
				dc1.Content = string.Concat("Got ", onNext.Value.Date.ToShortDateString(), ": ", onNext.Value.Price
					, ". Interval Between Signals: ", onNext.Interval.TotalMilliseconds);
				dc2.Content = previousPrices;
				dc2.Refresh();
				previousPrices.Add(onNext.Value);
			},
			(Exception ex)=> {string.Concat("Exception: ",  ex.Message).Dump();_done=true;},
			() => {"No more messages.".Dump(); _done=true;});
	observableCollection.Subscribe(handler);
	
}

void SampleStockPriceHistoricalScheduler()
{
	
	//from http://stackoverflow.com/questions/22255205/observable-from-list-of-timetamps
	
	//http://blog.niallconnaughton.com/2015/05/09/time-travel-with-reactive-extensions/  
		//this one is too old
	  var now = DateTime.Now;

    var events = new List<Tuple<DateTime, string, string>> {
        Tuple.Create(now.AddSeconds(1), "A", "Third A"),
        Tuple.Create(now.AddSeconds(2), "B", "Third B"),
        Tuple.Create(now.AddSeconds(3), "C", "Third C")
    };

    var eventSource = Observable.Generate(
        //(IEnumerator<Tuple<DateTime,string, string>>)events.GetEnumerator(),
		GetASampleStockPrice(20),
        s => s.MoveNext(), 
        s => s, //result sectorWx
        s => s.Current, // the data
        s => TimeSpan.FromMilliseconds(500) // the timing, could be an absolute time or a timespan
		); 

    eventSource.Subscribe((data)=> {Console.WriteLine("{0}@{1} ", data.Date.ToShortDateString(), data.Price);
		if (data.Price==20)
			Console.WriteLine("************");
	});              
}

IEnumerator<DateAndPrice> GetASampleStockPrice(int count){
	var pricesInumerator = GetSampleStockPrice(count).GetEnumerator();
	return pricesInumerator;
}

IEnumerable<DateAndPrice> GetSampleStockPrice(int maxSignalCount){

	var i=0;
	var startDate = DateTime.Now.AddDays(-100);
	while(i<maxSignalCount)
	{
		yield return new DateAndPrice(){Date=startDate.AddDays(i), Price=i+1 };
		//System.Threading.Thread.Sleep(1000/pingsPerSec);
		i++;
	}

}

void SampleUseOfSubjectToFireEventsAtWill(){
	_source = new List<int>{1,2,3}.ToObservable();
	IObserver<int> handler=Observer.Create(	
			(int onNext) => { string.Concat("Got ", onNext).Dump();},
			(Exception ex)=> {string.Concat("Exception: ",  ex.Message).Dump();_done=true;},
			() => {"No more messages.".Dump(); _done=true;});
	
	
	var subject = new Subject<double>();
    var collection = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    var observableCollection = collection
        .ToObservable()
        .Concat(subject); //at the end of the original collection, add the subject
    observableCollection.Subscribe(WriteThis);

    //now I want to add 100, 101, 102 which should reach my observers
    
	System.Threading.Thread.Sleep(500);
	subject.OnNext(100);
    System.Threading.Thread.Sleep(500);
	subject.OnNext(101);
	System.Threading.Thread.Sleep(500);
    subject.OnNext(102);

	using (IDisposable subscription = _source.Subscribe(handler)){
		while(!_done)
			System.Threading.Thread.Sleep(10000);
	}
	
	"All done".Dump();

}
void SampleRxPolling(){

	//This is going to generate a series of observable values every 100 milliseconds
	IObservable<int> source = Observable.Generate(0, i=> i<10, i=> i+1, i=>GetValueSquared(i), i=> TimeSpan.FromMilliseconds(100))
		.Retry(4);
	
	bool done = false;
	IObserver<int> handler=Observer.Create(	
			(int onNext) => { string.Concat("Got ", onNext).Dump();},
			(Exception ex)=> {string.Concat("Exception: ",  ex.Message).Dump();done=true;},
			() => {"No more messages.".Dump(); done=true;});
			
	using (IDisposable subscription = source.Subscribe(handler)){
		while(!done)
			System.Threading.Thread.Sleep(500);
	}
	
	"All done".Dump();
}

int numberofTimesErrorThrown;
int GetValueSquared(int i){

	if (i==2)
	{
		if (numberofTimesErrorThrown<2)
		{
			numberofTimesErrorThrown++;
			throw new Exception(string.Concat("I don't like the number ", i) );
		}
	}
	
	if (i==3)
	{
		if (numberofTimesErrorThrown<3)
		{
			numberofTimesErrorThrown++;
			throw new Exception(string.Concat("I don't like the number ", i) );
		}
	}
	return i;
}

void SampleRxObservable(){

	IDisposable subscription=null;
//	IObservable<int> source =Observable.Generate(0, i=> i<5, i=> i+1, 
//		i=> i*i, 
//		i=> TimeSpan.FromMilliseconds(i)).Distinct();
	IObservable<int> source = GetSampleItems().ToObservable();

	IObserver<int> handler=Observer.Create(	
			(int onNext) => { string.Concat("Got ", onNext).Dump();},
			(Exception ex)=> {ex.Message.Dump("Exception!!");},
			() => {"No more messages.".Dump(); }
		);
	
	using (subscription = source.Subscribe(handler)){
		System.Threading.Thread.Sleep(4000);
	}
	
//	subscription.Dispose();
}

// Define other methods and classes here
IEnumerable<int> GetSampleItems()
{
	var accumulator=0;
	while(accumulator<10)
	{
		 yield return accumulator;
		 System.Threading.Thread.Sleep(100);
		accumulator++;
	}

}