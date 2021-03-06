﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using Microsoft.AspNet.Identity;
using Payments.BLL.DTO;
using Payments.BLL.Infrastructure;
using Payments.BLL.Interfaces;
using Payments.Common.Enums;
using Payments.Common.NLog;
using Payments.WEB.Areas.Admin.Models;
using Payments.WEB.Util;

namespace Payments.WEB.Areas.Employee.Controllers
{
    [Authorize(Roles="employee, admin")]
    // admin controller to manage users
    public class UsersController : Controller
    {
        private IManageService service;

        public UsersController(IManageService serv)
        {
            NLog.LogInfo(this.GetType(), "Constructor UsersController execution");

            service = serv;
        }

        public ActionResult List()
        {
            NLog.LogInfo(this.GetType(), "Method List execution");

            ViewBag.Message = TempData["Message"]?.ToString();

            var list = service.GetProfiles();

            return View(list);
        }

        public ActionResult Show(string id)
        {
            NLog.LogInfo(this.GetType(), "Method Show execution");

            TempData["UserId"] = id;

            if (id == null)
            {
                TempData["Message"] = "Id is not passed";
                return RedirectToAction("List");
            }
            var user = service.GetProfile(id);
            
            if (user == null)
            {
                TempData["Message"] = "There is no user with id " + id;
                return RedirectToAction("List");
            }

            ViewBag.Message = TempData["Message"]?.ToString();
            return View(user);
        }

        public ActionResult Accounts(string id)
        {
            NLog.LogInfo(this.GetType(), "Method Accounts execution");

            if (id != null)
            {
                string sortType = this.Request.QueryString["Order"];

                var accounts = service.GetDebitAccountsByProfile(id, sortType: sortType);
                var accountsViewList = Mapper.Map<IEnumerable<DebitAccountDTO>, IEnumerable<DebitAccountViewModel>>(accounts);

                return View(accountsViewList);
            }

            return View();
        }

