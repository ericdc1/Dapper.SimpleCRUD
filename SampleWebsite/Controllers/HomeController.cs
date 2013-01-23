using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Dapper;
using SampleWebsite.Models;
using SampleWebsite.ViewModels;

namespace SampleWebsite.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        private DbConnection _connection;

        public ActionResult Index()
        {
            IEnumerable<Car> result;
            using (_connection = Utilities.GetOpenConnection())
            {
                result = _connection.GetList<Car>();
            }
            return View(result);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(CarAddEdit viewmodel)
        {
            if (ModelState.IsValid)
            {
                //manual mapping - this would be easier with automapper
                var car = CarAddEdit.MapCarAddEditToCar(viewmodel);
                using (_connection = Utilities.GetOpenConnection())
                {
                    _connection.Insert(car);
                }
                return RedirectToAction("index");
            }
            return View(viewmodel);
        }

        public ActionResult Edit(int id)
        {
            Car model;
            using (_connection = Utilities.GetOpenConnection())
            {
                model = _connection.Get<Car>(id);
            }
            return View(CarAddEdit.MapCarToCarAddEdit(model));
        }

        [HttpPost]
        public ActionResult Edit(CarAddEdit viewmodel)
        {
            if (ModelState.IsValid)
            {
                using (_connection = Utilities.GetOpenConnection())
                {
                    _connection.Update(CarAddEdit.MapCarAddEditToCar(viewmodel));
                }
                return RedirectToAction("index");
            }

            return View(viewmodel);
        }

        public ActionResult Delete(int id)
        {
            using (_connection = Utilities.GetOpenConnection())
            {
                _connection.Delete<Car>(id);
            }
            return RedirectToAction("index");
        }


    }
}
