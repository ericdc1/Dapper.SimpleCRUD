using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace DemoWebsite.Models
{
    /// <summary>
    /// A class which represents the Users table.
    /// </summary>
    [Table("Users")]
    public partial class User
    {
        [Key]
        public virtual int UserId { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        [Column("intAge")] 
        public virtual int Age { get; set; }
    }

}