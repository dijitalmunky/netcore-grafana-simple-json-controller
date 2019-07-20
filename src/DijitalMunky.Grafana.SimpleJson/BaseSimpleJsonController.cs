using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

using DijitalMunky.Grafana.SimpleJson.Models;
using System.Collections.Generic;

namespace DijitalMunky.Grafana.SimpleJson
{
  [ApiController]
  public abstract class BaseSimpleJsonController : ControllerBase, IActionFilter
  {
    private readonly bool _supportsTables;
    private readonly bool _providesAnnotations;
    private readonly bool _providesTags;
    private readonly ILogger<BaseSimpleJsonController> _logger;

    public BaseSimpleJsonController(bool supportsTables = false, bool providesAnnotations = false, bool providesTags = false, ILogger<BaseSimpleJsonController> logger = null) {
      _supportsTables = supportsTables;
      _providesAnnotations = providesAnnotations;
      _providesTags = providesTags;
      _logger = logger;
    }

    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context)
    {
      _logger?.LogDebug("Action completed with status code: {statusCode}", context.HttpContext.Response.StatusCode);
    }

    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
      _logger?.LogInformation("Checking Model for action {action}...", context.ActionDescriptor);
      if (!ModelState.IsValid) {
        _logger?.LogWarning("Model State is not valid, returning a bad request.  Issues were: {ModelState}", ModelState);
        context.Result = BadRequest(ModelState);
      }

      _logger?.LogInformation("Executing {action}...", context.ActionDescriptor);
    }

    [HttpGet("")]
    public async Task<IActionResult> Index() {

      _logger?.LogInformation("Checking Datasource...");
      if (!await CheckDatasource()) {
        _logger?.LogError("Datasource check failed.");
        return StatusCode(503);  // Service Unavailable
      }

      _logger?.LogInformation("Datasource check succeeded.");
      return Ok();
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] QueryRequest request) {

      // loop through the targets and call the implementation methid for each one
      if (request == null) {
        return BadRequest("A query must be supplied.");
      }

      var retVal = new List<IQueryResponse>();
      foreach (var target in request.Targets) {
        _logger?.LogDebug("Processing target {target}, with type {type} and refid {refid}.", target.Target, target.Type, target.RefId);

        switch (target.Type) {
          case EMetricType.Table:
            throw new NotImplementedException("Table targets are not implemented yet.");
          case EMetricType.Timeseries:
            retVal.Add(await ProcessTimeSeriesData(target.Target, new TimeSpan(0, 0, 0, 0, request.IntervalMs), request.Range, request.MaxDataPoints, request.AdhocFilters));
            break;
          default:
            throw new ArgumentException("Unimplemented target type specified.");
        }
      };

      return Ok(retVal);
    }

    [HttpPost("annotations")]
    public Task<IActionResult> Annotations([FromBody] AnnotationRequest request) {
      throw new NotImplementedException();
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request) {
      var metrics = await GetMetrics(request?.Target);

      if (metrics == null) {
        throw new InvalidProgramException("Please ensure that you do not return null from the GetMetrics method.");
      }

      if (metrics.Any() && metrics.ElementAt(0).Value == null) {
        return Ok(metrics.Select(e => e.Value));
      }

      return Ok(metrics);
    }

    [HttpPost("tag-keys")]
    public async Task<IActionResult> TagKeys() {
      if (!_providesTags) {
        return NotFound();
      }

      throw new NotImplementedException();
    }

    [HttpPost("tag-values")]
    public async Task<IActionResult> TagValues([FromBody] TagValuesRequest request) {
      if (!_providesTags) {
        return NotFound();
      }

      throw new NotImplementedException();
    }

    private async Task<TimeSeriesResponse> ProcessTimeSeriesData(string target, TimeSpan interval, TimeRange range, int? maxDataPoints, IEnumerable<Filter> adhocFilters)
    {
      var data = await GetTimeSeriesData(target, interval, range, maxDataPoints, adhocFilters);

      if (data == null) data = new List<TimeSeriesData>();

      return new TimeSeriesResponse {
        Target = target,
        DataPoints = data.Select(datapoint => new double[] { datapoint.Value, datapoint.Time.ToUnixTimeMilliseconds() }),
      };
    }


    /// Implement this method to retrieve time series data.
    /// When a request for data comes in, this method is invoked
    /// for each metric that is requested.  For example, give the following request:
    ///   {
    ///     "panelId": 1,
    ///     "range": {
    ///       "from": "2016-10-31T06:33:44.866Z",
    ///       "to": "2016-10-31T12:33:44.866Z",
    ///       "raw": {
    ///         "from": "now-6h",
    ///         "to": "now"
    ///       }
    ///     },
    ///     "rangeRaw": {
    ///       "from": "now-6h",
    ///       "to": "now"
    ///     },
    ///     "interval": "30s",
    ///     "intervalMs": 30000,
    ///     "targets": [
    ///        { "target": "upper_50", "refId": "A", "type": "timeserie" },
    ///        { "target": "upper_75", "refId": "B", "type": "timeserie" }
    ///     ],
    ///     "adhocFilters": [{
    ///       "key": "City",
    ///       "operator": "=",
    ///       "value": "Berlin"
    ///     }],
    ///     "format": "json",
    ///     "maxDataPoints": 550
    ///   }
    ///
    /// This method will be invoked once for "upper_50" and once for "upper_75".
    ///
    /// NOTE:  At this point in time, these methods are not invoked in parallel,
    ///        as EF.Core does not support parallel queries on the same DbContext.
    ///        However, this will become a configurable option in the future, with
    ///        default becoming to execute in parallel.
    protected abstract Task<IEnumerable<TimeSeriesData>> GetTimeSeriesData(string metric, TimeSpan interval, TimeRange timeRange, int? maxDataPoints, IEnumerable<Filter> adhocFilters);

    /// Override this method to perform any checks that your endpoint
    /// should perform when Grafana does a Test Connection.  Return
    /// false on any condition that should cause Grafana to fail
    /// the connection test
    protected virtual Task<bool> CheckDatasource()
    {
      _logger?.LogDebug("Default CheckDatasource implementation invoked, returning true.");
      return Task.FromResult(true);
    }

    /// Override this method to implement the /search endpoint.
    /// targetMetric may be null. in which case return all metrics to be able to filter
    /// on. Otherwise, return the values for the metric specified.
    ///
    ///  Note: the results of this method determine what type of response to send back
    ///  to grafana. According to the docs, this API can return either an array or a map.
    ///  For example:
    ///  `["upper_25","upper_50","upper_75","upper_90","upper_95"] ` (array)
    ///  or
    ///  `[ { "text" :"upper_25", "value": 1}, { "text" :"upper_75", "value": 2} ]` (map)
    ///
    ///  The controller will peek at the first value in the response from this method to determine
    ///  which style to return.  If the first  element has no value then an array will be used, other
    ///  wise it will return a map.
    ///
    ///  The default implementation returns an empty list.
    protected virtual Task<IEnumerable<SearchMetric>> GetMetrics(string targetMetric)
    {
        return Task.FromResult(new SearchMetric[0].AsEnumerable());
    }
  }
}
