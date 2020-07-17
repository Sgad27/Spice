using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Microsoft.AspNetCore.Authorization;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Constants.ManagerUser)]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var coupons = await _db.Coupon.ToListAsync();
            return View(coupons);
        }

        //GET - Create
        public IActionResult Create()
        {
            return View();
        }

        //POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                //Converting image to bytes
                var files = HttpContext.Request.Form.Files;
                if (files.Count() > 0)
                {
                    byte[] imageInBytes = null;
                    using (var fileStream = files[0].OpenReadStream())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            fileStream.CopyTo(memoryStream);
                            imageInBytes = memoryStream.ToArray();
                        }
                    }

                    coupon.Picture = imageInBytes;
                }

                _db.Coupon.Add(coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        //GET -Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var couponFromDB = await _db.Coupon.FindAsync(id);
            if (couponFromDB == null)
                return NotFound();

            return View(couponFromDB);
        }

        //POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Coupon coupon)
        {
            if (coupon.Id == 0)
                return NotFound();

            if (ModelState.IsValid)
            {
                var couponFromDb = await _db.Coupon.FindAsync(coupon.Id);
                if (couponFromDb == null)
                    return NotFound();

                //Converting image to bytes
                var files = HttpContext.Request.Form.Files;
                if (files.Count() > 0)
                {
                    byte[] imageInBytes = null;
                    using (var fileStream = files[0].OpenReadStream())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            fileStream.CopyTo(memoryStream);
                            imageInBytes = memoryStream.ToArray();
                        }
                    }

                    couponFromDb.Picture = imageInBytes;
                }

                couponFromDb.Name = coupon.Name;
                couponFromDb.Discount = coupon.Discount;
                couponFromDb.MinimumAmount = coupon.MinimumAmount;
                couponFromDb.IsActive = coupon.IsActive;
                couponFromDb.CouponType = coupon.CouponType;

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));

            }
            return View(coupon);
        }

        //GET -Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var coupon = await _db.Coupon.SingleOrDefaultAsync(m => m.Id == id);

            if (coupon == null)
                return NotFound();

            return View(coupon);
        }

        //GET -Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var couponFromDB = await _db.Coupon.FindAsync(id);
            if (couponFromDB == null)
                return NotFound();

            return View(couponFromDB);
        }

        //POST - Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null)
                return NotFound();

            var couponFromDB = await _db.Coupon.FindAsync(id);
            if (couponFromDB == null)
                return NotFound();

            _db.Remove(couponFromDB);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}
