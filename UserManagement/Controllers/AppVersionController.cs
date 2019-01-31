using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using UserManagement.Models;

namespace UserManagement.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AppVersionController : Controller
  {
    [HttpGet]
    public ApplicationVersion Get()
    {
      return new ApplicationVersion
      {
        AppVersion = "1.45",
        BuildVersion = "Azure Build",
        Host = Environment.MachineName
        
      };
    }
  }
}
