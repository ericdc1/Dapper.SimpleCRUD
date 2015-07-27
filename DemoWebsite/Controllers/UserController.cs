using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Web.Mvc;
using Dapper;
using DemoWebsite.ViewModels;
using MvcPaging;

namespace DemoWebsite.Controllers
{
    public class UserController : Controller
    {
        //
        // GET: /Home/
        private DbConnection _connection;

        public ActionResult Index()
        {
            return RedirectToAction("List");
        }
        public ActionResult List(int page = 1)
        {
            IEnumerable<UserViewModel> result;
            using (_connection = Utilities.GetOpenConnection())
            {
                var totalRecords = _connection.RecordCount<UserViewModel>();
                result = _connection.GetListPaged<UserViewModel>(page, 10, null, "intAge desc").ToPagedList(page, 10, totalRecords);
            }
            return View(result);
        }

        public ActionResult Details(int id)
        {
            UserViewModel result;
            using (_connection = Utilities.GetOpenConnection())
            {
                result = _connection.Get<UserViewModel>(id);
            }
            return View(result);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (_connection = Utilities.GetOpenConnection())
                {
                    _connection.Insert(model);
                }
                return RedirectToAction("index");
            }
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            UserViewModel model;
            using (_connection = Utilities.GetOpenConnection())
            {
                model = _connection.Get<UserViewModel>(id);
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (_connection = Utilities.GetOpenConnection())
                {
                    _connection.Update(model);
                }
                return RedirectToAction("index");
            }

            return View(model);
        }

        public ActionResult Delete(int id)
        {
            using (_connection = Utilities.GetOpenConnection())
            {
                _connection.Delete<UserViewModel>(id);
            }
            return RedirectToAction("index");
        }


    }
}
