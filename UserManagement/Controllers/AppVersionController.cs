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
    public AppVersion Get()
    {
      return new AppVersion
      {
        AppVesrion = "1.12",
        BuildVesrion = "1078brd"
      };
    }
  }
}
