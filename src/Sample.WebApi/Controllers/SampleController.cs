using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DijitalMunky.Grafana.SimpleJson.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DijitalMunky.Grafana.SimpleJson
{
    [ApiController]
    [Route("[controller]")]
    public class SampleController : BaseSimpleJsonController
    {
      public SampleController(ILogger<SampleController> logger) : base(supportsTables: false, providesAnnotations: false, providesTags: false, logger)
      {
      }

      protected override Task<IEnumerable<TimeSeriesData>> GetTimeSeriesData(string metric                     // the name of the metric sought
                                                                            , TimeSpan interval                // how far apart are the data values? e.g. every 30s
                                                                            , TimeRange timeRange              // the beginning and ending times for the data to retrieve
                                                                            , int? maxDataPoints               // the maximum number of datapoints to retrieve, if null or 0, then return 'em all!
                                                                            , IEnumerable<Filter> adhocFilters // any additional filters to apply
                                                                            )
      {
        return Task.Run(() => {
          var retVal = new List<TimeSeriesData>((int)(timeRange.AsTimeSpan() / interval));

          for(var count = 0; count < retVal.Capacity; count++) {
            retVal.Add(new TimeSeriesData { Time = timeRange.From + (interval * count), Value = count });
          }

          return retVal.AsEnumerable();
        });
      }
    }
}
