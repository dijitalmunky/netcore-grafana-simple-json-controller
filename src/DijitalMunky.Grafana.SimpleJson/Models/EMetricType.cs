using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DijitalMunky.Grafana.SimpleJson.Models {

  /// Represents an operator in a filter.
  public enum EMetricType {
    [EnumMember( Value = "timeserie" )]
    Timeseries,

    [EnumMember( Value = "table" )]
    Table,
  }
}
