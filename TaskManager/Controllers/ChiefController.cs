﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TaskManager.BusinessLogic.Interfaces;
using TaskManager.BusinessLogic.Models;
using TaskManager.Helpers;
using TaskManager.Models;
using WebMatrix.WebData;

namespace TaskManager.Controllers
{
    [Authorize(Roles = "Chief")]
    public class ChiefController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IUserService _userService;

        public ChiefController(ITaskService taskService, IUserService userService)
        {
            _taskService = taskService;
            _userService = userService;
        }

        public ActionResult Index(ChiefTaskViewModel model)
        {
            if (model == null)
            {
                model = new ChiefTaskViewModel();
            }
            if (model.ChiefTaskList == null)
            {
                model.ChiefTaskList = new List<ChiefTaskList>();
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
            var tasks = _taskService.GetTasks().ToList();

            tasks = tasks.Where(x => model.FilterTaskViewModel.ArchiveFilter ? x.AcceptCpmpleteDate != null : x.AcceptCpmpleteDate == null).ToList();

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
            model.RecipientList =
                new List<SelectListItem>(_userService.GetAllRecipients().Select(item => new SelectListItem
                {
                    Text = item.UserFullName,
                    Value = item.UserId.ToString()
                }));


            tasks.ForEach(x => model.ChiefTaskList.Add(new ChiefTaskList
            {
                AcceptCompleteDate = x.AcceptCpmpleteDate,
                CompleteDate = x.CompleteDate,
                CreationDate = x.CreateDate,
                Deadline = x.Deadline,
                PriorityId = x.TaskPriority != null ? x.TaskPriority.PriorityId.ToString() : "0",
                RecipientId = x.TaskRecipient != null ? x.TaskRecipient.UserId : (int?)null,
                RecipientName = x.TaskRecipient != null ? x.TaskRecipient.UserFullName : "не назначен",
                ResultComment = (x.CompleteDate.HasValue && string.IsNullOrEmpty(x.ResultComment)) ? "выполнено" : x.ResultComment,
                SenderName = x.TaskSender.UserFullName,
                TaskText = x.TaskText,
                TaskId = x.TaskId
            }));

            model.ChiefTaskList = model.ChiefTaskList.OrderBy(x => x.RecipientId.HasValue).ThenByDescending(x => x.CreationDate).ThenBy(x => x.PriorityId).ThenBy(x => x.Deadline).ToList();

            if (Request.IsAjaxRequest())
            {
                return PartialView("TaskListAjax", model);
            }
            return View(model);
        }

        public ActionResult NewTasksCount()
        {
            int count = _taskService.GetNotAssignedTasksCount();

            var res = new BadgeModel { Count = count };
            if (Session["NewTasksForManage"] != null && ((int)Session["NewTasksForManage"]) < count)
            {
                res.IsPlay = true;
            }
            Session["NewTasksForManage"] = count;
            return PartialView(res);
        }

        public ActionResult Edit(int taskId)
        {
            var task = _taskService.GetTaskById(taskId);
            if (task != null)
            {
                var taskViewModel = new ChiefTaskEditViewModel
                {
                    AcceptCompleteDate = task.AcceptCpmpleteDate,
                    CompleteDate = task.CompleteDate,
                    CreationDate = task.CreateDate,
                    Deadline = task.Deadline.HasValue ? task.Deadline.Value.ToString(ModelHelper.DateFormat) : string.Empty,
                    PriorityId = task.TaskPriority != null ? task.TaskPriority.PriorityId.ToString() : "2",
                    PriorityName = task.TaskPriority != null ? task.TaskPriority.PriorityName : string.Empty,
                    RecipientId = task.TaskRecipient != null ? task.TaskRecipient.UserId.ToString() : "0",
                    RecipientName = task.TaskRecipient != null ? task.TaskRecipient.UserFullName : string.Empty,
                    AssignDate = task.AssignDateTime.HasValue ? task.AssignDateTime.Value.ToString(ModelHelper.DateTimeFormatFull) : string.Empty,
                    ResultComment = task.ResultComment,
                    SenderName = task.TaskSender.UserFullName,
                    TaskText = task.TaskText,
                    TaskId = task.TaskId,
                    CommentsCount = task.Comments.Count
                };

                taskViewModel.RecipientsList = new List<SelectListItem>(_userService.GetAllRecipients().Select(item => new SelectListItem
                {
                    Text = item.UserFullName,
                    Value = item.UserId.ToString(),
                    Selected = task.RecipientId == item.UserId
                }));

                taskViewModel.PrioritiesList = new List<SelectListItem>(_taskService.GetPriorityList().Select(item => new SelectListItem
                {
                    Text = item.PriorityName,
                    Value = item.PriorityId.ToString(),
                    Selected = task.PriorityId == item.PriorityId
                }));

                return View(taskViewModel);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Edit(ChiefTaskEditViewModel model)
        {
            var task = _taskService.GetTaskById(model.TaskId);
            if (task != null)
            {
                int recipId;
                if (int.TryParse(model.RecipientId, out recipId))
                {
                    if ((task.RecipientId == null && recipId != 0) || (task.RecipientId != null && task.RecipientId != recipId))
                    {
                        task.TaskEeventLogs.Add(new TaskEventLog
                        {
                            EventDateTime = DateTime.Now,
                            PropertyName = "RecipientId",
                            UserId = WebSecurity.CurrentUserId,
                            OldValue = task.RecipientId.HasValue ? task.RecipientId.Value.ToString() : null,
                            NewValue = recipId == 0 ? null : recipId.ToString()
                        });
                        task.RecipientId = recipId == 0 ? (int?)null : recipId;
                        task.IsRecipientViewed = false;
                    }
                }
                if (recipId == 0)
                {
                    task.AssignDateTime = null;
                    task.Deadline = null;

                }
                else
                {
                    DateTime date;
                    if (DateTime.TryParse(model.Deadline, out date))
                    {
                        if (task.Deadline != date)
                        {
                            task.TaskEeventLogs.Add(new TaskEventLog
                            {
                                EventDateTime = DateTime.Now,
                                PropertyName = "Deadline",
                                UserId = WebSecurity.CurrentUserId,
                                OldValue = task.Deadline.HasValue ? task.Deadline.Value.ToString(Constants.DateFormat) : null,
                                NewValue = date.ToString(Constants.DateFormat)
                            });
                            task.Deadline = date;
                            task.AssignDateTime = DateTime.Now;
                            task.IsRecipientViewed = false;
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Deadline", "Назначьте срок исполнения");
                        model.RecipientsList = new List<SelectListItem>(_userService.GetAllRecipients().Select(item => new SelectListItem
                        {
                            Text = item.UserFullName,
                            Value = item.UserId.ToString(),
                            Selected = task.RecipientId == item.UserId
                        }));

                        model.PrioritiesList = new List<SelectListItem>(_taskService.GetPriorityList().Select(item => new SelectListItem
                        {
                            Text = item.PriorityName,
                            Value = item.PriorityId.ToString(),
                            Selected = task.PriorityId == item.PriorityId
                        }));
                        return View(model);
                    }
                }

                int priorId;
                if (int.TryParse(model.PriorityId, out priorId))
                {
                    if (task.PriorityId != priorId)
                    {
                        task.TaskEeventLogs.Add(new TaskEventLog
                        {
                            EventDateTime = DateTime.Now,
                            PropertyName = "PriorityId",
                            OldValue = task.PriorityId.HasValue ? task.PriorityId.Value.ToString() : null,
                            NewValue = priorId.ToString(),
                            UserId = WebSecurity.CurrentUserId
                        });
                        task.PriorityId = priorId;
                    }
                }
                _taskService.UpdateTask(task);
            }
            return RedirectToAction("Index");
        }
    }
}
