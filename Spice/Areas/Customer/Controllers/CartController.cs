using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Services;
using Spice.Utility;
using Stripe;
using Coupon = Spice.Models.Coupon;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IChargeServiceWrapper _chargeService;
        private readonly ICustomerServiceWrapper _customerService;
        private readonly IEmailSender _emailSender;

        [BindProperty]
        public OrderDetailsCart DetailsCart { get; set; }

        public CartController(ApplicationDbContext db, IChargeServiceWrapper chargeService, ICustomerServiceWrapper customerService, IEmailSender emailSender)
        {
            _db = db;
            _chargeService = chargeService;
            _customerService = customerService;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            DetailsCart = new OrderDetailsCart()
            {
                ListCart = await _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value).ToListAsync(),
                OrderHeader = new OrderHeader() { OrderTotal = 0 }
            };

            foreach (var list in DetailsCart.ListCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                DetailsCart.OrderHeader.OrderTotal = Math.Round(DetailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count), 2);
                list.MenuItem.Description = Common.ConvertToRawHtml(list.MenuItem.Description).Substring(0, 99) + "...";
            }

            DetailsCart.OrderHeader.OrderTotalOriginal = DetailsCart.OrderHeader.OrderTotal;

            var sessionCouponCode = HttpContext.Session.GetString(Constants.ssCouponCode);
            if (sessionCouponCode != null)
            {
                var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(c => c.Name.ToLower() == sessionCouponCode.ToLower());
                DetailsCart.OrderHeader.CouponCode = couponFromDb.Name;
                DetailsCart.OrderHeader.OrderTotal = Math.Round(DiscountedPrice(couponFromDb, DetailsCart.OrderHeader.OrderTotal), 2);
            }

            return View(DetailsCart);
        }

        public IActionResult AddCoupon()
        {
            HttpContext.Session.SetString(Constants.ssCouponCode, DetailsCart.OrderHeader.CouponCode ?? string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(Constants.ssCouponCode, string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {
            var cartFromDb = await _db.ShoppingCart.FirstOrDefaultAsync(s => s.Id == cartId);

            if (cartFromDb != null)
            {
                cartFromDb.Count = cartFromDb.Count + 1;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cartFromDb = await _db.ShoppingCart.FirstOrDefaultAsync(s => s.Id == cartId);

            if (cartFromDb != null)
            {
                if (cartFromDb.Count == 1)
                {
                    _db.Remove(cartFromDb);
                    await _db.SaveChangesAsync();

                    var count = _db.ShoppingCart.Where(s => s.ApplicationUserId == cartFromDb.ApplicationUserId).Count();
                    HttpContext.Session.SetInt32(Constants.ssShoppingCartCount, count);
                }
                else
                {
                    cartFromDb.Count = cartFromDb.Count - 1;
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cartFromDb = await _db.ShoppingCart.FirstOrDefaultAsync(s => s.Id == cartId);

            if (cartFromDb != null)
            {
                _db.Remove(cartFromDb);
                await _db.SaveChangesAsync();

                var count = _db.ShoppingCart.Where(s => s.ApplicationUserId == cartFromDb.ApplicationUserId).Count();
                HttpContext.Session.SetInt32(Constants.ssShoppingCartCount, count);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Summary()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            DetailsCart = new OrderDetailsCart()
            {
                ListCart = await _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value).ToListAsync(),
                OrderHeader = new OrderHeader() { OrderTotal = 0 }
            };

            foreach (var list in DetailsCart.ListCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                DetailsCart.OrderHeader.OrderTotal = Math.Round(DetailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count), 2);
            }

            DetailsCart.OrderHeader.OrderTotalOriginal = DetailsCart.OrderHeader.OrderTotal;

            var sessionCouponCode = HttpContext.Session.GetString(Constants.ssCouponCode);
            if (sessionCouponCode != null)
            {
                var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(c => c.Name.ToLower() == sessionCouponCode.ToLower());
                DetailsCart.OrderHeader.CouponCode = couponFromDb.Name;
                DetailsCart.OrderHeader.OrderTotal = Math.Round(DiscountedPrice(couponFromDb, DetailsCart.OrderHeader.OrderTotal), 2);
            }

            var applicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Id == claim.Value);
            DetailsCart.OrderHeader.PickupName = applicationUser.Name;
            DetailsCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            DetailsCart.OrderHeader.PickUpDate = DateTime.Now;
            DetailsCart.OrderHeader.PickUpTime = DateTime.Now;

            return View(DetailsCart);
        }

        [HttpPost, ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummaryPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            DetailsCart.ListCart = await _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value).ToListAsync();

            DetailsCart.OrderHeader.PaymentStatus = Constants.PaymentStatus.Pending;
            DetailsCart.OrderHeader.OrderDate = DateTime.Now;
            DetailsCart.OrderHeader.UserId = claim.Value;
            DetailsCart.OrderHeader.Status = Constants.OrderStatus.Pending;
            DetailsCart.OrderHeader.PickUpTime = Convert.ToDateTime(DetailsCart.OrderHeader.PickUpDate.ToShortDateString() + " " + DetailsCart.OrderHeader.PickUpTime.ToShortTimeString());
            _db.OrderHeader.Add(DetailsCart.OrderHeader);
            await _db.SaveChangesAsync();

            DetailsCart.OrderHeader.OrderTotalOriginal = 0;
            var orderDetailsList = new List<OrderDetails>();
            foreach (var item in DetailsCart.ListCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                var orderDetails = new OrderDetails()
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = DetailsCart.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                DetailsCart.OrderHeader.OrderTotalOriginal += item.MenuItem.Price * item.Count;
                _db.OrderDetails.Add(orderDetails);
            }

            DetailsCart.OrderHeader.OrderTotalOriginal = Math.Round(DetailsCart.OrderHeader.OrderTotalOriginal, 2);

            var sessionCouponCode = HttpContext.Session.GetString(Constants.ssCouponCode);
            if (sessionCouponCode != null)
            {
                var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(c => c.Name.ToLower() == sessionCouponCode.ToLower());
                DetailsCart.OrderHeader.CouponCode = couponFromDb.Name;
                DetailsCart.OrderHeader.OrderTotal = Math.Round(DiscountedPrice(couponFromDb, DetailsCart.OrderHeader.OrderTotalOriginal), 2);
            }
            else
                DetailsCart.OrderHeader.OrderTotal = DetailsCart.OrderHeader.OrderTotalOriginal;

            DetailsCart.OrderHeader.CouponCodeDiscount = DetailsCart.OrderHeader.OrderTotalOriginal - DetailsCart.OrderHeader.OrderTotal;

            _db.ShoppingCart.RemoveRange(DetailsCart.ListCart);
            HttpContext.Session.SetInt32(Constants.ssShoppingCartCount, 0);
            await _db.SaveChangesAsync();

            var user = await _db.ApplicationUser.FirstOrDefaultAsync(x => x.Id == claim.Value);

            var customer = _customerService.Create(new CustomerCreateOptions()
            {
                Name = user.Name,
                Source = stripeToken
            });

            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(DetailsCart.OrderHeader.OrderTotal * 100),
                Currency = "usd",
                Description = "Order Id : " + DetailsCart.OrderHeader.Id,
                Shipping = new ChargeShippingOptions()
                {
                    Name = DetailsCart.OrderHeader.ApplicationUser.Name,
                    Address = new AddressOptions()
                    {
                        Country = "US",
                        State = user.State,
                        City = user.City,
                        Line1 = user.StreetAddress,
                        PostalCode = user.PostalCode
                    },

                },
                Customer = customer.Id
            };

            var charge = _chargeService.Create(options);

            if (charge.BalanceTransactionId == null)
                DetailsCart.OrderHeader.PaymentStatus = Constants.PaymentStatus.Rejected;
            else
                DetailsCart.OrderHeader.TransactionId = charge.BalanceTransactionId;

            if (charge.Status.ToLower() == "succeeded")
            {
                var userEmailAddress = _db.ApplicationUser.FirstOrDefault(a => a.Id == claim.Value).Email;
                await _emailSender.SendEmailAsync(userEmailAddress, "Spice - Order Created : " + DetailsCart.OrderHeader.Id, "Order has been submitted successfully");
                DetailsCart.OrderHeader.PaymentStatus = Constants.PaymentStatus.Approved;
                DetailsCart.OrderHeader.Status = Constants.OrderStatus.Submitted;
            }
            else
                DetailsCart.OrderHeader.PaymentStatus = Constants.PaymentStatus.Rejected;

            await _db.SaveChangesAsync();

            return RedirectToAction("Confirm", "Order", new { id = DetailsCart.OrderHeader.Id });
        }

        private double DiscountedPrice(Coupon coupon, double originalOrderTotal)
        {
            if (coupon == null || coupon.MinimumAmount > originalOrderTotal)
                return originalOrderTotal;

            if (coupon.CouponType == ECouponType.Dollar)
                return (originalOrderTotal - coupon.Discount);

            return (originalOrderTotal - ((originalOrderTotal * coupon.Discount) / 100));

        }
    }
}
