using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserManagement.Models;

namespace UserManagement.Controllers
{ 
  [Route("api/[controller]")]
  [ApiController]
  [Authorize]
  public class LockUsersController : Controller
  {
    private readonly Context.SsoDbContext _sso;

    public LockUsersController(Context.SsoDbContext sso)
    {
      _sso = sso;
    }

   
    [HttpPut]
    public ActionResult Put(string username)
    {
      var user = _sso.Users.Where(u => u.Username == username).FirstOrDefault();

      if (user == null)
      {
        return NotFound();
      }
      var claims = from c in User.Claims select new { c.Type, c.Value };

      var hasAccess = claims.Where(r => r.Type == "role")
            .Where(v => v.Value == "Admin").Any();

      if (!hasAccess)
      {
        return Forbid();
      }

      user.IsLockedOut = true;
      user.PasswordRetryCount = 3;

      _sso.Entry(user).State = EntityState.Modified;
       _sso.SaveChanges();

      return Ok();

    }
  }
}
