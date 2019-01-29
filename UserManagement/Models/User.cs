using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace UserManagement.Models
{
 public class User
  {
    [Key]
    public string UsersId { get; set; }
    public string Username { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Profile { get; set; }
    //public bool IsLockedOut { get; set; }
    //public int PasswordRetryCount { get; set; }
    //public string Password { get; set; }
  }
}
