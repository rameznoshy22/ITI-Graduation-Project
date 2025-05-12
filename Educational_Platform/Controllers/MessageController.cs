﻿using Business_logic_layer.interfaces;
using Data_access_layer.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educational_Platform.Controllers
{
    [Authorize(Roles = "Instructor")]

    public class MessageController : Controller
    {
        private readonly IunitofWork _unitOfWork;

        public MessageController(IunitofWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

        }
        public async Task<IActionResult> Index()
        {
            await SetViewDataCounts();
            var messages = await _unitOfWork.Message.GetAllAsync();
            return View(messages);
        }
        private async Task SetViewDataCounts()
        {
            ViewData["messagesCount"] = await _unitOfWork.Message.GetCountAsync();
            ViewData["StudentCount"] = await _unitOfWork.student_CourseRepo.GetCountAsync();
            ViewData["LessonCount"] = await _unitOfWork.Lesson.GetCountAsync();
            ViewData["RevisionCount"] = await _unitOfWork.Revision.GetCountAsync();
            ViewData["questionCount"] = await _unitOfWork.questions.GetCountAsync();
            ViewData["CourseCount"] = await _unitOfWork.Course.GetCountAsync();
            ViewData["AssessmentCount"] = await _unitOfWork.Assessment.GetCountAsync();
            ViewData["examCount"] = await _unitOfWork.Exam.GetCountAsync();
            ViewData["CommentCount"] = await _unitOfWork.Comment.GetCountAsync();

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _unitOfWork.Message.GetByIdAsync(id);
            if (message == null)
            {
                TempData["error"] = "الرسالة غير موجودة.";
                return RedirectToAction("Index");
            }

            _unitOfWork.Message.DeleteAsync(message);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "تم حذف الرسالة بنجاح.";
            return RedirectToAction("Index");
        }

    }
}
