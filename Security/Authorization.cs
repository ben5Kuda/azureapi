using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Host.Security
{
  public class Authorization : AuthorizeAttribute
  {
    public Authorization()
    {
    
    }
  }
}
