using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Newtonsoft.Json;

namespace DatabaseLibrary.Entities.Client
{
    [Table("User")]
    public class UserClient : IUser
    {
        [Key]
        public Guid IdUser { get; set; }
        public string Name { get; set; }
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
