using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DemoWebsite.Models;

namespace DemoWebsite.ViewModels
{

    //viewmodel extends generated model and adds annotations and other items useful to the actual view
    //this method could be dangerous due to overposting if this viewmodel edits fewer properties than the model contains
    //http://bradwilson.typepad.com/blog/2010/01/input-validation-vs-model-validation-in-aspnet-mvc.html
    public partial class UserViewModel : User
    {
        [Required]
        [DisplayName("First Name")]
        public override string FirstName { get; set; }
        [Required]
        [DisplayName("Last Name")]
        public override string LastName { get; set; }

        [Range(0, 130)]
        [Column("intAge")] //adding this again since we are overriding the model Age property to put on the data annotations
        public override int Age { get; set; }

        [Editable(false)]
        [DisplayName("Full Name")]
        public string FullName
        {
            get { return string.Format("{0} {1}", FirstName, LastName); }
        }


    }

}