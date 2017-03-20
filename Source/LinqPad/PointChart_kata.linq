<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Drawing.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.DataVisualization.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>System.Windows.Forms.DataVisualization.Charting</Namespace>
</Query>

async void Main()
{
	var chart = new PointChart("my points");
	int n = 1;
	var sequence = Enumerable.Range (0, 21 ).Select (i => new PieSlice { Text = i.ToString(), Value =  (i + n) % 10 });
	chart.SetData(sequence);
	
	for(int i=0;i<100;i++)
	{
		chart.SetData (sequence);
		n++;
		await Task.Delay (100);
	}
}

class PointChart
{
	Chart chart = new Chart();
	Series series = new Series { ChartType = SeriesChartType.Point};
	
	public PointChart (string  name)
	{
		series.CustomProperties = "PieDrawingStyle=Concave";
		chart.Series.Add (series);
		chart.ChartAreas.Add (new ChartArea());
		chart.Dump (name);
	}
	
	public void SetData (IEnumerable<PieSlice> slices)
	{
		series.Points.SuspendUpdates();
		series.Points.Clear();
		foreach (var slice in slices)
		{
			series.Points.AddXY(slice.Text, slice.Value);
			series.Points.AddXY(slice.Text, slice.Value*-1);
		}
		series.Points.ResumeUpdates();
	}
}

class PieSlice
{
	public string Text;
	public double Value;
}