using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.App.Models;

namespace Web.App.Controllers
{
    public class CustomConnectionsController : Controller
    {
        private readonly CustomConnectionContext _context;

        public CustomConnectionsController(CustomConnectionContext context)
        {
            _context = context;
        }

        // GET: CustomConnections
        public async Task<IActionResult> Index()
        {
            return View(await _context.CustomConnection.ToListAsync());
        }

        // GET: CustomConnections/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customConnection = await _context.CustomConnection
                .SingleOrDefaultAsync(m => m.Id == id);
            if (customConnection == null)
            {
                return NotFound();
            }

            return View(customConnection);
        }

        // GET: CustomConnections/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CustomConnections/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Host")] CustomConnection customConnection)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customConnection);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(customConnection);
        }

        // GET: CustomConnections/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(m => m.Id == id);
            if (customConnection == null)
            {
                return NotFound();
            }
            return View(customConnection);
        }

        // POST: CustomConnections/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Name,Host")] CustomConnection customConnection)
        {
            if (id != customConnection.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customConnection);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomConnectionExists(customConnection.Id))
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
            return View(customConnection);
        }

        // GET: CustomConnections/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customConnection = await _context.CustomConnection
                .SingleOrDefaultAsync(m => m.Id == id);
            if (customConnection == null)
            {
                return NotFound();
            }

            return View(customConnection);
        }

        // POST: CustomConnections/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(m => m.Id == id);
            _context.CustomConnection.Remove(customConnection);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomConnectionExists(long id)
        {
            return _context.CustomConnection.Any(e => e.Id == id);
        }
    }
}
