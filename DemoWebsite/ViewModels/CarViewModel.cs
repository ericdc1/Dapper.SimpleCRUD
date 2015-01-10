using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DemoWebsite.ViewModels
{

    //viewmodel uses same properties from generated model 
    //and adds annotations and other items useful to the actual view
    public class CarViewModel : Models.Car
    {
        [Required]
        [StringLength(50)]
        public override string Make { get; set; }
        [Required]
        [StringLength(50)]
        [DisplayName("Model")]
        public override string ModelName { get; set; }
    }

}