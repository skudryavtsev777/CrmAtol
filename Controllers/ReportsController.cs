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

            // 1. Статистика для виджетов
            model.TotalRequests = await _context.Requests.CountAsync();
            model.OpenRequests = await _context.Requests.CountAsync(r => r.Status != RequestStatus.Закрыта);
            model.ClosedRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.Закрыта);
            model.OverdueRequests = await _context.Requests
                .CountAsync(r => r.DeadlineResolution < DateTime.Now && r.Status != RequestStatus.Закрыта);

            // 2. Данные для графика "Заявки по статусам"
            model.RequestsByStatus = await _context.Requests
                .GroupBy(r => r.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // 3. Данные для графика "Заявки по приоритетам"
            model.RequestsByPriority = await _context.Requests
                .GroupBy(r => r.Priority)
                .Select(g => new PriorityCount { Priority = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // 4. Данные для графика загрузки сотрудников (Топ-5)
            model.EmployeeLoad = await _context.Requests
                .Where(r => r.AssignedToUserId != null)
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