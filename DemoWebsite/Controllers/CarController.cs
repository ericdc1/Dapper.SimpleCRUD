using System.Collections.Generic;
using System.Data.Common;
using System.Web.Mvc;
using Dapper;
using DemoWebsite.ViewModels;

namespace DemoWebsite.Controllers
{
    public class CarController : Controller
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
            IEnumerable<CarViewModel> result;
            using (_connection = Utilities.GetOpenConnection())
            {
                result = _connection.GetList<CarViewModel>();
            }
            return View(result);
        }

        public ActionResult Details(int id)
        {
            CarViewModel result;
            using (_connection = Utilities.GetOpenConnection())
            {
                result = _connection.Get<CarViewModel>(id);
            }
            return View(result);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(CarViewModel model)
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
            CarViewModel model;
            using (_connection = Utilities.GetOpenConnection())
            {
                model = _connection.Get<CarViewModel>(id);
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(CarViewModel model)
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
                _connection.Delete<CarViewModel>(id);
            }
            return RedirectToAction("index");
        }


    }
}
