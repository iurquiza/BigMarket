<Query Kind="Program">
  <NuGetReference>Akka</NuGetReference>
  <NuGetReference>mongocsharpdriver</NuGetReference>
  <NuGetReference>Wire</NuGetReference>
  <Namespace>Akka.Actor</Namespace>
  <Namespace>Wire</Namespace>
</Query>

void Main()
{
	//shared to http://share.linqpad.net/jsb8ji.linq
	var system = ActorSystem.Create("greeting-system");
	
	var greeter = system.ActorOf<GreeterActor>("greeter"); //ActorRef 
	Util.ClearResults();
	
	for(int i=0;i<10;i++)
	{
		var result = greeter.Ask<GreetResponse>(new GreetRequest("Hello", "world " + i.ToString(), "happy"));
		string.Format("{0} with a {1}", result.Result.Message, result.Result.FacialExpressione).Dump();
	}
}

public class GreeterActor : ReceiveActor
{
    public GreeterActor()
    {
		
		//when a GreetRequest is the incoming request and the desired output is a GreetResponse
		Receive<GreetRequest>(greet => Sender.Tell(MakeGreeting(greet)));
		
    }
	
	private GreetResponse MakeGreeting(GreetRequest g)
	{
		var rand = new System.Random();
		
		var waitTime = rand.Next(1000);
		if (waitTime==0) 
			waitTime = 1;
		Console.Write(waitTime.ToString() + " ");
		System.Threading.Thread.Sleep(waitTime);
		
		return new GreetResponse(string.Format("{0} {1}. ", g.Greeting, g.Subject),  g.Mood + " smile");
	}
	
}

#region
class GreetRequest
{
	
	private readonly  string _Subject; public string Subject{get { return _Subject; }}
	private readonly  string _Greeting; public string Greeting{get { return _Greeting; }}
	private readonly  string _Mood; public string Mood{get { return _Mood; }}
	public GreetRequest(string greeting, string subject, string mood)
	{
		_Subject = subject;
		_Greeting = greeting;
		_Mood = mood;
	}
}

class GreetResponse
{
	private readonly  string _message;
	public string Message{get { return _message; }}
	
	private readonly  string _facialExpression;
	public string FacialExpressione{get { return _facialExpression; }}
	
	public GreetResponse (string message, string facialExp)
	{
		_message = message;
		_facialExpression = facialExp;
	}
}

#endregion

/*steps
1. found tutorial at http://blog.canberger.se/2014/10/getting-started-with-akkanet.html
2. install package: akka
3. follow instructions
4. install Wire serializer
*/

//todo: do akka tutorial on official site: http://getakka.net/