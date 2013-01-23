using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SampleWebsite.Models;
using Dapper;
using SampleWebsite.ViewModels;

namespace SampleWebsite.Controllers
{
    public class UserController : Controller
    {
        // GET: /Home/
        private DbConnection _connection;

        public ActionResult Index()
        {
            IEnumerable<User> result;
            using (_connection = Utilities.GetOpenConnection())
            {
                result = _connection.GetList<User>();
            }
            return View(UserAddEdit.MapListUserToUserAddEdit(result));
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(UserAddEdit viewmodel)
        {
            if (ModelState.IsValid)
            {
                //manual mapping - this would be easier with automapper
                var user = UserAddEdit.MapUserAddEditToUser(viewmodel);
                using (_connection = Utilities.GetOpenConnection())
                {
                    _connection.Insert(user);
                }
                return RedirectToAction("index");
            }
            return View(viewmodel);
        }

        public ActionResult Edit(int id)
        {
            User model;
            using (_connection = Utilities.GetOpenConnection())
            {
                model = _connection.Get<User>(id);
            }
            return View(UserAddEdit.MapUserToUserAddEdit(model));
        }

        [HttpPost]
        public ActionResult Edit(UserAddEdit viewmodel)
        {
            if (ModelState.IsValid)
            {
                using (_connection = Utilities.GetOpenConnection())
                {
                    _connection.Update(UserAddEdit.MapUserAddEditToUser(viewmodel));
                }
                return RedirectToAction("index");
            }

            return View(viewmodel);
        }

        public ActionResult Delete(int id)
        {
            using (_connection = Utilities.GetOpenConnection())
            {
                _connection.Delete<User>(id);
            }
            return RedirectToAction("index");
        }


    }

}

