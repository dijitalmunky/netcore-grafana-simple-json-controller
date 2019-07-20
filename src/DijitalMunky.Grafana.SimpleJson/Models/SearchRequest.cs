using System;
using System.Collections.Generic;

namespace DijitalMunky.Grafana.SimpleJson.Models {

  /// Represents a request to the /search endpoint.  As described by the SimpleJson
  /// plugin docs, the following JSON represents this object:
  /// { target: 'upper_50' }
  public sealed class SearchRequest {
    public string Target { get; set; }
  }
}
