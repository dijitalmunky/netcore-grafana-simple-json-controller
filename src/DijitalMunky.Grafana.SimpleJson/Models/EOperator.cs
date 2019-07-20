using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DijitalMunky.Grafana.SimpleJson.Models {

  /// Represents an operator in a filter.
  public enum EOperator {

    [EnumMember( Value = "=" )]
    Equals,

    [EnumMember( Value = "!=" )]
    NotEquals,

    [EnumMember( Value = "<" )]
    LessThan,

    [EnumMember( Value = "<=" )]
    LessThanOrEquals,

    [EnumMember( Value = ">" )]
    GreaterThan,

    [EnumMember( Value = ">=" )]
    GreaterThanOrEquals,

  }
}
