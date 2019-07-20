using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DijitalMunky.Grafana.SimpleJson.Models {

  /// Represents a Filter.  As described by the SimpleJson
  /// plugin docs, the following JSON represents this object:
  ///   { "target": "upper_50", "refId": "A", "type": "timeserie" }
  public sealed class TargetMetric {
    public string Target { get; set; }
    public string RefId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public EMetricType Type { get; set; }
  }
}
