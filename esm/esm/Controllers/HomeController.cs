﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using esm.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace esm.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {

        #region Инфа о приложении

        /*
        Метод вывода стандартной страницы информации о приложении.
        Выходные параметры:
        ActionResult, с помощью которого пользователю предоставляется страница About.
        */
        [AllowAnonymous]
        public ActionResult About()
        {
            ViewBag.Message = "MatanBar application description page.";
            return View();
        }

        /*
        Метод вывода стандартной страницы обратной связи.
        Выходные параметры:
        ActionResult, с помощью которого пользователю предоставляется страница Contact.
        */
        [AllowAnonymous]
        public ActionResult Contact()
        {
            ViewBag.Message = "MatanBar contact page.";
            return View();
        }

        #endregion

        #region Для вычислений и прочая ерунда

        /*
        Метод вывода главной страницы приложения. Здесь доступен выбор форм отправки задач, отправки функций, 
        просмотра статуса текущих вычислений и вывод результатов решений.
        Выходные параметры:
        ActionResult, с помощью которого пользователю предоставляется страница Master.
        */
        public ActionResult Master()
        {
            return View();
        }

        public ActionResult SendTask()
        {//Форма отправки исходнных данных на сервер
            return View();
        }

        /*
        Метод вывода страницы отправки функций.
        Выходные параметры:
        ActionResult, с помощью которого пользователю предоставляется страница SendFunc.
        */
        public ActionResult SendFunc()
        {
            return View();
        }

        /*
        Метод реализует сборку страницы для произведения вычислений на стороне клиента.
        Идентификатор пользователя берется из значения сессии.
        Входные параметры:
        1) Строка с идентификатором задачи. В строке должно находиться целое неотрицательное число или -1.
        По умолчанию стоит -1, что обозначает, что нужный идентификатор не был установлен.
        Все прочие значения являются ошибкой.
        2) Строка с именем функции. Имя функции может быть произвольной строкой содержащей печатные символы.
        По умолчанию стоит -1, что обозначает, что нужное имя не было установлено.
        Все прочие значения являются ошибкой.
        Выходные параметры:
        ActionResult, с помощью которого пользователь перенаправляется на страницу Master.
        Если в ходе работы произошли ошибки, выводится сообщение об этом.
        Побочные эффекты:
        В ходе выполнения метода:
        1) может модифицироваться файл /App_Data/log.txt;
        2) устанавливается время последней активности пользователя. 
        */
        public ActionResult Calculation(string task="-1", string func="-1")
        {
            try
            {
                Models.DatabaseMediator db = new Models.DatabaseMediator(Server.MapPath("~"));
                db.setUserLastActivity((int)Session["user_id"], DateTime.UtcNow);
                db.close();
                if (task != "-1" && func != "-1")
                {
                    string taskFile = "/Content/data/" + task + ".js";
                    string funcFile = "/Content/func/" + func + ".js";
                    string html = "<script src=\"" + taskFile + "\"> </script>";
                    html += "<script src=\"" + funcFile + "\"> </script>";
                    html += "<script src=\"/Scripts/client_calc.js\"> </script>";
                    html += "<script> makeCalculation(\"" + task + "\"); </script>";
                    return Content(html);
                }
                return View("Master");
            }
            catch (Exception e)
            {
                System.IO.File.AppendAllText(Server.MapPath("~/App_Data/log.txt"), e.Message);
                ViewBag.MessagerFromControl = "Произошла ошибка, зайдите позже.";
                return View("Master");
            }            
        }

        /*
        Метод в котором проверяется решает ли пользователь с данным идентификатором задачу поставленную текущим пользователем.
        Идентификатор текущего пользователя берется из значения сессии.
        Входные данные:
        Идентификатор проверяемого пользователя: целое неотрицательное число или -1.
        По умолчанию стоит -1, что обозначает, что нужный идентификатор не был установлен.
        Все прочие значения являются ошибкой.
        Выходные данные:
        Булева переменная, обозначающая ответ.        
        */
        public bool checkUser(int id)
        {
            try
            {
                int masterId = (int)Session["user_id"];
                Models.DatabaseMediator db = new Models.DatabaseMediator(Server.MapPath("~"));
                Task t = db.getUser(id).getTask();
                if (t == null)
                    return true;
                while (t.getParentTaskId() != -1)
                    t = db.loadTask(t.getParentTaskId());
                return t.getOwnerId() != masterId;
            }
            catch (Exception e)
            {
                throw e;
            }            
        }

        public ActionResult Results()
        {//Форма статуса вычислений. Если вычисление закончено, то показан результат
            Models.DatabaseMediator db = new Models.DatabaseMediator(Server.MapPath("~"));//обращаемся к базе
            Models.Task[] array = db.getUserTasks((int)Session["user_id"]);//выцыганиваем id юзера из сессии
            db.close();
            //ну и как то это всё обработали и  вывели

            List<String> list = new List<String>();
            string line;
            foreach (Models.Task t in array)
            {
                if(t.isSolved())
                {
                    // выводим
                    using (StreamReader sr = new StreamReader(t.getResultFilePath()))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            list.Add(t.getTaskId()+") "+line);
                        }
                    }
                }
            }
            if (list.Count == 0)
            {
                ViewBag.Out = null;
            }
            else
            {
                ViewBag.Out = list;
            }
            return View();           
        }

        public ActionResult Status()
        {
            List<String[]> userDataList = new List<String[]>();
            string line;
            using (StreamReader sr = new StreamReader(Server.MapPath("~/App_Data/UserData.txt")))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if(line != "")
                        userDataList.Add(line.Split('|'));
                }
            }

            List<String[]> userOnline = new List<String[]>();
            using (StreamReader sr = new StreamReader(Server.MapPath("~/App_Data/OnlineUsers.txt")))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line != "")
                        userOnline.Add(line.Split(' '));
                }
            }

            List<String[]> userStatus = new List<String[]>();
            for(int i=0; i<userDataList.Count; i++)
            {
                if ( checkUser(Convert.ToInt32(userDataList[i][0])) )
                {
                    continue;
                }
                userStatus.Add(new string[6]);
                for(var j=0; j<5; j++)
                {
                    userStatus[userStatus.Count-1][j] = userDataList[i][j];
                }
            }

            foreach (var i in userOnline)
            {
                for(int j=0; j<userStatus.Count; j++)
                {
                    if(userStatus[j][1] == i[0])
                    {
                        userStatus[j][5] = "True";
                    }
                    else
                    {
                        userStatus[j][5] = "False";
                    }
                }
            }
            ViewData["UserStat"] = userStatus;
            return View("Status");
        }

        public ActionResult resetTask(int id)
        {
            Scheduler s = new Scheduler(Server.MapPath("~"));
            s.resetTask(id);
            return Status();
        }

        /*
        Метод отвечающий за сбор результата с клиента. Если существует родительская задача, её состояние обновляется.
        Предполагается, что метод выполняется в фоновом для пользователя режиме.
        Идентификатор пользователя берется из значения сессии.
        Входные параметры:
        1) Строка с идентификатором задачи. В строке должно находиться целое неотрицательное число или -1.
        По умолчанию стоит -1, что обозначает, что нужный идентификатор не был установлен.
        Все прочие значения являются ошибкой.
        2) Строка с ответом на задачу. Ограничения не накладываются.
        Выходные параметры:
        ActionResult, с помощью которого пользователю предоставляется страница TransferOut.
        Побочные эффекты:
        1) Ответ записывается в файл с № задачи.
        2) В ходе работы может модифицироваться файл /App_Data/log.txt;
        */
        public ActionResult TransferOut(string task, string result)
        {
            try
            {
                int taskId = Convert.ToInt32(task);//каким-то образом получили id решеной задачи

                Models.DatabaseMediator db = new Models.DatabaseMediator(Server.MapPath("~"));
                Models.Task t = db.loadTask(taskId);//нашли нужную задачу
                t.setAnswer(result);//записали ответ
                db.saveTask(t);//сохранили в базу

                int userId = (int)Session["user_id"];
                User u = db.getUser(userId);
                u.resetTask();
                db.updateUser(u);

                int parent = t.getParentTaskId();
                if (parent != -1)//если есть родитель пишем результат в родителя
                {
                    Models.Task t2 = db.loadTask(parent);//нашли родительскую задачу
                    bool fin = t2.updateTask(result);//обновили её
                    db.saveTask(t2);
                    db.close();
                    if (fin)
                    {//если все данные родительской задачи получены, то ставим на выполнение
                        Models.Scheduler s = new Models.Scheduler(Server.MapPath("~"));
                        while (!s.setTask(t2))
                        {
                            System.Threading.Thread.Sleep(2000);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.IO.File.AppendAllText(Server.MapPath("~/App_Data/log.txt"), e.Message);
                ViewBag.MessagerFromControl = "Произошла ошибка, зайдите позже.";
                return View("Master");
            }

            return View();
        }

        /*
        Метод отвечающий за постановку задачи пользователям.
        Предполагается, что метод выполняется в фоновом для пользователя режиме.
        Идентификатор пользователя берется из значения сессии.
        Входные параметры:
        Целое число представляющее тип запроса: 
        1 - проверка, есть ли задача, которую нужно решить; 
        2 - запрос идентификатора задачи;
        3 - имя функции, которой нужно решить задачу.
        Все остальные значения - ошибочный запрос.
        Выходные параметры:
        Страница на которой в зависимости от запроса написано:
        1) ok - запрос успешно отработан; task - пользователю поставлена задача.
        2) номер задачи.
        3) имя функции.
        В случае ошибки на странице будет написано error.
        Побочные эффекты:
        Устанавливается время последней активности пользователя. 
        */
        [AllowAnonymous]
        public ContentResult BackgroundCheck(int request=0)
        {
            try
            {
                string result = "";
                Models.DatabaseMediator db = new Models.DatabaseMediator(Server.MapPath("~"));//обращаемся к базе
                Models.User u;
                Models.Task t;
                switch (request)
                {
                    default:
                    case 0://пустое поле или ошибка
                        result = "bad request";
                        break;
                    case 1://юзер говорит нам, что жив
                        u = db.getUser((int)Session["user_id"]);
                        db.setUserLastActivity(u.getId(), DateTime.UtcNow);
                        t = u.getTask();
                        if (t == null)
                            result = "ok";
                        else
                            result = "task";
                        break;
                    case 2://возвращаем юзеру id поставленной ему задачи
                        u = db.getUser((int)Session["user_id"]);
                        db.setUserLastActivity(u.getId(), DateTime.UtcNow);
                        t = u.getTask();
                        if (t != null)
                            result = t.getTaskId().ToString();
                        else
                            result = "-1";
                        break;
                    case 3://возвращаем юзеру название функции для его задачи
                        u = db.getUser((int)Session["user_id"]);
                        db.setUserLastActivity(u.getId(), DateTime.UtcNow);
                        t = u.getTask();
                        if (t != null)
                            result = t.getFunctionName();
                        else
                            result = "-1";
                        break;
                }
                db.close();
                //вызвать deadChecker
                return Content(result);
            }
            catch (Exception e)
            {
                System.IO.File.AppendAllText(Server.MapPath("~/App_Data/log.txt"), e.Message);
                ViewBag.MessagerFromControl = "Произошла ошибка, зайдите позже.";
                return Content("error");
            }
        }

        /*
        Метод проводит поиск неактивных пользователей с задачами и переназначает их задачи для других пользователей.
        Выполняется в фоне на сервере.
        */
        public void deadChecker()
        {
            Models.DatabaseMediator db = new Models.DatabaseMediator(Server.MapPath("~"));//обращаемся к базе
            List<Models.User> users = db.getUnactiveUsersWithTask();
            Models.Scheduler s = new Models.Scheduler(Server.MapPath("~"));
            for (int i = 0; i < users.Count; ++i)
            {
                while ( !s.resetTask(users[i].getId()) )
                {
                    System.Threading.Thread.Sleep(2000);
                }
            }
            db.close();
        }

        #endregion

        #region Авторизация и регистрация, главная

        /*
         * Метод для авторизации пользователя.
         * Входные параметры:
         * 1. Логин пользователя. Если ничего не было введено в поле "логин", то null.
         * 2. Пароль пользователя. Если ничего не было введено в поле "пароль", то null.
         * Выходные параметры:
         * Если авторизация успешна, то ActionResult с помощью которого пользователь перенаправляется на страницу Master,
         * иначе на страницу  Index. Куки, в которых хранится, информация о вошедшем пользователе.
         * Если в ходе работы произошли ошибки, выводится сообщение об этом.
         * Побочные эффекты:
         * 1. Модифицируется файл /App_Data/UserData.txt
         * 2. Модифицируется файл /App_Data/OnlineUsers.txt
         * 3. Модифицируется файл /App_Data/log.txt
         */
        [AllowAnonymous]
        public ActionResult Login(string username, string password)
        {
            try
            {
                SHA256Managed hash = new SHA256Managed();
                byte[] hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(username + password));
                string hashStr = BitConverter.ToString(hashBytes).Replace("-", "");
                System.IO.StreamReader file = new System.IO.StreamReader(new FileStream(Server.MapPath("~/App_Data/Users.txt"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line != "")
                    {
                        string[] logins = line.Split(' ');
                        if (hashStr == logins[1])
                        {
                            Models.DatabaseMediator db = new Models.DatabaseMediator(Server.MapPath("~"));//обращаемся к базе
                            Models.User u = db.getUserByLogin(logins[0]);
                            db.setUserLastActivity(u.getId(), DateTime.UtcNow);
                            Session["user_id"] = u.getId();//выцыганиваем id из базы
                            db.close();//закрыли базу
                            FormsAuthentication.SetAuthCookie(username, false);
                            HttpContext.Response.Cookies["login"].Value = username;
                            System.IO.StreamReader file1 = new System.IO.StreamReader(new FileStream(Server.MapPath("~/App_Data/OnlineUsers.txt"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
                            List<KeyValuePair<string, string>> users = new List<KeyValuePair<string, string>>();
                            string line1;
                            while ((line1 = file1.ReadLine()) != null)
                            {
                                if (line1 != "")
                                {
                                    string[] str = line1.Split(' ');
                                    users.Add(new KeyValuePair<string, string>(str[0], str[1]));
                                }
                            }
                            file1.Close();
                            System.IO.StreamWriter file2 = new System.IO.StreamWriter(new FileStream(Server.MapPath("~/App_Data/OnlineUsers.txt"), FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite));
                            users.Add(new KeyValuePair<string, string>(username, Request.UserHostAddress));
                            string result = "";
                            foreach (var str in users)
                            {
                                result += str.Key + " " + str.Value + "\n";
                            }
                            file2.Write(result);
                            file2.Close();

                            file.Close();
                            return View("Master");
                        }
                    }
                }
                file.Close();
                return View("Index");
            }
            catch(Exception e)
            {
                System.IO.File.AppendAllText(Server.MapPath("~/App_Data/log.txt"), e.Message);
                ViewBag.MessagerFromControl = "Произошла ошибка, зайдите позже.";
                return View("Index");
            }
        }

        /*
         * Метод, который возвращает стартовую страницу приложения.
         * Входные параметры:
         * Отсутствуют
         * Выходные параметры:
         * ActionResult с помощью которого пользователь перенаправляется на страницу Index.
         * Побочные эффекты:
         * Отсутствуют
         */
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }

        /*
         * Метод, который возвращает страницу регистрации.
         * Входные параметры:
         * Отсутствуют
         * Выходные параметры:
         * ActionResult с помощью которого пользователь перенаправляется на страницу Register.
         * Побочные эффекты:
         * Отсутствуют
         */
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View("Register");
        }

        /*
         * Метод для создания нового пользователя.
         * Входные параметры:
         * 1. Экземпляр класса RegMe, содержащий информацию о новом пользователе:
         * Логин - строка или null, если ничего не введено в поле "логин"
         * Пароль - строка или null, если ничего не введено в поле "пароль"
         * Повтор пароля - строка или null, если ничего не введено в поле "повторите пароль"
         * Выходные параметры:
         * Если данные введены корректно, то ActionResult с помощью которого пользователь перенаправляется на страницу Index,
         * иначе на страницу  Register.
         * Если в ходе работы произошли ошибки, выводится сообщение об этом.
         * Побочные эффекты:
         * 1. Модифицируется файл /App_Data/Users.txt
         * 2. Модифицируется файл /App_Data/log.txt
         */
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Create(RegMe regMe)
        {
            try
            {
                List<string> logins = new List<string>();
                System.IO.StreamReader file = new System.IO.StreamReader(new FileStream(Server.MapPath("~/App_Data/Users.txt"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));

                if (regMe.password != regMe.password1)
                {
                    ModelState.AddModelError("", "Пароли не совпадают!");
                }
                List<KeyValuePair<string, string>> logs = new List<KeyValuePair<string, string>>();
                if (!string.IsNullOrWhiteSpace(regMe.login))
                {
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line != "")
                        {
                            string[] str = line.Split(' ');
                            logs.Add(new KeyValuePair<string, string>(str[0], str[1]));
                            logins.Add(str[0]);
                        }
                    }
                    file.Close();
                    if (logins.Count() > 0 && logins.Contains(regMe.login))
                        ModelState.AddModelError("", "Такой пользователь уже существует");
                }
                if (ModelState.IsValid)
                {
                    SHA256Managed hash = new SHA256Managed();
                    byte[] hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(regMe.login + regMe.password));
                    string hashStr = BitConverter.ToString(hashBytes).Replace("-", "");
                    logs.Add(new KeyValuePair<string, string>(regMe.login, hashStr));
                    System.IO.StreamWriter file1 = new System.IO.StreamWriter(new FileStream(Server.MapPath("~/App_Data/Users.txt"), FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite));
                    string resStr = "";
                    foreach (var tmp in logs)
                    {
                        resStr += tmp.Key + " " + tmp.Value + "\n";
                    }
                    file1.Write(resStr);
                    file1.Close();
                    DatabaseMediator db = new DatabaseMediator(Server.MapPath("~"));
                    db.createUser(regMe.login);
                    return View("Index");
                }
                return View("Register");
            }
            catch (Exception e)
            {
                System.IO.File.AppendAllText(Server.MapPath("~/App_Data/log.txt"), e.Message);
                ViewBag.MessagerFromControl = "Произошла ошибка, зайдите позже.";
                return View("Index");
            }
        }

        /*
         * Метод для выхода из приложения.
         * Входные параметры:
         * 1. Строка логин - строка или null, если в куках ничего нет
         * Выходные параметры:
         * ActionResult с помощью которого пользователь перенаправляется на страницу Index.
         * Куки, содержащие информацию о пользователе очищаются.
         * Если в ходе работы произошли ошибки, выводится сообщение об этом.
         * Побочные эффекты:
         * 1. Модифицируется файл /App_Data/UserData.txt
         * 2. Модифицируется файл /App_Data/OnlineUsers.txt
         * 3. Модифицируется файл /App_Data/log.txt
         */
        public ActionResult Logout(string loginUser)
        {
            try
            {
                FormsAuthentication.SignOut();
                System.IO.StreamReader file = new System.IO.StreamReader(new FileStream(Server.MapPath("~/App_Data/OnlineUsers.txt"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
                List<KeyValuePair<string, string>> users = new List<KeyValuePair<string, string>>();
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line != "")
                    {
                        string[] logins = line.Split(' ');
                        users.Add(new KeyValuePair<string, string>(logins[0], logins[1]));
                    }
                }
                file.Close();
                System.IO.StreamWriter file1 = new System.IO.StreamWriter(new FileStream(Server.MapPath("~/App_Data/OnlineUsers.txt"), FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite));
                users = users.Where(c => c.Key != loginUser).ToList();
                string result = "";
                foreach (var str in users)
                {
                    result += str.Key + " " + str.Value + "\n";
                }
                file1.Write(result);
                file1.Close();
                DatabaseMediator db = new DatabaseMediator(Server.MapPath("~"));
                db.setUserLastActivity(loginUser, DateTime.UtcNow);
                return View("Index");
            }
            catch (Exception e)
            {
                System.IO.File.AppendAllText(Server.MapPath("~/App_Data/log.txt"), e.Message);
                ViewBag.MessagerFromControl = "Произошла ошибка, зайдите позже.";
                return View("Index");
            }
        }
        #endregion
       
        //не трогать, мое!!!
        [HttpPost]
        public ActionResult UploadTask(string method)
        {
            string result = "Файл загружен";
            string filePath = "";
            foreach (string file in Request.Files)
            {
                var upload = Request.Files[file];
                if (upload != null)
                {
                    // получаем имя файла
                    string fileName = System.IO.Path.GetFileName(upload.FileName);
                    // сохраняем файл в папку Files в проекте
                    int id = (int)Session["user_id"];
                    filePath = Server.MapPath("~/App_Data/usertask/" + id.ToString());
                    upload.SaveAs(filePath);
                }
            }

            //-----------------
            string resultCheckFile = checkFormatFile(filePath);
            if (resultCheckFile.Length > 0)
            {
                result = "Файл не загружен, ошибка: " + resultCheckFile ;
                //удаляем файл из репозитория
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            else
            {
                //успешная загрузка ставим задачу на выполнение
                Models.Scheduler s = new Models.Scheduler(Server.MapPath("~"));
                bool fl = s.createTask((int)Session["user_id"], filePath, method);
                if (!fl)
                    result = "В данный момент задача не может быть решена. Попробуйте позже.";
            }
            //----------------------
            ViewBag.MessagerFromControl = result;
            return View("SendTask");
        }


        public string checkFormatFile(string nameFile)
        {
           
            int iterator_by_str = -1;
            int saveN = 0;
            string result = "";
            using (StreamReader fs = new StreamReader(nameFile))
            {
                while (true)
                {
                    // Читаем строку из файла во временную переменную.
                    string str = fs.ReadLine();

                    // Если достигнут конец файла, прерываем считывание.
                    if (str == null)
                    {
                        //проверка на четность
                        if (iterator_by_str < saveN)
                        {
                            result = result + "Целое число в 0 строке, обозначающее количество вводимых данных, не соответсвует количеству данных, вводимых ниже 0 строки.";
                        }
                        break;
                    }

                    //проверяем строку
                    if (iterator_by_str == -1)
                    {
                        bool noNum = Regex.IsMatch(str, @"^((\D+))$");
                        bool NoInt = Regex.IsMatch(str, @"^((\d+)(\.+)(\d*))$");
                        bool noCorectFormat = Regex.IsMatch(str, @"^((\d+\,\d+))$");
                        if (noNum || NoInt || noCorectFormat)
                        {
                            return result = result + " В 0 строке должно быть целое число, обозначающее количество вводимых данных. ";
                        }
                        
                        saveN = int.Parse(str);
                    }
                    
                    if(iterator_by_str>=0 && iterator_by_str < saveN)
                    {
                        //Проверяем N данных на целое и вещественное число
                        string temp_str= Verification_by_integer_and_double(str, iterator_by_str + 1);
                        //Если пошли параметры до N данных
                        if (temp_str != "")
                        {
                            if (Verification_by_parameter(str, iterator_by_str + 1)=="")
                            {
                                result = result + "количество данных (" + ((int)((int)iterator_by_str + (int)1)) + ") меньше, чем заявлено в 0 строке (" + saveN + ")";
                            }
                            else
                            {
                                result = result + temp_str;
                            }
                        }
                    }
                    else if(iterator_by_str >= saveN)//проверка параметров
                    {
                        string temp_str = Verification_by_parameter(str, iterator_by_str + 1);
                        //Если данные идут после N итераций
                        if (temp_str != "")
                        {
                            if (Verification_by_integer_and_double(str, iterator_by_str + 1) == "")
                            {
                                result = result + "количество данных(" + ((int)((int)iterator_by_str + (int)1)) + ") больше, чем заявлено в 0 строке (" + saveN + ")";
                                
                            }
                            else
                            {
                                result = result + temp_str;
                            }
                        }
                    }

                    iterator_by_str++;

                }
            }

            // Выводим на экран.
            return result;
        }

        string Verification_by_integer_and_double(string str,int iterator_by_str)
        {
            double number_double;// для проверки на вещественное число, пока по другому лень сделать (try() catch{})
            int number_int;//для проверки на целое число, пока по другому лень сделать (try() catch{})
            bool flag_an_number_double= Regex.IsMatch(str, @"(^\s*[+-]{0,1}[0-9]+[.][0-9]+\s*$)|(^\s*[+-]{0,1}[0-9]+[.][0-9]+[eE]{0,1}[+-]{0,1}[0-9]+\s*$)");
            bool flag_an_number_int= Int32.TryParse(str, out number_int);
            string result = "";

            if (Regex.IsMatch(str, @"^((\d+\,\d+))$"))//если в числе есть запятая
            {
                result = result + " Не верный формат данных в " + (iterator_by_str) + " строке, для обозначения вещественного числа должна использоваться точка (" + str + "). ";
            }
            else if (!flag_an_number_double)
            {
                if (!flag_an_number_int)//если строка не целое и не вещественное число
                {
                    result = result + " Не верный формат данных в " + (iterator_by_str) + " строке, введеное число не вещественное и не целое (" + str + "). ";
                }
            }

            return result;
        }

        string Verification_by_parameter(string str, int iterator_by_str)
        {
            string result = "";
            bool flag_an_whitespace = Regex.IsMatch(str, @"^[a-zA-Z0-9.]+\s[a-zA-Z0-9.]+$");
            bool flag_an_parameter = false;// Regex.IsMatch(str, @"[a-zA-Z]");
            if (!flag_an_whitespace && !flag_an_parameter)// проверка на пробелы и переменную
            {
                result = result + " Не верный формат данных в " + (iterator_by_str) + " строке, в строке должны быть только два слова и один пробел между ними (" + str + ").";
            }
            return result;
        }

        /*
        Метод производит загрузку файла с кодом новой функции.
        Выходные параметры:
        ActionResult, с помощью которого пользователю предоставляется страница SendFunc.
        Побочные эффекты:
        Создание файла с именем {user%userid%}%filename%.js в папке /Content/func/.
        */
        [HttpPost]
        public ActionResult UploadFunc()
        {
            try
            {
                string filePath = "";
                foreach (string file in Request.Files)
                {
                    var upload = Request.Files[file];
                    if (upload != null)
                    {
                        // получаем имя файла
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(upload.FileName);
                        // сохраняем файл в папку Files в проекте
                        int id = (int)Session["user_id"];
                        filePath = Server.MapPath("~/Content/func/{user" + id.ToString() + "}" + fileName + ".js");
                        upload.SaveAs(filePath);
                    }
                }
                ViewBag.MessagerFromControl = "Файл загружен";
                return View("SendFunc");
            }
            catch (Exception e)
            {
                System.IO.File.AppendAllText(Server.MapPath("~/App_Data/log.txt"), e.Message);
                ViewBag.MessagerFromControl = "Произошла ошибка, зайдите позже.";
                return View("Master");
            }
        }

    }
    
}
