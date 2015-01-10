using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DemoWebsite.Models;

namespace DemoWebsite.ViewModels
{

    //viewmodel extends generated model and adds annotations and other items useful to the actual view
    //this method could be dangerous due to overposting if this viewmodel edits fewer properties than the model contains
    //http://bradwilson.typepad.com/blog/2010/01/input-validation-vs-model-validation-in-aspnet-mvc.html
    public partial class GUIDTestViewModel : GUIDTest
    {
        [Required]
        [DisplayName("Name")]
        public override string name { get; set; }

       
    }

}