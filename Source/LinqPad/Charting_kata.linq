<Query Kind="Program">
  <Reference>&lt;ProgramFilesX86&gt;\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.4.0.0\FSharp.Core.dll</Reference>
  <NuGetReference>Angara.Chart</NuGetReference>
  <Namespace>Angara.Charting</Namespace>
  <Namespace>Microsoft.FSharp.Core</Namespace>
  <Namespace>Microsoft.FSharp.Collections</Namespace>
</Query>

void Main()
{
	var x = Enumerable.Range(1, 10).Select(e => (double)e);
	var y = x.Select(t => t * t);
	x.Zip(y, (tx, ty) => new {x=tx, y=ty, delta=ty-tx}).Dump();

	TryChartingWithForms();
			
}

void TryChartingWithForms() {
	Util.Image("https://i.stack.imgur.com/a54bB.png").Dump();
}
void TryChartingWithANgara(IEnumerable<double> x, IEnumerable<double> y)
{
	//https://github.com/predictionmachines/Angara.Chart
	//this requires javascript to plot stuff
	var fsharpList = new FSharpList<PlotInfo>(Plot.line(
			LineX.NewValues(x.ToArray()),
			LineY.NewValues(y.ToArray()),
			FSharpOption<string>.Some("Yellow"), FSharpOption<Double>.Some(1),
			FSharpOption<LineTreatAs>.Some(LineTreatAs.Trajectory),
			null, null,
			FSharpOption<string>.Some("Disiplay name"),
			FSharpOption<LineTitles>.Some(new LineTitles(FSharpOption<string>.Some("x"), FSharpOption<string>.Some("y")))
			), FSharpList<PlotInfo>.Empty);

	Chart.ofList(
		fsharpList
		).Dump();

}