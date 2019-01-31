using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Host.Logger
{
  public class ElasticLogger
  {
    public string Source { get; set; }
    public string User { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string Duration { get; set; }
    public string ResponseContent { get; set; }
    public string Request { get; set; }
    public string RequestContent { get; set; }
    public string ServerName { get; set; }    
    public string ClientId { get; set; }
  }
}
