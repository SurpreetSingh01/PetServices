﻿using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using PetServices.Models;

namespace PetServices.Repositories
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly ApplicationDbContext _context;

        public ServiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Service>> GetAllAsync()
        {
            return await _context.Services.ToListAsync();
        }

        public async Task<Service> GetByIdAsync(int id)
        {
            return await _context.Services.FindAsync(id);
        }

        public async Task AddAsync(Service service)
        {
            _context.Services.Add(service);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Service service)
        {
            _context.Services.Update(service);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var service = await GetByIdAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
        }
    }
}
