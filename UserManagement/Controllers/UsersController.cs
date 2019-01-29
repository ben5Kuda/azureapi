using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using UserManagement;
using UserManagement.Models;

namespace UserManagement
{
  [Route("api/[controller]")]
  [ApiController]
  [Authorize]
  public class UsersController : Controller
  {
    private readonly Context.SsoDbContext _sso;

    public UsersController(Context.SsoDbContext sso)
    {
      _sso = sso;
    }
    [HttpGet]
    public IQueryable<User> Get()
    {
      return ModelQuery();
    }

    [HttpGet("{id}")]
    public User Get(string id)
    {
      return ModelQuery().Where(u => u.UsersId == id).FirstOrDefault();
    }

    private IQueryable<User> ModelQuery()
    {
      var dbResult = DbQuery();

      var result = from u in dbResult
                   select new User
                   {
                     UsersId = u.UsersId,
                     Username = u.Username,
                     Name = u.Name,
                     Surname = u.Surname,
                     Profile = u.Profile,
                   };

      return result;
    }
    private IQueryable<Context.Users> DbQuery()
    {
      return _sso.Users;
    }
  }
}
