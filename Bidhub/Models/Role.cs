﻿using System.ComponentModel.DataAnnotations;

namespace Bidhub.Models
{
    public class Role
    {
        [Key]
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }

        public ICollection<UserRoles> UserRoles { get; set; }
    }
}