        [HttpGet]
        public ActionResult CreateDebitAccount(string id)
        {
            NLog.LogInfo(this.GetType(), "Method CreateDebitAccount GET execution");

            if (id == null)
                return RedirectToAction("List");

            var debitAcc = new DebitAccountViewModel();
            debitAcc.ClientProfileId = id;

            return View(debitAcc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateDebitAccount(DebitAccountViewModel debitAcc)
        {
            NLog.LogInfo(this.GetType(), "Method CreateDebitAccount POST execution");

            if (ModelState.IsValid)
            {
                var debitAccDto = Mapper.Map<DebitAccountViewModel, DebitAccountDTO>(debitAcc);
                service.CreateDebitAccount(debitAccDto);

                TempData["Message"] = "New debit account was created";
                return RedirectToAction("Show", new { id = debitAcc.ClientProfileId});
            }

            return View();
        }

        public ActionResult BlockUser(string id)
        {
            NLog.LogInfo(this.GetType(), "Method BlockUser execution");

            try
            {
                service.BlockUser(id);

                TempData["Message"] = "User " + id + " was successfully blocked";
            }
            catch (ValidationException e)
            {
                NLog.LogError(this.GetType(), "Exception: " + e.Message);

                TempData["Message"] = e.Message;
            }

            return RedirectToAction("Show", new {id = id});
        }

        public ActionResult UnblockUser(string id)
        {
            NLog.LogInfo(this.GetType(), "Method UnblockUser execution");

            try
            {
                service.UnblockUser(id);

                TempData["Message"] = "User " + id + " was successfully unblocked";
            }
            catch (ValidationException e)
            {
                NLog.LogError(this.GetType(), "Exception: " + e.Message);

                TempData["Message"] = e.Message;
            }

            return RedirectToAction("Show", new { id = id });
        }

        // create credit card
        // payments
        [HttpGet]
        public ActionResult CreateDebitCard(string id)
        {
            NLog.LogInfo(this.GetType(), "Method CreateDebitCard GET execution");

            var accounts = service.GetDebitAccountsByProfile(id, true);

            if (accounts.Any())
            {
                TempData["Message"] = "Profile has no available debit accounts. Create a new account before creating card";

                return RedirectToAction("Show", new {id = id});
            }

            var items = new List<SelectListItem>();
            foreach (var account in accounts)
                items.Add(new SelectListItem {Selected = false,
                    Text = "Account id: " + account.AccountNumber + ", sum on the account: " + account.Sum + ", currency: " + account.Currency,
                    Value = account.AccountNumber.ToString()});

            ViewBag.DropDownListItems = items;
            
            return View();
        }

        // get list of card by user id
        [HttpGet]
        public ActionResult CardsList(string id)
        {
            NLog.LogInfo(this.GetType(), "Method CardsList GET execution");

            try
            {
                var accounts = service.GetCardsByProfile(id);
                return PartialView(accounts);
            }
            catch (ValidationException e)
            {
                NLog.LogError(this.GetType(), "Exception: " + e.Message);

                TempData["Message"] = e.Message;
            }

            if (TempData["UserId"] != null)
                    return RedirectToAction("Show", new { id = TempData["UserId"] });

                return RedirectToAction("List");
        }

        // create new debit card
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateDebitCard(CardViewModel card)
        {
            NLog.LogInfo(this.GetType(), "Method CreateDebitCard POST execution");

            if (ModelState.IsValid)
            {
                var depositCardDto = Mapper.Map<CardViewModel, CardDto>(card);
                service.CreateCard(depositCardDto);

                TempData["Message"] = "The new card was successfully created";

                if (TempData["UserId"] != null)
                    return RedirectToAction("Show", new { id = TempData["UserId"] });

                return RedirectToAction("List");
            }

            return View(card);
        }

        [HttpGet]
        public ActionResult EditDebitAccount(int? id)
        {
            NLog.LogInfo(this.GetType(), "Method EditDebitAccount GET execution");

            try
            {
                var debitAcc = service.GetDebitAccount(id);
                var debitAccViewModel = Mapper.Map<DebitAccountDTO, DebitAccountViewModel>(debitAcc);

                return View(debitAccViewModel);
            }
            catch (ValidationException e)
            {
                NLog.LogError(this.GetType(), "Exception: " + e.Message);

                TempData["Message"] = e.Message;
            }
            if (TempData["UserId"] != null)
                return RedirectToAction("Show", new { id = TempData["UserId"] });

            return RedirectToAction("List");

        }

        [HttpGet]
        public ActionResult Replenish(int? id)
        {
            NLog.LogInfo(this.GetType(), "Method Replenish GET execution");

            try
            {
                var account = service.GetDebitAccount(id);
                var payment = new PaymentViewModel()
                {
                    Recipient = account.AccountNumber.ToString()
                };

                return View(payment);
            }
            catch (ValidationException e)
            {
                NLog.LogError(this.GetType(), "Exception: " + e.Message);

                TempData["Message"] = e.Message;
            }

            if (TempData["UserId"] != null)
                return RedirectToAction("Show", new { id = TempData["UserId"] });

            return RedirectToAction("List");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Replenish(PaymentViewModel payment)
        {
            NLog.LogInfo(this.GetType(), "Method Replenish POST execution");

            if (ModelState.IsValid)
            {
                var paymentDto = Mapper.Map<PaymentViewModel, PaymentDTO>(payment);

                service.Replenish(paymentDto);
            }

            TempData["Message"] = "Account " + payment.Recipient + " was successfully replenished";

            if (TempData["UserId"] != null)
                return RedirectToAction("Show", new { id = TempData["UserId"] });

            return RedirectToAction("List");
        }

        [HttpGet]
        public ActionResult Withdraw(int? id)
        {
            NLog.LogInfo(this.GetType(), "Method Withdraw GET execution");

            try
            {
                var account = service.GetDebitAccount(id);
                var payment = new PaymentViewModel()
                {
                    Recipient = account.AccountNumber.ToString()
                };

                return View(payment);
            }
            catch (ValidationException e)
            {
                NLog.LogError(this.GetType(), "Exception: " + e.Message);

                TempData["Message"] = e.Message;
            }

            if (TempData["UserId"] != null)
                return RedirectToAction("Show", new { id = TempData["UserId"] });

            return RedirectToAction("List");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Withdraw(PaymentViewModel payment)
        {
            NLog.LogInfo(this.GetType(), "Method Withdraw POST execution");

            if (ModelState.IsValid)
            {
                var paymentDto = Mapper.Map<PaymentViewModel, PaymentDTO>(payment);

                try
                {
                    service.Withdraw(paymentDto);

                    TempData["Message"] = "Withdrawal from an account " +
                        payment.Recipient + " was successfully";

                    if (TempData["UserId"] != null)
                        return RedirectToAction("Show", new { id = TempData["UserId"] });

                    return RedirectToAction("List");
                }
                catch (ValidationException e)
                {
                    NLog.LogError(this.GetType(), "Exception: " + e.Message);

                    ModelState.AddModelError(e.Property, e.Message);
                }
            }

            return View(payment);
        }

        // create payment with certain account
        [HttpGet]
        public ActionResult Payment(int id)
        {
            NLog.LogInfo(this.GetType(), "Method Payment GET execution");

            var account = service.GetDebitAccount(id);

            if (account == null)
            {
                TempData["Message"] = "There is no  account with id " + id;

                if (TempData["UserId"] != null)
                    return RedirectToAction("Show", new { id = TempData["UserId"] });

                return RedirectToAction("List");
            }

            var payment = new PaymentViewModel
            {
                AccountAccountNumber = account.AccountNumber
            };

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Payment(PaymentViewModel payment)
        {
            NLog.LogInfo(this.GetType(), "Method Payment POST execution");

            if (ModelState.IsValid)
            {
                var paymentDto = Mapper.Map<PaymentViewModel, PaymentDTO>(payment);
                try
                {
                    service.Payment(paymentDto);
                }
                catch (ValidationException e)
                {
                    ModelState.AddModelError(e.Property, e.Message);
                    return View(payment);
                }

                TempData["Message"] = "Payment with account " + payment.AccountAccountNumber + " was sent to processing";

                if (TempData["UserId"] != null)
                    return RedirectToAction("Show", new { id = TempData["UserId"] });

                return RedirectToAction("List");
            }

            return View(payment);
        }

        // create payment without certain account
        [HttpGet]
        public ActionResult CreatePayment(string id)
        {
            NLog.LogInfo(this.GetType(), "Method CreatePayment GET execution");

            var accounts = service.GetDebitAccountsByProfile(id);

            if (accounts == null)
            {
                TempData["Message"] = "This user has no accounts";

                return RedirectToAction("Show", new { id = TempData["UserId"] });
            }

            var items = new List<SelectListItem>();
            foreach (var account in accounts)
                items.Add(new SelectListItem
                {
                    Selected = false,
                    Text = "Account id: " + account.AccountNumber + ", sum on the account: " + account.Sum + ", currency: " + account.Currency,
                    Value = account.AccountNumber.ToString()
                });

            ViewBag.DropDownListItems = items;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePayment(PaymentViewModel payment)
        {
            NLog.LogInfo(this.GetType(), "Method CreatePayment GET execution");

            // awful  bypass model state
            ModelState.Remove("Id");

            if (ModelState.IsValid)
            {
                var paymentDto = Mapper.Map<PaymentViewModel, PaymentDTO>(payment);
                try
                {
                    service.Payment(paymentDto);

                    TempData["Message"] = "Payment from account " + payment.AccountAccountNumber + " was sent to processing";

                    if (TempData["UserId"] != null)
                        return RedirectToAction("Show", new { id = TempData["UserId"] });

                    return RedirectToAction("List");
                }
                catch (ValidationException e)
                {
                    NLog.LogError(this.GetType(), "Exception: " + e.Message);

                    ModelState.AddModelError(e.Property, e.Message);
                }
            }

            // create data for drop-down list
            var profileId = service.GetAccountProfileId(payment.AccountAccountNumber);
            ViewData["UserId"] = profileId;
            var accounts = service.GetDebitAccountsByProfile(profileId);
            var items = new List<SelectListItem>();
            foreach (var account in accounts)
                items.Add(new SelectListItem
                {
                    Selected = false,
                    Text = "Account id: " + account.AccountNumber + ", sum on the account: " + account.Sum + ", currency: " + account.Currency,
                    Value = account.AccountNumber.ToString()
                });
            ViewBag.DropDownListItems = items;
            
            return View(payment);
        }

        public ActionResult PaymentsList(string id)
        {
            NLog.LogInfo(this.GetType(), "Method PaymentsList execution");

            try
            {
                string sortType = this.Request.QueryString["Order"];

                var listDto = service.GetPaymentsByProfile(id, sortType);

                var list = Mapper.Map<IEnumerable<PaymentDTO>, IEnumerable<PaymentViewModel>>(listDto);

                return View(list);
            }
            catch (ValidationException e)
            {
                NLog.LogError(this.GetType(), "Exception: " + e.Message);

                TempData["Message"] = e.Message;
            }

            if (TempData["UserId"] != null)
                return RedirectToAction("Show", new { id = TempData["UserId"] });

            return RedirectToAction("List");
        }

        public ActionResult ConfirmPayment(string id)
        {
            NLog.LogInfo(this.GetType(), "Method ConfirmPayment execution");

            try
            {
                service.ConfirmPayment(id);
                TempData["Message"] = "Payment id" + id + " was successfully executed";
            }
            catch (ValidationException e)
            {
                NLog.LogError(this.GetType(), "Exception: " + e.Message);

                TempData["Message"] = e.Message;
            }

            if (TempData["UserId"] != null)
                return RedirectToAction("Show", new { id = TempData["UserId"] });

            return RedirectToAction("List");
        }
    }
}