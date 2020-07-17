using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Constants.ManagerUser)]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        [TempData]
        public string StatusMessage { get; set; }

        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        //GET
        public async Task<IActionResult> Index()
        {
            return View(await _db.SubCategory.Include(c => c.Category).ToListAsync());
        }

        //GET - Create
        public async Task<IActionResult> Create()
        {
            var model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        //POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doesSubCategoryAlreadyExist = _db.SubCategory.Include(s => s.Category).Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);

                if (doesSubCategoryAlreadyExist.Count() > 0)
                {
                    StatusMessage = "Error: SubCategory exists under " + doesSubCategoryAlreadyExist.First().Category.Name + " category. Please use another name.";
                }
                else
                {
                    _db.SubCategory.Add(model.SubCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            var vmModel = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).Distinct().ToListAsync(),
                StatusMessage = StatusMessage
            };
            return View(vmModel);
        }

        public async Task<IActionResult> GetSubCategory(int id)
        {
            var subCategoryList = await _db.SubCategory.Where(x => x.CategoryId == id).ToListAsync();

            return Json(new SelectList(subCategoryList, "Id", "Name"));
        }

        //GET - Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var subCategoryFromDb = await _db.SubCategory.FirstOrDefaultAsync(x => x.Id == id);

            if (subCategoryFromDb == null)
                return NotFound();

            var model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategoryFromDb,
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        //POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoryAndCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doesSubCategoryAlreadyExist = _db.SubCategory.Include(s => s.Category).Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);

                if (doesSubCategoryAlreadyExist.Count() > 0)
                {
                    StatusMessage = "Error: SubCategory exists under " + doesSubCategoryAlreadyExist.First().Category.Name + " category. Please use another name.";
                }
                else
                {
                    var subCategoryFromDb = await _db.SubCategory.FindAsync(model.SubCategory.Id);
                    subCategoryFromDb.Name = model.SubCategory.Name;

                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            var vmModel = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).Distinct().ToListAsync(),
                StatusMessage = StatusMessage
            };
            return View(vmModel);
        }

        //GET -Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var subCategoryFromDb = await _db.SubCategory.FirstOrDefaultAsync(x => x.Id == id);

            if (subCategoryFromDb == null)
                return NotFound();

            var model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategoryFromDb,
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        //POST - Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null)
                return NotFound();

            var subCategoryFromDb = await _db.SubCategory.FindAsync(id);
            if (subCategoryFromDb == null)
                return NotFound();

            _db.Remove(subCategoryFromDb);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET -Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var subCategoryFromDb = await _db.SubCategory.FirstOrDefaultAsync(x => x.Id == id);

            if (subCategoryFromDb == null)
                return NotFound();

            var model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategoryFromDb,
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).Distinct().ToListAsync()
            };

            return View(model);
        }
    }
}
