using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using HelpDesk.Models;

namespace HelpDesk.Controllers
{
    [Authorize]
    public class RequestController : Controller
    {
        HelpdeskContext db = new HelpdeskContext();
        public ActionResult Index()
        {
            // получаем текущего пользователя
            User user = db.Users.Where(m => m.Login == HttpContext.User.Identity.Name).FirstOrDefault();

            var requests = db.Requests.Where(r => r.UserId == user.Id) //получаем заявки для текущего пользователя
                                    .Include(r => r.Category)  // добавляем категории
                                    .Include(r => r.Lifecycle)  // добавляем жизненный цикл заявок
                                    .Include(r => r.User)         // добавляем данные о пользователях
                                    .OrderByDescending(r => r.Lifecycle.Opened); // упорядочиваем по дате по убыванию   

            return View(requests.ToList());
        }

        [HttpGet]
        public ActionResult Create()
        {
            // получаем текущего пользователя
            User user = db.Users.Where(m => m.Login == HttpContext.User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                // получаем набор кабинетов для департамента, в котором работает пользователь
                var cabs = from cab in db.Activs
                           where cab.DepartmentId == user.DepartmentId
                           select cab;
                ViewBag.Cabs = new SelectList(cabs, "Id", "CabNumber");

                ViewBag.Categories = new SelectList(db.Categories, "Id", "Name");

                return View();
            }
            return RedirectToAction("LogOff", "Account");
        }

        // Создание новой заявки
        [HttpPost]
        public ActionResult Create(Request request, HttpPostedFileBase error)
        {
            // получаем текущего пользователя
            User user = db.Users.Where(m => m.Login == HttpContext.User.Identity.Name).FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("LogOff", "Account");
            }
            if (ModelState.IsValid)
            {
                // указываем статус Открыта у заявки
                request.Status = (int)RequestStatus.Open;
                //получаем время открытия
                DateTime current = DateTime.Now;

                //Создаем запись о жизненном цикле заявки
                Lifecycle newLifecycle = new Lifecycle() { Opened = current };
                request.Lifecycle = newLifecycle;

                //Добавляем жизненный цикл заявки
                db.Lifecycles.Add(newLifecycle);

                // указываем пользователя заявки
                request.UserId = user.Id;

                // если получен файл
                if (error != null)
                {
                    // Получаем расширение
                    string ext = error.FileName.Substring(error.FileName.LastIndexOf('.'));
                    // сохраняем файл по определенному пути на сервере
                    string path = current.ToString("dd/MM/yyyy H:mm:ss").Replace(":", "_").Replace("/", ".") + ext;
                    error.SaveAs(Server.MapPath("~/Files/" + path));
                    request.File = path;
                }
                //Добавляем заявку
                db.Requests.Add(request);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(request);
        }
    }
}
