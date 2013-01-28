using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using SampleWebsite.Models;

namespace SampleWebsite.ViewModels
{

    //viewmodel extends generated model and adds annotations and other items useful to the actual view
    //this method could be dangerous due to overposting if this viewmodel edits fewer properties than the model contains
    //http://bradwilson.typepad.com/blog/2010/01/input-validation-vs-model-validation-in-aspnet-mvc.html
    public partial class UserAddEdit : Models.User
    {
        [Required]
        public override string FirstName { get; set; }
        [Required]
        public override string LastName { get; set; }
        [Range(0, 130)]
        public override int? Age { get; set; }

        [Editable(false)]
        public string FullName
        {
            get { return string.Format("{0} {1}", FirstName, LastName); }
        }


        //mapper to convert from domain model to viewmodel 
        public static UserAddEdit MapUserToUserAddEdit(Models.User user)
        {
            var useraddedit = new UserAddEdit();
            useraddedit.Id = user.Id;
            useraddedit.Age = user.Age;
            useraddedit.FirstName = user.FirstName;
            useraddedit.LastName = user.LastName;
            return useraddedit;
        }

        //mapper to convert from viewmodel to domain model 
        public static Models.User MapUserAddEditToUser(UserAddEdit useraddedit)
        {
            var user = new Models.User();
            user.Id = useraddedit.Id;
            user.Age = useraddedit.Age;
            user.FirstName = useraddedit.FirstName;
            user.LastName = useraddedit.LastName;
            return user;
        }

        public static IEnumerable<UserAddEdit> MapListUserToUserAddEdit(IEnumerable<User> userlist)
        {
            var useraddeditlist = new List<ViewModels.UserAddEdit>();
            foreach (var user in userlist)
            {
                useraddeditlist.Add(MapUserToUserAddEdit(user));
            }
            return useraddeditlist;
        }

    }

}