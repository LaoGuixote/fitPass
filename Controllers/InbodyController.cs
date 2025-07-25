using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using fitPass.Models;
using System.Reflection.Metadata.Ecma335;

namespace fitPass.Controllers
{
    public class InbodyController : Controller
    {
        private readonly GymManagementContext _context;

        public InbodyController(GymManagementContext context)
        {
            _context = context;
        }

        // GET: Inbodies
        public IActionResult Index()
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");
            var record = _context.Inbodies.Where(a => a.MemberId == memberId).OrderBy(a => a.RecordDate).ToList();
            
            return View(record);
        }

        // GET: Inbodies/Create
        public async Task<IActionResult> Create()
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            var latestInbody = await _context.Inbodies.Where(i => i.MemberId == memberId).OrderByDescending(i => i.RecordDate).FirstOrDefaultAsync();
            int? h = latestInbody != null ? latestInbody.Height : null;

            // 創建 Inbody 模型實例，並將預設值賦予它
            var model = new Inbody
            {
                RecordDate = DateOnly.FromDateTime(DateTime.Today),
                Height = h
            };
            return View(model);
        }

        // POST: Inbodies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InBodyId,MemberId,Height,Weight,BodyFat,Bmr,RecordDate,Note,GoalNote")] Inbody inbody)
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (_context.Inbodies.Where(i => i.MemberId == memberId).Any(i => i.RecordDate == inbody.RecordDate))
            {
                inbody.RecordDate = null;
                ViewBag.s = "同一天僅能有一筆資料，如需修改請使用編輯功能。";
                return View("Create",inbody);
            }
            
            inbody.MemberId = memberId.Value;
            _context.Add(inbody);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Inbodies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var memberId = HttpContext.Session.GetInt32("MemberId");
            var inbody = await _context.Inbodies.FirstOrDefaultAsync(i => i.InBodyId == id && i.MemberId == memberId);
            if (inbody == null)
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["MemberId"] = new SelectList(_context.Accounts, "MemberId", "MemberId", inbody.MemberId);
            return View(inbody);
        }

        // POST: Inbodies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InBodyId,MemberId,Height,Weight,BodyFat,Bmr,RecordDate,Note,GoalNote")] Inbody inbody)
        {
            if (id != inbody.InBodyId)
            {
                return NotFound();
            }
            var memberId = HttpContext.Session.GetInt32("MemberId");
            inbody.MemberId = memberId.Value;
            try
            {
                _context.Update(inbody);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InbodyExists(inbody.InBodyId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Inbodies/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var inbody = await _context.Inbodies.FindAsync(id);
            if (inbody != null)
            {
                _context.Inbodies.Remove(inbody);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InbodyExists(int id)
        {
            return _context.Inbodies.Any(e => e.InBodyId == id);
        }
    }
}
