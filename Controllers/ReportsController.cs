using CrmAtol.Data;
using CrmAtol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CrmAtol.Models.Enums;

namespace CrmAtol.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Главная страница с дашбордом
        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel();

            // Статистика для виджетов (только не удаленные заявки)
            model.TotalRequests = await _context.Requests.CountAsync(r => r.IsDeleted == false);
            model.OpenRequests = await _context.Requests.CountAsync(r => r.IsDeleted == false && r.Status != RequestStatus.Закрыта);
            model.ClosedRequests = await _context.Requests.CountAsync(r => r.IsDeleted == false && r.Status == RequestStatus.Закрыта);
            model.OverdueRequests = await _context.Requests
                .CountAsync(r => r.IsDeleted == false && r.DeadlineResolution < DateTime.Now && r.Status != RequestStatus.Закрыта);

            // Данные для графика "Заявки по статусам"
            model.RequestsByStatus = await _context.Requests
                .Where(r => r.IsDeleted == false)
                .GroupBy(r => r.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // Данные для графика "Заявки по приоритетам"
            model.RequestsByPriority = await _context.Requests
                .Where(r => r.IsDeleted == false)
                .GroupBy(r => r.Priority)
                .Select(g => new PriorityCount { Priority = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // Данные для графика загрузки сотрудников (только активные, не удаленные заявки)
            model.EmployeeLoad = await _context.Requests
                .Where(r => r.IsDeleted == false && r.AssignedToUserId != null && r.Status != RequestStatus.Закрыта)
                .GroupBy(r => r.AssignedToUser.UserName)
                .Select(g => new EmployeeLoad { UserName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            return View(model);
        }

        // Страница с детальными отчетами (опционально)
        public async Task<IActionResult> Index()
        {
            // Можно передать список заявок или другой аналитики
            var requests = await _context.Requests
                .Include(r => r.Client)
                .Include(r => r.AssignedToUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(requests);
        }


    }
}