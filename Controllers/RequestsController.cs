using CrmAtol.Data;
using CrmAtol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CrmAtol.Models.Enums;

namespace CrmAtol.Controllers
{
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RequestsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Вспомогательный метод для фильтрации по ролям
        private async Task<List<Request>> GetFilteredRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return new List<Request>();

            var userRoles = await _userManager.GetRolesAsync(user);

            // Базовый запрос с фильтром по IsDeleted
            var query = _context.Requests
                .Include(r => r.Client)
                .Include(r => r.AssignedToUser)
                .Where(r => r.IsDeleted == false);  // <-- ДОБАВИТЬ ЭТУ СТРОКУ

            if (userRoles.Contains("Admin"))
            {
                return await query.ToListAsync();
            }
            else
            {
                return await query
                    .Where(r => r.AssignedToUserId == user.Id)
                    .ToListAsync();
            }
        }

        // Список заявок с фильтрацией
        public async Task<IActionResult> Index(string status, string priority, string assignedTo)
        {
            var requests = await GetFilteredRequests();

            if (!string.IsNullOrEmpty(status))
                requests = requests.Where(r => r.Status.ToString() == status).ToList();

            if (!string.IsNullOrEmpty(priority))
                requests = requests.Where(r => r.Priority.ToString() == priority).ToList();

            if (!string.IsNullOrEmpty(assignedTo))
                requests = requests.Where(r => r.AssignedToUser != null &&
                                               r.AssignedToUser.UserName.Contains(assignedTo, StringComparison.OrdinalIgnoreCase)).ToList();

            return View(requests);
        }

        // Детали заявки
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.Requests
                .Include(r => r.Client)
                .Include(r => r.AssignedToUser)
                .Where(r => r.IsDeleted == false)  // <-- ДОБАВИТЬ ЭТУ СТРОКУ
                .FirstOrDefaultAsync(m => m.Id == id);
            if (request == null) return NotFound();

            var interactions = await _context.Interactions
                .Where(i => i.RequestId == id)
                .Include(i => i.AuthorUser)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            ViewBag.Interactions = interactions;
            return View(request);
        }

        // Создание заявки (GET)
        public IActionResult Create()
        {
            ViewBag.Clients = _context.Clients.ToList();
            ViewBag.Users = _context.Users.ToList();
            return View();
        }

        // Создание заявки (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Request request)
        {
            // Диагностика
            System.Diagnostics.Debug.WriteLine("=== Метод Create POST вызван ===");
            System.Diagnostics.Debug.WriteLine($"ClientId: {request.ClientId}");
            System.Diagnostics.Debug.WriteLine($"Subject: {request.Subject}");
            System.Diagnostics.Debug.WriteLine($"Priority: {request.Priority}");
            System.Diagnostics.Debug.WriteLine($"AssignedToUserId: {request.AssignedToUserId}");

            try
            {
                // Генерация номера заявки
                var year = DateTime.Now.Year;
                var lastRequest = await _context.Requests
                    .Where(r => r.Number.StartsWith($"ОБ-{year}-"))
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync();

                int nextNum = 1;
                if (lastRequest != null)
                {
                    var parts = lastRequest.Number.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                        nextNum = lastNum + 1;
                }
                request.Number = $"ОБ-{year}-{nextNum:D5}";
                System.Diagnostics.Debug.WriteLine($"Сгенерирован номер: {request.Number}");

                // Расчет сроков SLA
                request.CreatedAt = DateTime.Now;
                request.Status = RequestStatus.Новая;

                switch (request.Priority)
                {
                    case RequestPriority.Низкий:
                        request.DeadlineReaction = DateTime.Now.AddHours(24);
                        request.DeadlineResolution = DateTime.Now.AddHours(72);
                        break;
                    case RequestPriority.Средний:
                        request.DeadlineReaction = DateTime.Now.AddHours(4);
                        request.DeadlineResolution = DateTime.Now.AddHours(24);
                        break;
                    case RequestPriority.Высокий:
                        request.DeadlineReaction = DateTime.Now.AddHours(1);
                        request.DeadlineResolution = DateTime.Now.AddHours(4);
                        break;
                    case RequestPriority.Критический:
                        request.DeadlineReaction = DateTime.Now.AddMinutes(30);
                        request.DeadlineResolution = DateTime.Now.AddHours(2);
                        break;
                }
                System.Diagnostics.Debug.WriteLine($"Срок реакции: {request.DeadlineReaction}");
                System.Diagnostics.Debug.WriteLine($"Срок решения: {request.DeadlineResolution}");

                _context.Add(request);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Заявка успешно сохранена!");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"СТЕК: {ex.StackTrace}");

                ViewBag.Error = ex.Message;
                ViewBag.Clients = _context.Clients.ToList();
                ViewBag.Users = _context.Users.ToList();
                return View(request);
            }
        }

        // Закрытие заявки
        [HttpPost]
        public async Task<IActionResult> Close(int id, string resolution)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = RequestStatus.Закрыта;
            request.ClosedAt = DateTime.Now;
            request.Resolution = resolution;

            _context.Update(request);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        // Добавление взаимодействия
        [HttpPost]
        public async Task<IActionResult> AddInteraction(int requestId, string type, string content)
        {
            // Получаем текущего пользователя
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            var interaction = new Interaction
            {
                RequestId = requestId,
                Type = Enum.Parse<InteractionType>(type),
                Content = content,
                CreatedAt = DateTime.Now,
                AuthorUserId = currentUser.Id  // Присваиваем Id пользователя
            };

            _context.Interactions.Add(interaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        // Редактирование заявки (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.Requests
                .Where(r => r.IsDeleted == false)  // <-- ДОБАВИТЬ ЭТУ СТРОКУ
                .FirstOrDefaultAsync(m => m.Id == id);
            if (request == null) return NotFound();
            ViewBag.Clients = _context.Clients.ToList();
            ViewBag.Users = _context.Users.ToList();
            return View(request);
        }

        // Редактирование заявки (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Request request)
        {
            if (id != request.Id) return NotFound();

            // Удаляем ошибки валидации для навигационных свойств
            ModelState.Remove("Client");
            ModelState.Remove("AssignedToUser");
            
            System.Diagnostics.Debug.WriteLine("=== Метод Edit POST вызван ===");
            System.Diagnostics.Debug.WriteLine($"id: {id}, request.Id: {request.Id}");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRequest = await _context.Requests.FindAsync(id);
                    if (existingRequest == null)
                    {
                        return NotFound();
                    }

                    // Обновляем поля
                    existingRequest.ClientId = request.ClientId;
                    existingRequest.Subject = request.Subject;
                    existingRequest.Description = request.Description;
                    existingRequest.Priority = request.Priority;
                    existingRequest.Status = request.Status;
                    existingRequest.AssignedToUserId = request.AssignedToUserId;

                    if (request.Status == RequestStatus.Закрыта)
                    {
                        existingRequest.ClosedAt = DateTime.Now;
                        existingRequest.Resolution = request.Resolution;
                    }

                    _context.Update(existingRequest);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Details), new { id = existingRequest.Id });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                    ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ModelState не валиден");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка валидации: {error.ErrorMessage}");
                }
            }

            ViewBag.Clients = _context.Clients.ToList();
            ViewBag.Users = _context.Users.ToList();
            return View(request);
        }

        // Удаление заявки (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.Requests
                .Include(r => r.Client)
                .Where(r => r.IsDeleted == false)  // <-- ДОБАВИТЬ ЭТУ СТРОКУ
                .FirstOrDefaultAsync(m => m.Id == id);
            if (request == null) return NotFound();
            return View(request);
        }

        // Удаление заявки (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request != null)
            {
                request.IsDeleted = true;  // Мягкое удаление
                _context.Update(request);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Страница со списком удаленных заявок
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deleted()
        {
            var requests = await _context.Requests
                .Include(r => r.Client)
                .Include(r => r.AssignedToUser)
                .Where(r => r.IsDeleted == true)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // Восстановление заявки
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request != null)
            {
                request.IsDeleted = false;
                _context.Update(request);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Deleted));
        }
    }
}