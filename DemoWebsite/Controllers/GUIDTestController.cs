using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Web.Mvc;
using Dapper;
using DemoWebsite.ViewModels;

namespace DemoWebsite.Controllers
{
    public class GUIDTestController : Controller
    {
        //
        // GET: /Home/
        private DbConnection _connection;

        public ActionResult Index()
        {
            return RedirectToAction("List");
        }
        public ActionResult List()
        {
            IEnumerable<GUIDTestViewModel> result;
            using (_connection = Utilities.GetOpenConnection())
            {
                result = _connection.GetList<GUIDTestViewModel>();
            }
            return View(result);
        }

        public ActionResult Details(string id)
        {
            GUIDTestViewModel result;
            using (_connection = Utilities.GetOpenConnection())
            {
                result = _connection.Get<GUIDTestViewModel>(id);
            }
            return View(result);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(GUIDTestViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (_connection = Utilities.GetOpenConnection())
                {
                    _connection.Insert<Guid, GUIDTestViewModel>(model);
                }
                return RedirectToAction("index");
            }
            return View(model);
        }

        public ActionResult Edit(string id)
        {
            GUIDTestViewModel model;
            using (_connection = Utilities.GetOpenConnection())
            {
                model = _connection.Get<GUIDTestViewModel>(id);
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(GUIDTestViewModel model)
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

        public ActionResult Delete(string id)
        {
            using (_connection = Utilities.GetOpenConnection())
            {
                _connection.Delete<GUIDTestViewModel>(id);
            }
            return RedirectToAction("index");
        }


    }
}
