using System;
using System.Collections.Generic;
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
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        //GET
        public async Task<IActionResult> Index()
        {
            return View(await _db.Category.ToListAsync());
        }

        //GET - Create
        public IActionResult Create()
        {
            return View();
        }

        //POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _db.Category.Add(category);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        //GET -Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var categoryFromDB = await _db.Category.FindAsync(id);
            if (categoryFromDB == null)
                return NotFound();

            return View(categoryFromDB);
        }

        //POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _db.Update(category);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));

            }
            return View(category);
        }

        //GET -Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var categoryFromDB = await _db.Category.FindAsync(id);
            if (categoryFromDB == null)
                return NotFound();

            return View(categoryFromDB);
        }

        //POST - Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null)
                return NotFound();

            var categoryFromDb = await _db.Category.FindAsync(id);
            if (categoryFromDb == null)
                return NotFound();

            _db.Remove(categoryFromDb);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET -Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var categoryFromDB = await _db.Category.FindAsync(id);
            if (categoryFromDB == null)
                return NotFound();

            return View(categoryFromDB);
        }
    }
}
