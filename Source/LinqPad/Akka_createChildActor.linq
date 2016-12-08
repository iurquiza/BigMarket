<Query Kind="Program">
  <GACReference>Microsoft.VisualStudio.Utilities, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a</GACReference>
  <NuGetReference>Akka</NuGetReference>
  <NuGetReference Prerelease="true">Akka.Serialization.Wire</NuGetReference>
  <NuGetReference>mongocsharpdriver</NuGetReference>
  <NuGetReference>Wire</NuGetReference>
  <Namespace>Akka</Namespace>
  <Namespace>Akka.Actor</Namespace>
  <Namespace>Akka.Serialization</Namespace>
  <Namespace>Microsoft.VisualStudio.Utilities</Namespace>
  <Namespace>Wire</Namespace>
  <Namespace>Wire.ValueSerializers</Namespace>
  <AppConfig OverrideConnection="true">
    <Content>
      <configuration>
        <configSections>
          <section name="akka" type="Akka.Configuration.Hocon.AkkaConfigurationSection, Akka" />
        </configSections>
        <akka>
          <hocon><![CDATA[
	akka {
	#stdout-loglevel = DEBUG
	loglevel = DEBUG
	#log-config-on-start = on
	actor {
		serializers {
		  wire = "Akka.Serialization.WireSerializer, Akka.Serialization.Wire"
		}
		serialization-bindings {
		  "System.Object" = wire
		}
		debug {
		    #receive = on # log any received message
		    #autoreceive= on # log automatically received messages, e.g. PoisonPill
		    #lifecycle = on # log actor lifecycle changes
		    #event-stream = on # log subscription changes for Akka.NET event stream
		    unhandled = on # log unhandled messages sent to actors
		  }
	  }
	}
	]]></hocon>
        </akka>
      </configuration>
    </Content>
  </AppConfig>
</Query>

private static int counter;
public static DumpContainer dc;
void Main()
{
	counter=0;
	dc = new DumpContainer().Dump();
	var system = ActorSystem.Create("my-system");
	
	//system.Serialization.AddSerializer( Wire.Serializer());
	var myActor= system.ActorOf<MyActor>("root"); 
	//myActor.Tell(new {Name="Dead Letter", Game="Football"});
	//myActor.Tell(new MyRequest{Name="Musa", Game="Nabi"});
	
	var myActorWithRetries = system.ActorOf<MyActorCoordinator>("actorWithRetries");
//	var greeterA = greeter.ActorOf<MyActor>("greeterA"); 
//	greeterA.Tell(new MyRequest{Name="Tom", Game="Cat chasing"});
	//myActor.Tell(new MathRequest {Num1=5, Num2=6, Operation="Add"});
	for(var i=0; i<2; i++)
	{
		//System.Threading.Thread.Sleep(500);
		var mreq = new MathRequest(4, i, "Divide");
		//myActor.Tell(mreq);
		//MathOperations.DoMathOperation(mreq);	
		myActorWithRetries.Tell(mreq);
	}		
}
static class MathOperations
{

	public static double DoMathOperation(MathRequest mreq){
		double result=0;
		var statusMsg = string.Concat(++counter, ") ", mreq.Operation.Replace("Divide", "Divid"), "ing ", mreq.Num1, " by ", mreq.Num2).Dump();
		dc.Content= statusMsg;
		switch (mreq.Operation)
		{
			case "Divide":
				result = mreq.Num1 / mreq.Num2;
				string.Format("{0}", result).Dump();
				if (mreq.Num2 == 0)
				{
					throw new Exception("Can't divide by zero buddy!");
				}
				break;
			default:
				("unhandled operation: " + mreq.Operation).Dump();
				break;
		}
		return result;
	}

}
class MyActorCoordinator : UntypedActor
{
	protected override void OnReceive(object msg) {
		if (msg is MathRequest)
		{
			var myMathRequest = (MathRequest)msg ;
			var mathActor = Context.ActorOf<MyActor>();
			mathActor.Tell(myMathRequest);
		}
	}

	protected override SupervisorStrategy SupervisorStrategy()
	{
		return new OneForOneStrategy(
			1, // maxNumberOfRetries
			TimeSpan.FromSeconds(30), // withinTimeRange
			x => // localOnlyDecider
			{
			//Maybe we consider ArithmeticException to not be application critical
			//so we just ignore the error and keep going.
			//if (x is ArithmeticException) return Directive.Resume;

			//Error that we cannot recover from, stop the failing actor
			if (x is NotSupportedException) return Directive.Stop;

			//In all other cases, just restart the failing actor
			else
			{
				string.Concat("Restart the failed operation: ", x.Message).Dump();
				return Directive.Restart;
			}
		});
	}
}
class MyActor :ReceiveActor {

	//when the parent Supervisor strategy tells the child to restart, this message sends the original message back to actor so it can retry it
	//this should be automatic in my opinion
	protected override void PreRestart(Exception reason, object message)
	{
		// put message back in mailbox for re-processing after restart
		var req = message as MathRequest;
		if (req != null)
		{
			string.Concat("PreRestart ", req.Operation, " " , req.Num1, " by ", req.Num2).Dump();
        	System.Threading.Thread.Sleep(500);
			Self.Tell(message);
		}
	}
	public MyActor()
	{
		//Util.ClearResults();
		Receive<MyRequest>(myReq => 
		{
			this.Self.Path.Name.Dump("Receive<MyRequest>");
			string.Concat(myReq.Name, " Game: ", myReq.Game).Dump();
			var prettyActor = Context.ActorOf<MyActor>("prettyActor");
			prettyActor.Tell(new PrettyPrintRequest{MyObj=myReq});
		});
		
		Receive<PrettyPrintRequest>(x=> 
		{
			string.Format("{0}/{1}", Self.Path.Parent, Self.Path.Name).Dump("Receive<PrettyPrintRequest> - child actor example ");
			string.Concat(" -- Received pretty print request").Dump();
		});
		
		Receive<MathRequest>(mreq=> 
		{ 
			string.Format("Receive<MathRequest>: {0}", Self.Path.Name).Dump();
			MathOperations.DoMathOperation(mreq);
		});
	}
}

class PrettyPrintRequest
{
	public MyRequest MyObj { get; set; }
}

class MyRequest
{
	public string Name { get; set; }
	public string Game { get; set; }
}

class MathRequest
{
	public int Num1 { get; private set; }
	public int Num2 { get; private set; }
	public string Operation { get; private set; }
	public MathRequest(int n1, int n2, string operation)
	{
		Num1 = n1;
		Num2 = n2;
		Operation = operation;
	}
}