using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly int _pageSize = 2;
        public OrderController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUser.Id == claim.Value),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };


            return View(orderDetailsViewModel);
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int pageNumber = 1)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            var orderheaders = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(o => o.ApplicationUser.Id == claim.Value).ToListAsync();

            foreach (var orderHeader in orderheaders)
            {
                var orderDetailsViewModel = new OrderDetailsViewModel()
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == orderHeader.Id).ToListAsync()
                };
                orderListVM.Orders.Add(orderDetailsViewModel);
            }

            var total = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(o => o.OrderHeader.Id).Skip((pageNumber - 1) * _pageSize).Take(_pageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo()
            {
                CurrentPage = pageNumber,
                ItemsPerPage = _pageSize,
                TotalItem = total,
                UrlParam = "/Customer/Order/OrderHistory?pageNumber=:"
            };

            return View(orderListVM);
        }

        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.FirstOrDefaultAsync(o => o.Id == id),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };

            orderDetailsViewModel.OrderHeader.ApplicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Id == orderDetailsViewModel.OrderHeader.UserId);

            return PartialView("_IndividualOrderDetails", orderDetailsViewModel);
        }

        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var orderHeader = await _db.OrderHeader.FirstOrDefaultAsync(o => o.Id == id);

            return PartialView("_OrderStatus", orderHeader.Status.ToString());
        }

        [Authorize(Roles = Constants.KitchenUser + "," + Constants.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var orderDetailsVM = new List<OrderDetailsViewModel>();
            var orderheaders = await _db.OrderHeader.Where(o => o.Status == Constants.OrderStatus.Submitted || o.Status == Constants.OrderStatus.InProcess).OrderByDescending(o => o.PickUpTime).ToListAsync();

            foreach (var orderHeader in orderheaders)
            {
                var orderDetailsViewModel = new OrderDetailsViewModel()
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == orderHeader.Id).ToListAsync()
                };
                orderDetailsVM.Add(orderDetailsViewModel);
            }

            return View(orderDetailsVM);
        }

        [Authorize(Roles = Constants.KitchenUser + "," + Constants.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int orderId)
        {
            var orderHeader = await _db.OrderHeader.FindAsync(orderId);
            orderHeader.Status = Constants.OrderStatus.InProcess;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(ManageOrder));
        }

        [Authorize(Roles = Constants.KitchenUser + "," + Constants.ManagerUser)]
        public async Task<IActionResult> OrderReady(int orderId)
        {
            var orderHeader = await _db.OrderHeader.FindAsync(orderId);
            orderHeader.Status = Constants.OrderStatus.Ready;
            await _db.SaveChangesAsync();

            var userEmailAddress = _db.ApplicationUser.FirstOrDefault(a => a.Id == orderHeader.UserId).Email;
            await _emailSender.SendEmailAsync(userEmailAddress, "Spice - Order is ready for pickup : " + orderHeader.Id, "Order is ready for pickup.");

            return RedirectToAction(nameof(ManageOrder));
        }

        [Authorize(Roles = Constants.KitchenUser + "," + Constants.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int orderId)
        {
            var orderHeader = await _db.OrderHeader.FindAsync(orderId);
            orderHeader.Status = Constants.OrderStatus.Cancelled;
            await _db.SaveChangesAsync();

            var userEmailAddress = _db.ApplicationUser.FirstOrDefault(a => a.Id == orderHeader.UserId).Email;
            await _emailSender.SendEmailAsync(userEmailAddress, "Spice - Order Cancelled : " + orderHeader.Id, "Order has been cancelled successfully");

            return RedirectToAction(nameof(ManageOrder));
        }


        [Authorize]
        public async Task<IActionResult> OrderPickup(int pageNumber = 1, string searchEmail = null, string searchName = null, string searchPhone = null)
        {
            var orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            var param = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?pageNumber=:");
            param.Append("&searchName=");
            if (searchName != null)
                param.Append(searchName);
            param.Append("&searchPhone=");
            if (searchPhone != null)
                param.Append(searchPhone);
            param.Append("&searchEmail=");
            if (searchEmail != null)
                param.Append(searchEmail);

            var orderHeaders = new List<OrderHeader>();
            if (searchEmail != null)
                orderHeaders = await _db.OrderHeader.Include(o => o.ApplicationUser).
                    Where(o => o.ApplicationUser.Email.ToLower() == searchEmail.ToLower()).ToListAsync();
            else if (searchName != null)
                orderHeaders = await _db.OrderHeader.Include(o => o.ApplicationUser).
                    Where(o => o.ApplicationUser.Name.ToLower().Contains(searchName.ToLower())).ToListAsync();
            else if (searchPhone != null)
                orderHeaders = await _db.OrderHeader.Include(o => o.ApplicationUser).
                    Where(o => o.ApplicationUser.PhoneNumber == searchPhone).ToListAsync();
            else
                orderHeaders = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(o => o.Status == Constants.OrderStatus.Ready).ToListAsync();

            foreach (var orderHeader in orderHeaders)
            {
                var orderDetailsViewModel = new OrderDetailsViewModel()
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == orderHeader.Id).ToListAsync()
                };
                orderListVM.Orders.Add(orderDetailsViewModel);
            }

            var total = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(o => o.OrderHeader.Id).Skip((pageNumber - 1) * _pageSize).Take(_pageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo()
            {
                CurrentPage = pageNumber,
                ItemsPerPage = _pageSize,
                TotalItem = total,
                UrlParam = param.ToString()
            };

            return View(orderListVM);
        }

        [Authorize(Roles = Constants.FrontDeskUser + "," + Constants.ManagerUser)]
        [HttpPost, ActionName("OrderPickup")]
        public async Task<IActionResult> OrderPickupPost(int orderId)
        {
            var orderHeader = await _db.OrderHeader.FindAsync(orderId);
            orderHeader.Status = Constants.OrderStatus.Completed;
            await _db.SaveChangesAsync();

            var userEmailAddress = _db.ApplicationUser.FirstOrDefault(a => a.Id == orderHeader.UserId).Email;
            await _emailSender.SendEmailAsync(userEmailAddress, "Spice - Order Completed : " + orderHeader.Id, "Order has been completed successfully");

            return RedirectToAction(nameof(OrderPickup));
        }
    }
}
