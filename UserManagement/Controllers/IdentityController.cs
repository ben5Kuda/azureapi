using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserManagement;
using UserManagement.Models;

namespace UserManagement.Controllers
{
  [Authorize]
  [Route("api/[controller]")]
  [ApiController]
  public class IdentityController : Controller
  {
    [HttpGet]
    public User Get()
    {
      var claims = from c in User.Claims select new { c.Type, c.Value };

      var user = new User
      {
        UsersId = (from u in claims
                   where u.Type == "sub"
                   select u.Value).FirstOrDefault(),
        Username = (from u in claims
                    where u.Type == "unique_name"
                    select u.Value).FirstOrDefault(),
        Name = (from u in claims
                where u.Type == "name"
                select u.Value).FirstOrDefault(),
        Surname = (from u in claims
                   where u.Type == "family_name"
                   select u.Value).FirstOrDefault(),
        Profile = (from u in claims
                   where u.Type == "role"
                   select u.Value).FirstOrDefault()

      };

      return user;
    }
  }
}
