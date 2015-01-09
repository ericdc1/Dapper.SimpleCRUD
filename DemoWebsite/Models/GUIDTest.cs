using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace DemoWebsite.Models
{

    /// <summary>
    /// A class which represents the GUIDTest table.
    /// </summary>
    [Table("GUIDTest")]
    public partial class GUIDTest
    {
        [Key]
        public virtual Guid guid { get; set; }
        public virtual string name { get; set; }
    }

}