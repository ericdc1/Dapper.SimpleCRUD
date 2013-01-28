using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SampleWebsite.ViewModels
{

    //viewmodel uses same properties from generated model 
    //and adds annotations and other items useful to the actual view
    public class CarAddEdit 
    {
        public int CarId { get; set; }
        [Required]
        [StringLength(50)]
        public string Make { get; set; }
        [Required]
        [StringLength(50)]
        public string Model { get; set; }


        //mapper to convert from domain model to viewmodel - would be easier with Automapper 
        public static CarAddEdit MapCarToCarAddEdit(Models.Car car)
        {
            var caraddedit = new CarAddEdit();
            caraddedit.CarId = car.CarId;
            caraddedit.Make = car.Make;
            caraddedit.Model = car.Model;
            return caraddedit;
        }

        //mapper to convert from viewmodel to domain model 
        public static Models.Car MapCarAddEditToCar(CarAddEdit caraddedit)
        {
            var car = new Models.Car();
            car.CarId = caraddedit.CarId;
            car.Make = caraddedit.Make;
            car.Model = caraddedit.Model;
            return car;
        }

    }

}