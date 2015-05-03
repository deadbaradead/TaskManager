﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TaskManager.BusinessLogic.Interfaces;
using TaskManager.BusinessLogic.Models;
using TaskManager.Helpers;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Authorize(Roles = "MasterChief")]
    public class MasterChiefController : Controller
    {
        private readonly IUserService _userService;
        private readonly ITaskService _taskService;

        public MasterChiefController(IUserService userService, ITaskService taskService)
        {
            _userService = userService;
            _taskService = taskService;
        }

        public ActionResult Index(MasterChiefViewModel model)
        {
            if (model == null)
            {
                model = new MasterChiefViewModel();
            }
            if (model.MasterChiefTaskList == null)
            {
                model.MasterChiefTaskList = new List<MasterChiefTaskList>();
            }

            if (model.FilterTaskViewModel == null)
            {
                model.FilterTaskViewModel = new FilterTaskViewModel
                {
                    ArchiveFilter = false,
                    CompleteFilter = false,
                    StartDateFilter = null,
                    EndDateFilter = null,
                    NotAssignedFilter = false,
                    OverdueFilter = false,
                    SearchText = string.Empty,
                    SelectedRecipient = string.Empty
                };
            }
            List<Task> tasks =
                _taskService.GetTasks()
                    .Where(
                        x =>
                            model.FilterTaskViewModel.ArchiveFilter
                                ? x.AcceptCpmpleteDate != null
                                : x.AcceptCpmpleteDate == null)
                    .ToList();
                        
                    if (model.FilterTaskViewModel.CompleteFilter)
                    {
                        tasks = tasks.Where(x => x.CompleteDate != null).ToList();
                    }
                    if (model.FilterTaskViewModel.NotAssignedFilter)
                    {
                        tasks = tasks.Where(x => x.RecipientId == null).ToList();
                    }
                    if (model.FilterTaskViewModel.OverdueFilter)
                    {
                        tasks = tasks.Where(x => x.Deadline.HasValue && x.Deadline.Value.Date < DateTime.Now.Date).ToList();
                    }
                    if (!string.IsNullOrEmpty(model.FilterTaskViewModel.SelectedRecipient) && model.FilterTaskViewModel.SelectedRecipient != "0")
                    {
                        tasks = tasks.Where(x => x.RecipientId.HasValue && x.RecipientId.ToString().Equals(model.FilterTaskViewModel.SelectedRecipient)).ToList();
                    }
                    if (model.FilterTaskViewModel.StartDateFilter.HasValue)
                    {
                        tasks = tasks.Where(x => x.CreateDate >= model.FilterTaskViewModel.StartDateFilter.Value).ToList();
                    }
                    if (model.FilterTaskViewModel.EndDateFilter.HasValue)
                    {
                        tasks = tasks.Where(x => x.AcceptCpmpleteDate.HasValue && x.AcceptCpmpleteDate <= model.FilterTaskViewModel.EndDateFilter.Value).ToList();
                    }
                    if (!string.IsNullOrEmpty(model.FilterTaskViewModel.SearchText))
                    {
                        tasks = tasks.Where(x => x.TaskText.ToLower().Contains(model.FilterTaskViewModel.SearchText.ToLower())).ToList();
                    }
                    model.RecipientList = new List<SelectListItem>(_userService.GetAllRecipients().Select(item => new SelectListItem
                    {
                        Text = item.UserFullName,
                        Value = item.UserId.ToString()
                    }));
            

            tasks.ForEach(x => model.MasterChiefTaskList.Add(new MasterChiefTaskList
            {
                AcceptCompleteDate = x.AcceptCpmpleteDate,
                CompleteDate = x.CompleteDate,
                CreationDate = x.CreateDate,
                Deadline = x.Deadline,
                Priority = x.Priority,
                RecipientId = x.RecipientId,
                RecipientName = x.RecipientId.HasValue ? _userService.GetUserById(x.RecipientId.Value).UserFullName : "не назначен",
                ResultComment = x.ResultComment,
                SenderName = _userService.GetUserById(x.SenderId).UserFullName,
                TaskText = x.TaskText,
                TaskId = x.TaskId
            }));

            model.MasterChiefTaskList = model.MasterChiefTaskList.OrderBy(x => x.RecipientId.HasValue).ThenByDescending(x => x.CreationDate).ThenBy(x => x.Deadline).ToList();

            if (Request.IsAjaxRequest())
            {
                return PartialView("TaskListAjax", model);
            }
            return View(model);
        }

        public ActionResult Edit(int taskId)
        {
            Task task = _taskService.GetTaskById(taskId);

            if (task != null)
            {
                var taskViewModel = new ChiefTaskEditViewModel
                {
                    AcceptCompleteDate = task.AcceptCpmpleteDate,
                    CompleteDate = task.CompleteDate,
                    CreationDate = task.CreateDate,
                    Deadline =
                        task.Deadline.HasValue ? task.Deadline.Value.ToString(Constants.DateFormat) : string.Empty,
                    Priority = task.Priority,
                    RecipientId = task.RecipientId.ToString(),
                    RecipientName = task.RecipientId.HasValue ? _userService.GetUserById(task.RecipientId.Value).UserFullName : string.Empty,
                    AssignDate =
                        task.AssignDateTime.HasValue
                            ? task.AssignDateTime.Value.ToString(Constants.DateFullFormat)
                            : string.Empty,
                    ResultComment = task.ResultComment,
                    SenderName = _userService.GetUserById(task.SenderId).UserFullName,
                    TaskText = task.TaskText,
                    TaskId = task.TaskId,
                    CommentsCount = _taskService.GetCommentsForTask(task.TaskId).Count()
                };

                taskViewModel.RecipientsList =
                    new List<SelectListItem>(_userService.GetAllRecipients().Select(item => new SelectListItem
                    {
                        Text = item.UserFullName,
                        Value = item.UserId.ToString(),
                        Selected = task.RecipientId == item.UserId
                    }));

                return View(taskViewModel);
            }
            
            return RedirectToAction("Index");
        }

        public ActionResult OverdueTasksCount()
        {
            int count;
            count = _taskService.GetOverdueTasks().Count();
            var res = new BadgeModel {Count = count};
            if (Session["NewTasksForManage"] != null && ((int) Session["NewTasksForManage"]) < count)
            {
                res.IsPlay = true;
            }
            Session["OverdueTasksForManage"] = count;
            return PartialView(res);
        }
    }
}