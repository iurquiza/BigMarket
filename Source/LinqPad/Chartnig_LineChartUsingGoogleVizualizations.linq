<Query Kind="Program" />

void Main()
{
	Util.RawHtml(@"
	
	<style>#chart_div {min-height:300px}
	table.headingpresenter {border-left: 0px;}
	</style>
  <script type=""text/javascript"" src=""https://www.gstatic.com/charts/loader.js""></script>
  <div id=""chart_div"" ></div>
  
  <script>
  google.charts.load('current', {packages: ['corechart', 'line']});
  google.charts.setOnLoadCallback(drawBasic);

	
function drawChart(){
      var chart = new google.visualization.LineChart(document.getElementById('chart_div'));
      chart.draw(data, options);
}
function drawBasic() {

  var options = {
        hAxis: {
          title: 'Time'
        },
        vAxis: {
          title: 'Popularity'
        }
      };

      var data = new google.visualization.DataTable();
      data.addColumn('number', 'X');
      data.addColumn('number', 'Dogs');
	  data.addColumn('number', 'Cats');

      data.addRows([
        [0, 5, 10],   [1, 10, 20],  [2, 23, 15],  [3, 17, 17],  [4, 18, 10],  [5, 9, 5]
      ]);

	drawChart();

	  
//	  setTimeout(function(){ 
//	  	data.addRows[6,10,6];
//	  	drawChart();}, 1000);
	  
    }
  </script>
	")
		.Dump();
		
	"https://developers.google.com/chart/interactive/docs/gallery/linechart".Dump("from");
	"https://jsfiddle.net/api/post/library/pure/".Dump("jsfiddle at");
	"http://jsbin.com/xefaxe/edit?html".Dump("Example of chart streaming");

	
}

// Define other methods and classes here