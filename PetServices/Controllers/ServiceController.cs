using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetServices.Models;
using PetServices.Repositories;

namespace PetServices.Controllers
{
    public class ServiceController : Controller
    {
        private readonly IServiceRepository _serviceRepo;

        public ServiceController(IServiceRepository serviceRepo)
        {
            _serviceRepo = serviceRepo;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _serviceRepo.GetAllAsync();
            return View(services);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            if (ModelState.IsValid)
            {
                await _serviceRepo.AddAsync(service);
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _serviceRepo.GetByIdAsync(id);
            if (service == null) return NotFound();
            return View(service);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _serviceRepo.UpdateAsync(service);
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // GET: Service/Delete/5 (Show confirmation page)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _serviceRepo.GetByIdAsync(id);
            if (service == null)
                return NotFound();

            return View(service); // Renders confirmation view
        }

        // POST: Service/Delete/5 (Actually delete after confirmation)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _serviceRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

    }
}
